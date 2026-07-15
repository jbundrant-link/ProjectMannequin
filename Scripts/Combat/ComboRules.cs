using Godot;

namespace ProjectMannequin.Combat;

/// <summary>
/// Inspectable, pure combo-scaling rules. Damage scaling is deterministic and
/// testable in isolation: positional decay per hit, starter proration applied to
/// follow-up hits, repeat-move decay to discourage loops, and a floor (raised for
/// supers). Defaults (starter proration 1.0, no repeats) reproduce the historical
/// scaling exactly so existing combos are unchanged.
/// </summary>
public static class ComboRules
{
    public const float PositionalDecayPerHit = 0.1f;
    public const float RepeatDecayPerUse = 0.05f;
    public const float SuperFloor = 0.5f;

    /// <summary>
    /// Damage multiplier for a hit at <paramref name="comboPosition"/> (1-based).
    /// The starter (position 1) always lands at full positional scale; proration
    /// and repeat decay apply to follow-ups. The result is clamped to the move's
    /// floor and 1.0.
    /// </summary>
    public static float ResolveDamageScale(
        int comboPosition,
        float moveMinScale,
        bool isSuper,
        float starterProration,
        int repeatUses)
    {
        var floor = Mathf.Clamp(
            isSuper ? Mathf.Max(SuperFloor, moveMinScale) : moveMinScale,
            0.0f,
            1.0f);

        var position = Mathf.Max(1, comboPosition);
        var positional = 1.0f - (position - 1) * PositionalDecayPerHit;
        var prorated = position <= 1
            ? positional
            : positional * Mathf.Clamp(starterProration, 0.1f, 1.0f);
        var scale = prorated - Mathf.Max(0, repeatUses) * RepeatDecayPerUse;

        return Mathf.Clamp(scale, floor, 1.0f);
    }
}
