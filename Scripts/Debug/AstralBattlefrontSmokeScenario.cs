using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public sealed class AstralBattlefrontSmokeScenario
{
    private readonly GameSimulation _simulation;
    private readonly HashSet<string> _seenVariants = new();
    private readonly bool _renderTest;
    private int _startedTick = -1;
    private bool _profileValid;
    private bool _sawBeam;
    private bool _sawFlight;
    private bool _sawInstinctEvade;
    private bool _sawUnlock;
    private bool _sawStageComplete;
    private bool _sawPlayableVariantCycle;
    private bool _formApplied;
    private bool _bossFinishApplied;
    private bool _playableCycleStarted;
    private bool _summaryPrinted;
    private int _nextDamageMilestoneIndex;
    private int _nextDamageAllowedTick;

    public AstralBattlefrontSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
        _renderTest =
            OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_RENDER_TEST") == "1";
    }

    public void UpdateBeforeSimulation(int tick)
    {
        var player = GetPlayer();
        var boss = GetBoss();
        if (player is null || boss is null)
        {
            return;
        }

        if (_startedTick < 0)
        {
            _startedTick = tick;
            boss.IsAiEnabled = false;
            player.IsVulnerable = true;
            _profileValid = ValidateProfile(boss);
            GD.Print($"[AstralSmoke] Boss profile initialized valid={_profileValid}.");
        }

        var elapsed = tick - _startedTick;
        if (_renderTest)
        {
            UpdateRenderTest(player, boss, elapsed, tick);
            return;
        }

        switch (elapsed)
        {
            case 5:
                PrepareActors(player, boss, playerX: 108.0f, bossX: 112.0f);
                boss.FacingRight = false;
                StartMove(boss, "goku_kamehameha_heavy", tick);
                break;
            case 405:
                PrepareActors(player, boss, playerX: 107.0f, bossX: 110.0f);
                StartMove(boss, "goku_flight_start", tick);
                break;
            case 530:
                PrepareActors(player, boss, playerX: 107.5f, bossX: 109.0f);
                var testMove = player.CurrentForm.FindMove("mannequin_heavy");
                if (testMove is not null)
                {
                    boss.StateMachine.TryResolveInstinctEvade(player, testMove, tick);
                }
                break;
            case 720:
                PrepareActors(player, boss, playerX: 107.5f, bossX: 109.0f);
                var masteredTestMove = player.CurrentForm.FindMove("mannequin_heavy");
                if (masteredTestMove is not null)
                {
                    boss.StateMachine.TryResolveInstinctEvade(player, masteredTestMove, tick);
                }
                break;
        }

        if (_nextDamageMilestoneIndex < DamageMilestones.Length
            && elapsed >= DamageMilestones[_nextDamageMilestoneIndex].Elapsed
            && tick >= _nextDamageAllowedTick)
        {
            ApplyScriptedDamageToHealth(
                player,
                boss,
                DamageMilestones[_nextDamageMilestoneIndex].TargetHealth,
                tick);
            _nextDamageMilestoneIndex++;
            _nextDamageAllowedTick = tick + 20;
        }

        if (!_bossFinishApplied
            && elapsed >= 770
            && _seenVariants.Count == RequiredVariantIds.Length)
        {
            // Phase transitions grant defense, so use an intentionally lethal
            // smoke-test hit after every transformation has been observed.
            ApplyScriptedDamage(player, boss, boss.Health * 4, tick);
            _bossFinishApplied = true;
        }

        var unlockedForm = player.FormArchive.GetForm("goku_archive_form");
        _sawUnlock |= unlockedForm is not null;
        if (!_formApplied && unlockedForm is not null && boss.IsDead)
        {
            player.SetForm(unlockedForm);
            _formApplied = true;
            GD.Print(
                $"[AstralSmoke] Playable form applied at tick {tick}; "
                + $"director handoff pending.");
        }

        if (_formApplied && !_playableCycleStarted && elapsed >= 810)
        {
            player.EndCurrentMove();
            StartMove(player, "goku_transform_next", tick);
            _playableCycleStarted = true;
        }
    }

    private static void UpdateRenderTest(
        CombatActor player,
        CombatActor boss,
        int elapsed,
        int tick)
    {
        switch (elapsed)
        {
            case 5:
                PrepareActors(player, boss, playerX: 112.0f, bossX: 106.0f);
                boss.FacingRight = true;
                break;
            case 20:
            case 80:
            case 140:
            case 200:
                PrepareActors(player, boss, playerX: 112.0f, bossX: 106.0f);
                boss.FacingRight = true;
                StartMove(boss, "goku_kamehameha_heavy", tick);
                break;
        }
    }

    public void CaptureAfterSimulation(
        int tick,
        IReadOnlyCollection<CombatPresentationEvent> events,
        ArcadeEncounterDirector? director)
    {
        _sawBeam |= _simulation.Projectiles.Any(projectile =>
            projectile.Move.Id == "goku_kamehameha_heavy");
        var boss = GetBoss();
        if (!string.IsNullOrWhiteSpace(boss?.CurrentVisualVariantId))
        {
            _seenVariants.Add(boss.CurrentVisualVariantId);
        }
        _sawFlight |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.FlightStarted);
        _sawInstinctEvade |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.InstinctEvaded);
        _sawUnlock |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.FormUnlocked
            && presentationEvent.Payload == "goku_archive_form");
        _sawStageComplete |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.StageCompleted
            && presentationEvent.Payload.StartsWith("astral_battlefront_mvp"));
        var player = GetPlayer();
        _sawPlayableVariantCycle |= player?.CurrentForm.Id == "goku_archive_form"
            && player.CurrentVisualVariantId == "kaioken";

        if (_startedTick < 0 || tick - _startedTick < 850 || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var passed = _profileValid
            && _sawBeam
            && _sawFlight
            && _seenVariants.Count == RequiredVariantIds.Length
            && _sawInstinctEvade
            && _sawUnlock
            && _sawPlayableVariantCycle
            && _sawStageComplete
            && director?.State == ArcadeStageState.Complete;
        GD.Print(
            $"[AstralSmoke] SUMMARY passed={passed} profile={_profileValid} beam={_sawBeam} " +
            $"forms={_seenVariants.Count}/{RequiredVariantIds.Length} flight={_sawFlight} instinct={_sawInstinctEvade} " +
            $"unlock={_sawUnlock} playableCycle={_sawPlayableVariantCycle} complete={_sawStageComplete} "
            + $"form={player?.CurrentForm.Id ?? "none"} state={director?.State} "
            + $"missing={string.Join(",", RequiredVariantIds.Where(id => !_seenVariants.Contains(id)))}");
        if (!passed)
        {
            GD.PushError("[AstralSmoke] Astral Battlefront boss loop failed.");
        }
    }

    private static bool ValidateProfile(CombatActor boss)
    {
        var requiredMoves = new[]
        {
            "goku_lp", "goku_mp", "goku_hp", "goku_lk", "goku_mk", "goku_hk",
            "goku_2lp", "goku_2mp", "goku_2hp", "goku_2lk", "goku_2mk", "goku_2hk",
            "goku_air_lp", "goku_air_mp", "goku_air_hp",
            "goku_air_lk", "goku_air_mk", "goku_air_hk",
            "goku_kamehameha_light", "goku_kamehameha_heavy",
            "goku_dragon_rising", "goku_dragon_flash", "goku_instant_step",
            "goku_meteor_smash", "goku_flight_start", "goku_flight_cancel",
            "goku_flight_ki_blast", "goku_flight_rush", "goku_flight_dive",
            "goku_super_kamehameha", "goku_god_kamehameha",
            "goku_spirit_bomb", "goku_instinct_rush", "goku_transform_next",
        };
        var commandsValid = ValidateCommands(boss);
        var graphicsValid = ValidateGraphics(boss);
        return boss.CurrentForm.Id == "astral_goku_boss"
            && boss.CurrentForm.AnimationProfileId == "goku"
            && boss.CurrentForm.CpuProfile is not null
            && boss.CurrentForm.FlightProfile is not null
            && boss.CurrentForm.BossPhases.Count == RequiredVariantIds.Length
            && boss.CurrentForm.VisualVariants.Count == RequiredVariantIds.Length
            && boss.CurrentForm.BossPhases
                .Select(phase => phase.VisualVariantId)
                .SequenceEqual(RequiredVariantIds)
            && requiredMoves.All(moveId => boss.CurrentForm.FindMove(moveId) is not null)
            && boss.CurrentForm.Moves.All(move =>
                move.Id is "goku_flight_start" or "goku_flight_cancel"
                || move.AnimationFrameSequence.Count > 0)
            && boss.UnlockableFormOnDefeat?.Id == "goku_archive_form"
            && commandsValid
            && graphicsValid;
    }

    private static bool ValidateGraphics(CombatActor boss)
    {
        var specialMoveIds = new[]
        {
            "goku_kamehameha_light", "goku_kamehameha_heavy",
            "goku_dragon_rising", "goku_dragon_flash", "goku_instant_step",
            "goku_meteor_smash", "goku_flight_cancel", "goku_flight_ki_blast",
            "goku_flight_rush", "goku_flight_dive", "goku_super_kamehameha",
            "goku_god_kamehameha", "goku_spirit_bomb", "goku_instinct_rush",
        };
        var variantsValid = boss.CurrentForm.VisualVariants.All(variant =>
            ResourceLoader.Exists(variant.SpriteSheetPath));
        var specialAtlasesValid = specialMoveIds.All(moveId =>
        {
            var move = boss.CurrentForm.FindMove(moveId);
            return move is not null
                && ResourceLoader.Exists(move.AnimationAtlasPath)
                && RequiredVariantIds.All(variantId =>
                    move.AnimationVariantAtlasPaths.TryGetValue(variantId, out var path)
                    && ResourceLoader.Exists(path));
        });
        var transitionPhases = boss.CurrentForm.BossPhases.Skip(1).ToArray();
        var transitionsValid = transitionPhases.Length == RequiredVariantIds.Length - 1
            && transitionPhases.All(phase =>
                phase.TransitionAnimation is { Frames.Count: > 0 } transition
                && ResourceLoader.Exists(transition.AtlasPath))
            && transitionPhases.Single(phase => phase.VisualVariantId == "blue")
                .TransitionAnimation is { Frames.Count: 17 }
            && transitionPhases.Single(phase => phase.VisualVariantId == "instinct")
                .TransitionAnimation is { Frames.Count: 64 };
        var signVariant = boss.CurrentForm.FindVisualVariant("ui_sign");
        var instinctVariant = boss.CurrentForm.FindVisualVariant("instinct");
        var evadeValid = new[] { signVariant, instinctVariant }.All(variant =>
            variant?.InstinctEvadeAnimation is { Frames.Count: > 0 } evade
            && ResourceLoader.Exists(evade.AtlasPath));
        var projectileVisualsValid = boss.CurrentForm.Moves
            .SelectMany(move => move.ProjectileSpawns)
            .Where(spawn => spawn.Id.StartsWith("goku_"))
            .All(spawn =>
                !string.IsNullOrWhiteSpace(spawn.VisualAtlasPath)
                && ResourceLoader.Exists(spawn.VisualAtlasPath)
                && spawn.VisualAtlasColumns == (
                    spawn.VisualType == ProjectileVisualType.Beam ? 2 : 4)
                && spawn.VisualAtlasRows == 2
                && spawn.VisualFrameSequence.Count > 0);
        return variantsValid
            && specialAtlasesValid
            && transitionsValid
            && evadeValid
            && projectileVisualsValid;
    }

    private static bool ValidateCommands(CombatActor boss)
    {
        var interpreter = new CommandInterpreter();
        try
        {
            foreach (var move in boss.CurrentForm.Moves)
            {
                if (!string.IsNullOrWhiteSpace(move.InputCommand))
                {
                    interpreter.Parse(
                        move.Id,
                        move.InputCommand,
                        move.Priority,
                        move.InputWindowFrames,
                        move.DirectionLeniency);
                }
            }
        }
        catch (System.ArgumentException exception)
        {
            GD.Print($"[AstralSmoke] Command validation failed: {exception.Message}");
            return false;
        }

        return true;
    }

    private static void PrepareActors(
        CombatActor player,
        CombatActor boss,
        float playerX,
        float bossX)
    {
        player.EndCurrentMove();
        boss.EndCurrentMove();
        player.SimPosition = new Vector3(playerX, 0.0f, 0.0f);
        boss.SimPosition = new Vector3(bossX, 0.0f, 0.0f);
        player.Velocity = Vector3.Zero;
        boss.Velocity = Vector3.Zero;
        player.FacingRight = bossX >= playerX;
        boss.FacingRight = playerX >= bossX;
        player.State = CombatActorState.Idle;
        boss.State = CombatActorState.Idle;
        boss.IsVulnerable = true;
    }

    private static void StartMove(CombatActor boss, string moveId, int tick)
    {
        var move = boss.CurrentForm.FindMove(moveId);
        if (move is not null)
        {
            boss.TryStartMove(move, tick);
        }
    }

    private static void ApplyScriptedDamage(
        CombatActor player,
        CombatActor boss,
        int damage,
        int tick)
    {
        boss.EndCurrentMove();
        boss.IsVulnerable = true;
        var move = player.CurrentForm.FindMove("mannequin_heavy");
        if (move is null)
        {
            return;
        }

        boss.ApplyHit(new HitApplication(
            player,
            boss,
            move,
            damage,
            move.HitstunFrames,
            move.BlockstunFrames,
            Vector3.Zero,
            false), tick);
    }

    private static void ApplyScriptedDamageToHealth(
        CombatActor player,
        CombatActor boss,
        int targetHealth,
        int tick)
    {
        var requiredFinalDamage =
            Mathf.Max(1, boss.Health - Mathf.Max(1, targetHealth));
        var defenseScale = Mathf.Max(
            0.01f,
            (100 - boss.DefenseModifierPercent) / 100.0f);
        ApplyScriptedDamage(
            player,
            boss,
            Mathf.CeilToInt(requiredFinalDamage / defenseScale),
            tick);
    }

    private static readonly string[] RequiredVariantIds =
    {
        "base",
        "kaioken",
        "false_super",
        "ss1",
        "ss2",
        "ss3",
        "ss4",
        "god",
        "blue",
        "blue_kaioken",
        "ui_sign",
        "instinct",
    };

    private static readonly (int Elapsed, int TargetHealth)[] DamageMilestones =
    {
        (60, 1320),
        (100, 1190),
        (140, 1080),
        (180, 960),
        (220, 830),
        (260, 715),
        (300, 600),
        (340, 470),
        (450, 350),
        (490, 235),
        (570, 110),
    };

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private CombatActor? GetBoss()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsBoss);
    }
}
