using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public partial class StageDebugDrawer : Node3D
{
    private GameSimulation? _simulation;
    private MeshInstance3D _meshInstance = null!;
    private ImmediateMesh _immediateMesh = null!;
    private StandardMaterial3D _material = null!;
    private bool _enabled;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        
        _immediateMesh = new ImmediateMesh();
        _meshInstance = new MeshInstance3D
        {
            Mesh = _immediateMesh,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        
        _material = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            VertexColorUseAsAlbedo = true,
            NoDepthTest = true,
        };
        _meshInstance.MaterialOverride = _material;
        AddChild(_meshInstance);
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F3 })
        {
            _enabled = !_enabled;
            Visible = _enabled;
        }
    }

    public override void _Process(double delta)
    {
        if (!_enabled || _simulation?.EncounterDirector?.Mission is null) return;
        
        var director = _simulation.EncounterDirector;
        var mission = director.Mission;
        
        _immediateMesh.ClearSurfaces();
        _immediateMesh.SurfaceBegin(Mesh.PrimitiveType.Lines, _material);
        
        // Draw Lane Bounds
        DrawLine(mission.StageMinX, mission.LaneMinZ, mission.StageMaxX, mission.LaneMinZ, Colors.Yellow);
        DrawLine(mission.StageMinX, mission.LaneMaxZ, mission.StageMaxX, mission.LaneMaxZ, Colors.Yellow);
        
        foreach (var encounter in mission.Encounters)
        {
            var laneMin = StageLaneRuntime.TargetMinZ(mission.LaneMinZ, encounter);
            var laneMax = StageLaneRuntime.TargetMaxZ(mission.LaneMaxZ, encounter);
            // Draw Trigger
            DrawLine(encounter.TriggerX, laneMin - 1f, encounter.TriggerX, laneMax + 1f, Colors.Magenta);
            
            // Draw Room Bounds
            DrawBox(encounter.ArenaMinX, encounter.ArenaMaxX, laneMin, laneMax, Colors.Cyan);
            
            // Draw Camera Lock
            DrawCrosshair(encounter.CameraLockX, 0f, Colors.Orange);
            
            // Draw Hazard Zones
            foreach (var zone in encounter.HazardZones)
            {
                DrawBox(zone.MinX, zone.MaxX, zone.MinZ, zone.MaxZ, Colors.Red);
            }
        }

        DrawBox(
            director.CurrentEncounter.ArenaMinX,
            director.CurrentEncounter.ArenaMaxX,
            director.CurrentLaneMinZ,
            director.CurrentLaneMaxZ,
            Colors.LimeGreen);
        
        _immediateMesh.SurfaceEnd();
    }
    
    private void DrawLine(float x1, float z1, float x2, float z2, Color color)
    {
        _immediateMesh.SurfaceSetColor(color);
        _immediateMesh.SurfaceAddVertex(new Vector3(x1, 0.05f, z1));
        _immediateMesh.SurfaceSetColor(color);
        _immediateMesh.SurfaceAddVertex(new Vector3(x2, 0.05f, z2));
    }
    
    private void DrawBox(float minX, float maxX, float minZ, float maxZ, Color color)
    {
        DrawLine(minX, minZ, maxX, minZ, color);
        DrawLine(maxX, minZ, maxX, maxZ, color);
        DrawLine(maxX, maxZ, minX, maxZ, color);
        DrawLine(minX, maxZ, minX, minZ, color);
    }
    
    private void DrawCrosshair(float x, float z, Color color)
    {
        DrawLine(x - 1f, z, x + 1f, z, color);
        DrawLine(x, z - 1f, x, z + 1f, color);
    }
}
