using System.Collections.Generic;
using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

/// <summary>
/// How a controller's face buttons are labelled by its manufacturer.
/// </summary>
/// <remarks>
/// Godot reports face buttons by SDL position, not by printed label:
/// <see cref="JoyButton.A"/> is always the bottom button. The printed name for
/// that position differs per family, which is the whole reason glyphs must
/// switch rather than being hardcoded.
/// </remarks>
public enum InputGlyphFamily
{
    Keyboard,
    Xbox,
    PlayStation,
    Nintendo,
    GenericGamepad,
}

/// <summary>
/// The single source of truth for which physical pad button drives which
/// gameplay action.
/// </summary>
/// <remarks>
/// <c>LocalInputManager.PollJoypad</c> reads this table to build its input
/// mask, and the glyph formatter reads the same table to label move lists. They
/// cannot drift apart, so a displayed glyph can never lie about the binding.
/// Block and Grab are deliberately absent: they are analog triggers, not
/// buttons, and are handled separately at both call sites.
/// </remarks>
public static class GamepadBindings
{
    public static readonly IReadOnlyList<(InputButtons Button, JoyButton Joy)> ButtonMap =
        new[]
        {
            (InputButtons.LightPunch, JoyButton.X),
            (InputButtons.MediumPunch, JoyButton.Y),
            (InputButtons.HeavyPunch, JoyButton.RightShoulder),
            (InputButtons.LightKick, JoyButton.A),
            (InputButtons.MediumKick, JoyButton.B),
            (InputButtons.HeavyKick, JoyButton.LeftShoulder),
            (InputButtons.Dash, JoyButton.LeftStick),
            (InputButtons.FormSwap, JoyButton.RightStick),
            // Jump must sit on a button every controller physically has.
            // It used to be Misc1, which is the Xbox Series share / PS5
            // microphone / Switch capture button and simply does not exist on
            // Xbox One pads, DualShock 4, or most third-party controllers, so
            // jumping was impossible on them. Up cannot be used instead: this
            // is a belt-scroller and Up is lane movement toward the backdrop.
            // Assist takes Misc1 because it is a single optional action, so
            // degrading it on older pads is far less severe than losing jump.
            (InputButtons.Jump, JoyButton.Back),
            (InputButtons.Start, JoyButton.Start),
            (InputButtons.Assist, JoyButton.Misc1),
        };

    /// <summary>Left trigger. Analog, so it is not part of the button map.</summary>
    public const JoyAxis BlockAxis = JoyAxis.TriggerLeft;

    /// <summary>Right trigger. Analog, so it is not part of the button map.</summary>
    public const JoyAxis GrabAxis = JoyAxis.TriggerRight;

    public const float TriggerThreshold = 0.35f;

    public static bool TryGetJoyButton(InputButtons button, out JoyButton joyButton)
    {
        foreach (var (candidate, joy) in ButtonMap)
        {
            if (candidate == button)
            {
                joyButton = joy;
                return true;
            }
        }

        joyButton = JoyButton.Invalid;
        return false;
    }

    /// <summary>
    /// Picks a glyph family from the device kind and the driver-reported pad
    /// name.
    /// </summary>
    /// <remarks>
    /// Pure and name-based so the deterministic suite can cover it without a
    /// controller attached. Unrecognised pads fall back to
    /// <see cref="InputGlyphFamily.GenericGamepad"/>, which uses SDL position
    /// names, rather than guessing a brand and printing the wrong glyph.
    /// </remarks>
    public static InputGlyphFamily DetectFamily(string deviceKind, string joyName)
    {
        if (!string.Equals(deviceKind, "gamepad", System.StringComparison.OrdinalIgnoreCase))
        {
            return InputGlyphFamily.Keyboard;
        }

        var name = (joyName ?? string.Empty).ToLowerInvariant();
        if (name.Length == 0)
        {
            return InputGlyphFamily.GenericGamepad;
        }

        if (name.Contains("xbox")
            || name.Contains("xinput")
            || name.Contains("x-input")
            || name.Contains("microsoft"))
        {
            return InputGlyphFamily.Xbox;
        }

        if (name.Contains("playstation")
            || name.Contains("dualshock")
            || name.Contains("dualsense")
            || name.Contains("sony")
            || name.Contains("ps3")
            || name.Contains("ps4")
            || name.Contains("ps5"))
        {
            return InputGlyphFamily.PlayStation;
        }

        if (name.Contains("nintendo")
            || name.Contains("switch")
            || name.Contains("joy-con")
            || name.Contains("joycon")
            || name.Contains("pro controller"))
        {
            return InputGlyphFamily.Nintendo;
        }

        return InputGlyphFamily.GenericGamepad;
    }

    /// <summary>
    /// The printed label for a pad button in the given family.
    /// </summary>
    public static string Label(InputGlyphFamily family, JoyButton joyButton) =>
        joyButton switch
        {
            JoyButton.A => family switch
            {
                InputGlyphFamily.PlayStation => "Cross",
                // Nintendo swaps the physical positions relative to SDL.
                InputGlyphFamily.Nintendo => "B",
                InputGlyphFamily.Xbox => "A",
                _ => "A (down)",
            },
            JoyButton.B => family switch
            {
                InputGlyphFamily.PlayStation => "Circle",
                InputGlyphFamily.Nintendo => "A",
                InputGlyphFamily.Xbox => "B",
                _ => "B (right)",
            },
            JoyButton.X => family switch
            {
                InputGlyphFamily.PlayStation => "Square",
                InputGlyphFamily.Nintendo => "Y",
                InputGlyphFamily.Xbox => "X",
                _ => "X (left)",
            },
            JoyButton.Y => family switch
            {
                InputGlyphFamily.PlayStation => "Triangle",
                InputGlyphFamily.Nintendo => "X",
                InputGlyphFamily.Xbox => "Y",
                _ => "Y (up)",
            },
            JoyButton.LeftShoulder => family == InputGlyphFamily.PlayStation ? "L1" : "LB",
            JoyButton.RightShoulder => family == InputGlyphFamily.PlayStation ? "R1" : "RB",
            JoyButton.LeftStick => family == InputGlyphFamily.PlayStation ? "L3" : "LS",
            JoyButton.RightStick => family == InputGlyphFamily.PlayStation ? "R3" : "RS",
            JoyButton.Start => family switch
            {
                InputGlyphFamily.PlayStation => "Options",
                InputGlyphFamily.Nintendo => "+",
                _ => "Menu",
            },
            JoyButton.Back => family switch
            {
                InputGlyphFamily.PlayStation => "Share",
                InputGlyphFamily.Nintendo => "-",
                _ => "View",
            },
            // SDL MISC1 is the Xbox Series share button, the PS5 microphone
            // button, and the Switch capture button. It is NOT the PlayStation
            // touchpad, which Godot exposes separately as JoyButton.Touchpad.
            JoyButton.Misc1 => family switch
            {
                InputGlyphFamily.PlayStation => "Mic",
                InputGlyphFamily.Nintendo => "Capture",
                InputGlyphFamily.Xbox => "Share",
                _ => "Misc",
            },
            JoyButton.Touchpad => "Touchpad",
            _ => joyButton.ToString(),
        };

    public static string TriggerLabel(InputGlyphFamily family, JoyAxis axis)
    {
        if (axis == BlockAxis)
        {
            return family == InputGlyphFamily.PlayStation ? "L2" : "LT";
        }

        return family == InputGlyphFamily.PlayStation ? "R2" : "RT";
    }
}
