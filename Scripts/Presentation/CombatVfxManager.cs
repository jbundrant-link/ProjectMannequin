using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.Presentation;

public enum CombatImpactStyle
{
    Strike,
    Guard,
    Parry,
    Dust,
    Armor,
    Knockout,
    Explosion,
}

/// <summary>
/// Presentation-only, pooled combat feedback. It consumes transient simulation
/// events and never changes combat state, keeping rollback/determinism intact.
/// </summary>
public partial class CombatVfxManager : CanvasLayer
{
    private const int PoolSize = 36;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";
    [Export] public string StrikeBurstTexturePath { get; set; } =
        "res://Assets/Vfx/Combat/project_mannequin_strike_burst_style_v1.png";

    private readonly List<CombatImpactBurst> _bursts = new();
    private GameSimulation? _simulation;
    private Texture2D? _strikeBurstTexture;
    private long _presentationEventCursor;
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private int _recycleCursor;

    public override void _Ready()
    {
        Layer = 0;
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        if (ResourceLoader.Exists(StrikeBurstTexturePath))
        {
            _strikeBurstTexture = GD.Load<Texture2D>(StrikeBurstTexturePath);
        }
        else
        {
            GD.PushWarning(
                $"Authored strike VFX not found: {StrikeBurstTexturePath}. "
                + "Using the pooled procedural fallback.");
        }

        for (var index = 0; index < PoolSize; index++)
        {
            var burst = new CombatImpactBurst
            {
                Name = $"ImpactBurst{index:00}",
                AuthoredStrikeTexture = _strikeBurstTexture,
            };
            AddChild(burst);
            _bursts.Add(burst);
        }
    }

    public override void _Process(double delta)
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
            HandleEvent(presentationEvent);
        }
    }

    private void HandleEvent(CombatPresentationEvent presentationEvent)
    {
        switch (presentationEvent.Type)
        {
            case CombatPresentationEventType.HitConnected:
                SpawnAtActor(
                    presentationEvent.TargetActorId,
                    CombatImpactStyle.Strike,
                    new Color(1.0f, 0.78f, 0.20f),
                    ResolveStrength(presentationEvent.Payload, 0.86f));
                break;
            case CombatPresentationEventType.LauncherHit:
                SpawnAtActor(
                    presentationEvent.TargetActorId,
                    CombatImpactStyle.Strike,
                    new Color(1.0f, 0.46f, 0.12f),
                    ResolveStrength(presentationEvent.Payload, 1.18f));
                break;
            case CombatPresentationEventType.CounterHit:
                SpawnAtActor(
                    presentationEvent.TargetActorId,
                    CombatImpactStyle.Strike,
                    new Color(1.0f, 0.30f, 0.14f),
                    ResolveStrength(presentationEvent.Payload, 1.22f));
                break;
            case CombatPresentationEventType.PunishCounter:
                SpawnAtActor(
                    presentationEvent.TargetActorId,
                    CombatImpactStyle.Strike,
                    new Color(1.0f, 0.12f, 0.08f),
                    ResolveStrength(presentationEvent.Payload, 1.42f));
                break;
            case CombatPresentationEventType.Blocked:
                SpawnAtActor(
                    presentationEvent.TargetActorId,
                    CombatImpactStyle.Guard,
                    new Color(0.36f, 0.78f, 1.0f),
                    0.82f);
                break;
            case CombatPresentationEventType.Parried:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Parry,
                    new Color(0.48f, 1.0f, 0.96f),
                    presentationEvent.Payload == "perfect" ? 1.45f : 1.1f);
                break;
            case CombatPresentationEventType.GuardBroken:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Guard,
                    new Color(1.0f, 0.72f, 0.16f),
                    1.42f);
                break;
            case CombatPresentationEventType.ArmorAbsorbed:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Armor,
                    new Color(0.72f, 0.48f, 1.0f),
                    1.08f);
                break;
            case CombatPresentationEventType.WallBounce:
            case CombatPresentationEventType.GroundBounce:
            case CombatPresentationEventType.Landed:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Dust,
                    new Color(0.78f, 0.76f, 0.68f),
                    presentationEvent.Type == CombatPresentationEventType.Landed ? 0.62f : 1.0f,
                    heightOffset: 0.15f);
                break;
            case CombatPresentationEventType.ActorDefeated:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Knockout,
                    new Color(1.0f, 0.92f, 0.54f),
                    1.55f);
                break;
            case CombatPresentationEventType.PropExploded:
                SpawnAtActor(
                    presentationEvent.SourceActorId,
                    CombatImpactStyle.Explosion,
                    new Color(1.0f, 0.34f, 0.08f),
                    1.72f,
                    heightOffset: 0.65f);
                break;
        }
    }

    private void SpawnAtActor(
        string actorId,
        CombatImpactStyle style,
        Color color,
        float strength,
        float heightOffset = 1.2f)
    {
        if (_simulation is null)
        {
            return;
        }

        var actor = _simulation.Actors.FirstOrDefault(candidate => candidate.ActorId == actorId);
        if (actor is null)
        {
            return;
        }

        var camera = GetViewport().GetCamera3D();
        var screenPosition = camera is null
            ? GetViewport().GetVisibleRect().Size * 0.5f
            : camera.UnprojectPosition(actor.GlobalPosition + new Vector3(0.0f, heightOffset, 0.0f));
        AcquireBurst().Activate(screenPosition, style, color, Mathf.Clamp(strength, 0.45f, 1.8f));
    }

    private CombatImpactBurst AcquireBurst()
    {
        foreach (var burst in _bursts)
        {
            if (!burst.IsActive)
            {
                return burst;
            }
        }

        var recycled = _bursts[_recycleCursor++ % _bursts.Count];
        return recycled;
    }

    private static float ResolveStrength(string payload, float baseStrength)
    {
        var separatorIndex = payload.LastIndexOf('|');
        if (separatorIndex < 0
            || separatorIndex >= payload.Length - 1
            || !int.TryParse(payload[(separatorIndex + 1)..], out var damage))
        {
            return baseStrength;
        }

        return baseStrength + Mathf.Clamp(damage / 2200.0f, 0.0f, 0.55f);
    }
}

/// <summary>A single reusable radial impact/dust drawing.</summary>
public partial class CombatImpactBurst : Node2D
{
    private CombatImpactStyle _style;
    private Color _color;
    private float _strength;
    private float _elapsed;
    private float _duration;

    public Texture2D? AuthoredStrikeTexture { get; set; }
    public bool IsActive { get; private set; }

    public override void _Ready()
    {
        Visible = false;
        SetProcess(false);
        ZIndex = 20;
    }

    public void Activate(
        Vector2 position,
        CombatImpactStyle style,
        Color color,
        float strength)
    {
        Position = position;
        _style = style;
        _color = color;
        _strength = strength;
        _elapsed = 0.0f;
        _duration = style switch
        {
            CombatImpactStyle.Parry => 0.28f,
            CombatImpactStyle.Knockout => 0.36f,
            CombatImpactStyle.Explosion => 0.42f,
            CombatImpactStyle.Dust => 0.30f,
            _ => 0.20f,
        };
        Rotation = style == CombatImpactStyle.Dust ? 0.0f : (position.X % 17.0f) * 0.012f;
        IsActive = true;
        Visible = true;
        SetProcess(true);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (!IsActive)
        {
            return;
        }

        _elapsed += (float)delta;
        if (_elapsed >= _duration)
        {
            IsActive = false;
            Visible = false;
            SetProcess(false);
            return;
        }

        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!IsActive)
        {
            return;
        }

        var progress = Mathf.Clamp(_elapsed / _duration, 0.0f, 1.0f);
        var ease = 1.0f - Mathf.Pow(1.0f - progress, 3.0f);
        var alpha = 1.0f - progress;
        var color = new Color(_color.R, _color.G, _color.B, alpha);
        var white = new Color(1.0f, 1.0f, 1.0f, alpha * 0.92f);
        var radius = Mathf.Lerp(8.0f, 34.0f, ease) * _strength;

        if (_style == CombatImpactStyle.Strike && AuthoredStrikeTexture is not null)
        {
            var authoredSize = Mathf.Clamp(
                Mathf.Lerp(120.0f, 220.0f, ease) * _strength,
                90.0f,
                280.0f);
            var authoredRect = new Rect2(
                new Vector2(-authoredSize * 0.5f, -authoredSize * 0.5f),
                new Vector2(authoredSize, authoredSize));
            DrawTextureRect(
                AuthoredStrikeTexture,
                authoredRect,
                tile: false,
                modulate: new Color(1.0f, 1.0f, 1.0f, alpha));
            return;
        }

        if (_style == CombatImpactStyle.Dust)
        {
            DrawArc(Vector2.Zero, radius, Mathf.Pi, Mathf.Tau, 18, color, 4.0f * _strength, true);
            for (var index = 0; index < 7; index++)
            {
                var horizontal = (index - 3) * radius * 0.24f;
                var height = 5.0f + (index % 3) * 4.0f;
                DrawCircle(new Vector2(horizontal, -height * ease), 4.0f * _strength, color);
            }
            return;
        }

        var rayCount = _style switch
        {
            CombatImpactStyle.Parry => 16,
            CombatImpactStyle.Knockout => 20,
            CombatImpactStyle.Explosion => 24,
            CombatImpactStyle.Armor => 12,
            _ => 10,
        };
        for (var index = 0; index < rayCount; index++)
        {
            var angle = Mathf.Tau * index / rayCount;
            var direction = Vector2.FromAngle(angle);
            var inner = direction * radius * 0.42f;
            var outerLength = radius * (0.82f + (index % 3) * 0.16f);
            DrawLine(inner, direction * outerLength, index % 2 == 0 ? white : color, 2.5f * _strength, true);
        }

        DrawCircle(Vector2.Zero, Mathf.Max(2.0f, radius * 0.18f), white);
        DrawArc(Vector2.Zero, radius * 0.48f, 0.0f, Mathf.Tau, 24, color, 3.0f * _strength, true);
        if (_style is CombatImpactStyle.Guard or CombatImpactStyle.Armor or CombatImpactStyle.Parry)
        {
            DrawArc(Vector2.Zero, radius * 0.76f, 0.0f, Mathf.Tau, 24, white, 2.0f * _strength, true);
        }
    }
}
