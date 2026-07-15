using System.Collections.Generic;
using System.Linq;

namespace ProjectMannequin.Progression;

/// <summary>
/// Builds a flavour "squad name" from the set of forms the player has archived
/// (Marvel Tokon-style dynamic team naming). Deterministic: the same set of
/// forms always produces the same name, independent of unlock order.
/// </summary>
public static class SquadNameGenerator
{
    private static readonly Dictionary<string, (string Adjective, string Noun)> FormWords = new()
    {
        ["blank_mannequin"] = ("Blank", "Mannequins"),
        ["archive_knight_form"] = ("Iron", "Vanguard"),
        ["world_warrior_ryu_form"] = ("Ansatsuken", "Warriors"),
        ["goku_archive_form"] = ("Saiyan", "Legends"),
    };

    public static string Generate(IReadOnlyList<string> formIds)
    {
        var known = formIds
            .Where(FormWords.ContainsKey)
            .Distinct()
            .ToList();

        if (known.Count == 0)
        {
            return "The Hollow Archive";
        }

        // Order-independent: sort so the same set yields the same name.
        known.Sort(System.StringComparer.Ordinal);

        if (known.Count == 1)
        {
            var single = FormWords[known[0]];
            return $"The {single.Adjective} {single.Noun}";
        }

        var adjective = FormWords[known[0]].Adjective;
        var noun = FormWords[known[^1]].Noun;
        return $"The {adjective} {noun}";
    }
}
