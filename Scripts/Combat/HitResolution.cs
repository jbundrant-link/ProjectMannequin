using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Combat;

/// <summary>
/// The defensive outcome of an incoming attack against a defender, in the order
/// the resolver evaluates them.
/// </summary>
public enum DefenseOutcome
{
    DeadOrInvulnerable,
    Armor,
    InstinctEvade,
    Parry,
    Block,
    Hit,
}

/// <summary>
/// Non-mutating snapshot of a defender's ability to answer an incoming attack.
/// Kept pure so resolution precedence can be reasoned about and unit-tested.
/// </summary>
public readonly record struct DefenseSnapshot(
    bool DeadOrInvulnerable,
    bool HasArmor,
    bool InstinctEvades,
    bool Parries,
    bool Blocks);

/// <summary>
/// Canonical, testable specification of how an incoming attack resolves against a
/// defender. <see cref="HitResolver"/> implements this precedence: dead or
/// invulnerable, then instinct-evade, then parry (Reflect Guard), then block,
/// otherwise a hit (which may become a counter-hit or punish-counter by timing).
/// Throws bypass block and parry upstream via move data (AttackHeight.Throw),
/// which is how Rush Throw beats passive blocking.
/// </summary>
public static class HitResolution
{
    public static readonly IReadOnlyList<DefenseOutcome> CanonicalOrder = new[]
    {
        DefenseOutcome.DeadOrInvulnerable,
        DefenseOutcome.Armor,
        DefenseOutcome.InstinctEvade,
        DefenseOutcome.Parry,
        DefenseOutcome.Block,
        DefenseOutcome.Hit,
    };

    public static DefenseOutcome Classify(DefenseSnapshot snapshot)
    {
        if (snapshot.DeadOrInvulnerable)
        {
            return DefenseOutcome.DeadOrInvulnerable;
        }

        if (snapshot.HasArmor)
        {
            return DefenseOutcome.Armor;
        }

        if (snapshot.InstinctEvades)
        {
            return DefenseOutcome.InstinctEvade;
        }

        if (snapshot.Parries)
        {
            return DefenseOutcome.Parry;
        }

        if (snapshot.Blocks)
        {
            return DefenseOutcome.Block;
        }

        return DefenseOutcome.Hit;
    }

    /// <summary>
    /// Reflect Guard pushback applied to the attacker on a successful parry.
    /// <paramref name="authored"/> below zero derives the default (bosses reflect
    /// harder), keeping the mechanic data-driven while preserving the historical
    /// 8 / 16 values.
    /// </summary>
    public static float ResolveReflectPushback(float authored, bool isBoss)
    {
        if (authored >= 0.0f)
        {
            return authored;
        }

        return isBoss ? 16.0f : 8.0f;
    }

    /// <summary>
    /// Chip damage an armored defender takes when absorbing a strike. The armored
    /// actor keeps its current state (no hitstun); armor never grants an illegal
    /// state skip and is bounded by the authored armor-box active window.
    /// </summary>
    public static int ResolveArmorChip(int baseDamage, float chipScale)
    {
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * Mathf.Clamp(chipScale, 0.0f, 1.0f)));
    }

    /// <summary>
    /// Damage after a weak-point multiplier. Multiplier 1.0 leaves damage
    /// unchanged so ordinary hurtboxes are unaffected.
    /// </summary>
    public static int ResolveWeakPointDamage(int damage, float multiplier)
    {
        return Mathf.Max(1, Mathf.RoundToInt(damage * Mathf.Max(0.0f, multiplier)));
    }

    /// <summary>
    /// Knockdown duration in frames. <paramref name="authored"/> below zero
    /// derives the default (a hard knockdown lasts longer), preserving the
    /// historical 40-frame soft knockdown.
    /// </summary>
    public static int ResolveKnockdownFrames(bool hardKnockdown, int authored)
    {
        if (authored >= 0)
        {
            return authored;
        }

        return hardKnockdown ? 60 : 40;
    }

    /// <summary>
    /// Upward velocity for a ground bounce, derived from the incoming fall speed
    /// with a floor so light launches still bounce readably. Ground bounce is
    /// opt-in per move so ordinary launches still knock down.
    /// </summary>
    public static float ResolveGroundBounceVelocity(float fallSpeed)
    {
        return Mathf.Max(6.0f, Mathf.Abs(fallSpeed) * 0.6f);
    }

    /// <summary>
    /// Reversed, dampened horizontal velocity for a wall bounce. Sign is flipped
    /// against the incoming horizontal speed so the target rebounds off the wall;
    /// a floor keeps the rebound readable. Wall bounce is opt-in per move.
    /// </summary>
    public static float ResolveWallBounceVelocity(float incomingXSpeed)
    {
        var magnitude = Mathf.Max(5.0f, Mathf.Abs(incomingXSpeed) * 0.7f);
        return incomingXSpeed >= 0.0f ? -magnitude : magnitude;
    }
}
