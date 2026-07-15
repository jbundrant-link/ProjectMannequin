using Godot;

namespace ProjectMannequin.Combat;

/// <summary>
/// Pure helpers for the boss-duel rules profile layered over side-scroller rooms.
/// Deterministic and unit-testable: fighting-game facing toward the opponent and
/// a tighter lane-depth constraint. The director applies these only while a boss
/// duel is active and the encounter opts in, so ordinary rooms are unchanged.
/// </summary>
public static class DuelRules
{
    /// <summary>Facing that points from <paramref name="selfX"/> toward the opponent.</summary>
    public static bool ResolveFacingRight(float selfX, float targetX)
    {
        return targetX >= selfX;
    }

    /// <summary>
    /// Clamps lane depth to a tighter duel band. A non-positive half-depth leaves
    /// the position unchanged.
    /// </summary>
    public static float ClampLaneDepth(float z, float halfDepth)
    {
        if (halfDepth <= 0.0f)
        {
            return z;
        }

        return Mathf.Clamp(z, -halfDepth, halfDepth);
    }
}
