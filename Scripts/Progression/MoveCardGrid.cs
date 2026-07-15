using System.Collections.Generic;

namespace ProjectMannequin.Progression;

/// <summary>
/// Pure model for the modular Move Card grid. Cards are laid out row-major in a
/// fixed-width grid; adjacent cards (right and down neighbours) can form
/// synergies that grant a run-wide damage bonus. This is the deterministic rules
/// core — a drag-to-arrange UI is a later layer that reorders the same list.
/// </summary>
public static class MoveCardGrid
{
    public const int Columns = 3;

    /// <summary>
    /// Yields the right/down neighbour index pairs for a row-major grid holding
    /// <paramref name="count"/> cells (each undirected edge exactly once).
    /// </summary>
    public static IEnumerable<(int A, int B)> AdjacentPairs(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var column = i % Columns;
            if (column + 1 < Columns && i + 1 < count)
            {
                yield return (i, i + 1);
            }

            if (i + Columns < count)
            {
                yield return (i, i + Columns);
            }
        }
    }
}

public sealed class MoveCardSynergy
{
    public string Name { get; init; } = "";
    public int BonusPercent { get; init; }
}

/// <summary>
/// Evaluates adjacency synergies between Move Card types placed in the grid.
/// Synergies are order-independent and grant a cumulative run-wide damage bonus.
/// </summary>
public static class MoveCardSynergyRules
{
    public static (int BonusPercent, List<MoveCardSynergy> Synergies) Evaluate(
        IReadOnlyList<MoveCardType> cells)
    {
        var synergies = new List<MoveCardSynergy>();
        var total = 0;
        foreach (var (a, b) in MoveCardGrid.AdjacentPairs(cells.Count))
        {
            var synergy = Match(cells[a], cells[b]);
            if (synergy is not null)
            {
                synergies.Add(synergy);
                total += synergy.BonusPercent;
            }
        }

        return (total, synergies);
    }

    private static MoveCardSynergy? Match(MoveCardType a, MoveCardType b)
    {
        bool Pair(MoveCardType x, MoveCardType y) => (a == x && b == y) || (a == y && b == x);

        if (Pair(MoveCardType.Launcher, MoveCardType.ComboExtension))
        {
            return new MoveCardSynergy { Name = "Juggle", BonusPercent = 8 };
        }

        if (Pair(MoveCardType.ComboExtension, MoveCardType.ComboExtension))
        {
            return new MoveCardSynergy { Name = "Rushdown", BonusPercent = 6 };
        }

        if (Pair(MoveCardType.Projectile, MoveCardType.Projectile))
        {
            return new MoveCardSynergy { Name = "Zoning", BonusPercent = 6 };
        }

        if (Pair(MoveCardType.Special, MoveCardType.Ultimate))
        {
            return new MoveCardSynergy { Name = "Overdrive", BonusPercent = 12 };
        }

        if (Pair(MoveCardType.Launcher, MoveCardType.Special))
        {
            return new MoveCardSynergy { Name = "Reversal", BonusPercent = 8 };
        }

        if (Pair(MoveCardType.Projectile, MoveCardType.Special))
        {
            return new MoveCardSynergy { Name = "Trap", BonusPercent = 6 };
        }

        return null;
    }
}
