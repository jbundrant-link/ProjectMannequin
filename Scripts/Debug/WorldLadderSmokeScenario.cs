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
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private readonly HashSet<string> _clearedEncounterIds = new();
    private readonly HashSet<string> _hazardWarnings = new();

    public WorldLadderSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
        _hud = simulation.GetNodeOrNull<MvpHud>("../MvpHud");
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
        if (director.Mission.StageNumber == 2)
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
        if (director.State != ArcadeStageState.EncounterActive)
        {
            _encounterActiveStartedTick = -1;
            return;
        }

        if (_encounterActiveStartedTick < 0)
        {
            _encounterActiveStartedTick = tick;
        }

        var wantsStyledPropCapture = director.Mission.WorldId == "archive_nexus"
            && !string.IsNullOrWhiteSpace(
                OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CACHE_CAPTURE"));
        var wantsStyledPickupCapture = director.Mission.WorldId == "archive_nexus"
            && !string.IsNullOrWhiteSpace(
                OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_PICKUP_CAPTURE"));
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
                player.SimPosition = new Vector3(
                    liveStyledCache.SimPosition.X,
                    player.SimPosition.Y,
                    liveStyledCache.SimPosition.Z);
                player.Velocity = Vector3.Zero;
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
            && !actor.IsBoss
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
        var laneRhythmPassed = director.Mission.StageNumber != 2
            || (_sawNarrowVaultLane && _sawWideVaultLane && _sawNarrowLaneClamp);
        var passed = director.Mission.StageNumber is >= 1 and <= 3
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
            && !_sawFormUnlock
            && !_sawAwaitingFormSwap;
        GD.Print(
            $"[LadderSmoke] SUMMARY passed={passed} "
            + $"stage={director.Mission.StageNumber} hordes={_hordesCleared} "
            + $"elite={_sawEliteSpawn} brain={_eliteHadArcadeBrain} "
            + $"completed={_sawStageCompleted} results={_sawResults} hazard={_sawHazardWarning} unlock={_sawFormUnlock} "
            + $"eliteIntro={_sawEliteIntroStarted}/{_sawEliteIntroReady}/{_sawEliteIntroFight} "
            + $"eliteBar={_sawEliteLifeBar} hazards={_hazardWarnings.Count}/{expectedHazards} "
            + $"explosion={_sawPropExplosion}/{_sawPropExplosionDamage}/{expectsPropExplosion} "
            + $"lanes={_sawNarrowVaultLane}/{_sawWideVaultLane}/{_sawNarrowLaneClamp} "
            + $"awaitSwap={_sawAwaitingFormSwap} tick={tick}");
        if (!passed)
        {
            GD.PushError($"[LadderSmoke] Stage {director.Mission.StageNumber} ladder behavior failed assertions.");
        }


        _simulation.GetTree().Quit();
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
        var targetEncounterIndex = director.Mission.StageNumber switch
        {
            2 => 1,
            3 => 0,
            _ => -1,
        };
        var minimumElapsed = director.Mission.StageNumber == 3 ? 70 : 88;
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
            GD.PushError($"[LadderSmoke] Could not save Stage 2 capture '{path}' ({error}).");
            return;
        }

        _stageCaptureSaved = true;
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
