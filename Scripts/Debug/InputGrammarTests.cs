using System.Text;
using ProjectMannequin.Core;
using ProjectMannequin.LocalInput;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Deterministic command-resolution tests for the input grammar. Feeds synthetic
/// input buffers into <see cref="CommandInterpreter"/> and asserts which command
/// wins for overlapping notations (236LP vs LP, 236HP vs 236LP, 22HP, crouch
/// normals, supers by priority) and that consumed presses are not re-matched.
/// Runs headless via PROJECT_MANNEQUIN_INPUT_GRAMMAR_TEST=1.
/// </summary>
public static class InputGrammarTests
{
    public static string Run()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Input Grammar Tests ===");
        var interpreter = new CommandInterpreter();
        var passed = 0;
        var failed = 0;

        void Check(string name, bool condition)
        {
            if (condition)
            {
                passed++;
                sb.AppendLine($"  PASS {name}");
            }
            else
            {
                failed++;
                sb.AppendLine($"  FAIL {name}");
            }
        }

        var lp = interpreter.Parse("light", "LP");
        var hp = interpreter.Parse("heavy_punch", "HP");
        var hk = interpreter.Parse("heavy_kick", "HK");
        var qcfLp = interpreter.Parse("qcf_light", "236LP");
        var qcfHp = interpreter.Parse("qcf_heavy", "236HP");
        var superHk = interpreter.Parse("super", "236HK", priority: 10);
        var doubleDownHp = interpreter.Parse("dd_heavy", "22HP");
        var crouchLp = interpreter.Parse("crouch_light", "2LP");

        // T1: an S, SD, D motion + LP resolves to 236LP, not the normal.
        var b1 = BufferFor236(InputButtons.LightPunch);
        var m1 = interpreter.FindBestCommand(new[] { lp, qcfLp }, b1, true);
        Check("S, SD, D + LP resolves 236LP", m1?.Command.Name == "qcf_light");

        // T2: a neutral LP press resolves to the normal (no motion present).
        var b2 = BufferNeutralPress(InputButtons.LightPunch);
        var m2 = interpreter.FindBestCommand(new[] { lp, qcfLp }, b2, true);
        Check("neutral LP resolves to LP", m2?.Command.Name == "light");

        // T3: the pressed button distinguishes overlapping motions.
        var b3 = BufferFor236(InputButtons.HeavyPunch);
        var m3 = interpreter.FindBestCommand(new[] { qcfLp, qcfHp }, b3, true);
        Check("236HP resolves to qcf_heavy", m3?.Command.Name == "qcf_heavy");

        // T4a: a higher-priority super beats a normal on the same button.
        var b4 = BufferFor236(InputButtons.HeavyKick);
        var m4 = interpreter.FindBestCommand(new[] { hk, superHk }, b4, true);
        Check("236HK beats HK by priority", m4?.Command.Name == "super");

        // T4b: without the motion, the normal wins (super cannot match).
        var b4b = BufferNeutralPress(InputButtons.HeavyKick);
        var m4b = interpreter.FindBestCommand(new[] { hk, superHk }, b4b, true);
        Check("neutral HK resolves to HK", m4b?.Command.Name == "heavy_kick");

        // T5: 22 motion + HP resolves to the double-down command.
        var b5 = BufferForDoubleDown(InputButtons.HeavyPunch);
        var m5 = interpreter.FindBestCommand(new[] { hp, doubleDownHp }, b5, true);
        Check("22HP beats HP", m5?.Command.Name == "dd_heavy");

        // T6: dedicated Crouch + LP resolves to the crouch normal over standing.
        var b6 = BufferHoldCrouchPress(InputButtons.LightPunch);
        var m6 = interpreter.FindBestCommand(new[] { lp, crouchLp }, b6, true);
        Check("C + LP resolves 2LP", m6?.Command.Name == "crouch_light");

        // T6b: S is motion down, not the crouch-normal modifier.
        var b6b = BufferHoldMotionDownPress(InputButtons.LightPunch);
        var m6b = interpreter.FindBestCommand(new[] { lp, crouchLp }, b6b, true);
        Check("S + LP remains standing LP", m6b?.Command.Name == "light");

        // T6c: C cannot silently replace S inside a multi-step motion.
        var b6c = BufferFor236UsingCrouch(InputButtons.LightPunch);
        var m6c = interpreter.FindBestCommand(new[] { lp, qcfLp }, b6c, true);
        Check("C sequence does not resolve 236LP", m6c?.Command.Name == "light");

        Check(
            "motion down maps from S/Down",
            PlayerInputBuffer.ToNumericDirection(InputButtons.Down, true) == 2);
        Check(
            "dedicated Crouch stays outside motion plane",
            PlayerInputBuffer.ToNumericDirection(InputButtons.Crouch, true) == 5);

        // T7: a consumed press is not matched again for the same tick.
        var b7 = BufferFor236(InputButtons.LightPunch);
        var first = interpreter.FindBestCommand(new[] { qcfLp }, b7, true);
        var firstMatched = first is not null;
        var consumedTick = firstMatched ? b7.GetFrame(first!.Value.InputAgeFrames).Tick : -1;
        var second = interpreter.FindBestCommand(new[] { qcfLp }, b7, true, consumedTick);
        Check("consumed press not re-matched", firstMatched && second is null);

        sb.AppendLine($"=== {passed} passed, {failed} failed ===");
        return sb.ToString();
    }

    // Facing right: 2=Down/S, 3=Down+Right/SD, 6=Right/D.
    private static PlayerInputBuffer BufferFor236(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.Down);
        buffer.PushFrame(2, InputButtons.Down | InputButtons.Right);
        buffer.PushFrame(3, InputButtons.Right);
        buffer.PushFrame(4, InputButtons.Right | button);
        return buffer;
    }

    private static PlayerInputBuffer BufferForDoubleDown(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.Down);
        buffer.PushFrame(2, InputButtons.None);
        buffer.PushFrame(3, InputButtons.Down);
        buffer.PushFrame(4, InputButtons.Down | button);
        return buffer;
    }

    private static PlayerInputBuffer BufferHoldCrouchPress(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.Crouch);
        buffer.PushFrame(2, InputButtons.Crouch | button);
        return buffer;
    }

    private static PlayerInputBuffer BufferHoldMotionDownPress(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.Down);
        buffer.PushFrame(2, InputButtons.Down | button);
        return buffer;
    }

    private static PlayerInputBuffer BufferFor236UsingCrouch(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.Crouch);
        buffer.PushFrame(2, InputButtons.Crouch | InputButtons.Right);
        buffer.PushFrame(3, InputButtons.Right);
        buffer.PushFrame(4, InputButtons.Right | button);
        return buffer;
    }

    private static PlayerInputBuffer BufferNeutralPress(InputButtons button)
    {
        var buffer = new PlayerInputBuffer();
        buffer.PushFrame(0, InputButtons.None);
        buffer.PushFrame(1, InputButtons.None);
        buffer.PushFrame(2, button);
        return buffer;
    }
}
