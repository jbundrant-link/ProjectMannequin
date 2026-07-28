using System.Collections.Generic;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

public static class HazardRosterFactory
{
    public static CharacterData CreateBoulderHazard()
    {
        return new CharacterData
        {
            Id = "hazard_boulder",
            DisplayName = "Boulder",
            Role = CharacterRole.Striker, // Using Striker so it doesn't count as a Boss or Minion for stage clear
            MaxHealth = 300,
            WalkSpeed = 0.0f,
            DashSpeed = 0.0f,
            JumpVelocity = 0.0f,
            Gravity = 30.0f,
            Weight = 5.0f, // Heavy, hard to launch
            MaxGuardGauge = 0,
            SpriteSheetPath = "res://Assets/Sprites/Hazards/boulder.png",
            SpriteSheetColumns = 1,
            SpriteSheetRows = 1,
            SpritePixelSize = 0.03f,
            SpriteGroundOffsetPixels = 32.0f,
            AnimationProfileId = "boulder", // Doesn't need to animate much
            TintSpriteSheet = false,
            RoleTags = new List<string> { "hazard" },
            Moves = new List<MoveData>(), // Cannot attack
            Pushbox = new CombatBoxDefinition { SizeX = 1.0f, SizeY = 1.0f, SizeZ = 1.0f, OffsetY = 0.5f, BoxType = CombatBoxType.Pushbox, Id = "boulder_pushbox" },
            CrouchingPushbox = new CombatBoxDefinition { SizeX = 1.0f, SizeY = 1.0f, SizeZ = 1.0f, OffsetY = 0.5f, BoxType = CombatBoxType.Pushbox, Id = "boulder_crouch_pushbox" },
            Hurtbox = new CombatBoxDefinition { SizeX = 1.2f, SizeY = 1.2f, SizeZ = 1.2f, OffsetY = 0.6f, BoxType = CombatBoxType.Hurtbox, Id = "boulder_hurtbox" },
            CrouchingHurtbox = new CombatBoxDefinition { SizeX = 1.2f, SizeY = 1.2f, SizeZ = 1.2f, OffsetY = 0.6f, BoxType = CombatBoxType.Hurtbox, Id = "boulder_crouch_hurtbox" },
        };
    }

    public static CharacterData CreateBreakableCrate()
    {
        return new CharacterData
        {
            Id = "prop_crate",
            DisplayName = "Crate",
            Role = CharacterRole.Striker,
            MaxHealth = 100,
            WalkSpeed = 0.0f,
            DashSpeed = 0.0f,
            JumpVelocity = 0.0f,
            Gravity = 30.0f,
            Weight = 2.0f, 
            MaxGuardGauge = 0,
            SpriteSheetPath = "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png",
            SpriteSheetColumns = 1,
            SpriteSheetRows = 1,
            SpritePixelSize = 0.00136f,
            SpriteGroundOffsetPixels = 674.0f,
            AnimationProfileId = "prop",
            TintSpriteSheet = false,
            RoleTags = new List<string> { "hazard", "breakable" },
            Moves = new List<MoveData>(), // Cannot attack
            Pushbox = new CombatBoxDefinition { SizeX = 1.0f, SizeY = 1.0f, SizeZ = 1.0f, OffsetY = 0.5f, BoxType = CombatBoxType.Pushbox, Id = "crate_pushbox" },
            CrouchingPushbox = new CombatBoxDefinition { SizeX = 1.0f, SizeY = 1.0f, SizeZ = 1.0f, OffsetY = 0.5f, BoxType = CombatBoxType.Pushbox, Id = "crate_crouch_pushbox" },
            Hurtbox = new CombatBoxDefinition { SizeX = 1.2f, SizeY = 1.2f, SizeZ = 1.2f, OffsetY = 0.6f, BoxType = CombatBoxType.Hurtbox, Id = "crate_hurtbox" },
            CrouchingHurtbox = new CombatBoxDefinition { SizeX = 1.2f, SizeY = 1.2f, SizeZ = 1.2f, OffsetY = 0.6f, BoxType = CombatBoxType.Hurtbox, Id = "crate_crouch_hurtbox" },
        };
    }

    public static CharacterData CreateExplosiveCanister()
    {
        var canister = CreateBreakableCrate();
        canister.Id = "prop_explosive_canister";
        canister.DisplayName = "Volatile Data Canister";
        canister.SpriteSheetPath =
            "res://Assets/Sprites/Props/Archive/archive_volatile_canister_style_v2.png";
        canister.SpritePixelSize = 0.00143f;
        canister.SpriteGroundOffsetPixels = 854.0f;
        canister.TintSpriteSheet = false;
        canister.RoleTags.Add("explosive");
        return canister;
    }

    public static CharacterData CreateWorldWarriorTrainingDummy()
    {
        var dummy = CreateBreakableCrate();
        dummy.Id = "world_warrior_training_dummy";
        dummy.DisplayName = "Training Dummy";
        dummy.SpriteSheetPath =
            "res://Assets/Sprites/Props/WorldWarrior/world_warrior_training_dummy_style_v1.png";
        dummy.SpritePixelSize = 0.00147291f;
        dummy.SpriteGroundOffsetPixels = 968.0f;
        dummy.RoleTags.Add("world_warrior");
        dummy.RoleTags.Add("training_prop");
        return dummy;
    }

    public static CharacterData CreateWorldWarriorSupplyCrate()
    {
        var crate = CreateBreakableCrate();
        crate.Id = "world_warrior_supply_crate";
        crate.DisplayName = "Sparring Supply Crate";
        crate.SpriteSheetPath =
            "res://Assets/Sprites/Props/WorldWarrior/world_warrior_supply_crate_style_v1.png";
        crate.SpritePixelSize = 0.00079027f;
        crate.SpriteGroundOffsetPixels = 848.0f;
        crate.RoleTags.Add("world_warrior");
        crate.RoleTags.Add("supply_prop");
        return crate;
    }

    public static CharacterData CreateWorldWarriorPavilionRackChest()
    {
        var crate = CreateBreakableCrate();
        crate.Id = "world_warrior_pavilion_rack_chest";
        crate.DisplayName = "Pavilion Rack Chest";
        crate.SpriteSheetPath =
            "res://Assets/Sprites/Props/WorldWarrior/world_warrior_pavilion_rack_chest_style_v1.png";
        crate.SpritePixelSize = 0.00132653f;
        crate.SpriteGroundOffsetPixels = 985.0f;
        crate.RoleTags.Add("world_warrior");
        crate.RoleTags.Add("supply_prop");
        return crate;
    }

    public static CharacterData CreateWorldWarriorGrandTournamentTrophyPodium()
    {
        var crate = CreateBreakableCrate();
        crate.Id = "world_warrior_grand_tournament_trophy_podium";
        crate.DisplayName = "Champion's Trophy Podium";
        crate.SpriteSheetPath =
            "res://Assets/Sprites/Props/WorldWarrior/world_warrior_grand_tournament_trophy_podium_style_v1.png";
        crate.SpritePixelSize = 0.00133721f;
        crate.SpriteGroundOffsetPixels = 887.0f;
        crate.RoleTags.Add("world_warrior");
        crate.RoleTags.Add("supply_prop");
        return crate;
    }

    public static CharacterData CreateWorldWarriorChampionsCourtyardLanternUrn()
    {
        var crate = CreateBreakableCrate();
        crate.Id = "world_warrior_champions_courtyard_lantern_urn";
        crate.DisplayName = "Champion's Lantern Urn";
        crate.SpriteSheetPath =
            "res://Assets/Sprites/Props/WorldWarrior/world_warrior_champions_courtyard_lantern_urn_style_v1.png";
        crate.SpritePixelSize = 0.0013432f;
        crate.SpriteGroundOffsetPixels = 909.0f;
        crate.RoleTags.Add("world_warrior");
        crate.RoleTags.Add("supply_prop");
        return crate;
    }

    public static CharacterData CreateMeterPickup()
    {
        return CreatePickup(
            "pickup_meter",
            "Meter Shard",
            "res://Assets/Sprites/Pickups/Archive/archive_meter_pickup_style_v2.png",
            0.00028f,
            876.0f);
    }

    public static CharacterData CreateHealthPickup()
    {
        return CreatePickup(
            "pickup_health",
            "Archive Vital",
            "res://Assets/Sprites/Pickups/Archive/archive_health_pickup_style_v2.png",
            0.00038f,
            672.0f);
    }

    public static CharacterData CreateWorldWarriorHealthPickup()
    {
        var pickup = CreatePickup(
            "pickup_health",
            "Vitality Gourd",
            "res://Assets/Sprites/Pickups/WorldWarrior/world_warrior_health_pickup_style_v1.png",
            0.00038125f,
            972.0f);
        pickup.RoleTags.Add("world_warrior");
        pickup.RoleTags.Add("health_pickup");
        return pickup;
    }

    public static CharacterData CreateWorldWarriorMeterPickup()
    {
        var pickup = CreatePickup(
            "pickup_meter",
            "Focus Drum",
            "res://Assets/Sprites/Pickups/WorldWarrior/world_warrior_meter_pickup_style_v1.png",
            0.00052895f,
            700.0f);
        pickup.RoleTags.Add("world_warrior");
        pickup.RoleTags.Add("meter_pickup");
        return pickup;
    }

    public static CharacterData CreateScorePickup()
    {
        return CreatePickup(
            "pickup_score",
            "Archive Data",
            "res://Assets/Sprites/Pickups/Archive/archive_data_pickup_style_v2.png",
            0.00036f,
            698.0f);
    }

    public static CharacterData CreateWorldWarriorScorePickup()
    {
        var pickup = CreatePickup(
            "pickup_score",
            "Judge's Laurel Fan",
            "res://Assets/Sprites/Pickups/WorldWarrior/world_warrior_score_pickup_style_v1.png",
            0.00042676f,
            854.0f);
        pickup.RoleTags.Add("world_warrior");
        pickup.RoleTags.Add("score_pickup");
        return pickup;
    }

    private static CharacterData CreatePickup(
        string id,
        string displayName,
        string spriteSheetPath,
        float spritePixelSize,
        float spriteGroundOffsetPixels)
    {
        return new CharacterData
        {
            Id = id,
            DisplayName = displayName,
            Role = CharacterRole.Striker,
            MaxHealth = 10, // doesn't really matter
            WalkSpeed = 0.0f,
            DashSpeed = 0.0f,
            JumpVelocity = 0.0f,
            Gravity = 30.0f,
            Weight = 1.0f,
            MaxGuardGauge = 0,
            SpriteSheetPath = spriteSheetPath,
            SpriteSheetColumns = 1,
            SpriteSheetRows = 1,
            SpritePixelSize = spritePixelSize,
            SpriteGroundOffsetPixels = spriteGroundOffsetPixels,
            AnimationProfileId = "pickup",
            TintSpriteSheet = false,
            RoleTags = new List<string> { "pickup" },
            Moves = new List<MoveData>(),
            // No pushbox so players walk through it
            Pushbox = new CombatBoxDefinition { SizeX = 0.0f, SizeY = 0.0f, SizeZ = 0.0f, BoxType = CombatBoxType.Pushbox, Id = "pickup_pushbox" },
            CrouchingPushbox = new CombatBoxDefinition { SizeX = 0.0f, SizeY = 0.0f, SizeZ = 0.0f, BoxType = CombatBoxType.Pushbox, Id = "pickup_crouch_pushbox" },
            // Small hurtbox for pickup collection overlap
            Hurtbox = new CombatBoxDefinition { SizeX = 0.5f, SizeY = 0.5f, SizeZ = 0.5f, OffsetY = 0.25f, BoxType = CombatBoxType.Hurtbox, Id = "pickup_hurtbox" },
            CrouchingHurtbox = new CombatBoxDefinition { SizeX = 0.5f, SizeY = 0.5f, SizeZ = 0.5f, OffsetY = 0.25f, BoxType = CombatBoxType.Hurtbox, Id = "pickup_crouch_hurtbox" },
        };
    }
}
