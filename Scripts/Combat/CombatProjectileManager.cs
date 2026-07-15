using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public sealed class CombatProjectileManager
{
    private readonly List<CombatProjectile> _projectiles = new();
    private readonly HashSet<string> _spawnedKeys = new();

    public IReadOnlyList<CombatProjectile> Projectiles => _projectiles;

    public void SpawnFromActors(
        IReadOnlyList<CombatActor> actors,
        Node visualParent,
        int tick)
    {
        foreach (var actor in actors)
        {
            var move = actor.CurrentMove;
            if (move is null || actor.IsDead)
            {
                continue;
            }

            foreach (var spawn in move.ProjectileSpawns.Where(
                         candidate => candidate.SpawnFrame == actor.CurrentMoveFrame))
            {
                var spawnKey =
                    $"{actor.ActorId}:{actor.CurrentMoveInstanceId}:{spawn.Id}:{spawn.SpawnFrame}";
                if (!_spawnedKeys.Add(spawnKey))
                {
                    continue;
                }

                _projectiles.Add(new CombatProjectile(
                    actor,
                    move,
                    spawn,
                    actor.CurrentMoveInstanceId,
                    tick,
                    visualParent));
            }
        }
    }

    public void Advance(int tick, float leftBound, float rightBound)
    {
        foreach (var projectile in _projectiles)
        {
            projectile.Advance();
            if (projectile.RemainingFrames <= 0
                || projectile.SimPosition.X < leftBound - 2.0f
                || projectile.SimPosition.X > rightBound + 2.0f)
            {
                projectile.Expire();
            }
        }

        RemoveExpired();
    }

    public int ResolveHits(
        IReadOnlyList<CombatActor> actors,
        int tick,
        ICollection<CombatPresentationEvent> presentationEvents)
    {
        var hitStopFrames = ResolveProjectileClashes(tick, presentationEvents);
        foreach (var projectile in _projectiles.Where(candidate => !candidate.IsExpired))
        {
            foreach (var target in actors)
            {
                if (!CanHit(projectile, target))
                {
                    continue;
                }

                var hurtboxes = target.ActiveBoxes.Where(box =>
                    box.Definition.BoxType is CombatBoxType.Hurtbox or CombatBoxType.WeakPointBox);
                if (!hurtboxes.Any(projectile.Overlaps))
                {
                    continue;
                }

                var move = projectile.Move;
                if (target.StateMachine.TryResolveInstinctEvade(
                        projectile.Owner,
                        move,
                        tick))
                {
                    break;
                }

                if (target.StateMachine.ResolveParry(projectile.Owner, move, tick))
                {
                    hitStopFrames = Mathf.Max(hitStopFrames, Mathf.Max(6, move.HitStopFrames + 2));
                    projectile.Expire();
                    break;
                }

                var targetWasComboVulnerable = target.State is CombatActorState.Hitstun
                    or CombatActorState.Knockdown
                    || target.SimPosition.Y > 0.001f;
                var damageScale = projectile.Owner.GetNextComboDamageScale(
                    target,
                    tick,
                    targetWasComboVulnerable,
                    move);
                var phaseDamageScale =
                    projectile.Owner.CurrentBossPhase?.DamageMultiplier ?? 1.0f;
                var damage = Mathf.Max(
                    1,
                    Mathf.RoundToInt(move.Damage * damageScale * phaseDamageScale * projectile.ClashDamageMultiplier));
                var facingSign = projectile.Velocity.X >= 0.0f ? 1.0f : -1.0f;
                var launchVelocity = new Vector3(
                    (move.LaunchX > 0.0f ? move.LaunchX : move.PushbackX) * facingSign,
                    move.LaunchY,
                    0.0f);
                var hit = new HitApplication(
                    projectile.Owner,
                    target,
                    move,
                    damage,
                    move.HitstunFrames,
                    move.BlockstunFrames,
                    launchVelocity,
                    move.IsLauncher);

                if (!projectile.ClashUnblockable
                    && target.CanBlockAttack(projectile.Owner, move)
                    && target.ApplyBlockedHit(hit, tick))
                {
                    projectile.RecordHit(target.ActorId);
                    projectile.Owner.AddMeter(Mathf.Max(1, move.MeterGain / 3));
                    presentationEvents.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.Blocked,
                        tick,
                        projectile.Owner.ActorId,
                        target.ActorId,
                        $"{move.Id}|0"));
                    hitStopFrames = Mathf.Max(hitStopFrames, Mathf.Max(2, move.HitStopFrames - 2));
                    if (projectile.Spawn.ExpireOnHit)
                    {
                        projectile.Expire();
                        break;
                    }
                    continue;
                }

                if (!target.ApplyHit(hit, tick))
                {
                    continue;
                }

                projectile.Owner.AddMeter(move.MeterGain);
                projectile.Owner.NotifySuccessfulHit(target, tick, targetWasComboVulnerable);
                projectile.RecordHit(target.ActorId);
                presentationEvents.Add(new CombatPresentationEvent(
                    move.IsLauncher
                        ? CombatPresentationEventType.LauncherHit
                        : CombatPresentationEventType.HitConnected,
                    tick,
                    projectile.Owner.ActorId,
                    target.ActorId,
                    $"{move.Id}|{damage}"));
                hitStopFrames = Mathf.Max(hitStopFrames, move.HitStopFrames);
                if (projectile.Spawn.ExpireOnHit)
                {
                    projectile.Expire();
                    break;
                }
            }
        }

        RemoveExpired();
        return hitStopFrames;
    }

    private int ResolveProjectileClashes(int tick, ICollection<CombatPresentationEvent> presentationEvents)
    {
        var hitStopFrames = 0;
        for (var first = 0; first < _projectiles.Count; first++)
        {
            var a = _projectiles[first];
            if (a.IsExpired || a.IsInClash)
            {
                continue;
            }

            for (var second = first + 1; second < _projectiles.Count; second++)
            {
                var b = _projectiles[second];
                if (b.IsExpired
                    || b.IsInClash
                    || a.Owner.TeamId == b.Owner.TeamId
                    || !a.Overlaps(b))
                {
                    continue;
                }

                if (!a.Spawn.ClashEligible || !b.Spawn.ClashEligible)
                {
                    continue;
                }

                if (a.Spawn.VisualType == ProjectileVisualType.Beam && b.Spawn.VisualType == ProjectileVisualType.Beam && a.Spawn.ClashStrength == b.Spawn.ClashStrength)
                {
                    a.IsInClash = true;
                    b.IsInClash = true;
                    presentationEvents.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.BeamClashStarted,
                        tick,
                        a.Owner.ActorId,
                        b.Owner.ActorId));
                    continue;
                }

                if (a.Spawn.ClashStrength <= b.Spawn.ClashStrength)
                {
                    a.Expire();
                }
                if (b.Spawn.ClashStrength <= a.Spawn.ClashStrength)
                {
                    b.Expire();
                }
                hitStopFrames = Mathf.Max(hitStopFrames, 4);
            }
        }

        return hitStopFrames;
    }

    private static bool CanHit(CombatProjectile projectile, CombatActor target)
    {
        return !projectile.IsExpired
            && projectile.Owner != target
            && projectile.Owner.TeamId != target.TeamId
            && !projectile.Owner.IsDead
            && !target.IsDead
            && !projectile.HasHit(target.ActorId)
            && !CombatActor.AreDuelIsolated(projectile.Owner, target);
    }

    private void RemoveExpired()
    {
        foreach (var projectile in _projectiles.Where(candidate => candidate.IsExpired))
        {
            projectile.DisposeVisual();
        }

        _projectiles.RemoveAll(projectile => projectile.IsExpired);
    }
}

public sealed class CombatProjectile
{
    private readonly Node3D _visual;
    private readonly Sprite3D? _spriteVisual;
    private readonly HashSet<string> _hitActorIds = new();
    private readonly float _facingSign;
    private int _ageFrames;

    public CombatProjectile(
        CombatActor owner,
        MoveData move,
        ProjectileSpawnData spawn,
        int moveInstanceId,
        int spawnTick,
        Node visualParent)
    {
        Owner = owner;
        Move = move;
        Spawn = spawn;
        MoveInstanceId = moveInstanceId;
        SpawnTick = spawnTick;
        RemainingFrames = Mathf.Max(1, spawn.LifetimeFrames);

        _facingSign = owner.FacingRight ? 1.0f : -1.0f;
        SimPosition = owner.SimPosition + new Vector3(
            spawn.OffsetX * _facingSign,
            spawn.OffsetY,
            spawn.OffsetZ);
        Velocity = new Vector3(
            spawn.VelocityX * _facingSign,
            spawn.VelocityY,
            spawn.VelocityZ);
        _visual = CreateVisual(spawn);
        if (spawn.VisualType == ProjectileVisualType.Beam && _facingSign < 0.0f)
        {
            _visual.RotationDegrees = new Vector3(0.0f, 180.0f, 0.0f);
        }
        visualParent.AddChild(_visual);
        _spriteVisual = _visual.GetNodeOrNull<Sprite3D>("Sprite");
        UpdateSpriteFrame();
        _visual.GlobalPosition = SimPosition;
    }

    public CombatActor Owner { get; }
    public MoveData Move { get; }
    public ProjectileSpawnData Spawn { get; }
    public int MoveInstanceId { get; }
    public int SpawnTick { get; }
    public int RemainingFrames { get; private set; }
    public Vector3 SimPosition { get; private set; }
    public Vector3 Velocity { get; }
    public bool IsExpired { get; private set; }
    public bool IsInClash { get; set; }
    public float ClashDamageMultiplier { get; set; } = 1.0f;
    public bool ClashUnblockable { get; set; }

    public void Advance()
    {
        if (IsExpired || IsInClash)
        {
            return;
        }

        SimPosition = Spawn.AttachToOwner
            ? Owner.SimPosition + new Vector3(
                Spawn.OffsetX * _facingSign,
                Spawn.OffsetY,
                Spawn.OffsetZ)
            : SimPosition + Velocity / GameConstants.TickRate;
        RemainingFrames--;
        _ageFrames++;
        UpdateSpriteFrame();
        _visual.GlobalPosition = SimPosition;
    }

    public bool Overlaps(CombatBox target)
    {
        var targetCenter = target.Center;
        var targetExtents = target.HalfExtents;
        var projectileExtents = HalfExtents;
        var collisionCenter = CollisionCenter;
        return Mathf.Abs(collisionCenter.X - targetCenter.X) <= projectileExtents.X + targetExtents.X
            && Mathf.Abs(collisionCenter.Y - targetCenter.Y) <= projectileExtents.Y + targetExtents.Y
            && Mathf.Abs(collisionCenter.Z - targetCenter.Z) <= projectileExtents.Z + targetExtents.Z;
    }

    public bool Overlaps(CombatProjectile other)
    {
        var extents = HalfExtents + other.HalfExtents;
        var delta = CollisionCenter - other.CollisionCenter;
        return Mathf.Abs(delta.X) <= extents.X
            && Mathf.Abs(delta.Y) <= extents.Y
            && Mathf.Abs(delta.Z) <= extents.Z;
    }

    public void Expire()
    {
        IsExpired = true;
    }

    public bool HasHit(string actorId)
    {
        return _hitActorIds.Contains(actorId);
    }

    public void RecordHit(string actorId)
    {
        _hitActorIds.Add(actorId);
    }

    public void DisposeVisual()
    {
        if (GodotObject.IsInstanceValid(_visual))
        {
            _visual.QueueFree();
        }
    }

    private Vector3 HalfExtents => new(
        Mathf.Max(0.01f, Spawn.SizeX) * 0.5f,
        Mathf.Max(0.01f, Spawn.SizeY) * 0.5f,
        Mathf.Max(0.01f, Spawn.SizeZ) * 0.5f);

    private Vector3 CollisionCenter => SimPosition + new Vector3(
        Spawn.CollisionOffsetX * _facingSign,
        Spawn.CollisionOffsetY,
        Spawn.CollisionOffsetZ);

    private static Node3D CreateVisual(ProjectileSpawnData spawn)
    {
        var projectileId = spawn.Id;
        var isSuper = projectileId.Contains("shinku", StringComparison.OrdinalIgnoreCase)
            || projectileId.Contains("denjin", StringComparison.OrdinalIgnoreCase);
        var isEx = projectileId.Contains("_ex", StringComparison.OrdinalIgnoreCase);
        var isDenjin = projectileId.Contains("denjin", StringComparison.OrdinalIgnoreCase);
        var root = new Node3D { Name = projectileId };

        if (!string.IsNullOrWhiteSpace(spawn.VisualAtlasPath)
            && spawn.VisualAtlasColumns > 0
            && spawn.VisualAtlasRows > 0
            && ResourceLoader.Exists(spawn.VisualAtlasPath))
        {
            AddSpriteVisual(root, spawn);
            return root;
        }

        if (spawn.VisualType == ProjectileVisualType.Beam)
        {
            AddBeamVisual(root, spawn, isSuper, isEx);
            return root;
        }

        var material = CreateEnergyMaterial(
            spawn.VisualColor,
            spawn.EmissionColor,
            isSuper ? 4.0f : isEx ? 3.3f : 2.6f);
        Mesh visualMesh = spawn.VisualType switch
        {
            ProjectileVisualType.Disc => new CylinderMesh
            {
                TopRadius = 0.52f,
                BottomRadius = 0.52f,
                Height = 0.12f,
                RadialSegments = 20,
            },
            ProjectileVisualType.Burst => new SphereMesh
            {
                Radius = 0.52f,
                Height = 0.9f,
                RadialSegments = 20,
                Rings = 10,
            },
            _ => new SphereMesh
            {
                Radius = isSuper ? 0.62f : 0.34f,
                Height = isSuper ? 1.1f : 0.62f,
                RadialSegments = 16,
                Rings = 8,
            },
        };
        var meshInstance = new MeshInstance3D
        {
            Mesh = visualMesh,
            MaterialOverride = material,
            Scale = Vector3.One * Mathf.Max(0.1f, spawn.VisualScale),
        };
        root.AddChild(meshInstance);
        return root;
    }

    private void UpdateSpriteFrame()
    {
        if (_spriteVisual is null)
        {
            return;
        }

        var sequence = Spawn.VisualFrameSequence;
        if (sequence.Count == 0)
        {
            _spriteVisual.Frame = 0;
            return;
        }

        var sequenceIndex = _ageFrames / Mathf.Max(1, Spawn.VisualFrameTicks);
        sequenceIndex = Spawn.VisualLoop
            ? sequenceIndex % sequence.Count
            : Mathf.Min(sequenceIndex, sequence.Count - 1);
        _spriteVisual.Frame = Mathf.Clamp(
            sequence[sequenceIndex],
            0,
            Mathf.Max(0, Spawn.VisualAtlasColumns * Spawn.VisualAtlasRows - 1));
    }

    private static void AddSpriteVisual(Node3D root, ProjectileSpawnData spawn)
    {
        var texture = GD.Load<Texture2D>(spawn.VisualAtlasPath);
        var columns = Mathf.Max(1, spawn.VisualAtlasColumns);
        var rows = Mathf.Max(1, spawn.VisualAtlasRows);
        var pixelSize = spawn.VisualPixelSize > 0.0f
            ? spawn.VisualPixelSize
            : 0.002f;
        var visualScale = Mathf.Max(0.1f, spawn.VisualScale);
        var frameWidth = texture.GetWidth() / (float)columns;
        var anchorOffsetX = (0.5f - Mathf.Clamp(spawn.VisualAnchorX, 0.0f, 1.0f))
            * frameWidth
            * pixelSize
            * visualScale;
        var sprite = new Sprite3D
        {
            Name = "Sprite",
            Texture = texture,
            Hframes = columns,
            Vframes = rows,
            PixelSize = pixelSize,
            Position = new Vector3(anchorOffsetX, 0.0f, -0.04f),
            Scale = Vector3.One * visualScale,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmaps,
            Shaded = false,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Billboard = BaseMaterial3D.BillboardModeEnum.Disabled,
            DoubleSided = true,
        };
        root.AddChild(sprite);
    }

    private static void AddBeamVisual(
        Node3D root,
        ProjectileSpawnData spawn,
        bool isSuper,
        bool isEx)
    {
        var visualScale = Mathf.Max(0.1f, spawn.VisualScale);
        var length = Mathf.Max(0.65f, spawn.SizeX) * visualScale;
        var radius = Mathf.Max(0.11f, spawn.SizeY * 0.5f) * visualScale;
        var energy = isSuper ? 2.8f : isEx ? 2.35f : 1.9f;
        var coreColor = spawn.VisualColor.Lightened(0.72f);
        var auraColor = new Color(
            spawn.VisualColor.R,
            spawn.VisualColor.G,
            spawn.VisualColor.B,
            0.28f);

        var aura = new MeshInstance3D
        {
            Name = "Aura",
            Mesh = CreateBeamCapsule(length, radius * 1.75f),
            MaterialOverride = CreateEnergyMaterial(
                auraColor,
                spawn.VisualColor,
                energy * 0.48f,
                additive: true),
            RotationDegrees = new Vector3(0.0f, 0.0f, 90.0f),
        };
        root.AddChild(aura);

        var body = new MeshInstance3D
        {
            Name = "Body",
            Mesh = CreateBeamCapsule(length, radius),
            MaterialOverride = CreateEnergyMaterial(
                spawn.VisualColor,
                spawn.EmissionColor,
                energy),
            RotationDegrees = new Vector3(0.0f, 0.0f, 90.0f),
        };
        root.AddChild(body);

        var core = new MeshInstance3D
        {
            Name = "Core",
            Mesh = CreateBeamCapsule(length * 1.02f, radius * 0.42f),
            MaterialOverride = CreateEnergyMaterial(
                coreColor,
                Colors.White,
                energy + 0.75f),
            RotationDegrees = new Vector3(0.0f, 0.0f, 90.0f),
        };
        root.AddChild(core);

        var head = new MeshInstance3D
        {
            Name = "Head",
            Mesh = new SphereMesh
            {
                Radius = radius * 1.35f,
                Height = radius * 2.7f,
                RadialSegments = 20,
                Rings = 10,
            },
            MaterialOverride = CreateEnergyMaterial(
                spawn.VisualColor.Lightened(0.42f),
                Colors.White,
                energy + 0.4f),
            Position = new Vector3(length * 0.5f, 0.0f, 0.0f),
        };
        root.AddChild(head);

        for (var index = 0; index < 3; index++)
        {
            var ring = new MeshInstance3D
            {
                Name = $"EnergyRing{index + 1}",
                Mesh = new TorusMesh
                {
                    InnerRadius = radius * 1.08f,
                    OuterRadius = radius * 1.34f,
                    Rings = 20,
                    RingSegments = 8,
                },
                MaterialOverride = CreateEnergyMaterial(
                    auraColor.Lightened(0.3f),
                    spawn.VisualColor,
                    energy * 0.72f,
                    additive: true),
                RotationDegrees = new Vector3(0.0f, 90.0f, 0.0f),
                Position = new Vector3(
                    (-length * 0.32f) + (index * length * 0.3f),
                    0.0f,
                    0.0f),
            };
            root.AddChild(ring);
        }
    }

    private static CapsuleMesh CreateBeamCapsule(float length, float radius)
    {
        return new CapsuleMesh
        {
            Radius = radius,
            Height = Mathf.Max(length, radius * 2.0f),
            RadialSegments = 20,
            Rings = 8,
        };
    }

    private static StandardMaterial3D CreateEnergyMaterial(
        Color color,
        Color emission,
        float emissionEnergy,
        bool additive = false)
    {
        return new StandardMaterial3D
        {
            AlbedoColor = color,
            EmissionEnabled = true,
            Emission = emission,
            EmissionEnergyMultiplier = emissionEnergy,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BlendMode = additive
                ? BaseMaterial3D.BlendModeEnum.Add
                : BaseMaterial3D.BlendModeEnum.Mix,
        };
    }
}
