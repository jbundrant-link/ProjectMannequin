using Godot;
using ProjectMannequin.Core;
using System.Collections.Generic;

namespace ProjectMannequin.LocalInput;

public enum LogicalUiAction
{
    Up,
    Down,
    Left,
    Right,
    Accept,
    Cancel,
    Pause,
    FormSwap,
    Reward1,
    Reward2,
    Reward3,
    Restart,
    MainMenu,
}

/// <summary>
/// Maps raw keyboard/gamepad events to the logical UI verbs used by modal
/// overlays. Combat still consumes deterministic input masks; this helper only
/// prevents player-facing menus from becoming keyboard-only.
/// </summary>
public static class UiInputRouter
{
    private const float StickPressThreshold = 0.65f;
    private const float StickReleaseThreshold = 0.35f;
    private static readonly Dictionary<(int Device, JoyAxis Axis), int> StickDirections = new();
    private static InputEventJoypadMotion? _cachedMotionEvent;
    private static LogicalUiAction? _cachedMotionAction;

    public static bool IsPressed(InputEvent inputEvent, LogicalUiAction action)
    {
        var assignedDevice = LocalInputManager.Active?.GetAssignedDevice(1)
            ?? InputDevicePreferences.ResolveP1Device();
        return IsPressedForAssignedDevice(inputEvent, action, assignedDevice);
    }

    public static bool IsPressedForAssignedDevice(
        InputEvent inputEvent,
        LogicalUiAction action,
        int assignedDevice)
    {
        return inputEvent switch
        {
            InputEventKey key => IsKeyboardPressed(key, action),
            InputEventJoypadButton button => IsJoypadPressed(
                button,
                action,
                assignedDevice),
            InputEventJoypadMotion motion => IsEventFromAssignedDevice(
                    motion,
                    assignedDevice)
                && ResolveJoypadMotion(motion) == action,
            _ => false,
        };
    }

    public static int RewardChoice(InputEvent inputEvent)
    {
        if (IsPressed(inputEvent, LogicalUiAction.Reward1)) return 0;
        if (IsPressed(inputEvent, LogicalUiAction.Reward2)) return 1;
        if (IsPressed(inputEvent, LogicalUiAction.Reward3)) return 2;
        return -1;
    }

    public static bool IsEventFromP1(InputEvent inputEvent)
    {
        var assigned = LocalInputManager.Active?.GetAssignedDevice(1)
            ?? InputDevicePreferences.ResolveP1Device();
        return IsEventFromAssignedDevice(inputEvent, assigned);
    }

    public static bool IsEventFromAssignedDevice(InputEvent inputEvent, int assignedDevice)
    {
        if (inputEvent is InputEventKey)
        {
            // Keyboard remains an emergency fallback even when a pad is selected,
            // so a disconnected controller can never strand the user in a modal.
            return true;
        }

        if (inputEvent is not (InputEventJoypadButton or InputEventJoypadMotion))
        {
            return false;
        }

        return assignedDevice != GameConstants.KeyboardDeviceId
            && inputEvent.Device == assignedDevice;
    }

    private static bool IsKeyboardPressed(InputEventKey key, LogicalUiAction action)
    {
        if (!key.Pressed || key.Echo)
        {
            return false;
        }

        return action switch
        {
            LogicalUiAction.Up => key.PhysicalKeycode is Key.Up or Key.W,
            LogicalUiAction.Down => key.PhysicalKeycode is Key.Down or Key.S,
            LogicalUiAction.Left => key.PhysicalKeycode is Key.Left or Key.A,
            LogicalUiAction.Right => key.PhysicalKeycode is Key.Right or Key.D,
            LogicalUiAction.Accept => key.PhysicalKeycode is Key.Enter or Key.Space or Key.J,
            LogicalUiAction.Cancel => key.PhysicalKeycode is Key.Escape or Key.Backspace or Key.Q,
            LogicalUiAction.Pause => key.PhysicalKeycode is Key.Escape or Key.Enter,
            LogicalUiAction.FormSwap => key.PhysicalKeycode == Key.Q,
            LogicalUiAction.Reward1 => key.PhysicalKeycode == Key.Key1,
            LogicalUiAction.Reward2 => key.PhysicalKeycode == Key.Key2,
            LogicalUiAction.Reward3 => key.PhysicalKeycode == Key.Key3,
            LogicalUiAction.Restart => key.PhysicalKeycode is Key.R or Key.Enter,
            LogicalUiAction.MainMenu => key.PhysicalKeycode == Key.M,
            _ => false,
        };
    }

    private static bool IsJoypadPressed(
        InputEventJoypadButton button,
        LogicalUiAction action,
        int assignedDevice)
    {
        if (!button.Pressed || !IsEventFromAssignedDevice(button, assignedDevice))
        {
            return false;
        }

        return action switch
        {
            LogicalUiAction.Up => button.ButtonIndex == JoyButton.DpadUp,
            LogicalUiAction.Down => button.ButtonIndex == JoyButton.DpadDown,
            LogicalUiAction.Left => button.ButtonIndex == JoyButton.DpadLeft,
            LogicalUiAction.Right => button.ButtonIndex == JoyButton.DpadRight,
            LogicalUiAction.Accept => button.ButtonIndex is JoyButton.A or JoyButton.X,
            LogicalUiAction.Cancel => button.ButtonIndex is JoyButton.B or JoyButton.RightStick,
            LogicalUiAction.Pause => button.ButtonIndex == JoyButton.Start,
            LogicalUiAction.FormSwap => button.ButtonIndex == JoyButton.RightStick,
            LogicalUiAction.Reward1 => button.ButtonIndex == JoyButton.X,
            LogicalUiAction.Reward2 => button.ButtonIndex == JoyButton.Y,
            LogicalUiAction.Reward3 => button.ButtonIndex == JoyButton.A,
            LogicalUiAction.Restart => button.ButtonIndex == JoyButton.A,
            LogicalUiAction.MainMenu => button.ButtonIndex == JoyButton.B,
            _ => false,
        };
    }

    public static void ResetNavigationState()
    {
        StickDirections.Clear();
        _cachedMotionEvent = null;
        _cachedMotionAction = null;
    }

    private static LogicalUiAction? ResolveJoypadMotion(InputEventJoypadMotion motion)
    {
        if (ReferenceEquals(_cachedMotionEvent, motion))
        {
            return _cachedMotionAction;
        }

        _cachedMotionEvent = motion;
        _cachedMotionAction = null;
        if (motion.Axis is not (JoyAxis.LeftX or JoyAxis.LeftY))
        {
            return null;
        }

        var key = (motion.Device, motion.Axis);
        if (System.Math.Abs(motion.AxisValue) <= StickReleaseThreshold)
        {
            StickDirections[key] = 0;
            return null;
        }

        var direction = motion.AxisValue >= StickPressThreshold
            ? 1
            : motion.AxisValue <= -StickPressThreshold
                ? -1
                : 0;
        if (direction == 0
            || StickDirections.GetValueOrDefault(key) == direction)
        {
            return null;
        }

        StickDirections[key] = direction;
        _cachedMotionAction = motion.Axis switch
        {
            JoyAxis.LeftX when direction < 0 => LogicalUiAction.Left,
            JoyAxis.LeftX => LogicalUiAction.Right,
            JoyAxis.LeftY when direction < 0 => LogicalUiAction.Up,
            JoyAxis.LeftY => LogicalUiAction.Down,
            _ => null,
        };
        return _cachedMotionAction;
    }
}
