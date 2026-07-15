using System.Collections.Generic;

namespace ProjectMannequin.Data;

public sealed class LoreFragment
{
    public string Id { get; set; } = "";
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string World { get; set; } = "";
}

/// <summary>
/// Authored, Hades-style lore fragments recovered by defeating bosses. Each boss
/// (keyed by the mission's unlockable form id) drops a small set of fragments;
/// collecting every fragment unlocks the secret ending / hidden challenger.
/// </summary>
public static class LoreCatalog
{
    private static readonly Dictionary<string, LoreFragment> Fragments = new();
    private static readonly Dictionary<string, List<string>> ByBoss = new();

    static LoreCatalog()
    {
        AddBossLore("archive_knight_form", new[]
        {
            new LoreFragment
            {
                Id = "lore_archive_1",
                Title = "The First Mannequin",
                World = "Archive District",
                Body = "Before the forms, there was only the blank shell — an empty vessel built to remember every fighter it ever faced.",
            },
            new LoreFragment
            {
                Id = "lore_archive_2",
                Title = "The Iron Vigil",
                World = "Archive District",
                Body = "The Archive Knight was the warden of the vault, sworn to test each shell before it could inherit a champion's soul.",
            },
        });

        AddBossLore("world_warrior_ryu_form", new[]
        {
            new LoreFragment
            {
                Id = "lore_warrior_1",
                Title = "The Wandering Road",
                World = "World Warrior Sector",
                Body = "Ryu's discipline echoes through the Archive: the answer is not in victory, but in the pursuit of the next battle.",
            },
            new LoreFragment
            {
                Id = "lore_warrior_2",
                Title = "Surge of the Hado",
                World = "World Warrior Sector",
                Body = "To copy the Hadouken, the shell first had to understand the stillness that precedes it.",
            },
        });

        AddBossLore("goku_archive_form", new[]
        {
            new LoreFragment
            {
                Id = "lore_astral_1",
                Title = "The Astral Spark",
                World = "Astral Battlefront",
                Body = "Goku's power was never borrowed — it was earned, form by form, across twelve ascensions of will.",
            },
            new LoreFragment
            {
                Id = "lore_astral_2",
                Title = "Beyond Instinct",
                World = "Astral Battlefront",
                Body = "The Ultra Instinct sign is not a technique. It is the silence of a mind that no longer needs to think to win.",
            },
        });
    }

    private static void AddBossLore(string bossFormId, LoreFragment[] fragments)
    {
        var ids = new List<string>();
        foreach (var fragment in fragments)
        {
            Fragments[fragment.Id] = fragment;
            ids.Add(fragment.Id);
        }

        ByBoss[bossFormId] = ids;
    }

    public static IReadOnlyList<string> GetFragmentsForBoss(string bossFormId)
    {
        return ByBoss.TryGetValue(bossFormId, out var ids) ? ids : new List<string>();
    }

    public static LoreFragment? GetFragment(string id)
    {
        return Fragments.TryGetValue(id, out var fragment) ? fragment : null;
    }

    public static IReadOnlyCollection<string> AllFragmentIds => Fragments.Keys;
}
