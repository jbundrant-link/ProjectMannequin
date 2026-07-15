using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMannequin.Progression;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for the between-encounter reward roll: offer size,
/// distinctness, owned-item exclusion, seed determinism, and count clamping.
/// Runs headless via PROJECT_MANNEQUIN_REWARD_TEST=1.
/// </summary>
public static class RunRewardTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Run Reward Tests ===");
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

        var none = new List<string>();

        Check("move card catalog is populated", MoveCardCatalog.Catalog.Count >= 6);
        Check("artifact catalog is populated", ArtifactCatalog.Catalog.Count >= 1);

        var offer = RunRewardSystem.RollOffer(1234, none, none);
        Check("offer returns three options", offer.Count == 3);
        Check(
            "offer options are distinct",
            offer.Select(option => option.Id).Distinct().Count() == offer.Count);
        Check(
            "offer options have display names",
            offer.All(option => !string.IsNullOrEmpty(option.DisplayName)));
        Check(
            "offer options have icon keys",
            offer.All(option => !string.IsNullOrEmpty(option.IconKey)));

        // Determinism: the same seed reproduces the same ordered offer.
        var repeat = RunRewardSystem.RollOffer(1234, none, none);
        Check(
            "same seed reproduces the offer",
            offer.Select(option => option.Id).SequenceEqual(repeat.Select(option => option.Id)));

        // Owned move cards are excluded from the pool.
        var ownAllCards = MoveCardCatalog.Catalog.Keys.ToList();
        var artifactOnly = RunRewardSystem.RollOffer(99, ownAllCards, none);
        Check(
            "owned move cards are excluded",
            artifactOnly.All(option => option.Kind == RewardKind.Artifact));
        Check(
            "offer clamps to remaining pool",
            artifactOnly.Count == System.Math.Min(3, ArtifactCatalog.Catalog.Count));

        // Count clamping.
        Check("count zero yields empty offer", RunRewardSystem.RollOffer(7, none, none, 0).Count == 0);
        var fullPool = MoveCardCatalog.Catalog.Count + ArtifactCatalog.Catalog.Count;
        Check(
            "large count yields whole pool",
            RunRewardSystem.RollOffer(7, none, none, 999).Count == fullPool);

        // Every rolled option resolves back to a real catalog entry.
        Check(
            "options resolve to catalog entries",
            offer.All(option => option.Kind == RewardKind.MoveCard
                ? MoveCardCatalog.GetCard(option.Id) != null
                : ArtifactCatalog.GetArtifact(option.Id) != null));

        // Procedural reward icons rasterize for every category without throwing.
        string[] iconKeys =
        {
            "Launcher", "Projectile", "ComboExtension", "Special", "Ultimate", "Artifact", "Basic",
        };
        var iconsOk = true;
        foreach (var key in iconKeys)
        {
            var texture = ProjectMannequin.Presentation.ProceduralRewardIconFactory.GetIcon(key);
            if (texture is null || texture.GetWidth() != 64 || texture.GetHeight() != 64)
            {
                iconsOk = false;
            }
        }

        Check("procedural icons rasterize for every category", iconsOk);

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
