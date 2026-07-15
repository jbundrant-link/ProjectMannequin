using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjectMannequin.Data;

/// <summary>
/// Derives four short arcade stages from each existing full-route mission. The
/// original MvpWorldCatalog remains authoritative and backwards-compatible for
/// debug/smoke callers; ladder stages are deep-cloned before normalization so
/// run-time phase bursts or other mutations never leak into catalog data.
/// </summary>
public static class WorldRunCatalog
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        IncludeFields = true,
    };

    private static readonly Dictionary<string, StageBlueprint[]> Blueprints = new()
    {
        ["archive_nexus"] = new[]
        {
            new StageBlueprint("Intake Boulevard", "The Archive opens its outer gates", "archive_scout", "Index Warden Veyra", 210.0f),
            new StageBlueprint("Index Vaults", "Raiders hold the catalog stacks", "archive_raider", "Cipher Captain Rhune", 225.0f),
            new StageBlueprint("Corruption Repository", "The deepest records fight back", "archive_bruiser", "Overseer Basalt", 240.0f),
            new StageBlueprint("Knight's Reliquary", "Champion record: Archive Knight", "", "", 420.0f),
        },
        ["world_warrior_sector"] = new[]
        {
            new StageBlueprint("Dojo Approach", "The opening qualifier", "world_warrior_rookie", "Dojo Prodigy Kenzo", 210.0f),
            new StageBlueprint("Pavilion Circuit", "Challengers fill the lantern court", "world_warrior_striker", "Pavilion Ace Makoto", 225.0f),
            new StageBlueprint("Grand Tournament Floor", "Only the strongest remain", "world_warrior_grappler", "Grand Grappler Tetsu", 240.0f),
            new StageBlueprint("Champion's Courtyard", "Final challenge: Ryu", "", "", 360.0f),
        },
        ["astral_battlefront"] = new[]
        {
            new StageBlueprint("Skyfall Breach", "Invaders descend through the shattered sky", "astral_saibaman", "Saibaman Alpha", 225.0f),
            new StageBlueprint("Capsule Causeway", "The Frieza Force blocks the ascent", "astral_frieza_scout", "Vanguard Commander Lyra", 240.0f),
            new StageBlueprint("Energy Rail Convergence", "Power gathers at the summit rail", "astral_ki_captain", "Ki Captain Prime", 270.0f),
            new StageBlueprint("Tournament Summit", "Final challenge: Goku", "", "", 540.0f),
        },
    };

    public static WorldRunData CreateRun(string worldId)
    {
        var source = MvpWorldCatalog.CreateMission(worldId);
        if (!Blueprints.TryGetValue(worldId, out var blueprints))
        {
            source.StageNumber = 1;
            source.StageTitle = source.DisplayName;
            source.StageSubtitle = source.WorldDisplayName;
            source.IsFinalStage = true;
            source.RequiresFormSwapToComplete = !string.IsNullOrWhiteSpace(source.BossFormId);
            return new WorldRunData
            {
                WorldId = worldId,
                DisplayName = source.WorldDisplayName,
                Stages = new List<StageMissionData> { source },
            };
        }

        var hordes = source.Encounters
            .Where(encounter => encounter.Kind == StageEncounterKind.Horde)
            .Take(6)
            .ToArray();
        var finalBoss = source.Encounters.LastOrDefault(
            encounter => encounter.Kind == StageEncounterKind.Boss);
        if (hordes.Length != 6 || finalBoss is null)
        {
            throw new InvalidOperationException(
                $"World '{worldId}' must contain six horde encounters followed by one boss before it can form a ladder.");
        }

        var stages = new List<StageMissionData>(4);
        for (var stageIndex = 0; stageIndex < 3; stageIndex++)
        {
            var firstSource = hordes[stageIndex * 2];
            var secondSource = hordes[stageIndex * 2 + 1];
            var sourceStartX = firstSource.TriggerX - 12.0f;
            var first = Clone(firstSource);
            var second = Clone(secondSource);
            ShiftEncounter(first, -sourceStartX);
            ShiftEncounter(second, -sourceStartX);
            first.RouteChoices.Clear();
            second.RouteChoices.Clear();

            var eliteArenaMin = second.ArenaMaxX + 2.0f;
            var elite = CreateEliteEncounter(
                source,
                blueprints[stageIndex],
                stageIndex + 1,
                eliteArenaMin);
            AuthorStageSetPiece(source.WorldId, stageIndex, first, second, elite);
            var stageMaxX = elite.ArenaMaxX + 3.0f;
            var stage = CreateStageShell(
                source,
                blueprints[stageIndex],
                stageIndex + 1,
                sourceStartX,
                sourceStartX + stageMaxX,
                stageMaxX,
                isFinal: false);
            stage.Encounters = new List<StageEncounterData> { first, second, elite };
            stages.Add(stage);
        }

        var finalSourceStartX = finalBoss.TriggerX - 14.0f;
        var finalEncounter = Clone(finalBoss);
        ShiftEncounter(finalEncounter, -finalSourceStartX);
        finalEncounter.RouteChoices.Clear();
        var finalStageMaxX = MathF.Max(26.0f, finalEncounter.ArenaMaxX + 3.0f);
        var finalStage = CreateStageShell(
            source,
            blueprints[3],
            stageNumber: 4,
            sourceStartX: finalSourceStartX,
            sourceEndX: finalSourceStartX + finalStageMaxX,
            stageMaxX: finalStageMaxX,
            isFinal: true);
        finalStage.BossFormId = source.BossFormId;
        finalStage.Encounters = new List<StageEncounterData> { finalEncounter };
        stages.Add(finalStage);

        return new WorldRunData
        {
            WorldId = source.WorldId,
            DisplayName = source.WorldDisplayName,
            Stages = stages,
        };
    }

    private static StageMissionData CreateStageShell(
        StageMissionData source,
        StageBlueprint blueprint,
        int stageNumber,
        float sourceStartX,
        float sourceEndX,
        float stageMaxX,
        bool isFinal)
    {
        var stage = Clone(source);
        stage.Id = $"{source.Id}_stage_{stageNumber}";
        stage.DisplayName = blueprint.Title;
        stage.StageNumber = stageNumber;
        stage.StageTitle = blueprint.Title;
        stage.StageSubtitle = blueprint.Subtitle;
        stage.ParTimeSeconds = blueprint.ParTimeSeconds;
        var thresholdScale = isFinal ? 1.55f : 1.0f + (stageNumber - 1) * 0.12f;
        stage.RankScoreS = (int)(20000 * thresholdScale);
        stage.RankScoreA = (int)(15000 * thresholdScale);
        stage.RankScoreB = (int)(10000 * thresholdScale);
        stage.RankScoreC = (int)(6000 * thresholdScale);
        stage.StageIntroFrames = 108;
        stage.StageIntroReadyFrame = 72;
        stage.IsFinalStage = isFinal;
        stage.RequiresFormSwapToComplete = isFinal;
        stage.BossFormId = isFinal ? source.BossFormId : "";
        stage.StageMinX = 0.0f;
        stage.StageMaxX = stageMaxX;
        stage.BackgroundPanels = source.BackgroundPanels
            .Where(panel => panel.MaxX >= sourceStartX && panel.MinX <= sourceEndX)
            .Select(panel =>
            {
                var clone = Clone(panel);
                clone.MinX -= sourceStartX;
                clone.MaxX -= sourceStartX;
                return clone;
            })
            .ToList();
        ApplyBespokeStageArt(stage);
        stage.Encounters.Clear();
        return stage;
    }

    private static void ApplyBespokeStageArt(StageMissionData stage)
    {
        if (stage.WorldId != "archive_nexus" || stage.StageNumber == 1)
        {
            return;
        }

        var artStem = stage.StageNumber switch
        {
            2 => "archive_index_vaults",
            3 => "archive_corruption_repository",
            4 => "archive_knights_reliquary",
            _ => "",
        };
        if (string.IsNullOrEmpty(artStem))
        {
            return;
        }

        stage.StageTexturePath =
            $"res://Assets/Stages/ArchiveDistrict/{artStem}_backdrop_higgsfield_v1.png";
        stage.FloorTexturePath =
            $"res://Assets/Stages/ArchiveDistrict/{artStem}_floor_higgsfield_v1.png";
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = stage.StageNumber == 4 ? 26.0f : 24.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = stage.StageTexturePath,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 4.55f,
                PositionZ = -6.0f,
                ScaleYMultiplier = stage.StageNumber == 4 ? 0.90f : 0.95f,
            },
        };
    }

    private static StageEncounterData CreateEliteEncounter(
        StageMissionData source,
        StageBlueprint blueprint,
        int stageNumber,
        float arenaMinX)
    {
        const float arenaWidth = 14.0f;
        var triggerX = arenaMinX + 6.0f;
        return new StageEncounterData
        {
            Id = $"{source.WorldId}_stage_{stageNumber}_elite",
            DisplayName = blueprint.EliteName,
            Kind = StageEncounterKind.Elite,
            TriggerX = triggerX,
            ArenaMinX = arenaMinX,
            ArenaMaxX = arenaMinX + arenaWidth,
            CameraLockX = arenaMinX + arenaWidth * 0.56f,
            CameraOrthographicSize = 8.8f,
            LocksFightRoom = true,
            CameraTransitionFrames = 24,
            GateReleaseDelayFrames = 40,
            MaxActiveEnemies = 1,
            MaxSimultaneousAttackers = 1,
            MaxAttackersPerPlayer = 1,
            LightColorR = 1.0f,
            LightColorG = 0.78f,
            LightColorB = 0.34f,
            LightEnergyMultiplier = 1.16f,
            Spawns = new List<EnemySpawnData>
            {
                new()
                {
                    ArchetypeId = blueprint.EliteArchetypeId,
                    DisplayName = blueprint.EliteName,
                    EntryEdge = EnemyEntryEdge.Right,
                    EntryProfile = EnemyEntryProfile.WalkIn,
                    WarningLeadFrames = 35,
                    EntryDistance = 2.2f,
                    OffsetX = 0.0f,
                    LaneZ = 0.0f,
                },
            },
        };
    }

    private static void ShiftEncounter(StageEncounterData encounter, float deltaX)
    {
        encounter.TriggerX += deltaX;
        encounter.ArenaMinX += deltaX;
        encounter.ArenaMaxX += deltaX;
        encounter.CameraLockX += deltaX;
        foreach (var prop in encounter.Props)
        {
            prop.PositionX += deltaX;
        }

        foreach (var zone in encounter.HazardZones)
        {
            zone.MinX += deltaX;
            zone.MaxX += deltaX;
        }
    }

    private static void AuthorStageSetPiece(
        string worldId,
        int stageIndex,
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        if (worldId != "archive_nexus")
        {
            return;
        }

        if (stageIndex == 1)
        {
            AuthorIndexVaults(firstEncounter, secondEncounter, eliteEncounter);
            return;
        }

        if (stageIndex == 2)
        {
            AuthorCorruptionRepository(firstEncounter, secondEncounter, eliteEncounter);
            return;
        }

        if (stageIndex != 0)
        {
            return;
        }

        firstEncounter.Props.Add(new StagePropData
        {
            Id = "archive_intake_health_cache",
            PositionX = firstEncounter.ArenaMaxX - 2.2f,
            PositionZ = 2.15f,
            Health = 80,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "archive_intake_data_cache",
            PositionX = secondEncounter.ArenaMinX + 2.2f,
            PositionZ = -2.15f,
            Health = 95,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = ArchiveDataCacheSprite,
            SpritePixelSize = ArchiveDataCachePixelSize,
            SpriteGroundOffsetPixels = 674.0f,
        });

        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "archive_intake_tram",
            Behavior = StageHazardBehavior.LinearSweep,
            Targets = StageHazardTargetMask.All,
            MinX = secondEncounter.ArenaMinX + 0.6f,
            MaxX = secondEncounter.ArenaMinX + 2.6f,
            MinZ = -0.72f,
            MaxZ = 0.72f,
            ActivationDelayFrames = 24,
            WarningLeadFrames = 72,
            ActiveFrames = 96,
            RepeatIntervalFrames = 264,
            MovementOffsetX = 10.0f,
            WarningText = "INTAKE TRAM — CLEAR THE CENTER LANE",
            DamagePerSecond = 90.0f,
            KnockbackX = 9.0f,
            HitstunFrames = 18,
            ActiveDuringBoss = false,
        });
    }

    private static void AuthorIndexVaults(
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        // Compressed catalog stacks teach close-range crowd control, then open
        // into a wider scan chamber with risky cache pockets.
        firstEncounter.UsesLaneBounds = true;
        firstEncounter.LaneMinZ = -1.65f;
        firstEncounter.LaneMaxZ = 1.65f;
        firstEncounter.LaneTransitionFrames = 30;
        secondEncounter.UsesLaneBounds = true;
        secondEncounter.LaneMinZ = -2.75f;
        secondEncounter.LaneMaxZ = 2.75f;
        secondEncounter.LaneTransitionFrames = 30;
        eliteEncounter.UsesLaneBounds = true;
        eliteEncounter.LaneMinZ = -2.40f;
        eliteEncounter.LaneMaxZ = 2.40f;
        eliteEncounter.LaneTransitionFrames = 30;

        secondEncounter.Props.Add(new StagePropData
        {
            Id = "index_vault_health_cache",
            PositionX = secondEncounter.ArenaMinX + 2.2f,
            PositionZ = 2.35f,
            Health = 105,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "index_vault_meter_cache",
            PositionX = secondEncounter.ArenaMaxX - 2.2f,
            PositionZ = -2.35f,
            Health = 105,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = ArchiveMeterCacheSprite,
            SpritePixelSize = ArchiveMeterCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });

        // Alternating far/near scan strips never overlap. A center corridor is
        // always safe, while the off-window permits deliberate cache access.
        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "index_vault_far_scan",
            Behavior = StageHazardBehavior.StaticPulse,
            Targets = StageHazardTargetMask.All,
            MinX = secondEncounter.ArenaMinX + 0.8f,
            MaxX = secondEncounter.ArenaMaxX - 0.8f,
            MinZ = -2.55f,
            MaxZ = -0.45f,
            ActivationDelayFrames = 20,
            WarningLeadFrames = 60,
            ActiveFrames = 90,
            RepeatIntervalFrames = 340,
            WarningText = "INDEX SCAN — FAR LANE CHARGING",
            DamagePerSecond = 72.0f,
            KnockbackZ = 4.0f,
            HitstunFrames = 14,
            ActiveDuringBoss = false,
        });
        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "index_vault_near_scan",
            Behavior = StageHazardBehavior.StaticPulse,
            Targets = StageHazardTargetMask.All,
            MinX = secondEncounter.ArenaMinX + 0.8f,
            MaxX = secondEncounter.ArenaMaxX - 0.8f,
            MinZ = 0.45f,
            MaxZ = 2.55f,
            ActivationDelayFrames = 110,
            WarningLeadFrames = 60,
            ActiveFrames = 90,
            RepeatIntervalFrames = 340,
            WarningText = "INDEX SCAN — NEAR LANE CHARGING",
            DamagePerSecond = 72.0f,
            KnockbackZ = -4.0f,
            HitstunFrames = 14,
            ActiveDuringBoss = false,
        });

        var eliteSpawn = eliteEncounter.Spawns.Single();
        eliteSpawn.EntryEdge = EnemyEntryEdge.FarLane;
        eliteSpawn.EntryProfile = EnemyEntryProfile.Background;
        eliteSpawn.LaneZ = -1.85f;
        eliteSpawn.EntryDistance = 2.6f;
        eliteSpawn.WarningLeadFrames = 48;
    }

    private static void AuthorCorruptionRepository(
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        firstEncounter.UsesLaneBounds = true;
        firstEncounter.LaneMinZ = -2.60f;
        firstEncounter.LaneMaxZ = 2.60f;
        firstEncounter.LaneTransitionFrames = 28;
        secondEncounter.UsesLaneBounds = true;
        secondEncounter.LaneMinZ = -2.30f;
        secondEncounter.LaneMaxZ = 2.30f;
        secondEncounter.LaneTransitionFrames = 28;
        eliteEncounter.UsesLaneBounds = true;
        eliteEncounter.LaneMinZ = -2.55f;
        eliteEncounter.LaneMaxZ = 2.55f;
        eliteEncounter.LaneTransitionFrames = 30;

        firstEncounter.Props.Add(new StagePropData
        {
            Id = "repository_data_cache",
            PositionX = firstEncounter.ArenaMinX + 1.8f,
            PositionZ = 2.25f,
            Health = 115,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = ArchiveDataCacheSprite,
            SpritePixelSize = ArchiveDataCachePixelSize,
            SpriteGroundOffsetPixels = 674.0f,
        });
        firstEncounter.Props.Add(new StagePropData
        {
            Id = "repository_volatile_canister",
            ArchetypeId = "prop_explosive_canister",
            PositionX = firstEncounter.ArenaMaxX - 2.4f,
            PositionZ = -1.25f,
            Health = 72,
            IsThrowable = false,
            SpawnsPickupOnBreak = false,
            ExplodesOnBreak = true,
            ExplosionTargets = StageHazardTargetMask.All,
            ExplosionRadius = 3.2f,
            ExplosionDamage = 55,
            ExplosionKnockback = 8.0f,
            ExplosionHitstunFrames = 18,
            SpritePath = ArchiveVolatileCanisterSprite,
            SpritePixelSize = ArchiveVolatileCanisterPixelSize,
            SpriteGroundOffsetPixels = 854.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "repository_health_cache",
            PositionX = secondEncounter.ArenaMinX + 2.0f,
            PositionZ = -2.05f,
            Health = 120,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "repository_meter_cache",
            PositionX = secondEncounter.ArenaMaxX - 2.0f,
            PositionZ = 2.05f,
            Health = 120,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = ArchiveMeterCacheSprite,
            SpritePixelSize = ArchiveMeterCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });

        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_far",
            centerX: firstEncounter.ArenaMinX + 3.0f,
            centerZ: -1.55f,
            delay: 20,
            warning: "FALLING SHELF — FAR POCKET");
        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_near",
            centerX: firstEncounter.ArenaMinX + 8.0f,
            centerZ: 1.50f,
            delay: 65,
            warning: "FALLING SHELF — NEAR POCKET");
        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_center",
            centerX: firstEncounter.ArenaMaxX - 2.4f,
            centerZ: 0.0f,
            delay: 110,
            warning: "DATA DEBRIS — CENTER IMPACT");

        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "repository_security_sweep",
            Behavior = StageHazardBehavior.LinearSweep,
            Targets = StageHazardTargetMask.All,
            MinX = secondEncounter.ArenaMinX + 0.7f,
            MaxX = secondEncounter.ArenaMinX + 2.5f,
            MinZ = -0.70f,
            MaxZ = 0.70f,
            ActivationDelayFrames = 30,
            WarningLeadFrames = 54,
            ActiveFrames = 90,
            RepeatIntervalFrames = 260,
            MovementOffsetX = 10.8f,
            WarningText = "CORRUPTION SWEEP — CLEAR THE CENTER",
            DamagePerSecond = 86.0f,
            KnockbackX = 9.0f,
            HitstunFrames = 19,
            ActiveDuringBoss = false,
        });

        var eliteSpawn = eliteEncounter.Spawns.Single();
        eliteSpawn.EntryEdge = EnemyEntryEdge.FarLane;
        eliteSpawn.EntryProfile = EnemyEntryProfile.DropIn;
        eliteSpawn.LaneZ = 0.0f;
        eliteSpawn.EntryHeight = 5.4f;
        eliteSpawn.WarningLeadFrames = 48;
    }

    private static void AddRepositoryFallingStrike(
        StageEncounterData encounter,
        string id,
        float centerX,
        float centerZ,
        int delay,
        string warning)
    {
        encounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = id,
            Behavior = StageHazardBehavior.FallingStrike,
            Targets = StageHazardTargetMask.All,
            MinX = centerX - 1.2f,
            MaxX = centerX + 1.2f,
            MinZ = centerZ - 1.0f,
            MaxZ = centerZ + 1.0f,
            ActivationDelayFrames = delay,
            WarningLeadFrames = 45,
            ActiveFrames = 20,
            RepeatIntervalFrames = 270,
            WarningText = warning,
            DamagePerSecond = 140.0f,
            KnockbackX = 5.5f,
            HitstunFrames = 24,
            ActiveDuringBoss = false,
        });
    }

    private static T Clone<T>(T source)
    {
        return JsonSerializer.Deserialize<T>(
                   JsonSerializer.Serialize(source, CloneOptions),
                   CloneOptions)
               ?? throw new InvalidOperationException($"Could not clone {typeof(T).Name}.");
    }

    private const string ArchiveHealthCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_health_cache_style_v2.png";
    private const string ArchiveMeterCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_meter_cache_style_v2.png";
    private const string ArchiveDataCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png";
    private const string ArchiveVolatileCanisterSprite =
        "res://Assets/Sprites/Props/Archive/archive_volatile_canister_style_v2.png";
    private const float ArchiveHealthCachePixelSize = 0.00114f;
    private const float ArchiveMeterCachePixelSize = 0.00103f;
    private const float ArchiveDataCachePixelSize = 0.00136f;
    private const float ArchiveVolatileCanisterPixelSize = 0.00143f;

    private readonly record struct StageBlueprint(
        string Title,
        string Subtitle,
        string EliteArchetypeId,
        string EliteName,
        float ParTimeSeconds);
}
