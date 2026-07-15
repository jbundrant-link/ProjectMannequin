using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMannequin.Progression;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic tests for Tier 2 build systems: Move Card grid adjacency,
/// synergy detection, the expanded artifact library (themes + curses), and
/// squad-name generation. Runs headless via PROJECT_MANNEQUIN_BUILD_TEST=1.
/// </summary>
public static class RunBuildTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Run Build Tests ===");
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

        // --- Move Card grid adjacency ---
        var pairs2 = MoveCardGrid.AdjacentPairs(2).ToList();
        Check("two cells share one edge", pairs2.Count == 1 && pairs2[0] == (0, 1));

        var pairs3 = MoveCardGrid.AdjacentPairs(3).ToList();
        Check("a full row has two horizontal edges", pairs3.Count == 2);

        var pairs4 = MoveCardGrid.AdjacentPairs(4).ToList();
        Check("row wrap adds a vertical edge", pairs4.Contains((0, 3)) && !pairs4.Contains((2, 3)));

        // --- Synergy detection ---
        var juggle = MoveCardSynergyRules.Evaluate(new List<MoveCardType>
        {
            MoveCardType.Launcher, MoveCardType.ComboExtension,
        });
        Check(
            "launcher next to combo yields Juggle",
            juggle.BonusPercent == 8 && juggle.Synergies.Any(s => s.Name == "Juggle"));

        var overdrive = MoveCardSynergyRules.Evaluate(new List<MoveCardType>
        {
            MoveCardType.Special, MoveCardType.Ultimate,
        });
        Check("special next to ultimate yields Overdrive", overdrive.BonusPercent == 12);

        var none = MoveCardSynergyRules.Evaluate(new List<MoveCardType>
        {
            MoveCardType.Basic, MoveCardType.Basic,
        });
        Check("basic pair has no synergy", none.BonusPercent == 0);

        Check(
            "synergy is order-independent",
            MoveCardSynergyRules.Evaluate(new List<MoveCardType>
            {
                MoveCardType.ComboExtension, MoveCardType.Launcher,
            }).BonusPercent == 8);

        // --- Artifact library ---
        Check("artifact library expanded", ArtifactCatalog.Catalog.Count >= 8);
        Check("artifact pool is deep", ArtifactCatalog.Catalog.Count >= 14);
        Check("has cursed artifacts", ArtifactCatalog.Catalog.Values.Any(a => a.IsCursed));
        Check(
            "has a heal-on-acquire artifact",
            ArtifactCatalog.Catalog.Values.Any(a => a.HealOnAcquirePercent > 0));
        Check(
            "has multiple themes",
            ArtifactCatalog.Catalog.Values.Select(a => a.Theme).Distinct().Count() >= 3);
        Check(
            "cursed artifacts carry a real drawback",
            ArtifactCatalog.Catalog.Values
                .Where(a => a.IsCursed)
                .All(a => a.HealthDrainPerSecond > 0 || a.DefenseModifierPercent < 0 || a.MeterGainModifierPercent < 0));
        Check(
            "curses come in varied shapes (drain / meter / defense)",
            ArtifactCatalog.Catalog.Values.Any(a => a.IsCursed && a.HealthDrainPerSecond > 0)
            && ArtifactCatalog.Catalog.Values.Any(a => a.IsCursed && a.MeterGainModifierPercent < 0)
            && ArtifactCatalog.Catalog.Values.Any(a => a.IsCursed && a.DefenseModifierPercent < 0));

        // --- Squad naming ---
        Check(
            "empty roster has a fallback name",
            SquadNameGenerator.Generate(new List<string>()) == "The Hollow Archive");
        Check(
            "single form names the squad",
            SquadNameGenerator.Generate(new List<string> { "world_warrior_ryu_form" }).Contains("Warriors"));
        Check(
            "squad name is order-independent",
            SquadNameGenerator.Generate(new List<string> { "goku_archive_form", "archive_knight_form" })
            == SquadNameGenerator.Generate(new List<string> { "archive_knight_form", "goku_archive_form" }));

        // --- Lore catalog (narrative) ---
        Check("lore catalog is authored", ProjectMannequin.Data.LoreCatalog.AllFragmentIds.Count >= 6);
        Check(
            "each boss drops lore",
            ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss("world_warrior_ryu_form").Count > 0
            && ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss("goku_archive_form").Count > 0
            && ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss("archive_knight_form").Count > 0);
        Check(
            "lore fragments resolve with titles",
            ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss("goku_archive_form")
                .All(id => !string.IsNullOrEmpty(ProjectMannequin.Data.LoreCatalog.GetFragment(id)?.Title)));
        Check(
            "lore fragments have authored bodies (shown in the Codex)",
            ProjectMannequin.Data.LoreCatalog.AllFragmentIds
                .All(id => (ProjectMannequin.Data.LoreCatalog.GetFragment(id)?.Body?.Length ?? 0) > 20));
        Check(
            "unknown boss yields no lore",
            ProjectMannequin.Data.LoreCatalog.GetFragmentsForBoss("nonexistent").Count == 0);

        // --- Death tips (Death as a Teacher) ---
        Check(
            "boss move maps to its specific tip",
            ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove("world_warrior_boss_shoryuken")
                .Contains("invincible on startup"));
        Check(
            "regular enemy move has an authored tip",
            ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove("archive_bruiser_attack")
                .Contains("punish"));
        Check(
            "unknown projectile falls back to the zoning tip",
            ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove("mystery_fireball_blast")
                .Contains("projectile"));
        Check(
            "unknown reversal falls back to the anti-air tip",
            ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove("some_uppercut")
                .Contains("anti-air"));
        Check(
            "empty move id still yields a usable tip",
            !string.IsNullOrWhiteSpace(ProjectMannequin.Data.DeathTipsCatalog.GetTipForMove("")));

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }
}
