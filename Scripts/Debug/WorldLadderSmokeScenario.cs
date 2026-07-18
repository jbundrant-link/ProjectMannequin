using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Stage;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

public sealed class WorldLadderSmokeScenario
{
    private const int EnemyCaptureMinimumWarmupFrames = 30;
    private const int EnemyCaptureOverlayTimeoutFrames = 300;

    private readonly GameSimulation _simulation;
    private readonly MvpHud? _hud;
    private int _nextDefeatTick = 80;
    private bool _sawEliteSpawn;
    private bool _eliteHadArcadeBrain;
    private bool _sawStageCompleted;
    private bool _sawFormUnlock;
    private bool _sawResults;
    private bool _sawHazardWarning;
    private bool _sawEliteIntroStarted;
    private bool _sawEliteIntroReady;
    private bool _sawEliteIntroFight;
    private bool _sawEliteLifeBar;
    private bool _sawAwaitingFormSwap;
    private bool _sawNarrowVaultLane;
    private bool _sawWideVaultLane;
    private bool _requestedNarrowLaneClamp;
    private bool _sawNarrowLaneClamp;
    private bool _sawPropExplosion;
    private bool _sawPropExplosionDamage;
    private int _hordesCleared;
    private int _encounterActiveStartedTick = -1;
    private bool _summaryPrinted;
    private bool _stageCaptureSaved;
    private bool _cacheCaptureSaved;
    private int _cacheObservedTick = -1;
    private int _cacheCaptureSavedTick = -1;
    private bool _pickupCaptureSaved;
    private int _pickupObservedTick = -1;
    private string _pickupCaptureActorId = "";
    private Vector3 _pickupCapturePosition;
    private string _raiderCaptureActorId = "";
    private int _raiderCaptureStartedTick = -1;
    private int _raiderCaptureArmedTick = -1;
    private int _raiderAttackHoldStartedTick = -1;
    private bool _raiderCaptureFinished;
    private bool _raiderOriginalAiEnabled;
    private bool _raiderOriginalVulnerable;
    private readonly HashSet<string> _raiderHiddenActorIds = new();
    private readonly HashSet<string> _stageCaptureHiddenActorIds = new();
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private readonly HashSet<string> _clearedEncounterIds = new();
    private readonly HashSet<string> _hazardWarnings = new();

    public WorldLadderSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
        _hud = simulation.GetNodeOrNull<MvpHud>("../MvpHud");
        PrepareEnemyCaptureOutput(EnemyIdleCaptureVariable());
        PrepareEnemyCaptureOutput(EnemyAttackCaptureVariable());
        OS.SetEnvironment(EnemyActorIdVariable(), "");
        GD.Print("[LadderSmoke] Driver active.");
    }

    public void UpdateBeforeSimulation(int tick, ArcadeEncounterDirector director)
    {
        CapturePublishedEvents();
        var player = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        if (player is null)
        {
            return;
        }

        player.IsVulnerable = false;
        if (director.Mission.WorldId == "archive_nexus"
            && director.Mission.StageNumber == 2)
        {
            var encounterIndex = director.Mission.Encounters.FindIndex(encounter =>
                encounter.Id == director.CurrentEncounter.Id);
            var laneWidth = director.CurrentLaneMaxZ - director.CurrentLaneMinZ;
            _sawNarrowVaultLane |= encounterIndex == 0
                && System.Math.Abs(laneWidth - 3.30f) < 0.06f;
            _sawWideVaultLane |= encounterIndex == 1
                && System.Math.Abs(laneWidth - 5.50f) < 0.06f;
            if (!_requestedNarrowLaneClamp
                && director.State == ArcadeStageState.EncounterActive
                && encounterIndex == 0
                && _sawNarrowVaultLane)
            {
                player.SimPosition = new Vector3(
                    player.SimPosition.X,
                    player.SimPosition.Y,
                    director.Mission.LaneMaxZ);
                _requestedNarrowLaneClamp = true;
            }
        }
        _sawEliteLifeBar |= director.State == ArcadeStageState.EliteIntro
            && _hud is { BossLifeBarVisible: true, BossHealthBarValue: > 0.0 };
        if (director.State == ArcadeStageState.AwaitingReward
            && director.PendingRewardOptions.Count > 0)
        {
            director.RequestRewardChoice(0);
            return;
        }

        if (director.State == ArcadeStageState.Traveling)
        {
            player.SimPosition = new Vector3(
                Mathf.Min(player.SimPosition.X + 0.34f, director.CurrentEncounter.TriggerX + 0.1f),
                player.SimPosition.Y,
                player.SimPosition.Z);
            return;
        }

        _sawAwaitingFormSwap |= director.State == ArcadeStageState.AwaitingFormSwap;
        if (director.State == ArcadeStageState.AwaitingFormSwap)
        {
            var unlockedForm = player.FormArchive.GetForm(director.Mission.BossFormId);
            if (unlockedForm is not null && player.CurrentForm.Id != unlockedForm.Id)
            {
                player.SetForm(unlockedForm);
            }
            return;
        }

        if (director.State != ArcadeStageState.EncounterActive)
        {
            _encounterActiveStartedTick = -1;
            return;
        }

        if (_encounterActiveStartedTick < 0)
        {
            _encounterActiveStartedTick = tick;
        }

        StageAuthoredCaptureActors(director);

        if (TryDriveRaiderCapture(tick, director, player))
        {
            return;
        }

        var wantsStyledPropCapture = director.Mission.WorldId == "archive_nexus"
            && !string.IsNullOrWhiteSpace(
                OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CACHE_CAPTURE"));
        var wantsStyledPickupCapture = director.Mission.WorldId == "archive_nexus"
            && !string.IsNullOrWhiteSpace(
                OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_PICKUP_CAPTURE"));
        var wantsExplosionCapture = director.Mission.WorldId == "archive_nexus"
            && !string.IsNullOrWhiteSpace(
                OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_EXPLOSION_CAPTURE"));
        var propCaptureSuffix = ResolvePropCaptureSuffix();
        var pickupCaptureSuffix = ResolvePickupCaptureSuffix();
        if ((wantsStyledPropCapture || wantsStyledPickupCapture) && !_cacheCaptureSaved)
        {
            var liveStyledCache = _simulation.Actors.FirstOrDefault(actor =>
                !actor.IsDead
                && actor.CurrentForm.RoleTags.Contains("breakable")
                && actor.CurrentForm.SpriteSheetPath.EndsWith(propCaptureSuffix));
            if (liveStyledCache is not null)
            {
                _cacheObservedTick = _cacheObservedTick < 0 ? tick : _cacheObservedTick;
                if (wantsExplosionCapture
                    && liveStyledCache.CurrentForm.RoleTags.Contains("explosive"))
                {
                    StageExplosionCaptureActors(director, liveStyledCache, player);
                }
                else
                {
                    player.SimPosition = new Vector3(
                        liveStyledCache.SimPosition.X,
                        player.SimPosition.Y,
                        liveStyledCache.SimPosition.Z);
                    player.Velocity = Vector3.Zero;
                }
                if (tick <= _cacheObservedTick + 24)
                {
                    return;
                }
            }
        }

        if (wantsStyledPickupCapture && !_pickupCaptureSaved)
        {
            var liveStyledPickup = _simulation.Actors.FirstOrDefault(actor =>
                !actor.IsDead
                && actor.CurrentForm.RoleTags.Contains("pickup")
                && actor.CurrentForm.SpriteSheetPath.EndsWith(pickupCaptureSuffix));
            if (liveStyledPickup is not null)
            {
                if (_pickupObservedTick < 0)
                {
                    _pickupObservedTick = tick;
                    _pickupCaptureActorId = liveStyledPickup.ActorId;
                    _pickupCapturePosition = liveStyledPickup.SimPosition
                        + new Vector3(-2.6f, 0.0f, 0.0f);
                }

                if (liveStyledPickup.ActorId == _pickupCaptureActorId)
                {
                    liveStyledPickup.SimPosition = _pickupCapturePosition;
                    liveStyledPickup.Velocity = Vector3.Zero;
                    player.SimPosition = _pickupCapturePosition
                        + new Vector3(2.8f, 0.0f, 0.0f);
                    player.Velocity = Vector3.Zero;
                    if (tick <= _pickupObservedTick + 12)
                    {
                        return;
                    }
                }
            }
        }

        if (AuthoredStageCapturePending(director))
        {
            return;
        }

        if (tick < _nextDefeatTick)
        {
            return;
        }

        var explosiveTarget = tick - _encounterActiveStartedTick >= 20
            ? _simulation.Actors.FirstOrDefault(actor =>
            !actor.IsPlayerControlled
            && !actor.IsDead
            && actor.CurrentForm.RoleTags.Contains("explosive"))
            : null;
        var styledPickupPropTarget = wantsStyledPickupCapture
            && _cacheCaptureSavedTick >= 0
            && tick >= _cacheCaptureSavedTick + 12
            && !_pickupCaptureSaved
                ? _simulation.Actors.FirstOrDefault(actor =>
                    !actor.IsPlayerControlled
                    && !actor.IsDead
                    && actor.CurrentForm.RoleTags.Contains("breakable")
                    && actor.CurrentForm.SpriteSheetPath.EndsWith(propCaptureSuffix))
                : null;
        if (explosiveTarget is not null)
        {
            var explosionVictim = _simulation.Actors.FirstOrDefault(actor =>
                !actor.IsPlayerControlled
                && actor != explosiveTarget
                && !actor.IsDead
                && !actor.CurrentForm.RoleTags.Contains("hazard")
                && !actor.CurrentForm.RoleTags.Contains("pickup"));
            if (explosionVictim is not null)
            {
                explosionVictim.SimPosition = explosiveTarget.SimPosition
                    + new Vector3(1.0f, 0.0f, 0.0f);
            }
        }
        var target = explosiveTarget
            ?? styledPickupPropTarget
            ?? _simulation.Actors.FirstOrDefault(actor =>
            !actor.IsPlayerControlled
            && (!actor.IsBoss || director.Mission.IsFinalStage)
            && !actor.IsDead
            && (actor.ArcadeBrain?.IsEnteringStage == false
                || tick - _encounterActiveStartedTick > 150)
            && (!wantsStyledPickupCapture
                || _pickupCaptureSaved
                || !actor.CurrentForm.RoleTags.Contains("breakable")));
        var move = player.CurrentForm.FindMove("mannequin_heavy");
        if (target is null || move is null)
        {
            return;
        }

        target.ApplyHit(new HitApplication(
            player,
            target,
            move,
            target.Health,
            move.HitstunFrames,
            move.BlockstunFrames,
            Vector3.Zero,
            false), tick);
        _nextDefeatTick = tick + 32;
    }

    private bool TryDriveRaiderCapture(
        int tick,
        ArcadeEncounterDirector director,
        CombatActor player)
    {
        var idleCapturePath = OS.GetEnvironment(EnemyIdleCaptureVariable());
        var attackCapturePath = OS.GetEnvironment(EnemyAttackCaptureVariable());
        var targetStage = CapturedEnemyStageNumber();
        var captureRequested = director.Mission.StageNumber == targetStage
            && (!string.IsNullOrWhiteSpace(idleCapturePath)
                || !string.IsNullOrWhiteSpace(attackCapturePath));
        if (!captureRequested || _raiderCaptureFinished)
        {
            return false;
        }

        var raider = string.IsNullOrWhiteSpace(_raiderCaptureActorId)
            ? _simulation.Actors.FirstOrDefault(actor =>
                !actor.IsDead
                && actor.CurrentForm.Id == CapturedEnemyFormId()
                && (actor.ArcadeBrain is null
                    || !actor.ArcadeBrain.IsEnteringStage))
            : _simulation.Actors.FirstOrDefault(actor =>
                actor.ActorId == _raiderCaptureActorId
                && !actor.IsDead);
        if (raider is null)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_raiderCaptureActorId))
        {
            _raiderCaptureActorId = raider.ActorId;
            _raiderCaptureStartedTick = tick;
            _raiderOriginalAiEnabled = raider.IsAiEnabled;
            _raiderOriginalVulnerable = raider.IsVulnerable;
            GD.Print(
                $"[LadderSmoke] Staging live {raider.CurrentForm.DisplayName} "
                + $"{_raiderCaptureActorId} for runtime captures.");
        }

        StageRaiderCaptureActors(director, raider, player);
        raider.IsAiEnabled = false;
        raider.IsVulnerable = false;
        raider.FacingRight = true;

        if (_raiderCaptureArmedTick < 0)
        {
            raider.ClearCurrentMove();
            raider.State = CombatActorState.Idle;
            var captureWarmup = tick - _raiderCaptureStartedTick;
            if (captureWarmup < EnemyCaptureMinimumWarmupFrames
                || (_hud?.CenterNotificationVisible == true
                    && captureWarmup < EnemyCaptureOverlayTimeoutFrames))
            {
                return true;
            }

            _raiderCaptureArmedTick = tick;
            OS.SetEnvironment(EnemyActorIdVariable(), _raiderCaptureActorId);
            GD.Print(
                $"[LadderSmoke] {raider.CurrentForm.DisplayName} capture armed "
                + "after encounter overlays cleared.");
        }

        var idleCaptureReady = string.IsNullOrWhiteSpace(idleCapturePath)
            || CaptureFileExists(idleCapturePath);
        if (_raiderAttackHoldStartedTick < 0 && !idleCaptureReady)
        {
            raider.ClearCurrentMove();
            raider.State = CombatActorState.Idle;
            if (tick - _raiderCaptureArmedTick <= 120)
            {
                return true;
            }

            GD.PushError(
                $"[LadderSmoke] Timed out waiting for "
                + $"{raider.CurrentForm.DisplayName} idle capture '{idleCapturePath}'.");
        }

        var attack = raider.CurrentForm.FindMove(CapturedEnemyMoveId());
        if (attack is null)
        {
            GD.PushError(
                $"[LadderSmoke] {raider.CurrentForm.DisplayName} capture could not find "
                + $"move '{CapturedEnemyMoveId()}'.");
            FinishRaiderCapture(tick, raider);
            return false;
        }

        if (string.IsNullOrWhiteSpace(attackCapturePath))
        {
            FinishRaiderCapture(tick, raider);
            return false;
        }

        if (raider.CurrentMove?.Id != attack.Id)
        {
            raider.ClearCurrentMove();
            raider.Velocity = Vector3.Zero;
            if (!raider.TryStartMove(attack, tick))
            {
                GD.PushError(
                    $"[LadderSmoke] {raider.CurrentForm.DisplayName} capture could not "
                    + $"start '{attack.DisplayName}'.");
                FinishRaiderCapture(tick, raider);
                return false;
            }

            return true;
        }

        var captureFrame = CapturedEnemyMoveFrame();
        if (raider.CurrentMoveFrame < captureFrame)
        {
            return true;
        }

        _raiderAttackHoldStartedTick = _raiderAttackHoldStartedTick < 0
            ? tick
            : _raiderAttackHoldStartedTick;
        raider.CurrentMoveFrame = captureFrame;
        raider.Velocity = Vector3.Zero;

        if (CaptureFileExists(attackCapturePath))
        {
            FinishRaiderCapture(tick, raider);
            return false;
        }

        if (tick - _raiderAttackHoldStartedTick <= 120)
        {
            return true;
        }

        GD.PushError(
            $"[LadderSmoke] Timed out waiting for "
            + $"{raider.CurrentForm.DisplayName} attack capture '{attackCapturePath}'.");
        FinishRaiderCapture(tick, raider);
        return false;
    }

    private void StageRaiderCaptureActors(
        ArcadeEncounterDirector director,
        CombatActor raider,
        CombatActor player)
    {
        var encounter = director.CurrentEncounter;
        var center = new Vector3(
            (encounter.ArenaMinX + encounter.ArenaMaxX) * 0.5f,
            0.0f,
            Mathf.Clamp(0.0f, director.CurrentLaneMinZ, director.CurrentLaneMaxZ));
        raider.SimPosition = center + new Vector3(-1.45f, 0.0f, 0.0f);
        raider.Velocity = Vector3.Zero;
        player.SimPosition = center + new Vector3(1.55f, 0.0f, 0.0f);
        player.Velocity = Vector3.Zero;
        player.FacingRight = false;

        foreach (var actor in _simulation.Actors.Where(actor =>
            actor != raider
            && actor != player))
        {
            actor.Visible = false;
            _raiderHiddenActorIds.Add(actor.ActorId);
        }
    }

    private void FinishRaiderCapture(int tick, CombatActor raider)
    {
        raider.IsAiEnabled = _raiderOriginalAiEnabled;
        raider.IsVulnerable = _raiderOriginalVulnerable;
        foreach (var actor in _simulation.Actors.Where(actor =>
            _raiderHiddenActorIds.Contains(actor.ActorId)
            && !actor.IsDead))
        {
            actor.Visible = true;
        }

        _raiderCaptureFinished = true;
        _nextDefeatTick = Mathf.Max(_nextDefeatTick, tick + 12);
        GD.Print(
            $"[LadderSmoke] Live {raider.CurrentForm.DisplayName} runtime captures completed.");
    }

    private static bool CaptureFileExists(string path)
    {
        return !string.IsNullOrWhiteSpace(path)
            && File.Exists(path)
            && new FileInfo(path).Length > 0;
    }

    private static void PrepareEnemyCaptureOutput(string environmentVariable)
    {
        var path = OS.GetEnvironment(environmentVariable);
        if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private void StageExplosionCaptureActors(
        ArcadeEncounterDirector director,
        CombatActor explosive,
        CombatActor player)
    {
        var encounter = director.CurrentEncounter;
        var center = new Vector3(
            (encounter.ArenaMinX + encounter.ArenaMaxX) * 0.5f,
            0.0f,
            Mathf.Clamp(0.0f, director.CurrentLaneMinZ, director.CurrentLaneMaxZ));
        explosive.SimPosition = center;
        explosive.Velocity = Vector3.Zero;
        player.SimPosition = center + new Vector3(2.6f, 0.0f, 0.7f);
        player.Velocity = Vector3.Zero;

        var supportingActors = _simulation.Actors
            .Where(actor => actor != explosive && actor != player && !actor.IsDead)
            .OrderBy(actor => actor.ActorId)
            .ToArray();
        for (var index = 0; index < supportingActors.Length; index++)
        {
            var column = index % 3;
            var row = index / 3;
            supportingActors[index].SimPosition = center + new Vector3(
                -2.8f + column * 1.25f,
                0.0f,
                -1.0f + row * 1.1f);
            supportingActors[index].Velocity = Vector3.Zero;
        }
    }

    public void CaptureAfterSimulation(
        int tick,
        ArcadeEncounterDirector director,
        ICollection<CombatPresentationEvent> events)
    {
        _sawResults |= ProjectMannequin.Progression.RunSessionManager.Instance
            .ScoreManager.LastStageResults is not null;
        _sawEliteLifeBar |= director.State == ArcadeStageState.EliteIntro
            && _hud is { BossLifeBarVisible: true, BossHealthBarValue: > 0.0 };
        if (_requestedNarrowLaneClamp && director.Mission.StageNumber == 2)
        {
            var clampedPlayer = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
            _sawNarrowLaneClamp |= clampedPlayer is not null
                && clampedPlayer.SimPosition.Z <= director.CurrentLaneMaxZ + 0.001f
                && clampedPlayer.SimPosition.Z >= director.CurrentLaneMinZ - 0.001f;
        }
        CaptureAuthoredStageFrameIfRequested(director);
        CaptureStyledCacheFrameIfRequested(director);
        CapturePickupFrameIfRequested(director);

        if (!_summaryPrinted && tick % 300 == 0)
        {
            GD.Print(
                $"[LadderSmoke] tick={tick} state={director.State} "
                + $"encounter={director.CurrentEncounter.Id} remaining={director.EnemiesRemaining}");
        }

        if (!_summaryPrinted && tick >= 1800)
        {
            _summaryPrinted = true;
            GD.PushError(
                $"[LadderSmoke] Timed out state={director.State} "
                + $"encounter={director.CurrentEncounter.Id} remaining={director.EnemiesRemaining}.");
            _simulation.GetTree().Quit();
            return;
        }

        if (_summaryPrinted
            || director.State != ArcadeStageState.Complete
            || ProjectMannequin.Progression.RunSessionManager.Instance.ScoreManager.LastStageResults is null)
        {
            return;
        }

        _summaryPrinted = true;
        var expectedHazards = director.Mission.Encounters
            .SelectMany(encounter => encounter.HazardZones)
            .Select(hazard => hazard.WarningText)
            .Distinct()
            .Count();
        var expectsPropExplosion = director.Mission.Encounters
            .SelectMany(encounter => encounter.Props)
            .Any(prop => prop.ExplodesOnBreak);
        var expectsVaultLaneRhythm = director.Mission.WorldId == "archive_nexus"
            && director.Mission.StageNumber == 2;
        var laneRhythmPassed = !expectsVaultLaneRhythm
            || (_sawNarrowVaultLane && _sawWideVaultLane && _sawNarrowLaneClamp);
        var enemyCapturePassed = EnemyCapturePassed(director);
        var aftermathCapturePath = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_LADDER_AFTERMATH_CAPTURE");
        var aftermathCapturePassed = string.IsNullOrWhiteSpace(aftermathCapturePath)
            || CaptureFileExists(aftermathCapturePath);
        var nonFinalPassed = director.Mission.StageNumber is >= 1 and <= 3
            && !director.Mission.IsFinalStage
            && _hordesCleared == 2
            && _sawEliteSpawn
            && _eliteHadArcadeBrain
            && _sawStageCompleted
            && _sawResults
            && _sawHazardWarning == (expectedHazards > 0)
            && _hazardWarnings.Count >= expectedHazards
            && _sawPropExplosion == expectsPropExplosion
            && _sawPropExplosionDamage == expectsPropExplosion
            && laneRhythmPassed
            && _sawEliteIntroStarted
            && _sawEliteIntroReady
            && _sawEliteIntroFight
            && _sawEliteLifeBar
            && enemyCapturePassed
            && aftermathCapturePassed
            && !_sawFormUnlock
            && !_sawAwaitingFormSwap;
        var player = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        var finalPassed = director.Mission.StageNumber == 4
            && director.Mission.IsFinalStage
            && _hordesCleared == 0
            && !_sawEliteSpawn
            && _sawStageCompleted
            && _sawResults
            && _sawFormUnlock
            && _sawAwaitingFormSwap
            && player?.CurrentForm.Id == director.Mission.BossFormId
            && _sawHazardWarning == (expectedHazards > 0)
            && _hazardWarnings.Count >= expectedHazards
            && _sawPropExplosion == expectsPropExplosion
            && _sawPropExplosionDamage == expectsPropExplosion
            && enemyCapturePassed
            && aftermathCapturePassed;
        var passed = nonFinalPassed || finalPassed;
        GD.Print(
            $"[LadderSmoke] SUMMARY passed={passed} "
            + $"stage={director.Mission.StageNumber} hordes={_hordesCleared} "
            + $"elite={_sawEliteSpawn} brain={_eliteHadArcadeBrain} "
            + $"completed={_sawStageCompleted} results={_sawResults} hazard={_sawHazardWarning} unlock={_sawFormUnlock} "
            + $"eliteIntro={_sawEliteIntroStarted}/{_sawEliteIntroReady}/{_sawEliteIntroFight} "
            + $"eliteBar={_sawEliteLifeBar} hazards={_hazardWarnings.Count}/{expectedHazards} "
            + $"explosion={_sawPropExplosion}/{_sawPropExplosionDamage}/{expectsPropExplosion} "
            + $"lanes={_sawNarrowVaultLane}/{_sawWideVaultLane}/{_sawNarrowLaneClamp} "
            + $"enemyCapture={enemyCapturePassed} "
            + $"aftermathCapture={aftermathCapturePassed} "
            + $"awaitSwap={_sawAwaitingFormSwap} tick={tick}");
        if (!passed)
        {
            GD.PushError($"[LadderSmoke] Stage {director.Mission.StageNumber} ladder behavior failed assertions.");
        }


        _simulation.GetTree().Quit();
    }

    private static bool EnemyCapturePassed(ArcadeEncounterDirector director)
    {
        var targetStage = CapturedEnemyStageNumber();
        if (director.Mission.StageNumber != targetStage)
        {
            return true;
        }

        var idleCapturePath = OS.GetEnvironment(EnemyIdleCaptureVariable());
        var attackCapturePath = OS.GetEnvironment(EnemyAttackCaptureVariable());
        return (string.IsNullOrWhiteSpace(idleCapturePath)
                || CaptureFileExists(idleCapturePath))
            && (string.IsNullOrWhiteSpace(attackCapturePath)
                || CaptureFileExists(attackCapturePath));
    }

    private static string CapturedEnemyFormId()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_FORM_ID");
        return string.IsNullOrWhiteSpace(value) ? "archive_raider" : value;
    }

    private static string CapturedEnemyMoveId()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_ID");
        return string.IsNullOrWhiteSpace(value) ? "archive_raider_attack" : value;
    }

    private static int CapturedEnemyStageNumber()
    {
        return CapturedEnemyFormId() switch
        {
            "index_warden_veyra" => 1,
            "archive_scout" => 1,
            "world_warrior_rookie" => 1,
            "cipher_captain_rhune" => 2,
            "overseer_basalt" => 3,
            "archive_bruiser" => 3,
            "world_warrior_grappler" => 3,
            "archive_knight_boss" => 4,
            _ => 2,
        };
    }

    private static int CapturedEnemyMoveFrame()
    {
        var value = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_ENEMY_MOVE_FRAME");
        return int.TryParse(value, out var frame) ? Mathf.Max(0, frame) : 17;
    }

    private static string EnemyIdleCaptureVariable()
    {
        return CapturedEnemyFormId() switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_IDLE_CAPTURE",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_IDLE_CAPTURE",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_IDLE_CAPTURE",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_IDLE_CAPTURE",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_IDLE_CAPTURE",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_IDLE_CAPTURE",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_IDLE_CAPTURE",
        };
    }

    private static string EnemyAttackCaptureVariable()
    {
        return CapturedEnemyFormId() switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_ATTACK_CAPTURE",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_ATTACK_CAPTURE",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_ATTACK_CAPTURE",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_ATTACK_CAPTURE",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_ATTACK_CAPTURE",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_ATTACK_CAPTURE",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_ATTACK_CAPTURE",
        };
    }

    private static string EnemyActorIdVariable()
    {
        return CapturedEnemyFormId() switch
        {
            "index_warden_veyra" => "PROJECT_MANNEQUIN_LADDER_VEYRA_ACTOR_ID",
            "archive_scout" => "PROJECT_MANNEQUIN_LADDER_SCOUT_ACTOR_ID",
            "cipher_captain_rhune" => "PROJECT_MANNEQUIN_LADDER_RHUNE_ACTOR_ID",
            "overseer_basalt" => "PROJECT_MANNEQUIN_LADDER_BASALT_ACTOR_ID",
            "archive_bruiser" => "PROJECT_MANNEQUIN_LADDER_BRUISER_ACTOR_ID",
            "archive_knight_boss" => "PROJECT_MANNEQUIN_LADDER_KNIGHT_ACTOR_ID",
            _ => "PROJECT_MANNEQUIN_LADDER_RAIDER_ACTOR_ID",
        };
    }

    private void CapturePublishedEvents()
    {
        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var presentationEvent in _presentationEventBuffer)
        {
            switch (presentationEvent.Type)
            {
                case CombatPresentationEventType.EncounterCleared:
                    var clearedEncounter = _simulation.EncounterDirector?.Mission.Encounters
                        .FirstOrDefault(encounter => encounter.Id == presentationEvent.SourceActorId);
                    if (_clearedEncounterIds.Add(presentationEvent.SourceActorId)
                        && clearedEncounter?.Kind == StageEncounterKind.Horde)
                    {
                        _hordesCleared++;
                    }
                    break;
                case CombatPresentationEventType.EliteEnemySpawned:
                    _sawEliteSpawn = true;
                    var elite = _simulation.Actors.FirstOrDefault(actor =>
                        actor.ActorId == presentationEvent.SourceActorId);
                    _eliteHadArcadeBrain |= elite is { IsElite: true, IsBoss: false, ArcadeBrain: not null };
                    break;
                case CombatPresentationEventType.StageCompleted:
                    _sawStageCompleted = true;
                    break;
                case CombatPresentationEventType.FormUnlocked:
                    _sawFormUnlock = true;
                    break;
                case CombatPresentationEventType.StageResultsReady:
                    _sawResults = true;
                    break;
                case CombatPresentationEventType.HazardZoneWarning:
                    _sawHazardWarning = true;
                    _hazardWarnings.Add(presentationEvent.Payload);
                    break;
                case CombatPresentationEventType.HazardZoneDamage
                    when presentationEvent.Payload.StartsWith("repository_volatile_canister|"):
                    _sawPropExplosionDamage = true;
                    break;
                case CombatPresentationEventType.EliteIntroStarted:
                    _sawEliteIntroStarted = true;
                    break;
                case CombatPresentationEventType.EliteIntroReady:
                    _sawEliteIntroReady = true;
                    break;
                case CombatPresentationEventType.EliteIntroFight:
                    _sawEliteIntroFight = true;
                    break;
                case CombatPresentationEventType.PropExploded:
                    _sawPropExplosion = true;
                    break;
            }
        }
    }

    private void CaptureAuthoredStageFrameIfRequested(ArcadeEncounterDirector director)
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CAPTURE");
        var targetEncounterIndex = AuthoredStageCaptureEncounterIndex(director);
        var minimumElapsed = AuthoredStageCaptureMinimumElapsed(director);
        if (_stageCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || targetEncounterIndex < 0
            || director.State != ArcadeStageState.EncounterActive
            || director.Mission.Encounters.FindIndex(encounter =>
                encounter.Id == director.CurrentEncounter.Id) != targetEncounterIndex
            || director.StateElapsedFrames < minimumElapsed)
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = _simulation.GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError($"[LadderSmoke] Could not save authored stage capture '{path}' ({error}).");
            return;
        }

        _stageCaptureSaved = true;
        RestoreAuthoredStageCaptureActors();
        GD.Print($"[LadderSmoke] Authored stage capture saved: {path}");
    }

    private void StageAuthoredCaptureActors(ArcadeEncounterDirector director)
    {
        if (!AuthoredStageCapturePending(director)
            || OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CLEAR_STAGE_CAPTURE") != "1")
        {
            return;
        }

        foreach (var actor in _simulation.Actors.Where(actor => actor.Visible))
        {
            _stageCaptureHiddenActorIds.Add(actor.ActorId);
            actor.Visible = false;
        }
    }

    private void RestoreAuthoredStageCaptureActors()
    {
        foreach (var actor in _simulation.Actors.Where(actor =>
                     _stageCaptureHiddenActorIds.Contains(actor.ActorId)))
        {
            actor.Visible = true;
        }
        _stageCaptureHiddenActorIds.Clear();
    }

    private bool AuthoredStageCapturePending(ArcadeEncounterDirector director)
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CAPTURE");
        var targetEncounterIndex = AuthoredStageCaptureEncounterIndex(director);
        return !_stageCaptureSaved
            && !string.IsNullOrWhiteSpace(path)
            && targetEncounterIndex >= 0
            && director.Mission.Encounters.FindIndex(encounter =>
                encounter.Id == director.CurrentEncounter.Id) == targetEncounterIndex;
    }

    private static int AuthoredStageCaptureEncounterIndex(
        ArcadeEncounterDirector director)
    {
        var encounterNumberText = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_LADDER_CAPTURE_ENCOUNTER_NUMBER");
        if (int.TryParse(encounterNumberText, out var encounterNumber)
            && encounterNumber > 0)
        {
            return Mathf.Clamp(
                encounterNumber - 1,
                0,
                director.Mission.Encounters.Count - 1);
        }

        return director.Mission.StageNumber switch
        {
            1 => 1,
            2 => 1,
            3 => 0,
            _ => -1,
        };
    }

    private static int AuthoredStageCaptureMinimumElapsed(
        ArcadeEncounterDirector director)
    {
        var minimumElapsedText = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_LADDER_CAPTURE_MINIMUM_ELAPSED");
        return int.TryParse(minimumElapsedText, out var authoredMinimum)
            ? Mathf.Max(0, authoredMinimum)
            : director.Mission.StageNumber == 3 ? 70 : 88;
    }

    private void CapturePickupFrameIfRequested(ArcadeEncounterDirector director)
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_PICKUP_CAPTURE");
        if (_pickupCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || director.Mission.WorldId != "archive_nexus"
            || _pickupObservedTick < 0
            || _simulation.CurrentTick < _pickupObservedTick + 12
            || !_simulation.Actors.Any(actor =>
                !actor.IsDead
                && actor.ActorId == _pickupCaptureActorId
                && actor.CurrentForm.RoleTags.Contains("pickup")
                && actor.CurrentForm.SpriteSheetPath.EndsWith(
                    ResolvePickupCaptureSuffix())))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = _simulation.GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError($"[LadderSmoke] Could not save pickup capture '{path}' ({error}).");
            return;
        }

        _pickupCaptureSaved = true;
        GD.Print($"[LadderSmoke] Styled pickup capture saved: {path}");
    }

    private void CaptureStyledCacheFrameIfRequested(ArcadeEncounterDirector director)
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CACHE_CAPTURE");
        if (_cacheCaptureSaved
            || string.IsNullOrWhiteSpace(path)
            || director.Mission.WorldId != "archive_nexus"
            || _cacheObservedTick < 0
            || _simulation.CurrentTick < _cacheObservedTick + 24
            || !_simulation.Actors.Any(actor =>
                !actor.IsDead
                && actor.CurrentForm.RoleTags.Contains("breakable")
                && actor.CurrentForm.SpriteSheetPath.EndsWith(
                    ResolvePropCaptureSuffix())))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = _simulation.GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError($"[LadderSmoke] Could not save styled cache capture '{path}' ({error}).");
            return;
        }

        _cacheCaptureSaved = true;
        _cacheCaptureSavedTick = _simulation.CurrentTick;
        GD.Print($"[LadderSmoke] Styled cache capture saved: {path}");
    }

    private static string ResolvePropCaptureSuffix()
    {
        var suffix = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_PROP_SPRITE_SUFFIX");
        return string.IsNullOrWhiteSpace(suffix)
            ? "archive_health_cache_style_v2.png"
            : suffix;
    }

    private static string ResolvePickupCaptureSuffix()
    {
        var suffix = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_PICKUP_SPRITE_SUFFIX");
        return string.IsNullOrWhiteSpace(suffix)
            ? "archive_health_pickup_style_v2.png"
            : suffix;
    }
}
