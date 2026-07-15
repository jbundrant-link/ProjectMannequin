using System.Text;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for the defensive-resolution specification: the canonical
/// precedence in <see cref="HitResolution.Classify"/> and the data-driven Reflect
/// Guard pushback derivation. Runs headless via PROJECT_MANNEQUIN_DEFENSE_TEST=1.
/// </summary>
public static class DefensiveMechanicsTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Defensive Mechanics Tests ===");
        var passed = 0;
        var failed = 0;

        void Check(string name, bool condition)
        {
            if (condition)
            {
                passed++;
                sb.AppendLine($"  PASS {name}");
            }
            else
            {
                failed++;
                sb.AppendLine($"  FAIL {name}");
            }
        }

        // Resolution precedence.
        Check(
            "dead/invulnerable beats everything",
            HitResolution.Classify(new DefenseSnapshot(true, true, true, true, true))
                == DefenseOutcome.DeadOrInvulnerable);
        Check(
            "armor beats instinct/parry/block",
            HitResolution.Classify(new DefenseSnapshot(false, true, true, true, true))
                == DefenseOutcome.Armor);
        Check(
            "instinct-evade beats parry",
            HitResolution.Classify(new DefenseSnapshot(false, false, true, true, true))
                == DefenseOutcome.InstinctEvade);
        Check(
            "parry beats block",
            HitResolution.Classify(new DefenseSnapshot(false, false, false, true, true))
                == DefenseOutcome.Parry);
        Check(
            "block beats hit",
            HitResolution.Classify(new DefenseSnapshot(false, false, false, false, true))
                == DefenseOutcome.Block);
        Check(
            "undefended resolves to hit",
            HitResolution.Classify(new DefenseSnapshot(false, false, false, false, false))
                == DefenseOutcome.Hit);
        Check("canonical order has 6 stages", HitResolution.CanonicalOrder.Count == 6);

        // Armor chip and weak-point damage (data-driven).
        Check("armor chip is a fraction of base", HitResolution.ResolveArmorChip(40, 0.25f) == 10);
        Check("armor chip is at least 1", HitResolution.ResolveArmorChip(1, 0.0f) == 1);
        Check("weak-point multiplier scales damage", HitResolution.ResolveWeakPointDamage(50, 1.5f) == 75);
        Check("weak-point 1.0 leaves damage", HitResolution.ResolveWeakPointDamage(50, 1.0f) == 50);

        // Knockdown and ground bounce (data-driven).
        Check("soft knockdown default", HitResolution.ResolveKnockdownFrames(false, -1) == 40);
        Check("hard knockdown default", HitResolution.ResolveKnockdownFrames(true, -1) == 60);
        Check("authored knockdown overrides", HitResolution.ResolveKnockdownFrames(true, 24) == 24);
        Check(
            "ground bounce has a floor",
            Mathf.IsEqualApprox(HitResolution.ResolveGroundBounceVelocity(2.0f), 6.0f));
        Check(
            "ground bounce scales with fall speed",
            Mathf.IsEqualApprox(HitResolution.ResolveGroundBounceVelocity(20.0f), 12.0f));
        Check(
            "wall bounce reverses rightward speed",
            Mathf.IsEqualApprox(HitResolution.ResolveWallBounceVelocity(10.0f), -7.0f));
        Check(
            "wall bounce reverses leftward speed",
            Mathf.IsEqualApprox(HitResolution.ResolveWallBounceVelocity(-10.0f), 7.0f));
        Check(
            "wall bounce has a floor",
            Mathf.IsEqualApprox(HitResolution.ResolveWallBounceVelocity(2.0f), -5.0f));

        // Reflect Guard pushback (data-driven).
        Check(
            "reflect derives non-boss default",
            Mathf.IsEqualApprox(HitResolution.ResolveReflectPushback(-1.0f, false), 8.0f));
        Check(
            "reflect derives boss default",
            Mathf.IsEqualApprox(HitResolution.ResolveReflectPushback(-1.0f, true), 16.0f));
        Check(
            "authored reflect overrides derived",
            Mathf.IsEqualApprox(HitResolution.ResolveReflectPushback(24.0f, true), 24.0f));

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
