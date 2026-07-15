using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless tests for the pure training-dummy behavior resolution: each setting
/// produces the right input in representative situations, and the stateful
/// brain tracks "has been hit" and "recovered from hit" correctly across ticks.
/// Failures print the setting and the mask involved.
///
/// Run with the environment flag PROJECT_MANNEQUIN_TRAINING_TEST=1.
/// </summary>
public static class TrainingDummyTests
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

        log.Append("=== Training Dummy Tests ===\n");

        Check(
            Resolve(TrainingDummySetting.Stand, grounded: true, actionable: true, tick: 8) == InputButtons.None,
            "stand holds neutral");

        Check(
            Resolve(TrainingDummySetting.Crouch, grounded: true, actionable: true, tick: 3) == InputButtons.Crouch,
            "crouch holds down");

        Check(
            Resolve(TrainingDummySetting.Jump, grounded: true, actionable: true, tick: TrainingDummyController.JumpPeriodFrames)
                == InputButtons.Jump,
            "jump presses on the beat while grounded");

        Check(
            Resolve(TrainingDummySetting.Jump, grounded: true, actionable: true, tick: TrainingDummyController.JumpPeriodFrames + 1)
                == InputButtons.None,
            "jump is neutral off the beat");

        Check(
            Resolve(TrainingDummySetting.Jump, grounded: false, actionable: true, tick: TrainingDummyController.JumpPeriodFrames)
                == InputButtons.None,
            "jump does not re-press while airborne");

        Check(
            Resolve(TrainingDummySetting.GuardAll, grounded: true, actionable: true, tick: 7) == InputButtons.Block,
            "guard all holds block");

        Check(
            Resolve(TrainingDummySetting.MashJab, grounded: true, actionable: true, tick: TrainingDummyController.MashPeriodFrames)
                == InputButtons.LightPunch,
            "mash jab taps on the beat");

        Check(
            Resolve(TrainingDummySetting.MashJab, grounded: true, actionable: false, tick: TrainingDummyController.MashPeriodFrames)
                == InputButtons.None,
            "mash jab waits while not actionable");

        Check(
            Resolve(TrainingDummySetting.ParryAttempt, grounded: true, actionable: true, tick: TrainingDummyController.ParryPeriodFrames)
                == (InputButtons.MediumPunch | InputButtons.MediumKick),
            "parry attempt presses both mediums on the beat");

        // GuardAfterFirstHit and Reversal require the stateful brain to observe a
        // hit reaction first, so exercise them through TrainingDummyBrain.

        var guardBrain = new TrainingDummyBrain(TrainingDummySetting.GuardAfterFirstHit);
        var beforeHit = guardBrain.Advance(1, CombatActorState.Idle, grounded: true);
        var duringHit = guardBrain.Advance(2, CombatActorState.Hitstun, grounded: true);
        var afterHit = guardBrain.Advance(20, CombatActorState.Idle, grounded: true);

        Check(beforeHit == InputButtons.None, "guard-after-first-hit stands before any hit", $"got {beforeHit}");
        Check(afterHit == InputButtons.Block, "guard-after-first-hit blocks once hit", $"got {afterHit}");
        Check(duringHit == InputButtons.Block, "guard-after-first-hit keeps guard held through hitstun", $"got {duringHit}");

        var reversalBrain = new TrainingDummyBrain(TrainingDummySetting.Reversal);
        var idleBeforeKnockdown = reversalBrain.Advance(1, CombatActorState.Idle, grounded: true);
        reversalBrain.Advance(2, CombatActorState.Knockdown, grounded: true);
        reversalBrain.Advance(3, CombatActorState.Knockdown, grounded: true);
        var wakeupFrame = reversalBrain.Advance(30, CombatActorState.Idle, grounded: true);
        var frameAfterWakeup = reversalBrain.Advance(31, CombatActorState.Idle, grounded: true);

        Check(idleBeforeKnockdown == InputButtons.None, "reversal is idle before knockdown", $"got {idleBeforeKnockdown}");
        Check(wakeupFrame == InputButtons.HeavyPunch, "reversal fires the instant it recovers", $"got {wakeupFrame}");
        Check(frameAfterWakeup == InputButtons.None, "reversal fires only on the recovery frame", $"got {frameAfterWakeup}");

        var parsedOk = TrainingDummyController.TryParse("guard_after_first_hit", out var parsedSetting)
            && parsedSetting == TrainingDummySetting.GuardAfterFirstHit
            && TrainingDummyController.TryParse("MASH_JAB", out var mashSetting)
            && mashSetting == TrainingDummySetting.MashJab
            && !TrainingDummyController.TryParse("nonsense", out _)
            && !TrainingDummyController.TryParse("", out _);
        Check(parsedOk, "setting parsing accepts known keys and rejects unknown ones");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }

    private static InputButtons Resolve(TrainingDummySetting setting, bool grounded, bool actionable, int tick)
    {
        var situation = new TrainingDummySituation(
            tick,
            actionable ? CombatActorState.Idle : CombatActorState.Attacking,
            grounded,
            HasBeenHit: false,
            ActionableNow: actionable,
            RecoveredFromHit: false);
        return TrainingDummyController.Resolve(setting, situation);
    }
}
