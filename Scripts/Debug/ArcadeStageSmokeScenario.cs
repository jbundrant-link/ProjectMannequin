using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public sealed class ArcadeStageSmokeScenario
{
    private readonly GameSimulation _simulation;
    private readonly HashSet<EnemyEntryEdge> _observedEntryEdges = new();
    private readonly HashSet<EnemyEntryProfile> _observedEntryProfiles = new();
    private readonly HashSet<string> _enteredActorIds = new();
    private readonly HashSet<string> _completedEntryActorIds = new();
    private string _observedEncounterId = "";
    private int _encounterStartedTick;
    private int _nextDefeatTick;
    private int _encountersCleared;
    private int _enemiesEntered;
    private int _fightRoomsLocked;
    private int _fightRoomsOpened;
    private int _wavesStarted;
    private int _wavesCleared;
    private int _waveRewardsGranted;
    private int _entryWarnings;
    private int _entriesCompleted;
    private int _maximumCrowdAttackers;
    private bool _sawApproach;
    private bool _sawLaneAlignment;
    private bool _sawRetreat;
    private bool _fightRoomBoundsHeld = true;
    private bool _crowdSlotsUnique = true;
    private bool _summaryPrinted;

    public ArcadeStageSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public void UpdateBeforeSimulation(int tick, ArcadeEncounterDirector director)
    {
        var player = _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
        if (player is null)
        {
            return;
        }

        player.IsVulnerable = false;
        var encounter = director.CurrentEncounter;
        if (director.State == ArcadeStageState.Traveling)
        {
            if (encounter.Kind == StageEncounterKind.Boss)
            {
                player.SimPosition = new Vector3(
                    Mathf.Min(player.SimPosition.X + 0.22f, encounter.TriggerX - 0.25f),
                    player.SimPosition.Y,
                    player.SimPosition.Z);
                return;
            }

            player.SimPosition += new Vector3(0.22f, 0.0f, 0.0f);
            return;
        }

        if (director.State != ArcadeStageState.EncounterActive
            || encounter.Kind != StageEncounterKind.Horde)
        {
            return;
        }

        if (_observedEncounterId != encounter.Id)
        {
            _observedEncounterId = encounter.Id;
            _encounterStartedTick = tick;
            _nextDefeatTick = tick + 120;
        }

        if (tick < _nextDefeatTick)
        {
            return;
        }

        var target = _simulation.Actors.FirstOrDefault(actor =>
            !actor.IsPlayerControlled
            && !actor.IsBoss
            && !actor.IsDead
            && actor.ArcadeBrain?.IsEnteringStage == false);
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
        _nextDefeatTick = tick + 45;
    }

    public void CaptureAfterSimulation(
        int tick,
        ArcadeEncounterDirector director,
        ICollection<CombatPresentationEvent> events)
    {
        foreach (var presentationEvent in events)
        {
            if (presentationEvent.Type == CombatPresentationEventType.EnemyEntered)
            {
                _enemiesEntered++;
                _enteredActorIds.Add(presentationEvent.SourceActorId);
                var parts = presentationEvent.Payload.Split('|');
                var edgeName = parts[0];
                if (System.Enum.TryParse<EnemyEntryEdge>(edgeName, out var edge))
                {
                    _observedEntryEdges.Add(edge);
                }

                if (parts.Length > 1
                    && System.Enum.TryParse<EnemyEntryProfile>(
                        parts[1],
                        out var entryProfile))
                {
                    _observedEntryProfiles.Add(entryProfile);
                }
            }
            else if (presentationEvent.Type == CombatPresentationEventType.EncounterCleared
                     && director.CurrentEncounter.Kind == StageEncounterKind.Horde)
            {
                _encountersCleared++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.FightRoomLocked
                     && director.CurrentEncounter.Kind == StageEncounterKind.Horde)
            {
                _fightRoomsLocked++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.FightRoomOpened)
            {
                _fightRoomsOpened++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.WaveStarted
                     && director.CurrentEncounter.Kind == StageEncounterKind.Horde)
            {
                _wavesStarted++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.WaveCleared
                     && director.CurrentEncounter.Kind == StageEncounterKind.Horde)
            {
                _wavesCleared++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.WaveRewardGranted)
            {
                _waveRewardsGranted++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.EnemyEntryWarning)
            {
                _entryWarnings++;
            }
            else if (presentationEvent.Type == CombatPresentationEventType.EnemyEntryCompleted)
            {
                _entriesCompleted++;
                _completedEntryActorIds.Add(presentationEvent.SourceActorId);
            }
        }

        if (director.IsArenaLocked)
        {
            var encounter = director.CurrentEncounter;
            foreach (var player in _simulation.Actors.Where(actor =>
                         actor.IsPlayerControlled && !actor.IsDead))
            {
                _fightRoomBoundsHeld &=
                    player.SimPosition.X >= encounter.ArenaMinX + encounter.PlayerBoundaryInset - 0.01f
                    && player.SimPosition.X <= encounter.ArenaMaxX - encounter.PlayerBoundaryInset + 0.01f;
            }
        }

        var activeBrains = _simulation.Actors
                     .Where(actor => !actor.IsDead)
                     .Select(actor => actor.ArcadeBrain)
                     .Where(brain => brain is not null)
                     .Cast<ArcadeEnemyBrain>()
                     .ToArray();
        foreach (var brain in activeBrains)
        {
            _sawApproach |= brain.CurrentIntent == CpuFighterIntent.Approach;
            _sawLaneAlignment |= brain.CurrentIntent == CpuFighterIntent.AlignLane;
            _sawRetreat |= brain.CurrentIntent == CpuFighterIntent.Retreat;
        }

        foreach (var targetGroup in activeBrains
                     .Where(brain =>
                         !brain.IsEnteringStage
                         && !string.IsNullOrWhiteSpace(brain.TargetActorId))
                     .GroupBy(brain => brain.TargetActorId))
        {
            _crowdSlotsUnique &=
                targetGroup.Select(brain => brain.FormationSlotIndex).Distinct().Count()
                == targetGroup.Count();
        }

        _maximumCrowdAttackers = Mathf.Max(
            _maximumCrowdAttackers,
            director.ActiveCrowdAttackerCount);

        if (tick % 180 == 0)
        {
            var activeEnemies = _simulation.Actors.Count(actor =>
                !actor.IsPlayerControlled && !actor.IsDead);
            GD.Print(
                $"[ScrollSmoke] tick={tick} state={director.State} encounter={director.CurrentEncounter.Id} camera={director.CameraCenterX:0.0} active={activeEnemies} queued={director.PendingEnemyCount}");
        }

        if (_summaryPrinted
            || director.CurrentEncounter.Kind != StageEncounterKind.Boss
            || director.State != ArcadeStageState.Traveling
            || director.CameraCenterX < director.CurrentEncounter.TriggerX - 1.0f)
        {
            return;
        }

        _summaryPrinted = true;
        var expectedHordeRooms = director.Mission.Encounters.Count(encounter =>
            encounter.Kind == StageEncounterKind.Horde && encounter.LocksFightRoom);
        var expectedHordeWaves = director.Mission.Encounters
            .Where(encounter => encounter.Kind == StageEncounterKind.Horde)
            .Sum(encounter => Mathf.Max(1, encounter.Waves.Count));
        var passed = _encountersCleared == expectedHordeRooms
            && _enemiesEntered == director.TotalMinions
            && _observedEntryEdges.Count == 4
            && _observedEntryProfiles.Count == 5
            && _entryWarnings > 0
            && _entriesCompleted == director.TotalMinions
            && _maximumCrowdAttackers > 0
            && _maximumCrowdAttackers <= 2
            && _crowdSlotsUnique
            && _sawApproach
            && _sawLaneAlignment
            && _sawRetreat
            && _fightRoomsLocked == expectedHordeRooms
            && _fightRoomsOpened == expectedHordeRooms
            && _wavesStarted == expectedHordeWaves
            && _wavesCleared == expectedHordeWaves
            && expectedHordeWaves > expectedHordeRooms
            && _waveRewardsGranted > 0
            && _fightRoomBoundsHeld;
        GD.Print(
            $"[ScrollSmoke] SUMMARY passed={passed} cleared={_encountersCleared}/{expectedHordeRooms} waves={_wavesStarted}/{_wavesCleared}/{expectedHordeWaves} rewards={_waveRewardsGranted} entered={_enemiesEntered}/{director.TotalMinions} entryDone={_entriesCompleted} missingEntry={string.Join(",", _enteredActorIds.Except(_completedEntryActorIds))} warnings={_entryWarnings} profiles={_observedEntryProfiles.Count} rooms={_fightRoomsLocked}/{_fightRoomsOpened} bounds={_fightRoomBoundsHeld} slots={_crowdSlotsUnique} attackers={_maximumCrowdAttackers} edges={_observedEntryEdges.Count} approach={_sawApproach} align={_sawLaneAlignment} retreat={_sawRetreat} progress={director.StageProgress:0.00}");
        if (!passed)
        {
            GD.PushError("[ScrollSmoke] Pre-boss scrolling-stage behavior did not satisfy all assertions.");
        }
    }
}
