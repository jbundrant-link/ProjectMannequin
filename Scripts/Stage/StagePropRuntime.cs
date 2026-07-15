using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Stage;

/// <summary>Pure targeting and knockback rules for authored prop explosions.</summary>
public static class StagePropRuntime
{
    public static bool CanExplosionAffect(
        StagePropData prop,
        Vector3 origin,
        Vector3 target,
        bool targetIsPlayer)
    {
        if (!prop.ExplodesOnBreak
            || prop.ExplosionRadius <= 0.0f
            || prop.ExplosionDamage <= 0)
        {
            return false;
        }

        var targetMask = targetIsPlayer
            ? StageHazardTargetMask.Players
            : StageHazardTargetMask.Enemies;
        if (!prop.ExplosionTargets.HasFlag(targetMask))
        {
            return false;
        }

        var delta = new Vector2(target.X - origin.X, target.Z - origin.Z);
        return delta.LengthSquared() <= prop.ExplosionRadius * prop.ExplosionRadius;
    }

    public static Vector3 ResolveExplosionKnockback(
        Vector3 origin,
        Vector3 target,
        float magnitude)
    {
        var direction = new Vector2(target.X - origin.X, target.Z - origin.Z);
        if (direction.LengthSquared() <= 0.0001f)
        {
            direction = Vector2.Right;
        }
        else
        {
            direction = direction.Normalized();
        }

        return new Vector3(direction.X, 0.0f, direction.Y)
            * Mathf.Max(0.0f, magnitude);
    }
}
