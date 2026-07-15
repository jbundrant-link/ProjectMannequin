using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Combat;

/// <summary>
/// Pure, testable rules for the modular boss-AI mechanics ("MechMods"). Kept
/// deterministic and side-effect free so each behavior can be reasoned about and
/// unit-tested independently of the <see cref="CpuFighterBrain"/> decision chain.
/// </summary>
public static class AiModules
{
    // Module keys for per-phase enablement of discretionary AI behaviors.
    public const string ModuleThrow = "throw";
    public const string ModuleAntiAir = "anti_air";
    public const string ModuleParry = "parry";
    public const string ModuleJumpEvade = "jump_evade";

    /// <summary>
    /// Whether a discretionary AI module is enabled for the current phase. A null
    /// or empty disabled list means every module is available.
    /// </summary>
    public static bool IsModuleEnabled(string moduleKey, IReadOnlyList<string>? disabledModules)
    {
        return disabledModules is null || !disabledModules.Contains(moduleKey);
    }
    /// <summary>
    /// Rush Throw likelihood: a guard-break throw becomes more probable the
    /// longer the opponent has held block, ramping from <paramref name="baseChance"/>
    /// up to <paramref name="baseChance"/> + <paramref name="maxBonus"/>.
    /// </summary>
    public static float ResolveRushThrowChance(
        float baseChance,
        int blockHeldFrames,
        float maxBonus,
        int rampFrames)
    {
        var ramp = rampFrames <= 0
            ? 1.0f
            : Mathf.Clamp((float)Mathf.Max(0, blockHeldFrames) / rampFrames, 0.0f, 1.0f);
        return Mathf.Clamp(baseChance + ramp * Mathf.Max(0.0f, maxBonus), 0.0f, 1.0f);
    }

    /// <summary>
    /// The tick until which a boss should hold intentional neutral after a phase
    /// transition/burst, so it recovers into phase-specific spacing instead of
    /// attacking immediately.
    /// </summary>
    public static int ResolvePhaseNeutralUntil(int tick, int transitionFrames, int burstRecoveryFrames)
    {
        return tick + Mathf.Max(0, transitionFrames) + Mathf.Max(0, burstRecoveryFrames);
    }

    /// <summary>
    /// Fairness: a chosen move must belong to the current phase's move pool. An
    /// empty pool means "no restriction" (the whole form is available).
    /// </summary>
    public static bool IsMoveLegalForPhase(string moveId, IReadOnlyList<string> enabledMoveIds)
    {
        return enabledMoveIds is null || enabledMoveIds.Count == 0 || enabledMoveIds.Contains(moveId);
    }
}
