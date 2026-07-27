using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;

using ProjectMannequin.LocalInput;

namespace ProjectMannequin.UI;

/// <summary>
/// Translates the project's fighting-game command notation (for example
/// "236LP") into the actual controls the local player has to press, instead of
/// abstract LP/MP/HP tokens.
/// </summary>
/// <remarks>
/// Labels follow whichever device Player 1 is using: keyboard keys read from
/// the live <see cref="InputMap"/> so they match what <c>LocalInputManager</c>
/// registered, or the printed pad glyph for the active controller family. Both
/// resolve through <see cref="InputGlyphs"/>.
///
/// Directional motions are shown facing right (the standard move-list
/// convention): 6 = forward, 4 = back.
/// </remarks>
public static class InputCommandFormatter
{
    private static readonly Dictionary<int, string> DirectionArrows = new()
    {
        [1] = "↙",
        [2] = "↓",
        [3] = "↘",
        [4] = "←",
        [6] = "→",
        [7] = "↖",
        [8] = "↑",
        [9] = "↗",
    };

    // Two-letter attack tokens -> (input action, default key when unbound).
    private static readonly Dictionary<string, (string Action, string Fallback)> AttackButtons = new()
    {
        ["LP"] = ("p1_lp", "J"),
        ["MP"] = ("p1_mp", "K"),
        ["HP"] = ("p1_hp", "L"),
        ["LK"] = ("p1_lk", "U"),
        ["MK"] = ("p1_mk", "I"),
        ["HK"] = ("p1_hk", "O"),
    };

    // Legacy single-letter tokens accepted by the command grammar.
    private static readonly Dictionary<char, (string Action, string Fallback)> LegacyButtons = new()
    {
        ['J'] = ("p1_jump", "Space"),
        ['B'] = ("p1_block", "B"),
        ['G'] = ("p1_grab", "H"),
        ['D'] = ("p1_dash", "E"),
        ['F'] = ("p1_form_swap", "Q"),
        ['A'] = ("p1_assist", "R"),
    };

    /// <summary>
    /// Returns the label currently bound to an input action for whatever device
    /// Player 1 is using: the keyboard key (for example "J" for "p1_lp"), or the
    /// printed pad glyph when a controller is active.
    /// </summary>
    public static string KeyFor(string action, string fallback) =>
        InputGlyphs.LabelForAction(action, fallback);

    /// <summary>
    /// Converts a command notation string into a display string for the active
    /// device.
    /// </summary>
    public static string ToDisplayCommand(string notation)
    {
        if (string.IsNullOrWhiteSpace(notation))
        {
            return "";
        }

        var normalized = notation.Trim().ToUpperInvariant();
        var directions = new List<int>();
        var buttons = new List<string>();

        for (var index = 0; index < normalized.Length;)
        {
            var raw = normalized[index];
            if (char.IsWhiteSpace(raw) || raw == '+' || raw == ',' || raw == '>')
            {
                index++;
                continue;
            }

            if (raw >= '1' && raw <= '9')
            {
                if (raw != '5')
                {
                    directions.Add(raw - '0');
                }

                index++;
                continue;
            }

            if (index + 1 < normalized.Length
                && (raw is 'L' or 'M' or 'H')
                && normalized[index + 1] is 'P' or 'K')
            {
                var token = normalized.Substring(index, 2);
                var (action, fallback) = AttackButtons[token];
                buttons.Add(KeyFor(action, fallback));
                index += 2;
                continue;
            }

            if (LegacyButtons.TryGetValue(raw, out var legacy))
            {
                buttons.Add(KeyFor(legacy.Action, legacy.Fallback));
            }

            index++;
        }

        var builder = new StringBuilder();
        if (directions.Count == 1 && directions[0] == 2)
        {
            // A lone 2 is a crouching normal in the authored command grammar.
            // It uses the dedicated crouch key, unlike the 2 inside 236/214
            // motions, which uses the S/down directional input.
            builder.Append(KeyFor("p1_crouch", "C"));
        }
        else if (directions.Count > 0)
        {
            var arrows = directions
                .Select(direction => DirectionArrows.TryGetValue(direction, out var arrow) ? arrow : "")
                .Where(arrow => arrow.Length > 0);
            builder.Append(string.Join(" ", arrows));
        }

        if (buttons.Count > 0)
        {
            if (builder.Length > 0)
            {
                builder.Append("  +  ");
            }

            builder.Append(string.Join(" + ", buttons));
        }

        return builder.ToString();
    }

    /// <summary>Legend line mapping direction arrows to their movement keys.</summary>
    public static string DirectionLegend()
    {
        return $"Motions:   ↑ = {KeyFor("p1_up", "W")}    ← = {KeyFor("p1_left", "A")}"
            + $"    ↓ = {KeyFor("p1_down", "S")}    → = {KeyFor("p1_right", "D")}"
            + $"     (Crouching normals = {KeyFor("p1_crouch", "C")})";
    }

    /// <summary>Legend line mapping the six attack buttons to their keys.</summary>
    public static string ButtonLegend()
    {
        return $"Punches:  {KeyFor("p1_lp", "J")} {KeyFor("p1_mp", "K")} {KeyFor("p1_hp", "L")}  =  Light / Medium / Heavy"
            + $"      Kicks:  {KeyFor("p1_lk", "U")} {KeyFor("p1_mk", "I")} {KeyFor("p1_hk", "O")}  =  Light / Medium / Heavy";
    }

    /// <summary>Legend line for the remaining system buttons.</summary>
    public static string SystemLegend()
    {
        return $"Jump = {KeyFor("p1_jump", "Space")}    Dash = {KeyFor("p1_dash", "E")}"
            + $"    Grab = {KeyFor("p1_grab", "H")}    Block = {KeyFor("p1_block", "B")}"
            + $"    Form Swap = {KeyFor("p1_form_swap", "Q")}";
    }
}
