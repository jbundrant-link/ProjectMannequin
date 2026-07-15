using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.Data;

public enum CharacterRole
{
    Mannequin,
    Striker,
    Grappler,
    Zoner,
    Rushdown,
    Boss,
    Support,
}

public sealed class CharacterData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public CharacterRole Role { get; set; } = CharacterRole.Mannequin;

    public int MaxHealth { get; set; } = 100;
    public int MaxMeter { get; set; } = GameConstants.MeterMax;
    public float WalkSpeed { get; set; } = GameConstants.DefaultWalkSpeed;
    public float DashSpeed { get; set; } = GameConstants.DefaultDashSpeed;
    public float JumpVelocity { get; set; } = GameConstants.DefaultJumpVelocity;
    public float Gravity { get; set; } = GameConstants.DefaultGravity;
    public float Weight { get; set; } = 1.0f;
    public int MaxGuardGauge { get; set; }
    public float ParryReflectPushback { get; set; } = -1.0f; // -1 = derive (8, or 16 for bosses)
    public int GuardBreakFrames { get; set; } = 90;
    public int GuardRecoveryDelayFrames { get; set; } = 90;
    public float GuardRecoveryPerSecond { get; set; } = 18.0f;
    public string SpriteSheetPath { get; set; } = "";
    public int SpriteSheetColumns { get; set; } = 10;
    public int SpriteSheetRows { get; set; } = 9;
    public float SpritePixelSize { get; set; } = 0.018f;
    public float SpriteGroundOffsetPixels { get; set; } = 120.0f;
    public float BossPresentationScale { get; set; } = 1.18f;
    public bool TintSpriteSheet { get; set; }
    public string AnimationProfileId { get; set; } = "mannequin";
    public string SelectPortraitPath { get; set; } = "";

    public CombatBoxDefinition Hurtbox { get; set; } = new()
    {
        Id = "standing_hurtbox",
        BoxType = CombatBoxType.Hurtbox,
        OffsetY = 1.0f,
        SizeX = 0.9f,
        SizeY = 2.0f,
        SizeZ = 0.9f,
    };

    public CombatBoxDefinition Pushbox { get; set; } = new()
    {
        Id = "standing_pushbox",
        BoxType = CombatBoxType.Pushbox,
        OffsetY = 1.0f,
        SizeX = 0.8f,
        SizeY = 1.8f,
        SizeZ = 0.8f,
    };

    public CombatBoxDefinition CrouchingHurtbox { get; set; } = new()
    {
        Id = "crouching_hurtbox",
        BoxType = CombatBoxType.Hurtbox,
        OffsetY = 0.72f,
        SizeX = 1.0f,
        SizeY = 1.42f,
        SizeZ = 0.9f,
    };

    public CombatBoxDefinition CrouchingPushbox { get; set; } = new()
    {
        Id = "crouching_pushbox",
        BoxType = CombatBoxType.Pushbox,
        OffsetY = 0.68f,
        SizeX = 0.9f,
        SizeY = 1.34f,
        SizeZ = 0.8f,
    };

    public List<string> RoleTags { get; set; } = new();
    public List<string> SynergyTags { get; set; } = new();
    public List<MoveData> Moves { get; set; } = new();
    public List<BossPhaseData> BossPhases { get; set; } = new();
    public List<CharacterVisualVariantData> VisualVariants { get; set; } = new();
    public FlightProfileData? FlightProfile { get; set; }
    public CpuFighterProfileData? CpuProfile { get; set; }
    public ArcadeEnemyProfileData? ArcadeEnemyProfile { get; set; }

    /// <summary>
    /// Optional presentation-only clip played during the boss-fight intro
    /// cinematic (Phase 1). Wall-clock timed so it animates while the simulation
    /// is frozen. Null =&gt; the actor simply holds its idle stance.
    /// </summary>
    public SpriteAnimationClipData? IntroAnimation { get; set; }

    public MoveData? FindMove(string moveId)
    {
        return Moves.FirstOrDefault(move => move.Id == moveId);
    }

    public CharacterVisualVariantData? FindVisualVariant(string variantId)
    {
        return VisualVariants.FirstOrDefault(variant => variant.Id == variantId);
    }
}

public sealed class CharacterVisualVariantData
{
    public string Id { get; set; } = "";
    public string SpriteSheetPath { get; set; } = "";
    public int SpriteSheetColumns { get; set; } = 10;
    public int SpriteSheetRows { get; set; } = 9;
    public float SpritePixelSize { get; set; } = 0.018f;
    public float SpriteGroundOffsetPixels { get; set; } = 120.0f;
    public float PresentationScale { get; set; } = 1.0f;
    public bool TintSpriteSheet { get; set; }
    public string AnimationProfileId { get; set; } = "";
    public Color AuraColor { get; set; } = Colors.Transparent;
    public SpriteAnimationClipData? InstinctEvadeAnimation { get; set; }
}

public sealed class SpriteAnimationClipData
{
    public string AtlasPath { get; set; } = "";
    public int AtlasColumns { get; set; } = 1;
    public int AtlasRows { get; set; } = 1;
    public float PixelSize { get; set; } = 0.018f;
    public float GroundOffsetPixels { get; set; } = 120.0f;
    public List<int> Frames { get; set; } = new();
    public List<int> Durations { get; set; } = new();

    /// <summary>
    /// For intro clips: after the flourish plays through once, only frames from
    /// this index to the end keep looping (the fighting-game "settle into combat
    /// stance" tail, like a MUGEN state-190 intro returning to idle). 0 loops the
    /// whole clip. Ignored by non-intro (state) clips.
    /// </summary>
    public int LoopStartFrameIndex { get; set; }

    /// <summary>
    /// For intro clips: the frame index (into <see cref="Frames"/>) where the
    /// power-up peaks. A ki-aura ramps up to this frame then fades as the fighter
    /// settles into stance.
    /// </summary>
    public int PeakFrameIndex { get; set; }

    /// <summary>
    /// For intro clips: the ki-aura colour flared behind the fighter around the
    /// power-up peak. Transparent (default) = no aura.
    /// </summary>
    public Color IntroAuraColor { get; set; }
}

public sealed class FlightProfileData
{
    public string ActivationMoveId { get; set; } = "";
    public int DurationFrames { get; set; } = 180;
    public int ActivationCooldownFrames { get; set; } = 240;
    public int NaturalLandingRecoveryFrames { get; set; } = 18;
    public int ManualCancelRecoveryFrames { get; set; } = 10;
    public int HitExtensionFrames { get; set; } = 24;
    public int SharedMobilityLockoutFrames { get; set; } = 18;
    public float MinimumHeight { get; set; } = 0.8f;
    public float MaximumHeight { get; set; } = 3.3f;
    public float HorizontalSpeed { get; set; } = 6.6f;
    public float VerticalSpeed { get; set; } = 5.4f;
    public float LaneSpeed { get; set; } = 2.2f;
    public float Acceleration { get; set; } = 0.18f;
}

public sealed class BossPhaseData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public float HealthThreshold { get; set; } = 1.0f;
    public List<string> EnabledMoveIds { get; set; } = new();
    public int TransitionFrames { get; set; } = 36;
    public int AttackIntervalFrames { get; set; } = 72;
    public float AttackRange { get; set; } = 2.1f;
    public float MovementSpeedMultiplier { get; set; } = 1.0f;
    public int ReactionFrameModifier { get; set; }
    public float AggressionMultiplier { get; set; } = 1.0f;
    public float DefenseMultiplier { get; set; } = 1.0f;
    public int MeterGrant { get; set; }
    public string VisualVariantId { get; set; } = "";
    public bool FlightEnabled { get; set; }
    public int InstinctCharges { get; set; }
    public int InstinctCooldownFrames { get; set; } = 90;
    public float DamageMultiplier { get; set; } = 1.0f;
    public SpriteAnimationClipData? TransitionAnimation { get; set; }

    // Phase Burst: authored transition shockwave. Defaults reproduce the
    // historical hardcoded burst so existing bosses are unchanged.
    public bool TriggersPhaseBurst { get; set; } = true;
    public float PhaseBurstPushback { get; set; } = 25.0f;
    public int PhaseBurstHitstunFrames { get; set; } = 40;
    public int PhaseBurstDamageBuffPercent { get; set; } = 25;
    public int PhaseBurstDefenseBuffPercent { get; set; } = 25;
    public float PhaseBurstBoundsExpansion { get; set; } = 8.0f;

    // Extra intentional-neutral frames after the phase burst before the AI acts.
    public int PhaseBurstRecoveryFrames { get; set; }

    // Discretionary AI modules this phase disables (see AiModules.Module* keys).
    public List<string> DisabledAiModules { get; set; } = new();
}
