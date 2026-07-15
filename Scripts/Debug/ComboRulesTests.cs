using System.Text;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for the combo-scaling rules: positional decay, floor,
/// super floor, starter proration (applied to follow-ups only), and repeat-move
/// decay, plus a representative BnB route showing monotonically decreasing
/// per-hit scale. Runs headless via PROJECT_MANNEQUIN_COMBO_TEST=1.
/// </summary>
public static class ComboRulesTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Combo Rules Tests ===");
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

        // Positional decay and floor.
        Check("starter is full scale", Approx(ComboRules.ResolveDamageScale(1, 0.35f, false, 1.0f, 0), 1.0f));
        Check("second hit decays 10%", Approx(ComboRules.ResolveDamageScale(2, 0.35f, false, 1.0f, 0), 0.9f));
        Check("third hit decays 20%", Approx(ComboRules.ResolveDamageScale(3, 0.35f, false, 1.0f, 0), 0.8f));
        Check("deep combo hits floor", Approx(ComboRules.ResolveDamageScale(10, 0.35f, false, 1.0f, 0), 0.35f));
        Check("super uses raised floor", Approx(ComboRules.ResolveDamageScale(10, 0.35f, true, 1.0f, 0), 0.5f));

        // Starter proration applies to follow-ups only.
        Check(
            "proration spares the starter",
            Approx(ComboRules.ResolveDamageScale(1, 0.1f, false, 0.5f, 0), 1.0f));
        Check(
            "proration scales the follow-up",
            Approx(ComboRules.ResolveDamageScale(2, 0.1f, false, 0.5f, 0), 0.45f));

        // Repeat-move decay.
        Check(
            "one repeat subtracts 0.05",
            Approx(ComboRules.ResolveDamageScale(2, 0.1f, false, 1.0f, 1), 0.85f));
        Check(
            "repeat decay stacks",
            Approx(ComboRules.ResolveDamageScale(2, 0.1f, false, 1.0f, 3), 0.75f));
        Check(
            "repeat decay respects floor",
            Approx(ComboRules.ResolveDamageScale(2, 0.8f, false, 1.0f, 5), 0.8f));

        // Representative BnB route: scale must be non-increasing and stay >= floor.
        const float floor = 0.35f;
        var previous = 2.0f;
        var monotonic = true;
        var aboveFloor = true;
        for (var position = 1; position <= 6; position++)
        {
            var scale = ComboRules.ResolveDamageScale(position, floor, false, 1.0f, 0);
            monotonic &= scale <= previous + 0.0001f;
            aboveFloor &= scale >= floor - 0.0001f;
            previous = scale;
        }

        Check("BnB route scale is non-increasing", monotonic);
        Check("BnB route stays at or above floor", aboveFloor);

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
