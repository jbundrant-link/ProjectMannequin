using System.Text;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for boss-duel-mode helpers. Currently covers the authored
/// Phase Burst push resolution (direction away from the boss, clamped magnitude).
/// Runs headless via PROJECT_MANNEQUIN_BOSS_MODE_TEST=1.
/// </summary>
public static class BossDuelModeTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Boss Duel Mode Tests ===");
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

        bool Approx(float a, float b) => Mathf.IsEqualApprox(a, b);

        // Phase Burst push resolution.
        Check(
            "target to the right is pushed right",
            Approx(PhaseBurst.ResolvePushVelocity(95.0f, 97.0f, 25.0f), 25.0f));
        Check(
            "target to the left is pushed left",
            Approx(PhaseBurst.ResolvePushVelocity(95.0f, 93.0f, 25.0f), -25.0f));
        Check(
            "target on the boss is pushed right (tie)",
            Approx(PhaseBurst.ResolvePushVelocity(95.0f, 95.0f, 25.0f), 25.0f));
        Check(
            "authored magnitude is honored",
            Approx(PhaseBurst.ResolvePushVelocity(95.0f, 97.0f, 12.0f), 12.0f));
        Check(
            "negative magnitude is clamped to zero",
            Approx(PhaseBurst.ResolvePushVelocity(95.0f, 93.0f, -5.0f), 0.0f));

        // Duel rules: facing lock and lane-depth constraint.
        Check("faces right toward a boss on the right", DuelRules.ResolveFacingRight(94.0f, 96.0f));
        Check("faces left toward a boss on the left", !DuelRules.ResolveFacingRight(96.0f, 94.0f));
        Check(
            "lane depth clamps beyond the band",
            Approx(DuelRules.ClampLaneDepth(5.0f, 2.0f), 2.0f)
                && Approx(DuelRules.ClampLaneDepth(-5.0f, 2.0f), -2.0f));
        Check(
            "lane depth leaves in-band positions",
            Approx(DuelRules.ClampLaneDepth(1.0f, 2.0f), 1.0f));
        Check(
            "non-positive half-depth is a no-op",
            Approx(DuelRules.ClampLaneDepth(9.0f, 0.0f), 9.0f));

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
