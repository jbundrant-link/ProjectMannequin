using System.Collections.Generic;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

public static class GokuRosterFactory
{
    private const string GokuSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_higgsfield_v1_sheet.png";
    private const string KaiokenSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_kaioken_higgsfield_v1_sheet.png";
    private const string FalseSuperSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_false_super_higgsfield_v1_sheet.png";
    private const string SuperSaiyan1SheetPath =
        "res://Assets/Sprites/Goku/goku_astral_ss1_higgsfield_v1_sheet.png";
    private const string SuperSaiyan2SheetPath =
        "res://Assets/Sprites/Goku/goku_astral_ss2_higgsfield_v1_sheet.png";
    private const string SuperSaiyan3SheetPath =
        "res://Assets/Sprites/Goku/goku_astral_ss3_higgsfield_v1_sheet.png";
    private const string SuperSaiyan4SheetPath =
        "res://Assets/Sprites/Goku/goku_astral_ss4_higgsfield_v1_sheet.png";
    private const string GodSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_god_higgsfield_v1_sheet.png";
    private const string BlueSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_blue_higgsfield_v1_sheet.png";
    private const string BlueKaiokenSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_blue_kaioken_higgsfield_v1_sheet.png";
    private const string UltraInstinctSignSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_ui_sign_higgsfield_v1_sheet.png";
    private const string MasteredUltraInstinctSheetPath =
        "res://Assets/Sprites/Goku/goku_astral_instinct_higgsfield_v1_sheet.png";

    public static CharacterData CreateGokuBoss()
    {
        return new CharacterData
        {
            Id = "astral_goku_boss",
            DisplayName = "Goku",
            Role = CharacterRole.Boss,
            MaxHealth = 1440,
            WalkSpeed = 4.25f,
            DashSpeed = 9.2f,
            JumpVelocity = 12.4f,
            Gravity = 27.0f,
            Weight = 1.0f,
            MaxGuardGauge = 125,
            GuardBreakFrames = 70,
            GuardRecoveryDelayFrames = 92,
            GuardRecoveryPerSecond = 15.0f,
            SpriteSheetPath = GokuSheetPath,
            SpriteSheetColumns = 8,
            SpriteSheetRows = 18,
            SpritePixelSize = 0.0144f,
            SpriteGroundOffsetPixels = 152.0f,
            AnimationProfileId = "goku",
            BossPresentationScale = 1.0f,
            TintSpriteSheet = false,
            RoleTags = new List<string> { "boss", "astral", "rushdown", "zoner" },
            SynergyTags = new List<string>
            {
                "martial_arts",
                "energy",
                "flight",
                "speed",
            },
            IntroAnimation = new SpriteAnimationClipData
            {
                // Higgsfield boss intro (DBZ-style): Goku braces and ki-charges to a
                // peak power-up scream, then drops into his stance. Plays once during
                // the boss-intro cinematic, then idle-bounces the stance until FIGHT.
                AtlasPath = "res://Assets/Sprites/Goku/goku_intro_higgsfield_v1.png",
                AtlasColumns = 4,
                AtlasRows = 2,
                PixelSize = 0.0175f,
                GroundOffsetPixels = 122.0f,
                Frames = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 6 },
                Durations = new List<int> { 12, 8, 12, 22, 14, 8, 10, 16, 10 },
                LoopStartFrameIndex = 5,
                PeakFrameIndex = 3,
                IntroAuraColor = new Color(1.0f, 0.86f, 0.32f),
            },
            FlightProfile = new FlightProfileData
            {
                ActivationMoveId = "goku_flight_start",
                DurationFrames = 180,
                ActivationCooldownFrames = 250,
                NaturalLandingRecoveryFrames = 20,
                ManualCancelRecoveryFrames = 9,
                HitExtensionFrames = 24,
                SharedMobilityLockoutFrames = 18,
                MinimumHeight = 0.9f,
                MaximumHeight = 3.4f,
                HorizontalSpeed = 6.8f,
                VerticalSpeed = 5.5f,
                LaneSpeed = 2.25f,
                Acceleration = 0.2f,
            },
            CpuProfile = new CpuFighterProfileData
            {
                Id = "astral_goku_story_cpu",
                ReactionFrames = 6,
                DecisionIntervalFrames = 8,
                GuardHoldFrames = 20,
                MovementCommitmentFrames = 13,
                PreferredRangeMin = 1.0f,
                PreferredRangeMax = 5.8f,
                LaneTolerance = 0.48f,
                DashDistance = 5.4f,
                Aggression = 0.78f,
                GuardChance = 0.65f,
                AntiAirChance = 0.84f,
                PunishChance = 0.84f,
                RetreatChance = 0.17f,
                JumpEvadeChance = 0.16f,
                MistakeChance = 0.07f,
            },
            Moves = CreateGokuMoves(),
            VisualVariants = CreateGokuVisualVariants(),
            BossPhases = CreateGokuBossPhases(),
        };
    }

    public static CharacterData CreateGokuArchiveForm()
    {
        var form = CreateGokuBoss();
        form.Id = "goku_archive_form";
        form.DisplayName = "Goku Archive Form";
        form.Role = CharacterRole.Striker;
        form.MaxHealth = 290;
        form.WalkSpeed = 4.65f;
        form.DashSpeed = 9.4f;
        form.MaxGuardGauge = 0;
        form.CpuProfile = null;
        form.BossPhases.Clear();
        form.RoleTags = new List<string> { "playable_form", "astral", "rushdown", "zoner" };
        return form;
    }

    public static CharacterData CreateSaibaman()
    {
        return CreateAstralEnemy(
            "astral_saibaman",
            "Saibaman",
            maxHealth: 88,
            walkSpeed: 4.6f,
            attackRange: 0.9f,
            retreatDistance: 2.2f,
            tint: new Color(0.44f, 0.84f, 0.28f),
            moves: new List<MoveData>
            {
                Melee(
                    "saibaman_claw",
                    "Claw Rush",
                    startup: 5,
                    active: 5,
                    recovery: 15,
                    damage: 15,
                    reach: 1.05f,
                    forwardVelocity: 2.2f),
                Melee(
                    "saibaman_headbutt",
                    "Leaping Headbutt",
                    startup: 9,
                    active: 6,
                    recovery: 21,
                    damage: 22,
                    reach: 1.25f,
                    forwardVelocity: 3.4f),
            });
    }

    public static CharacterData CreateFriezaScout()
    {
        return CreateAstralEnemy(
            "astral_frieza_scout",
            "Frieza Force Scout",
            maxHealth: 105,
            walkSpeed: 3.7f,
            attackRange: 3.8f,
            retreatDistance: 4.8f,
            tint: new Color(0.55f, 0.42f, 0.92f),
            moves: new List<MoveData>
            {
                Projectile(
                    "scout_ki_shot",
                    "Scout Ki Shot",
                    "LP",
                    startup: 13,
                    recovery: 25,
                    damage: 18,
                    velocity: 7.5f,
                    visualType: ProjectileVisualType.Orb,
                    color: new Color(0.72f, 0.32f, 1.0f)),
                Melee(
                    "scout_baton",
                    "Baton Swipe",
                    startup: 7,
                    active: 4,
                    recovery: 18,
                    damage: 16,
                    reach: 1.15f),
            });
    }

    public static CharacterData CreateFriezaHeavy()
    {
        return CreateAstralEnemy(
            "astral_frieza_heavy",
            "Frieza Force Heavy",
            maxHealth: 175,
            walkSpeed: 2.8f,
            attackRange: 1.2f,
            retreatDistance: 2.8f,
            tint: new Color(0.84f, 0.33f, 0.38f),
            moves: new List<MoveData>
            {
                Melee(
                    "heavy_hammer",
                    "Armored Hammer",
                    startup: 14,
                    active: 7,
                    recovery: 25,
                    damage: 31,
                    reach: 1.4f,
                    launchY: 4.5f),
                Melee(
                    "heavy_charge",
                    "Shoulder Charge",
                    startup: 12,
                    active: 8,
                    recovery: 24,
                    damage: 27,
                    reach: 1.55f,
                    forwardVelocity: 3.8f),
            });
    }

    public static CharacterData CreateKiCaptain()
    {
        return CreateAstralEnemy(
            "astral_ki_captain",
            "Ki Captain",
            maxHealth: 145,
            walkSpeed: 3.5f,
            attackRange: 3.1f,
            retreatDistance: 4.2f,
            tint: new Color(0.22f, 0.72f, 0.94f),
            moves: new List<MoveData>
            {
                Projectile(
                    "captain_beam",
                    "Command Beam",
                    "HP",
                    startup: 18,
                    recovery: 32,
                    damage: 27,
                    velocity: 9.0f,
                    visualType: ProjectileVisualType.Beam,
                    color: new Color(0.2f, 0.78f, 1.0f)),
                Melee(
                    "captain_launcher",
                    "Rising Strike",
                    startup: 9,
                    active: 6,
                    recovery: 22,
                    damage: 23,
                    reach: 1.2f,
                    launchY: 8.0f),
            });
    }

    private static List<MoveData> CreateGokuMoves()
    {
        var moves = new List<MoveData>
        {
            Normal("goku_lp", "Quick Jab", "LP", 10, 4, 3, 8, 12, 0.96f, 1.16f),
            Normal("goku_mp", "Driving Palm", "MP", 15, 6, 4, 13, 20, 1.12f, 1.18f),
            Normal("goku_hp", "Heavy Straight", "HP", 20, 9, 5, 18, 31, 1.35f, 1.22f),
            Normal("goku_lk", "Quick Kick", "LK", 11, 5, 4, 10, 14, 1.02f, 0.82f),
            Normal("goku_mk", "Turning Kick", "MK", 16, 7, 5, 15, 23, 1.32f, 1.02f),
            Normal("goku_hk", "Meteor Roundhouse", "HK", 21, 10, 6, 21, 34, 1.62f, 1.34f),
            Normal("goku_2lp", "Crouching Jab", "2LP", 12, 4, 3, 9, 11, 0.98f, 0.68f, MovePosture.Crouching),
            Normal("goku_2mp", "Crouching Palm", "2MP", 16, 6, 4, 13, 19, 1.14f, 0.72f, MovePosture.Crouching),
            Normal("goku_2hp", "Rising Launcher", "2HP", 22, 8, 5, 20, 29, 1.12f, 1.08f, MovePosture.Crouching, launchY: 10.5f),
            Normal("goku_2lk", "Low Toe Kick", "2LK", 13, 4, 3, 10, 10, 1.12f, 0.3f, MovePosture.Crouching, AttackHeight.Low),
            Normal("goku_2mk", "Low Sweep Kick", "2MK", 18, 7, 5, 17, 22, 1.45f, 0.36f, MovePosture.Crouching, AttackHeight.Low),
            Normal("goku_2hk", "Leg Sweep", "2HK", 22, 9, 6, 24, 29, 1.72f, 0.34f, MovePosture.Crouching, AttackHeight.Low),
            AirNormal("goku_air_lp", "Air Jab", "LP", 13, 4, 4, 9, 13, 1.0f, 0.78f),
            AirNormal("goku_air_mp", "Air Hammer Fist", "MP", 18, 6, 5, 12, 21, 1.18f, 0.68f),
            AirNormal("goku_air_hp", "Air Smash", "HP", 23, 7, 5, 15, 30, 1.35f, 0.6f),
            AirNormal("goku_air_lk", "Air Knee", "LK", 14, 4, 5, 9, 12, 1.04f, 0.52f),
            AirNormal("goku_air_mk", "Dive Kick", "MK", 19, 6, 6, 12, 22, 1.38f, 0.55f),
            AirNormal("goku_air_hk", "Axe Kick", "HK", 24, 8, 6, 16, 32, 1.56f, 0.64f),
            DragonRush(),
            new()
            {
                Id = "goku_transform_next",
                DisplayName = "Ascend",
                InputCommand = "22HP",
                Priority = 118,
                StartupFrames = 8,
                ActiveFrames = 1,
                RecoveryFrames = 13,
                Damage = 0,
                HitstunFrames = 0,
                BlockstunFrames = 0,
                CyclesVisualVariantOnStart = true,
                AnimationFrameSequence = FrameRange(0, 6),
                AnimationFrameDurations = new List<int> { 2, 2, 2, 3, 4, 7 },
                Tags = new List<string> { "transformation", "mobility" },
            },
            new()
            {
                Id = "goku_kaioken_buff",
                DisplayName = "Kaioken",
                InputCommand = "22LK",
                Priority = 120,
                StartupFrames = 12,
                ActiveFrames = 1,
                RecoveryFrames = 20,
                SuperFreezeFrames = 30,
                IsSuper = true,
                MeterCost = 100,
                Damage = 0,
                CyclesVisualVariantOnStart = true,
                AnimationFrameSequence = FrameRange(0, 6),
                AnimationFrameDurations = new List<int> { 2, 2, 2, 3, 4, 7 },
                Tags = new List<string> { "transformation", "buff" },
            },
        };

        moves.Add(Projectile(
            "goku_kamehameha_light",
            "Light Kamehameha",
            "236LP",
            12,
            28,
            29,
            8.4f,
            ProjectileVisualType.Beam,
            new Color(0.26f, 0.72f, 1.0f)));
        moves.Add(Projectile(
            "goku_kamehameha_heavy",
            "Heavy Kamehameha",
            "236HP",
            16,
            34,
            39,
            9.8f,
            ProjectileVisualType.Beam,
            new Color(0.2f, 0.66f, 1.0f),
            clashStrength: 2,
            visualScale: 1.25f));
        moves.Add(RisingAttack(
            "goku_dragon_rising",
            "Dragon Rising Fist",
            "623HP",
            6,
            11,
            30,
            48));
        moves.Add(RushAttack(
            "goku_dragon_flash",
            "Dragon Flash Rush",
            "236HK",
            10,
            12,
            22,
            42,
            5.6f));
        moves.Add(RushAttack(
            "goku_instant_step",
            "Instant Step",
            "214MP",
            5,
            6,
            14,
            26,
            7.2f,
            invulnerableFrames: 5));
        moves.Add(RushAttack(
            "goku_meteor_smash",
            "Meteor Smash",
            "214HK",
            12,
            14,
            25,
            52,
            4.5f,
            launchY: 7.0f));

        moves.Add(new MoveData
        {
            Id = "goku_flight_start",
            DisplayName = "Ki Flight",
            InputCommand = "214HP",
            Priority = 92,
            StartupFrames = 12,
            ActiveFrames = 1,
            RecoveryFrames = 5,
            Damage = 0,
            HitstunFrames = 0,
            BlockstunFrames = 0,
            StartsFlight = true,
            Tags = new List<string> { "special", "flight", "mobility" },
            AnimationFrameSequence = FrameRange(20, 6),
        });
        moves.Add(new MoveData
        {
            Id = "goku_flight_cancel",
            DisplayName = "Flight Cancel",
            InputCommand = "4HP",
            Priority = 102,
            StartupFrames = 1,
            ActiveFrames = 1,
            RecoveryFrames = 8,
            Damage = 0,
            HitstunFrames = 0,
            BlockstunFrames = 0,
            AllowGround = false,
            AllowAir = true,
            RequiresFlight = true,
            AllowDuringFlight = true,
            EndsFlightOnStart = true,
            Tags = new List<string> { "special", "flight", "mobility" },
            AnimationFrameSequence = FrameRange(20, 3),
        });
        moves.Add(FlightProjectile());
        moves.Add(FlightRush());
        moves.Add(FlightDiveKick());

        moves.Add(Projectile(
            "goku_super_kamehameha",
            "Super Kamehameha",
            "236236HP",
            10,
            38,
            82,
            11.4f,
            ProjectileVisualType.Beam,
            new Color(0.32f, 0.82f, 1.0f),
            meterCost: 100,
            isSuper: true,
            clashStrength: 4,
            visualScale: 1.65f));
        moves.Add(Projectile(
            "goku_god_kamehameha",
            "God Kamehameha",
            "236236LP",
            8,
            34,
            91,
            12.6f,
            ProjectileVisualType.Beam,
            new Color(0.1f, 0.66f, 1.0f),
            meterCost: 100,
            isSuper: true,
            clashStrength: 5,
            visualScale: 1.9f));
        moves.Add(SpiritBomb());
        moves.Add(InstinctRush());

        GokuMugenAnimationCatalog.Apply(moves);
        return moves;
    }

    private static CharacterData CreateAstralEnemy(
        string id,
        string displayName,
        int maxHealth,
        float walkSpeed,
        float attackRange,
        float retreatDistance,
        Color tint,
        List<MoveData> moves)
    {
        return new CharacterData
        {
            Id = id,
            DisplayName = displayName,
            Role = maxHealth >= 150
                ? CharacterRole.Grappler
                : attackRange > 2.0f
                    ? CharacterRole.Zoner
                    : CharacterRole.Rushdown,
            MaxHealth = maxHealth,
            WalkSpeed = walkSpeed,
            DashSpeed = walkSpeed * 1.65f,
            JumpVelocity = 8.5f,
            Gravity = 27.0f,
            Weight = maxHealth >= 150 ? 1.5f : 0.9f,
            SpriteSheetPath =
                $"res://Assets/Sprites/Enemies/{id}_higgsfield_v1.png",
            SpriteSheetColumns = 10,
            SpriteSheetRows = 9,
            SpritePixelSize = 0.018f,
            SpriteGroundOffsetPixels = 120.0f,
            TintSpriteSheet = false,
            RoleTags = new List<string> { "enemy", "astral" },
            SynergyTags = new List<string> { "energy" },
            ArcadeEnemyProfile = new ArcadeEnemyProfileData
            {
                Id = $"{id}_arcade_ai",
                AttackRange = attackRange,
                PositionTolerance = 0.3f,
                LaneTolerance = 0.45f,
                SlotLaneSpacing = 0.62f,
                RetreatDistance = retreatDistance,
                RetreatFrames = 30,
                ReengageDelayFrames = 25,
                ApproachSpeedMultiplier = 0.86f,
                RetreatSpeedMultiplier = 0.76f,
            },
            Moves = moves,
        };
    }

    private static List<CharacterVisualVariantData> CreateGokuVisualVariants()
    {
        return new List<CharacterVisualVariantData>
        {
            VisualVariant("base", GokuSheetPath, Colors.Transparent),
            VisualVariant("kaioken", KaiokenSheetPath, new Color(1.0f, 0.12f, 0.16f)),
            VisualVariant("false_super", FalseSuperSheetPath, new Color(0.92f, 0.72f, 0.12f)),
            VisualVariant("ss1", SuperSaiyan1SheetPath, new Color(1.0f, 0.78f, 0.12f)),
            VisualVariant("ss2", SuperSaiyan2SheetPath, new Color(0.88f, 0.86f, 0.3f)),
            VisualVariant("ss3", SuperSaiyan3SheetPath, new Color(1.0f, 0.72f, 0.08f)),
            VisualVariant("ss4", SuperSaiyan4SheetPath, new Color(0.96f, 0.12f, 0.42f)),
            VisualVariant("god", GodSheetPath, new Color(1.0f, 0.18f, 0.12f)),
            VisualVariant("blue", BlueSheetPath, new Color(0.12f, 0.72f, 1.0f)),
            VisualVariant(
                "blue_kaioken",
                BlueKaiokenSheetPath,
                new Color(0.88f, 0.16f, 0.38f)),
            VisualVariant(
                "ui_sign",
                UltraInstinctSignSheetPath,
                new Color(0.55f, 0.78f, 1.0f)),
            VisualVariant(
                "instinct",
                MasteredUltraInstinctSheetPath,
                new Color(0.76f, 0.9f, 1.0f)),
        };
    }

    private static List<BossPhaseData> CreateGokuBossPhases()
    {
        return new List<BossPhaseData>
        {
            CreateBossPhase(
                "base_challenger",
                "Base Challenger",
                1.0f,
                "base",
                GokuSheetPath,
                0,
                BasePhaseMoves()),
            CreateBossPhase(
                "kaioken_burst",
                "Kaioken",
                0.92f,
                "kaioken",
                KaiokenSheetPath,
                1,
                BasePhaseMoves()),
            CreateBossPhase(
                "false_super_saiyan",
                "False Super Saiyan",
                0.83f,
                "false_super",
                FalseSuperSheetPath,
                2,
                BasePhaseMoves()),
            CreateBossPhase(
                "super_saiyan",
                "Super Saiyan",
                0.75f,
                "ss1",
                SuperSaiyan1SheetPath,
                3,
                SaiyanPhaseMoves()),
            CreateBossPhase(
                "super_saiyan_2",
                "Super Saiyan 2",
                0.67f,
                "ss2",
                SuperSaiyan2SheetPath,
                4,
                SaiyanPhaseMoves()),
            CreateBossPhase(
                "super_saiyan_3",
                "Super Saiyan 3",
                0.58f,
                "ss3",
                SuperSaiyan3SheetPath,
                5,
                SaiyanPhaseMoves()),
            CreateBossPhase(
                "super_saiyan_4",
                "Super Saiyan 4",
                0.5f,
                "ss4",
                SuperSaiyan4SheetPath,
                6,
                SaiyanPhaseMoves()),
            CreateBossPhase(
                "super_saiyan_god",
                "Super Saiyan God",
                0.42f,
                "god",
                GodSheetPath,
                7,
                DivinePhaseMoves(),
                flightEnabled: true),
            CreateBossPhase(
                "super_saiyan_blue",
                "Super Saiyan Blue",
                0.33f,
                "blue",
                BlueSheetPath,
                8,
                DivinePhaseMoves(),
                flightEnabled: true,
                transitionFrames: 51,
                transitionAnimation: GokuMugenAnimationCatalog.CreateBlueTransitionClip()),
            CreateBossPhase(
                "blue_kaioken",
                "Blue Kaioken",
                0.25f,
                "blue_kaioken",
                BlueKaiokenSheetPath,
                9,
                DivinePhaseMoves(),
                flightEnabled: true),
            CreateBossPhase(
                "ultra_instinct_sign",
                "Ultra Instinct Sign",
                0.17f,
                "ui_sign",
                UltraInstinctSignSheetPath,
                10,
                InstinctPhaseMoves(),
                flightEnabled: true,
                instinctCharges: 1),
            CreateBossPhase(
                "mastered_ultra_instinct",
                "Mastered Ultra Instinct",
                0.08f,
                "instinct",
                MasteredUltraInstinctSheetPath,
                11,
                InstinctPhaseMoves(),
                flightEnabled: true,
                instinctCharges: 3,
                transitionFrames: 128,
                transitionAnimation: GokuMugenAnimationCatalog.CreateInstinctTransitionClip()),
        };
    }

    private static BossPhaseData CreateBossPhase(
        string id,
        string displayName,
        float healthThreshold,
        string variantId,
        string variantAtlasPath,
        int powerTier,
        List<string> enabledMoveIds,
        bool flightEnabled = false,
        int instinctCharges = 0,
        int transitionFrames = 22,
        SpriteAnimationClipData? transitionAnimation = null)
    {
        return new BossPhaseData
        {
            Id = id,
            DisplayName = displayName,
            HealthThreshold = healthThreshold,
            TransitionFrames = powerTier == 0 ? 0 : transitionFrames,
            AttackIntervalFrames = Mathf.Max(38, 64 - powerTier * 2),
            AttackRange = 2.25f + powerTier * 0.16f,
            MovementSpeedMultiplier = 1.0f + powerTier * 0.025f,
            ReactionFrameModifier = -(powerTier / 4),
            AggressionMultiplier = 1.0f + powerTier * 0.035f,
            DefenseMultiplier = 1.0f + powerTier * 0.016f,
            DamageMultiplier = 1.0f + powerTier * 0.025f,
            MeterGrant = powerTier == 0 ? 0 : 35,
            VisualVariantId = variantId,
            FlightEnabled = flightEnabled,
            InstinctCharges = instinctCharges,
            InstinctCooldownFrames = instinctCharges > 0 ? 96 : 0,
            TransitionAnimation = powerTier == 0
                ? null
                : transitionAnimation
                    ?? GokuMugenAnimationCatalog.CreateFormTransitionClip(variantAtlasPath),
            EnabledMoveIds = enabledMoveIds,
        };
    }

    private static CharacterVisualVariantData VisualVariant(
        string id,
        string path,
        Color auraColor)
    {
        return new CharacterVisualVariantData
        {
            Id = id,
            SpriteSheetPath = path,
            SpriteSheetColumns = 8,
            SpriteSheetRows = 18,
            SpritePixelSize = 0.0144f,
            SpriteGroundOffsetPixels = 152.0f,
            PresentationScale = 1.0f,
            TintSpriteSheet = false,
            AnimationProfileId = "goku",
            AuraColor = auraColor,
            InstinctEvadeAnimation = id switch
            {
                "ui_sign" => GokuMugenAnimationCatalog.CreateInstinctEvadeClip(
                    GokuMugenAnimationCatalog.UltraInstinctSignSpecialAtlas),
                "instinct" => GokuMugenAnimationCatalog.CreateInstinctEvadeClip(),
                _ => null,
            },
        };
    }

    private static MoveData Normal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float reach,
        float height,
        MovePosture posture = MovePosture.Standing,
        AttackHeight attackHeight = AttackHeight.Mid,
        float launchY = 0.0f)
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
            HitstunFrames = 13 + damage / 2,
            BlockstunFrames = 7 + damage / 5,
            HitStopFrames = damage >= 28 ? 6 : 4,
            MeterGain = 5 + damage / 6,
            PushbackX = 1.2f + damage * 0.055f,
            LaunchY = launchY,
            IsLauncher = launchY > 0.0f,
            Posture = posture,
            AttackHeight = attackHeight,
            CancelStartFrame = startup + 1,
            CancelEndFrame = startup + active + 2,
            CancelTags = new List<string> { "special", "super" },
            Tags = new List<string> { "normal", damage >= 28 ? "heavy" : "punish" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                Hitbox($"{id}_hit", startup, startup + active - 1, reach, height),
            },
        };
    }

    private static MoveData AirNormal(
        string id,
        string displayName,
        string command,
        int priority,
        int startup,
        int active,
        int recovery,
        int damage,
        float reach,
        float height)
    {
        var move = Normal(
            id,
            displayName,
            command,
            priority,
            startup,
            active,
            recovery,
            damage,
            reach,
            height,
            MovePosture.Air);
        move.AllowGround = false;
        move.AllowAir = true;
        move.JumpCancelStartFrame = startup + 1;
        move.JumpCancelEndFrame = startup + active + 2;
        return move;
    }

    private static MoveData Melee(
        string id,
        string displayName,
        int startup,
        int active,
        int recovery,
        int damage,
        float reach,
        float forwardVelocity = 0.0f,
        float launchY = 0.0f)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = "LP",
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 14 + damage / 3,
            BlockstunFrames = 8,
            HitStopFrames = damage > 24 ? 6 : 4,
            PushbackX = 1.6f + damage * 0.05f,
            LaunchY = launchY,
            IsLauncher = launchY > 0.0f,
            ForwardVelocity = forwardVelocity,
            ForwardVelocityStartFrame = 1,
            ForwardVelocityEndFrame = startup + active,
            Tags = new List<string> { "normal" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                Hitbox($"{id}_hit", startup, startup + active - 1, reach, 1.0f),
            },
        };
    }

    private static MoveData Projectile(
        string id,
        string displayName,
        string command,
        int startup,
        int recovery,
        int damage,
        float velocity,
        ProjectileVisualType visualType,
        Color color,
        int meterCost = 0,
        bool isSuper = false,
        int clashStrength = 1,
        float visualScale = 1.0f)
    {
        var move = new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = isSuper ? 112 : 82,
            StartupFrames = startup,
            ActiveFrames = 1,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = isSuper ? 42 : 24,
            BlockstunFrames = isSuper ? 22 : 14,
            HitStopFrames = isSuper ? 9 : 5,
            MeterGain = isSuper ? 0 : 9,
            MeterCost = meterCost,
            GuardDamage = damage / 3,
            SuperFreezeFrames = isSuper ? 22 : 0,
            IsSuper = isSuper,
            IsCinematicSuper = isSuper,
            MinimumDamageScale = isSuper ? 0.52f : 0.35f,
            Tags = new List<string> { isSuper ? "super" : "special", "projectile", "zoning" },
            ProjectileSpawns = new List<ProjectileSpawnData>
            {
                new()
                {
                    Id = $"{id}_projectile",
                    SpawnFrame = startup,
                    OffsetX = 1.15f,
                    OffsetY = 1.15f,
                    VelocityX = velocity,
                    LifetimeFrames = 100,
                    SizeX = visualType == ProjectileVisualType.Beam ? 1.5f : 0.82f,
                    SizeY = visualType == ProjectileVisualType.Beam ? 0.42f : 0.72f,
                    SizeZ = 0.76f,
                    VisualType = visualType,
                    VisualColor = color,
                    EmissionColor = color.Darkened(0.18f),
                    VisualScale = visualScale,
                    ClashStrength = clashStrength,
                },
            },
        };

        if (id.StartsWith("goku_"))
        {
            ConfigureGokuProjectileVisual(
                move.ProjectileSpawns[0],
                visualType,
                move.TotalFrames);
        }

        return move;
    }

    private static void ConfigureGokuProjectileVisual(
        ProjectileSpawnData spawn,
        ProjectileVisualType visualType,
        int moveTotalFrames)
    {
        spawn.VisualAtlasColumns = 4;
        spawn.VisualAtlasRows = 2;
        switch (visualType)
        {
            case ProjectileVisualType.Beam:
            {
                var beamStrength = spawn.VisualScale;
                spawn.VisualAtlasPath =
                    "res://Assets/Sprites/Goku/goku_astral_kamehameha_full_lane_higgsfield_v3_sheet.png";
                spawn.VisualAtlasColumns = 2;
                spawn.VisualAtlasRows = 2;
                spawn.SpawnFrame = Mathf.Max(
                    spawn.SpawnFrame,
                    Mathf.RoundToInt(moveTotalFrames * 0.35f));
                spawn.OffsetX = 1.4f;
                spawn.OffsetY = 2.0f;
                spawn.VelocityX = 0.0f;
                spawn.LifetimeFrames = spawn.ClashStrength >= 4 ? 34 : 28;
                spawn.VisualScale = Mathf.Clamp(
                    0.98f + (beamStrength - 1.0f) * 0.08f,
                    0.98f,
                    1.08f);
                spawn.SizeX = 14.2f * spawn.VisualScale;
                spawn.SizeY = 1.55f * spawn.VisualScale;
                spawn.SizeZ = 0.9f;
                spawn.CollisionOffsetX = spawn.SizeX * 0.5f;
                spawn.AttachToOwner = true;
                spawn.ExpireOnHit = false;
                spawn.VisualPixelSize = 0.011f;
                spawn.VisualAnchorX = 0.05f;
                spawn.VisualFrameTicks = 5;
                spawn.VisualLoop = false;
                spawn.VisualFrameSequence = new List<int> { 0, 1, 2, 1, 2, 3 };
                break;
            }
            case ProjectileVisualType.Burst:
                spawn.VisualAtlasPath =
                    "res://Assets/Sprites/Goku/goku_astral_spirit_bomb_fx_higgsfield_v1_sheet.png";
                spawn.VisualPixelSize = 0.0042f;
                spawn.VisualAnchorX = 0.5f;
                spawn.VisualFrameTicks = 5;
                spawn.VisualLoop = false;
                spawn.VisualFrameSequence = new List<int> { 4, 5 };
                break;
            default:
                spawn.VisualAtlasPath =
                    "res://Assets/Sprites/Goku/goku_astral_ki_blast_fx_higgsfield_v1_sheet.png";
                spawn.VisualPixelSize = 0.0018f;
                spawn.VisualAnchorX = 0.5f;
                spawn.VisualFrameTicks = 3;
                spawn.VisualLoop = true;
                spawn.VisualFrameSequence = new List<int> { 3, 4, 5 };
                break;
        }
    }

    private static MoveData RisingAttack(
        string id,
        string displayName,
        string command,
        int startup,
        int active,
        int recovery,
        int damage)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = 90,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 30,
            BlockstunFrames = 15,
            HitStopFrames = 7,
            MeterGain = 12,
            PushbackX = 3.8f,
            LaunchX = 2.2f,
            LaunchY = 11.0f,
            InitialVelocityY = 10.0f,
            InvulnerableStartupFrames = 5,
            IsLauncher = true,
            Tags = new List<string> { "special", "anti_air", "launcher", "punish" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                Hitbox($"{id}_hit", startup, startup + active - 1, 1.1f, 1.45f),
            },
        };
    }

    private static MoveData RushAttack(
        string id,
        string displayName,
        string command,
        int startup,
        int active,
        int recovery,
        int damage,
        float forwardVelocity,
        int invulnerableFrames = 0,
        float launchY = 0.0f)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = displayName,
            InputCommand = command,
            Priority = 86,
            StartupFrames = startup,
            ActiveFrames = active,
            RecoveryFrames = recovery,
            Damage = damage,
            HitstunFrames = 27,
            BlockstunFrames = 14,
            HitStopFrames = 6,
            MeterGain = 11,
            PushbackX = 3.4f,
            LaunchY = launchY,
            IsLauncher = launchY > 0.0f,
            ForwardVelocity = forwardVelocity,
            ForwardVelocityStartFrame = 1,
            ForwardVelocityEndFrame = startup + active,
            InvulnerableStartupFrames = invulnerableFrames,
            Tags = new List<string> { "special", "gap_closer", "rush", "punish" },
            CombatBoxes = new List<CombatBoxDefinition>
            {
                Hitbox($"{id}_hit", startup, startup + active - 1, 1.35f, 1.05f),
            },
        };
    }

    private static MoveData FlightProjectile()
    {
        var move = Projectile(
            "goku_flight_ki_blast",
            "Flight Ki Barrage",
            "236MP",
            7,
            15,
            25,
            10.5f,
            ProjectileVisualType.Orb,
            new Color(0.3f, 0.76f, 1.0f),
            clashStrength: 2);
        move.AllowGround = false;
        move.AllowAir = true;
        move.RequiresFlight = true;
        move.AllowDuringFlight = true;
        move.FlightTimeRestoreFrames = 12;
        move.ProjectileSpawns[0].VelocityY = -2.2f;
        return move;
    }

    private static MoveData FlightRush()
    {
        var move = RushAttack(
            "goku_flight_rush",
            "Sky Dragon Rush",
            "236HK",
            6,
            10,
            15,
            38,
            6.8f);
        move.AllowGround = false;
        move.AllowAir = true;
        move.RequiresFlight = true;
        move.AllowDuringFlight = true;
        move.FlightTimeRestoreFrames = 18;
        return move;
    }

    private static MoveData FlightDiveKick()
    {
        var move = AirNormal(
            "goku_flight_dive",
            "Comet Dive",
            "214HK",
            94,
            6,
            12,
            18,
            42,
            1.42f,
            0.48f);
        move.RequiresFlight = true;
        move.AllowDuringFlight = true;
        move.EndsFlightOnStart = true;
        move.ForwardVelocity = 5.4f;
        move.ForwardVelocityStartFrame = 1;
        move.ForwardVelocityEndFrame = 16;
        move.LaunchY = -4.0f;
        move.Tags = new List<string> { "special", "flight", "overhead", "gap_closer" };
        return move;
    }

    private static MoveData SpiritBomb()
    {
        var move = Projectile(
            "goku_spirit_bomb",
            "Spirit Bomb",
            "214214HP",
            38,
            48,
            118,
            4.4f,
            ProjectileVisualType.Burst,
            new Color(0.55f, 0.9f, 1.0f),
            meterCost: 100,
            isSuper: true,
            clashStrength: 6,
            visualScale: 2.5f);
        move.Unblockable = true;
        move.ProjectileSpawns[0].OffsetY = 2.4f;
        move.ProjectileSpawns[0].VelocityY = -0.65f;
        move.ProjectileSpawns[0].SizeX = 1.65f;
        move.ProjectileSpawns[0].SizeY = 1.65f;
        move.ProjectileSpawns[0].SizeZ = 1.4f;
        return move;
    }

    private static MoveData InstinctRush()
    {
        var move = RushAttack(
            "goku_instinct_rush",
            "Instinct Rush",
            "236236HK",
            5,
            22,
            30,
            104,
            7.8f,
            invulnerableFrames: 8,
            launchY: 8.0f);
        move.Priority = 116;
        move.MeterCost = 100;
        move.IsSuper = true;
        move.IsCinematicSuper = true;
        move.SuperFreezeFrames = 24;
        move.MinimumDamageScale = 0.58f;
        move.Tags = new List<string> { "super", "rush", "launcher", "punish" };
        return move;
    }

    private static CombatBoxDefinition Hitbox(
        string id,
        int startFrame,
        int endFrame,
        float reach,
        float height)
    {
        return new CombatBoxDefinition
        {
            Id = id,
            BoxType = CombatBoxType.Hitbox,
            StartFrame = startFrame,
            EndFrame = endFrame,
            OffsetX = reach * 0.65f,
            OffsetY = height,
            SizeX = reach,
            SizeY = 0.82f,
            SizeZ = 1.32f,
        };
    }

    private static List<string> BasePhaseMoves()
    {
        return new List<string>
        {
            "goku_lp", "goku_mp", "goku_hp", "goku_lk", "goku_mk", "goku_hk",
            "goku_2lp", "goku_2mp", "goku_2hp", "goku_2lk", "goku_2mk", "goku_2hk",
            "goku_air_lp", "goku_air_mp", "goku_air_hp",
            "goku_air_lk", "goku_air_mk", "goku_air_hk",
            "goku_kamehameha_light", "goku_dragon_rising",
            "goku_dragon_flash", "goku_instant_step", "goku_meteor_smash",
            "goku_super_kamehameha", "goku_kaioken_buff",
        };
    }

    private static List<string> SaiyanPhaseMoves()
    {
        var moves = BasePhaseMoves();
        moves.AddRange(new[]
        {
            "goku_kamehameha_heavy",
        });
        return moves;
    }

    private static List<string> DivinePhaseMoves()
    {
        var moves = SaiyanPhaseMoves();
        moves.AddRange(new[]
        {
            "goku_flight_start",
            "goku_flight_cancel",
            "goku_flight_ki_blast",
            "goku_flight_rush",
            "goku_flight_dive",
            "goku_god_kamehameha",
            "goku_spirit_bomb",
        });
        return moves;
    }

    private static List<string> InstinctPhaseMoves()
    {
        var moves = DivinePhaseMoves();
        moves.Add("goku_instinct_rush");
        return moves;
    }

    private static MoveData DragonRush()
    {
        return new MoveData
        {
            Id = "goku_dragon_rush",
            DisplayName = "Dragon Rush",
            InputCommand = "LP+LK", // Throw input
            Priority = 95,
            StartupFrames = 19,
            ActiveFrames = 4,
            RecoveryFrames = 22,
            Damage = 1200,
            BlockstunFrames = 0,
            HitstunFrames = 60,
            HitStopFrames = 15,
            AttackHeight = AttackHeight.Throw,
            Tags = new List<string> { "throw", "special", "unblockable" },
            ForwardVelocity = 5.0f,
            ForwardVelocityStartFrame = 10,
            ForwardVelocityEndFrame = 19,
            IsLauncher = true,
            LaunchY = 8.0f, // DBFZ style massive launcher
            CombatBoxes = new List<CombatBoxDefinition>
            {
                new CombatBoxDefinition
                {
                    Id = "dragon_rush_grab",
                    BoxType = CombatBoxType.Grabbox,
                    StartFrame = 19,
                    EndFrame = 22,
                    OffsetX = 1.0f,
                    OffsetY = 1.0f,
                    SizeX = 1.5f,
                    SizeY = 1.5f,
                },
            },
            AnimationFrameSequence = FrameRange(20, 8),
        };
    }

    private static List<int> FrameRange(int startFrame, int count)
    {
        var sequence = new List<int>(count);
        for (var index = 0; index < count; index++)
        {
            sequence.Add(startFrame + index);
        }

        return sequence;
    }
}
