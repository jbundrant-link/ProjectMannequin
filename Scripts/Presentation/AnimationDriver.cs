using ProjectMannequin.Combat;

namespace ProjectMannequin.Presentation;

/// <summary>
/// The animation clips the presentation layer can play. Which clip plays is a
/// pure function of combat state; presentation never decides combat outcomes.
/// </summary>
public enum AnimationClipKind
{
    Idle,
    Walk,
    Crouch,
    Dash,
    JumpStartup,
    Jump,
    Landing,
    Attack,
    Block,
    Blockstun,
    Parry,
    Hitstun,
    GuardBreak,
    Knockdown,
    Wakeup,
    FormSwap,
    Flight,
    InstinctEvade,
    Cinematic,
    Defeated,
}

/// <summary>
/// The combat-owned inputs the animation layer consumes each frame. The
/// presentation clock advances only on non-frozen simulation ticks, so any
/// timing derived from it automatically freezes during hitstop and super
/// freeze.
/// </summary>
public readonly record struct AnimationDriverSnapshot(
    CombatActorState State,
    string MoveId,
    int MoveFrame,
    int MoveTotalFrames,
    bool FacingRight,
    int PresentationClock,
    int HitStopFramesRemaining,
    int SuperPauseFramesRemaining,
    int BossPhaseIndex);

/// <summary>
/// The resolved instruction the presentation layer should render. Move clips are
/// driven by <see cref="MoveFrame"/> (the authoritative combat frame); looping
/// state clips are driven by <see cref="StateClipElapsed"/> (frozen-aware).
/// When <see cref="IsFrozen"/> is set, any AnimationTree/AnimationPlayer should
/// hold its current frame and resume from the combat frame afterwards.
/// </summary>
public readonly record struct AnimationDriverState(
    AnimationClipKind Clip,
    string MoveId,
    int MoveFrame,
    int MoveTotalFrames,
    bool FacingRight,
    bool IsFrozen,
    int StateClipElapsed,
    int BossPhaseIndex)
{
    /// <summary>True for clips whose frame is driven by the combat move frame.</summary>
    public bool IsMoveDriven => Clip == AnimationClipKind.Attack;
}

/// <summary>
/// Maps combat state to an <see cref="AnimationDriverState"/>: it is the single
/// contract that makes animation a visual slave to the simulation. It tracks the
/// per-clip start on the frozen-aware presentation clock (so looping clips reset
/// when the clip changes and freeze during hitstop/super freeze) and detects the
/// brief wake-up window when an actor rises from knockdown.
/// </summary>
public sealed class AnimationDriver
{
    public const int WakeupWindowFrames = 12;

    private bool _initialized;
    private AnimationClipKind _lastClip = AnimationClipKind.Idle;
    private CombatActorState _lastState = CombatActorState.Idle;
    private int _clipStartClock;
    private int _wakeupUntilClock = int.MinValue;

    public AnimationDriverState Resolve(in AnimationDriverSnapshot snapshot)
    {
        if (_initialized
            && _lastState == CombatActorState.Knockdown
            && snapshot.State != CombatActorState.Knockdown)
        {
            _wakeupUntilClock = snapshot.PresentationClock + WakeupWindowFrames;
        }

        var baseClip = ResolveClip(snapshot.State);
        var clip = baseClip == AnimationClipKind.Idle && snapshot.PresentationClock < _wakeupUntilClock
            ? AnimationClipKind.Wakeup
            : baseClip;

        if (!_initialized || clip != _lastClip)
        {
            _clipStartClock = snapshot.PresentationClock;
        }

        _initialized = true;
        _lastClip = clip;
        _lastState = snapshot.State;

        var isFrozen = snapshot.HitStopFramesRemaining > 0 || snapshot.SuperPauseFramesRemaining > 0;
        var stateClipElapsed = System.Math.Max(0, snapshot.PresentationClock - _clipStartClock);

        return new AnimationDriverState(
            clip,
            snapshot.MoveId,
            snapshot.MoveFrame,
            snapshot.MoveTotalFrames,
            snapshot.FacingRight,
            isFrozen,
            stateClipElapsed,
            snapshot.BossPhaseIndex);
    }

    public static AnimationClipKind ResolveClip(CombatActorState state)
    {
        return state switch
        {
            CombatActorState.Attacking => AnimationClipKind.Attack,
            CombatActorState.Blocking => AnimationClipKind.Block,
            CombatActorState.Blockstun => AnimationClipKind.Blockstun,
            CombatActorState.Parrying => AnimationClipKind.Parry,
            CombatActorState.Hitstun => AnimationClipKind.Hitstun,
            CombatActorState.GuardBreak => AnimationClipKind.GuardBreak,
            CombatActorState.Knockdown => AnimationClipKind.Knockdown,
            CombatActorState.Dashing => AnimationClipKind.Dash,
            CombatActorState.HomingDash => AnimationClipKind.Dash,
            CombatActorState.JumpStartup => AnimationClipKind.JumpStartup,
            CombatActorState.Jumping => AnimationClipKind.Jump,
            CombatActorState.Landing => AnimationClipKind.Landing,
            CombatActorState.Crouching => AnimationClipKind.Crouch,
            CombatActorState.Walking => AnimationClipKind.Walk,
            CombatActorState.FormSwapping => AnimationClipKind.FormSwap,
            CombatActorState.Flying => AnimationClipKind.Flight,
            CombatActorState.InstinctEvade => AnimationClipKind.InstinctEvade,
            CombatActorState.CinematicLocked => AnimationClipKind.Cinematic,
            CombatActorState.Dead => AnimationClipKind.Defeated,
            _ => AnimationClipKind.Idle,
        };
    }
}
