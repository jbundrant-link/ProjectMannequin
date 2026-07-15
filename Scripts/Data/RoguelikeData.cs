using System.Collections.Generic;
using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

public enum MoveCardRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

public enum MoveCardType
{
    Basic,
    ComboExtension,
    Launcher,
    Projectile,
    Special,
    Ultimate
}

public sealed class MoveCardData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public MoveCardRarity Rarity { get; set; } = MoveCardRarity.Common;
    public MoveCardType CardType { get; set; } = MoveCardType.Basic;
    
    // The actual move that will be added to the character's MoveData lists
    public MoveData Move { get; set; } = new();
}

public sealed class ArtifactData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public MoveCardRarity Rarity { get; set; } = MoveCardRarity.Common;

    // Optional modifiers that automatically apply stats
    public int DamageModifierPercent { get; set; } = 0;
    public int DefenseModifierPercent { get; set; } = 0;
    public int MeterGainModifierPercent { get; set; } = 0;

    // Tier 2 build depth: themed + cursed artifacts.
    public string Theme { get; set; } = "universal";
    public int HealOnAcquirePercent { get; set; } = 0;
    public bool IsCursed { get; set; } = false;
    public int HealthDrainPerSecond { get; set; } = 0;
}
