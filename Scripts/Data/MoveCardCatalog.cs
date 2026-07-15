using System.Collections.Generic;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

/// <summary>
/// Library of run-acquirable Move Cards. When a card is picked as a run reward,
/// its <see cref="MoveCardData.Move"/> is injected into the player's blank
/// mannequin form so it becomes usable immediately for the rest of the run.
/// Cards use motion inputs so they never collide with the mannequin's normals.
/// </summary>
public static class MoveCardCatalog
{
    public static readonly Dictionary<string, MoveCardData> Catalog = new();

    static MoveCardCatalog()
    {
        Add(new MoveCardData
        {
            Id = "card_rising_knuckle",
            DisplayName = "Rising Knuckle",
            Rarity = MoveCardRarity.Uncommon,
            CardType = MoveCardType.Launcher,
            Move = new MoveData
            {
                Id = "card_move_rising_knuckle",
                DisplayName = "Rising Knuckle",
                InputCommand = "623LP",
                Priority = 70,
                StartupFrames = 6,
                ActiveFrames = 6,
                RecoveryFrames = 24,
                Damage = 20,
                HitstunFrames = 30,
                BlockstunFrames = 12,
                HitStopFrames = 7,
                MeterGain = 10,
                IsLauncher = true,
                LaunchY = 8.0f,
                PushbackX = 1.0f,
                Tags = new List<string> { "special", "launcher" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    Hitbox("rising_knuckle", 6, 11, 0.9f, 1.6f, 1.0f, 1.9f),
                },
            },
        });

        Add(new MoveCardData
        {
            Id = "card_spin_kick",
            DisplayName = "Spin Kick",
            Rarity = MoveCardRarity.Common,
            CardType = MoveCardType.ComboExtension,
            Move = new MoveData
            {
                Id = "card_move_spin_kick",
                DisplayName = "Spin Kick",
                InputCommand = "214LK",
                Priority = 55,
                StartupFrames = 8,
                ActiveFrames = 6,
                RecoveryFrames = 16,
                Damage = 18,
                HitstunFrames = 22,
                BlockstunFrames = 12,
                HitStopFrames = 6,
                MeterGain = 8,
                PushbackX = 3.0f,
                CancelStartFrame = 10,
                CancelEndFrame = 20,
                CancelTags = new List<string> { "special" },
                Tags = new List<string> { "special", "normal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    Hitbox("spin_kick", 8, 13, 1.1f, 1.05f, 1.35f, 1.1f),
                },
            },
        });

        Add(new MoveCardData
        {
            Id = "card_gut_buster",
            DisplayName = "Gut Buster",
            Rarity = MoveCardRarity.Common,
            CardType = MoveCardType.ComboExtension,
            Move = new MoveData
            {
                Id = "card_move_gut_buster",
                DisplayName = "Gut Buster",
                InputCommand = "214LP",
                Priority = 52,
                StartupFrames = 7,
                ActiveFrames = 4,
                RecoveryFrames = 14,
                Damage = 16,
                HitstunFrames = 20,
                BlockstunFrames = 11,
                HitStopFrames = 5,
                MeterGain = 8,
                PushbackX = 2.2f,
                CancelStartFrame = 8,
                CancelEndFrame = 16,
                CancelTags = new List<string> { "special" },
                Tags = new List<string> { "special", "normal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    Hitbox("gut_buster", 7, 10, 1.0f, 0.9f, 1.2f, 0.9f),
                },
            },
        });

        Add(new MoveCardData
        {
            Id = "card_crushing_blow",
            DisplayName = "Crushing Blow",
            Rarity = MoveCardRarity.Rare,
            CardType = MoveCardType.Special,
            Move = new MoveData
            {
                Id = "card_move_crushing_blow",
                DisplayName = "Crushing Blow",
                InputCommand = "236HP",
                Priority = 78,
                StartupFrames = 12,
                ActiveFrames = 5,
                RecoveryFrames = 26,
                Damage = 32,
                HitstunFrames = 30,
                BlockstunFrames = 16,
                HitStopFrames = 9,
                MeterGain = 12,
                PushbackX = 4.0f,
                CausesWallBounce = true,
                Tags = new List<string> { "special", "heavy" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    Hitbox("crushing_blow", 12, 16, 1.2f, 1.1f, 1.5f, 1.4f),
                },
            },
        });

        Add(new MoveCardData
        {
            Id = "card_energy_bolt",
            DisplayName = "Energy Bolt",
            Rarity = MoveCardRarity.Uncommon,
            CardType = MoveCardType.Projectile,
            Move = new MoveData
            {
                Id = "card_move_energy_bolt",
                DisplayName = "Energy Bolt",
                InputCommand = "236LP",
                Priority = 74,
                StartupFrames = 12,
                ActiveFrames = 1,
                RecoveryFrames = 24,
                Damage = 16,
                HitstunFrames = 24,
                BlockstunFrames = 14,
                HitStopFrames = 5,
                MeterGain = 9,
                GuardDamage = 5,
                Tags = new List<string> { "special", "projectile", "zoning" },
                ProjectileSpawns = new List<ProjectileSpawnData>
                {
                    new()
                    {
                        Id = "card_energy_bolt_projectile",
                        SpawnFrame = 12,
                        OffsetX = 1.15f,
                        OffsetY = 1.15f,
                        VelocityX = 8.4f,
                        LifetimeFrames = 100,
                        SizeX = 0.82f,
                        SizeY = 0.72f,
                        SizeZ = 0.76f,
                        VisualType = ProjectileVisualType.Orb,
                        VisualColor = new Color(0.42f, 0.86f, 1.0f),
                        EmissionColor = new Color(0.16f, 0.5f, 1.0f),
                        ClashStrength = 1,
                    },
                },
            },
        });

        Add(new MoveCardData
        {
            Id = "card_meteor_finish",
            DisplayName = "Meteor Finish",
            Rarity = MoveCardRarity.Legendary,
            CardType = MoveCardType.Ultimate,
            Move = new MoveData
            {
                Id = "card_move_meteor_finish",
                DisplayName = "Meteor Finish",
                InputCommand = "236HK",
                Priority = 112,
                StartupFrames = 9,
                ActiveFrames = 6,
                RecoveryFrames = 34,
                Damage = 60,
                HitstunFrames = 42,
                BlockstunFrames = 22,
                HitStopFrames = 12,
                SuperFreezeFrames = 22,
                IsSuper = true,
                IsCinematicSuper = true,
                MeterCost = 100,
                MeterGain = 0,
                MinimumDamageScale = 0.52f,
                PushbackX = 3.0f,
                CausesHardKnockdown = true,
                Tags = new List<string> { "super", "finisher" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    Hitbox("meteor_finish", 9, 14, 1.25f, 1.2f, 1.7f, 1.8f),
                },
            },
        });
    }

    private static void Add(MoveCardData card)
    {
        Catalog[card.Id] = card;
    }

    private static CombatBoxDefinition Hitbox(
        string id,
        int startFrame,
        int endFrame,
        float offsetX,
        float offsetY,
        float sizeX,
        float sizeY)
    {
        return new CombatBoxDefinition
        {
            Id = id,
            BoxType = CombatBoxType.Hitbox,
            StartFrame = startFrame,
            EndFrame = endFrame,
            OffsetX = offsetX,
            OffsetY = offsetY,
            OffsetZ = 0.0f,
            SizeX = sizeX,
            SizeY = sizeY,
            SizeZ = 1.1f,
        };
    }

    public static MoveCardData? GetCard(string id)
    {
        return Catalog.TryGetValue(id, out var card) ? card : null;
    }
}
