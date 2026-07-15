namespace ProjectMannequin.Combat;

/// <summary>
/// Identifies which fighting-layer rule set is authoritative for the current
/// simulation tick. This is the single source of truth for horde versus
/// boss-duel combat so future boss-duel mechanics (Reflect Guard, Phase Burst,
/// Rush Throw) hook in through one boundary instead of scattered stage-state
/// checks. All modes still run through the same fixed-tick pipeline and the
/// single <see cref="HitResolver"/>; the mode never grants a separate combat
/// path that could bypass the resolver.
/// </summary>
public enum CombatMode
{
    /// <summary>Normal Quiver-style side-scroller horde combat.</summary>
    Horde,

    /// <summary>Fighting-game boss encounter with duel rules layered on top.</summary>
    BossDuel,

    /// <summary>Training/replay harness (reserved for the training-mode pass).</summary>
    Training,

    /// <summary>Local versus testing (reserved for a future versus pass).</summary>
    Versus,
}
