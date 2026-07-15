using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Presentation;

namespace ProjectMannequin.DebugTools;

public partial class DebugOverlay : CanvasLayer
{
    private readonly Queue<string> _eventLog = new();
    private Label _label = null!;
    private FrameTimelineView _timelineView = null!;
    private GameSimulation? _simulation;
    private LocalInputManager? _inputManager;
    private readonly CommandInterpreter _commandInterpreter = new();
    private int _lastLoggedTick = -1;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public NodePath LocalInputManagerPath { get; set; } = "../LocalInputManager";
    [Export] public bool VisibleOnStart { get; set; }

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        _inputManager = GetNodeOrNull<LocalInputManager>(LocalInputManagerPath);

        _label = new Label
        {
            Name = "DebugLabel",
            Position = new Vector2(16.0f, 16.0f),
        };
        _label.AddThemeFontSizeOverride("font_size", 14);
        AddChild(_label);

        _timelineView = new FrameTimelineView
        {
            Name = "FrameTimelineView",
            Position = new Vector2(16.0f, 340.0f),
            Size = new Vector2(640.0f, 130.0f),
            Visible = false,
        };
        AddChild(_timelineView);

        Visible = VisibleOnStart;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } keyEvent)
        {
            return;
        }

        var command = DebugCommandResolver.Resolve(keyEvent.PhysicalKeycode);
        if (command == DebugCommand.None)
        {
            return;
        }

        if (!DebugCommandResolver.IsAlwaysAvailable(command) && !Visible)
        {
            return;
        }

        ExecuteCommand(command);
        GetViewport().SetInputAsHandled();
    }

    private void ExecuteCommand(DebugCommand command)
    {
        switch (command)
        {
            case DebugCommand.ToggleOverlay:
                Visible = !Visible;
                break;
            case DebugCommand.ToggleFrameInspector:
                _timelineView.Visible = !_timelineView.Visible;
                _timelineView.QueueRedraw();
                break;
            case DebugCommand.ExportFrameData:
                ExportFrameData();
                break;
            case DebugCommand.KillEnemies:
                KillEnemies();
                break;
            case DebugCommand.SkipToNextEncounter:
                SkipToNextEncounter();
                break;
            case DebugCommand.SpawnDebugProp:
                SpawnDebugProp();
                break;
            case DebugCommand.RefillResources:
                RefillResources();
                break;
            case DebugCommand.ToggleDummyGuard:
                ToggleDummyGuard();
                break;
            case DebugCommand.AdvanceBossPhase:
                AdvanceBossPhase();
                break;
            case DebugCommand.ForceKnockdown:
                ForceKnockdown();
                break;
        }
    }

    private void KillEnemies()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var enemy in _simulation.Actors.Where(a => !a.IsPlayerControlled && !a.IsDead))
        {
            enemy.ApplyHazardDamage(999999, _simulation.CurrentTick);
        }
    }

    private void SkipToNextEncounter()
    {
        if (_simulation?.EncounterDirector is null)
        {
            return;
        }

        var player = _simulation.Actors.FirstOrDefault(a => a.IsPlayerControlled);
        if (player is null)
        {
            return;
        }

        var nextEncounter = _simulation.EncounterDirector.Mission.Encounters
            .FirstOrDefault(e => e.TriggerX > player.SimPosition.X + 1.0f);
        if (nextEncounter is null)
        {
            return;
        }

        foreach (var p in _simulation.Actors.Where(a => a.IsPlayerControlled))
        {
            p.SimPosition = new Vector3(nextEncounter.TriggerX, p.SimPosition.Y, p.SimPosition.Z);
        }
    }

    private void SpawnDebugProp()
    {
        var player = _simulation?.Actors.FirstOrDefault(a => a.IsPlayerControlled);
        if (_simulation is null || player is null)
        {
            return;
        }

        var form = HazardRosterFactory.CreateBreakableCrate();
        var actorId = $"debug_prop_{System.Guid.NewGuid().ToString()[..4]}";
        var actorRoot = player.GetParent<Node3D>();
        var actor = CombatActorFactory.CreateAndRegister(
            actorRoot,
            _simulation,
            actorId,
            "Debug Crate",
            form,
            player.SimPosition + new Vector3(2f, 0f, 0f),
            teamId: 2,
            playerId: 0,
            isPlayer: false,
            isBoss: false,
            presentationTint: Colors.White);
        actor.State = CombatActorState.Idle;
    }

    private void RefillResources()
    {
        if (_simulation is null)
        {
            return;
        }

        foreach (var player in _simulation.Actors.Where(a => a.IsPlayerControlled))
        {
            player.AddMeter(GameConstants.MeterMax);
            player.RestoreHealth(player.CurrentForm.MaxHealth);
        }
    }

    private void ToggleDummyGuard()
    {
        var dummy = _simulation?.Actors.FirstOrDefault(a => !a.IsPlayerControlled && !a.IsBoss && !a.IsDead);
        if (dummy is null)
        {
            return;
        }

        if (dummy.TrainingBrain is { Setting: TrainingDummySetting.GuardAll })
        {
            dummy.TrainingBrain.Setting = TrainingDummySetting.Stand;
        }
        else
        {
            dummy.TrainingBrain = new TrainingDummyBrain(TrainingDummySetting.GuardAll);
        }
    }

    private void AdvanceBossPhase()
    {
        if (_simulation is null)
        {
            return;
        }

        var boss = _simulation.Actors.FirstOrDefault(a => a.IsBoss && !a.IsDead);
        boss?.ForceNextBossPhase(_simulation.CurrentTick);
    }

    private void ForceKnockdown()
    {
        if (_simulation is null)
        {
            return;
        }

        var player = _simulation.Actors.FirstOrDefault(a => a.IsPlayerControlled);
        player?.StateMachine.EnterKnockdown(_simulation.CurrentTick);
    }

    private void ExportFrameData()
    {
        try
        {
            var report = FrameDataExporter.BuildFullReport(RosterCatalog.ReferenceForms());
            const string path = "user://frame_data_report.txt";
            using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
            if (file is null)
            {
                GD.PrintErr("[FrameData] Could not open export file.");
                return;
            }

            file.StoreString(report);
            GD.Print($"[FrameData] Exported -> {ProjectSettings.GlobalizePath(path)}");
        }
        catch (System.Exception exception)
        {
            GD.PrintErr($"[FrameData] Export failed: {exception.Message}");
        }
    }

    private void AppendCommandResolution(System.Collections.Generic.List<string> lines)
    {
        var human = _simulation?.Actors.FirstOrDefault(a => a.IsPlayerControlled && a.PlayerId == 1);
        var buffer = _inputManager?.GetBufferForPlayer(1);
        if (human is null || buffer is null || buffer.Count == 0)
        {
            return;
        }

        var defs = human.CurrentForm.Moves
            .Where(m => !string.IsNullOrWhiteSpace(m.InputCommand))
            .Select(m => _commandInterpreter.Parse(
                m.Id, m.InputCommand, m.Priority, m.InputWindowFrames, m.DirectionLeniency))
            .ToList();
        if (defs.Count == 0)
        {
            return;
        }

        var candidates = _commandInterpreter.Diagnose(defs, buffer, human.FacingRight);
        var consumed = candidates.FirstOrDefault(c => c.Matched);
        var consumedText = consumed.Command is null
            ? "-"
            : $"{consumed.Command.Name}@{consumed.InputAgeFrames}f";
        lines.Add($"Cmd P1: {consumedText}");

        var rejected = new System.Collections.Generic.List<string>();
        foreach (var candidate in candidates)
        {
            if (candidate.Matched)
            {
                break;
            }

            rejected.Add(candidate.Command.Notation);
        }

        if (rejected.Count > 0)
        {
            lines.Add($"Cmd P1 rejected(>pri): {string.Join(",", rejected.Take(5))}");
        }
    }

    public override void _Process(double delta)
    {
        if (_simulation is null)
        {
            return;
        }

        CaptureNewEvents();

        var lines = new System.Collections.Generic.List<string>
        {
            $"Tick: {_simulation.CurrentTick} | Mode: {_simulation.CurrentMode} | Hit-stop: {_simulation.HitStopFramesRemaining}f | Super-pause: {_simulation.SuperPauseFramesRemaining}f",
            "Input: WASD move, E dash, Space jump, J/K/L = LP/MP/HP, U/I/O = LK/MK/HK, S + attack crouches, B block, K+I parry, Q swap",
            "Debug: F1 overlay, F2 frame bars, F3 export | 1 kill 2 skip 3 prop 4 refill 5 dummy-guard 6 boss-phase 7 knockdown",
        };

        var activePlayersCount = _simulation.Actors.Count(a => a.IsPlayerControlled);
        lines.Add($"Active Players: {activePlayersCount}");

        for (int i = 1; i <= activePlayersCount; i++)
        {
            var pBuffer = _inputManager?.GetBufferForPlayer(i);
            if (pBuffer is not null)
            {
                lines.Add($"P{i} Held: {pBuffer.CurrentHeld} | Just: {pBuffer.JustPressed}");
            }
        }

        if (Visible)
        {
            AppendCommandResolution(lines);
        }

        foreach (var actor in _simulation.Actors)
        {
            var move = actor.CurrentMove is null ? "-" : $"{actor.CurrentMove.Id}@{actor.CurrentMoveFrame}";
            var forms = string.Join(",", actor.FormArchive.ActiveLoadout);
            var guard = actor.CurrentForm.MaxGuardGauge > 0
                ? $" Guard {actor.GuardGauge}/{actor.CurrentForm.MaxGuardGauge}"
                : "";
            lines.Add($"{actor.ActorId}: {actor.State} HP {actor.Health}/{actor.CurrentForm.MaxHealth} Meter {actor.Meter}{guard} Combo {actor.ComboHitCount} Form {actor.CurrentForm.Id} Move {move} Forms [{forms}]");
            if (actor.CurrentBossPhase is not null)
            {
                lines.Add(
                    $"  boss phase: {actor.CurrentBossPhaseIndex + 1}/{actor.CurrentForm.BossPhases.Count} {actor.CurrentBossPhase.Id} visual={actor.CurrentVisualVariantId}");
            }

            if (actor.StateMachine.FlightFramesRemaining > 0
                || actor.StateMachine.FlightCooldownFramesRemaining > 0)
            {
                lines.Add(
                    $"  flight: {(actor.IsFlightActive ? "active" : "inactive")} time={actor.StateMachine.FlightFramesRemaining} cooldown={actor.StateMachine.FlightCooldownFramesRemaining} shared-lock={actor.StateMachine.FlightSharedLockoutFramesRemaining}");
            }

            if (actor.InstinctCharges > 0
                || actor.StateMachine.InstinctCooldownFramesRemaining > 0)
            {
                lines.Add(
                    $"  instinct: charges={actor.InstinctCharges} cooldown={actor.StateMachine.InstinctCooldownFramesRemaining}");
            }

            if (actor.CpuBrain is not null)
            {
                lines.Add(
                    $"  cpu: {actor.CpuBrain.CurrentIntent} target={actor.CpuBrain.TargetActorId} react={actor.CpuBrain.ReactionFramesRemaining} decide={actor.CpuBrain.DecisionFramesRemaining} ({actor.CpuBrain.LastReason})");
            }

            if (actor.ArcadeBrain is not null)
            {
                lines.Add(
                    $"  arcade ai: {actor.ArcadeBrain.Mode}/{actor.ArcadeBrain.CurrentIntent} "
                    + $"slot={actor.ArcadeBrain.FormationSlotIndex} entering={actor.ArcadeBrain.IsEnteringStage} "
                    + $"target={actor.ArcadeBrain.TargetActorId} ({actor.ArcadeBrain.LastReason})");
            }

            var visual = actor.GetNodeOrNull<CharacterVisualComponent>("CharacterVisualComponent");
            if (visual is { IsUsingFallbackSpriteSheet: true })
            {
                lines.Add($"  visual: generated fallback sprite sheet ({visual.LoadedSpriteSheetPath})");
            }

            if (actor.StateMachine.HitstunFramesRemaining > 0)
            {
                lines.Add($"  hitstun: {actor.StateMachine.HitstunFramesRemaining}");
            }

            if (actor.StateMachine.BlockstunFramesRemaining > 0)
            {
                lines.Add($"  blockstun: {actor.StateMachine.BlockstunFramesRemaining}");
            }

            if (actor.StateMachine.ParryActiveFramesRemaining > 0
                || actor.StateMachine.ParryRecoveryFramesRemaining > 0)
            {
                lines.Add(
                    $"  parry: active={actor.StateMachine.ParryActiveFramesRemaining} recovery={actor.StateMachine.ParryRecoveryFramesRemaining}");
            }

            if (actor.StateMachine.GuardBreakFramesRemaining > 0)
            {
                lines.Add($"  guard break: {actor.StateMachine.GuardBreakFramesRemaining}");
            }

            if (actor.StateMachine.FormSwapCooldownFrames > 0)
            {
                lines.Add($"  swap cd: {actor.StateMachine.FormSwapCooldownFrames}");
            }

            if (actor.StateMachine.CinematicLockFramesRemaining > 0)
            {
                lines.Add($"  cinematic lock: {actor.StateMachine.CinematicLockFramesRemaining}");
            }

            if (actor.StateMachine.DashFramesRemaining > 0
                || actor.StateMachine.JumpStartupFramesRemaining > 0
                || actor.StateMachine.LandingFramesRemaining > 0)
            {
                lines.Add(
                    $"  mobility: dash={actor.StateMachine.DashFramesRemaining} jumpStart={actor.StateMachine.JumpStartupFramesRemaining} landing={actor.StateMachine.LandingFramesRemaining}");
            }
        }

        foreach (var presentationEvent in _eventLog)
        {
            lines.Add(presentationEvent);
        }

        UpdateFrameInspector(lines);

        _label.Text = string.Join('\n', lines);
    }

    private void UpdateFrameInspector(List<string> lines)
    {
        var inspected = _simulation?.Actors.FirstOrDefault(a => a.IsPlayerControlled && a.PlayerId == 1);
        if (inspected?.CurrentMove is null)
        {
            _timelineView.SetTimeline(null, 0);
            return;
        }

        var timeline = MoveTimeline.Build(inspected.CurrentMove);
        _timelineView.SetTimeline(timeline, inspected.CurrentMoveFrame);

        if (!Visible)
        {
            return;
        }

        lines.Add(
            $"Frame data: {timeline.MoveId} S{timeline.StartupFrames}/A{timeline.ActiveFrames}/R{timeline.RecoveryFrames} "
            + $"frame {inspected.CurrentMoveFrame}/{timeline.TotalFrames}");
        lines.Add($"  phase : {timeline.ToPhaseStrip()}");
        lines.Add($"  here  : {timeline.ToCursorStrip(inspected.CurrentMoveFrame)}");
        foreach (var row in timeline.ToWindowRows())
        {
            lines.Add($"  {row}");
        }
    }

    private void CaptureNewEvents()
    {
        if (_simulation is null || _simulation.CurrentTick == _lastLoggedTick)
        {
            return;
        }

        _lastLoggedTick = _simulation.CurrentTick;
        foreach (var presentationEvent in _simulation.LastPresentationEvents)
        {
            var target = string.IsNullOrWhiteSpace(presentationEvent.TargetActorId)
                ? ""
                : $" -> {presentationEvent.TargetActorId}";
            var payload = string.IsNullOrWhiteSpace(presentationEvent.Payload)
                ? ""
                : $" {presentationEvent.Payload}";
            _eventLog.Enqueue($"event: {presentationEvent.Type} {presentationEvent.SourceActorId}{target}{payload}");
        }

        while (_eventLog.Count > 8)
        {
            _eventLog.Dequeue();
        }
    }
}
