using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Progression;
using ProjectMannequin.Stage;
using ProjectMannequin.DebugTools;

namespace ProjectMannequin.Core;

public partial class GameSimulation : Node
{
    private readonly List<CombatActor> _actors = new();
    private readonly List<CombatPresentationEvent> _presentationEvents = new();
    private readonly List<SequencedPresentationEvent> _presentationEventHistory = new();
    private long _nextPresentationEventSequence;
    private readonly HashSet<string> _processedDefeats = new();
    private readonly HitResolver _hitResolver = new();
    private readonly CommandInterpreter _commandInterpreter = new();
    private readonly CombatProjectileManager _projectileManager = new();

    private LocalInputManager? _localInputManager;
    private CombatFeatureSmokeScenario? _combatFeatureSmokeScenario;
    private BossDuelSmokeScenario? _bossDuelSmokeScenario;
    private WorldWarriorBossSmokeScenario? _worldWarriorBossSmokeScenario;
    private AstralBattlefrontSmokeScenario? _astralBattlefrontSmokeScenario;
    private TrainingDummySmokeScenario? _trainingDummySmokeScenario;
    private PresentationEventAuditScenario? _presentationEventAuditScenario;
    private int _hitStopFramesRemaining;
    private int _superPauseFramesRemaining;
    private string _superPauseOwnerActorId = "";
    private int _beamClashFramesRemaining;
    private int _beamClashScore;
    private CombatProjectile? _clashProjectileA;
    private CombatProjectile? _clashProjectileB;

    private ReplayRecorder? _replayRecorder;
    private string _replayRecordPath = "";
    private int _replayRecordFrames = 240;
    private bool _replayHeaderCaptured;
    private bool _replayFinalized;

    private ReplayInputSource? _replayPlayback;
    private Dictionary<int, ulong>? _replayExpectedChecks;
    private int _replayPlaybackFrames;
    private int _replayVerifyPassed;
    private int _replayVerifyFailed;
    private bool _replayPlaybackSummarized;

    private bool _snapshotSelfCheckEnabled;
    private bool _snapshotSelfCheckDone;

    [Export] public NodePath LocalInputManagerPath { get; set; } = "../LocalInputManager";

    public int CurrentTick { get; private set; }
    public int PresentationClock { get; private set; }
    public IReadOnlyList<CombatActor> Actors => _actors;

    public bool IsReplayPlayback => _replayPlayback is not null;
    public IReadOnlyList<CombatProjectile> Projectiles => _projectileManager.Projectiles;
    public IReadOnlyList<CombatPresentationEvent> LastPresentationEvents => _presentationEvents;
    public ArcadeEncounterDirector? EncounterDirector { get; private set; }
    public CombatMode CurrentMode => EncounterDirector?.ActiveCombatMode ?? CombatMode.Horde;
    public int HitStopFramesRemaining => _hitStopFramesRemaining;
    public int SuperPauseFramesRemaining => _superPauseFramesRemaining;
    public string SuperPauseOwnerActorId => _superPauseOwnerActorId;
    public int BeamClashFramesRemaining => _beamClashFramesRemaining;
    public int BeamClashScore => _beamClashScore;

    public long CopyPresentationEventsSince(
        long afterSequence,
        ICollection<CombatPresentationEvent> destination)
    {
        foreach (var entry in _presentationEventHistory)
        {
            if (entry.Sequence > afterSequence)
            {
                destination.Add(entry.Event);
            }
        }

        return _nextPresentationEventSequence;
    }

    public override void _Ready()
    {
        Engine.PhysicsTicksPerSecond = GameConstants.TickRate;
        _localInputManager = GetNodeOrNull<LocalInputManager>(LocalInputManagerPath);
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_FRAME_DATA_REPORT") == "1")
        {
            GD.Print(ProjectMannequin.Data.FrameDataReport.Build(
                ProjectMannequin.Data.RosterCatalog.ReferenceForms()));
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_INPUT_GRAMMAR_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.InputGrammarTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_SETTINGS_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.SettingsTests.Run());
        }

        // Audio only: the window half of Apply would override the viewport the
        // capture tooling configures for deterministic runtime evidence.
        ProjectMannequin.Settings.SettingsStore.ApplyAudio(
            ProjectMannequin.Settings.SettingsStore.Current);

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_DEFENSE_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.DefensiveMechanicsTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_COMBO_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.ComboRulesTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_MODE_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.BossDuelModeTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_AI_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.AiModulesTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_COMBO_ROUTE_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.ComboRouteTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_REPLAY_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.ReplayTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_TRAINING_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.TrainingDummyTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_FORM_SELECT_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.FormSelectTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ASSIST_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.AssistTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_TOOLING_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.DesignerToolingTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ANIM_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.AnimationDriverTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_DETERMINISM_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.DeterminismTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_REWARD_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.RunRewardTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BUILD_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.RunBuildTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ACCEPTANCE_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.AcceptanceTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_RUN_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.WorldRunTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_RUN_CHECKPOINT_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.RunCheckpointTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_RUN_SCORE_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.RunScoreTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_HAZARD_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.StageHazardTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ARCHIVE_MAP_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.ArchiveMapTests.Run());
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_TEST") == "1")
        {
            GD.Print(ProjectMannequin.DebugTools.ResultsFlowTests.Run());
        }

        _snapshotSelfCheckEnabled = OS.GetEnvironment("PROJECT_MANNEQUIN_SNAPSHOT_TEST") == "1";

        var frameDataExportPath = OS.GetEnvironment("PROJECT_MANNEQUIN_FRAME_DATA_EXPORT");
        if (!string.IsNullOrWhiteSpace(frameDataExportPath))
        {
            try
            {
                var report = ProjectMannequin.Data.FrameDataExporter.BuildFullReport(
                    ProjectMannequin.Data.RosterCatalog.ReferenceForms());
                System.IO.File.WriteAllText(frameDataExportPath, report);
                GD.Print($"[FrameData] Exported balancing report -> {frameDataExportPath}");
            }
            catch (System.Exception exception)
            {
                GD.PrintErr($"[FrameData] Export failed '{frameDataExportPath}': {exception.Message}");
            }
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_COMBAT_SMOKE_TEST") == "1")
        {
            _combatFeatureSmokeScenario = new CombatFeatureSmokeScenario(this);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST") == "1")
        {
            _bossDuelSmokeScenario = new BossDuelSmokeScenario(this);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1")
        {
            _worldWarriorBossSmokeScenario = new WorldWarriorBossSmokeScenario(this);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1")
        {
            _astralBattlefrontSmokeScenario = new AstralBattlefrontSmokeScenario(this);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_TRAINING_SMOKE_TEST") == "1"
            && ProjectMannequin.Combat.TrainingDummyController.TryParse(
                OS.GetEnvironment("PROJECT_MANNEQUIN_TRAINING_DUMMY"), out var trainingSmokeSetting))
        {
            _trainingDummySmokeScenario = new TrainingDummySmokeScenario(this, trainingSmokeSetting);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_ACCEPTANCE_SMOKE_TEST") == "1")
        {
            _presentationEventAuditScenario = new PresentationEventAuditScenario();
        }

        SetupReplayFromEnvironment();
    }

    public override void _PhysicsProcess(double delta)
    {
        StepSimulation();
    }

    public void RegisterActor(CombatActor actor)
    {
        if (_actors.Contains(actor))
        {
            return;
        }

        actor.Simulation = this;
        _actors.Add(actor);
    }

    public void UnregisterActor(CombatActor actor)
    {
        _actors.Remove(actor);
    }

    public void SetEncounterDirector(ArcadeEncounterDirector director)
    {
        EncounterDirector = director;
    }

    /// <summary>Captures the whole simulation's authoritative state for rollback/replay tooling.</summary>
    public GameStateSnapshot CaptureSnapshot()
    {
        return new GameStateSnapshot
        {
            Tick = CurrentTick,
            PresentationClock = PresentationClock,
            HitStopFramesRemaining = _hitStopFramesRemaining,
            SuperPauseFramesRemaining = _superPauseFramesRemaining,
            Actors = _actors.Select(actor => actor.CaptureSnapshot()).ToList(),
        };
    }

    /// <summary>Restores the simulation's authoritative state for matching actors.</summary>
    public void RestoreSnapshot(GameStateSnapshot snapshot)
    {
        CurrentTick = snapshot.Tick;
        PresentationClock = snapshot.PresentationClock;
        _hitStopFramesRemaining = snapshot.HitStopFramesRemaining;
        _superPauseFramesRemaining = snapshot.SuperPauseFramesRemaining;

        var byId = snapshot.Actors.ToDictionary(actor => actor.ActorId);
        foreach (var actor in _actors)
        {
            if (byId.TryGetValue(actor.ActorId, out var actorSnapshot))
            {
                actor.RestoreSnapshot(actorSnapshot);
            }
        }
    }

    public void StepSimulation()
    {
        CurrentTick++;
        _presentationEvents.Clear();

        var runSession = RunSessionManager.Instance;
        if (runSession.HasActiveRun && EncounterDirector is not null)
        {
            runSession.ScoreManager.AdvanceGameplayFrame(EncounterDirector.State);
        }

        _localInputManager?.PollInputs(CurrentTick);
        CaptureReplayInputs(CurrentTick);
        _combatFeatureSmokeScenario?.UpdateBeforeSimulation(CurrentTick);
        _bossDuelSmokeScenario?.UpdateBeforeSimulation(CurrentTick);
        if (_beamClashFramesRemaining > 0)
        {
            _beamClashFramesRemaining--;
            
            var inputA = _clashProjectileA?.Owner.IsPlayerControlled == true ? _localInputManager?.GetBufferForPlayer(_clashProjectileA.Owner.PlayerId) : null;
            var inputB = _clashProjectileB?.Owner.IsPlayerControlled == true ? _localInputManager?.GetBufferForPlayer(_clashProjectileB.Owner.PlayerId) : null;
            
            var attackButtons = InputButtons.LightPunch | InputButtons.MediumPunch | InputButtons.HeavyPunch | InputButtons.LightKick | InputButtons.MediumKick | InputButtons.HeavyKick;
            
            if (inputA != null && inputA.JustPressed.HasAny(attackButtons))
            {
                _beamClashScore += 1;
            }
            if (inputB != null && inputB.JustPressed.HasAny(attackButtons))
            {
                _beamClashScore -= 1;
            }
            
            if (_clashProjectileA?.Owner.IsPlayerControlled == false && CurrentTick % 4 == 0) _beamClashScore += 1;
            if (_clashProjectileB?.Owner.IsPlayerControlled == false && CurrentTick % 4 == 0) _beamClashScore -= 1;

            _presentationEvents.Add(new CombatPresentationEvent(
                CombatPresentationEventType.BeamClashUpdated,
                CurrentTick,
                _clashProjectileA?.Owner.ActorId ?? "",
                _clashProjectileB?.Owner.ActorId ?? "",
                $"{_beamClashScore}|{_beamClashFramesRemaining}"));

            if (_beamClashFramesRemaining == 0)
            {
                ResolveBeamClash();
            }
            PublishPresentationEvents();
            return;
        }

        if (_superPauseFramesRemaining > 0)
        {
            _superPauseFramesRemaining--;
            if (_superPauseFramesRemaining == 0)
            {
                _superPauseOwnerActorId = "";
            }

            _combatFeatureSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
            _bossDuelSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
            PublishPresentationEvents();
            return;
        }

        if (_hitStopFramesRemaining > 0)
        {
            _hitStopFramesRemaining--;
            _combatFeatureSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
            _bossDuelSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
            PublishPresentationEvents();
            return;
        }

        EncounterDirector?.UpdateBeforeSimulation(CurrentTick, _presentationEvents);
        
        // The boss-duel smoke scenario deterministically drives the actors, so it
        // must not be frozen by the boss-intro or reward pauses.
        if (EncounterDirector?.State is ArcadeStageState.StageIntro or ArcadeStageState.EliteIntro or ArcadeStageState.BossIntro or ArcadeStageState.AwaitingReward or ArcadeStageState.AwaitingRouteChoice
            && _bossDuelSmokeScenario is null)
        {
            PublishPresentationEvents();
            return;
        }

        _worldWarriorBossSmokeScenario?.UpdateBeforeSimulation(CurrentTick);
        _astralBattlefrontSmokeScenario?.UpdateBeforeSimulation(CurrentTick);

        // Advances only on ticks that actually simulate (not during hitstop,
        // super pause, beam clash, or boss intro), so presentation timing derived
        // from it freezes automatically.
        PresentationClock++;
        foreach (var actor in _actors)
        {
            var input = actor.IsPlayerControlled
                ? _localInputManager?.GetBufferForPlayer(actor.PlayerId)
                : actor.ResolveTrainingInput(CurrentTick);

            actor.StateMachine.Update(CurrentTick, input, _commandInterpreter, _actors);
            actor.UpdateTimers(CurrentTick);
        }

        _projectileManager.SpawnFromActors(_actors, this, CurrentTick);
        foreach (var actor in _actors)
        {
            actor.IntegrateMotion(CurrentTick);
            actor.UpdateComboState(CurrentTick);
            actor.UpdateGuardGauge(CurrentTick);
        }

        ResolvePushboxes();

        var leftX = EncounterDirector?.CameraLeftX ?? -9999.0f;
        var rightX = EncounterDirector?.CameraRightX ?? 9999.0f;
        _projectileManager.Advance(CurrentTick, leftX, rightX);

        foreach (var actor in _actors)
        {
            actor.RefreshCombatBoxes(CurrentTick);
        }

        var hitStopFrames = _hitResolver.Resolve(_actors, CurrentTick, leftX, rightX, _presentationEvents);
        hitStopFrames = Mathf.Max(
            hitStopFrames,
            _projectileManager.ResolveHits(_actors, CurrentTick, _presentationEvents));
        // Marvel Tōkon-style assist ghosts deal damage via a synthesized strike
        // hitbox during their active window (deterministic, ordered iteration).
        foreach (var actor in _actors)
        {
            hitStopFrames = Mathf.Max(
                hitStopFrames,
                actor.AssistSystem.ResolveHits(actor, _actors, CurrentTick, _presentationEvents));
        }
        if (hitStopFrames > 0)
        {
            _hitStopFramesRemaining = hitStopFrames;
            if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1")
            {
                GD.Print($"[BossSmoke] Hit-stop {hitStopFrames}f at tick {CurrentTick}.");
            }

            _presentationEvents.Add(new CombatPresentationEvent(
                CombatPresentationEventType.HitStopStarted,
                CurrentTick,
                "simulation",
                Payload: hitStopFrames.ToString()));
        }

        foreach (var actor in _actors)
        {
            foreach (var actorEvent in actor.DrainEvents())
            {
                _presentationEvents.Add(actorEvent);
                if (actorEvent.Type == CombatPresentationEventType.SuperStarted)
                {
                    BeginSuperPause(actor, actorEvent);
                }
            }
        }

        ProcessDefeats();
        EncounterDirector?.UpdateAfterSimulation(CurrentTick, _presentationEvents);
        if (runSession.HasActiveRun)
        {
            runSession.ScoreManager.CaptureEvents(
                CurrentTick,
                _presentationEvents,
                _actors,
                EncounterDirector?.Mission,
                runSession,
                _presentationEvents);
        }
        _worldWarriorBossSmokeScenario?.CaptureAfterSimulation(
            CurrentTick,
            _presentationEvents,
            EncounterDirector);
        _astralBattlefrontSmokeScenario?.CaptureAfterSimulation(
            CurrentTick,
            _presentationEvents,
            EncounterDirector);
        _combatFeatureSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
        _bossDuelSmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
        _trainingDummySmokeScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);
        _presentationEventAuditScenario?.CaptureAfterSimulation(CurrentTick, _presentationEvents);

        foreach (var actor in _actors)
        {
            actor.UpdatePresentationTransform();
        }

        foreach (var evt in _presentationEvents)
        {
            if (evt.Type == CombatPresentationEventType.BeamClashStarted)
            {
                _clashProjectileA = _projectileManager.Projectiles.FirstOrDefault(p => p.Owner.ActorId == evt.SourceActorId && p.IsInClash);
                _clashProjectileB = _projectileManager.Projectiles.FirstOrDefault(p => p.Owner.ActorId == evt.TargetActorId && p.IsInClash);
                _beamClashFramesRemaining = 180;
                _beamClashScore = 0;
            }
        }

        PublishPresentationEvents();

        ProcessReplayEndOfStep(CurrentTick);
        ProcessSnapshotSelfCheck(CurrentTick);
    }

    private void PublishPresentationEvents()
    {
        foreach (var presentationEvent in _presentationEvents)
        {
            _presentationEventHistory.Add(new SequencedPresentationEvent(
                ++_nextPresentationEventSequence,
                presentationEvent));
        }

        const int maximumHistory = 1024;
        if (_presentationEventHistory.Count > maximumHistory)
        {
            _presentationEventHistory.RemoveRange(
                0,
                _presentationEventHistory.Count - maximumHistory);
        }
    }

    private readonly record struct SequencedPresentationEvent(
        long Sequence,
        CombatPresentationEvent Event);

    private void SetupReplayFromEnvironment()
    {
        var recordPath = OS.GetEnvironment("PROJECT_MANNEQUIN_REPLAY_RECORD");
        if (!string.IsNullOrWhiteSpace(recordPath))
        {
            _replayRecordPath = recordPath;
            _replayRecordFrames = ResolveReplayFrameLimit(240);
            _replayRecorder = new ReplayRecorder();
            GD.Print($"[Replay] Recording enabled: up to {_replayRecordFrames} frames -> {recordPath}");
        }

        var playPath = OS.GetEnvironment("PROJECT_MANNEQUIN_REPLAY_PLAY");
        if (string.IsNullOrWhiteSpace(playPath))
        {
            return;
        }

        if (!System.IO.File.Exists(playPath))
        {
            GD.PrintErr($"[Replay] Playback file not found: {playPath}");
            return;
        }

        try
        {
            var recording = ReplaySerializer.Parse(System.IO.File.ReadAllText(playPath));
            _replayPlayback = new ReplayInputSource(recording);
            _replayPlaybackFrames = recording.Header.FrameCount;
            _replayExpectedChecks = new Dictionary<int, ulong>();
            foreach (var check in recording.StateChecks)
            {
                _replayExpectedChecks[check.Tick] = check.Fingerprint;
            }

            _localInputManager?.SetReplaySource(_replayPlayback);

            if (string.IsNullOrWhiteSpace(
                    OS.GetEnvironment(ProjectMannequin.Data.MvpMissionSelection.WorldOverrideEnvironmentVariable))
                && !string.IsNullOrWhiteSpace(recording.Header.WorldId))
            {
                ProjectMannequin.Data.MvpMissionSelection.TrySelectWorld(recording.Header.WorldId);
            }

            GD.Print(
                $"[Replay] Playback loaded: {recording.Header.FrameCount} frames, "
                + $"{recording.StateChecks.Count} checks, world={recording.Header.WorldId}.");
        }
        catch (System.Exception exception)
        {
            GD.PrintErr($"[Replay] Failed to load playback '{playPath}': {exception.Message}");
        }
    }

    private static int ResolveReplayFrameLimit(int fallback)
    {
        var raw = OS.GetEnvironment("PROJECT_MANNEQUIN_REPLAY_FRAMES");
        return int.TryParse(raw, out var frames) && frames > 0 ? frames : fallback;
    }

    private void CaptureReplayInputs(int tick)
    {
        if (_replayRecorder is null || _replayFinalized)
        {
            return;
        }

        if (!_replayHeaderCaptured && !TryCaptureReplayHeader())
        {
            return;
        }

        var masks = new uint[_replayRecorder.PlayerCount];
        for (var slot = 0; slot < masks.Length; slot++)
        {
            var buffer = _localInputManager?.GetBufferForPlayer(slot + 1);
            masks[slot] = buffer is null ? 0u : (uint)buffer.CurrentHeld;
        }

        _replayRecorder.RecordInputs(tick, masks);
    }

    private bool TryCaptureReplayHeader()
    {
        var players = _actors
            .Where(actor => actor.IsPlayerControlled)
            .OrderBy(actor => actor.PlayerId)
            .ToList();
        if (players.Count == 0)
        {
            return false;
        }

        var worldId = ProjectMannequin.Data.MvpMissionSelection.SelectedWorldId;
        _replayRecorder!.PlayerCount = players.Max(actor => actor.PlayerId);
        _replayRecorder.WorldId = worldId;
        _replayRecorder.MissionId = worldId;
        _replayRecorder.Seed = ComputeReplaySeed(players);
        _replayRecorder.SetForms(players.Select(actor =>
            new ReplayFormEntry(actor.PlayerId, actor.CurrentForm.Id)));
        _replayHeaderCaptured = true;
        return true;
    }

    private static uint ComputeReplaySeed(IReadOnlyList<CombatActor> players)
    {
        var hash = ReplayFingerprint.Begin();
        hash = ReplayFingerprint.CombineInt(hash, players.Count);
        foreach (var formId in players
                     .Select(actor => actor.CurrentForm.Id)
                     .OrderBy(id => id, System.StringComparer.Ordinal))
        {
            hash = ReplayFingerprint.CombineString(hash, formId);
        }

        return (uint)(hash ^ (hash >> 32));
    }

    private ulong ComputeSimulationFingerprint()
    {
        var hash = ReplayFingerprint.Begin();
        hash = ReplayFingerprint.CombineInt(hash, CurrentTick);
        hash = ReplayFingerprint.CombineInt(hash, _actors.Count);
        foreach (var actor in _actors)
        {
            hash = actor.ComputeStateFingerprint(hash);
        }

        return hash;
    }

    private void ProcessReplayEndOfStep(int tick)
    {
        if (_replayRecorder is not null && _replayHeaderCaptured && !_replayFinalized)
        {
            if (tick % 30 == 0)
            {
                _replayRecorder.RecordStateCheck(tick, ComputeSimulationFingerprint());
            }

            if (_replayRecorder.SampleCount >= _replayRecordFrames)
            {
                FinalizeReplayRecording(tick);
            }
        }

        if (_replayExpectedChecks is null)
        {
            return;
        }

        if (_replayExpectedChecks.TryGetValue(tick, out var expected))
        {
            var actual = ComputeSimulationFingerprint();
            if (actual == expected)
            {
                _replayVerifyPassed++;
            }
            else
            {
                _replayVerifyFailed++;
                GD.Print($"[Replay] MISMATCH tick {tick}: expected {expected:X} got {actual:X}.");
            }
        }

        if (!_replayPlaybackSummarized && tick >= _replayPlaybackFrames)
        {
            _replayPlaybackSummarized = true;
            var total = _replayVerifyPassed + _replayVerifyFailed;
            GD.Print(
                $"[Replay] Playback verified {_replayVerifyPassed}/{total} checks. "
                + $"passed={_replayVerifyFailed == 0 && total > 0}");
        }
    }

    private void FinalizeReplayRecording(int tick)
    {
        if (_replayRecorder is null || _replayFinalized)
        {
            return;
        }

        _replayFinalized = true;
        _replayRecorder.RecordStateCheck(tick, ComputeSimulationFingerprint());
        var recording = _replayRecorder.Build();

        try
        {
            var directory = System.IO.Path.GetDirectoryName(_replayRecordPath);
            if (!string.IsNullOrEmpty(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }

            System.IO.File.WriteAllText(_replayRecordPath, ReplaySerializer.Serialize(recording));
            GD.Print(
                $"[Replay] Recorded {recording.Header.FrameCount} frames, "
                + $"{recording.StateChecks.Count} checks -> {_replayRecordPath}");
        }
        catch (System.Exception exception)
        {
            GD.PrintErr($"[Replay] Failed to write '{_replayRecordPath}': {exception.Message}");
        }
    }

    private void ProcessSnapshotSelfCheck(int tick)
    {
        if (!_snapshotSelfCheckEnabled || _snapshotSelfCheckDone || tick < 50 || _actors.Count == 0)
        {
            return;
        }

        // Prove capture/restore round-trips exactly within a single tick (no
        // spawn/despawn in the window): capture, mutate, then restore and confirm
        // the mutation was detectable and fully reverted.
        _snapshotSelfCheckDone = true;
        var before = CaptureSnapshot();

        var target = _actors[0];
        target.SimPosition += new Vector3(3.0f, 1.0f, 0.5f);
        target.Velocity += new Vector3(2.0f, 0.0f, 0.0f);
        target.FacingRight = !target.FacingRight;
        target.AddMeter(13);
        var mutated = CaptureSnapshot();

        RestoreSnapshot(before);
        var afterRestore = CaptureSnapshot();

        var mutatedDetected = !before.Matches(mutated);
        var restored = before.Matches(afterRestore);
        GD.Print(
            $"[Snapshot] self-check mutatedDetected={mutatedDetected} restored={restored} "
            + $"passed={mutatedDetected && restored}");
        if (!restored)
        {
            foreach (var difference in before.Diff(afterRestore))
            {
                GD.Print($"[Snapshot]   diff: {difference}");
            }
        }
    }

    private void ResolveBeamClash()
    {
        if (_clashProjectileA != null && _clashProjectileB != null)
        {
            if (_beamClashScore >= 0)
            {
                _clashProjectileB.Expire();
                _clashProjectileA.IsInClash = false;
                _clashProjectileA.ClashDamageMultiplier = 1.5f;
                _clashProjectileA.ClashUnblockable = true;
            }
            else
            {
                _clashProjectileA.Expire();
                _clashProjectileB.IsInClash = false;
                _clashProjectileB.ClashDamageMultiplier = 1.5f;
                _clashProjectileB.ClashUnblockable = true;
            }
        }
        _presentationEvents.Add(new CombatPresentationEvent(
            CombatPresentationEventType.BeamClashResolved,
            CurrentTick,
            _clashProjectileA?.Owner.ActorId ?? "",
            _clashProjectileB?.Owner.ActorId ?? "",
            _beamClashScore.ToString()));
        _clashProjectileA = null;
        _clashProjectileB = null;
    }

    private void BeginSuperPause(CombatActor actor, CombatPresentationEvent presentationEvent)
    {
        var freezeFrames = actor.CurrentMove?.SuperFreezeFrames ?? 0;
        if (freezeFrames <= _superPauseFramesRemaining)
        {
            return;
        }

        _superPauseFramesRemaining = freezeFrames;
        _superPauseOwnerActorId = presentationEvent.SourceActorId;
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1")
        {
            GD.Print(
                $"[BossAiFight] Super {presentationEvent.Payload} started at tick {CurrentTick}; freeze={freezeFrames}f.");
        }
    }

    private void ProcessDefeats()
    {
        foreach (var defeated in _actors.Where(actor => actor.IsDead && _processedDefeats.Add(actor.ActorId)))
        {
            _presentationEvents.Add(new CombatPresentationEvent(
                CombatPresentationEventType.ActorDefeated,
                CurrentTick,
                defeated.ActorId));

            if (!defeated.IsBoss || defeated.UnlockableFormOnDefeat is null)
            {
                if (defeated.IsPlayerControlled)
                {
                    var session = RunSessionManager.Instance;
                    if (session.RemainingLives > 0)
                    {
                        session.LoseLife();
                        defeated.Revive();
                        defeated.InvincibilityFrames = 180;
                        _presentationEvents.Add(new CombatPresentationEvent(
                            CombatPresentationEventType.PlayerRevived,
                            CurrentTick,
                            defeated.ActorId));
                    }
                    else
                    {
                        _presentationEvents.Add(new CombatPresentationEvent(
                            CombatPresentationEventType.GameOver,
                            CurrentTick,
                            defeated.ActorId));
                    }
                }
                continue;
            }

            foreach (var player in _actors.Where(actor => actor.IsPlayerControlled && !actor.IsDead))
            {
                var unlocked = player.FormArchive.UnlockForm(defeated.UnlockableFormOnDefeat);
                if (!unlocked)
                {
                    continue;
                }

                _presentationEvents.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.FormUnlocked,
                    CurrentTick,
                    player.ActorId,
                    defeated.ActorId,
                    defeated.UnlockableFormOnDefeat.Id));

                MvpProgressStore.UnlockForm(defeated.UnlockableFormOnDefeat.Id);
            }
        }
    }

    private void ResolvePushboxes()
    {
        for (var i = 0; i < _actors.Count; i++)
        {
            for (var j = i + 1; j < _actors.Count; j++)
            {
                var a = _actors[i];
                var b = _actors[j];
                if (a.IsDead || b.IsDead)
                {
                    continue;
                }

                var delta = b.SimPosition - a.SimPosition;
                var minDistanceX = 0.7f;
                var minDistanceZ = 0.6f;

                if (Mathf.Abs(delta.X) >= minDistanceX || Mathf.Abs(delta.Z) >= minDistanceZ)
                {
                    continue;
                }

                var pushX = (minDistanceX - Mathf.Abs(delta.X)) * 0.5f * Mathf.Sign(delta.X == 0.0f ? 1.0f : delta.X);
                var pushZ = (minDistanceZ - Mathf.Abs(delta.Z)) * 0.5f * Mathf.Sign(delta.Z == 0.0f ? 1.0f : delta.Z);

                a.SimPosition -= new Vector3(pushX, 0.0f, pushZ);
                b.SimPosition += new Vector3(pushX, 0.0f, pushZ);
            }
        }
    }
}
