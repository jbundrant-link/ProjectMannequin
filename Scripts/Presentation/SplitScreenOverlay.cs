using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Optional split-screen presentation for Isolated-Duel encounters with two or
/// more players. It overlays one <see cref="SubViewport"/> per player (all
/// sharing the main World3D), each with a camera framing that player's own
/// sub-arena, so the simultaneous 1v1 duels are legible.
///
/// Purely presentation: it only becomes visible during an isolated duel with
/// multiple players and tears its viewports down otherwise, so ordinary
/// single-screen play is completely untouched.
/// </summary>
public partial class SplitScreenOverlay : CanvasLayer
{
    private GameSimulation? _simulation;
    private HBoxContainer _split = null!;
    private readonly List<Camera3D> _cameras = new();
    private readonly List<SubViewportContainer> _containers = new();
    private bool _snap;
    private bool _introFocusActive;
    private float _introFocusElapsed;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private const float IntroBossFocusSeconds = 1.15f;

    public void Configure(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public override void _Ready()
    {
        Layer = 0; // Above the base 3D render, below the HUD (layer 1).
        Visible = false;

        var root = new Control();
        root.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        _split = new HBoxContainer();
        _split.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _split.AddThemeConstantOverride("separation", 4);
        root.AddChild(_split);
    }

    public override void _Process(double delta)
    {
        var director = _simulation?.EncounterDirector;
        var players = _simulation?.Actors
            .Where(actor => actor.IsPlayerControlled)
            .OrderBy(actor => actor.PlayerId)
            .ToList();

        var active = director is not null
            && players is { Count: >= 2 }
            && director.IsBossDuelActive
            && director.CurrentEncounter.BossType == BossEncounterType.IsolatedDuel;

        if (!active)
        {
            _introFocusActive = false;
            _introFocusElapsed = 0.0f;
            if (Visible)
            {
                Visible = false;
            }

            if (_containers.Count > 0)
            {
                ClearViews();
            }

            return;
        }

        if (_cameras.Count != players!.Count)
        {
            BuildViews(players.Count);
        }

        Visible = true;
        CaptureIntroEvents();
        if (_introFocusActive)
        {
            _introFocusElapsed += (float)delta;
        }

        var followWeight = 1.0f - Mathf.Exp(-8.0f * (float)delta);
        for (var i = 0; i < players.Count && i < _cameras.Count; i++)
        {
            var player = players[i];
            var rival = _simulation!.Actors.FirstOrDefault(actor =>
                !actor.IsPlayerControlled
                && !actor.IsDead
                && actor.LockedTargetPlayerId == player.PlayerId);

            var playerX = player.SimPosition.X;
            var introFocusActor = _introFocusActive
                ? _introFocusElapsed < IntroBossFocusSeconds
                    ? rival
                    : player
                : null;
            var centerX = introFocusActor?.SimPosition.X
                ?? (rival is not null ? (playerX + rival.SimPosition.X) * 0.5f : playerX);
            var centerZ = introFocusActor?.SimPosition.Z ?? 0.0f;
            var focusAirHeight = Mathf.Clamp(introFocusActor?.SimPosition.Y ?? 0.0f, 0.0f, 3.2f);
            var spread = rival is not null ? Mathf.Abs(playerX - rival.SimPosition.X) : 0.0f;
            var targetSize = introFocusActor is not null
                ? 5.8f
                : Mathf.Clamp(6.5f + spread * 0.55f, 7.0f, 13.0f);
            var targetPosition = introFocusActor is not null
                ? new Vector3(centerX, 5.7f + focusAirHeight * 0.25f, 10.2f)
                : new Vector3(centerX, 6.5f, 11.5f);

            var camera = _cameras[i];
            if (_snap)
            {
                camera.GlobalPosition = targetPosition;
                camera.Size = targetSize;
            }
            else
            {
                camera.GlobalPosition = camera.GlobalPosition.Lerp(targetPosition, followWeight);
                camera.Size = Mathf.Lerp(camera.Size, targetSize, followWeight);
            }

            camera.LookAt(new Vector3(centerX, 1.15f + focusAirHeight * 0.85f, centerZ), Vector3.Up);
        }

        _snap = false;
    }

    private void CaptureIntroEvents()
    {
        if (_simulation is null)
        {
            return;
        }

        _presentationEventBuffer.Clear();
        _presentationEventCursor = _simulation.CopyPresentationEventsSince(
            _presentationEventCursor,
            _presentationEventBuffer);
        foreach (var presentationEvent in _presentationEventBuffer)
        {
            switch (presentationEvent.Type)
            {
                case CombatPresentationEventType.BossIntroStarted:
                    _introFocusActive = true;
                    _introFocusElapsed = 0.0f;
                    break;
                case CombatPresentationEventType.BossIntroReady:
                case CombatPresentationEventType.BossIntroFight:
                    _introFocusActive = false;
                    _introFocusElapsed = 0.0f;
                    break;
            }
        }
    }

    private void BuildViews(int count)
    {
        ClearViews();

        var world = GetTree().Root.World3D;
        for (var i = 0; i < count; i++)
        {
            var container = new SubViewportContainer
            {
                Stretch = true,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore,
            };
            var viewport = new SubViewport
            {
                World3D = world,
                RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
                HandleInputLocally = false,
            };
            var camera = new Camera3D
            {
                Projection = Camera3D.ProjectionType.Orthogonal,
                Size = 9.0f,
                Current = true,
            };
            viewport.AddChild(camera);
            container.AddChild(viewport);
            _split.AddChild(container);
            _containers.Add(container);
            _cameras.Add(camera);
        }

        _snap = true;
    }

    private void ClearViews()
    {
        foreach (var container in _containers)
        {
            container.QueueFree();
        }

        _containers.Clear();
        _cameras.Clear();
    }
}
