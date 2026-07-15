using Godot;

namespace ProjectMannequin.Combat;

/// <summary>
/// Pure helpers for the authored boss Phase Burst — the transition shockwave a
/// boss emits when it advances a phase. Kept pure so the push direction and
/// magnitude are deterministic and unit-testable; the director applies the
/// authored <c>BossPhaseData</c> burst values through these.
/// </summary>
public static class PhaseBurst
{
    /// <summary>
    /// Horizontal push applied to a target by the burst: away from the boss,
    /// with the authored magnitude. Targets to the boss's right are pushed right
    /// (positive), targets to the left are pushed left (negative).
    /// </summary>
    public static float ResolvePushVelocity(float bossX, float targetX, float magnitude)
    {
        var direction = targetX >= bossX ? 1.0f : -1.0f;
        return direction * Mathf.Max(0.0f, magnitude);
    }
}
