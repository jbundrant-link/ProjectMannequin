using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;
using ProjectMannequin.DebugTools;

using ProjectMannequin.LocalInput;

namespace ProjectMannequin.Stage;

public enum ArcadeStageState
{
    StageIntro,
    Traveling,
    EliteIntro,
    BossIntro,
    EncounterActive,
    Intermission,
    AwaitingFormSwap,
    AwaitingReward,
    AwaitingRouteChoice,
    Complete,
}

public sealed class ArcadeEncounterDirector
{
    private const int DefeatedActorLifetimeFrames = 90;

    private readonly GameSimulation _simulation;
    private readonly Node3D _actorRoot;
    private readonly HashSet<string> _activeEncounterActorIds = new();
    private readonly HashSet<string> _activeWaveActorIds = new();
    private readonly HashSet<string> _isolatedDuelPlacedBossIds = new();
    private readonly HashSet<string> _catchUpActorIds = new();
    private readonly HashSet<string> _hardCatchUpActorIds = new();
    private readonly Dictionary<string, int> _defeatedActorTicks = new();
    private readonly Dictionary<string, StagePropData> _propDataByActorId = new();
    private readonly List<EnemySpawnData> _pendingSpawns = new();
    private readonly HashSet<EnemySpawnData> _warnedSpawns = new();
    private readonly HashSet<string> _warnedHazardZones = new();
    private readonly HashSet<string> _triggeredHazardCycles = new();
    private readonly List<StageWaveData> _encounterWaves = new();
    private readonly ArcadeCrowdCoordinator _crowdCoordinator = new();
    private readonly List<RewardOption> _pendingRewardOptions = new();
    private readonly List<RouteChoiceData> _pendingRouteChoices = new();

    private int _encounterIndex;
    private int _stateStartedTick;
    private int _waveSpawnStartedTick;
    private int _nextWaveStartTick;
    private int _currentWaveIndex = -1;
    private int _spawnSequence;
    private int _spawnedInCurrentEncounter;
    private int _resolvedEnemyCount;
    private int _defeatedMinions;
    private int _requestedRewardChoice = -1;
    private int _requestedRouteChoice = -1;
    private readonly bool _rewardsEnabled;
    private bool _waveStarted;
    private bool _bossIntroReadyEmitted;
    private bool _bossIntroStartedEmitted;
    private bool _eliteIntroReadyEmitted;
    private int _bossIntroCutsceneFrames;
    private bool _stageCompleteEventSent;
    private bool _stageIntroStartedEmitted;
    private bool _stageIntroReadyEmitted;
    private int _nextBossSmokeAttackTick = 30;
    private float _cameraCenterX;
    private float _cameraCenterY;
    private float _cameraTransitionStartX;
    private float _cameraTransitionTargetX;
    private int _cameraTransitionStartedTick;
    private int _cameraTransitionFrames;
    private bool _cameraTransitionActive;
    private string _laneBoundsEncounterId = "";
    private float _currentLaneMinZ;
    private float _currentLaneMaxZ;
    private float _laneTransitionStartMinZ;
    private float _laneTransitionStartMaxZ;
    private int _laneTransitionStartedTick;
    private bool _laneTransitionActive;
    private readonly CpuFighterSmokeScenario? _cpuSmokeScenario;
    private readonly ArcadeStageSmokeScenario? _stageSmokeScenario;
    private readonly MultiplayerCameraSmokeScenario? _cameraSmokeScenario;
    private readonly WorldLadderSmokeScenario? _ladderSmokeScenario;

    // Screenshot render tests capture at fixed wall-clock times, so the extra
    // READY-hold window would shift what is on screen. Collapse it there to keep
    // the fight-start tick identical to the pre-sequence timing.
    private readonly bool _suppressBossIntroReadyHold;

    public ArcadeEncounterDirector(
        GameSimulation simulation,
        Node3D actorRoot,
        StageMissionData mission)
    {
        _simulation = simulation;
        _actorRoot = actorRoot;
        Mission = mission;
        _rewardsEnabled = DisplayServer.GetName() != "headless"
            && string.IsNullOrEmpty(OS.GetEnvironment("PROJECT_MANNEQUIN_REPLAY_PLAY"))
            && OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_SMOKE_TEST") != "1";
        _suppressBossIntroReadyHold =
            OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_RENDER_TEST") == "1";
        _cameraCenterX = mission.StageMinX + mission.CameraViewportWidth * 0.5f;
        _currentLaneMinZ = mission.LaneMinZ;
        _currentLaneMaxZ = mission.LaneMaxZ;
        if (mission.StageIntroFrames > 0)
        {
            State = ArcadeStageState.StageIntro;
            _stateStartedTick = 0;
        }
        foreach (var validationError in StageMissionValidator.Validate(mission))
        {
            GD.PushError($"[StageMission] {validationError}");
        }

        _cpuSmokeScenario = OS.GetEnvironment("PROJECT_MANNEQUIN_CPU_SMOKE_TEST") == "1"
            ? new CpuFighterSmokeScenario(simulation)
            : null;
        _stageSmokeScenario = OS.GetEnvironment("PROJECT_MANNEQUIN_SCROLL_SMOKE_TEST") == "1"
            ? new ArcadeStageSmokeScenario(simulation)
            : null;
        _cameraSmokeScenario = OS.GetEnvironment("PROJECT_MANNEQUIN_CAMERA_SMOKE_TEST") == "1"
            ? new MultiplayerCameraSmokeScenario(simulation)
            : null;
        _ladderSmokeScenario = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_SMOKE_TEST") == "1"
            ? new WorldLadderSmokeScenario(simulation)
            : null;
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_CPU_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_SPLIT_SCREEN_TEST") == "1")
        {
            var bossIndex = mission.Encounters.FindLastIndex(
                encounter => encounter.Kind == StageEncounterKind.Boss);
            _encounterIndex = Mathf.Max(0, bossIndex);
            _cameraCenterX = mission.Encounters[_encounterIndex].CameraLockX;
        }

        BeginEncounterLaneTransition(0);
    }

    public StageMissionData Mission { get; }
    public ArcadeStageState State { get; private set; } = ArcadeStageState.Traveling;
    public int CurrentEncounterNumber => Mathf.Min(_encounterIndex + 1, Mission.Encounters.Count);
    public int TotalEncounterCount => Mission.Encounters.Count;
    public int DefeatedMinions => _defeatedMinions;
    public StageEncounterData CurrentEncounter =>
        Mission.Encounters[Mathf.Clamp(_encounterIndex, 0, Mission.Encounters.Count - 1)];
    public StageWaveData? CurrentWave =>
        _currentWaveIndex >= 0 && _currentWaveIndex < _encounterWaves.Count
            ? _encounterWaves[_currentWaveIndex]
            : null;
    public int CurrentWaveNumber =>
        _encounterWaves.Count == 0 ? 0 : Mathf.Clamp(_currentWaveIndex + 1, 1, _encounterWaves.Count);
    public int TotalWaveCount => _encounterWaves.Count;
    public int TotalMinions => Mission.Encounters
        .Where(encounter => encounter.Kind == StageEncounterKind.Horde)
        .Sum(CountConfiguredEnemies);
    public int PendingEnemyCount => _pendingSpawns.Count;
    public int ActiveCrowdAttackerCount =>
        _crowdCoordinator.ActiveAttackReservationCount;
    public float CameraCenterX => _cameraCenterX;
    public float CameraCenterY => _cameraCenterY;
    public int StateElapsedFrames => Mathf.Max(0, _simulation.CurrentTick - _stateStartedTick);
    public float CurrentLaneMinZ => _currentLaneMinZ;
    public float CurrentLaneMaxZ => _currentLaneMaxZ;
    public float CameraLeftX => _cameraCenterX - Mission.CameraViewportWidth * 0.5f;
    public float CameraRightX => _cameraCenterX + Mission.CameraViewportWidth * 0.5f;
    public float StageProgress =>
        Mathf.InverseLerp(
            Mission.StageMinX + Mission.CameraViewportWidth * 0.5f,
            Mission.StageMaxX - Mission.CameraViewportWidth * 0.5f,
            _cameraCenterX);
    public bool ShowAdvancePrompt => State == ArcadeStageState.Traveling;
    public bool IsArenaLocked => CurrentEncounter.LocksFightRoom
        && State is ArcadeStageState.BossIntro
            or ArcadeStageState.EliteIntro
            or ArcadeStageState.EncounterActive
            or ArcadeStageState.Intermission
            or ArcadeStageState.AwaitingFormSwap
            or ArcadeStageState.AwaitingReward;

    /// <summary>
    /// True while an authored boss encounter is being introduced or actively
    /// fought. Single source of truth for boss-duel rules; systems must query
    /// this instead of re-deriving boss state from stage-state checks.
    /// </summary>
    public bool IsBossDuelActive =>
        CurrentEncounter.Kind == StageEncounterKind.Boss
        && State is ArcadeStageState.BossIntro or ArcadeStageState.EncounterActive;

    /// <summary>
    /// The fighting-layer rule set that is authoritative this tick. Boss
    /// encounters use <see cref="CombatMode.BossDuel"/>; everything else is
    /// <see cref="CombatMode.Horde"/>.
    /// </summary>
    public CombatMode ActiveCombatMode =>
        IsBossDuelActive ? CombatMode.BossDuel : CombatMode.Horde;

    public int EnemiesRemaining
    {
        get
        {
            var liveEnemies = _simulation.Actors.Count(actor =>
                _activeEncounterActorIds.Contains(actor.ActorId) && !actor.IsDead);
            var firstFutureWaveIndex = _waveStarted
                ? _currentWaveIndex + 1
                : Mathf.Max(0, _currentWaveIndex);
            var futureEnemies = _encounterWaves
                .Skip(firstFutureWaveIndex)
                .Sum(wave => wave.Spawns.Count);
            return liveEnemies + _pendingSpawns.Count + futureEnemies;
        }
    }

    public string CurrentObjective
    {
        get
        {
            if (Mission.Encounters.Count == 0)
            {
                return "No encounters configured.";
            }

            var encounter = Mission.Encounters[Mathf.Clamp(_encounterIndex, 0, Mission.Encounters.Count - 1)];
            return State switch
            {
                ArcadeStageState.StageIntro => $"Stage {Mission.StageNumber}: {Mission.StageTitle}",
                ArcadeStageState.Traveling => $"Advance right to {encounter.DisplayName}.",
                ArcadeStageState.EncounterActive when encounter.Kind == StageEncounterKind.Boss
                    => $"Defeat {encounter.DisplayName} and archive its combat form.",
                ArcadeStageState.EncounterActive when encounter.Kind == StageEncounterKind.Elite
                    => $"Defeat {encounter.DisplayName} to clear Stage {Mission.StageNumber}.",
                ArcadeStageState.EncounterActive
                    => $"Clear {encounter.DisplayName}. Wave {CurrentWaveNumber}/{TotalWaveCount}. "
                        + $"{EnemiesRemaining} hostiles remain.",
                ArcadeStageState.Intermission => "Sector clear. Prepare to advance.",
                ArcadeStageState.AwaitingReward =>
                    "Sector clear. Choose a reward ("
                    + $"{InputGlyphs.UiActionLabel(LogicalUiAction.Reward1)}/"
                    + $"{InputGlyphs.UiActionLabel(LogicalUiAction.Reward2)}/"
                    + $"{InputGlyphs.UiActionLabel(LogicalUiAction.Reward3)}).",
                ArcadeStageState.AwaitingFormSwap
                    => "Combat form archived. Press "
                        + $"{InputGlyphs.UiActionLabel(LogicalUiAction.FormSwap)}"
                        + " to shapeshift and complete the mission.",
                ArcadeStageState.Complete
                    => Mission.IsFinalStage
                        ? $"{Mission.DisplayName} complete. Boss form inheritance confirmed."
                        : $"Stage {Mission.StageNumber} clear. Prepare for the next Archive sector.",
                _ => $"Advance through {Mission.DisplayName}.",
            };
        }
    }

    public IReadOnlyList<RewardOption> PendingRewardOptions => _pendingRewardOptions;

    /// <summary>
    /// Called by the HUD when the player selects a between-encounter reward. The
    /// choice is applied on the next simulation tick in
    /// <see cref="UpdateBeforeSimulation"/> so all mutation stays on the sim thread.
    /// </summary>
    public void RequestRewardChoice(int index)
    {
        if (State == ArcadeStageState.AwaitingReward
            && index >= 0
            && index < _pendingRewardOptions.Count)
        {
            _requestedRewardChoice = index;
        }
    }

    public IReadOnlyList<RouteChoiceData> PendingRouteChoices => _pendingRouteChoices;

    public void RequestRouteChoice(int index)
    {
        if (State == ArcadeStageState.AwaitingRouteChoice
            && index >= 0
            && index < _pendingRouteChoices.Count)
        {
            _requestedRouteChoice = index;
        }
    }

    private bool BeginRouteChoice(
        StageEncounterData encounter,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        _pendingRouteChoices.Clear();
        _pendingRouteChoices.AddRange(encounter.RouteChoices);
        _requestedRouteChoice = -1;
        State = ArcadeStageState.AwaitingRouteChoice;
        _stateStartedTick = tick;
        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.RouteChoiceReady,
            tick,
            encounter.Id,
            Payload: string.Join("|", encounter.RouteChoices.Select(choice => choice.Label))));
        return true;
    }

    private void ApplyRouteChoice(
        int index,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        if (index >= 0 && index < _pendingRouteChoices.Count)
        {
            var choice = _pendingRouteChoices[index];
            var targetIndex = Mission.Encounters.FindIndex(
                encounter => encounter.Id == choice.TargetEncounterId);
            _encounterIndex = targetIndex >= 0
                ? targetIndex
                : Mathf.Min(_encounterIndex + 1, Mission.Encounters.Count - 1);
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.RouteChosen,
                tick,
                Mission.Id,
                Payload: choice.Label));
        }

        _pendingRouteChoices.Clear();
        State = ArcadeStageState.Traveling;
        _stateStartedTick = tick;
        _cameraTransitionActive = false;
    }

    public void UpdateBeforeSimulation(int tick, ICollection<CombatPresentationEvent> events)
    {
        var player = GetPlayer();
        if (player is null || player.IsDead || Mission.Encounters.Count == 0)
        {
            return;
        }

        UpdateEncounterLaneBounds(tick);

        _ladderSmokeScenario?.UpdateBeforeSimulation(tick, this);

        if (State == ArcadeStageState.StageIntro)
        {
            var elapsed = tick - _stateStartedTick;
            if (!_stageIntroStartedEmitted)
            {
                _stageIntroStartedEmitted = true;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.StageIntroStarted,
                    tick,
                    Mission.Id,
                    Payload: $"{Mission.StageNumber}|{Mission.StageTitle}|{Mission.StageSubtitle}"));
            }

            if (!_stageIntroReadyEmitted && elapsed >= Mission.StageIntroReadyFrame)
            {
                _stageIntroReadyEmitted = true;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.StageIntroReady,
                    tick,
                    Mission.Id,
                    Payload: Mission.StageTitle));
            }

            if (elapsed >= Mission.StageIntroFrames)
            {
                State = ArcadeStageState.Traveling;
                _stateStartedTick = tick;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.StageIntroFinished,
                    tick,
                    Mission.Id,
                    Payload: Mission.StageTitle));
            }

            return;
        }

        if (State == ArcadeStageState.AwaitingReward)
        {
            if (_requestedRewardChoice >= 0)
            {
                ApplyRewardChoice(_requestedRewardChoice, tick, events);
                _requestedRewardChoice = -1;
            }

            return;
        }

        if (State == ArcadeStageState.AwaitingRouteChoice)
        {
            if (_requestedRouteChoice >= 0)
            {
                ApplyRouteChoice(_requestedRouteChoice, tick, events);
                _requestedRouteChoice = -1;
            }

            return;
        }

        if (State == ArcadeStageState.EliteIntro)
        {
            var eliteElapsed = tick - _stateStartedTick;
            if (!_eliteIntroReadyEmitted
                && eliteElapsed >= CurrentEncounter.EliteIntroReadyFrames)
            {
                _eliteIntroReadyEmitted = true;
                var elite = _simulation.Actors.FirstOrDefault(actor =>
                    _activeEncounterActorIds.Contains(actor.ActorId));
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.EliteIntroReady,
                    tick,
                    elite?.ActorId ?? CurrentEncounter.Id,
                    Payload: CurrentEncounter.DisplayName));
            }

            if (eliteElapsed >= CurrentEncounter.EliteIntroFightFrames)
            {
                State = ArcadeStageState.EncounterActive;
                _stateStartedTick = tick;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.EliteIntroFight,
                    tick,
                    CurrentEncounter.Id,
                    Payload: CurrentEncounter.DisplayName));
            }

            return;
        }

        if (State == ArcadeStageState.Intermission
            && tick - _stateStartedTick >= CurrentEncounter.GateReleaseDelayFrames)
        {
            var clearedEncounter = CurrentEncounter;
            if (_rewardsEnabled
                && clearedEncounter.RouteChoices.Count > 1
                && BeginRouteChoice(clearedEncounter, tick, events))
            {
                return;
            }

            _encounterIndex++;
            State = ArcadeStageState.Traveling;
            _stateStartedTick = tick;
            _cameraTransitionActive = false;
            if (clearedEncounter.LocksFightRoom)
            {
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.FightRoomOpened,
                    tick,
                    clearedEncounter.Id,
                    Payload: CurrentEncounter.Id));
            }

            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.StageAdvanceOpened,
                tick,
                Mission.Id,
                Payload: CurrentObjective));
        }

        if (State == ArcadeStageState.Traveling)
        {
            var encounter = CurrentEncounter;
            if (player.SimPosition.X >= encounter.TriggerX - 0.05f)
            {
                StartEncounter(encounter, tick, events);
            }
        }

        if (State == ArcadeStageState.BossIntro)
        {
            var introElapsed = tick - _stateStartedTick;
            var cutsceneFrames = _bossIntroCutsceneFrames;
            var cinematicFrames = CurrentEncounter.BossIntroCinematicFrames;
            var readyFrames = _suppressBossIntroReadyHold
                ? 0
                : CurrentEncounter.BossIntroReadyFrames;

            // Pre-cinematic gate: a matchup cutscene (if any) plays first while the
            // sim stays frozen; the cinematic build-up (Phase 1 / BossIntroStarted)
            // only begins once the cutscene window elapses.
            if (!_bossIntroStartedEmitted && introElapsed >= cutsceneFrames)
            {
                _bossIntroStartedEmitted = true;
                var introBoss = _simulation.Actors.FirstOrDefault(
                    actor => _activeEncounterActorIds.Contains(actor.ActorId));
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.BossIntroStarted,
                    tick,
                    introBoss?.ActorId ?? CurrentEncounter.Id,
                    Payload: CurrentEncounter.DisplayName));
            }

            // Phase 1 -> Phase 2/3 gate: the cinematic build-up has elapsed, so
            // reveal the HUD and raise the READY call-to-action. The player stays
            // frozen; only the presentation choreography advances.
            if (!_bossIntroReadyEmitted && introElapsed >= cutsceneFrames + cinematicFrames)
            {
                _bossIntroReadyEmitted = true;
                var readyBoss = _simulation.Actors.FirstOrDefault(
                    actor => _activeEncounterActorIds.Contains(actor.ActorId));
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.BossIntroReady,
                    tick,
                    readyBoss?.ActorId ?? CurrentEncounter.Id,
                    Payload: CurrentEncounter.DisplayName));
            }

            // Phase 4 gate: the READY hold has elapsed, so release into the fight.
            // Entering EncounterActive resumes the frozen simulation, which is the
            // single authoritative point where player input is unlocked.
            if (introElapsed >= cutsceneFrames + cinematicFrames + readyFrames)
            {
                State = ArcadeStageState.EncounterActive;
                _stateStartedTick = tick;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.BossIntroFight,
                    tick,
                    CurrentEncounter.Id));
                
                var boss = _simulation.Actors.FirstOrDefault(actor => _activeEncounterActorIds.Contains(actor.ActorId));
                if (boss != null)
                {
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.BossPhaseChanged,
                        tick,
                        boss.ActorId,
                        Payload: "BossActivated"));
                }
            }
        }

        if (State == ArcadeStageState.EncounterActive)
        {
            TryStartCurrentWave(CurrentEncounter, tick, events);
            if (_waveStarted && CurrentWave is not null)
            {
                ProcessPendingSpawns(CurrentEncounter, CurrentWave, tick, events);
            }
        }

        _stageSmokeScenario?.UpdateBeforeSimulation(tick, this);
        UpdateCameraTracking(tick);
        RunBossSmokeInput(tick);
        _cpuSmokeScenario?.UpdateBeforeSimulation(tick, State == ArcadeStageState.EncounterActive);
        ClampActorsToCurrentStageBounds(tick);
        ApplyDuelRules(tick);
        ApplyIsolatedDuelArenas(tick);
        _cameraSmokeScenario?.UpdateBeforeSimulation(tick, this);
    }

    public void UpdateAfterSimulation(int tick, ICollection<CombatPresentationEvent> events)
    {
        ApplyPartyTether(tick, events);
        ClampActorsToCurrentStageBounds(tick);
        ApplyIsolatedDuelFacing(tick);
        EvaluateHazardZones(tick, events);
        TrackAndCleanupDefeatedActors(tick, events);
        _cpuSmokeScenario?.CaptureAfterSimulation(tick, events);
        _cameraSmokeScenario?.CaptureAfterSimulation(tick, this, events);

        if (State == ArcadeStageState.EncounterActive && CurrentWaveIsDefeated())
        {
            CompleteCurrentWave(tick, events);
        }

        _stageSmokeScenario?.CaptureAfterSimulation(tick, this, events);
        _ladderSmokeScenario?.CaptureAfterSimulation(tick, this, events);

        foreach (var evt in events.ToArray())
        {
            if (evt.Type == CombatPresentationEventType.BossPhaseChanged && evt.Payload != "BossActivated")
            {
                var currentBoss = _simulation.Actors.FirstOrDefault(a => a.ActorId == evt.SourceActorId);
                var burstPhase = currentBoss?.CurrentBossPhase;
                var boundsExpansion = burstPhase?.PhaseBurstBoundsExpansion ?? 8.0f;

                // Wall break cinematic!
                events.Add(new CombatPresentationEvent(CombatPresentationEventType.WallBreakStarted, tick, evt.SourceActorId));

                // Expand stage bounds dynamically for the new phase.
                Mission.StageMaxX += boundsExpansion;
                Mission.StageMinX -= boundsExpansion;
                if (CurrentEncounter != null)
                {
                    // Keep the authored duel lock centered while expanding the
                    // arena bounds; drifting the lock each phase can push both
                    // fighters off-screen and reveal out-of-bounds stage space.
                    BeginCameraTransition(
                        CurrentEncounter.CameraLockX,
                        tick,
                        CurrentEncounter.CameraTransitionFrames);

                    if (currentBoss != null && (burstPhase?.TriggersPhaseBurst ?? true))
                    {
                        // Phase Burst: authored transition shockwave and buffs.
                        currentBoss.DamageModifierPercent = burstPhase?.PhaseBurstDamageBuffPercent ?? 25;
                        currentBoss.DefenseModifierPercent = burstPhase?.PhaseBurstDefenseBuffPercent ?? 25;
                        var burstPushback = burstPhase?.PhaseBurstPushback ?? 25.0f;
                        var burstHitstun = burstPhase?.PhaseBurstHitstunFrames ?? 40;

                        foreach (var otherActor in _simulation.Actors.Where(a => a.TeamId != currentBoss.TeamId && !a.IsDead))
                        {
                            otherActor.Velocity = new Godot.Vector3(
                                PhaseBurst.ResolvePushVelocity(
                                    currentBoss.SimPosition.X,
                                    otherActor.SimPosition.X,
                                    burstPushback),
                                0.0f,
                                0.0f);
                            otherActor.StateMachine.EnterHitstun(burstHitstun, tick);
                        }

                        // Presentation-only signal so HUD, camera, audio, and
                        // shake can react to the burst without owning the outcome.
                        events.Add(new CombatPresentationEvent(
                            CombatPresentationEventType.PhaseBurst,
                            tick,
                            currentBoss.ActorId,
                            Payload: burstPushback.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                    }

                    if (currentBoss != null
                        && currentBoss.CurrentBossPhaseIndex == CurrentEncounter.MergeAtPhase
                        && CurrentEncounter.BossType == BossEncounterType.TagTeam)
                    {
                        // Drop barriers, remove target locks
                        foreach (var bossInstance in _simulation.Actors.Where(a => _activeEncounterActorIds.Contains(a.ActorId)))
                        {
                            bossInstance.LockedTargetPlayerId = null;
                        }
                    }
                }
                
                if (evt.Payload == "mastered_instinct")
                {
                    // Trigger massive aura effect for UI/Presentation
                    events.Add(new CombatPresentationEvent(CombatPresentationEventType.SuperStarted, tick, evt.SourceActorId, "Mastered Instinct Aura"));
                }
                
                events.Add(new CombatPresentationEvent(CombatPresentationEventType.DashStarted, tick, evt.SourceActorId, "Sparking Burst"));

                break; // Only one wall break per tick
            }
        }

        if (State != ArcadeStageState.AwaitingFormSwap)
        {
            return;
        }

        var player = GetPlayer();
        if (player?.CurrentForm.Id != Mission.BossFormId)
        {
            return;
        }

        State = ArcadeStageState.Complete;
        _stateStartedTick = tick;
        EmitStageCompleted(tick, player.ActorId, events);
    }

    private void StartEncounter(
        StageEncounterData encounter,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        State = encounter.Kind switch
        {
            StageEncounterKind.Boss => ArcadeStageState.BossIntro,
            StageEncounterKind.Elite => ArcadeStageState.EliteIntro,
            _ => ArcadeStageState.EncounterActive,
        };
        _stateStartedTick = tick;
        _bossIntroReadyEmitted = false;
        _bossIntroStartedEmitted = true;
        _eliteIntroReadyEmitted = false;
        _bossIntroCutsceneFrames = 0;
        _spawnedInCurrentEncounter = 0;
        _activeEncounterActorIds.Clear();
        _activeWaveActorIds.Clear();
        _pendingSpawns.Clear();
        _warnedSpawns.Clear();
        _crowdCoordinator.Configure(
            encounter.MaxSimultaneousAttackers,
            encounter.MaxAttackersPerPlayer);
        PrepareEncounterWaves(encounter, tick);
        _currentWaveIndex = 0;
        _waveStarted = false;
        _nextWaveStartTick = tick + (CurrentWave?.StartDelayFrames ?? 0);
        if (encounter.LocksFightRoom)
        {
            BeginCameraTransition(
                encounter.CameraLockX,
                tick,
                encounter.CameraTransitionFrames);
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.FightRoomLocked,
                tick,
                encounter.Id,
                Payload: $"{encounter.ArenaMinX:0.###}|{encounter.ArenaMaxX:0.###}|{encounter.CameraLockX:0.###}"));
        }

        SpawnProps(encounter, tick, events);
        TryStartCurrentWave(encounter, tick, events);

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_SMOKE_TEST") == "1")
        {
            GD.Print(
                $"[StageSmoke] Started {encounter.Id}: active={_activeEncounterActorIds.Count}, queued={_pendingSpawns.Count}, camera={_cameraCenterX:0.00}, tick={tick}.");
        }

        if (encounter.Kind == StageEncounterKind.Boss)
        {
            var bossActor = _simulation.Actors
                .FirstOrDefault(actor => _activeEncounterActorIds.Contains(actor.ActorId));
            var bossActorId = bossActor?.ActorId ?? encounter.Id;
            var playerFormId = GetPlayer()?.CurrentForm.Id ?? "";
            var bossFormId = bossActor?.CurrentForm.Id ?? "";
            var cutscene = ResolveBossIntroCutscene(encounter, bossActor);
            _bossIntroCutsceneFrames = cutscene?.DurationFrames ?? 0;

            if (_bossIntroCutsceneFrames > 0)
            {
                // A matchup cutscene plays first (SF4/DBFZ rival intro); the
                // BossIntroStarted cinematic is deferred until the cutscene window
                // elapses inside the BossIntro loop. The sim stays frozen throughout.
                _bossIntroStartedEmitted = false;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.BossIntroCutscene,
                    tick,
                    bossActorId,
                    Payload: $"{playerFormId}|{bossFormId}"));
            }
            else
            {
                _bossIntroStartedEmitted = true;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.BossIntroStarted,
                    tick,
                    bossActorId,
                    Payload: encounter.DisplayName));
            }
        }
        else if (encounter.Kind == StageEncounterKind.Elite)
        {
            var elite = _simulation.Actors.FirstOrDefault(actor =>
                _activeEncounterActorIds.Contains(actor.ActorId));
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.EliteIntroStarted,
                tick,
                elite?.ActorId ?? encounter.Id,
                Payload: encounter.DisplayName));
        }
        else
        {
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.EncounterStarted,
                tick,
                encounter.Id,
                Payload: encounter.DisplayName));
        }
    }

    private MatchupCutsceneData? ResolveBossIntroCutscene(
        StageEncounterData encounter,
        CombatActor? fallbackBoss)
    {
        if (_suppressBossIntroReadyHold)
        {
            return null;
        }

        if (encounter.BossType == BossEncounterType.IsolatedDuel)
        {
            MatchupCutsceneData? longest = null;
            foreach (var player in _simulation.Actors.Where(actor => actor.IsPlayerControlled && !actor.IsDead))
            {
                var boss = _simulation.Actors.FirstOrDefault(actor =>
                    actor.IsBoss
                    && !actor.IsDead
                    && actor.LockedTargetPlayerId == player.PlayerId);
                var candidate = MatchupCutsceneCatalog.TryGet(
                    player.CurrentForm.Id,
                    boss?.CurrentForm.Id ?? "");
                if (candidate is not null
                    && (longest is null || candidate.DurationFrames > longest.DurationFrames))
                {
                    longest = candidate;
                }
            }

            return longest;
        }

        return MatchupCutsceneCatalog.TryGet(
            GetPlayer()?.CurrentForm.Id ?? "",
            fallbackBoss?.CurrentForm.Id ?? "");
    }

    private void ProcessPendingSpawns(
        StageEncounterData encounter,
        StageWaveData wave,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        EmitPendingEntryWarnings(encounter, tick, events);
        var activeCount = _simulation.Actors.Count(actor =>
            _activeEncounterActorIds.Contains(actor.ActorId) && !actor.IsDead);
        while (_pendingSpawns.Count > 0
               && activeCount < Mathf.Max(
                   1,
                   wave.MaxActiveEnemies > 0
                       ? wave.MaxActiveEnemies
                       : encounter.MaxActiveEnemies)
               && tick - _waveSpawnStartedTick >= _pendingSpawns[0].SpawnDelayFrames)
        {
            var spawn = _pendingSpawns[0];
            _pendingSpawns.RemoveAt(0);

            var players = _simulation.Actors.Where(a => a.IsPlayerControlled).ToList();
            if (encounter.SpawnPolicy == EnemySpawnPolicy.PerPlayer && players.Count > 0)
            {
                foreach (var player in players)
                {
                    var targetId = (encounter.BossType == BossEncounterType.IsolatedDuel || encounter.BossType == BossEncounterType.TagTeam) 
                        ? (int?)player.PlayerId 
                        : null;
                    string? archetypeOverride = null;
                    if (encounter.BossType == BossEncounterType.IsolatedDuel
                        && encounter.IsolatedDuelBossArchetypeIds.Count > 0)
                    {
                        var slot = Mathf.Max(0, player.PlayerId - 1);
                        archetypeOverride = encounter.IsolatedDuelBossArchetypeIds[
                            slot % encounter.IsolatedDuelBossArchetypeIds.Count];
                    }
                    var actor = SpawnEnemy(encounter, spawn, tick, targetId, events, archetypeOverride);
                    activeCount++;
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.EnemyEntered,
                        tick,
                        actor.ActorId,
                        Payload: $"{spawn.EntryEdge}|{ResolveEntryProfile(spawn)}|{archetypeOverride ?? spawn.ArchetypeId}"));
                }
            }
            else
            {
                var actor = SpawnEnemy(encounter, spawn, tick, null, events);
                activeCount++;
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.EnemyEntered,
                    tick,
                    actor.ActorId,
                    Payload: $"{spawn.EntryEdge}|{ResolveEntryProfile(spawn)}|{spawn.ArchetypeId}"));
            }
        }
    }

    private void EmitPendingEntryWarnings(
        StageEncounterData encounter,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var elapsed = tick - _waveSpawnStartedTick;
        foreach (var spawn in _pendingSpawns)
        {
            if (spawn.SpawnDelayFrames <= 0
                || spawn.WarningLeadFrames <= 0
                || elapsed < Mathf.Max(
                    0,
                    spawn.SpawnDelayFrames - spawn.WarningLeadFrames)
                || !_warnedSpawns.Add(spawn))
            {
                continue;
            }

            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.EnemyEntryWarning,
                tick,
                encounter.Id,
                Payload: $"{spawn.EntryEdge}|{ResolveEntryProfile(spawn)}|{spawn.ArchetypeId}"));
        }
    }

    private void SpawnProps(StageEncounterData encounter, int tick, ICollection<CombatPresentationEvent> events)
    {
        foreach (var propData in encounter.Props)
        {
            var form = propData.ArchetypeId switch
            {
                "prop_crate" => HazardRosterFactory.CreateBreakableCrate(),
                "prop_explosive_canister" => HazardRosterFactory.CreateExplosiveCanister(),
                "world_warrior_training_dummy" => HazardRosterFactory.CreateWorldWarriorTrainingDummy(),
                "world_warrior_supply_crate" => HazardRosterFactory.CreateWorldWarriorSupplyCrate(),
                "world_warrior_pavilion_rack_chest" => HazardRosterFactory.CreateWorldWarriorPavilionRackChest(),
                "world_warrior_grand_tournament_trophy_podium" => HazardRosterFactory.CreateWorldWarriorGrandTournamentTrophyPodium(),
                _ => HazardRosterFactory.CreateBreakableCrate(),
            };
            var tint = Colors.White;
            
            form.MaxHealth = propData.Health;
            if (!string.IsNullOrEmpty(propData.SpritePath))
            {
                form.SpriteSheetPath = propData.SpritePath;
            }
            if (propData.SpritePixelSize > 0.0f)
            {
                form.SpritePixelSize = propData.SpritePixelSize;
            }
            if (propData.SpriteGroundOffsetPixels > 0.0f)
            {
                form.SpriteGroundOffsetPixels = propData.SpriteGroundOffsetPixels;
            }
            
            var actorId = $"{encounter.Id}_prop_{++_spawnSequence}";
            var spawnPosition = new Godot.Vector3(propData.PositionX, 0, propData.PositionZ);
            
            var actor = CombatActorFactory.CreateAndRegister(
                _actorRoot,
                _simulation,
                actorId,
                form.DisplayName,
                form,
                spawnPosition,
                teamId: 2, 
                playerId: 0,
                isPlayer: false,
                isBoss: false,
                presentationTint: tint);

            actor.State = CombatActorState.Idle;
            _propDataByActorId[actorId] = propData;
            
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.EnemyEntered,
                tick,
                actorId,
                Payload: $"Prop|{propData.ArchetypeId}"));
        }
    }

    private void SpawnPickup(
        Godot.Vector3 position,
        int tick,
        StagePickupType pickupType = StagePickupType.Meter)
    {
        var form = pickupType switch
        {
            StagePickupType.Health when Mission.WorldId == "world_warrior_sector" =>
                HazardRosterFactory.CreateWorldWarriorHealthPickup(),
            StagePickupType.Meter when Mission.WorldId == "world_warrior_sector" =>
                HazardRosterFactory.CreateWorldWarriorMeterPickup(),
            StagePickupType.Score when Mission.WorldId == "world_warrior_sector" =>
                HazardRosterFactory.CreateWorldWarriorScorePickup(),
            StagePickupType.Health => HazardRosterFactory.CreateHealthPickup(),
            StagePickupType.Score => HazardRosterFactory.CreateScorePickup(),
            _ => HazardRosterFactory.CreateMeterPickup(),
        };
        var tint = Colors.White;
        var actorId = $"pickup_{++_spawnSequence}";
        var actor = CombatActorFactory.CreateAndRegister(
            _actorRoot,
            _simulation,
            actorId,
            form.DisplayName,
            form,
            position,
            teamId: 0,
            playerId: 0,
            isPlayer: false,
            isBoss: false,
            presentationTint: tint);

        actor.State = CombatActorState.Idle;
    }

    private void EvaluateHazardZones(int tick, ICollection<CombatPresentationEvent> events)
    {
        if (State != ArcadeStageState.EncounterActive)
        {
            return;
        }

        var encounter = CurrentEncounter;
        if (encounter.HazardZones.Count == 0)
        {
            return;
        }

        var isBossEncounter = encounter.Kind == StageEncounterKind.Boss;
        var elapsed = tick - _stateStartedTick;

        foreach (var zone in encounter.HazardZones)
        {
            if (isBossEncounter && !zone.ActiveDuringBoss)
            {
                continue;
            }

            var hazardFrame = StageHazardRuntime.Resolve(zone, elapsed);
            var cycle = zone.RepeatIntervalFrames > 0
                ? Mathf.Max(0, elapsed - zone.ActivationDelayFrames) / zone.RepeatIntervalFrames
                : 0;
            var warnKey = $"{encounter.Id}:{zone.Id}:{cycle}";

            if (hazardFrame.IsWarning)
            {
                // Warning phase: announce on entry, then flash periodically.
                if (_warnedHazardZones.Add(warnKey) || elapsed % 30 == 0)
                {
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.HazardZoneWarning,
                        tick,
                        encounter.Id,
                        Payload: zone.WarningText));
                }

                continue;
            }

            if (!hazardFrame.IsActive)
            {
                continue;
            }

            if (zone.Behavior == StageHazardBehavior.PushZone)
            {
                var pushStep = StageHazardRuntime.ResolvePushStep(zone);
                foreach (var target in HazardTargetsInside(zone, hazardFrame))
                {
                    var position = target.SimPosition + pushStep;
                    target.SimPosition = new Vector3(
                        Mathf.Clamp(position.X, encounter.ArenaMinX, encounter.ArenaMaxX),
                        position.Y,
                        Mathf.Clamp(position.Z, CurrentLaneMinZ, CurrentLaneMaxZ));
                }

                continue;
            }

            // Falling strikes impact once on active entry. Persistent zones keep
            // their twice-per-second damage cadence.
            var impactKey = $"{encounter.Id}:{zone.Id}:{cycle}";
            if (zone.Behavior == StageHazardBehavior.FallingStrike)
            {
                if (!_triggeredHazardCycles.Add(impactKey))
                {
                    continue;
                }

                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.HazardZoneImpacted,
                    tick,
                    encounter.Id,
                    Payload: zone.Id));
            }
            else if (tick % 30 != 0)
            {
                continue;
            }

            var damage = Mathf.Max(1, Mathf.RoundToInt(zone.DamagePerSecond * 0.5f));
            foreach (var target in HazardTargetsInside(zone, hazardFrame))
            {
                if (!target.ApplyHazardDamage(damage, tick))
                {
                    continue;
                }

                target.Velocity += new Vector3(zone.KnockbackX, 0.0f, zone.KnockbackZ);
                if (zone.HitstunFrames > 0 && !target.IsDead)
                {
                    target.StateMachine.EnterHitstun(zone.HitstunFrames, tick);
                }

                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.HazardZoneDamage,
                    tick,
                    encounter.Id,
                    target.ActorId,
                    $"{zone.Id}|-{damage}"));
            }
        }
    }

    private CombatActor SpawnEnemy(
        StageEncounterData encounter,
        EnemySpawnData spawn,
        int tick,
        int? targetPlayerId,
        ICollection<CombatPresentationEvent> events,
        string? archetypeOverride = null)
    {
        var isBoss = encounter.Kind == StageEncounterKind.Boss;
        var isAuthoredElite = encounter.Kind == StageEncounterKind.Elite;
        var archetypeId = string.IsNullOrEmpty(archetypeOverride) ? spawn.ArchetypeId : archetypeOverride;
        var form = archetypeId switch
        {
            "archive_scout" => TestRosterFactory.CreateArchiveScout(),
            "index_warden_veyra" => TestRosterFactory.CreateIndexWardenVeyra(),
            "archive_raider" => TestRosterFactory.CreateArchiveRaider(),
            "cipher_captain_rhune" => TestRosterFactory.CreateCipherCaptainRhune(),
            "archive_bruiser" => TestRosterFactory.CreateArchiveBruiser(),
            "overseer_basalt" => TestRosterFactory.CreateOverseerBasalt(),
            "archive_knight_boss" => TestRosterFactory.CreateTestBoss(),
            "world_warrior_rookie" => TestRosterFactory.CreateWorldWarriorRookie(),
            "world_warrior_dojo_prodigy_kenzo" => TestRosterFactory.CreateWorldWarriorDojoProdigyKenzo(),
            "world_warrior_striker" => TestRosterFactory.CreateWorldWarriorStriker(),
            "world_warrior_pavilion_ace_makoto" => TestRosterFactory.CreateWorldWarriorPavilionAceMakoto(),
            "world_warrior_grappler" => TestRosterFactory.CreateWorldWarriorGrappler(),
            "world_warrior_grand_grappler_tetsu" => TestRosterFactory.CreateWorldWarriorGrandGrapplerTetsu(),
            "world_warrior_ryu_boss" => TestRosterFactory.CreateWorldWarriorRyuBoss(),
            "astral_saibaman" => GokuRosterFactory.CreateSaibaman(),
            "astral_frieza_scout" => GokuRosterFactory.CreateFriezaScout(),
            "astral_frieza_heavy" => GokuRosterFactory.CreateFriezaHeavy(),
            "astral_ki_captain" => GokuRosterFactory.CreateKiCaptain(),
            "astral_goku_boss" => GokuRosterFactory.CreateGokuBoss(),
            "hazard_boulder" => HazardRosterFactory.CreateBoulderHazard(),
            _ => TestRosterFactory.CreateTrainingEnemy(),
        };

        var tint = archetypeId switch
        {
            "archive_scout" => new Color(0.92f, 0.42f, 0.34f),
            "index_warden_veyra" => Colors.White,
            "archive_raider" => new Color(0.78f, 0.30f, 0.42f),
            "cipher_captain_rhune" => Colors.White,
            "archive_bruiser" => new Color(0.98f, 0.66f, 0.24f),
            "overseer_basalt" => Colors.White,
            "archive_knight_boss" => new Color(0.68f, 0.46f, 0.92f),
            "world_warrior_rookie" => new Color(0.92f, 0.64f, 0.30f),
            "world_warrior_dojo_prodigy_kenzo" => Colors.White,
            "world_warrior_striker" => new Color(0.36f, 0.72f, 0.96f),
            "world_warrior_pavilion_ace_makoto" => Colors.White,
            "world_warrior_grappler" => new Color(0.78f, 0.42f, 0.86f),
            "world_warrior_grand_grappler_tetsu" => Colors.White,
            "world_warrior_ryu_boss" => Colors.White,
            "astral_saibaman" => new Color(0.44f, 0.84f, 0.28f),
            "astral_frieza_scout" => new Color(0.55f, 0.42f, 0.92f),
            "astral_frieza_heavy" => new Color(0.84f, 0.33f, 0.38f),
            "astral_ki_captain" => new Color(0.22f, 0.72f, 0.94f),
            "astral_goku_boss" => Colors.White,
            "hazard_boulder" => Colors.White,
            _ => Colors.Salmon,
        };

        var actorId = $"{encounter.Id}_{++_spawnSequence}";
        var spawnPosition = ResolveSpawnPosition(encounter, spawn);
        var displayName = string.IsNullOrEmpty(archetypeOverride) ? spawn.DisplayName : form.DisplayName;
        var playerCount = _simulation.Actors.Count(a => a.IsPlayerControlled);

        // E2: co-op HP scaling (encounter multiplier defaults to 1.0 = off).
        if (encounter.PerPlayerHPMultiplier > 1.0f && playerCount > 1)
        {
            var scale = 1.0f + (playerCount - 1) * (encounter.PerPlayerHPMultiplier - 1.0f);
            form.MaxHealth = Mathf.RoundToInt(form.MaxHealth * scale);
        }

        // E1: elite variants for non-boss enemies (off in headless/replay so
        // deterministic smoke tests are unaffected).
        var isElite = isAuthoredElite || (!isBoss && _rewardsEnabled && RollElite());
        if (isElite)
        {
            form.MaxHealth = Mathf.RoundToInt(form.MaxHealth * 1.8f);
            displayName = isAuthoredElite ? spawn.DisplayName : $"Elite {displayName}";
            tint = isAuthoredElite && form.RoleTags.Contains("named_elite")
                ? Colors.White
                : new Color(1.0f, 0.84f, 0.32f);
        }

        var actor = CombatActorFactory.CreateAndRegister(
            _actorRoot,
            _simulation,
            actorId,
            displayName,
            form,
            spawnPosition,
            teamId: 2,
            playerId: 0,
            isPlayer: false,
            isBoss: isBoss,
            presentationTint: tint);

        if (isElite)
        {
            actor.IsElite = true;
            actor.DamageModifierPercent = 30;
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.EliteEnemySpawned,
                tick,
                actor.ActorId,
                Payload: displayName));
        }

        actor.LockedTargetPlayerId = targetPlayerId;
        if (spawn.SpawnHeight > 0.0f)
        {
            actor.HoverHeight = spawn.SpawnHeight;
            actor.SimPosition = new Vector3(actor.SimPosition.X, spawn.SpawnHeight, actor.SimPosition.Z);
            actor.Position = actor.SimPosition;
        }

        actor.AiSlotIndex = _spawnedInCurrentEncounter++;
        actor.AiInitialDelayFrames = 16 + ((actor.AiSlotIndex * 13 + _spawnSequence * 7) % 32);
        if (!isBoss)
        {
            actor.AiAttackIntervalFrames = 88 + ((_spawnSequence * 11) % 28);
            actor.ArcadeBrain?.SetCrowdCoordinator(_crowdCoordinator);
            var entryProfile = ResolveEntryProfile(spawn);
            actor.ArcadeBrain?.ConfigureStageEntry(
                entryProfile,
                ResolveEntryTarget(encounter, spawn, entryProfile),
                tick);
            if (entryProfile == EnemyEntryProfile.DropIn)
            {
                actor.State = CombatActorState.Jumping;
                actor.Velocity = Vector3.Zero;
            }
        }

        if (isBoss && Mission.RequiresFormSwapToComplete && !string.IsNullOrWhiteSpace(Mission.BossFormId))
        {
            actor.UnlockableFormOnDefeat = Mission.BossFormId switch
            {
                "world_warrior_ryu_form" => TestRosterFactory.CreateWorldWarriorRyuForm(),
                "goku_archive_form" => GokuRosterFactory.CreateGokuArchiveForm(),
                _ => TestRosterFactory.CreateArchiveKnightForm(),
            };
            if ((OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1")
                && OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") != "1")
            {
                actor.IsAiEnabled = false;
            }
        }

        _activeEncounterActorIds.Add(actor.ActorId);
        _activeWaveActorIds.Add(actor.ActorId);
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_SMOKE_TEST") == "1")
        {
            GD.Print(
                $"[StageSmoke] Spawned {actor.ActorId} from {spawn.EntryEdge} at ({spawnPosition.X:0.00},{spawnPosition.Z:0.00}), tick={tick}.");
        }

        return actor;
    }

    private bool RollElite()
    {
        // Deterministic per-spawn roll (~18% chance) seeded by spawn ordinals.
        var random = new System.Random(_spawnSequence * 97 + _encounterIndex * 13 + 7);
        return random.NextDouble() < 0.18;
    }

    private void CompleteCurrentEncounter(int tick, ICollection<CombatPresentationEvent> events)
    {
        var encounter = Mission.Encounters[_encounterIndex];
        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.EncounterCleared,
            tick,
            encounter.Id,
            Payload: encounter.DisplayName));

        if (encounter.Kind == StageEncounterKind.Boss)
        {
            if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1")
            {
                GD.Print($"[BossAiFight] Boss defeated at tick {tick}.");
            }

            if (Mission.RequiresFormSwapToComplete)
            {
                State = ArcadeStageState.AwaitingFormSwap;
                _stateStartedTick = tick;
            }
            else
            {
                State = ArcadeStageState.Complete;
                _stateStartedTick = tick;
                EmitStageCompleted(tick, GetPlayer()?.ActorId ?? encounter.Id, events);
            }
            return;
        }

        _defeatedMinions += _resolvedEnemyCount;

        if (encounter.Kind == StageEncounterKind.Elite)
        {
            State = ArcadeStageState.Complete;
            _stateStartedTick = tick;
            EmitStageCompleted(tick, GetPlayer()?.ActorId ?? encounter.Id, events);
            return;
        }

        if (_rewardsEnabled && TryOfferReward(encounter, tick, events))
        {
            return;
        }

        EnterIntermission(tick, events);
    }

    private void EmitStageCompleted(
        int tick,
        string sourceActorId,
        ICollection<CombatPresentationEvent> events)
    {
        if (_stageCompleteEventSent)
        {
            return;
        }

        _stageCompleteEventSent = true;
        var completedEncounter = CurrentEncounter;
        if (completedEncounter.LocksFightRoom)
        {
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.FightRoomOpened,
                tick,
                completedEncounter.Id,
                Payload: Mission.Id));
        }

        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.StageCompleted,
            tick,
            sourceActorId,
            Payload: Mission.Id));
    }

    private bool TryOfferReward(
        StageEncounterData encounter,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var offer = RunRewardSystem.RollOffer(
            tick + _encounterIndex * 101,
            RunSessionManager.Instance.EquippedMoveCards,
            RunSessionManager.Instance.ActiveArtifacts);
        if (offer.Count == 0)
        {
            return false;
        }

        _pendingRewardOptions.Clear();
        _pendingRewardOptions.AddRange(offer);
        _requestedRewardChoice = -1;
        State = ArcadeStageState.AwaitingReward;
        _stateStartedTick = tick;

        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.RewardOfferReady,
            tick,
            encounter.Id,
            Payload: string.Join(
                "|",
                offer.Select(option =>
                    $"{option.DisplayName}::{option.Description}::{option.Rarity}::{option.IconKey}"))));
        return true;
    }

    private void ApplyRewardChoice(
        int index,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        if (index >= 0 && index < _pendingRewardOptions.Count)
        {
            var option = _pendingRewardOptions[index];
            if (option.Kind == RewardKind.MoveCard)
            {
                RunSessionManager.Instance.EquippedMoveCards.Add(option.Id);
                var card = MoveCardCatalog.GetCard(option.Id);
                if (card is not null)
                {
                    foreach (var actor in _simulation.Actors.Where(a => a.IsPlayerControlled))
                    {
                        if (actor.CurrentForm.Moves.All(m => m.Id != card.Move.Id))
                        {
                            actor.CurrentForm.Moves.Add(card.Move);
                        }
                    }
                }
            }

            else
            {
                RunSessionManager.Instance.ActiveArtifacts.Add(option.Id);
                var artifact = ArtifactCatalog.GetArtifact(option.Id);
                if (artifact is { HealOnAcquirePercent: > 0 })
                {
                    foreach (var actor in _simulation.Actors.Where(a => a.IsPlayerControlled))
                    {
                        var healAmount = Mathf.RoundToInt(
                            actor.CurrentForm.MaxHealth * (artifact.HealOnAcquirePercent / 100f));
                        var restored = actor.RestoreHealth(healAmount);
                        if (restored > 0)
                        {
                            events.Add(new CombatPresentationEvent(
                                CombatPresentationEventType.HealthRestored,
                                tick,
                                actor.ActorId,
                                Payload: restored.ToString()));
                        }
                    }
                }
            }

            var synergyBonus = RunSessionManager.Instance.GetSynergyDamageBonusPercent();
            var acquiredLabel = synergyBonus > 0
                ? $"{option.DisplayName}  (+{synergyBonus}% synergy)"
                : option.DisplayName;
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.RewardChosen,
                tick,
                _simulation.Actors.FirstOrDefault(a => a.IsPlayerControlled)?.ActorId ?? "",
                Payload: acquiredLabel));
        }

        _pendingRewardOptions.Clear();
        EnterIntermission(tick, events);
    }

    private IEnumerable<CombatActor> HazardTargetsInside(
        StageHazardZoneData zone,
        StageHazardFrame hazardFrame)
    {
        return _simulation.Actors.Where(actor =>
            !actor.IsDead
            && !actor.CurrentForm.RoleTags.Contains("pickup")
            && !actor.CurrentForm.RoleTags.Contains("hazard")
            && (actor.IsPlayerControlled
                ? zone.Targets.HasFlag(StageHazardTargetMask.Players)
                : zone.Targets.HasFlag(StageHazardTargetMask.Enemies))
            && actor.SimPosition.X >= hazardFrame.MinX
            && actor.SimPosition.X <= hazardFrame.MaxX
            && actor.SimPosition.Z >= hazardFrame.MinZ
            && actor.SimPosition.Z <= hazardFrame.MaxZ);
    }

    private void EnterIntermission(int tick, ICollection<CombatPresentationEvent> events)
    {
        State = ArcadeStageState.Intermission;
        _stateStartedTick = tick;

        var player = GetPlayer();
        if (player is null)
        {
            return;
        }

        var restored = player.RestoreHealth(24);
        if (restored > 0)
        {
            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.HealthRestored,
                tick,
                player.ActorId,
                Payload: restored.ToString()));
        }
    }

    private bool CurrentWaveIsDefeated()
    {
        return _waveStarted
            && _pendingSpawns.Count == 0
            && _activeWaveActorIds.Count > 0
            && _simulation.Actors
                .Where(actor => _activeWaveActorIds.Contains(actor.ActorId))
                .All(actor => actor.IsDead);
    }

    private void CompleteCurrentWave(
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var wave = CurrentWave;
        if (wave is null)
        {
            return;
        }

        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.WaveCleared,
            tick,
            CurrentEncounter.Id,
            wave.Id,
            wave.DisplayName));
        ApplyWaveRewards(wave, tick, events);

        _waveStarted = false;
        _pendingSpawns.Clear();
        _activeWaveActorIds.Clear();
        if (_currentWaveIndex + 1 >= _encounterWaves.Count)
        {
            CompleteCurrentEncounter(tick, events);
            return;
        }

        _currentWaveIndex++;
        _nextWaveStartTick = tick + (CurrentWave?.StartDelayFrames ?? 0);
    }

    private void TryStartCurrentWave(
        StageEncounterData encounter,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var wave = CurrentWave;
        if (_waveStarted || wave is null || tick < _nextWaveStartTick)
        {
            return;
        }

        _waveStarted = true;
        _waveSpawnStartedTick = tick;
        _activeWaveActorIds.Clear();
        _pendingSpawns.Clear();
        _warnedSpawns.Clear();
        _pendingSpawns.AddRange(wave.Spawns.OrderBy(spawn => spawn.SpawnDelayFrames));
        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.WaveStarted,
            tick,
            encounter.Id,
            wave.Id,
            wave.DisplayName));
        ProcessPendingSpawns(encounter, wave, tick, events);
    }

    private void PrepareEncounterWaves(StageEncounterData encounter, int tick)
    {
        _encounterWaves.Clear();
        var authoredWaves = encounter.Waves.Count > 0
            ? encounter.Waves
            : new List<StageWaveData>
            {
                new()
                {
                    Id = $"{encounter.Id}_wave_1",
                    DisplayName = encounter.DisplayName,
                    MaxActiveEnemies = encounter.MaxActiveEnemies,
                    Spawns = encounter.Spawns,
                    UseRandomPool = encounter.UseRandomPool,
                    RandomArchetypePool = encounter.RandomArchetypePool,
                    RandomSpawnCount = encounter.RandomSpawnCount,
                },
            };

        for (var waveIndex = 0; waveIndex < authoredWaves.Count; waveIndex++)
        {
            var source = authoredWaves[waveIndex];
            var resolved = new StageWaveData
            {
                Id = source.Id,
                DisplayName = string.IsNullOrWhiteSpace(source.DisplayName)
                    ? $"Wave {waveIndex + 1}"
                    : source.DisplayName,
                StartDelayFrames = source.StartDelayFrames,
                MaxActiveEnemies = source.MaxActiveEnemies,
                HealthReward = source.HealthReward,
                MeterReward = source.MeterReward,
                Spawns = source.UseRandomPool && source.RandomArchetypePool.Count > 0
                    ? CreateRandomWaveSpawns(source, tick + waveIndex * 7919)
                    : new List<EnemySpawnData>(source.Spawns),
            };
            _encounterWaves.Add(resolved);
        }

        _resolvedEnemyCount = _encounterWaves.Sum(wave => wave.Spawns.Count);
    }

    private static List<EnemySpawnData> CreateRandomWaveSpawns(
        StageWaveData wave,
        int seed)
    {
        var random = new System.Random(seed);
        var spawns = new List<EnemySpawnData>();
        for (var spawnIndex = 0; spawnIndex < wave.RandomSpawnCount; spawnIndex++)
        {
            var archetype = wave.RandomArchetypePool[
                random.Next(wave.RandomArchetypePool.Count)];
            spawns.Add(new EnemySpawnData
            {
                ArchetypeId = archetype,
                DisplayName = "Random " + archetype,
                OffsetX = 5.0f,
                LaneZ = (float)(random.NextDouble() * 2.0 - 1.0),
                EntryEdge = random.NextDouble() > 0.5
                    ? EnemyEntryEdge.Right
                    : EnemyEntryEdge.Left,
                SpawnDelayFrames = spawnIndex * 60,
                EntryDistance = 2.0f,
            });
        }

        return spawns;
    }

    private void ApplyWaveRewards(
        StageWaveData wave,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        if (wave.HealthReward <= 0 && wave.MeterReward <= 0)
        {
            return;
        }

        foreach (var player in _simulation.Actors.Where(actor =>
                     actor.IsPlayerControlled && !actor.IsDead))
        {
            var restoredHealth = player.RestoreHealth(wave.HealthReward);
            var previousMeter = player.Meter;
            player.AddMeter(wave.MeterReward);
            var restoredMeter = player.Meter - previousMeter;
            if (restoredHealth > 0)
            {
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.HealthRestored,
                    tick,
                    player.ActorId,
                    Payload: restoredHealth.ToString()));
            }

            if (restoredHealth > 0 || restoredMeter > 0)
            {
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.WaveRewardGranted,
                    tick,
                    player.ActorId,
                    wave.Id,
                    $"{restoredHealth}|{restoredMeter}"));
            }
        }
    }

    private void ApplyPartyTether(
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var livePlayers = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .OrderBy(actor => actor.PlayerId)
            .ToArray();
        if (livePlayers.Length < 2
            || State is not (ArcadeStageState.Traveling or ArcadeStageState.Complete))
        {
            _catchUpActorIds.Clear();
            _hardCatchUpActorIds.Clear();
            return;
        }

        var leader = livePlayers.MaxBy(actor => actor.SimPosition.X);
        if (leader is null)
        {
            return;
        }

        foreach (var player in livePlayers)
        {
            if (player == leader)
            {
                _catchUpActorIds.Remove(player.ActorId);
                _hardCatchUpActorIds.Remove(player.ActorId);
                continue;
            }

            var separation = leader.SimPosition.X - player.SimPosition.X;
            if (separation <= Mission.PartySoftSeparationX)
            {
                if (_catchUpActorIds.Remove(player.ActorId))
                {
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.PlayerCatchUpCompleted,
                        tick,
                        player.ActorId,
                        leader.ActorId));
                }

                _hardCatchUpActorIds.Remove(player.ActorId);
                continue;
            }

            if (_catchUpActorIds.Add(player.ActorId))
            {
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.PlayerCatchUpStarted,
                    tick,
                    player.ActorId,
                    leader.ActorId));
            }

            var targetX = leader.SimPosition.X - Mission.PartySoftSeparationX;
            var catchUpStep = Mission.PartyCatchUpSpeed / GameConstants.TickRate;
            var correctedX = Mathf.MoveToward(
                player.SimPosition.X,
                targetX,
                catchUpStep);
            if (separation > Mission.PartyHardSeparationX)
            {
                correctedX = Mathf.Max(
                    correctedX,
                    leader.SimPosition.X - Mission.PartyHardSeparationX);
                if (_hardCatchUpActorIds.Add(player.ActorId))
                {
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.PlayerCatchUpWarped,
                        tick,
                        player.ActorId,
                        leader.ActorId));
                }
            }

            player.SimPosition = new Vector3(
                correctedX,
                player.SimPosition.Y,
                player.SimPosition.Z);
        }
    }

    private static int CountConfiguredEnemies(StageEncounterData encounter)
    {
        if (encounter.Waves.Count == 0)
        {
            return encounter.UseRandomPool
                ? encounter.RandomSpawnCount
                : encounter.Spawns.Count;
        }

        return encounter.Waves.Sum(wave =>
            wave.UseRandomPool ? wave.RandomSpawnCount : wave.Spawns.Count);
    }

    private void TrackAndCleanupDefeatedActors(
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        var newlyDefeatedActors = _simulation.Actors.Where(actor =>
                !actor.IsPlayerControlled
                && !actor.IsBoss
                && actor.IsDead
                && !_defeatedActorTicks.ContainsKey(actor.ActorId))
            .ToArray();
        foreach (var actor in newlyDefeatedActors)
        {
            _crowdCoordinator.ReleaseActor(actor);
            _defeatedActorTicks[actor.ActorId] = tick;

            if (actor.CurrentForm.RoleTags.Contains("pickup"))
            {
                actor.Visible = false;
            }

            if (actor.CurrentForm.RoleTags.Contains("breakable"))
            {
                // Props do not have a death animation. Hide the intact sprite as
                // soon as it breaks while retaining the actor briefly for event
                // consumers and deterministic cleanup.
                actor.Visible = false;
                if (_propDataByActorId.TryGetValue(actor.ActorId, out var propData))
                {
                    if (propData.ExplodesOnBreak)
                    {
                        ApplyPropExplosion(actor, propData, tick, events);
                    }

                    if (propData.SpawnsPickupOnBreak
                        && DeterministicDropRoll(actor.ActorId) <= Mathf.Clamp(propData.DropChance, 0.0f, 1.0f))
                    {
                        SpawnPickup(actor.SimPosition, tick, propData.DropType);
                    }
                }
            }
        }

        var expiredActorIds = _defeatedActorTicks
            .Where(pair => tick - pair.Value >= DefeatedActorLifetimeFrames)
            .Select(pair => pair.Key)
            .ToArray();

        foreach (var actorId in expiredActorIds)
        {
            var actor = _simulation.Actors.FirstOrDefault(candidate => candidate.ActorId == actorId);
            if (actor is not null)
            {
                _crowdCoordinator.ReleaseActor(actor);
                _simulation.UnregisterActor(actor);
                actor.QueueFree();
            }

            _defeatedActorTicks.Remove(actorId);
            _propDataByActorId.Remove(actorId);
        }
    }

    private void ApplyPropExplosion(
        CombatActor source,
        StagePropData prop,
        int tick,
        ICollection<CombatPresentationEvent> events)
    {
        events.Add(new CombatPresentationEvent(
            CombatPresentationEventType.PropExploded,
            tick,
            source.ActorId,
            Payload: prop.Id));

        foreach (var target in _simulation.Actors.Where(actor =>
                     actor != source
                     && !actor.IsDead
                     && !actor.CurrentForm.RoleTags.Contains("pickup")
                     && !actor.CurrentForm.RoleTags.Contains("hazard")
                     && StagePropRuntime.CanExplosionAffect(
                         prop,
                         source.SimPosition,
                         actor.SimPosition,
                         actor.IsPlayerControlled)))
        {
            if (!target.ApplyHazardDamage(prop.ExplosionDamage, tick))
            {
                continue;
            }

            target.Velocity += StagePropRuntime.ResolveExplosionKnockback(
                source.SimPosition,
                target.SimPosition,
                prop.ExplosionKnockback);
            target.InvincibilityFrames = Mathf.Max(target.InvincibilityFrames, 8);
            if (prop.ExplosionHitstunFrames > 0 && !target.IsDead)
            {
                target.StateMachine.EnterHitstun(prop.ExplosionHitstunFrames, tick);
            }

            events.Add(new CombatPresentationEvent(
                CombatPresentationEventType.HazardZoneDamage,
                tick,
                source.ActorId,
                target.ActorId,
                $"{prop.Id}|-{prop.ExplosionDamage}"));
        }
    }

    private static float DeterministicDropRoll(string actorId)
    {
        unchecked
        {
            uint hash = 2166136261;
            foreach (var character in actorId)
            {
                hash ^= character;
                hash *= 16777619;
            }

            return (hash & 0x00FFFFFF) / (float)0x01000000;
        }
    }

    private void ClampActorsToCurrentStageBounds(int tick)
    {
        if (Mission.Encounters.Count == 0)
        {
            return;
        }

        var duelPlayers =
            IsBossDuelActive && CurrentEncounter.BossType == BossEncounterType.IsolatedDuel
                ? _simulation.Actors
                    .Where(a => a.IsPlayerControlled && !a.IsDead)
                    .OrderBy(a => a.PlayerId)
                    .ToList()
                : null;

        foreach (var actor in _simulation.Actors.Where(actor => !actor.IsDead))
        {
            var isPlayer = actor.IsPlayerControlled;
            float minX;
            float maxX;

            if (duelPlayers is { Count: >= 2 }
                && TryGetIsolatedDuelBand(actor, duelPlayers, out var bandMin, out var bandMax))
            {
                // Isolated duel: each player and their locked rival own a
                // horizontal band of the full stage, independent of the shared
                // camera, so the two duels stay spatially separated (each split
                // viewport camera then frames its own band).
                minX = bandMin;
                maxX = bandMax;
            }
            else
            {
                minX = isPlayer
                    ? Mathf.Max(Mission.StageMinX, CameraLeftX + Mission.PlayerLeftScreenMargin)
                    : Mathf.Max(Mission.StageMinX - Mission.EnemyEntryPadding, CameraLeftX - Mission.EnemyEntryPadding);
                maxX = isPlayer
                    ? Mathf.Min(Mission.StageMaxX, CameraRightX - Mission.PlayerRightScreenMargin)
                    : Mathf.Min(Mission.StageMaxX + Mission.EnemyEntryPadding, CameraRightX + Mission.EnemyEntryPadding);
                if (IsArenaLocked)
                {
                    var roomPadding = isPlayer
                        ? CurrentEncounter.PlayerBoundaryInset
                        : -Mission.EnemyEntryPadding;
                    minX = Mathf.Max(minX, CurrentEncounter.ArenaMinX + roomPadding);
                    maxX = Mathf.Min(maxX, CurrentEncounter.ArenaMaxX - roomPadding);
                }
            }

            if (minX > maxX)
            {
                var roomCenterX = (CurrentEncounter.ArenaMinX + CurrentEncounter.ArenaMaxX) * 0.5f;
                minX = roomCenterX;
                maxX = roomCenterX;
            }

            if (actor.State == CombatActorState.Hitstun
                && actor.PendingWallBounce
                && (actor.SimPosition.X < minX || actor.SimPosition.X > maxX))
            {
                actor.PendingWallBounce = false;
                var bounceX = HitResolution.ResolveWallBounceVelocity(actor.Velocity.X);
                actor.Velocity = new Vector3(
                    bounceX,
                    Mathf.Max(actor.Velocity.Y, 6.0f),
                    actor.Velocity.Z);
                actor.QueueEvent(CombatPresentationEventType.WallBounce, tick);
            }

            var lanePadding = isPlayer ? 0.0f : Mission.EnemyEntryPadding;
            actor.SimPosition = new Vector3(
                Mathf.Clamp(actor.SimPosition.X, minX, maxX),
                actor.SimPosition.Y,
                Mathf.Clamp(
                    actor.SimPosition.Z,
                    _currentLaneMinZ - lanePadding,
                    _currentLaneMaxZ + lanePadding));

            if (isPlayer)
            {
                const float wallThreshold = 0.7f;
                actor.NearLeftWall = actor.SimPosition.X <= minX + wallThreshold;
                actor.NearRightWall = actor.SimPosition.X >= maxX - wallThreshold;
                actor.InVerticalSection = IsArenaLocked && CurrentEncounter.VerticalSection;
            }
        }
    }

    private void ApplyDuelRules(int tick)
    {
        if (!IsBossDuelActive)
        {
            return;
        }

        var encounter = CurrentEncounter;
        if (!encounter.DuelFacingLock && encounter.DuelLaneHalfDepth <= 0.0f)
        {
            return;
        }

        var boss = _simulation.Actors.FirstOrDefault(
            a => a.IsBoss && !a.IsDead && _activeEncounterActorIds.Contains(a.ActorId));
        if (boss is null)
        {
            return;
        }

        foreach (var player in _simulation.Actors.Where(a => a.IsPlayerControlled && !a.IsDead))
        {
            if (encounter.DuelFacingLock)
            {
                player.FacingRight = DuelRules.ResolveFacingRight(player.SimPosition.X, boss.SimPosition.X);
            }

            if (encounter.DuelLaneHalfDepth > 0.0f)
            {
                player.SimPosition = new Godot.Vector3(
                    player.SimPosition.X,
                    player.SimPosition.Y,
                    DuelRules.ClampLaneDepth(player.SimPosition.Z, encounter.DuelLaneHalfDepth));
            }
        }

        if (encounter.DuelLaneHalfDepth > 0.0f)
        {
            boss.SimPosition = new Godot.Vector3(
                boss.SimPosition.X,
                boss.SimPosition.Y,
                DuelRules.ClampLaneDepth(boss.SimPosition.Z, encounter.DuelLaneHalfDepth));
        }
    }

    /// <summary>
    /// E3: for an Isolated-Duel encounter with multiple players, partition the
    /// arena into equal horizontal sub-arenas and clamp each player and their
    /// locked rival into their own band so duels cannot bleed into each other.
    /// </summary>
    private void ApplyIsolatedDuelArenas(int tick)
    {
        if (!IsBossDuelActive || CurrentEncounter.BossType != BossEncounterType.IsolatedDuel)
        {
            return;
        }

        var players = _simulation.Actors
            .Where(a => a.IsPlayerControlled && !a.IsDead)
            .OrderBy(a => a.PlayerId)
            .ToList();
        if (players.Count < 2)
        {
            return;
        }

        var bandWidth = (Mission.StageMaxX - Mission.StageMinX) / players.Count;
        for (var i = 0; i < players.Count; i++)
        {
            var bandMin = Mission.StageMinX + bandWidth * i + 0.5f;
            var bandMax = Mission.StageMinX + bandWidth * (i + 1) - 0.5f;
            var player = players[i];

            var rivals = _simulation.Actors.Where(a =>
                !a.IsPlayerControlled
                && !a.IsDead
                && a.LockedTargetPlayerId == player.PlayerId).ToList();

            // One-time face-off: place the player on the left and the boss on the
            // right of the band so every player's split has the same orientation
            // and the boss never spawns behind the player.
            var boss = rivals.FirstOrDefault(a => a.IsBoss);
            if (boss is not null && _isolatedDuelPlacedBossIds.Add(boss.ActorId))
            {
                var center = (bandMin + bandMax) * 0.5f;
                var gap = Mathf.Min(1.6f, (bandMax - bandMin) * 0.35f);
                player.SimPosition = new Godot.Vector3(
                    center - gap, player.SimPosition.Y, player.SimPosition.Z);
                player.FacingRight = true;
                boss.SimPosition = new Godot.Vector3(
                    center + gap, boss.SimPosition.Y, boss.SimPosition.Z);
                boss.FacingRight = false;
            }

            ClampActorX(player, bandMin, bandMax);
            foreach (var rival in rivals)
            {
                ClampActorX(rival, bandMin, bandMax);
            }
        }
    }

    /// <summary>
    /// E3: keep each player oriented toward their own locked boss during an
    /// isolated duel. Runs post-simulation so it is the authoritative facing for
    /// the tick: directional attacks always read toward the boss and the boss is
    /// never treated as "behind" the player.
    /// </summary>
    private void ApplyIsolatedDuelFacing(int tick)
    {
        if (!IsBossDuelActive || CurrentEncounter.BossType != BossEncounterType.IsolatedDuel)
        {
            return;
        }

        foreach (var player in _simulation.Actors.Where(a => a.IsPlayerControlled && !a.IsDead))
        {
            var boss = _simulation.Actors.FirstOrDefault(a =>
                a.IsBoss && !a.IsDead && a.LockedTargetPlayerId == player.PlayerId);
            if (boss is null)
            {
                continue;
            }

            player.FacingRight = boss.SimPosition.X >= player.SimPosition.X;
        }
    }

    private static void ClampActorX(CombatActor actor, float minX, float maxX)
    {
        if (actor.SimPosition.X < minX || actor.SimPosition.X > maxX)
        {
            actor.SimPosition = new Godot.Vector3(
                Mathf.Clamp(actor.SimPosition.X, minX, maxX),
                actor.SimPosition.Y,
                actor.SimPosition.Z);
        }
    }

    /// <summary>
    /// E3: resolve the isolated-duel band (a horizontal slice of the full stage)
    /// that an actor belongs to. Players own the band matching their turn order;
    /// a locked rival inherits the band of the player it is locked to. Returns
    /// false for unlocked enemies / neutral actors so they clamp normally.
    /// </summary>
    private bool TryGetIsolatedDuelBand(
        CombatActor actor,
        IReadOnlyList<CombatActor> orderedPlayers,
        out float bandMin,
        out float bandMax)
    {
        bandMin = 0.0f;
        bandMax = 0.0f;

        var bandPlayerId = actor.IsPlayerControlled ? actor.PlayerId : actor.LockedTargetPlayerId;
        if (bandPlayerId is null)
        {
            return false;
        }

        var index = -1;
        for (var i = 0; i < orderedPlayers.Count; i++)
        {
            if (orderedPlayers[i].PlayerId == bandPlayerId)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            return false;
        }

        var bandWidth = (Mission.StageMaxX - Mission.StageMinX) / orderedPlayers.Count;
        bandMin = Mission.StageMinX + bandWidth * index + 0.5f;
        bandMax = Mission.StageMinX + bandWidth * (index + 1) - 0.5f;
        return true;
    }

    private void UpdateVerticalCamera()
    {
        var target = 0.0f;
        if (IsArenaLocked && CurrentEncounter.VerticalSection)
        {
            var players = _simulation.Actors
                .Where(a => a.IsPlayerControlled && !a.IsDead)
                .ToArray();
            if (players.Length > 0)
            {
                target = Mathf.Clamp(
                    players.Max(a => a.SimPosition.Y),
                    0.0f,
                    CurrentEncounter.VerticalCeiling);
            }
        }

        _cameraCenterY = Mathf.MoveToward(_cameraCenterY, target, 0.35f);
    }

    private void UpdateCameraTracking(int tick)
    {
        UpdateVerticalCamera();
        if (IsArenaLocked)
        {
            UpdateFightRoomCamera(tick);
            return;
        }
        var livePlayers = _simulation.Actors
            .Where(actor => actor.IsPlayerControlled && !actor.IsDead)
            .ToArray();
        if (livePlayers.Length == 0)
        {
            return;
        }

        var playerMidpointX = livePlayers.Average(actor => actor.SimPosition.X);
        var forwardLimit = State == ArcadeStageState.Complete
            ? Mission.StageMaxX - Mission.CameraViewportWidth * 0.5f
            : CurrentEncounter.CameraLockX;
        var threshold = _cameraCenterX + Mission.CameraFollowThresholdX;
        if (playerMidpointX < threshold || _cameraCenterX >= forwardLimit)
        {
            return;
        }

        var desiredCenter = playerMidpointX - Mission.CameraFollowThresholdX;
        var scrollStep = Mission.CameraScrollSpeed / GameConstants.TickRate;
        _cameraCenterX = Mathf.Min(
            forwardLimit,
            Mathf.MoveToward(_cameraCenterX, desiredCenter, scrollStep));
    }

    private void BeginCameraTransition(float targetX, int tick, int transitionFrames)
    {
        _cameraTransitionStartX = _cameraCenterX;
        _cameraTransitionTargetX = targetX;
        _cameraTransitionStartedTick = tick;
        _cameraTransitionFrames = transitionFrames;
        _cameraTransitionActive = transitionFrames > 0
            && !Mathf.IsEqualApprox(_cameraCenterX, targetX);
        if (!_cameraTransitionActive)
        {
            _cameraCenterX = targetX;
        }
    }

    private void UpdateFightRoomCamera(int tick)
    {
        if (!_cameraTransitionActive)
        {
            _cameraCenterX = CurrentEncounter.CameraLockX;
            return;
        }

        var progress = Mathf.Clamp(
            (tick - _cameraTransitionStartedTick) / (float)_cameraTransitionFrames,
            0.0f,
            1.0f);
        var easedProgress = progress * progress * (3.0f - 2.0f * progress);
        _cameraCenterX = Mathf.Lerp(
            _cameraTransitionStartX,
            _cameraTransitionTargetX,
            easedProgress);
        if (progress >= 1.0f)
        {
            _cameraTransitionActive = false;
            _cameraCenterX = _cameraTransitionTargetX;
        }
    }

    private void BeginEncounterLaneTransition(int tick)
    {
        if (Mission.Encounters.Count == 0
            || _laneBoundsEncounterId == CurrentEncounter.Id)
        {
            return;
        }

        _laneBoundsEncounterId = CurrentEncounter.Id;
        _laneTransitionStartMinZ = _currentLaneMinZ;
        _laneTransitionStartMaxZ = _currentLaneMaxZ;
        _laneTransitionStartedTick = tick;
        var targetMin = StageLaneRuntime.TargetMinZ(Mission.LaneMinZ, CurrentEncounter);
        var targetMax = StageLaneRuntime.TargetMaxZ(Mission.LaneMaxZ, CurrentEncounter);
        _laneTransitionActive = CurrentEncounter.LaneTransitionFrames > 0
            && (!Mathf.IsEqualApprox(_currentLaneMinZ, targetMin)
                || !Mathf.IsEqualApprox(_currentLaneMaxZ, targetMax));
        if (!_laneTransitionActive)
        {
            _currentLaneMinZ = targetMin;
            _currentLaneMaxZ = targetMax;
        }
    }

    private void UpdateEncounterLaneBounds(int tick)
    {
        BeginEncounterLaneTransition(tick);
        if (!_laneTransitionActive)
        {
            return;
        }

        var frame = StageLaneRuntime.Resolve(
            _laneTransitionStartMinZ,
            _laneTransitionStartMaxZ,
            Mission.LaneMinZ,
            Mission.LaneMaxZ,
            CurrentEncounter,
            tick - _laneTransitionStartedTick);
        _currentLaneMinZ = frame.MinZ;
        _currentLaneMaxZ = frame.MaxZ;
        if (frame.Progress >= 1.0f)
        {
            _laneTransitionActive = false;
        }
    }

    private Vector3 ResolveSpawnPosition(StageEncounterData encounter, EnemySpawnData spawn)
    {
        var laneMinZ = StageLaneRuntime.TargetMinZ(Mission.LaneMinZ, encounter);
        var laneMaxZ = StageLaneRuntime.TargetMaxZ(Mission.LaneMaxZ, encounter);
        var entryProfile = ResolveEntryProfile(spawn);
        if (entryProfile == EnemyEntryProfile.DropIn)
        {
            return new Vector3(
                Mathf.Clamp(
                    encounter.TriggerX + spawn.OffsetX,
                    encounter.ArenaMinX,
                    encounter.ArenaMaxX),
                spawn.EntryHeight,
                Mathf.Clamp(spawn.LaneZ, laneMinZ, laneMaxZ));
        }

        if (entryProfile == EnemyEntryProfile.Ambush)
        {
            return new Vector3(
                Mathf.Clamp(
                    encounter.TriggerX + spawn.OffsetX,
                    encounter.ArenaMinX,
                    encounter.ArenaMaxX),
                0.0f,
                Mathf.Clamp(spawn.LaneZ, laneMinZ, laneMaxZ));
        }

        return spawn.EntryEdge switch
        {
            EnemyEntryEdge.Left => new Vector3(
                CameraLeftX - spawn.EntryDistance + spawn.OffsetX,
                0.0f,
                spawn.LaneZ),
            EnemyEntryEdge.Right => new Vector3(
                CameraRightX + spawn.EntryDistance + spawn.OffsetX,
                0.0f,
                spawn.LaneZ),
            EnemyEntryEdge.FarLane => new Vector3(
                encounter.TriggerX + spawn.OffsetX,
                0.0f,
                laneMinZ - spawn.EntryDistance),
            EnemyEntryEdge.NearLane => new Vector3(
                encounter.TriggerX + spawn.OffsetX,
                0.0f,
                laneMaxZ + spawn.EntryDistance),
            _ => new Vector3(encounter.TriggerX + spawn.OffsetX, 0.0f, spawn.LaneZ),
        };
    }

    private Vector3 ResolveEntryTarget(
        StageEncounterData encounter,
        EnemySpawnData spawn,
        EnemyEntryProfile entryProfile)
    {
        var laneMinZ = StageLaneRuntime.TargetMinZ(Mission.LaneMinZ, encounter);
        var laneMaxZ = StageLaneRuntime.TargetMaxZ(Mission.LaneMaxZ, encounter);
        var targetX = entryProfile switch
        {
            EnemyEntryProfile.WalkIn when spawn.EntryEdge == EnemyEntryEdge.Left
                => encounter.ArenaMinX + 0.8f,
            EnemyEntryProfile.WalkIn when spawn.EntryEdge == EnemyEntryEdge.Right
                => encounter.ArenaMaxX - 0.8f,
            _ => Mathf.Clamp(
                encounter.TriggerX + spawn.OffsetX,
                encounter.ArenaMinX + 0.4f,
                encounter.ArenaMaxX - 0.4f),
        };
        return new Vector3(
            targetX,
            0.0f,
            Mathf.Clamp(spawn.LaneZ, laneMinZ, laneMaxZ));
    }

    private static EnemyEntryProfile ResolveEntryProfile(EnemySpawnData spawn)
    {
        if (spawn.EntryProfile != EnemyEntryProfile.Auto)
        {
            return spawn.EntryProfile;
        }

        return spawn.EntryEdge switch
        {
            EnemyEntryEdge.FarLane => EnemyEntryProfile.Background,
            EnemyEntryEdge.NearLane => EnemyEntryProfile.Foreground,
            _ => EnemyEntryProfile.WalkIn,
        };
    }

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private void RunBossSmokeInput(int tick)
    {
        var aiFightTest = OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1";
        if ((OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") != "1" && !aiFightTest)
            || State != ArcadeStageState.EncounterActive
            || tick < _nextBossSmokeAttackTick)
        {
            return;
        }

        var player = GetPlayer();
        var boss = _simulation.Actors.FirstOrDefault(actor => actor.IsBoss && !actor.IsDead);
        if (aiFightTest && player is not null)
        {
            player.IsVulnerable = false;
        }

        if (tick % 100 == 0 && player is not null && boss is not null)
        {
            GD.Print(
                $"[BossSmoke] Status tick {tick}: player={player.State}, boss={boss.State}, vulnerable={boss.IsVulnerable}, HP={boss.Health}.");
        }

        if (player is null
            || boss is null
            || !boss.IsVulnerable
            || player.State is not (CombatActorState.Idle or CombatActorState.Walking))
        {
            return;
        }

        var move = player.CurrentForm.FindMove("mannequin_heavy");
        if (move is null)
        {
            return;
        }

        boss.SimPosition = new Vector3(player.SimPosition.X + 1.35f, 0.0f, player.SimPosition.Z);
        boss.Velocity = Vector3.Zero;
        player.FacingRight = true;
        if (player.TryStartMove(move, tick))
        {
            _nextBossSmokeAttackTick = tick + 52;
            var logPrefix = aiFightTest ? "[BossAiFight]" : "[BossSmoke]";
            GD.Print(
                $"{logPrefix} Attack started at tick {tick}; boss HP {boss.Health}, phase {boss.CurrentBossPhaseIndex + 1}, intent {boss.CpuBrain?.CurrentIntent}.");
        }
    }

}
