using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic combo-route regression tests built on the pure
/// <see cref="ComboRouteSimulator"/>. Verifies representative BnB routes and, on
/// failure, prints the per-hit breakdown (hit position, move id, scale, damage)
/// rather than a bare pass/fail. Runs headless via
/// PROJECT_MANNEQUIN_COMBO_ROUTE_TEST=1.
/// </summary>
public static class ComboRouteTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Combo Route Tests ===");
        var passed = 0;
        var failed = 0;

        void Check(string name, bool condition, string detail = "")
        {
            if (condition)
            {
                passed++;
                sb.AppendLine($"  PASS {name}");
            }
            else
            {
                failed++;
                sb.AppendLine($"  FAIL {name}{(string.IsNullOrEmpty(detail) ? "" : $" -> {detail}")}");
            }
        }

        static string Dump(IReadOnlyList<ComboHitResult> route)
        {
            return string.Join(", ", route.Select(hit => $"#{hit.Position} {hit.MoveId} x{hit.Scale:0.00}={hit.Damage}"));
        }

        // Route 1: mannequin BnB with distinct moves.
        var route1 = new List<ComboHit>
        {
            new("mannequin_light", 40),
            new("mannequin_medium", 60),
            new("mannequin_heavy", 90),
            new("mannequin_launcher", 70),
        };
        var r1 = ComboRouteSimulator.Simulate(route1);
        var raw1 = 40 + 60 + 90 + 70;
        var total1 = ComboRouteSimulator.TotalDamage(r1);
        Check("BnB starter deals full damage", r1[0].Damage == 40, Dump(r1));
        Check("BnB scaling is non-increasing", IsNonIncreasing(r1), Dump(r1));
        Check("BnB total is below the raw sum", total1 < raw1, $"total={total1} raw={raw1} :: {Dump(r1)}");

        // Route 2: repeat-move decay.
        var route2 = new List<ComboHit>
        {
            new("jab", 30),
            new("jab", 30),
            new("jab", 30),
        };
        var r2 = ComboRouteSimulator.Simulate(route2);
        Check("repeated jab decays below positional-only", r2[1].Scale < 0.9f, Dump(r2));
        Check("repeat route is strictly decreasing", IsStrictlyDecreasing(r2), Dump(r2));

        // Route 3: super ender respects the super floor.
        var route3 = new List<ComboHit>
        {
            new("jab", 30),
            new("strong", 50),
            new("super", 200, IsSuper: true, MinScale: 0.5f),
        };
        var r3 = ComboRouteSimulator.Simulate(route3);
        Check("super ender stays at or above the super floor", r3[2].Scale >= 0.5f, Dump(r3));

        // Route 4: a deep combo bottoms out at the floor.
        var route4 = Enumerable.Range(0, 8).Select(i => new ComboHit($"m{i}", 60)).ToList();
        var r4 = ComboRouteSimulator.Simulate(route4);
        Check("deep combo last hit reaches the floor", Mathf.IsEqualApprox(r4[^1].Scale, 0.35f), Dump(r4));

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }

    private static bool IsNonIncreasing(IReadOnlyList<ComboHitResult> route)
    {
        for (var i = 1; i < route.Count; i++)
        {
            if (route[i].Scale > route[i - 1].Scale + 0.0001f)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsStrictlyDecreasing(IReadOnlyList<ComboHitResult> route)
    {
        for (var i = 1; i < route.Count; i++)
        {
            if (route[i].Scale >= route[i - 1].Scale)
            {
                return false;
            }
        }

        return true;
    }
}
