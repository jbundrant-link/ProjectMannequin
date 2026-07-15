using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectMannequin.Progression;

public enum RewardKind
{
    MoveCard,
    Artifact,
}

/// <summary>
/// A single selectable option in a between-encounter reward offer.
/// </summary>
public sealed class RewardOption
{
    public RewardKind Kind { get; init; }
    public string Id { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public string Description { get; init; } = "";
    public string IconKey { get; init; } = "";
    public MoveCardRarity Rarity { get; init; }
}

/// <summary>
/// Pure, deterministic reward roll. Builds an offer of distinct Move Cards and
/// Artifacts the player does not already own, then shuffles with a seeded RNG so
/// the same seed always produces the same offer (safe for replay/determinism).
/// </summary>
public static class RunRewardSystem
{
    public static List<RewardOption> RollOffer(
        int seed,
        IReadOnlyCollection<string> ownedCardIds,
        IReadOnlyCollection<string> ownedArtifactIds,
        int count = 3)
    {
        var pool = new List<RewardOption>();

        foreach (var card in MoveCardCatalog.Catalog.Values)
        {
            if (ownedCardIds.Contains(card.Id))
            {
                continue;
            }

            pool.Add(new RewardOption
            {
                Kind = RewardKind.MoveCard,
                Id = card.Id,
                DisplayName = card.DisplayName,
                Description = DescribeCard(card),
                IconKey = card.CardType.ToString(),
                Rarity = card.Rarity,
            });
        }

        foreach (var artifact in ArtifactCatalog.Catalog.Values)
        {
            if (ownedArtifactIds.Contains(artifact.Id))
            {
                continue;
            }

            pool.Add(new RewardOption
            {
                Kind = RewardKind.Artifact,
                Id = artifact.Id,
                DisplayName = artifact.DisplayName,
                Description = artifact.Description,
                IconKey = "Artifact",
                Rarity = artifact.Rarity,
            });
        }

        // Deterministic Fisher-Yates shuffle so identical seeds reproduce offers.
        var random = new Random(seed);
        for (var i = pool.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool.Take(Math.Max(0, count)).ToList();
    }

    private static string DescribeCard(MoveCardData card)
    {
        var move = card.Move;
        if (move.IsSuper)
        {
            return $"{card.CardType} • {move.Damage} dmg • {move.MeterCost} meter";
        }

        return $"{card.CardType} • {move.Damage} dmg • {move.StartupFrames}f startup";
    }
}
