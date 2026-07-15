using System.Collections.Generic;

namespace ProjectMannequin.Progression;

/// <summary>
/// Library of run artifacts. Artifacts passively modify the player's combat
/// stats for the rest of the run; cursed artifacts pair a strong upside with a
/// drawback (health drain). Artifacts are world-themed for flavour.
/// </summary>
public static class ArtifactCatalog
{
    public static readonly Dictionary<string, ArtifactData> Catalog = new();

    static ArtifactCatalog()
    {
        // --- Dragon Ball themed ---
        Add(new ArtifactData
        {
            Id = "gravity_training",
            DisplayName = "Gravity Training",
            Description = "+20% damage, but -10% defense.",
            Rarity = MoveCardRarity.Rare,
            Theme = "dragon_ball",
            DamageModifierPercent = 20,
            DefenseModifierPercent = -10,
        });
        Add(new ArtifactData
        {
            Id = "senzu_extract",
            DisplayName = "Senzu Extract",
            Description = "+25% meter gain.",
            Rarity = MoveCardRarity.Uncommon,
            Theme = "dragon_ball",
            MeterGainModifierPercent = 25,
        });
        Add(new ArtifactData
        {
            Id = "senzu_bean",
            DisplayName = "Senzu Bean",
            Description = "Instantly restores all health when acquired.",
            Rarity = MoveCardRarity.Epic,
            Theme = "dragon_ball",
            HealOnAcquirePercent = 100,
        });
        Add(new ArtifactData
        {
            Id = "kaioken_charm",
            DisplayName = "Kaioken Charm",
            Description = "+30% damage, but slowly drains your health. (Cursed)",
            Rarity = MoveCardRarity.Epic,
            Theme = "dragon_ball",
            IsCursed = true,
            DamageModifierPercent = 30,
            HealthDrainPerSecond = 3,
        });

        // --- Street Fighter themed ---
        Add(new ArtifactData
        {
            Id = "parry_master",
            DisplayName = "Parry Master",
            Description = "+18% defense.",
            Rarity = MoveCardRarity.Rare,
            Theme = "street_fighter",
            DefenseModifierPercent = 18,
        });
        Add(new ArtifactData
        {
            Id = "revenge_gauge",
            DisplayName = "Revenge Gauge",
            Description = "+35% damage, but -20% defense. (Cursed)",
            Rarity = MoveCardRarity.Epic,
            Theme = "street_fighter",
            IsCursed = true,
            DamageModifierPercent = 35,
            DefenseModifierPercent = -20,
        });

        // --- Universal ---
        Add(new ArtifactData
        {
            Id = "iron_hide",
            DisplayName = "Iron Hide",
            Description = "+25% defense, but -8% damage.",
            Rarity = MoveCardRarity.Uncommon,
            Theme = "universal",
            DefenseModifierPercent = 25,
            DamageModifierPercent = -8,
        });
        Add(new ArtifactData
        {
            Id = "focus_lens",
            DisplayName = "Focus Lens",
            Description = "+15% damage and +10% meter gain.",
            Rarity = MoveCardRarity.Uncommon,
            Theme = "universal",
            DamageModifierPercent = 15,
            MeterGainModifierPercent = 10,
        });
        Add(new ArtifactData
        {
            Id = "berserker_band",
            DisplayName = "Berserker Band",
            Description = "+40% damage, -10% defense, drains health. (Cursed)",
            Rarity = MoveCardRarity.Legendary,
            Theme = "universal",
            IsCursed = true,
            DamageModifierPercent = 40,
            DefenseModifierPercent = -10,
            HealthDrainPerSecond = 4,
        });

        // --- Expanded pool: varied curse shapes so builds diverge ---
        Add(new ArtifactData
        {
            Id = "hyperbolic_chamber",
            DisplayName = "Hyperbolic Chamber",
            Description = "+28% damage, but -30% meter gain. (Cursed)",
            Rarity = MoveCardRarity.Rare,
            Theme = "dragon_ball",
            IsCursed = true,
            DamageModifierPercent = 28,
            MeterGainModifierPercent = -30,
        });
        Add(new ArtifactData
        {
            Id = "power_pole",
            DisplayName = "Power Pole",
            Description = "+22% damage and +8% meter gain.",
            Rarity = MoveCardRarity.Epic,
            Theme = "dragon_ball",
            DamageModifierPercent = 22,
            MeterGainModifierPercent = 8,
        });
        Add(new ArtifactData
        {
            Id = "worn_gi",
            DisplayName = "Worn Training Gi",
            Description = "+22% defense, but slowly drains health. (Cursed)",
            Rarity = MoveCardRarity.Rare,
            Theme = "street_fighter",
            IsCursed = true,
            DefenseModifierPercent = 22,
            HealthDrainPerSecond = 2,
        });
        Add(new ArtifactData
        {
            Id = "hado_focus",
            DisplayName = "Hadou Focus",
            Description = "+14% damage and +12% defense.",
            Rarity = MoveCardRarity.Rare,
            Theme = "street_fighter",
            DamageModifierPercent = 14,
            DefenseModifierPercent = 12,
        });
        Add(new ArtifactData
        {
            Id = "tenacity_totem",
            DisplayName = "Tenacity Totem",
            Description = "+30% defense, but -12% meter gain.",
            Rarity = MoveCardRarity.Uncommon,
            Theme = "universal",
            DefenseModifierPercent = 30,
            MeterGainModifierPercent = -12,
        });
        Add(new ArtifactData
        {
            Id = "reckless_charm",
            DisplayName = "Reckless Charm",
            Description = "+50% damage, but -35% defense. (Cursed)",
            Rarity = MoveCardRarity.Legendary,
            Theme = "universal",
            IsCursed = true,
            DamageModifierPercent = 50,
            DefenseModifierPercent = -35,
        });
    }

    private static void Add(ArtifactData artifact)
    {
        Catalog[artifact.Id] = artifact;
    }

    public static ArtifactData? GetArtifact(string id)
    {
        return Catalog.TryGetValue(id, out var artifact) ? artifact : null;
    }
}
