using System;
using Godot;

namespace ProjectMannequin.Core;

public static class GameConstants
{
    public const int TickRate = 60;
    public const int MaxPlayers = 4;
    public const int InputBufferSize = 60;
    public const int KeyboardDeviceId = -1;

    public const float DefaultWalkSpeed = 5.0f;
    public const float DefaultDashSpeed = 9.0f;
    public const float DefaultJumpVelocity = 12.0f;
    public const float DefaultGravity = 28.0f;
    public const float DefaultLaneSpeedMultiplier = 0.65f;
    public const float DefaultAirControlMultiplier = 0.72f;

    // Wall traversal (wall-run / wall-jump / wall-slide).
    public const int WallRunFrames = 20;
    public const float WallRunClimbSpeed = 6.5f;
    public const float WallSlideFallSpeed = -3.0f;

    public const int JumpStartupFrames = 4;
    public const int LandingRecoveryFrames = 4;
    public const int DashDurationFrames = 13;
    public const int DashAttackCancelFrame = 4;
    public const int DashJumpCancelFrame = 3;
    public const int HomingDashDurationFrames = 24;
    public const int HomingDashAttackCancelFrame = 7;
    public const float HomingDashSpeed = 12.5f;
    public const int RunDoubleTapWindowFrames = 12;
    public const int ComboDisplayFrames = 90;
    public const int ParryActiveFrames = 6;
    public const int ParryRecoveryFrames = 18;
    public const int PerfectParryRecoilFrames = 28;
    public const int StandardParryRecoilFrames = 18;
    public const int ParryMeterReward = 12;
    public const int WakeupInvulnerabilityFrames = 8;

    public const int DefaultFormSwapCooldownFrames = 180;
    public const int FormSwapStartupFrames = 8;
    public const int FormSwapActiveFrames = 12;
    public const int FormSwapRecoveryFrames = 10;
    public const int DefaultFormSwapMeterCost = 0;

    // Assist System (Marvel Tōkon-style directional assists)
    public const int AssistCooldownFrames = 300;    // 5 seconds between same-form assists
    public const int AssistActiveDuration = 45;     // How long the ghost stays active
    public const int AssistStartupFrames = 6;       // Delay before assist attack comes out
    public const int AssistLockoutFrames = 12;      // Brief lockout on the caller after summoning
    // Assist strike hitbox (the ghost has no CombatBoxes of its own, so the
    // resolver synthesizes a fixed AABB in front of the ghost during its
    // active window). Deterministic constants shared by all assists.
    public const float AssistHitboxForwardOffset = 0.5f; // ahead of the ghost, in facing dir
    public const float AssistHitboxCenterY = 1.0f;       // torso height
    public const float AssistHitboxHalfWidth = 1.2f;
    public const float AssistHitboxHalfHeight = 1.3f;
    public const float AssistHitboxHalfDepth = 0.9f;

    public const int MeterMax = 100;
    public const int MeterGainOnHit = 8;
    public const int MeterGainOnDamageTaken = 4;

    public const float BowlingPinVelocityThreshold = 100.0f;
    public const int BowlingPinCollateralDamage = 15;

    public static readonly Color[] StandardPlayerColors =
    {
        new(0.20f, 0.55f, 1.00f), // P1 blue
        new(1.00f, 0.22f, 0.22f), // P2 red
        new(0.20f, 0.82f, 0.35f), // P3 green
        new(1.00f, 0.86f, 0.20f), // P4 yellow
    };
}

[Flags]
public enum InputButtons : uint
{
    None = 0,
    Up = 1u << 0,
    Down = 1u << 1,
    Left = 1u << 2,
    Right = 1u << 3,
    LightPunch = 1u << 4,
    MediumPunch = 1u << 5,
    HeavyPunch = 1u << 6,
    LightKick = 1u << 7,
    MediumKick = 1u << 8,
    HeavyKick = 1u << 9,
    Jump = 1u << 10,
    Block = 1u << 11,
    Grab = 1u << 12,
    Dash = 1u << 13,
    FormSwap = 1u << 14,
    Start = 1u << 15,
    Crouch = 1u << 16,
    Assist = 1u << 17,
}

public static class InputButtonExtensions
{
    public static bool HasAll(this InputButtons mask, InputButtons buttons)
    {
        return (mask & buttons) == buttons;
    }

    public static bool HasAny(this InputButtons mask, InputButtons buttons)
    {
        return (mask & buttons) != InputButtons.None;
    }
}
