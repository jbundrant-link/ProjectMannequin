using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public sealed class WorldWarriorBossSmokeScenario
{
    private readonly GameSimulation _simulation;
    private int _startedTick = -1;
    private bool _bossProfileValid;
    private bool _sawProjectile;
    private bool _sawProjectileHit;
    private bool _sawShoryuken;
    private bool _sawFormUnlock;
    private bool _sawStageComplete;
    private bool _formApplied;
    private bool _summaryPrinted;

    public WorldWarriorBossSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
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
            _bossProfileValid = boss.CurrentForm.Id == "world_warrior_ryu_boss"
                && boss.CurrentForm.CpuProfile is not null
                && boss.CurrentForm.BossPhases.Count == 3
                && boss.CurrentForm.SpriteSheetPath.EndsWith("ryu_higgsfield_v4_sheet.png")
                && boss.CurrentForm.SpriteSheetColumns == 16
                && boss.CurrentForm.SpriteSheetRows == 15
                && ResourceLoader.Exists("res://Assets/Sprites/Ryu/ryu_higgsfield_v5_actions.png")
                && new[]
                {
                    "ryu_light_kick",
                    "ryu_medium_kick",
                    "ryu_heavy_kick",
                    "ryu_close_jab",
                    "ryu_close_strong",
                    "ryu_close_fierce",
                    "ryu_close_medium_kick",
                    "ryu_crouch_jab",
                    "ryu_crouch_strong",
                    "ryu_crouch_fierce",
                    "ryu_crouch_short",
                    "ryu_crouch_medium_kick",
                    "ryu_air_jab",
                    "ryu_air_strong",
                    "ryu_air_fierce",
                    "ryu_air_short",
                    "ryu_air_forward",
                    "ryu_air_roundhouse",
                    "ryu_collarbone_breaker",
                    "ryu_solar_plexus",
                    "ryu_leap_attack",
                    "ryu_shoulder_throw",
                    "ryu_back_throw",
                    "ryu_hadouken_light",
                    "ryu_hadouken_medium",
                    "ryu_hadouken_heavy",
                    "ryu_hadouken_ex",
                    "ryu_shoryuken_light",
                    "ryu_shoryuken_medium",
                    "ryu_shoryuken_heavy",
                    "ryu_shoryuken_ex",
                    "ryu_tatsumaki_light",
                    "ryu_tatsumaki_medium",
                    "ryu_tatsumaki_heavy",
                    "ryu_tatsumaki_ex",
                    "ryu_air_tatsumaki_light",
                    "ryu_air_tatsumaki_medium",
                    "ryu_air_tatsumaki_heavy",
                    "ryu_air_tatsumaki_ex",
                    "ryu_joudan_light",
                    "ryu_joudan_medium",
                    "ryu_joudan_heavy",
                    "ryu_joudan_ex",
                    "ryu_shinku_hadouken",
                    "ryu_shin_shoryuken",
                    "ryu_denjin_hadouken",
                }.All(moveId => boss.CurrentForm.FindMove(moveId) is not null)
                && new[]
                {
                    "ryu_collarbone_breaker",
                    "ryu_solar_plexus",
                    "ryu_shoulder_throw",
                    "ryu_back_throw",
                    "ryu_hadouken_ex",
                    "ryu_shoryuken_ex",
                    "ryu_tatsumaki_ex",
                    "ryu_air_tatsumaki_ex",
                    "ryu_joudan_ex",
                    "ryu_shinku_hadouken",
                    "ryu_shin_shoryuken",
                    "ryu_denjin_hadouken",
                }.All(moveId =>
                    boss.CurrentForm.FindMove(moveId)?.AnimationFrameSequence.Count > 0)
                && ValidateMoveCommandsAndCancels(boss)
                && boss.UnlockableFormOnDefeat?.Id == "world_warrior_ryu_form";
            if (!_bossProfileValid)
            {
                LogProfileFailures(boss);
            }
            GD.Print("[WorldWarriorSmoke] Boss profile initialized.");
        }

        var elapsed = tick - _startedTick;
        var arenaCenterX = _simulation.EncounterDirector?.CurrentEncounter.CameraLockX ?? 94.0f;
        switch (elapsed)
        {
            case 5:
                PrepareActors(player, boss, playerX: arenaCenterX - 2.0f, bossX: arenaCenterX + 2.0f);
                boss.FacingRight = false;
                StartMove(boss, "ryu_hadouken_heavy", tick);
                break;
            case 65:
                PrepareActors(player, boss, playerX: arenaCenterX + 1.2f, bossX: arenaCenterX);
                boss.FacingRight = true;
                StartMove(boss, "ryu_shoryuken_heavy", tick);
                break;
            case 112:
                boss.EndCurrentMove();
                boss.IsVulnerable = true;
                var finisher = player.CurrentForm.FindMove("mannequin_heavy");
                if (finisher is not null)
                {
                    boss.ApplyHit(new HitApplication(
                        player,
                        boss,
                        finisher,
                        boss.Health + 1,
                        finisher.HitstunFrames,
                        finisher.BlockstunFrames,
                        Vector3.Zero,
                        false), tick);
                }
                break;
        }

        if (!_sawProjectileHit && elapsed is >= 10 and < 60)
        {
            var projectile = _simulation.Projectiles.FirstOrDefault(candidate =>
                candidate.Move.Id == "ryu_hadouken_heavy");
            if (projectile is not null)
            {
                var travelSign = Mathf.Sign(projectile.Velocity.X);
                player.SimPosition = new Vector3(
                    projectile.SimPosition.X + travelSign * 0.12f,
                    0.0f,
                    projectile.SimPosition.Z);
                player.Position = player.SimPosition;
            }
        }

        var unlockedForm = player.FormArchive.GetForm("world_warrior_ryu_form");
        _sawFormUnlock |= unlockedForm is not null;
        if (!_formApplied && unlockedForm is not null && boss.IsDead)
        {
            player.SetForm(unlockedForm);
            _formApplied = true;
        }
    }

    public void CaptureAfterSimulation(
        int tick,
        IReadOnlyCollection<CombatPresentationEvent> events,
        ArcadeEncounterDirector? director)
    {
        _sawProjectile |= _simulation.Projectiles.Any(projectile =>
            projectile.Move.Id == "ryu_hadouken_heavy");
        _sawProjectileHit |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.HitConnected
            && presentationEvent.Payload.StartsWith("ryu_hadouken_heavy|"));
        _sawShoryuken |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.MoveStarted
            && presentationEvent.Payload == "Heavy Shoryuken");
        _sawFormUnlock |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.FormUnlocked
            && presentationEvent.Payload == "world_warrior_ryu_form");
        _sawStageComplete |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.StageCompleted
            && presentationEvent.Payload.StartsWith("world_warrior_sector_mvp"));

        if (_startedTick < 0 || tick - _startedTick < 155 || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var passed = _bossProfileValid
            && _sawProjectile
            && _sawProjectileHit
            && _sawShoryuken
            && _sawFormUnlock
            && _sawStageComplete
            && director?.State == ArcadeStageState.Complete;
        GD.Print(
            $"[WorldWarriorSmoke] SUMMARY passed={passed} profile={_bossProfileValid} " +
            $"projectile={_sawProjectile}/{_sawProjectileHit} shoryuken={_sawShoryuken} " +
            $"unlock={_sawFormUnlock} complete={_sawStageComplete}");
        if (!passed)
        {
            GD.PushError("[WorldWarriorSmoke] World Warrior boss loop failed.");
        }
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
    }

    private static void StartMove(CombatActor boss, string moveId, int tick)
    {
        var move = boss.CurrentForm.FindMove(moveId);
        if (move is not null)
        {
            boss.TryStartMove(move, tick);
        }
    }

    private static void LogProfileFailures(CombatActor boss)
    {
        var requiredMoveIds = new[]
        {
            "ryu_crouch_strong",
            "ryu_crouch_fierce",
            "ryu_crouch_short",
            "ryu_close_jab",
            "ryu_close_strong",
            "ryu_close_fierce",
            "ryu_close_medium_kick",
            "ryu_air_strong",
            "ryu_air_short",
            "ryu_air_forward",
            "ryu_collarbone_breaker",
            "ryu_solar_plexus",
            "ryu_leap_attack",
            "ryu_shoulder_throw",
            "ryu_back_throw",
            "ryu_hadouken_ex",
            "ryu_shoryuken_ex",
            "ryu_tatsumaki_ex",
            "ryu_air_tatsumaki_ex",
            "ryu_joudan_ex",
            "ryu_shinku_hadouken",
            "ryu_shin_shoryuken",
            "ryu_denjin_hadouken",
        };
        var missingMoves = requiredMoveIds
            .Where(moveId => boss.CurrentForm.FindMove(moveId) is null)
            .ToArray();
        var missingAnimations = requiredMoveIds
            .Where(moveId =>
                boss.CurrentForm.FindMove(moveId)?.AnimationFrameSequence.Count <= 0)
            .ToArray();
        GD.Print(
            $"[WorldWarriorSmoke] PROFILE resourceV5=" +
            $"{ResourceLoader.Exists("res://Assets/Sprites/Ryu/ryu_higgsfield_v5_actions.png")} " +
            $"moves={boss.CurrentForm.Moves.Count} missingMoves=[{string.Join(",", missingMoves)}] " +
            $"missingAnimations=[{string.Join(",", missingAnimations)}] " +
            $"unlock={boss.UnlockableFormOnDefeat?.Id}");
    }

    private static bool ValidateMoveCommandsAndCancels(CombatActor boss)
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

                if (move.CancelIntoMoveIds.Any(
                        moveId => boss.CurrentForm.FindMove(moveId) is null))
                {
                    return false;
                }
            }
        }
        catch (System.ArgumentException exception)
        {
            GD.Print($"[WorldWarriorSmoke] Command validation failed: {exception.Message}");
            return false;
        }

        return true;
    }

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private CombatActor? GetBoss()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsBoss);
    }
}
