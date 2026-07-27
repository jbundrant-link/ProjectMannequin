using System.Collections.Generic;
using System.Linq;

namespace ProjectMannequin.Data;

public static class MvpWorldCatalog
{
    public static IReadOnlyList<ArchiveWorldData> Worlds { get; } = new[]
    {
        new ArchiveWorldData
        {
            Id = "archive_nexus",
            DisplayName = "Archive Nexus",
            CombatIdentity = "Adaptive arcade combat",
            StageName = "Archive District",
            Availability = ArchiveWorldAvailability.Playable,
        },
        new ArchiveWorldData
        {
            Id = "world_warrior_sector",
            DisplayName = "World Warrior Sector",
            CombatIdentity = "Grounded martial arts and projectiles",
            StageName = "Tournament District",
            Availability = ArchiveWorldAvailability.Playable,
        },
        new ArchiveWorldData
        {
            Id = "iron_fist_foundry",
            DisplayName = "Iron Fist Foundry",
            CombatIdentity = "Launchers, stances, and heavy strikes",
            StageName = "Foundry Causeway",
            Availability = ArchiveWorldAvailability.Locked,
        },
        new ArchiveWorldData
        {
            Id = "astral_battlefront",
            DisplayName = "Astral Battlefront",
            CombatIdentity = "Aerial rushes and energy attacks",
            StageName = "Shattered Skyway",
            Availability = ArchiveWorldAvailability.Playable,
        },
        new ArchiveWorldData
        {
            Id = "training_room",
            DisplayName = "Training Room",
            CombatIdentity = "Practice makes perfect",
            StageName = "Simulation Deck",
            Availability = ArchiveWorldAvailability.Playable,
        },
    };

    public static ArchiveWorldData? FindWorld(string worldId)
    {
        return Worlds.FirstOrDefault(world => world.Id == worldId);
    }

    public static StageMissionData CreateMission(string worldId)
    {
        return worldId switch
        {
            "world_warrior_sector" => CreateWorldWarriorSectorMission(),
            "astral_battlefront" => CreateAstralBattlefrontMission(),
            "training_room" => CreateTrainingRoomMission(),
            _ => CreateArchiveDistrictMission(),
        };
    }

    public static StageMissionData CreateTrainingRoomMission()
    {
        return new StageMissionData
        {
            Id = "training_room_mvp",
            WorldId = "training_room",
            WorldDisplayName = "Training Room",
            DisplayName = "Simulation Deck",
            BossFormId = "",
            StageTexturePath = "res://Assets/Stages/ArchiveDistrict/archive_district_stage_higgsfield_v1.png", // Reusing for now
            StageMinX = 0.0f,
            StageMaxX = 24.0f,
            LaneMinZ = -3.0f,
            LaneMaxZ = 3.0f,
            CameraViewportWidth = 16.0f,
            CameraScrollSpeed = 4.6f,
            CameraFollowThresholdX = 0.45f,
            BackgroundPanels = BuildRepeatedStagePanels(
                "res://Assets/Stages/ArchiveDistrict/archive_district_stage_higgsfield_v1.png",
                0.0f,
                24.0f,
                16.0f,
                0.90f),
            Encounters = new List<StageEncounterData>
            {
                new()
                {
                    Id = "training_dummy",
                    DisplayName = "Training Dummy",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 8.0f,
                    ArenaMinX = 2.0f,
                    ArenaMaxX = 22.0f,
                    CameraLockX = 12.0f,
                    MaxActiveEnemies = 1,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("training_dummy", "Dummy", EnemyEntryEdge.Right, 0, 4.0f, 0.0f),
                    },
                }
            }
        };
    }

    public static StageMissionData CreateArchiveDistrictMission()
    {
        return new StageMissionData
        {
            Id = "archive_district_mvp",
            WorldId = "archive_nexus",
            WorldDisplayName = "Archive Nexus",
            DisplayName = "Archive District",
            BossFormId = "archive_knight_form",
            StageTexturePath =
                "res://Assets/Stages/ArchiveDistrict/archive_district_stage_higgsfield_v1.png",
            FloorTexturePath =
                "res://Assets/Stages/ArchiveDistrict/archive_district_floor_higgsfield_v1.png",
            FloorTextureTopFraction = 0.0f,
            FloorTextureTileWidth = 24.0f,
            StageMinX = 0.0f,
            StageMaxX = 102.0f,
            LaneMinZ = -3.0f,
            LaneMaxZ = 3.0f,
            CameraViewportWidth = 16.0f,
            CameraScrollSpeed = 4.6f,
            CameraFollowThresholdX = 0.45f,
            FarColorR = 0.025f,
            FarColorG = 0.055f,
            FarColorB = 0.075f,
            FloorColorR = 0.11f,
            FloorColorG = 0.15f,
            FloorColorB = 0.17f,
            LaneAccentR = 0.18f,
            LaneAccentG = 0.82f,
            LaneAccentB = 0.86f,
            BackgroundPanels = BuildRepeatedStagePanels(
                "res://Assets/Stages/ArchiveDistrict/archive_district_backdrop_band_higgsfield_v1.png",
                0.0f,
                102.0f,
                18.0f,
                0.82f,
                cropBottomFraction: 0.0f,
                positionY: 4.6f,
                scaleYMultiplier: 1.0f),
            Encounters = new List<StageEncounterData>
            {
                new()
                {
                    Id = "breach_01",
                    DisplayName = "Outer Patrol",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 12.0f,
                    ArenaMinX = 6.0f,
                    ArenaMaxX = 20.0f,
                    CameraLockX = 14.0f,
                    MaxActiveEnemies = 2,
                    Waves = new List<StageWaveData>
                    {
                        new()
                        {
                            Id = "breach_01_patrol",
                            DisplayName = "Patrol Pair",
                            Spawns = new List<EnemySpawnData>
                            {
                                Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.Right, 0, 0.0f, -1.15f),
                            },
                        },
                        new()
                        {
                            Id = "breach_01_reinforcement",
                            DisplayName = "Lane Reinforcement",
                            StartDelayFrames = 36,
                            HealthReward = 8,
                            MeterReward = 6,
                            Spawns = new List<EnemySpawnData>
                            {
                                Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.FarLane, 0, 1.1f, -1.5f),
                            },
                        },
                    },
                },
                new()
                {
                    Id = "breach_02",
                    DisplayName = "Service Alley",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 25.0f,
                    ArenaMinX = 19.0f,
                    ArenaMaxX = 33.0f,
                    CameraLockX = 27.0f,
                    MaxActiveEnemies = 2,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.Left, 0, 0.0f, 1.25f),
                        Spawn(
                            "archive_scout",
                            "Archive Scout",
                            EnemyEntryEdge.Right,
                            28,
                            0.0f,
                            -1.3f,
                            EnemyEntryProfile.DropIn),
                    },
                },
                new()
                {
                    Id = "breach_03",
                    DisplayName = "Cross-Lane Ambush",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 38.0f,
                    ArenaMinX = 32.0f,
                    ArenaMaxX = 46.0f,
                    CameraLockX = 40.0f,
                    LightColorR = 0.78f,
                    LightColorG = 0.94f,
                    LightColorB = 1.0f,
                    LightEnergyMultiplier = 1.08f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.NearLane, 0, -1.0f, 1.55f),
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.Right, 24, 0.0f, 0.15f),
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.Left, 54, 0.0f, -1.4f),
                    },
                },
                new()
                {
                    Id = "breach_04",
                    DisplayName = "Transit Platform",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 52.0f,
                    ArenaMinX = 46.0f,
                    ArenaMaxX = 60.0f,
                    CameraLockX = 54.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.Right, 0, 0.0f, -1.2f),
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.FarLane, 20, 0.8f, -1.65f),
                        Spawn(
                            "archive_bruiser",
                            "Archive Bruiser",
                            EnemyEntryEdge.Left,
                            58,
                            0.0f,
                            0.6f,
                            EnemyEntryProfile.Ambush),
                    },
                },
                new()
                {
                    Id = "breach_05",
                    DisplayName = "Archive Boulevard",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 66.0f,
                    ArenaMinX = 60.0f,
                    ArenaMaxX = 74.0f,
                    CameraLockX = 68.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.Left, 0, 0.0f, -1.55f),
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.NearLane, 32, 1.0f, 1.6f),
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.Right, 64, 0.0f, 0.25f),
                    },
                },
                new()
                {
                    Id = "breach_06",
                    DisplayName = "Gatebreaker Rush",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 80.0f,
                    ArenaMinX = 74.0f,
                    ArenaMaxX = 88.0f,
                    CameraLockX = 82.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.Right, 0, 0.0f, 1.45f),
                        Spawn("archive_raider", "Archive Raider", EnemyEntryEdge.Left, 18, 0.0f, -1.25f),
                        Spawn("archive_bruiser", "Archive Bruiser", EnemyEntryEdge.FarLane, 52, 0.6f, -1.7f),
                        Spawn("archive_scout", "Archive Scout", EnemyEntryEdge.NearLane, 90, -0.8f, 1.7f),
                    },
                },
                new()
                {
                    Id = "archive_knight_gate",
                    DisplayName = "Archive Knight",
                    Kind = StageEncounterKind.Boss,
                    TriggerX = 94.0f,
                    ArenaMinX = 88.0f,
                    ArenaMaxX = 102.0f,
                    CameraLockX = 94.0f,
                    CameraOrthographicSize = 9.1f,
                    LightColorR = 0.74f,
                    LightColorG = 0.82f,
                    LightColorB = 1.0f,
                    LightEnergyMultiplier = 1.18f,
                    MaxActiveEnemies = 1,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("archive_knight_boss", "Archive Knight", EnemyEntryEdge.Right, 0, 0.0f, 0.0f),
                    },
                },
            },
        };
    }
    public static StageMissionData CreateWorldWarriorSectorMission()
    {
        return new StageMissionData
        {
            Id = "world_warrior_sector_mvp",
            WorldId = "world_warrior_sector",
            WorldDisplayName = "World Warrior Sector",
            DisplayName = "Tournament District",
            BossFormId = "world_warrior_ryu_form",
            StageTexturePath =
                "res://Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png",
            StageTextureScaleX = 3.25f,
            StageTextureScaleY = 0.85f,
            StageTexturePositionY = 0.0f,
            StageMinX = 0.0f,
            StageMaxX = 102.0f,
            LaneMinZ = -3.0f,
            LaneMaxZ = 3.0f,
            CameraViewportWidth = 16.0f,
            CameraScrollSpeed = 4.6f,
            CameraFollowThresholdX = 0.45f,
            FarColorR = 0.075f,
            FarColorG = 0.055f,
            FarColorB = 0.05f,
            FloorColorR = 0.23f,
            FloorColorG = 0.24f,
            FloorColorB = 0.21f,
            LaneAccentR = 0.84f,
            LaneAccentG = 0.20f,
            LaneAccentB = 0.16f,
            BackgroundPanels = BuildRepeatedStagePanels(
                "res://Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png",
                0.0f,
                102.0f,
                17.0f,
                0.92f),
            Encounters = new List<StageEncounterData>
            {
                new()
                {
                    Id = "qualifier_01",
                    DisplayName = "Dojo Approach",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 12.0f,
                    ArenaMinX = 6.0f,
                    ArenaMaxX = 20.0f,
                    CameraLockX = 14.0f,
                    MaxActiveEnemies = 2,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.Right, 0, 0.0f, -1.15f),
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.FarLane, 38, 1.1f, -1.5f),
                    },
                },
                new()
                {
                    Id = "qualifier_02",
                    DisplayName = "Lantern Market",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 25.0f,
                    ArenaMinX = 19.0f,
                    ArenaMaxX = 33.0f,
                    CameraLockX = 27.0f,
                    MaxActiveEnemies = 2,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.Left, 0, 0.0f, 1.25f),
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.Right, 28, 0.0f, -1.3f),
                    },
                },
                new()
                {
                    Id = "qualifier_03",
                    DisplayName = "Cross-Street Challenge",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 38.0f,
                    ArenaMinX = 32.0f,
                    ArenaMaxX = 46.0f,
                    CameraLockX = 40.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.NearLane, 0, -1.0f, 1.55f),
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.Right, 24, 0.0f, 0.15f),
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.Left, 54, 0.0f, -1.4f),
                    },
                },
                new()
                {
                    Id = "qualifier_04",
                    DisplayName = "Training Arcade",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 52.0f,
                    ArenaMinX = 46.0f,
                    ArenaMaxX = 60.0f,
                    CameraLockX = 54.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.Right, 0, 0.0f, -1.2f),
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.FarLane, 20, 0.8f, -1.65f),
                        Spawn("world_warrior_grappler", "Tournament Grappler", EnemyEntryEdge.Left, 58, 0.0f, 0.6f),
                    },
                },
                new()
                {
                    Id = "qualifier_05",
                    DisplayName = "Challenger Boulevard",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 66.0f,
                    ArenaMinX = 60.0f,
                    ArenaMaxX = 74.0f,
                    CameraLockX = 68.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.Left, 0, 0.0f, -1.55f),
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.NearLane, 32, 1.0f, 1.6f),
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.Right, 64, 0.0f, 0.25f),
                    },
                },
                new()
                {
                    Id = "qualifier_06",
                    DisplayName = "Final Qualifier",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 80.0f,
                    ArenaMinX = 74.0f,
                    ArenaMaxX = 88.0f,
                    CameraLockX = 82.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.Right, 0, 0.0f, 1.45f),
                        Spawn("world_warrior_striker", "Street Challenger", EnemyEntryEdge.Left, 18, 0.0f, -1.25f),
                        Spawn("world_warrior_grappler", "Tournament Grappler", EnemyEntryEdge.FarLane, 52, 0.6f, -1.7f),
                        Spawn("world_warrior_rookie", "Dojo Rookie", EnemyEntryEdge.NearLane, 90, -0.8f, 1.7f),
                    },
                },
                new()
                {
                    Id = "world_warrior_champion_gate",
                    DisplayName = "Ryu",
                    Kind = StageEncounterKind.Boss,
                    BossType = BossEncounterType.IsolatedDuel,
                    SpawnPolicy = EnemySpawnPolicy.PerPlayer,
                    TriggerX = 94.0f,
                    ArenaMinX = 88.0f,
                    ArenaMaxX = 102.0f,
                    CameraLockX = 94.0f,
                    CameraOrthographicSize = 8.6f,
                    LightColorR = 1.0f,
                    LightColorG = 0.82f,
                    LightColorB = 0.68f,
                    LightEnergyMultiplier = 1.14f,
                    MaxActiveEnemies = 1,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("world_warrior_ryu_boss", "Ryu", EnemyEntryEdge.Right, 0, 0.0f, 0.0f),
                    },
                },
            },
        };
    }

    public static StageMissionData CreateAstralBattlefrontMission()
    {
        return new StageMissionData
        {
            Id = "astral_battlefront_mvp",
            WorldId = "astral_battlefront",
            WorldDisplayName = "Astral Battlefront",
            DisplayName = "Shattered Skyway",
            BossFormId = "goku_archive_form",
            StageTexturePath =
                "res://Assets/Stages/AstralBattlefront/astral_shattered_skyway_higgsfield_v1.png",
            FloorTexturePath =
                "res://Assets/Stages/AstralBattlefront/astral_skyway_floor_style_v1.png",
            FloorTextureTopFraction = 0.0f,
            FloorTextureTileWidth = 12.0f,
            StageTexturePositionY = 1.0f,
            StageMinX = 0.0f,
            StageMaxX = 116.0f,
            LaneMinZ = -3.2f,
            LaneMaxZ = 3.2f,
            CameraViewportWidth = 16.0f,
            CameraScrollSpeed = 5.0f,
            CameraFollowThresholdX = 0.42f,
            FarColorR = 0.025f,
            FarColorG = 0.035f,
            FarColorB = 0.09f,
            FloorColorR = 0.10f,
            FloorColorG = 0.13f,
            FloorColorB = 0.19f,
            LaneAccentR = 0.16f,
            LaneAccentG = 0.70f,
            LaneAccentB = 1.0f,
            PresentationMode = StagePresentationMode.CompositeTraversal,
            CompositeSegments = new List<StageCompositeSegmentData>
            {
                CompositeSegment(
                    "astral_route_01_skyfall_higgsfield_v2.png",
                    0.0f,
                    16.57f),
                CompositeSegment(
                    "astral_route_02_capsule_causeway_higgsfield_v2.png",
                    16.57f,
                    33.14f),
                CompositeSegment(
                    "astral_route_03_energy_rail_higgsfield_v2.png",
                    33.14f,
                    49.71f),
                CompositeSegment(
                    "astral_route_02_capsule_causeway_higgsfield_v2.png",
                    49.71f,
                    66.28f,
                    flipH: true),
                CompositeSegment(
                    "astral_route_03_energy_rail_higgsfield_v2.png",
                    66.28f,
                    82.85f,
                    flipH: true),
                CompositeSegment(
                    "astral_route_01_skyfall_higgsfield_v2.png",
                    82.85f,
                    99.42f,
                    flipH: true),
                CompositeSegment(
                    "astral_route_04_tournament_summit_higgsfield_v2.png",
                    99.42f,
                    116.0f),
            },
            BackgroundPanels = new List<StageBackgroundPanelData>
            {
                StagePanel(
                    "astral_route_01_skyfall_higgsfield_v2.png",
                    0.0f,
                    16.57f),
                StagePanel(
                    "astral_route_02_capsule_causeway_higgsfield_v2.png",
                    16.57f,
                    33.14f),
                StagePanel(
                    "astral_route_03_energy_rail_higgsfield_v2.png",
                    33.14f,
                    49.71f),
                StagePanel(
                    "astral_route_02_capsule_causeway_higgsfield_v2.png",
                    49.71f,
                    66.28f,
                    flipH: true),
                StagePanel(
                    "astral_route_03_energy_rail_higgsfield_v2.png",
                    66.28f,
                    82.85f,
                    flipH: true),
                StagePanel(
                    "astral_route_01_skyfall_higgsfield_v2.png",
                    82.85f,
                    99.42f,
                    flipH: true),
                StagePanel(
                    "astral_route_04_tournament_summit_higgsfield_v2.png",
                    99.42f,
                    116.0f),
            },
            Encounters = new List<StageEncounterData>
            {
                new()
                {
                    Id = "astral_breach_01",
                    DisplayName = "Crater Landing",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 12.0f,
                    ArenaMinX = 6.0f,
                    ArenaMaxX = 21.0f,
                    CameraLockX = 14.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.Right, 0, 0.0f, -1.15f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.FarLane, 22, 0.9f, -1.55f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.NearLane, 46, -0.6f, 1.55f),
                    },
                },
                new()
                {
                    Id = "astral_breach_02",
                    DisplayName = "Capsule Causeway",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 28.0f,
                    ArenaMinX = 21.0f,
                    ArenaMaxX = 36.0f,
                    CameraLockX = 29.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.Right, 0, 0.0f, -1.4f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.Left, 26, 0.0f, 1.2f),
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.FarLane, 58, 0.8f, -1.7f),
                    },
                },
                new()
                {
                    Id = "astral_breach_03",
                    DisplayName = "Broken Overpass",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 44.0f,
                    ArenaMinX = 36.0f,
                    ArenaMaxX = 52.0f,
                    CameraLockX = 45.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_frieza_heavy", "Frieza Force Heavy", EnemyEntryEdge.Right, 0, 0.0f, 0.4f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.NearLane, 24, -0.8f, 1.65f),
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.Left, 48, 0.0f, -1.45f),
                    },
                },
                new()
                {
                    Id = "astral_breach_04",
                    DisplayName = "Energy Rail",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 60.0f,
                    ArenaMinX = 52.0f,
                    ArenaMaxX = 68.0f,
                    CameraLockX = 61.0f,
                    MaxActiveEnemies = 3,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_ki_captain", "Ki Captain", EnemyEntryEdge.FarLane, 0, 0.5f, -1.6f),
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.Right, 20, 0.0f, 1.35f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.Left, 44, 0.0f, 0.0f),
                    },
                },
                new()
                {
                    Id = "astral_breach_05",
                    DisplayName = "Ruined Plaza",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 76.0f,
                    ArenaMinX = 68.0f,
                    ArenaMaxX = 84.0f,
                    CameraLockX = 77.0f,
                    MaxActiveEnemies = 4,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.Left, 0, 0.0f, -1.55f),
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.Right, 18, 0.0f, 1.45f),
                        Spawn("astral_frieza_heavy", "Frieza Force Heavy", EnemyEntryEdge.NearLane, 42, -0.7f, 1.75f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.FarLane, 66, 0.6f, -1.7f),
                    },
                },
                new()
                {
                    Id = "astral_breach_06",
                    DisplayName = "Tournament Approach",
                    Kind = StageEncounterKind.Horde,
                    TriggerX = 92.0f,
                    ArenaMinX = 84.0f,
                    ArenaMaxX = 101.0f,
                    CameraLockX = 93.0f,
                    MaxActiveEnemies = 4,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_ki_captain", "Ki Captain", EnemyEntryEdge.Right, 0, 0.0f, -1.45f),
                        Spawn("astral_frieza_heavy", "Frieza Force Heavy", EnemyEntryEdge.Left, 24, 0.0f, 0.4f),
                        Spawn("astral_frieza_scout", "Frieza Force Scout", EnemyEntryEdge.FarLane, 48, 0.8f, -1.75f),
                        Spawn("astral_saibaman", "Saibaman", EnemyEntryEdge.NearLane, 72, -0.8f, 1.75f),
                    },
                },
                new()
                {
                    Id = "astral_champion_gate",
                    DisplayName = "Goku",
                    Kind = StageEncounterKind.Boss,
                    BossType = BossEncounterType.TagTeam,
                    SpawnPolicy = EnemySpawnPolicy.PerPlayer,
                    MergeAtPhase = 1,
                    TriggerX = 108.0f,
                    ArenaMinX = 101.0f,
                    ArenaMaxX = 116.0f,
                    CameraLockX = 108.0f,
                    CameraOrthographicSize = 9.4f,
                    LightColorR = 0.68f,
                    LightColorG = 0.82f,
                    LightColorB = 1.0f,
                    LightEnergyMultiplier = 1.28f,
                    MaxActiveEnemies = 1,
                    Spawns = new List<EnemySpawnData>
                    {
                        Spawn("astral_goku_boss", "Goku", EnemyEntryEdge.Right, 0, 0.0f, 0.0f),
                    },
                },
            },
        };
    }

    private static StageBackgroundPanelData StagePanel(
        string fileName,
        float minX,
        float maxX,
        bool flipH = false)
    {
        return new StageBackgroundPanelData
        {
            TexturePath =
                $"res://Assets/Stages/AstralBattlefront/{fileName}",
            MinX = minX,
            MaxX = maxX,
            PositionY = 0.0f,
            PositionZ = -6.0f,
            Layer = StageVisualLayerKind.Midground,
            Sampling = StageTextureSampling.Linear,
            ParallaxFactorX = 0.90f,
            CropBottomFraction = 0.36f,
            AlignBottomToFloor = true,
            FlipH = flipH,
        };
    }

    private static StageCompositeSegmentData CompositeSegment(
        string fileName,
        float minX,
        float maxX,
        bool flipH = false)
    {
        return new StageCompositeSegmentData
        {
            TexturePath =
                $"res://Assets/Stages/AstralBattlefront/{fileName}",
            MinX = minX,
            MaxX = maxX,
            HorizonFraction = 0.66f,
            FloorEndFraction = 0.84f,
            BackdropPositionZ = -6.0f,
            ForegroundPositionZ = 4.2f,
            ParallaxFactorX = 0.90f,
            ForegroundParallaxFactorX = 1.08f,
            FlipH = flipH,
        };
    }

    private static List<StageBackgroundPanelData> BuildRepeatedStagePanels(
        string texturePath,
        float stageMinX,
        float stageMaxX,
        float panelWidth,
        float parallaxFactor,
        float cropBottomFraction = 0.4f,
        float positionY = 2.35f,
        float scaleYMultiplier = 1.0f)
    {
        var panels = new List<StageBackgroundPanelData>();
        var panelIndex = 0;
        for (var minX = stageMinX - panelWidth;
             minX < stageMaxX + panelWidth;
             minX += panelWidth)
        {
            panels.Add(new StageBackgroundPanelData
            {
                TexturePath = texturePath,
                MinX = minX,
                MaxX = minX + panelWidth,
                PositionY = positionY,
                PositionZ = -6.0f,
                Layer = StageVisualLayerKind.Midground,
                Sampling = StageTextureSampling.Linear,
                ParallaxFactorX = parallaxFactor,
                // Backdrop shows buildings/skyline only; the road lives on the
                // walkable floor plane so fighters stay grounded in every lane.
                ScaleYMultiplier = scaleYMultiplier,
                CropBottomFraction = cropBottomFraction,
                // Mirror every other panel so adjacent panels share identical
                // edge pixels. This yields a seamless, continuously flowing
                // skyline (a classic belt-scroll trick used by Final Fight/TMNT)
                // instead of hard-seamed duplicate copies of the same image.
                FlipH = (panelIndex % 2) == 1,
            });
            panelIndex++;
        }

        return panels;
    }


    private static EnemySpawnData Spawn(
        string archetypeId,
        string displayName,
        EnemyEntryEdge entryEdge,
        int spawnDelayFrames,
        float offsetX,
        float laneZ,
        EnemyEntryProfile entryProfile = EnemyEntryProfile.Auto)
    {
        return new EnemySpawnData
        {
            ArchetypeId = archetypeId,
            DisplayName = displayName,
            EntryEdge = entryEdge,
            EntryProfile = entryProfile,
            SpawnDelayFrames = spawnDelayFrames,
            OffsetX = offsetX,
            LaneZ = laneZ,
        };
    }
}
