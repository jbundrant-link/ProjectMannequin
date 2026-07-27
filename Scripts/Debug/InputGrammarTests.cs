using System.Text;
using Godot;
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

        var availableDevices = new[]
        {
            new LocalInputDeviceOption(
                GameConstants.KeyboardDeviceId,
                "Keyboard",
                "keyboard",
                ""),
            new LocalInputDeviceOption(4, "Pad A", "gamepad", "guid-a"),
            new LocalInputDeviceOption(9, "Pad B", "gamepad", "guid-b"),
        };
        Check(
            "keyboard preference resolves keyboard",
            LocalInputAssignmentPolicy.ResolvePreferredP1Device(
                "keyboard",
                "",
                availableDevices) == GameConstants.KeyboardDeviceId);
        Check(
            "gamepad GUID re-resolves unstable runtime ID",
            LocalInputAssignmentPolicy.ResolvePreferredP1Device(
                "gamepad",
                "GUID-B",
                availableDevices) == 9);
        Check(
            "missing preferred pad falls back to connected pad",
            LocalInputAssignmentPolicy.ResolvePreferredP1Device(
                "gamepad",
                "missing",
                availableDevices) == 4);
        var assignments = LocalInputAssignmentPolicy.BuildAssignments(
            GameConstants.MaxPlayers,
            new[] { 4, 9 },
            9);
        Check(
            "P1 preference claims pad and P2 gets next unclaimed pad",
            assignments[0] == 9 && assignments[1] == 4);
        Check(
            "duplicate live-device assignment is rejected",
            !LocalInputAssignmentPolicy.CanAssign(assignments, 2, 9, new[] { 4, 9 }));
        Check(
            "disconnect requests assignment refresh",
            LocalInputAssignmentPolicy.ShouldRefresh(
                replayActive: false,
                assignments,
                new[] { 4 },
                GameConstants.KeyboardDeviceId));
        Check(
            "replay prevents live assignment refresh",
            !LocalInputAssignmentPolicy.ShouldRefresh(
                replayActive: true,
                assignments,
                new[] { 4 },
                GameConstants.KeyboardDeviceId));

        UiInputRouter.ResetNavigationState();
        var rightPress = new InputEventJoypadMotion
        {
            Device = 9,
            Axis = JoyAxis.LeftX,
            AxisValue = 0.8f,
        };
        Check(
            "assigned P1 left stick navigates right",
            UiInputRouter.IsPressedForAssignedDevice(
                rightPress,
                LogicalUiAction.Right,
                assignedDevice: 9));
        Check(
            "unassigned pad cannot navigate P1 UI",
            !UiInputRouter.IsPressedForAssignedDevice(
                new InputEventJoypadMotion
                {
                    Device = 4,
                    Axis = JoyAxis.LeftY,
                    AxisValue = -0.8f,
                },
                LogicalUiAction.Up,
                assignedDevice: 9));
        Check(
            "held stick direction is edge-triggered",
            !UiInputRouter.IsPressedForAssignedDevice(
                new InputEventJoypadMotion
                {
                    Device = 9,
                    Axis = JoyAxis.LeftX,
                    AxisValue = 0.9f,
                },
                LogicalUiAction.Right,
                assignedDevice: 9));
        UiInputRouter.IsPressedForAssignedDevice(
            new InputEventJoypadMotion
            {
                Device = 9,
                Axis = JoyAxis.LeftX,
                AxisValue = 0.0f,
            },
            LogicalUiAction.Right,
            assignedDevice: 9);
        Check(
            "stick navigation rearms after dead-zone release",
            UiInputRouter.IsPressedForAssignedDevice(
                new InputEventJoypadMotion
                {
                    Device = 9,
                    Axis = JoyAxis.LeftX,
                    AxisValue = 0.8f,
                },
                LogicalUiAction.Right,
                assignedDevice: 9));
        Check(
            "keyboard remains emergency modal fallback",
            UiInputRouter.IsEventFromAssignedDevice(
                new InputEventKey { Pressed = true, PhysicalKeycode = Key.Escape },
                assignedDevice: 9));

        // --- Input glyph switching -------------------------------------

        Check(
            "keyboard preference never resolves to a gamepad family",
            GamepadBindings.DetectFamily("keyboard", "Xbox Series Controller")
                == InputGlyphFamily.Keyboard);

        Check(
            "pad families are detected from the driver name",
            GamepadBindings.DetectFamily("gamepad", "Xbox Series X Controller")
                == InputGlyphFamily.Xbox
            && GamepadBindings.DetectFamily("gamepad", "Sony DualSense Wireless Controller")
                == InputGlyphFamily.PlayStation
            && GamepadBindings.DetectFamily("gamepad", "Nintendo Switch Pro Controller")
                == InputGlyphFamily.Nintendo);

        // An unknown pad must not be guessed into a brand, because printing the
        // wrong face-button name is worse than printing the SDL position.
        Check(
            "unknown pads fall back to generic position labels",
            GamepadBindings.DetectFamily("gamepad", "Some Arcade Stick")
                == InputGlyphFamily.GenericGamepad
            && GamepadBindings.DetectFamily("gamepad", "")
                == InputGlyphFamily.GenericGamepad
            && GamepadBindings.Label(InputGlyphFamily.GenericGamepad, JoyButton.A)
                .Contains("down"));

        // Godot reports face buttons by SDL position, so the same position has
        // to print a different name per family. This is the whole feature.
        Check(
            "the same physical button prints its family's printed label",
            GamepadBindings.Label(InputGlyphFamily.Xbox, JoyButton.A) == "A"
            && GamepadBindings.Label(InputGlyphFamily.PlayStation, JoyButton.A) == "Cross"
            && GamepadBindings.Label(InputGlyphFamily.Nintendo, JoyButton.A) == "B"
            && GamepadBindings.Label(InputGlyphFamily.PlayStation, JoyButton.Y) == "Triangle"
            && GamepadBindings.Label(InputGlyphFamily.Nintendo, JoyButton.Y) == "X");

        Check(
            "shoulder, stick, and trigger labels follow the family",
            GamepadBindings.Label(InputGlyphFamily.Xbox, JoyButton.LeftShoulder) == "LB"
            && GamepadBindings.Label(InputGlyphFamily.PlayStation, JoyButton.LeftShoulder) == "L1"
            && GamepadBindings.TriggerLabel(InputGlyphFamily.Xbox, GamepadBindings.BlockAxis) == "LT"
            && GamepadBindings.TriggerLabel(InputGlyphFamily.PlayStation, GamepadBindings.GrabAxis) == "R2");

        // The glyph table and the poll loop must stay one table: a label that
        // claims a binding the pad does not have is a lie to the player.
        var everyAttackIsBound = true;
        foreach (var attack in new[]
                 {
                     InputButtons.LightPunch,
                     InputButtons.MediumPunch,
                     InputButtons.HeavyPunch,
                     InputButtons.LightKick,
                     InputButtons.MediumKick,
                     InputButtons.HeavyKick,
                 })
        {
            if (!GamepadBindings.TryGetJoyButton(attack, out _))
            {
                everyAttackIsBound = false;
            }
        }

        Check(
            "every attack button resolves through the shared binding table",
            everyAttackIsBound
            && !GamepadBindings.TryGetJoyButton(InputButtons.Block, out _)
            && !GamepadBindings.TryGetJoyButton(InputButtons.Grab, out _));

        // SDL MISC1 is the Xbox share / PS5 microphone / Switch capture button.
        // Calling it the PlayStation touchpad was wrong and shipped into a
        // capture before it was caught, so it is pinned here.
        Check(
            "Misc1 is never mislabelled as the PlayStation touchpad",
            GamepadBindings.Label(InputGlyphFamily.PlayStation, JoyButton.Misc1) == "Mic"
            && GamepadBindings.Label(InputGlyphFamily.Xbox, JoyButton.Misc1) == "Share"
            && GamepadBindings.Label(InputGlyphFamily.Nintendo, JoyButton.Misc1) == "Capture"
            && GamepadBindings.Label(InputGlyphFamily.PlayStation, JoyButton.Touchpad) == "Touchpad");

        // Jump must live on a button that physically exists on every pad.
        // Misc1 is the Xbox Series share / PS5 mic / Switch capture button and
        // is absent on Xbox One, DualShock 4, and most third-party controllers,
        // so binding jump there made jumping impossible on them.
        var universalButtons = new System.Collections.Generic.HashSet<JoyButton>
        {
            JoyButton.A, JoyButton.B, JoyButton.X, JoyButton.Y,
            JoyButton.LeftShoulder, JoyButton.RightShoulder,
            JoyButton.LeftStick, JoyButton.RightStick,
            JoyButton.Start, JoyButton.Back,
            JoyButton.DpadUp, JoyButton.DpadDown,
            JoyButton.DpadLeft, JoyButton.DpadRight,
        };
        Check(
            "jump is bound to a button present on every controller",
            GamepadBindings.TryGetJoyButton(InputButtons.Jump, out var jumpButton)
            && universalButtons.Contains(jumpButton)
            && jumpButton != JoyButton.Misc1);

        var everyCoreActionIsUniversal = true;
        foreach (var core in new[]
                 {
                     InputButtons.LightPunch, InputButtons.MediumPunch, InputButtons.HeavyPunch,
                     InputButtons.LightKick, InputButtons.MediumKick, InputButtons.HeavyKick,
                     InputButtons.Jump, InputButtons.Dash, InputButtons.FormSwap,
                     InputButtons.Start,
                 })
        {
            if (!GamepadBindings.TryGetJoyButton(core, out var joy)
                || !universalButtons.Contains(joy))
            {
                everyCoreActionIsUniversal = false;
            }
        }

        Check(
            "every core action sits on a universally present button",
            everyCoreActionIsUniversal);

        var distinctJoyButtons = new System.Collections.Generic.HashSet<JoyButton>();
        var noDoubleBinding = true;
        foreach (var (_, joy) in GamepadBindings.ButtonMap)
        {
            if (!distinctJoyButtons.Add(joy))
            {
                noDoubleBinding = false;
            }
        }

        Check(
            "no two gameplay actions share one pad button",
            noDoubleBinding && distinctJoyButtons.Count == GamepadBindings.ButtonMap.Count);

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
