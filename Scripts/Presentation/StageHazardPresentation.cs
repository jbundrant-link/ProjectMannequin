using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Stage;

namespace ProjectMannequin.Presentation;

/// <summary>
/// World-space telegraphs for authored stage hazards. Simulation owns timing and
/// damage; this component mirrors pure StageHazardRuntime frames for readability.
/// </summary>
public partial class StageHazardPresentation : Node3D
{
    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";

    private readonly Dictionary<string, HazardVisual> _visuals = new();
    private readonly Dictionary<string, AftermathVisual> _aftermathVisuals = new();
    private readonly List<CombatPresentationEvent> _presentationEventBuffer = new();
    private readonly HashSet<string> _aftermathCaptureHiddenActorIds = new();
    private readonly HashSet<string> _aftermathCaptureHiddenSourceIds = new();
    private GameSimulation? _simulation;
    private string _encounterId = "";
    private long _presentationEventCursor;
    private int _aftermathCaptureStableFrames;
    private bool _aftermathCaptureActorsHidden;
    private bool _aftermathCaptureSaved;

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
    }

    public override void _Process(double delta)
    {
        var director = _simulation?.EncounterDirector;
        if (director is null)
        {
            HideAll();
            return;
        }

        var encounter = director.CurrentEncounter;
        if (_encounterId != encounter.Id)
        {
            ClearAftermath();
            _encounterId = encounter.Id;
            Rebuild(encounter);
        }

        CaptureAftermathEvents(encounter);

        var hazardsVisible = director.State == ArcadeStageState.EncounterActive
            && !_aftermathCaptureActorsHidden;
        foreach (var zone in encounter.HazardZones)
        {
            if (!_visuals.TryGetValue(zone.Id, out var visual))
            {
                continue;
            }

            if (!hazardsVisible
                || (encounter.Kind == StageEncounterKind.Boss && !zone.ActiveDuringBoss))
            {
                visual.Mesh.Visible = false;
                if (visual.FieldMesh is not null)
                {
                    visual.FieldMesh.Visible = false;
                }
                if (visual.Sprite is not null)
                {
                    visual.Sprite.Visible = false;
                }
                continue;
            }

            var frame = StageHazardRuntime.Resolve(zone, director.StateElapsedFrames);
            if (frame.Phase is StageHazardPhase.Dormant or StageHazardPhase.Cooldown)
            {
                visual.Mesh.Visible = false;
                if (visual.FieldMesh is not null)
                {
                    visual.FieldMesh.Visible = false;
                }
                if (visual.Sprite is not null)
                {
                    visual.Sprite.Visible = false;
                }
                continue;
            }

            visual.Mesh.Visible = true;
            var width = Mathf.Max(0.05f, frame.MaxX - frame.MinX);
            var depth = Mathf.Max(0.05f, frame.MaxZ - frame.MinZ);
            if (visual.Plane is not null)
            {
                visual.Plane.Size = new Vector2(width, depth);
                visual.Mesh.Scale = Vector3.One;
            }
            else
            {
                // Cylinder has a unit diameter; non-uniform X/Z scale projects
                // an authored circular/elliptical landing marker on the floor.
                visual.Mesh.Scale = new Vector3(width, 1.0f, depth);
            }
            visual.Mesh.Position = new Vector3(
                (frame.MinX + frame.MaxX) * 0.5f,
                0.045f,
                (frame.MinZ + frame.MaxZ) * 0.5f);
            var pulse = 0.5f + Mathf.Sin(Time.GetTicksMsec() * 0.014f) * 0.5f;
            if (visual.FieldMesh is not null
                && visual.FieldPlane is not null
                && visual.FieldMaterial is not null)
            {
                visual.FieldMesh.Visible = true;
                visual.FieldPlane.Size = new Vector2(width, depth);
                visual.FieldMesh.Position = new Vector3(
                    (frame.MinX + frame.MaxX) * 0.5f,
                    0.055f,
                    (frame.MinZ + frame.MaxZ) * 0.5f);
                visual.FieldMaterial.AlbedoColor = frame.IsWarning
                    ? new Color(1.0f, 1.0f, 1.0f, 0.24f + pulse * 0.16f)
                    : new Color(1.0f, 1.0f, 1.0f, 0.62f + pulse * 0.18f);
                visual.FieldMaterial.EmissionEnergyMultiplier = frame.IsWarning
                    ? 1.25f
                    : 2.1f;
            }
            if (visual.Sprite is not null)
            {
                visual.Sprite.Visible = true;
                var travelHeight = zone.Behavior == StageHazardBehavior.FallingStrike
                    && frame.IsWarning
                        ? zone.SpriteTravelHeight * (1.0f - frame.Progress)
                        : 0.0f;
                visual.Sprite.Position = new Vector3(
                    Mathf.Lerp(frame.MinX, frame.MaxX, zone.SpriteAnchorX),
                    zone.SpritePixelSize * zone.SpriteGroundOffsetPixels + travelHeight,
                    Mathf.Lerp(frame.MinZ, frame.MaxZ, zone.SpriteAnchorZ) - 0.08f);
                visual.Sprite.Modulate = frame.IsWarning
                    ? zone.Behavior == StageHazardBehavior.FallingStrike
                        ? new Color(1.0f, 0.84f, 0.72f, 0.68f + pulse * 0.20f)
                        : zone.Behavior == StageHazardBehavior.LinearSweep
                            ? new Color(1.0f, 1.0f, 1.0f, 0.62f + pulse * 0.18f)
                            : new Color(1.0f, 0.78f, 0.48f, 0.34f + pulse * 0.18f)
                    : Colors.White;
            }
            var color = zone.Behavior == StageHazardBehavior.FallingStrike
                ? frame.IsWarning
                    ? new Color(0.88f, 0.34f, 1.0f, 0.18f + pulse * 0.24f)
                    : new Color(1.0f, 0.60f, 0.10f, 0.40f + pulse * 0.20f)
                : frame.IsWarning
                    ? new Color(1.0f, 0.72f, 0.12f, 0.18f + pulse * 0.22f)
                    : new Color(1.0f, 0.18f, 0.08f, 0.32f + pulse * 0.18f);
            visual.Material.AlbedoColor = color;
            visual.Material.Emission = new Color(color.R, color.G, color.B);
            visual.Material.EmissionEnergyMultiplier = frame.IsWarning ? 1.15f : 1.8f;
        }

        CaptureAftermathFrameIfRequested();
    }

    private void CaptureAftermathEvents(StageEncounterData encounter)
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
            if (presentationEvent.Type == CombatPresentationEventType.HazardZoneImpacted
                && presentationEvent.SourceActorId == encounter.Id)
            {
                var zone = encounter.HazardZones.FirstOrDefault(candidate =>
                    candidate.Id == presentationEvent.Payload);
                if (zone?.AftermathVisual is not null)
                {
                    SpawnAftermath(
                        zone.Id,
                        zone.AftermathVisual,
                        new Vector3(
                            (zone.MinX + zone.MaxX) * 0.5f,
                            0.0f,
                            (zone.MinZ + zone.MaxZ) * 0.5f));
                }
            }
            else if (presentationEvent.Type == CombatPresentationEventType.PropExploded)
            {
                var prop = encounter.Props.FirstOrDefault(candidate =>
                    candidate.Id == presentationEvent.Payload);
                if (prop?.AftermathVisual is null)
                {
                    continue;
                }

                var actor = _simulation.Actors.FirstOrDefault(candidate =>
                    candidate.ActorId == presentationEvent.SourceActorId);
                var position = actor?.SimPosition
                    ?? new Vector3(prop.PositionX, 0.0f, prop.PositionZ);
                SpawnAftermath(prop.Id, prop.AftermathVisual, position);
            }
        }
    }

    private void SpawnAftermath(
        string sourceId,
        StageAftermathVisualData visual,
        Vector3 position)
    {
        if (_aftermathVisuals.Remove(sourceId, out var existing))
        {
            existing.Root.QueueFree();
        }

        var root = new Node3D
        {
            Name = $"Aftermath_{sourceId}",
            Position = new Vector3(position.X, 0.0f, position.Z),
        };
        AddChild(root);

        var decalTexture = GD.Load<Texture2D>(visual.DecalTexturePath);
        var decalMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoTexture = decalTexture,
            AlbedoColor = new Color(1.0f, 1.0f, 1.0f, visual.DecalOpacity),
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
        };
        var decal = new MeshInstance3D
        {
            Name = "Decal",
            Mesh = new PlaneMesh
            {
                Size = new Vector2(visual.DecalSizeX, visual.DecalSizeZ),
                Material = decalMaterial,
            },
            Position = new Vector3(0.0f, 0.028f, 0.0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        };
        root.AddChild(decal);

        var fragments = new Sprite3D
        {
            Name = "Fragments",
            Texture = GD.Load<Texture2D>(visual.FragmentSpritePath),
            PixelSize = visual.FragmentPixelSize,
            Position = new Vector3(
                visual.FragmentOffsetX,
                visual.FragmentPixelSize * visual.FragmentGroundOffsetPixels,
                visual.FragmentOffsetZ - 0.08f),
            FlipH = visual.FragmentFlipH,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            Shaded = false,
            AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            DoubleSided = true,
        };
        root.AddChild(fragments);
        _aftermathVisuals[sourceId] = new AftermathVisual(root);
    }

    private void CaptureAftermathFrameIfRequested()
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_AFTERMATH_CAPTURE");
        if (_aftermathCaptureSaved || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var sourceId = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_LADDER_AFTERMATH_SOURCE_ID");
        sourceId = string.IsNullOrWhiteSpace(sourceId)
            ? "repository_volatile_canister"
            : sourceId;
        if (!_aftermathVisuals.ContainsKey(sourceId))
        {
            _aftermathCaptureStableFrames = 0;
            return;
        }

        _aftermathCaptureStableFrames++;
        var delayText = OS.GetEnvironment(
            "PROJECT_MANNEQUIN_LADDER_AFTERMATH_CAPTURE_DELAY_FRAMES");
        var delay = int.TryParse(delayText, out var authoredDelay)
            ? Mathf.Max(1, authoredDelay)
            : 45;
        if (_aftermathCaptureStableFrames < delay)
        {
            return;
        }

        if (!_aftermathCaptureActorsHidden
            && OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_CLEAR_STAGE_CAPTURE") == "1")
        {
            foreach (var actor in _simulation!.Actors.Where(actor => actor.Visible))
            {
                _aftermathCaptureHiddenActorIds.Add(actor.ActorId);
                actor.Visible = false;
            }
            foreach (var pair in _aftermathVisuals.Where(pair =>
                         pair.Key != sourceId && pair.Value.Root.Visible))
            {
                pair.Value.Root.Visible = false;
                _aftermathCaptureHiddenSourceIds.Add(pair.Key);
            }
            HideAll();
            _aftermathCaptureActorsHidden = true;
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = GetViewport().GetTexture().GetImage().SavePng(path);
        RestoreAftermathCaptureActors();
        if (error != Error.Ok)
        {
            GD.PushError($"[StageHazardPresentation] Could not save aftermath capture '{path}' ({error}).");
            return;
        }

        _aftermathCaptureSaved = true;
        GD.Print($"[StageHazardPresentation] Aftermath capture saved: {path}");
    }

    private void RestoreAftermathCaptureActors()
    {
        if (_simulation is not null)
        {
            foreach (var actor in _simulation.Actors.Where(actor =>
                         _aftermathCaptureHiddenActorIds.Contains(actor.ActorId)
                         && !actor.IsDead))
            {
                actor.Visible = true;
            }
        }
        _aftermathCaptureHiddenActorIds.Clear();
        foreach (var sourceId in _aftermathCaptureHiddenSourceIds)
        {
            if (_aftermathVisuals.TryGetValue(sourceId, out var visual))
            {
                visual.Root.Visible = true;
            }
        }
        _aftermathCaptureHiddenSourceIds.Clear();
        _aftermathCaptureActorsHidden = false;
    }

    private void ClearAftermath()
    {
        RestoreAftermathCaptureActors();
        foreach (var visual in _aftermathVisuals.Values)
        {
            visual.Root.QueueFree();
        }
        _aftermathVisuals.Clear();
        _aftermathCaptureStableFrames = 0;
    }

    private void Rebuild(StageEncounterData encounter)
    {
        foreach (var visual in _visuals.Values)
        {
            visual.Mesh.QueueFree();
            visual.FieldMesh?.QueueFree();
            visual.Sprite?.QueueFree();
        }
        _visuals.Clear();

        foreach (var zone in encounter.HazardZones)
        {
            PlaneMesh? plane = null;
            PrimitiveMesh shape;
            if (zone.Behavior == StageHazardBehavior.FallingStrike)
            {
                shape = new CylinderMesh
                {
                    TopRadius = 0.5f,
                    BottomRadius = 0.5f,
                    Height = 0.025f,
                    RadialSegments = 32,
                };
            }
            else
            {
                plane = new PlaneMesh
                {
                    Size = new Vector2(
                        Mathf.Max(0.05f, zone.MaxX - zone.MinX),
                        Mathf.Max(0.05f, zone.MaxZ - zone.MinZ)),
                };
                shape = plane;
            }
            var material = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                AlbedoColor = new Color(1.0f, 0.72f, 0.12f, 0.25f),
                EmissionEnabled = true,
                Emission = new Color(1.0f, 0.55f, 0.08f),
                EmissionEnergyMultiplier = 1.2f,
            };
            shape.Material = material;
            var mesh = new MeshInstance3D
            {
                Name = $"HazardTelegraph_{zone.Id}",
                Mesh = shape,
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                Visible = false,
            };
            AddChild(mesh);
            MeshInstance3D? fieldMesh = null;
            PlaneMesh? fieldPlane = null;
            StandardMaterial3D? fieldMaterial = null;
            if (plane is not null
                && !string.IsNullOrWhiteSpace(zone.FieldTexturePath)
                && ResourceLoader.Exists(zone.FieldTexturePath))
            {
                var fieldTexture = GD.Load<Texture2D>(zone.FieldTexturePath);
                fieldMaterial = new StandardMaterial3D
                {
                    ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                    Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                    AlbedoTexture = fieldTexture,
                    EmissionEnabled = true,
                    Emission = Colors.White,
                    EmissionTexture = fieldTexture,
                    EmissionEnergyMultiplier = 1.25f,
                    Uv1Scale = zone.FieldFlipH
                        ? new Vector3(-1.0f, 1.0f, 1.0f)
                        : Vector3.One,
                    Uv1Offset = zone.FieldFlipH
                        ? new Vector3(1.0f, 0.0f, 0.0f)
                        : Vector3.Zero,
                };
                fieldPlane = new PlaneMesh
                {
                    Size = plane.Size,
                    Material = fieldMaterial,
                };
                fieldMesh = new MeshInstance3D
                {
                    Name = $"HazardField_{zone.Id}",
                    Mesh = fieldPlane,
                    CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
                    Visible = false,
                };
                AddChild(fieldMesh);
            }
            Sprite3D? sprite = null;
            if (!string.IsNullOrWhiteSpace(zone.SpritePath)
                && ResourceLoader.Exists(zone.SpritePath))
            {
                sprite = new Sprite3D
                {
                    Name = $"HazardSprite_{zone.Id}",
                    Texture = GD.Load<Texture2D>(zone.SpritePath),
                    PixelSize = zone.SpritePixelSize,
                    FlipH = zone.SpriteFlipH,
                    TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
                    Shaded = false,
                    AlphaCut = SpriteBase3D.AlphaCutMode.OpaquePrepass,
                    Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
                    DoubleSided = true,
                    Visible = false,
                };
                AddChild(sprite);
            }
            _visuals[zone.Id] = new HazardVisual(
                mesh,
                plane,
                material,
                fieldMesh,
                fieldPlane,
                fieldMaterial,
                sprite);
        }
    }

    private void HideAll()
    {
        foreach (var visual in _visuals.Values)
        {
            visual.Mesh.Visible = false;
            if (visual.FieldMesh is not null)
            {
                visual.FieldMesh.Visible = false;
            }
            if (visual.Sprite is not null)
            {
                visual.Sprite.Visible = false;
            }
        }
    }

    private sealed record HazardVisual(
        MeshInstance3D Mesh,
        PlaneMesh? Plane,
        StandardMaterial3D Material,
        MeshInstance3D? FieldMesh,
        PlaneMesh? FieldPlane,
        StandardMaterial3D? FieldMaterial,
        Sprite3D? Sprite);

    private sealed record AftermathVisual(Node3D Root);
}
