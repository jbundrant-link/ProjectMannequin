using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

public partial class CombatBoxViewer : Node3D
{
    private readonly List<MeshInstance3D> _boxPool = new();
    private readonly Dictionary<CombatBoxType, StandardMaterial3D> _materials = new();
    private GameSimulation? _simulation;
    private bool _enabled;

    [Export] public NodePath SimulationPath { get; set; } = "../GameSimulation";

    public override void _Ready()
    {
        _simulation = GetNodeOrNull<GameSimulation>(SimulationPath);
        _materials[CombatBoxType.Hitbox] = MakeMaterial(new Color(1.0f, 0.08f, 0.08f, 0.38f));
        _materials[CombatBoxType.Hurtbox] = MakeMaterial(new Color(0.16f, 0.78f, 1.0f, 0.24f));
        _materials[CombatBoxType.Pushbox] = MakeMaterial(new Color(1.0f, 1.0f, 1.0f, 0.18f));
        _materials[CombatBoxType.Grabbox] = MakeMaterial(new Color(1.0f, 0.12f, 0.9f, 0.36f));
        _materials[CombatBoxType.ProjectileBox] = MakeMaterial(new Color(1.0f, 0.58f, 0.08f, 0.34f));
        _materials[CombatBoxType.ArmorBox] = MakeMaterial(new Color(0.22f, 0.35f, 1.0f, 0.28f));
        _materials[CombatBoxType.WeakPointBox] = MakeMaterial(new Color(1.0f, 0.92f, 0.1f, 0.38f));
        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.F2 })
        {
            _enabled = !_enabled;
            Visible = _enabled;
        }
    }

    public override void _Process(double delta)
    {
        if (!_enabled || _simulation is null)
        {
            return;
        }

        var boxes = _simulation.Actors
            .SelectMany(actor => actor.ActiveBoxes)
            .ToArray();

        for (var index = 0; index < boxes.Length; index++)
        {
            var view = GetOrCreateBox(index);
            var box = boxes[index];
            view.Visible = true;
            view.GlobalPosition = box.Center;
            view.Scale = box.HalfExtents * 2.0f;
            view.MaterialOverride = _materials[box.Definition.BoxType];
            view.Name = $"{box.Owner.ActorId}_{box.Definition.BoxType}_{box.Definition.Id}";
        }

        for (var index = boxes.Length; index < _boxPool.Count; index++)
        {
            _boxPool[index].Visible = false;
        }
    }

    private MeshInstance3D GetOrCreateBox(int index)
    {
        while (_boxPool.Count <= index)
        {
            var meshInstance = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = Vector3.One },
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            };
            AddChild(meshInstance);
            _boxPool.Add(meshInstance);
        }

        return _boxPool[index];
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            NoDepthTest = true,
        };
    }
}
