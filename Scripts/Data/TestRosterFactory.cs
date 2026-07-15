using System.Collections.Generic;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

public static class TestRosterFactory
{
    public static CharacterData CreateBlankMannequin()
    {
        return new CharacterData
        {
            Id = "blank_mannequin",
            DisplayName = "Blank Mannequin",
            Role = CharacterRole.Mannequin,
            MaxHealth = 240,
            WalkSpeed = 5.2f,
            DashSpeed = 10.8f,
            JumpVelocity = 12.4f,
            Gravity = 28.0f,
            RoleTags = new List<string> { "base", "adaptive" },
            SynergyTags = new List<string> { "martial_arts", "archive" },
            IntroAnimation = new SpriteAnimationClipData
            {
                // Higgsfield hero intro: the mannequin powers up (raise/flex) then
                // settles into a fighting stance. Played on wall-clock time during
                // the boss-intro cinematic (Phase 1). Like a MUGEN state-190 intro /
                // SF4 / DBFZ entrance, the flourish plays ONCE then idle-bounces the
                // combat stance (frames 5-7) until the FIGHT release.
                AtlasPath = "res://Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png",
                AtlasColumns = 4,
                AtlasRows = 2,
                PixelSize = 0.017f,
                GroundOffsetPixels = 122.0f,
                Frames = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 6 },
                Durations = new List<int> { 10, 8, 16, 9, 8, 9, 10, 16, 10 },
                LoopStartFrameIndex = 5,
                PeakFrameIndex = 2,
                IntroAuraColor = new Color(0.5f, 0.9f, 1.0f),
            },
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = "mannequin_light",
                    DisplayName = "Template Jab",
                    InputCommand = "LP",
                    Priority = 10,
                    StartupFrames = 4,
                    ActiveFrames = 5,
                    RecoveryFrames = 10,
                    Damage = 14,
                    HitstunFrames = 16,
                    MeterGain = 6,
                    ForwardVelocity = 2.4f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 5,
                    CancelStartFrame = 6,
                    CancelEndFrame = 14,
                    NextAutoComboMoveId = "mannequin_medium",
                    CancelTags = new List<string> { "normal", "special" },
                    Tags = new List<string> { "normal" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("jab", 4, 8, 0.95f, 1.15f, 1.05f, 0.75f, 1.25f),
                    },
                },
                new()
                {
                    Id = "mannequin_medium",
                    DisplayName = "Template Cross",
                    InputCommand = "MP",
                    Priority = 20,
                    StartupFrames = 6,
                    ActiveFrames = 5,
                    RecoveryFrames = 14,
                    Damage = 22,
                    HitstunFrames = 20,
                    PushbackX = 2.5f,
                    MeterGain = 8,
                    ForwardVelocity = 2.0f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 7,
                    CancelStartFrame = 8,
                    CancelEndFrame = 17,
                    CancelIntoMoveIds = new List<string>
                    {
                        "mannequin_heavy",
                        "mannequin_medium_kick",
                        "mannequin_launcher",
                    },
                    NextAutoComboMoveId = "mannequin_heavy",
                    CancelTags = new List<string> { "special" },
                    Tags = new List<string> { "normal" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("cross", 6, 10, 1.08f, 1.15f, 1.25f, 0.9f, 1.35f),
                    },
                },
                new()
                {
                    Id = "mannequin_heavy",
                    DisplayName = "Template Smash",
                    InputCommand = "HP",
                    Priority = 18,
                    StartupFrames = 9,
                    ActiveFrames = 6,
                    RecoveryFrames = 19,
                    Damage = 34,
                    HitstunFrames = 26,
                    PushbackX = 3.1f,
                    MeterGain = 11,
                    ForwardVelocity = 1.65f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 9,
                    Tags = new List<string> { "normal", "heavy" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("smash", 9, 14, 1.22f, 1.16f, 1.45f, 1.0f, 1.4f),
                    },
                },
                new()
                {
                    Id = "mannequin_light_kick",
                    DisplayName = "Template Low Kick",
                    InputCommand = "LK",
                    Priority = 11,
                    StartupFrames = 5,
                    ActiveFrames = 5,
                    RecoveryFrames = 11,
                    Damage = 15,
                    HitstunFrames = 16,
                    PushbackX = 1.9f,
                    MeterGain = 6,
                    ForwardVelocity = 2.2f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 5,
                    CancelStartFrame = 7,
                    CancelEndFrame = 14,
                    NextAutoComboMoveId = "mannequin_medium_kick",
                    CancelTags = new List<string> { "normal", "special" },
                    Tags = new List<string> { "normal", "kick", "light" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("low_kick", 5, 9, 0.95f, 0.62f, 1.15f, 0.52f, 1.25f),
                    },
                },
                new()
                {
                    Id = "mannequin_medium_kick",
                    DisplayName = "Template Side Kick",
                    InputCommand = "MK",
                    Priority = 21,
                    StartupFrames = 7,
                    ActiveFrames = 5,
                    RecoveryFrames = 15,
                    Damage = 25,
                    HitstunFrames = 21,
                    PushbackX = 2.8f,
                    MeterGain = 8,
                    ForwardVelocity = 1.85f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 7,
                    CancelStartFrame = 9,
                    CancelEndFrame = 18,
                    CancelIntoMoveIds = new List<string>
                    {
                        "mannequin_heavy",
                        "mannequin_heavy_kick",
                        "mannequin_launcher",
                    },
                    NextAutoComboMoveId = "mannequin_heavy_kick",
                    CancelTags = new List<string> { "special" },
                    Tags = new List<string> { "normal", "kick", "medium" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("side_kick", 7, 11, 1.24f, 0.95f, 1.45f, 0.62f, 1.35f),
                    },
                },
                new()
                {
                    Id = "mannequin_heavy_kick",
                    DisplayName = "Template Roundhouse",
                    InputCommand = "HK",
                    Priority = 19,
                    StartupFrames = 11,
                    ActiveFrames = 6,
                    RecoveryFrames = 22,
                    Damage = 37,
                    HitstunFrames = 29,
                    PushbackX = 4.0f,
                    MeterGain = 11,
                    ForwardVelocity = 1.45f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 10,
                    Tags = new List<string> { "normal", "kick", "heavy" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("roundhouse", 11, 16, 1.32f, 1.08f, 1.68f, 0.78f, 1.45f),
                    },
                },
                new()
                {
                    Id = "mannequin_launcher",
                    DisplayName = "Blank Launcher",
                    InputCommand = "2HP",
                    Priority = 30,
                    StartupFrames = 8,
                    ActiveFrames = 4,
                    RecoveryFrames = 18,
                    Damage = 26,
                    HitstunFrames = 30,
                    MeterGain = 10,
                    ForwardVelocity = 1.4f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 8,
                    IsLauncher = true,
                    LaunchX = 2.2f,
                    LaunchY = 7.5f,
                    Posture = MovePosture.Crouching,
                    JumpCancelStartFrame = 10,
                    JumpCancelEndFrame = 24,
                    Tags = new List<string> { "normal", "launcher" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("launcher", 8, 12, 1.05f, 1.0f, 1.3f, 1.2f, 1.3f),
                    },
                },
                new()
                {
                    Id = "mannequin_air_light",
                    DisplayName = "Air Template Tap",
                    InputCommand = "LP",
                    Priority = 45,
                    StartupFrames = 4,
                    ActiveFrames = 5,
                    RecoveryFrames = 12,
                    Damage = 16,
                    HitstunFrames = 22,
                    PushbackX = 1.4f,
                    MeterGain = 7,
                    AllowGround = false,
                    AllowAir = true,
                    Posture = MovePosture.Air,
                    CancelStartFrame = 6,
                    CancelEndFrame = 15,
                    CancelIntoMoveIds = new List<string>
                    {
                        "mannequin_air_medium",
                        "mannequin_air_light_kick",
                        "mannequin_air_heavy",
                    },
                    Tags = new List<string> { "normal", "air", "light" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("air_tap", 4, 8, 0.88f, 0.72f, 1.1f, 0.78f, 1.25f),
                    },
                },
                new()
                {
                    Id = "mannequin_air_heavy",
                    DisplayName = "Air Template Spike",
                    InputCommand = "HP",
                    Priority = 46,
                    StartupFrames = 7,
                    ActiveFrames = 5,
                    RecoveryFrames = 18,
                    Damage = 32,
                    HitstunFrames = 26,
                    PushbackX = 2.3f,
                    LaunchX = 1.2f,
                    LaunchY = -4.0f,
                    MeterGain = 10,
                    AllowGround = false,
                    AllowAir = true,
                    Posture = MovePosture.Air,
                    Tags = new List<string> { "normal", "air", "heavy" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("air_spike", 7, 11, 0.98f, 0.52f, 1.25f, 0.9f, 1.3f),
                    },
                },
                CreateCrouchingNormal(
                    "mannequin_crouch_light",
                    "Crouching Jab",
                    "2LP",
                    priority: 55,
                    startup: 4,
                    active: 4,
                    recovery: 9,
                    damage: 12,
                    hitstun: 16,
                    offsetX: 0.88f,
                    offsetY: 0.68f,
                    sizeX: 1.0f,
                    sizeY: 0.56f,
                    cancelInto: new[] { "mannequin_crouch_medium", "mannequin_crouch_light_kick" },
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "mannequin_crouch_medium",
                    "Crouching Cross",
                    "2MP",
                    priority: 56,
                    startup: 6,
                    active: 5,
                    recovery: 13,
                    damage: 20,
                    hitstun: 20,
                    offsetX: 1.02f,
                    offsetY: 0.72f,
                    sizeX: 1.2f,
                    sizeY: 0.62f,
                    cancelInto: new[] { "mannequin_launcher", "mannequin_crouch_medium_kick" },
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "mannequin_crouch_light_kick",
                    "Crouching Shin Kick",
                    "2LK",
                    priority: 55,
                    startup: 5,
                    active: 5,
                    recovery: 10,
                    damage: 14,
                    hitstun: 17,
                    offsetX: 1.0f,
                    offsetY: 0.34f,
                    sizeX: 1.28f,
                    sizeY: 0.38f,
                    cancelInto: new[] { "mannequin_crouch_medium", "mannequin_crouch_medium_kick" },
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "mannequin_crouch_medium_kick",
                    "Crouching Sweep",
                    "2MK",
                    priority: 57,
                    startup: 7,
                    active: 5,
                    recovery: 15,
                    damage: 23,
                    hitstun: 22,
                    offsetX: 1.18f,
                    offsetY: 0.32f,
                    sizeX: 1.52f,
                    sizeY: 0.42f,
                    cancelInto: new[] { "mannequin_launcher", "mannequin_crouch_heavy_kick" },
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "mannequin_crouch_heavy_kick",
                    "Crouching Arc Sweep",
                    "2HK",
                    priority: 58,
                    startup: 10,
                    active: 6,
                    recovery: 21,
                    damage: 34,
                    hitstun: 28,
                    offsetX: 1.28f,
                    offsetY: 0.34f,
                    sizeX: 1.72f,
                    sizeY: 0.48f),
                CreateAirNormal(
                    "mannequin_air_medium",
                    "Air Template Cross",
                    "MP",
                    priority: 47,
                    startup: 6,
                    active: 5,
                    recovery: 13,
                    damage: 23,
                    hitstun: 23,
                    offsetX: 1.0f,
                    offsetY: 0.68f,
                    sizeX: 1.26f,
                    sizeY: 0.78f,
                    cancelInto: new[] { "mannequin_air_medium_kick", "mannequin_air_heavy" }),
                CreateAirNormal(
                    "mannequin_air_light_kick",
                    "Air Template Knee",
                    "LK",
                    priority: 47,
                    startup: 4,
                    active: 6,
                    recovery: 11,
                    damage: 17,
                    hitstun: 21,
                    offsetX: 0.82f,
                    offsetY: 0.42f,
                    sizeX: 1.08f,
                    sizeY: 0.72f,
                    cancelInto: new[] { "mannequin_air_medium", "mannequin_air_medium_kick" }),
                CreateAirNormal(
                    "mannequin_air_medium_kick",
                    "Air Template Side Kick",
                    "MK",
                    priority: 48,
                    startup: 7,
                    active: 6,
                    recovery: 14,
                    damage: 27,
                    hitstun: 24,
                    offsetX: 1.08f,
                    offsetY: 0.48f,
                    sizeX: 1.46f,
                    sizeY: 0.72f,
                    cancelInto: new[] { "mannequin_air_heavy", "mannequin_air_heavy_kick" }),
                CreateAirNormal(
                    "mannequin_air_heavy_kick",
                    "Air Template Axe Kick",
                    "HK",
                    priority: 49,
                    startup: 9,
                    active: 6,
                    recovery: 18,
                    damage: 36,
                    hitstun: 28,
                    offsetX: 1.06f,
                    offsetY: 0.38f,
                    sizeX: 1.42f,
                    sizeY: 1.02f,
                    launchY: -3.2f),
                new()
                {
                    Id = "archive_pulse",
                    DisplayName = "Archive Pulse",
                    InputCommand = "236LP",
                    Priority = 80,
                    StartupFrames = 10,
                    ActiveFrames = 6,
                    RecoveryFrames = 20,
                    Damage = 38,
                    HitstunFrames = 24,
                    PushbackX = 4.0f,
                    MeterGain = 12,
                    ForwardVelocity = 1.1f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 7,
                    Tags = new List<string> { "special", "archive" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("archive_pulse", 10, 15, 1.55f, 1.1f, 2.0f, 1.35f, 1.8f),
                    },
                },
                new()
                {
                    Id = "archive_burst",
                    DisplayName = "Archive Burst",
                    InputCommand = "236HK",
                    Priority = 100,
                    StartupFrames = 6,
                    ActiveFrames = 10,
                    RecoveryFrames = 34,
                    Damage = 92,
                    HitstunFrames = 42,
                    PushbackX = 5.5f,
                    MeterCost = 100,
                    IsSuper = true,
                    SuperFreezeFrames = 18,
                    Tags = new List<string> { "super", "archive" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("archive_burst", 6, 15, 1.65f, 1.15f, 2.6f, 1.9f, 2.2f),
                    },
                },
                new()
                {
                    Id = "mannequin_uppercut",
                    DisplayName = "Template Uppercut",
                    InputCommand = "623HP",
                    Priority = 85,
                    StartupFrames = 5,
                    ActiveFrames = 8,
                    RecoveryFrames = 28,
                    Damage = 45,
                    HitstunFrames = 28,
                    PushbackX = 2.0f,
                    MeterGain = 15,
                    InvulnerableStartupFrames = 5,
                    ForwardVelocity = 1.5f,
                    ForwardVelocityStartFrame = 0,
                    ForwardVelocityEndFrame = 12,
                    LaunchX = 1.8f,
                    LaunchY = 8.5f,
                    IsLauncher = true,
                    Tags = new List<string> { "special", "anti_air" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("uppercut", 5, 12, 1.2f, 1.5f, 1.6f, 2.0f, 1.5f),
                    },
                },
            },
        };
    }

    public static CharacterData CreateTrainingEnemy()
    {
        return new CharacterData
        {
            Id = "training_enemy",
            DisplayName = "Training Enemy",
            Role = CharacterRole.Striker,
            MaxHealth = 120,
            WalkSpeed = 3.0f,
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = "enemy_swing",
                    DisplayName = "Practice Swing",
                    StartupFrames = 14,
                    ActiveFrames = 5,
                    RecoveryFrames = 24,
                    Damage = 10,
                    HitstunFrames = 12,
                    PushbackX = 1.6f,
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("enemy_swing", 14, 18, 0.85f, 1.0f, 0.9f, 0.9f, 1.0f),
                    },
                },
            },
        };
    }

    public static CharacterData CreateArchiveScout()
    {
        return CreateArchiveMinion(
            "archive_scout",
            "Archive Scout",
            CharacterRole.Rushdown,
            maxHealth: 54,
            walkSpeed: 3.7f,
            attackName: "Scout Jab",
            startupFrames: 12,
            recoveryFrames: 24,
            damage: 7,
            pushbackX: 1.25f);
    }

    public static CharacterData CreateArchiveRaider()
    {
        return CreateArchiveMinion(
            "archive_raider",
            "Archive Raider",
            CharacterRole.Striker,
            maxHealth: 76,
            walkSpeed: 3.1f,
            attackName: "Raider Cross",
            startupFrames: 15,
            recoveryFrames: 28,
            damage: 9,
            pushbackX: 1.7f);
    }

    public static CharacterData CreateArchiveBruiser()
    {
        return CreateArchiveMinion(
            "archive_bruiser",
            "Archive Bruiser",
            CharacterRole.Grappler,
            maxHealth: 118,
            walkSpeed: 2.35f,
            attackName: "Bruiser Hammer",
            startupFrames: 20,
            recoveryFrames: 34,
            damage: 14,
            pushbackX: 2.8f);
    }

    public static CharacterData CreateTestBoss()
    {
        return new CharacterData
        {
            Id = "archive_knight_boss",
            DisplayName = "Archive Knight",
            Role = CharacterRole.Boss,
            MaxHealth = 320,
            WalkSpeed = 2.6f,
            DashSpeed = 6.2f,
            Weight = 1.5f,
            MaxGuardGauge = 100,
            GuardBreakFrames = 78,
            GuardRecoveryDelayFrames = 90,
            GuardRecoveryPerSecond = 20.0f,
            RoleTags = new List<string> { "boss", "sword" },
            SynergyTags = new List<string> { "metal", "sword" },
            IntroAnimation = new SpriteAnimationClipData
            {
                // Archive Knight intro: the mannequin-knight powers up into a battle
                // stance. Uses the mannequin intro atlas recolored to the knight's
                // purple armor (same procedural base art), so it matches the boss.
                // Plays once during the cinematic, then idle-bounces the stance.
                AtlasPath = "res://Assets/Sprites/Mannequin/knight_intro_higgsfield_v1.png",
                AtlasColumns = 4,
                AtlasRows = 2,
                PixelSize = 0.017f,
                GroundOffsetPixels = 122.0f,
                Frames = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 6 },
                Durations = new List<int> { 10, 8, 16, 9, 8, 9, 10, 16, 10 },
                LoopStartFrameIndex = 5,
                PeakFrameIndex = 2,
                IntroAuraColor = new Color(0.72f, 0.56f, 1.0f),
            },
            CpuProfile = new CpuFighterProfileData
            {
                Id = "archive_knight_story_cpu",
                ReactionFrames = 8,
                DecisionIntervalFrames = 10,
                GuardHoldFrames = 24,
                MovementCommitmentFrames = 16,
                PreferredRangeMin = 1.15f,
                PreferredRangeMax = 2.55f,
                LaneTolerance = 0.48f,
                DashDistance = 4.4f,
                Aggression = 0.64f,
                GuardChance = 0.64f,
                AntiAirChance = 0.78f,
                PunishChance = 0.76f,
                RetreatChance = 0.30f,
                JumpEvadeChance = 0.10f,
                MistakeChance = 0.10f,
            },
            Hurtbox = new CombatBoxDefinition
            {
                Id = "boss_hurtbox",
                BoxType = CombatBoxType.Hurtbox,
                OffsetY = 1.25f,
                SizeX = 1.25f,
                SizeY = 2.5f,
                SizeZ = 1.1f,
            },
            Pushbox = new CombatBoxDefinition
            {
                Id = "boss_pushbox",
                BoxType = CombatBoxType.Pushbox,
                OffsetY = 1.15f,
                SizeX = 1.15f,
                SizeY = 2.2f,
                SizeZ = 1.0f,
            },
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = "boss_cleave",
                    DisplayName = "Archive Cleave",
                    StartupFrames = 18,
                    ActiveFrames = 7,
                    RecoveryFrames = 30,
                    Damage = 22,
                    HitstunFrames = 22,
                    HitStopFrames = 5,
                    PushbackX = 3.5f,
                    GuardDamage = 16,
                    AttackHeight = AttackHeight.Mid,
                    Tags = new List<string> { "normal", "anti_air" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("boss_cleave", 18, 24, 1.3f, 1.25f, 1.7f, 1.2f, 1.6f),
                    },
                },
                new()
                {
                    Id = "boss_vault_lunge",
                    DisplayName = "Vault Lunge",
                    StartupFrames = 10,
                    ActiveFrames = 6,
                    RecoveryFrames = 24,
                    Damage = 18,
                    HitstunFrames = 19,
                    HitStopFrames = 4,
                    PushbackX = 2.8f,
                    GuardDamage = 22,
                    AttackHeight = AttackHeight.Overhead,
                    ForwardVelocity = 5.4f,
                    ForwardVelocityStartFrame = 2,
                    ForwardVelocityEndFrame = 13,
                    Tags = new List<string> { "special", "gap_closer", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("boss_vault_lunge", 10, 15, 1.25f, 1.08f, 1.55f, 0.95f, 1.35f),
                    },
                },
                new()
                {
                    Id = "boss_archive_sweep",
                    DisplayName = "Archive Sweep",
                    StartupFrames = 24,
                    ActiveFrames = 8,
                    RecoveryFrames = 34,
                    Damage = 25,
                    HitstunFrames = 25,
                    HitStopFrames = 6,
                    PushbackX = 4.2f,
                    GuardDamage = 28,
                    AttackHeight = AttackHeight.Low,
                    Tags = new List<string> { "special", "wide", "keep_out" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("boss_archive_sweep", 24, 31, 0.85f, 1.05f, 3.6f, 1.5f, 4.6f),
                    },
                },
                new()
                {
                    Id = "boss_final_breaker",
                    DisplayName = "Final Archive Breaker",
                    StartupFrames = 14,
                    ActiveFrames = 8,
                    RecoveryFrames = 30,
                    Damage = 34,
                    HitstunFrames = 32,
                    HitStopFrames = 8,
                    PushbackX = 4.8f,
                    LaunchX = 3.2f,
                    LaunchY = 6.2f,
                    GuardDamage = 30,
                    AttackHeight = AttackHeight.Overhead,
                    ForwardVelocity = 3.4f,
                    ForwardVelocityStartFrame = 4,
                    ForwardVelocityEndFrame = 17,
                    Tags = new List<string> { "heavy", "launcher", "anti_air", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("boss_final_breaker", 14, 21, 1.4f, 1.2f, 2.05f, 1.45f, 2.0f),
                    },
                },
                new()
                {
                    Id = "boss_archive_cataclysm",
                    DisplayName = "Archive Cataclysm",
                    StartupFrames = 20,
                    ActiveFrames = 10,
                    RecoveryFrames = 38,
                    Damage = 52,
                    HitstunFrames = 40,
                    HitStopFrames = 10,
                    PushbackX = 6.2f,
                    LaunchX = 4.0f,
                    LaunchY = 5.4f,
                    MeterCost = 100,
                    GuardDamage = 40,
                    SuperFreezeFrames = 24,
                    InvulnerableStartupFrames = 20,
                    MinimumDamageScale = 0.5f,
                    IsSuper = true,
                    Unblockable = true,
                    AttackHeight = AttackHeight.Overhead,
                    Tags = new List<string> { "super", "wide", "archive", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("boss_archive_cataclysm", 20, 29, 1.25f, 1.2f, 4.8f, 2.25f, 3.0f),
                    },
                },
            },
            BossPhases = new List<BossPhaseData>
            {
                new()
                {
                    Id = "sentinel_stance",
                    DisplayName = "Sentinel Stance",
                    HealthThreshold = 1.0f,
                    EnabledMoveIds = new List<string> { "boss_cleave" },
                    TransitionFrames = 0,
                    AttackIntervalFrames = 82,
                    AttackRange = 2.1f,
                    MovementSpeedMultiplier = 0.9f,
                    ReactionFrameModifier = 2,
                    AggressionMultiplier = 0.76f,
                    DefenseMultiplier = 0.72f,
                },
                new()
                {
                    Id = "broken_seal",
                    DisplayName = "Broken Seal",
                    HealthThreshold = 0.66f,
                    EnabledMoveIds = new List<string>
                    {
                        "boss_vault_lunge",
                        "boss_cleave",
                        "boss_archive_sweep",
                    },
                    TransitionFrames = 42,
                    AttackIntervalFrames = 62,
                    AttackRange = 2.8f,
                    MovementSpeedMultiplier = 1.12f,
                    ReactionFrameModifier = 0,
                    AggressionMultiplier = 1.08f,
                    DefenseMultiplier = 1.0f,
                },
                new()
                {
                    Id = "last_archive",
                    DisplayName = "Last Archive",
                    HealthThreshold = 0.33f,
                    EnabledMoveIds = new List<string>
                    {
                        "boss_archive_cataclysm",
                        "boss_final_breaker",
                        "boss_vault_lunge",
                        "boss_archive_sweep",
                    },
                    TransitionFrames = 54,
                    AttackIntervalFrames = 46,
                    AttackRange = 3.0f,
                    MovementSpeedMultiplier = 1.32f,
                    ReactionFrameModifier = -2,
                    AggressionMultiplier = 1.28f,
                    DefenseMultiplier = 1.16f,
                    MeterGrant = 100,
                },
            },
        };
    }

    public static CharacterData CreateArchiveKnightForm()
    {
        var form = CreateTestBoss();
        form.Id = "archive_knight_form";
        form.DisplayName = "Archive Knight Form";
        form.Role = CharacterRole.Striker;
        form.MaxHealth = 270;
        form.WalkSpeed = 4.4f;
        form.DashSpeed = 7.2f;
        form.MaxGuardGauge = 0;
        form.BossPhases.Clear();
        form.CpuProfile = null;
        form.Moves = new List<MoveData>
        {
            new()
            {
                Id = "knight_light",
                DisplayName = "Knight Pommel",
                InputCommand = "LP",
                Priority = 10,
                StartupFrames = 5,
                ActiveFrames = 5,
                RecoveryFrames = 12,
                Damage = 18,
                HitstunFrames = 17,
                MeterGain = 7,
                ForwardVelocity = 2.1f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 6,
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_pommel", 5, 9, 1.0f, 1.1f, 1.1f, 0.85f, 1.25f),
                },
            },
            new()
            {
                Id = "knight_medium",
                DisplayName = "Knight Shoulder",
                InputCommand = "MP",
                Priority = 20,
                StartupFrames = 7,
                ActiveFrames = 5,
                RecoveryFrames = 15,
                Damage = 28,
                HitstunFrames = 22,
                PushbackX = 3.0f,
                MeterGain = 9,
                ForwardVelocity = 2.2f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 8,
                Tags = new List<string> { "normal", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_shoulder", 7, 11, 1.1f, 1.18f, 1.35f, 1.0f, 1.35f),
                },
            },
            new()
            {
                Id = "knight_heavy",
                DisplayName = "Knight Breaker",
                InputCommand = "HP",
                Priority = 18,
                StartupFrames = 10,
                ActiveFrames = 6,
                RecoveryFrames = 22,
                Damage = 42,
                HitstunFrames = 30,
                PushbackX = 4.2f,
                MeterGain = 12,
                ForwardVelocity = 1.7f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 10,
                Tags = new List<string> { "normal", "heavy", "sword" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_breaker", 10, 15, 1.35f, 1.2f, 1.65f, 1.15f, 1.45f),
                },
            },
            new()
            {
                Id = "knight_light_kick",
                DisplayName = "Knight Low Kick",
                InputCommand = "LK",
                Priority = 11,
                StartupFrames = 5,
                ActiveFrames = 5,
                RecoveryFrames = 12,
                Damage = 19,
                HitstunFrames = 17,
                PushbackX = 2.0f,
                MeterGain = 7,
                ForwardVelocity = 2.0f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 6,
                Tags = new List<string> { "normal", "kick", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_low_kick", 5, 9, 1.02f, 0.64f, 1.18f, 0.54f, 1.3f),
                },
            },
            new()
            {
                Id = "knight_medium_kick",
                DisplayName = "Knight Drive Kick",
                InputCommand = "MK",
                Priority = 21,
                StartupFrames = 8,
                ActiveFrames = 5,
                RecoveryFrames = 16,
                Damage = 30,
                HitstunFrames = 23,
                PushbackX = 3.25f,
                MeterGain = 9,
                ForwardVelocity = 2.25f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 9,
                Tags = new List<string> { "normal", "kick", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_drive_kick", 8, 12, 1.28f, 0.96f, 1.5f, 0.68f, 1.4f),
                },
            },
            new()
            {
                Id = "knight_heavy_kick",
                DisplayName = "Knight Iron Roundhouse",
                InputCommand = "HK",
                Priority = 19,
                StartupFrames = 12,
                ActiveFrames = 6,
                RecoveryFrames = 24,
                Damage = 45,
                HitstunFrames = 32,
                PushbackX = 4.8f,
                MeterGain = 12,
                ForwardVelocity = 1.55f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 11,
                Tags = new List<string> { "normal", "kick", "heavy", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_iron_roundhouse", 12, 17, 1.42f, 1.06f, 1.82f, 0.82f, 1.48f),
                },
            },
            new()
            {
                Id = "knight_air_light",
                DisplayName = "Knight Air Pommel",
                InputCommand = "LP",
                Priority = 45,
                StartupFrames = 4,
                ActiveFrames = 5,
                RecoveryFrames = 12,
                Damage = 20,
                HitstunFrames = 23,
                PushbackX = 1.6f,
                MeterGain = 8,
                AllowGround = false,
                AllowAir = true,
                CancelStartFrame = 6,
                CancelEndFrame = 15,
                CancelIntoMoveIds = new List<string> { "knight_air_heavy" },
                Tags = new List<string> { "normal", "air", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_air_pommel", 4, 8, 0.92f, 0.72f, 1.16f, 0.78f, 1.3f),
                },
            },
            new()
            {
                Id = "knight_air_heavy",
                DisplayName = "Knight Air Breaker",
                InputCommand = "HP",
                Priority = 46,
                StartupFrames = 7,
                ActiveFrames = 5,
                RecoveryFrames = 19,
                Damage = 40,
                HitstunFrames = 29,
                PushbackX = 2.6f,
                LaunchX = 1.4f,
                LaunchY = -4.8f,
                MeterGain = 11,
                AllowGround = false,
                AllowAir = true,
                Tags = new List<string> { "normal", "air", "heavy", "metal" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_air_breaker", 7, 11, 1.05f, 0.54f, 1.36f, 0.95f, 1.35f),
                },
            },
            new()
            {
                Id = "knight_cleave",
                DisplayName = "Inherited Cleave",
                InputCommand = "236HP",
                Priority = 80,
                StartupFrames = 12,
                ActiveFrames = 7,
                RecoveryFrames = 22,
                Damage = 48,
                HitstunFrames = 27,
                PushbackX = 4.6f,
                MeterGain = 12,
                ForwardVelocity = 1.4f,
                ForwardVelocityStartFrame = 0,
                ForwardVelocityEndFrame = 9,
                Tags = new List<string> { "special", "sword" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("inherited_cleave", 12, 18, 1.55f, 1.25f, 2.1f, 1.35f, 1.8f),
                },
            },
            new()
            {
                Id = "knight_archive_burst",
                DisplayName = "Inherited Archive Burst",
                InputCommand = "236HK",
                Priority = 100,
                StartupFrames = 8,
                ActiveFrames = 10,
                RecoveryFrames = 34,
                Damage = 104,
                HitstunFrames = 44,
                PushbackX = 6.0f,
                MeterCost = 100,
                IsSuper = true,
                SuperFreezeFrames = 22,
                Tags = new List<string> { "super", "archive", "sword" },
                CombatBoxes = new List<CombatBoxDefinition>
                {
                    BasicHitbox("knight_archive_burst", 8, 17, 1.7f, 1.18f, 2.75f, 1.95f, 2.25f),
                },
            },
        };

        return form;
    }

    private static CharacterData CreateArchiveMinion(
        string id,
        string displayName,
        CharacterRole role,
        int maxHealth,
        float walkSpeed,
        string attackName,
        int startupFrames,
        int recoveryFrames,
        int damage,
        float pushbackX)
    {
        return new CharacterData
        {
            Id = id,
            DisplayName = displayName,
            Role = role,
            MaxHealth = maxHealth,
            WalkSpeed = walkSpeed,
            Weight = role == CharacterRole.Grappler ? 1.3f : 1.0f,
            SpriteSheetPath = $"res://Assets/Sprites/Enemies/{id}_higgsfield_v1.png",
            SpriteSheetColumns = 10,
            SpriteSheetRows = 9,
            SpritePixelSize = 0.018f,
            SpriteGroundOffsetPixels = 120.0f,
            TintSpriteSheet = false,
            RoleTags = new List<string> { "enemy", "archive_construct" },
            SynergyTags = new List<string> { "archive" },
            ArcadeEnemyProfile = CreateArcadeEnemyProfile(id, role),
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = $"{id}_attack",
                    DisplayName = attackName,
                    StartupFrames = startupFrames,
                    ActiveFrames = 5,
                    RecoveryFrames = recoveryFrames,
                    Damage = damage,
                    HitstunFrames = 12,
                    PushbackX = pushbackX,
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox(
                            $"{id}_hit",
                            startupFrames,
                            startupFrames + 4,
                            0.88f,
                            1.0f,
                            role == CharacterRole.Grappler ? 1.15f : 0.92f,
                            0.9f,
                            1.0f),
                    },
                },
            },
        };
    }

    private static MoveData CreateCrouchingNormal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        int hitstun,
        float offsetX,
        float offsetY,
        float sizeX,
        float sizeY,
        string[]? cancelInto = null,
        string[]? cancelTags = null)
    {
        var canCancel = cancelInto is { Length: > 0 } || cancelTags is { Length: > 0 };
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = hitstun,
            PushbackX = 1.4f + damage * 0.045f,
            MeterGain = Mathf.Max(5, damage / 3),
            ForwardVelocity = 1.2f,
            ForwardVelocityStartFrame = 0,
            ForwardVelocityEndFrame = startup,
            Posture = MovePosture.Crouching,
            AttackHeight = command.EndsWith("K") ? AttackHeight.Low : AttackHeight.Mid,
            CancelStartFrame = canCancel ? startup + 1 : -1,
            CancelEndFrame = canCancel ? startup + active + 4 : -1,
            CancelIntoMoveIds = cancelInto is null ? new List<string>() : new List<string>(cancelInto),
            CancelTags = cancelTags is null ? new List<string>() : new List<string>(cancelTags),
            Tags = new List<string> { "normal", "crouch" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                BasicHitbox(
                    $"{id}_hit",
                    startup,
                    startup + active - 1,
                    offsetX,
                    offsetY,
                    sizeX,
                    sizeY,
                    1.3f),
            },
        };
    }

    private static MoveData CreateAirNormal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        int hitstun,
        float offsetX,
        float offsetY,
        float sizeX,
        float sizeY,
        string[]? cancelInto = null,
        float launchY = 0.0f)
    {
        var canCancel = cancelInto is { Length: > 0 };
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = hitstun,
            PushbackX = 1.2f + damage * 0.04f,
            LaunchX = launchY < 0.0f ? 1.1f : 0.0f,
            LaunchY = launchY,
            MeterGain = Mathf.Max(6, damage / 3),
            AllowGround = false,
            AllowAir = true,
            Posture = MovePosture.Air,
            CancelStartFrame = canCancel ? startup + 1 : -1,
            CancelEndFrame = canCancel ? startup + active + 4 : -1,
            CancelIntoMoveIds = cancelInto is null ? new List<string>() : new List<string>(cancelInto),
            Tags = new List<string> { "normal", "air" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                BasicHitbox(
                    $"{id}_hit",
                    startup,
                    startup + active - 1,
                    offsetX,
                    offsetY,
                    sizeX,
                    sizeY,
                    1.3f),
            },
        };
    }

    private static MoveData CreateRyuProjectile(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int recovery,
        int damage,
        float velocity,
        int meterCost = 0,
        bool isSuper = false,
        int superFreezeFrames = 0,
        float projectileScale = 1.0f)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = 1,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = isSuper ? 38 : 21,
            BlockstunFrames = isSuper ? 20 : 13,
            HitStopFrames = isSuper ? 8 : 4,
            MeterGain = isSuper ? 0 : 9,
            MeterCost = meterCost,
            PushbackX = isSuper ? 5.0f : 2.8f,
            SuperFreezeFrames = superFreezeFrames,
            IsSuper = isSuper,
            MinimumDamageScale = isSuper ? 0.5f : 0.35f,
            CancelTags = isSuper ? new List<string>() : new List<string> { "super" },
            Tags = new List<string>
            {
                isSuper ? "super" : "special",
                "projectile",
                "keep_out",
            },
            ProjectileSpawns = new List<ProjectileSpawnData>
            {
                new()
                {
                    Id = $"{id}_projectile",
                    SpawnFrame = startup,
                    OffsetX = isSuper ? 1.2f : 1.1f,
                    OffsetY = 2.65f,
                    // Preserve the user-approved hand-aligned visual while the
                    // gameplay volume intersects standing/crouching hurtboxes.
                    CollisionOffsetY = -1.15f,
                    VelocityX = velocity,
                    LifetimeFrames = 105,
                    SizeX = (isSuper ? 1.7f : 0.82f) * projectileScale,
                    SizeY = (isSuper ? 1.1f : 0.58f) * projectileScale,
                    SizeZ = (isSuper ? 1.0f : 0.72f) * projectileScale,
                },
            },
        };
    }

    private static MoveData CreateRyuShoryuken(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float riseVelocity,
        int invulnerableFrames,
        int meterCost = 0,
        int hitCount = 1)
    {
        var move = new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 28,
            BlockstunFrames = 15,
            HitStopFrames = 7,
            MeterGain = meterCost > 0 ? 0 : 12,
            MeterCost = meterCost,
            PushbackX = 2.4f,
            LaunchX = 2.2f,
            LaunchY = 10.5f,
            InitialVelocityY = riseVelocity,
            IsLauncher = true,
            InvulnerableStartupFrames = invulnerableFrames,
            ForwardVelocity = 1.4f,
            ForwardVelocityStartFrame = 1,
            ForwardVelocityEndFrame = startup + active,
            Tags = new List<string> { "special", "launcher", "anti_air", "punish" },
        };

        for (var hit = 0; hit < hitCount; hit++)
        {
            var hitFrame = startup + Mathf.RoundToInt(
                hit * Mathf.Max(1, active - 1) / (float)Mathf.Max(1, hitCount));
            move.CombatBoxes.Add(new CombatBoxDefinition
            {
                Id = $"{id}_hit_{hit + 1}",
                BoxType = CombatBoxType.Hitbox,
                StartFrame = hitFrame,
                EndFrame = Mathf.Min(startup + active - 1, hitFrame + 1),
                OffsetX = 0.95f,
                OffsetY = 1.45f,
                SizeX = 1.25f,
                SizeY = 1.65f,
                SizeZ = 1.35f,
                DamageOverride = Mathf.CeilToInt(damage / (float)hitCount),
            });
        }

        return move;
    }

    private static MoveData CreateRyuTatsumaki(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float forwardVelocity,
        int hitCount,
        bool air,
        int meterCost = 0)
    {
        var move = new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 25,
            BlockstunFrames = 14,
            HitStopFrames = 5,
            MeterGain = meterCost > 0 ? 0 : 11,
            MeterCost = meterCost,
            PushbackX = 3.4f,
            InitialVelocityY = air ? 0.0f : 7.2f,
            ForwardVelocity = forwardVelocity,
            ForwardVelocityStartFrame = Mathf.Max(1, startup - 2),
            ForwardVelocityEndFrame = startup + active,
            AllowGround = !air,
            AllowAir = air,
            Posture = air ? MovePosture.Air : MovePosture.Standing,
            Tags = new List<string>
            {
                "special",
                air ? "air" : "ground",
                "gap_closer",
                "corner_carry",
            },
        };

        for (var hit = 0; hit < hitCount; hit++)
        {
            var hitFrame = startup + Mathf.RoundToInt(
                hit * Mathf.Max(1, active - 1) / (float)Mathf.Max(1, hitCount));
            move.CombatBoxes.Add(new CombatBoxDefinition
            {
                Id = $"{id}_hit_{hit + 1}",
                BoxType = CombatBoxType.Hitbox,
                StartFrame = hitFrame,
                EndFrame = Mathf.Min(startup + active - 1, hitFrame + 1),
                OffsetX = 0.72f,
                OffsetY = 1.02f,
                SizeX = 2.1f,
                SizeY = 1.16f,
                SizeZ = 1.6f,
                DamageOverride = Mathf.CeilToInt(damage / (float)hitCount),
            });
        }

        return move;
    }

    private static MoveData CreateRyuJoudan(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float forwardVelocity,
        int meterCost = 0)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 25,
            BlockstunFrames = 14,
            HitStopFrames = 6,
            MeterGain = meterCost > 0 ? 0 : 11,
            MeterCost = meterCost,
            PushbackX = 4.0f,
            LaunchX = 4.8f,
            LaunchY = 3.5f,
            ForwardVelocity = forwardVelocity,
            ForwardVelocityStartFrame = 2,
            ForwardVelocityEndFrame = startup + active,
            Tags = new List<string> { "special", "gap_closer", "corner_carry" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                BasicHitbox(
                    $"{id}_hit",
                    startup,
                    startup + active - 1,
                    1.35f,
                    1.05f,
                    1.8f,
                    0.82f,
                    1.4f),
            },
        };
    }

    private static MoveData CreateRyuThrow(
        string id,
        string displayName,
        string command,
        int priority,
        int damage,
        float launchX)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = 5,
            ActiveFrames = 2,
            RecoveryFrames = 24,
            Damage = damage,
            HitstunFrames = 32,
            HitStopFrames = 8,
            MeterGain = 10,
            PushbackX = launchX,
            LaunchX = launchX,
            LaunchY = 5.0f,
            AttackHeight = AttackHeight.Throw,
            Unblockable = true,
            Unparryable = true,
            Tags = new List<string> { "throw", "grapple", "punish" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                new()
                {
                    Id = $"{id}_grab",
                    BoxType = CombatBoxType.Grabbox,
                    StartFrame = 5,
                    EndFrame = 6,
                    OffsetX = 0.75f,
                    OffsetY = 1.0f,
                    SizeX = 0.9f,
                    SizeY = 1.55f,
                    SizeZ = 1.15f,
                },
            },
        };
    }

    private static MoveData CreateRyuCommandNormal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        AttackHeight attackHeight,
        float forwardVelocity,
        int hitCount = 1)
    {
        var move = new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 24,
            BlockstunFrames = 13,
            HitStopFrames = 6,
            MeterGain = 10,
            PushbackX = 3.0f,
            AttackHeight = attackHeight,
            ForwardVelocity = forwardVelocity,
            ForwardVelocityStartFrame = 1,
            ForwardVelocityEndFrame = startup + active,
            CancelStartFrame = startup + 1,
            CancelEndFrame = startup + active + 2,
            CancelTags = new List<string> { "super" },
            Tags = new List<string> { "normal", "command_normal", "gap_closer" },
        };

        for (var hit = 0; hit < hitCount; hit++)
        {
            var hitFrame = startup + Mathf.RoundToInt(
                hit * Mathf.Max(1, active - 1) / (float)Mathf.Max(1, hitCount));
            move.CombatBoxes.Add(new CombatBoxDefinition
            {
                Id = $"{id}_hit_{hit + 1}",
                BoxType = CombatBoxType.Hitbox,
                StartFrame = hitFrame,
                EndFrame = Mathf.Min(startup + active - 1, hitFrame + 1),
                OffsetX = 1.15f,
                OffsetY = attackHeight == AttackHeight.Overhead ? 1.28f : 1.05f,
                SizeX = 1.35f,
                SizeY = 0.9f,
                SizeZ = 1.35f,
                DamageOverride = Mathf.CeilToInt(damage / (float)hitCount),
            });
        }

        return move;
    }

    private static MoveData CreateRyuCloseNormal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float offsetX,
        float offsetY,
        float sizeX,
        float sizeY)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = priority,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 16 + damage / 4,
            BlockstunFrames = 9 + damage / 8,
            HitStopFrames = Mathf.Max(3, damage / 5),
            MeterGain = Mathf.Max(5, damage / 3),
            PushbackX = 1.5f + damage * 0.04f,
            MaximumStartRange = 1.55f,
            CancelStartFrame = startup + 1,
            CancelEndFrame = startup + active + 3,
            CancelTags = new List<string> { "special", "super" },
            Tags = new List<string> { "normal", "close", "punish" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                BasicHitbox(
                    $"{id}_hit",
                    startup,
                    startup + active - 1,
                    offsetX,
                    offsetY,
                    sizeX,
                    sizeY,
                    1.3f),
            },
        };
    }

    public static CharacterData CreateWorldWarriorRyuBoss()
    {
        var character = new CharacterData
        {
            Id = "world_warrior_ryu_boss",
            DisplayName = "Ryu",
            Role = CharacterRole.Boss,
            MaxHealth = 480,
            WalkSpeed = 3.8f,
            DashSpeed = 8.2f,
            JumpVelocity = 11.0f,
            Gravity = 28.0f,
            Weight = 1.1f,
            MaxGuardGauge = 110,
            GuardBreakFrames = 72,
            GuardRecoveryDelayFrames = 84,
            GuardRecoveryPerSecond = 18.0f,
            SpriteSheetPath = "res://Assets/Sprites/Ryu/ryu_higgsfield_v4_sheet.png",
            SpriteSheetColumns = 16,
            SpriteSheetRows = 15,
            SpritePixelSize = 0.018f,
            SpriteGroundOffsetPixels = 152.0f,
            AnimationProfileId = "ryu_v4",
            BossPresentationScale = 1.0f,
            TintSpriteSheet = false,
            RoleTags = new List<string> { "boss", "world_warrior", "shoto" },
            SynergyTags = new List<string> { "martial_arts", "fire", "projectile" },
            IntroAnimation = new SpriteAnimationClipData
            {
                // Higgsfield boss intro (SF4-style): Ryu grips and re-ties his red
                // headband, then drops into his classic guard. Plays once during the
                // boss-intro cinematic, then idle-bounces the stance until FIGHT.
                AtlasPath = "res://Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png",
                AtlasColumns = 4,
                AtlasRows = 2,
                PixelSize = 0.0172f,
                GroundOffsetPixels = 122.0f,
                Frames = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 6 },
                Durations = new List<int> { 12, 10, 14, 20, 14, 8, 10, 18, 10 },
                LoopStartFrameIndex = 5,
                PeakFrameIndex = 3,
                IntroAuraColor = new Color(0.85f, 0.92f, 1.0f),
            },
            CpuProfile = new CpuFighterProfileData
            {
                Id = "world_warrior_ryu_story_cpu",
                ReactionFrames = 7,
                DecisionIntervalFrames = 9,
                GuardHoldFrames = 22,
                MovementCommitmentFrames = 15,
                PreferredRangeMin = 1.15f,
                PreferredRangeMax = 4.8f,
                LaneTolerance = 0.46f,
                DashDistance = 5.0f,
                Aggression = 0.70f,
                GuardChance = 0.70f,
                AntiAirChance = 0.82f,
                PunishChance = 0.80f,
                RetreatChance = 0.24f,
                JumpEvadeChance = 0.10f,
                MistakeChance = 0.09f,
            },
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = "ryu_jab",
                    DisplayName = "Standing Jab",
                    InputCommand = "LP",
                    Priority = 10,
                    StartupFrames = 4,
                    ActiveFrames = 3,
                    RecoveryFrames = 8,
                    Damage = 12,
                    HitstunFrames = 14,
                    BlockstunFrames = 8,
                    HitStopFrames = 3,
                    MeterGain = 5,
                    PushbackX = 1.4f,
                    CancelStartFrame = 5,
                    CancelEndFrame = 10,
                    CancelIntoMoveIds = new List<string>
                    {
                        "ryu_strong",
                        "ryu_light_kick",
                        "ryu_hadouken_light",
                        "ryu_shoryuken_light",
                    },
                    Tags = new List<string> { "normal", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_jab_hit", 4, 6, 0.95f, 1.15f, 1.0f, 0.72f, 1.25f),
                    },
                },
                new()
                {
                    Id = "ryu_strong",
                    DisplayName = "Standing Strong",
                    InputCommand = "MP",
                    Priority = 15,
                    StartupFrames = 6,
                    ActiveFrames = 4,
                    RecoveryFrames = 13,
                    Damage = 20,
                    HitstunFrames = 19,
                    BlockstunFrames = 10,
                    HitStopFrames = 4,
                    MeterGain = 7,
                    PushbackX = 2.1f,
                    CancelStartFrame = 7,
                    CancelEndFrame = 12,
                    CancelIntoMoveIds = new List<string>
                    {
                        "ryu_fierce",
                        "ryu_medium_kick",
                        "ryu_hadouken_medium",
                        "ryu_shoryuken_medium",
                    },
                    Tags = new List<string> { "normal", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_strong_hit", 6, 9, 1.1f, 1.18f, 1.15f, 0.82f, 1.3f),
                    },
                },
                new()
                {
                    Id = "ryu_fierce",
                    DisplayName = "Solar-Plex Strike",
                    InputCommand = "HP",
                    Priority = 20,
                    StartupFrames = 9,
                    ActiveFrames = 5,
                    RecoveryFrames = 19,
                    Damage = 32,
                    HitstunFrames = 25,
                    BlockstunFrames = 13,
                    HitStopFrames = 6,
                    MeterGain = 10,
                    PushbackX = 3.1f,
                    ForwardVelocity = 1.6f,
                    ForwardVelocityStartFrame = 2,
                    ForwardVelocityEndFrame = 9,
                    CancelStartFrame = 10,
                    CancelEndFrame = 15,
                    CancelTags = new List<string> { "special", "super" },
                    Tags = new List<string> { "normal", "heavy", "gap_closer", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_fierce_hit", 9, 13, 1.28f, 1.2f, 1.42f, 0.9f, 1.35f),
                    },
                },
                new()
                {
                    Id = "ryu_light_kick",
                    DisplayName = "Standing Short",
                    InputCommand = "LK",
                    Priority = 11,
                    StartupFrames = 5,
                    ActiveFrames = 4,
                    RecoveryFrames = 10,
                    Damage = 14,
                    HitstunFrames = 15,
                    BlockstunFrames = 8,
                    HitStopFrames = 3,
                    MeterGain = 5,
                    PushbackX = 1.5f,
                    CancelStartFrame = 6,
                    CancelEndFrame = 11,
                    CancelIntoMoveIds = new List<string>
                    {
                        "ryu_medium_kick",
                        "ryu_hadouken_light",
                        "ryu_tatsumaki_light",
                    },
                    Tags = new List<string> { "normal", "kick", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_light_kick_hit", 5, 8, 0.92f, 0.82f, 1.02f, 0.72f, 1.25f),
                    },
                },
                new()
                {
                    Id = "ryu_medium_kick",
                    DisplayName = "Standing Forward",
                    InputCommand = "MK",
                    Priority = 16,
                    StartupFrames = 7,
                    ActiveFrames = 5,
                    RecoveryFrames = 15,
                    Damage = 24,
                    HitstunFrames = 21,
                    BlockstunFrames = 11,
                    HitStopFrames = 5,
                    MeterGain = 8,
                    PushbackX = 2.4f,
                    CancelStartFrame = 8,
                    CancelEndFrame = 14,
                    CancelIntoMoveIds = new List<string>
                    {
                        "ryu_heavy_kick",
                        "ryu_hadouken_medium",
                        "ryu_tatsumaki_medium",
                    },
                    Tags = new List<string> { "normal", "kick" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_medium_kick_hit", 7, 11, 1.12f, 1.04f, 1.35f, 0.82f, 1.3f),
                    },
                },
                new()
                {
                    Id = "ryu_heavy_kick",
                    DisplayName = "Standing Roundhouse",
                    InputCommand = "HK",
                    Priority = 21,
                    StartupFrames = 10,
                    ActiveFrames = 6,
                    RecoveryFrames = 21,
                    Damage = 34,
                    HitstunFrames = 26,
                    BlockstunFrames = 14,
                    HitStopFrames = 6,
                    MeterGain = 10,
                    PushbackX = 3.4f,
                    Tags = new List<string> { "normal", "kick", "heavy" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox("ryu_heavy_kick_hit", 10, 15, 1.24f, 1.36f, 1.7f, 0.92f, 1.35f),
                    },
                },
                CreateRyuCloseNormal(
                    "ryu_close_jab",
                    "Close Jab",
                    "LP",
                    priority: 30,
                    startup: 3,
                    active: 3,
                    recovery: 7,
                    damage: 13,
                    offsetX: 0.82f,
                    offsetY: 1.12f,
                    sizeX: 0.92f,
                    sizeY: 0.72f),
                CreateRyuCloseNormal(
                    "ryu_close_strong",
                    "Close Strong",
                    "MP",
                    priority: 35,
                    startup: 5,
                    active: 4,
                    recovery: 11,
                    damage: 22,
                    offsetX: 0.92f,
                    offsetY: 1.18f,
                    sizeX: 1.05f,
                    sizeY: 0.82f),
                CreateRyuCloseNormal(
                    "ryu_close_fierce",
                    "Close Fierce",
                    "HP",
                    priority: 40,
                    startup: 6,
                    active: 5,
                    recovery: 17,
                    damage: 34,
                    offsetX: 1.0f,
                    offsetY: 1.28f,
                    sizeX: 1.18f,
                    sizeY: 1.0f),
                CreateRyuCloseNormal(
                    "ryu_close_medium_kick",
                    "Close Forward",
                    "MK",
                    priority: 36,
                    startup: 5,
                    active: 5,
                    recovery: 13,
                    damage: 25,
                    offsetX: 1.02f,
                    offsetY: 0.95f,
                    sizeX: 1.25f,
                    sizeY: 0.82f),
                CreateCrouchingNormal(
                    "ryu_crouch_jab",
                    "Crouching Jab",
                    "2LP",
                    priority: 12,
                    startup: 4,
                    active: 3,
                    recovery: 9,
                    damage: 11,
                    hitstun: 14,
                    offsetX: 0.92f,
                    offsetY: 0.7f,
                    sizeX: 1.0f,
                    sizeY: 0.62f,
                    cancelInto: new[] { "ryu_strong", "ryu_hadouken_light", "ryu_shoryuken_light" }),
                CreateCrouchingNormal(
                    "ryu_crouch_strong",
                    "Crouching Strong",
                    "2MP",
                    priority: 16,
                    startup: 6,
                    active: 4,
                    recovery: 13,
                    damage: 19,
                    hitstun: 18,
                    offsetX: 1.0f,
                    offsetY: 0.72f,
                    sizeX: 1.15f,
                    sizeY: 0.7f,
                    cancelTags: new[] { "special", "super" }),
                CreateCrouchingNormal(
                    "ryu_crouch_fierce",
                    "Crouching Fierce",
                    "2HP",
                    priority: 21,
                    startup: 8,
                    active: 5,
                    recovery: 19,
                    damage: 28,
                    hitstun: 24,
                    offsetX: 0.92f,
                    offsetY: 1.1f,
                    sizeX: 1.1f,
                    sizeY: 1.35f,
                    cancelInto: new[] { "ryu_shoryuken_heavy" }),
                CreateCrouchingNormal(
                    "ryu_crouch_short",
                    "Crouching Short",
                    "2LK",
                    priority: 13,
                    startup: 4,
                    active: 3,
                    recovery: 10,
                    damage: 10,
                    hitstun: 13,
                    offsetX: 0.88f,
                    offsetY: 0.3f,
                    sizeX: 1.12f,
                    sizeY: 0.42f,
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "ryu_crouch_medium_kick",
                    "Crouching Forward",
                    "2MK",
                    priority: 18,
                    startup: 7,
                    active: 5,
                    recovery: 17,
                    damage: 22,
                    hitstun: 20,
                    offsetX: 1.18f,
                    offsetY: 0.36f,
                    sizeX: 1.46f,
                    sizeY: 0.48f,
                    cancelTags: new[] { "special" }),
                CreateCrouchingNormal(
                    "ryu_sweep",
                    "Crouching Roundhouse",
                    "2HK",
                    priority: 22,
                    startup: 9,
                    active: 6,
                    recovery: 24,
                    damage: 28,
                    hitstun: 26,
                    offsetX: 1.28f,
                    offsetY: 0.34f,
                    sizeX: 1.78f,
                    sizeY: 0.5f),
                CreateAirNormal(
                    "ryu_air_jab",
                    "Jumping Jab",
                    "LP",
                    priority: 13,
                    startup: 4,
                    active: 4,
                    recovery: 9,
                    damage: 13,
                    hitstun: 15,
                    offsetX: 0.88f,
                    offsetY: 0.82f,
                    sizeX: 1.02f,
                    sizeY: 0.72f,
                    cancelInto: new[] { "ryu_air_strong", "ryu_air_fierce", "ryu_air_roundhouse" }),
                CreateAirNormal(
                    "ryu_air_strong",
                    "Jumping Strong",
                    "MP",
                    priority: 18,
                    startup: 6,
                    active: 5,
                    recovery: 12,
                    damage: 20,
                    hitstun: 19,
                    offsetX: 0.98f,
                    offsetY: 0.7f,
                    sizeX: 1.18f,
                    sizeY: 0.78f,
                    cancelInto: new[] { "ryu_air_fierce", "ryu_air_tatsumaki_medium" }),
                CreateAirNormal(
                    "ryu_air_fierce",
                    "Jumping Fierce",
                    "HP",
                    priority: 23,
                    startup: 7,
                    active: 5,
                    recovery: 14,
                    damage: 29,
                    hitstun: 24,
                    offsetX: 1.02f,
                    offsetY: 0.62f,
                    sizeX: 1.32f,
                    sizeY: 0.9f),
                CreateAirNormal(
                    "ryu_air_short",
                    "Jumping Short",
                    "LK",
                    priority: 14,
                    startup: 4,
                    active: 5,
                    recovery: 9,
                    damage: 12,
                    hitstun: 14,
                    offsetX: 0.92f,
                    offsetY: 0.55f,
                    sizeX: 1.05f,
                    sizeY: 0.68f,
                    cancelInto: new[] { "ryu_air_forward", "ryu_air_tatsumaki_light" }),
                CreateAirNormal(
                    "ryu_air_forward",
                    "Jumping Forward",
                    "MK",
                    priority: 19,
                    startup: 6,
                    active: 6,
                    recovery: 12,
                    damage: 22,
                    hitstun: 20,
                    offsetX: 1.08f,
                    offsetY: 0.62f,
                    sizeX: 1.35f,
                    sizeY: 0.78f,
                    cancelInto: new[] { "ryu_air_roundhouse", "ryu_air_tatsumaki_medium" }),
                CreateAirNormal(
                    "ryu_air_roundhouse",
                    "Jumping Roundhouse",
                    "HK",
                    priority: 24,
                    startup: 8,
                    active: 6,
                    recovery: 16,
                    damage: 31,
                    hitstun: 25,
                    offsetX: 1.18f,
                    offsetY: 0.72f,
                    sizeX: 1.62f,
                    sizeY: 0.84f),
                CreateRyuCommandNormal(
                    "ryu_collarbone_breaker",
                    "Collarbone Breaker",
                    "6MP",
                    priority: 62,
                    startup: 17,
                    active: 4,
                    recovery: 22,
                    damage: 26,
                    attackHeight: AttackHeight.Overhead,
                    forwardVelocity: 1.8f),
                CreateRyuCommandNormal(
                    "ryu_solar_plexus",
                    "Solar Plexus Strike",
                    "6HP",
                    priority: 66,
                    startup: 18,
                    active: 7,
                    recovery: 21,
                    damage: 38,
                    attackHeight: AttackHeight.Mid,
                    forwardVelocity: 2.3f,
                    hitCount: 2),
                new()
                {
                    Id = "ryu_leap_attack",
                    DisplayName = "Leap Attack",
                    InputCommand = "6MP+MK",
                    Priority = 68,
                    StartupFrames = 7,
                    ActiveFrames = 6,
                    RecoveryFrames = 15,
                    Damage = 24,
                    HitstunFrames = 21,
                    BlockstunFrames = 12,
                    HitStopFrames = 5,
                    MeterGain = 8,
                    PushbackX = 2.8f,
                    InitialVelocityY = 5.8f,
                    ForwardVelocity = 3.2f,
                    ForwardVelocityStartFrame = 1,
                    ForwardVelocityEndFrame = 12,
                    AttackHeight = AttackHeight.Overhead,
                    Tags = new List<string> { "normal", "command_normal", "overhead" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox(
                            "ryu_leap_attack_hit",
                            7,
                            12,
                            1.12f,
                            0.82f,
                            1.45f,
                            0.88f,
                            1.35f),
                    },
                },
                CreateRyuThrow(
                    "ryu_shoulder_throw",
                    "Shoulder Throw",
                    "G",
                    priority: 70,
                    damage: 42,
                    launchX: 4.6f),
                CreateRyuThrow(
                    "ryu_back_throw",
                    "Back Throw",
                    "4G",
                    priority: 72,
                    damage: 46,
                    launchX: 5.2f),
                CreateRyuProjectile(
                    "ryu_hadouken_light",
                    "Light Hadouken",
                    "236LP",
                    priority: 80,
                    startup: 13,
                    recovery: 30,
                    damage: 26,
                    velocity: 7.2f),
                CreateRyuProjectile(
                    "ryu_hadouken_medium",
                    "Medium Hadouken",
                    "236MP",
                    priority: 81,
                    startup: 12,
                    recovery: 29,
                    damage: 29,
                    velocity: 8.5f),
                CreateRyuProjectile(
                    "ryu_hadouken_heavy",
                    "Heavy Hadouken",
                    "236HP",
                    priority: 82,
                    startup: 11,
                    recovery: 28,
                    damage: 32,
                    velocity: 9.8f),
                CreateRyuProjectile(
                    "ryu_hadouken_ex",
                    "EX Hadouken",
                    "236LP+MP",
                    priority: 96,
                    startup: 9,
                    recovery: 24,
                    damage: 42,
                    velocity: 11.2f,
                    meterCost: 40,
                    projectileScale: 1.18f),
                CreateRyuShoryuken(
                    "ryu_shoryuken_light",
                    "Light Shoryuken",
                    "623LP",
                    priority: 88,
                    startup: 4,
                    active: 6,
                    recovery: 25,
                    damage: 30,
                    riseVelocity: 8.4f,
                    invulnerableFrames: 3),
                CreateRyuShoryuken(
                    "ryu_shoryuken_medium",
                    "Medium Shoryuken",
                    "623MP",
                    priority: 89,
                    startup: 5,
                    active: 8,
                    recovery: 28,
                    damage: 38,
                    riseVelocity: 9.6f,
                    invulnerableFrames: 4,
                    hitCount: 2),
                CreateRyuShoryuken(
                    "ryu_shoryuken_heavy",
                    "Heavy Shoryuken",
                    "623HP",
                    priority: 90,
                    startup: 6,
                    active: 10,
                    recovery: 32,
                    damage: 48,
                    riseVelocity: 10.8f,
                    invulnerableFrames: 5,
                    hitCount: 2),
                CreateRyuShoryuken(
                    "ryu_shoryuken_ex",
                    "EX Shoryuken",
                    "623LP+MP",
                    priority: 97,
                    startup: 4,
                    active: 12,
                    recovery: 29,
                    damage: 58,
                    riseVelocity: 11.4f,
                    invulnerableFrames: 7,
                    meterCost: 40,
                    hitCount: 3),
                CreateRyuTatsumaki(
                    "ryu_tatsumaki_light",
                    "Light Tatsumaki Senpukyaku",
                    "214LK",
                    priority: 83,
                    startup: 8,
                    active: 6,
                    recovery: 18,
                    damage: 26,
                    forwardVelocity: 3.2f,
                    hitCount: 1,
                    air: false),
                CreateRyuTatsumaki(
                    "ryu_tatsumaki_medium",
                    "Medium Tatsumaki Senpukyaku",
                    "214MK",
                    priority: 84,
                    startup: 8,
                    active: 12,
                    recovery: 20,
                    damage: 34,
                    forwardVelocity: 4.0f,
                    hitCount: 2,
                    air: false),
                CreateRyuTatsumaki(
                    "ryu_tatsumaki_heavy",
                    "Heavy Tatsumaki Senpukyaku",
                    "214HK",
                    priority: 85,
                    startup: 9,
                    active: 18,
                    recovery: 23,
                    damage: 42,
                    forwardVelocity: 4.8f,
                    hitCount: 3,
                    air: false),
                CreateRyuTatsumaki(
                    "ryu_tatsumaki_ex",
                    "EX Tatsumaki Senpukyaku",
                    "214LK+MK",
                    priority: 95,
                    startup: 6,
                    active: 20,
                    recovery: 20,
                    damage: 50,
                    forwardVelocity: 5.2f,
                    hitCount: 4,
                    air: false,
                    meterCost: 40),
                CreateRyuTatsumaki(
                    "ryu_air_tatsumaki_light",
                    "Air Light Tatsumaki",
                    "214LK",
                    priority: 86,
                    startup: 5,
                    active: 8,
                    recovery: 12,
                    damage: 24,
                    forwardVelocity: 2.8f,
                    hitCount: 1,
                    air: true),
                CreateRyuTatsumaki(
                    "ryu_air_tatsumaki_medium",
                    "Air Medium Tatsumaki",
                    "214MK",
                    priority: 87,
                    startup: 5,
                    active: 14,
                    recovery: 12,
                    damage: 32,
                    forwardVelocity: 3.4f,
                    hitCount: 2,
                    air: true),
                CreateRyuTatsumaki(
                    "ryu_air_tatsumaki_heavy",
                    "Air Heavy Tatsumaki",
                    "214HK",
                    priority: 88,
                    startup: 6,
                    active: 20,
                    recovery: 13,
                    damage: 40,
                    forwardVelocity: 3.9f,
                    hitCount: 3,
                    air: true),
                CreateRyuTatsumaki(
                    "ryu_air_tatsumaki_ex",
                    "Air EX Tatsumaki",
                    "214LK+MK",
                    priority: 98,
                    startup: 4,
                    active: 22,
                    recovery: 11,
                    damage: 48,
                    forwardVelocity: 4.3f,
                    hitCount: 4,
                    air: true,
                    meterCost: 40),
                CreateRyuJoudan(
                    "ryu_joudan_light",
                    "Light Joudan Sokutogeri",
                    "41236LK",
                    priority: 84,
                    startup: 14,
                    active: 4,
                    recovery: 20,
                    damage: 27,
                    forwardVelocity: 2.6f),
                CreateRyuJoudan(
                    "ryu_joudan_medium",
                    "Medium Joudan Sokutogeri",
                    "41236MK",
                    priority: 85,
                    startup: 16,
                    active: 5,
                    recovery: 21,
                    damage: 33,
                    forwardVelocity: 3.1f),
                CreateRyuJoudan(
                    "ryu_joudan_heavy",
                    "Heavy Joudan Sokutogeri",
                    "41236HK",
                    priority: 86,
                    startup: 18,
                    active: 6,
                    recovery: 23,
                    damage: 40,
                    forwardVelocity: 3.6f),
                CreateRyuJoudan(
                    "ryu_joudan_ex",
                    "EX Joudan Sokutogeri",
                    "41236LK+MK",
                    priority: 96,
                    startup: 11,
                    active: 8,
                    recovery: 19,
                    damage: 50,
                    forwardVelocity: 4.2f,
                    meterCost: 40),
                CreateRyuProjectile(
                    "ryu_shinku_hadouken",
                    "Shinku Hadouken",
                    "236236HP",
                    priority: 110,
                    startup: 10,
                    recovery: 36,
                    damage: 74,
                    velocity: 10.8f,
                    meterCost: 100,
                    isSuper: true,
                    superFreezeFrames: 20),
                new()
                {
                    Id = "ryu_shin_shoryuken",
                    DisplayName = "Shin Shoryuken",
                    InputCommand = "236236HK",
                    Priority = 112,
                    StartupFrames = 7,
                    ActiveFrames = 24,
                    RecoveryFrames = 34,
                    Damage = 96,
                    HitstunFrames = 44,
                    BlockstunFrames = 22,
                    HitStopFrames = 10,
                    MeterCost = 100,
                    PushbackX = 4.2f,
                    LaunchX = 3.0f,
                    LaunchY = 12.0f,
                    InitialVelocityY = 7.0f,
                    InvulnerableStartupFrames = 7,
                    SuperFreezeFrames = 24,
                    IsLauncher = true,
                    IsSuper = true,
                    IsCinematicSuper = true,
                    MinimumDamageScale = 0.55f,
                    Tags = new List<string> { "super", "launcher", "anti_air", "punish" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        new()
                        {
                            Id = "ryu_shin_shoryuken_hit_1",
                            BoxType = CombatBoxType.Hitbox,
                            StartFrame = 7,
                            EndFrame = 9,
                            OffsetX = 0.9f,
                            OffsetY = 1.2f,
                            SizeX = 1.25f,
                            SizeY = 1.5f,
                            SizeZ = 1.35f,
                            DamageOverride = 28,
                        },
                        new()
                        {
                            Id = "ryu_shin_shoryuken_hit_2",
                            BoxType = CombatBoxType.Hitbox,
                            StartFrame = 15,
                            EndFrame = 17,
                            OffsetX = 0.95f,
                            OffsetY = 1.42f,
                            SizeX = 1.25f,
                            SizeY = 1.65f,
                            SizeZ = 1.35f,
                            DamageOverride = 30,
                        },
                        new()
                        {
                            Id = "ryu_shin_shoryuken_hit_3",
                            BoxType = CombatBoxType.Hitbox,
                            StartFrame = 25,
                            EndFrame = 30,
                            OffsetX = 0.95f,
                            OffsetY = 1.65f,
                            SizeX = 1.35f,
                            SizeY = 1.9f,
                            SizeZ = 1.4f,
                            DamageOverride = 38,
                        },
                    },
                },
                CreateRyuProjectile(
                    "ryu_denjin_hadouken",
                    "Denjin Hadouken",
                    "214214HP",
                    priority: 111,
                    startup: 28,
                    recovery: 28,
                    damage: 66,
                    velocity: 7.0f,
                    meterCost: 100,
                    isSuper: true,
                    superFreezeFrames: 20,
                    projectileScale: 1.16f),
            },
            BossPhases = new List<BossPhaseData>
            {
                new()
                {
                    Id = "measured_challenger",
                    DisplayName = "Measured Challenger",
                    HealthThreshold = 1.0f,
                    TransitionFrames = 0,
                    EnabledMoveIds = new List<string>
                    {
                        "ryu_jab",
                        "ryu_strong",
                        "ryu_light_kick",
                        "ryu_crouch_jab",
                        "ryu_crouch_short",
                        "ryu_sweep",
                        "ryu_collarbone_breaker",
                        "ryu_shoulder_throw",
                        "ryu_hadouken_light",
                        "ryu_shoryuken_light",
                    },
                    AttackIntervalFrames = 66,
                    AttackRange = 5.8f,
                    MovementSpeedMultiplier = 0.92f,
                    AggressionMultiplier = 0.82f,
                    DefenseMultiplier = 0.86f,
                },
                new()
                {
                    Id = "rising_answer",
                    DisplayName = "Rising Answer",
                    HealthThreshold = 0.66f,
                    TransitionFrames = 36,
                    EnabledMoveIds = new List<string>
                    {
                        "ryu_jab",
                        "ryu_fierce",
                        "ryu_medium_kick",
                        "ryu_heavy_kick",
                        "ryu_crouch_strong",
                        "ryu_crouch_fierce",
                        "ryu_crouch_medium_kick",
                        "ryu_sweep",
                        "ryu_solar_plexus",
                        "ryu_leap_attack",
                        "ryu_back_throw",
                        "ryu_hadouken_medium",
                        "ryu_hadouken_heavy",
                        "ryu_shoryuken_medium",
                        "ryu_shoryuken_heavy",
                        "ryu_tatsumaki_medium",
                        "ryu_tatsumaki_heavy",
                        "ryu_joudan_medium",
                        "ryu_joudan_heavy",
                    },
                    AttackIntervalFrames = 50,
                    AttackRange = 6.2f,
                    MovementSpeedMultiplier = 1.08f,
                    AggressionMultiplier = 1.08f,
                    DefenseMultiplier = 1.0f,
                },
                new()
                {
                    Id = "true_world_warrior",
                    DisplayName = "True World Warrior",
                    HealthThreshold = 0.32f,
                    TransitionFrames = 48,
                    EnabledMoveIds = new List<string>
                    {
                        "ryu_shinku_hadouken",
                        "ryu_shin_shoryuken",
                        "ryu_denjin_hadouken",
                        "ryu_shoryuken_ex",
                        "ryu_tatsumaki_ex",
                        "ryu_joudan_ex",
                        "ryu_hadouken_ex",
                        "ryu_shoryuken_heavy",
                        "ryu_tatsumaki_heavy",
                        "ryu_hadouken_heavy",
                        "ryu_fierce",
                        "ryu_heavy_kick",
                        "ryu_crouch_medium_kick",
                        "ryu_sweep",
                    },
                    AttackIntervalFrames = 40,
                    AttackRange = 7.0f,
                    MovementSpeedMultiplier = 1.2f,
                    ReactionFrameModifier = -1,
                    AggressionMultiplier = 1.22f,
                    DefenseMultiplier = 1.12f,
                    MeterGrant = 100,
                },
            },
        };
        RyuMugenAnimationCatalog.Apply(character);
        return character;
    }

    public static CharacterData CreateWorldWarriorRyuForm()
    {
        var form = CreateWorldWarriorRyuBoss();
        form.Id = "world_warrior_ryu_form";
        form.DisplayName = "Ryu Form";
        form.Role = CharacterRole.Striker;
        form.MaxHealth = 265;
        form.WalkSpeed = 4.5f;
        form.DashSpeed = 8.4f;
        form.MaxGuardGauge = 0;
        form.CpuProfile = null;
        form.BossPhases.Clear();
        form.RoleTags = new List<string> { "playable_form", "world_warrior", "shoto" };
        return form;
    }

    public static CharacterData CreateWorldWarriorRookie()
    {
        return CreateWorldWarriorMinion(
            "world_warrior_rookie",
            "Dojo Rookie",
            CharacterRole.Rushdown,
            maxHealth: 64,
            walkSpeed: 3.45f,
            attackName: "Quick Palm",
            startupFrames: 12,
            recoveryFrames: 23,
            damage: 8,
            pushbackX: 1.4f);
    }

    public static CharacterData CreateWorldWarriorStriker()
    {
        return CreateWorldWarriorMinion(
            "world_warrior_striker",
            "Street Challenger",
            CharacterRole.Striker,
            maxHealth: 82,
            walkSpeed: 3.1f,
            attackName: "Turning Kick",
            startupFrames: 16,
            recoveryFrames: 27,
            damage: 11,
            pushbackX: 2.0f);
    }

    public static CharacterData CreateWorldWarriorGrappler()
    {
        return CreateWorldWarriorMinion(
            "world_warrior_grappler",
            "Tournament Grappler",
            CharacterRole.Grappler,
            maxHealth: 122,
            walkSpeed: 2.3f,
            attackName: "Shoulder Throw",
            startupFrames: 20,
            recoveryFrames: 34,
            damage: 15,
            pushbackX: 2.8f);
    }

    private static CharacterData CreateWorldWarriorMinion(
        string id,
        string displayName,
        CharacterRole role,
        int maxHealth,
        float walkSpeed,
        string attackName,
        int startupFrames,
        int recoveryFrames,
        int damage,
        float pushbackX)
    {
        return new CharacterData
        {
            Id = id,
            DisplayName = displayName,
            Role = role,
            MaxHealth = maxHealth,
            WalkSpeed = walkSpeed,
            Weight = role == CharacterRole.Grappler ? 1.3f : 1.0f,
            SpriteSheetPath = $"res://Assets/Sprites/Enemies/{id}_higgsfield_v1.png",
            SpriteSheetColumns = 10,
            SpriteSheetRows = 9,
            SpritePixelSize = 0.018f,
            SpriteGroundOffsetPixels = 120.0f,
            TintSpriteSheet = false,
            RoleTags = new List<string> { "enemy", "world_warrior_qualifier" },
            SynergyTags = new List<string> { "martial_arts" },
            ArcadeEnemyProfile = CreateArcadeEnemyProfile(id, role),
            Moves = new List<MoveData>
            {
                new()
                {
                    Id = $"{id}_attack",
                    DisplayName = attackName,
                    StartupFrames = startupFrames,
                    ActiveFrames = 5,
                    RecoveryFrames = recoveryFrames,
                    Damage = damage,
                    HitstunFrames = 14,
                    PushbackX = pushbackX,
                    Tags = new List<string> { "normal", "martial_arts" },
                    CombatBoxes = new List<CombatBoxDefinition>
                    {
                        BasicHitbox(
                            $"{id}_attack_hit",
                            startupFrames,
                            startupFrames + 4,
                            0.9f,
                            1.0f,
                            role == CharacterRole.Grappler ? 1.18f : 0.96f,
                            0.9f,
                            1.05f),
                    },
                },
            },
        };
    }

    private static ArcadeEnemyProfileData CreateArcadeEnemyProfile(string id, CharacterRole role)
    {
        return role switch
        {
            CharacterRole.Rushdown => new ArcadeEnemyProfileData
            {
                Id = $"{id}_arcade_ai",
                AttackRange = 1.0f,
                PositionTolerance = 0.24f,
                LaneTolerance = 0.38f,
                SlotLaneSpacing = 0.7f,
                RetreatDistance = 2.35f,
                RetreatFrames = 26,
                ReengageDelayFrames = 16,
                ApproachSpeedMultiplier = 0.94f,
                RetreatSpeedMultiplier = 0.88f,
            },
            CharacterRole.Grappler => new ArcadeEnemyProfileData
            {
                Id = $"{id}_arcade_ai",
                AttackRange = 1.12f,
                PositionTolerance = 0.34f,
                LaneTolerance = 0.5f,
                SlotLaneSpacing = 0.5f,
                RetreatDistance = 3.1f,
                RetreatFrames = 48,
                ReengageDelayFrames = 38,
                ApproachSpeedMultiplier = 0.68f,
                RetreatSpeedMultiplier = 0.58f,
            },
            _ => new ArcadeEnemyProfileData
            {
                Id = $"{id}_arcade_ai",
                AttackRange = 1.08f,
                PositionTolerance = 0.28f,
                LaneTolerance = 0.42f,
                SlotLaneSpacing = 0.62f,
                RetreatDistance = 2.7f,
                RetreatFrames = 34,
                ReengageDelayFrames = 25,
                ApproachSpeedMultiplier = 0.82f,
                RetreatSpeedMultiplier = 0.72f,
            },
        };
    }

    private static CombatBoxDefinition BasicHitbox(
        string id,
        int startFrame,
        int endFrame,
        float offsetX,
        float offsetY,
        float sizeX,
        float sizeY,
        float sizeZ)
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
            SizeZ = sizeZ,
        };
    }
}
