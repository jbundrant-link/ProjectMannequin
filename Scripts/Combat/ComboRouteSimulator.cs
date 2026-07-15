using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Combat;

/// <summary>One authored hit in a combo route for the route simulator/tests.</summary>
public readonly record struct ComboHit(
    string MoveId,
    int BaseDamage,
    bool IsSuper = false,
    float MinScale = 0.35f,
    float ProrationScale = 1.0f);

/// <summary>The resolved scaling and damage for one hit of a simulated route.</summary>
public readonly record struct ComboHitResult(
    int Position,
    string MoveId,
    float Scale,
    int Damage);

/// <summary>
/// Pure, deterministic simulation of a combo route using the same rules the live
/// combat applies (<see cref="ComboRules"/>): the starter's proration applies to
/// follow-up hits and repeat-move decay accumulates per move id. Enables
/// route-level combo unit tests without running the full simulation.
/// </summary>
public static class ComboRouteSimulator
{
    public static IReadOnlyList<ComboHitResult> Simulate(IReadOnlyList<ComboHit> route)
    {
        var results = new List<ComboHitResult>();
        if (route is null || route.Count == 0)
        {
            return results;
        }

        var starterProration = route[0].ProrationScale;
        var uses = new Dictionary<string, int>();

        for (var index = 0; index < route.Count; index++)
        {
            var hit = route[index];
            var position = index + 1;
            var repeatUses = uses.TryGetValue(hit.MoveId, out var used) ? used : 0;
            var scale = ComboRules.ResolveDamageScale(
                position,
                hit.MinScale,
                hit.IsSuper,
                starterProration,
                repeatUses);
            var damage = Mathf.Max(1, Mathf.RoundToInt(hit.BaseDamage * scale));
            results.Add(new ComboHitResult(position, hit.MoveId, scale, damage));
            uses[hit.MoveId] = repeatUses + 1;
        }

        return results;
    }

    public static int TotalDamage(IReadOnlyList<ComboHitResult> results)
    {
        var total = 0;
        foreach (var result in results)
        {
            total += result.Damage;
        }

        return total;
    }
}
