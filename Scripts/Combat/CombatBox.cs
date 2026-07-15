using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public enum CombatBoxType
{
    Hitbox,
    Hurtbox,
    Pushbox,
    Grabbox,
    ProjectileBox,
    ArmorBox,
    WeakPointBox,
}

public sealed class CombatBoxDefinition
{
    public string Id { get; set; } = "";
    public CombatBoxType BoxType { get; set; }
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public float OffsetX { get; set; }
    public float OffsetY { get; set; } = 1.0f;
    public float OffsetZ { get; set; }
    public float SizeX { get; set; } = 1.0f;
    public float SizeY { get; set; } = 2.0f;
    public float SizeZ { get; set; } = 1.0f;
    public int DamageOverride { get; set; } = -1;
    public bool AllowMultipleHits { get; set; }
    public float WeakPointDamageMultiplier { get; set; } = 1.0f;
    public float ArmorChipScale { get; set; } = 0.25f;
    public string HitEffectTag { get; set; } = "default";

    public bool IsActiveOnFrame(int frame)
    {
        return frame >= StartFrame && frame <= EndFrame;
    }
}

public sealed class CombatBox
{
    public CombatBox(
        CombatActor owner,
        CombatBoxDefinition definition,
        string moveId,
        int moveInstanceId,
        int spawnTick)
    {
        Owner = owner;
        Definition = definition;
        MoveId = moveId;
        MoveInstanceId = moveInstanceId;
        SpawnTick = spawnTick;
    }

    public CombatActor Owner { get; }
    public CombatBoxDefinition Definition { get; }
    public string MoveId { get; }
    public int MoveInstanceId { get; }
    public int SpawnTick { get; }

    public Vector3 Center
    {
        get
        {
            var facingSign = Owner.FacingRight ? 1.0f : -1.0f;
            return Owner.SimPosition + new Vector3(
                Definition.OffsetX * facingSign,
                Definition.OffsetY,
                Definition.OffsetZ);
        }
    }

    public Vector3 HalfExtents => new(
        Mathf.Max(0.01f, Definition.SizeX) * 0.5f,
        Mathf.Max(0.01f, Definition.SizeY) * 0.5f,
        Mathf.Max(0.01f, Definition.SizeZ) * 0.5f);

    public bool Overlaps(CombatBox other)
    {
        var centerA = Center;
        var centerB = other.Center;
        var extentsA = HalfExtents;
        var extentsB = other.HalfExtents;

        return Mathf.Abs(centerA.X - centerB.X) <= extentsA.X + extentsB.X
            && Mathf.Abs(centerA.Y - centerB.Y) <= extentsA.Y + extentsB.Y
            && Mathf.Abs(centerA.Z - centerB.Z) <= extentsA.Z + extentsB.Z;
    }
}
