using System.Collections.Generic;

namespace ProjectMannequin.Data;

/// <summary>
/// Data definition for the Archive Hub — the meta-progression lobby between runs.
/// </summary>
public sealed class ArchiveHubData
{
    public string HubId { get; set; } = "archive_hub";
    public string DisplayName { get; set; } = "The Form Archive";

    /// <summary>Number of trophy pedestals available for unlocked boss forms.</summary>
    public int MaxTrophyPedestals { get; set; } = 12;

    /// <summary>The texture path for the hub background.</summary>
    public string BackgroundTexturePath { get; set; } = "res://Assets/Sprites/Backgrounds/Hub_Archive_bg.png";

    /// <summary>Lore fragments unlocked across all runs.</summary>
    public List<string> LoreFragments { get; set; } = new();
}
