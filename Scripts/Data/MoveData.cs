using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

public enum MovePosture
{
    Standing,
    Crouching,
    Air,
}

public enum AttackHeight
{
    Mid,
    Low,
    Overhead,
    Throw,
}

public enum ProjectileVisualType
{
    Orb,
    Beam,
    Disc,
    Burst,
}

public sealed class MoveData
{
    public string Id { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string InputCommand { get; set; } = "";
    public int Priority { get; set; }
    public int InputWindowFrames { get; set; } = 15;
    public int DirectionLeniency { get; set; } = 1;

    public int StartupFrames { get; set; } = 4;
    public int ActiveFrames { get; set; } = 3;
    public int RecoveryFrames { get; set; } = 10;
    public int Damage { get; set; } = 10;
    public int HitstunFrames { get; set; } = 18;
    public int BlockstunFrames { get; set; } = 10;
    public int HitStopFrames { get; set; } = 4;
    public int BlockStopFrames { get; set; } = -1; // -1 = derive from hit-stop
    public int MeterGain { get; set; } = 5;
    public int MeterCost { get; set; }
    public int GuardDamage { get; set; }
    public int SuperFreezeFrames { get; set; }
    public int InvulnerableStartupFrames { get; set; }
    public float MinimumDamageScale { get; set; } = 0.35f;
    public float ProrationScale { get; set; } = 1.0f; // starter proration for follow-up hits

    public float PushbackX { get; set; } = 2.0f;
    public float LaunchX { get; set; }
    public float LaunchY { get; set; }
    public float ForwardVelocity { get; set; }
    public int ForwardVelocityStartFrame { get; set; }
    public int ForwardVelocityEndFrame { get; set; } = -1;
    public float InitialVelocityY { get; set; }
    public float MinimumStartRange { get; set; } = -1.0f;
    public float MaximumStartRange { get; set; } = -1.0f;
    public bool IsLauncher { get; set; }
    public bool CausesGroundBounce { get; set; }
    public bool CausesWallBounce { get; set; }
    public bool CausesHardKnockdown { get; set; }
    public int KnockdownFrames { get; set; } = -1; // -1 = derive (40, or 60 hard)
    public bool IsSuper { get; set; }
    public bool IsCinematicSuper { get; set; }
    public bool Unblockable { get; set; }
    public bool Unparryable { get; set; }
    public bool AllowDuplicateHits { get; set; }
    public bool AllowGround { get; set; } = true;
    public bool AllowAir { get; set; }
    public bool StartsFlight { get; set; }
    public bool CyclesVisualVariantOnStart { get; set; }
    public bool RequiresFlight { get; set; }
    public bool AllowDuringFlight { get; set; }
    public bool EndsFlightOnStart { get; set; }
    public int FlightTimeRestoreFrames { get; set; }
    public MovePosture Posture { get; set; } = MovePosture.Standing;
    public AttackHeight AttackHeight { get; set; } = AttackHeight.Mid;

    public int CancelStartFrame { get; set; } = -1;
    public int CancelEndFrame { get; set; } = -1;
    public int JumpCancelStartFrame { get; set; } = -1;
    public int JumpCancelEndFrame { get; set; } = -1;
    public bool JumpCancelOnHitOnly { get; set; } = true;
    public string NextAutoComboMoveId { get; set; } = "";
    public List<string> CancelIntoMoveIds { get; set; } = new();
    public List<string> CancelTags { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<CombatBoxDefinition> CombatBoxes { get; set; } = new();
    public List<ProjectileSpawnData> ProjectileSpawns { get; set; } = new();
    public string AnimationAtlasPath { get; set; } = "";
    public Dictionary<string, string> AnimationVariantAtlasPaths { get; set; } = new();
    public int AnimationAtlasColumns { get; set; }
    public int AnimationAtlasRows { get; set; }
    public float AnimationPixelSize { get; set; }
    public float AnimationGroundOffsetPixels { get; set; }
    public List<int> AnimationFrameSequence { get; set; } = new();
    public List<int> AnimationFrameDurations { get; set; } = new();

    public int TotalFrames => StartupFrames + ActiveFrames + RecoveryFrames;
    public int FirstActiveFrame => StartupFrames;
    public int LastActiveFrame => StartupFrames + ActiveFrames - 1;

    public IEnumerable<CombatBoxDefinition> GetBoxesForFrame(int moveFrame)
    {
        return CombatBoxes.Where(box => box.IsActiveOnFrame(moveFrame));
    }

    public bool IsInCancelWindow(int moveFrame)
    {
        if (CancelStartFrame < 0 || CancelEndFrame < 0)
        {
            return false;
        }

        return moveFrame >= CancelStartFrame && moveFrame <= CancelEndFrame;
    }

    public bool IsForwardVelocityActive(int moveFrame)
    {
        if (Mathf.IsZeroApprox(ForwardVelocity) || ForwardVelocityEndFrame < ForwardVelocityStartFrame)
        {
            return false;
        }

        return moveFrame >= ForwardVelocityStartFrame && moveFrame <= ForwardVelocityEndFrame;
    }

    public bool CanStartFromAirState(bool isAirborne)
    {
        return isAirborne ? AllowAir : AllowGround;
    }

    public bool CanStartAtRange(float range)
    {
        return (MinimumStartRange < 0.0f || range >= MinimumStartRange)
            && (MaximumStartRange < 0.0f || range <= MaximumStartRange);
    }

    public bool IsInJumpCancelWindow(int moveFrame)
    {
        return JumpCancelStartFrame >= 0
            && JumpCancelEndFrame >= JumpCancelStartFrame
            && moveFrame >= JumpCancelStartFrame
            && moveFrame <= JumpCancelEndFrame;
    }

    public bool CanCancelInto(MoveData targetMove)
    {
        return CancelIntoMoveIds.Contains(targetMove.Id)
            || targetMove.Tags.Any(tag => CancelTags.Contains(tag));
    }
}

public sealed class ProjectileSpawnData
{
    public string Id { get; set; } = "";
    public int SpawnFrame { get; set; }
    public float OffsetX { get; set; } = 1.0f;
    public float OffsetY { get; set; } = 1.0f;
    public float OffsetZ { get; set; }
    public float VelocityX { get; set; } = 8.0f;
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public int LifetimeFrames { get; set; } = 90;
    public float SizeX { get; set; } = 0.8f;
    public float SizeY { get; set; } = 0.6f;
    public float SizeZ { get; set; } = 0.7f;
    public float CollisionOffsetX { get; set; }
    public float CollisionOffsetY { get; set; }
    public float CollisionOffsetZ { get; set; }
    public bool AttachToOwner { get; set; }
    public bool ExpireOnHit { get; set; } = true;
    public ProjectileVisualType VisualType { get; set; } = ProjectileVisualType.Orb;
    public Color VisualColor { get; set; } = new(0.24f, 0.68f, 1.0f);
    public Color EmissionColor { get; set; } = new(0.08f, 0.42f, 1.0f);
    public float VisualScale { get; set; } = 1.0f;
    public string VisualAtlasPath { get; set; } = "";
    public int VisualAtlasColumns { get; set; }
    public int VisualAtlasRows { get; set; }
    public float VisualPixelSize { get; set; }
    public float VisualAnchorX { get; set; } = 0.5f;
    public int VisualFrameTicks { get; set; } = 3;
    public bool VisualLoop { get; set; } = true;
    public List<int> VisualFrameSequence { get; set; } = new();
    public int ClashStrength { get; set; } = 1;
    public bool ClashEligible { get; set; } = true;
}
