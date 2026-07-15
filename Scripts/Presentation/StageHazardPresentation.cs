using System.Collections.Generic;
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
    private GameSimulation? _simulation;
    private string _encounterId = "";

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
            _encounterId = encounter.Id;
            Rebuild(encounter);
        }

        var hazardsVisible = director.State == ArcadeStageState.EncounterActive;
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
                continue;
            }

            var frame = StageHazardRuntime.Resolve(zone, director.StateElapsedFrames);
            if (frame.Phase is StageHazardPhase.Dormant or StageHazardPhase.Cooldown)
            {
                visual.Mesh.Visible = false;
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
    }

    private void Rebuild(StageEncounterData encounter)
    {
        foreach (var visual in _visuals.Values)
        {
            visual.Mesh.QueueFree();
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
            _visuals[zone.Id] = new HazardVisual(mesh, plane, material);
        }
    }

    private void HideAll()
    {
        foreach (var visual in _visuals.Values)
        {
            visual.Mesh.Visible = false;
        }
    }

    private sealed record HazardVisual(
        MeshInstance3D Mesh,
        PlaneMesh? Plane,
        StandardMaterial3D Material);
}
