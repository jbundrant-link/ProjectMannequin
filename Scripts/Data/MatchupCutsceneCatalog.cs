using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Data;

/// <summary>
/// Presentation content + timing for a pre-fight matchup cutscene (the SF4 /
/// DBFZ style "rival intro" that plays before the READY/FIGHT hype beats). The
/// data lives here so the deterministic simulation can read <see cref="MatchupCutsceneData.DurationFrames"/>
/// to size the frozen cutscene window, while the presentation layer reads the
/// title/taunt/accent to build the overlay.
/// </summary>
public sealed class MatchupCutsceneData
{
    /// <summary>Frames the simulation stays frozen on the cutscene before the
    /// cinematic build-up (Phase 1) begins. ~60 = 1s.</summary>
    public int DurationFrames { get; set; } = 240;

    public string Subtitle { get; set; } = "";
    public string Taunt { get; set; } = "";
    public Color AccentColor { get; set; } = new(1.0f, 0.36f, 0.30f);
}

/// <summary>
/// Registry of matchup cutscenes keyed by the fighters facing off. Look-up tries
/// the specific <c>player&gt;boss</c> pairing first (a true "rival" intro), then
/// falls back to a boss-only entry (<c>&gt;boss</c>) so every listed boss gets a
/// dramatic entrance regardless of which archived form the player brings.
/// Missing entry =&gt; no cutscene (straight into the standard hype sequence).
/// </summary>
public static class MatchupCutsceneCatalog
{
    private static readonly Dictionary<string, MatchupCutsceneData> Entries = new()
    {
        // World Warrior boss — plays for any player form facing him.
        [">world_warrior_ryu_boss"] = new MatchupCutsceneData
        {
            DurationFrames = 255,
            Subtitle = "THE WANDERING WORLD WARRIOR",
            Taunt = "\"Show me the true depth of your resolve.\"",
            AccentColor = new Color(0.96f, 0.82f, 0.44f),
        },

        // Astral Battlefront boss.
        [">astral_goku_boss"] = new MatchupCutsceneData
        {
            DurationFrames = 255,
            Subtitle = "THE ASTRAL SAIYAN",
            Taunt = "\"Let's see what you've got — give it everything!\"",
            AccentColor = new Color(1.0f, 0.62f, 0.16f),
        },

        // Archive District / Nexus boss.
        [">archive_knight_boss"] = new MatchupCutsceneData
        {
            DurationFrames = 255,
            Subtitle = "WARDEN OF THE HOLLOW ARCHIVE",
            Taunt = "\"None may overwrite what the Archive has sealed.\"",
            AccentColor = new Color(0.66f, 0.52f, 0.92f),
        },
    };

    public static MatchupCutsceneData? TryGet(string playerFormId, string bossFormId)
    {
        if (string.IsNullOrWhiteSpace(bossFormId))
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(playerFormId)
            && Entries.TryGetValue($"{playerFormId}>{bossFormId}", out var rival))
        {
            return rival;
        }

        return Entries.TryGetValue($">{bossFormId}", out var byBoss) ? byBoss : null;
    }
}
