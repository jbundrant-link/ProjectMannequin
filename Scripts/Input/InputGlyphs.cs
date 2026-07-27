using System.Collections.Generic;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Progression;

namespace ProjectMannequin.LocalInput;

/// <summary>
/// Resolves the on-screen label for a gameplay action against whatever device
/// Player 1 is actually using.
/// </summary>
/// <remarks>
/// Every move-list entry and legend routes through
/// <see cref="LabelForAction"/>, so switching the active device relabels the
/// whole UI without any call site knowing which family is active. Gamepad
/// labels come from <see cref="GamepadBindings"/>, the same table the input
/// poll reads, so a glyph cannot claim a binding the pad does not have.
/// </remarks>
public static class InputGlyphs
{
    /// <summary>
    /// Bumped whenever the active device changes. UI that caches rendered
    /// labels compares this to know it must rebuild.
    /// </summary>
    public static int Version { get; private set; }

    private static InputGlyphFamily? _cachedFamily;

    private static readonly Dictionary<string, InputButtons> ActionButtons = new()
    {
        ["p1_lp"] = InputButtons.LightPunch,
        ["p1_mp"] = InputButtons.MediumPunch,
        ["p1_hp"] = InputButtons.HeavyPunch,
        ["p1_lk"] = InputButtons.LightKick,
        ["p1_mk"] = InputButtons.MediumKick,
        ["p1_hk"] = InputButtons.HeavyKick,
        ["p1_jump"] = InputButtons.Jump,
        ["p1_dash"] = InputButtons.Dash,
        ["p1_form_swap"] = InputButtons.FormSwap,
        ["p1_assist"] = InputButtons.Assist,
        ["p1_start"] = InputButtons.Start,
    };

    private static readonly Dictionary<string, string> DirectionActions = new()
    {
        ["p1_up"] = "\u2191",
        ["p1_down"] = "\u2193",
        ["p1_left"] = "\u2190",
        ["p1_right"] = "\u2192",
        ["p1_crouch"] = "\u2193",
    };

    public static InputGlyphFamily ActiveFamily
    {
        get
        {
            if (_cachedFamily is { } cached)
            {
                return cached;
            }

            var resolved = ResolveActiveFamily();
            _cachedFamily = resolved;
            return resolved;
        }
    }

    public static bool UsingGamepad => ActiveFamily != InputGlyphFamily.Keyboard;

    /// <summary>
    /// Drops the cached family and bumps <see cref="Version"/> if the active
    /// device actually changed.
    /// </summary>
    /// <returns>True when the family changed, so callers can rebuild UI.</returns>
    public static bool Invalidate()
    {
        var previous = _cachedFamily;
        _cachedFamily = null;
        var current = ActiveFamily;
        if (previous is not null && previous == current)
        {
            return false;
        }

        Version++;
        return true;
    }

    /// <summary>
    /// The label a player should read for an action: a keyboard key when on
    /// keyboard, otherwise the printed pad glyph for the active family.
    /// </summary>
    public static string LabelForAction(string action, string keyboardFallback)
    {
        var family = ActiveFamily;
        if (family == InputGlyphFamily.Keyboard)
        {
            return KeyboardKeyFor(action, keyboardFallback);
        }

        return GamepadLabelFor(action, family)
            // An action with no pad binding still has to render something
            // truthful, so fall back to the keyboard key rather than inventing
            // a glyph.
            ?? KeyboardKeyFor(action, keyboardFallback);
    }

    /// <summary>
    /// Label for a logical UI action, so on-screen prompts name the control the
    /// player actually has rather than always naming a key.
    /// </summary>
    /// <remarks>
    /// The pad bindings mirror <c>UiInputRouter.MatchesJoypadButton</c>. Prompts
    /// that hardcode a key read as simply wrong on a controller, which is the
    /// defect this exists to prevent.
    /// </remarks>
    public static string UiActionLabel(LogicalUiAction action)
    {
        var family = ActiveFamily;
        if (family == InputGlyphFamily.Keyboard)
        {
            return action switch
            {
                LogicalUiAction.FormSwap => "Q",
                LogicalUiAction.Reward1 => "1",
                LogicalUiAction.Reward2 => "2",
                LogicalUiAction.Reward3 => "3",
                LogicalUiAction.Accept => "Enter",
                LogicalUiAction.Cancel => "Esc",
                LogicalUiAction.Pause => "Esc",
                LogicalUiAction.Restart => "R",
                LogicalUiAction.MainMenu => "M",
                _ => action.ToString(),
            };
        }

        var joyButton = action switch
        {
            LogicalUiAction.FormSwap => JoyButton.RightStick,
            LogicalUiAction.Reward1 => JoyButton.X,
            LogicalUiAction.Reward2 => JoyButton.Y,
            LogicalUiAction.Reward3 => JoyButton.A,
            LogicalUiAction.Accept => JoyButton.A,
            LogicalUiAction.Cancel => JoyButton.B,
            LogicalUiAction.Pause => JoyButton.Start,
            LogicalUiAction.Restart => JoyButton.A,
            LogicalUiAction.MainMenu => JoyButton.B,
            _ => JoyButton.Invalid,
        };

        return joyButton == JoyButton.Invalid
            ? action.ToString()
            : GamepadBindings.Label(family, joyButton);
    }

    private static string? GamepadLabelFor(string action, InputGlyphFamily family)
    {
        if (DirectionActions.TryGetValue(action, out var arrow))
        {
            return $"Stick {arrow}";
        }

        if (action == "p1_block")
        {
            return GamepadBindings.TriggerLabel(family, GamepadBindings.BlockAxis);
        }

        if (action == "p1_grab")
        {
            return GamepadBindings.TriggerLabel(family, GamepadBindings.GrabAxis);
        }

        if (ActionButtons.TryGetValue(action, out var button)
            && GamepadBindings.TryGetJoyButton(button, out var joyButton))
        {
            return GamepadBindings.Label(family, joyButton);
        }

        return null;
    }

    private static string KeyboardKeyFor(string action, string fallback)
    {
        var name = new StringName(action);
        if (InputMap.HasAction(name))
        {
            foreach (var inputEvent in InputMap.ActionGetEvents(name))
            {
                if (inputEvent is not InputEventKey keyEvent)
                {
                    continue;
                }

                var key = keyEvent.PhysicalKeycode != Key.None
                    ? keyEvent.PhysicalKeycode
                    : keyEvent.Keycode;
                if (key != Key.None)
                {
                    return OS.GetKeycodeString(key);
                }
            }
        }

        return fallback;
    }

    private static InputGlyphFamily ResolveActiveFamily()
    {
        // Capture tooling has no physical pad attached, so allow a forced
        // family. Gated on persistence being disabled, which is only true in a
        // smoke or capture run, never in a player session.
        if (MvpProgressStore.IsPersistenceDisabled)
        {
            var forced = OS.GetEnvironment("PROJECT_MANNEQUIN_FORCE_GLYPH_FAMILY");
            if (!string.IsNullOrWhiteSpace(forced)
                && System.Enum.TryParse<InputGlyphFamily>(forced, ignoreCase: true, out var family))
            {
                return family;
            }
        }

        var device = InputDevicePreferences.ResolveP1Device();
        if (device == GameConstants.KeyboardDeviceId)
        {
            return InputGlyphFamily.Keyboard;
        }

        return GamepadBindings.DetectFamily("gamepad", Input.GetJoyName(device));
    }
}
