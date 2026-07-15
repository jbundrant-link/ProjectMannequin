using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Presentation;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless tests for the <see cref="AnimationDriver"/> contract: clip mapping
/// from combat state, frozen-aware state-clip timing (holds during hitstop and
/// super freeze), clip-change resets, and the wake-up window out of knockdown.
/// Failures print the resolved clip/elapsed so mapping regressions are clear.
///
/// Run with the environment flag PROJECT_MANNEQUIN_ANIM_TEST=1.
/// </summary>
public static class AnimationDriverTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label, string detail = "")
        {
            if (condition)
            {
                passed++;
                log.Append("  PASS ").Append(label).Append('\n');
            }
            else
            {
                failed++;
                log.Append("  FAIL ").Append(label).Append('\n');
                if (detail.Length > 0)
                {
                    log.Append("       ").Append(detail).Append('\n');
                }
            }
        }

        log.Append("=== Animation Driver Tests ===\n");

        Check(
            AnimationDriver.ResolveClip(CombatActorState.Idle) == AnimationClipKind.Idle
                && AnimationDriver.ResolveClip(CombatActorState.Walking) == AnimationClipKind.Walk
                && AnimationDriver.ResolveClip(CombatActorState.Attacking) == AnimationClipKind.Attack
                && AnimationDriver.ResolveClip(CombatActorState.Knockdown) == AnimationClipKind.Knockdown
                && AnimationDriver.ResolveClip(CombatActorState.Blocking) == AnimationClipKind.Block
                && AnimationDriver.ResolveClip(CombatActorState.Parrying) == AnimationClipKind.Parry
                && AnimationDriver.ResolveClip(CombatActorState.Dead) == AnimationClipKind.Defeated,
            "combat states map to the right clips");

        var attackState = new AnimationDriver().Resolve(Snapshot(
            CombatActorState.Attacking, moveId: "mannequin_uppercut", moveFrame: 7, clock: 100));
        Check(
            attackState.Clip == AnimationClipKind.Attack
                && attackState.MoveId == "mannequin_uppercut"
                && attackState.MoveFrame == 7
                && attackState.IsMoveDriven,
            "attack clip passes through the authoritative move frame");

        Check(
            !new AnimationDriver().Resolve(Snapshot(CombatActorState.Idle, clock: 5)).IsFrozen,
            "no freeze without hitstop or super pause");

        Check(
            new AnimationDriver().Resolve(Snapshot(CombatActorState.Attacking, clock: 5, hitStop: 6)).IsFrozen,
            "hitstop freezes the driver");

        Check(
            new AnimationDriver().Resolve(Snapshot(CombatActorState.Attacking, clock: 5, superPause: 20)).IsFrozen,
            "super pause freezes the driver");

        // Frozen-aware timing: the presentation clock holds during a freeze, so
        // the state-clip elapsed does not advance until it resumes.
        var freezeDriver = new AnimationDriver();
        var t0 = freezeDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 50));
        var t1 = freezeDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 50, hitStop: 4));
        var t2 = freezeDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 55));
        Check(t0.StateClipElapsed == 0, "state-clip elapsed starts at zero", $"elapsed={t0.StateClipElapsed}");
        Check(t1.StateClipElapsed == 0, "elapsed holds while the clock is frozen", $"elapsed={t1.StateClipElapsed}");
        Check(t2.StateClipElapsed == 5, "elapsed resumes once the clock advances", $"elapsed={t2.StateClipElapsed}");

        // Changing clip resets the state-clip elapsed.
        var resetDriver = new AnimationDriver();
        resetDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 10));
        var walkStart = resetDriver.Resolve(Snapshot(CombatActorState.Walking, clock: 20));
        var walkLater = resetDriver.Resolve(Snapshot(CombatActorState.Walking, clock: 26));
        Check(
            walkStart.StateClipElapsed == 0 && walkLater.StateClipElapsed == 6,
            "changing clip resets the state-clip elapsed",
            $"start={walkStart.StateClipElapsed} later={walkLater.StateClipElapsed}");

        // Wake-up window when rising from knockdown.
        var wakeDriver = new AnimationDriver();
        wakeDriver.Resolve(Snapshot(CombatActorState.Knockdown, clock: 40));
        var risingClip = wakeDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 45));
        var recoveredClip = wakeDriver.Resolve(Snapshot(CombatActorState.Idle, clock: 45 + AnimationDriver.WakeupWindowFrames + 1));
        Check(risingClip.Clip == AnimationClipKind.Wakeup, "rising from knockdown plays the wake-up clip", $"clip={risingClip.Clip}");
        Check(recoveredClip.Clip == AnimationClipKind.Idle, "wake-up returns to idle after the window", $"clip={recoveredClip.Clip}");

        var facing = new AnimationDriver().Resolve(Snapshot(CombatActorState.Idle, clock: 1, facingRight: false));
        Check(!facing.FacingRight, "facing passes through");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }

    private static AnimationDriverSnapshot Snapshot(
        CombatActorState state,
        string moveId = "",
        int moveFrame = 0,
        int clock = 0,
        int hitStop = 0,
        int superPause = 0,
        bool facingRight = true)
    {
        return new AnimationDriverSnapshot(
            state,
            moveId,
            moveFrame,
            MoveTotalFrames: 20,
            facingRight,
            clock,
            hitStop,
            superPause,
            BossPhaseIndex: 0);
    }
}
