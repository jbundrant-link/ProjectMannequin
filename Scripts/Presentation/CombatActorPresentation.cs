using System.Collections.Generic;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Presentation;

public partial class CombatActorPresentation : Node3D
{
    private readonly Dictionary<string, BodyPart> _parts = new();
    private CombatActor? _actor;
    private GameSimulation? _simulation;
    private Label3D _label = null!;
    private MeshInstance3D _healthBack = null!;
    private MeshInstance3D _healthFill = null!;

    private StandardMaterial3D _outlineMaterial = null!;
    private StandardMaterial3D _plateMaterial = null!;
    private StandardMaterial3D _plateLightMaterial = null!;
    private StandardMaterial3D _plateShadowMaterial = null!;
    private StandardMaterial3D _jointMaterial = null!;
    private StandardMaterial3D _accentMaterial = null!;
    private StandardMaterial3D _healthFillMaterial = null!;

    private string _lastFormId = "";

    public override void _Ready()
    {
        _actor = GetParentOrNull<CombatActor>();
        _simulation = GetNodeOrNull<GameSimulation>("../../GameSimulation");

        _outlineMaterial = MakeMaterial(new Color(0.055f, 0.035f, 0.055f));
        _plateMaterial = MakeMaterial(new Color(0.86f, 0.61f, 0.43f));
        _plateLightMaterial = MakeMaterial(new Color(1.0f, 0.82f, 0.60f));
        _plateShadowMaterial = MakeMaterial(new Color(0.48f, 0.27f, 0.25f));
        _jointMaterial = MakeMaterial(new Color(0.11f, 0.07f, 0.10f));
        _accentMaterial = MakeMaterial(new Color(0.85f, 0.95f, 1.0f));
        _healthFillMaterial = MakeMaterial(new Color(0.2f, 0.95f, 0.35f));

        BuildMannequin();
        BuildHudElements();
    }

    public override void _Process(double delta)
    {
        if (_actor is null)
        {
            return;
        }

        if (_lastFormId != _actor.CurrentForm.Id)
        {
            _lastFormId = _actor.CurrentForm.Id;
            ApplyFormStyle(_actor.CurrentForm);
        }

        UpdateBodyScale();
        UpdatePose();
        UpdateLabelAndHealth();
    }

    private void BuildMannequin()
    {
        AddPart("head", _plateLightMaterial, new Vector3(0.42f, 0.56f, 0.44f), new Vector3(0.0f, 2.72f, 0.0f));
        AddPart("facePlate", _accentMaterial, new Vector3(0.08f, 0.34f, 0.30f), new Vector3(0.22f, 2.74f, -0.02f));
        AddPart("neck", _jointMaterial, new Vector3(0.24f, 0.18f, 0.28f), new Vector3(0.0f, 2.38f, 0.0f), true);

        AddPart("chest", _plateMaterial, new Vector3(0.82f, 0.62f, 0.42f), new Vector3(0.0f, 2.08f, 0.0f));
        AddPart("chestLight", _plateLightMaterial, new Vector3(0.32f, 0.42f, 0.08f), new Vector3(0.14f, 2.12f, -0.22f));
        AddPart("spine", _jointMaterial, new Vector3(0.28f, 0.28f, 0.32f), new Vector3(0.0f, 1.68f, 0.0f), true);
        AddPart("abdomen", _plateMaterial, new Vector3(0.58f, 0.48f, 0.36f), new Vector3(0.0f, 1.48f, 0.0f));
        AddPart("pelvis", _plateShadowMaterial, new Vector3(0.72f, 0.34f, 0.42f), new Vector3(0.0f, 1.10f, 0.0f));

        AddLimb("left", -1.0f);
        AddLimb("right", 1.0f);
    }

    private void AddLimb(string side, float sign)
    {
        AddPart($"{side}Shoulder", _jointMaterial, new Vector3(0.24f, 0.30f, 0.32f), new Vector3(sign * 0.56f, 2.14f, 0.0f), true);
        AddPart($"{side}UpperArm", _plateMaterial, new Vector3(0.28f, 0.58f, 0.30f), new Vector3(sign * 0.72f, 1.74f, 0.0f));
        AddPart($"{side}Elbow", _jointMaterial, new Vector3(0.22f, 0.22f, 0.28f), new Vector3(sign * 0.72f, 1.38f, 0.0f), true);
        AddPart($"{side}Forearm", _plateMaterial, new Vector3(0.26f, 0.52f, 0.28f), new Vector3(sign * 0.72f, 1.08f, 0.0f));
        AddPart($"{side}Fist", _plateShadowMaterial, new Vector3(0.30f, 0.26f, 0.30f), new Vector3(sign * 0.72f, 0.72f, -0.02f));

        AddPart($"{side}Hip", _jointMaterial, new Vector3(0.26f, 0.24f, 0.32f), new Vector3(sign * 0.28f, 0.92f, 0.0f), true);
        AddPart($"{side}Thigh", _plateMaterial, new Vector3(0.30f, 0.66f, 0.34f), new Vector3(sign * 0.28f, 0.56f, 0.0f));
        AddPart($"{side}Knee", _jointMaterial, new Vector3(0.24f, 0.20f, 0.30f), new Vector3(sign * 0.28f, 0.16f, 0.0f), true);
        AddPart($"{side}Shin", _plateMaterial, new Vector3(0.26f, 0.58f, 0.30f), new Vector3(sign * 0.28f, -0.18f, 0.0f));
        AddPart($"{side}Foot", _plateShadowMaterial, new Vector3(0.48f, 0.18f, 0.34f), new Vector3(sign * 0.34f, -0.56f, -0.06f));
    }

    private void BuildHudElements()
    {
        _healthBack = MakeBox("HealthBack", MakeMaterial(new Color(0.08f, 0.08f, 0.08f)));
        _healthFill = MakeBox("HealthFill", _healthFillMaterial);

        _label = new Label3D
        {
            Name = "ActorLabel",
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            PixelSize = 0.014f,
            Modulate = Colors.White,
            OutlineSize = 6,
            Text = "",
        };
        AddChild(_label);
    }

    private BodyPart AddPart(
        string partName,
        Material fillMaterial,
        Vector3 size,
        Vector3 position,
        bool roundJoint = false)
    {
        var root = new Node3D
        {
            Name = partName,
            Position = position,
        };
        AddChild(root);

        var outline = new MeshInstance3D
        {
            Name = "Outline",
            Mesh = roundJoint ? new SphereMesh() : new BoxMesh { Size = Vector3.One },
            MaterialOverride = _outlineMaterial,
            Scale = size * 1.16f,
        };
        root.AddChild(outline);

        var fill = new MeshInstance3D
        {
            Name = "Fill",
            Mesh = roundJoint ? new SphereMesh() : new BoxMesh { Size = Vector3.One },
            MaterialOverride = fillMaterial,
            Scale = size,
        };
        root.AddChild(fill);

        var part = new BodyPart(root, outline, fill, position, size);
        _parts[partName] = part;
        return part;
    }

    private void UpdateBodyScale()
    {
        if (_actor is null)
        {
            return;
        }

        var bossScale = _actor.IsBoss ? 1.18f : 1.0f;
        var formScale = _actor.CurrentForm.Id.Contains("knight") && _actor.TeamId == 1 ? 1.06f : 1.0f;
        var eliteScale = _actor.IsElite ? 1.15f : 1.0f;
        Scale = Vector3.One * bossScale * formScale * eliteScale;
    }

    private void UpdatePose()
    {
        if (_actor is null)
        {
            return;
        }

        ResetParts();

        var facingSign = _actor.FacingRight ? 1.0f : -1.0f;
        var time = Time.GetTicksMsec() * 0.001f;
        var speed = _actor.State == CombatActorState.Dashing ? 13.0f : 8.0f;
        var swing = Mathf.Sin(time * speed);
        var counterSwing = Mathf.Sin(time * speed + Mathf.Pi);

        Position = _actor.State switch
        {
            CombatActorState.Walking => new Vector3(0.0f, Mathf.Abs(swing) * 0.035f, 0.0f),
            CombatActorState.Dashing => new Vector3(0.0f, Mathf.Abs(swing) * 0.055f, 0.0f),
            CombatActorState.Crouching => new Vector3(0.0f, -0.06f, 0.0f),
            CombatActorState.Attacking => new Vector3(facingSign * 0.06f, 0.02f, 0.0f),
            CombatActorState.Blocking or CombatActorState.Blockstun => new Vector3(-facingSign * 0.05f, -0.02f, 0.0f),
            CombatActorState.FormSwapping => new Vector3(0.0f, Mathf.Sin(time * 24.0f) * 0.035f, 0.0f),
            _ => Vector3.Zero,
        };

        var isBossIntro = _actor.IsBoss && _simulation?.EncounterDirector?.State == ProjectMannequin.Stage.ArcadeStageState.BossIntro;
        
        RotationDegrees = _actor.State switch
        {
            CombatActorState.Hitstun => new Vector3(0.0f, 0.0f, _actor.FacingRight ? -7.0f : 7.0f),
            CombatActorState.Dead => new Vector3(0.0f, 0.0f, 82.0f),
            _ => new Vector3(0.0f, _actor.FacingRight ? -8.0f : 8.0f, 0.0f),
        };

        SetIdleBreathing(time);

        if (_actor.State is CombatActorState.Walking or CombatActorState.Dashing)
        {
            SetWalkPose(swing, counterSwing);
        }

        if (_actor.State == CombatActorState.Attacking)
        {
            SetAttackPose(facingSign);
        }

        if (_actor.State == CombatActorState.Jumping)
        {
            SetJumpPose();
        }

        if (_actor.State is CombatActorState.Blocking or CombatActorState.Blockstun)
        {
            SetGuardPose(facingSign);
        }

        if (_actor.State == CombatActorState.FormSwapping)
        {
            SetFormSwapPose(time);
        }

        if (isBossIntro)
        {
            SetBossIntroPose(time);
        }

        var isPlayerIntro = _actor.IsPlayerControlled
            && _simulation?.EncounterDirector?.State == ProjectMannequin.Stage.ArcadeStageState.BossIntro;
        if (isPlayerIntro)
        {
            SetPlayerIntroPose(time);
        }
    }

    private void ResetParts()
    {
        foreach (var part in _parts.Values)
        {
            part.Root.Position = part.BasePosition;
            part.Root.RotationDegrees = Vector3.Zero;
            part.Root.Scale = Vector3.One;
        }
    }

    private void SetIdleBreathing(float time)
    {
        var breath = Mathf.Sin(time * 2.4f) * 0.025f;
        Offset("chest", new Vector3(0.0f, breath, 0.0f));
        Offset("head", new Vector3(0.0f, breath * 0.8f, 0.0f));
        Rotate("leftForearm", new Vector3(0.0f, 0.0f, -3.0f));
        Rotate("rightForearm", new Vector3(0.0f, 0.0f, 3.0f));
    }

    private void SetWalkPose(float swing, float counterSwing)
    {
        PoseArm("left", swing);
        PoseArm("right", counterSwing);
        PoseLeg("left", counterSwing);
        PoseLeg("right", swing);

        Rotate("chest", new Vector3(0.0f, 0.0f, swing * 2.4f));
        Rotate("pelvis", new Vector3(0.0f, 0.0f, -swing * 2.0f));
    }

    private void PoseArm(string side, float swing)
    {
        var sign = side == "left" ? -1.0f : 1.0f;
        Offset($"{side}UpperArm", new Vector3(sign * swing * 0.07f, -Mathf.Abs(swing) * 0.02f, 0.0f));
        Offset($"{side}Forearm", new Vector3(sign * swing * 0.10f, -Mathf.Abs(swing) * 0.02f, 0.0f));
        Offset($"{side}Fist", new Vector3(sign * swing * 0.13f, -Mathf.Abs(swing) * 0.025f, 0.0f));
        Rotate($"{side}UpperArm", new Vector3(0.0f, 0.0f, sign * swing * 12.0f));
        Rotate($"{side}Forearm", new Vector3(0.0f, 0.0f, sign * swing * 10.0f));
    }

    private void PoseLeg(string side, float swing)
    {
        var sign = side == "left" ? -1.0f : 1.0f;
        Offset($"{side}Thigh", new Vector3(sign * swing * 0.08f, Mathf.Abs(swing) * 0.02f, 0.0f));
        Offset($"{side}Shin", new Vector3(sign * swing * 0.12f, Mathf.Abs(swing) * 0.015f, 0.0f));
        Offset($"{side}Foot", new Vector3(sign * swing * 0.17f, Mathf.Max(0.0f, swing) * 0.06f, -0.02f));
        Rotate($"{side}Thigh", new Vector3(0.0f, 0.0f, -sign * swing * 10.0f));
        Rotate($"{side}Shin", new Vector3(0.0f, 0.0f, sign * swing * 8.0f));
    }

    private void SetAttackPose(float facingSign)
    {
        var leadSide = facingSign > 0.0f ? "right" : "left";
        var backSide = facingSign > 0.0f ? "left" : "right";
        var leadSign = leadSide == "left" ? -1.0f : 1.0f;
        var backSign = backSide == "left" ? -1.0f : 1.0f;

        Offset($"{leadSide}Shoulder", new Vector3(facingSign * 0.10f, 0.0f, -0.02f));
        Offset($"{leadSide}UpperArm", new Vector3(facingSign * 0.28f, 0.12f, -0.04f));
        Offset($"{leadSide}Forearm", new Vector3(facingSign * 0.55f, 0.12f, -0.06f));
        Offset($"{leadSide}Fist", new Vector3(facingSign * 0.88f, 0.15f, -0.08f));
        Rotate($"{leadSide}UpperArm", new Vector3(0.0f, 0.0f, -leadSign * 22.0f));
        Rotate($"{leadSide}Forearm", new Vector3(0.0f, 0.0f, -leadSign * 34.0f));

        Offset($"{backSide}Fist", new Vector3(-facingSign * 0.12f, 0.16f, 0.02f));
        Rotate($"{backSide}UpperArm", new Vector3(0.0f, 0.0f, backSign * 16.0f));
        Rotate($"{backSide}Forearm", new Vector3(0.0f, 0.0f, backSign * 28.0f));

        Rotate("chest", new Vector3(0.0f, 0.0f, -facingSign * 4.0f));
        Rotate("pelvis", new Vector3(0.0f, 0.0f, facingSign * 4.0f));
    }

    private void SetBossIntroPose(float time)
    {
        var floatAnim = Mathf.Sin(time * 4.0f) * 0.1f;
        Position += new Vector3(0.0f, 0.8f + floatAnim, 0.0f);
        
        Rotate("chest", new Vector3(15.0f, -10.0f, 0.0f));
        Rotate("head", new Vector3(-10.0f, 15.0f, 0.0f));
        Rotate("leftShoulder", new Vector3(25.0f, 0.0f, 65.0f));
        Rotate("rightShoulder", new Vector3(-25.0f, 0.0f, -65.0f));
        Rotate("leftElbow", new Vector3(0.0f, -45.0f, 0.0f));
        Rotate("rightElbow", new Vector3(0.0f, 45.0f, 0.0f));
        Rotate("leftHip", new Vector3(-15.0f, 0.0f, 15.0f));
        Rotate("rightHip", new Vector3(15.0f, 0.0f, -15.0f));
        Rotate("leftKnee", new Vector3(25.0f, 0.0f, 0.0f));
        Rotate("rightKnee", new Vector3(25.0f, 0.0f, 0.0f));
    }

    private void SetPlayerIntroPose(float time)
    {
        // Grounded fighting-ready stance: fists up with a confident bounce.
        var bounce = Mathf.Abs(Mathf.Sin(time * 7.0f)) * 0.05f;
        Position += new Vector3(0.0f, bounce, 0.0f);
        Rotate("chest", new Vector3(8.0f, 0.0f, 0.0f));
        Rotate("head", new Vector3(-6.0f, 0.0f, 0.0f));
        Rotate("leftShoulder", new Vector3(0.0f, 0.0f, 45.0f));
        Rotate("rightShoulder", new Vector3(0.0f, 0.0f, -45.0f));
        Rotate("leftElbow", new Vector3(0.0f, -60.0f, 0.0f));
        Rotate("rightElbow", new Vector3(0.0f, 60.0f, 0.0f));
    }

    private void SetJumpPose()
    {
        Offset("leftThigh", new Vector3(-0.08f, 0.14f, 0.0f));
        Offset("rightThigh", new Vector3(0.08f, 0.04f, 0.0f));
        Offset("leftShin", new Vector3(-0.16f, 0.10f, 0.0f));
        Offset("rightShin", new Vector3(0.10f, -0.02f, 0.0f));
        Rotate("leftThigh", new Vector3(0.0f, 0.0f, 12.0f));
        Rotate("rightThigh", new Vector3(0.0f, 0.0f, -7.0f));
        Rotate("leftForearm", new Vector3(0.0f, 0.0f, -24.0f));
        Rotate("rightForearm", new Vector3(0.0f, 0.0f, 24.0f));
    }

    private void SetGuardPose(float facingSign)
    {
        var leadSide = facingSign > 0.0f ? "right" : "left";
        var backSide = facingSign > 0.0f ? "left" : "right";
        var leadSign = leadSide == "left" ? -1.0f : 1.0f;
        var backSign = backSide == "left" ? -1.0f : 1.0f;

        Offset($"{leadSide}Fist", new Vector3(facingSign * 0.16f, 0.42f, -0.08f));
        Offset($"{backSide}Fist", new Vector3(facingSign * 0.06f, 0.26f, -0.04f));
        Rotate($"{leadSide}UpperArm", new Vector3(0.0f, 0.0f, -leadSign * 28.0f));
        Rotate($"{leadSide}Forearm", new Vector3(0.0f, 0.0f, -leadSign * 58.0f));
        Rotate($"{backSide}UpperArm", new Vector3(0.0f, 0.0f, -backSign * 18.0f));
        Rotate($"{backSide}Forearm", new Vector3(0.0f, 0.0f, -backSign * 46.0f));
        Rotate("chest", new Vector3(0.0f, 0.0f, -facingSign * 5.0f));
    }

    private void SetFormSwapPose(float time)
    {
        var pulse = 1.0f + Mathf.Sin(time * 30.0f) * 0.04f;
        foreach (var part in _parts.Values)
        {
            part.Root.Scale = Vector3.One * pulse;
        }
    }

    private void UpdateLabelAndHealth()
    {
        if (_actor is null)
        {
            return;
        }

        var maxHealth = Mathf.Max(1, _actor.CurrentForm.MaxHealth);
        var healthRatio = Mathf.Clamp((float)_actor.Health / maxHealth, 0.0f, 1.0f);
        var barY = _actor.IsBoss ? 3.55f : 3.25f;

        _healthBack.Scale = new Vector3(_actor.IsBoss ? 1.55f : 1.25f, 0.08f, 0.04f);
        _healthBack.Position = new Vector3(0.0f, barY, 0.0f);
        _healthFill.Scale = new Vector3(_healthBack.Scale.X * healthRatio, _healthBack.Scale.Y * 0.72f, _healthBack.Scale.Z * 1.2f);
        _healthFill.Position = _healthBack.Position + new Vector3((_healthFill.Scale.X - _healthBack.Scale.X) * 0.5f, 0.012f, -0.03f);
        _healthFillMaterial.AlbedoColor = healthRatio switch
        {
            > 0.55f => new Color(0.2f, 0.95f, 0.35f),
            > 0.25f => new Color(1.0f, 0.82f, 0.2f),
            _ => new Color(1.0f, 0.2f, 0.16f),
        };

        _label.Position = new Vector3(0.0f, barY + 0.28f, 0.0f);
        _label.Text = $"{_actor.Name}\n{_actor.CurrentForm.DisplayName}  HP {_actor.Health}/{maxHealth}";
    }

    private void ApplyFormStyle(CharacterData form)
    {
        if (_actor is null)
        {
            return;
        }

        var baseColor = new Color(0.86f, 0.61f, 0.43f);
        var lightColor = new Color(1.0f, 0.82f, 0.60f);
        var shadowColor = new Color(0.48f, 0.27f, 0.25f);
        var accentColor = _actor.TeamId == 1
            ? GameConstants.StandardPlayerColors[Mathf.Clamp(_actor.PlayerId - 1, 0, GameConstants.StandardPlayerColors.Length - 1)]
            : new Color(1.0f, 0.36f, 0.20f);

        if (form.Id.Contains("knight"))
        {
            baseColor = _actor.TeamId == 1 ? new Color(0.68f, 0.74f, 0.82f) : new Color(0.54f, 0.44f, 0.70f);
            lightColor = _actor.TeamId == 1 ? new Color(0.92f, 0.96f, 1.0f) : new Color(0.82f, 0.68f, 1.0f);
            shadowColor = _actor.TeamId == 1 ? new Color(0.28f, 0.34f, 0.45f) : new Color(0.25f, 0.17f, 0.34f);
            accentColor = _actor.TeamId == 1 ? new Color(0.50f, 0.82f, 1.0f) : new Color(1.0f, 0.74f, 0.22f);
        }

        _plateMaterial.AlbedoColor = baseColor;
        _plateLightMaterial.AlbedoColor = lightColor;
        _plateShadowMaterial.AlbedoColor = shadowColor;
        _accentMaterial.AlbedoColor = accentColor;
    }

    private void Offset(string partName, Vector3 offset)
    {
        if (_parts.TryGetValue(partName, out var part))
        {
            part.Root.Position = part.BasePosition + offset;
        }
    }

    private void Rotate(string partName, Vector3 rotationDegrees)
    {
        if (_parts.TryGetValue(partName, out var part))
        {
            part.Root.RotationDegrees = rotationDegrees;
        }
    }

    private MeshInstance3D MakeBox(string nodeName, Material material)
    {
        var meshInstance = new MeshInstance3D
        {
            Name = nodeName,
            Mesh = new BoxMesh { Size = Vector3.One },
            MaterialOverride = material,
        };
        AddChild(meshInstance);
        return meshInstance;
    }

    private static StandardMaterial3D MakeMaterial(Color color)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            Roughness = 0.88f,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
        };
    }

    private sealed record BodyPart(
        Node3D Root,
        MeshInstance3D Outline,
        MeshInstance3D Fill,
        Vector3 BasePosition,
        Vector3 BaseScale);
}

