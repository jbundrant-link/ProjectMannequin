using System;
using System.Collections.Generic;

namespace ProjectMannequin.Settings;

public enum OptionRowKind
{
    Slider,
    Toggle,
    Choice,
    Action,
}

/// <summary>
/// One navigable row of the options surface.
/// </summary>
public sealed record OptionRow(
    string Id,
    string Label,
    OptionRowKind Kind,
    string ValueText,
    string Description);

/// <summary>
/// The options surface as data: an ordered row list plus adjust and activate
/// verbs that mutate <see cref="SettingsData"/>.
/// </summary>
/// <remarks>
/// Kept separate from the overlay so navigation, clamping, wrap-around, and
/// value formatting are covered by the deterministic suite instead of only by
/// clicking through a menu. The overlay renders these rows and forwards input.
/// </remarks>
public sealed class OptionsModel
{
    private const float VolumeStep = 0.05f;
    private const float ShakeStep = 0.10f;
    private const float HudScaleStep = 0.05f;
    private const float SafeAreaStep = 0.01f;
    private const float RenderScaleStep = 0.05f;

    private static readonly (int Width, int Height)[] Resolutions =
    {
        (1280, 720),
        (1600, 900),
        (1920, 1080),
        (2560, 1440),
        (3840, 2160),
    };

    private readonly List<string> _order = new()
    {
        "master_volume",
        "music_volume",
        "sfx_volume",
        "ui_volume",
        "display_mode",
        "resolution",
        "render_scale",
        "vsync",
        "hud_scale",
        "hud_safe_area",
        "shake_intensity",
        "reduced_flash",
        "high_contrast",
        "hold_to_block",
        "reset_defaults",
        "erase_save_data",
    };

    public OptionsModel(SettingsData settings)
    {
        Settings = (settings ?? new SettingsData()).Normalize();
    }

    public SettingsData Settings { get; private set; }

    public int SelectedIndex { get; private set; }

    public int RowCount => _order.Count;

    public string SelectedId => _order[SelectedIndex];

    /// <summary>True once a row has changed, so the overlay knows to persist.</summary>
    public bool IsDirty { get; private set; }

    public void ClearDirty() => IsDirty = false;

    /// <summary>
    /// True once the destructive erase row has been pressed a first time and is
    /// waiting for confirmation. Cleared by moving off the row.
    /// </summary>
    public bool EraseArmed { get; private set; }

    /// <summary>
    /// Set when the player confirmed the erase. The surface consumes it and
    /// performs the deletion, so the model itself never touches disk.
    /// </summary>
    public bool EraseRequested { get; private set; }

    public void ClearEraseRequest() => EraseRequested = false;

    /// <summary>Moves the cursor, wrapping so a pad flick never dead-ends.</summary>
    public void MoveSelection(int delta)
    {
        if (delta == 0)
        {
            return;
        }

        // Moving away disarms the erase confirmation, so a player who navigates
        // past the row and later presses Accept elsewhere cannot trip it.
        EraseArmed = false;
        SelectedIndex = ((SelectedIndex + delta) % RowCount + RowCount) % RowCount;
    }

    public IReadOnlyList<OptionRow> BuildRows()
    {
        var rows = new List<OptionRow>(_order.Count);
        foreach (var id in _order)
        {
            rows.Add(BuildRow(id));
        }

        return rows;
    }

    public OptionRow BuildRow(string id)
    {
        return id switch
        {
            "master_volume" => Slider("Master Volume", Percent(Settings.MasterVolume),
                "Overall output level."),
            "music_volume" => Slider("Music Volume", Percent(Settings.MusicVolume),
                "Background music only."),
            "sfx_volume" => Slider("Effects Volume", Percent(Settings.SfxVolume),
                "Hits, guards, and stage effects."),
            "ui_volume" => Slider("Interface Volume", Percent(Settings.UiVolume),
                "Menu and confirmation sounds."),
            "display_mode" => Choice("Display Mode", Settings.DisplayMode switch
            {
                WindowDisplayMode.Fullscreen => "Fullscreen",
                WindowDisplayMode.Borderless => "Borderless",
                _ => "Windowed",
            }, "Windowed, borderless, or fullscreen."),
            "resolution" => Choice(
                "Resolution",
                $"{Settings.ResolutionWidth} x {Settings.ResolutionHeight}",
                "Window size when not fullscreen."),
            "render_scale" => Slider("Render Scale", Percent(Settings.RenderScale),
                "Lower to gain performance, higher to sharpen."),
            "vsync" => Toggle("VSync", Settings.VSyncEnabled,
                "Removes tearing at the cost of latency."),
            "hud_scale" => Slider("HUD Scale", Percent(Settings.HudScale),
                "Size of lifebars, score, and timers."),
            "hud_safe_area" => Slider("HUD Safe Area", Percent(Settings.HudSafeAreaInset),
                "Insets the HUD away from screen edges."),
            "shake_intensity" => Slider("Screen Shake", Percent(Settings.ShakeIntensity),
                "Zero disables camera shake entirely."),
            "reduced_flash" => Toggle("Reduced Flash", Settings.ReducedFlash,
                "Softens impact flashes and strobing effects."),
            "high_contrast" => Toggle("High-Contrast Telegraphs",
                Settings.HighContrastTelegraphs,
                "Adds shape and pattern cues, not colour alone."),
            "hold_to_block" => Choice("Block Input",
                Settings.HoldToBlock ? "Hold" : "Toggle",
                "Hold the guard button or toggle guard on and off."),
            "reset_defaults" => new OptionRow("reset_defaults", "Reset To Defaults",
                OptionRowKind.Action, "", "Restores every setting on this screen."),
            "erase_save_data" => new OptionRow("erase_save_data",
                EraseArmed ? "Erase All Save Data - CONFIRM?" : "Erase All Save Data",
                OptionRowKind.Action,
                EraseArmed ? "Press again" : "",
                EraseArmed
                    ? "This deletes progress, the active run, settings, and controls. It cannot be undone. Move away to cancel."
                    : "Deletes progress, the active run, settings, and controls. Asks for confirmation first."),
            _ => new OptionRow(id, id, OptionRowKind.Action, "", ""),
        };

        OptionRow Slider(string label, string value, string description) =>
            new(id, label, OptionRowKind.Slider, value, description);

        OptionRow Toggle(string label, bool value, string description) =>
            new(id, label, OptionRowKind.Toggle, value ? "On" : "Off", description);

        OptionRow Choice(string label, string value, string description) =>
            new(id, label, OptionRowKind.Choice, value, description);
    }

    /// <summary>Adjusts the selected row. Negative is left, positive is right.</summary>
    public void Adjust(int direction)
    {
        if (direction == 0)
        {
            return;
        }

        var sign = Math.Sign(direction);
        switch (SelectedId)
        {
            case "master_volume":
                Settings.MasterVolume += sign * VolumeStep;
                break;
            case "music_volume":
                Settings.MusicVolume += sign * VolumeStep;
                break;
            case "sfx_volume":
                Settings.SfxVolume += sign * VolumeStep;
                break;
            case "ui_volume":
                Settings.UiVolume += sign * VolumeStep;
                break;
            case "display_mode":
                Settings.DisplayMode = CycleDisplayMode(Settings.DisplayMode, sign);
                break;
            case "resolution":
                CycleResolution(sign);
                break;
            case "render_scale":
                Settings.RenderScale += sign * RenderScaleStep;
                break;
            case "vsync":
                Settings.VSyncEnabled = !Settings.VSyncEnabled;
                break;
            case "hud_scale":
                Settings.HudScale += sign * HudScaleStep;
                break;
            case "hud_safe_area":
                Settings.HudSafeAreaInset += sign * SafeAreaStep;
                break;
            case "shake_intensity":
                Settings.ShakeIntensity += sign * ShakeStep;
                break;
            case "reduced_flash":
                Settings.ReducedFlash = !Settings.ReducedFlash;
                break;
            case "high_contrast":
                Settings.HighContrastTelegraphs = !Settings.HighContrastTelegraphs;
                break;
            case "hold_to_block":
                Settings.HoldToBlock = !Settings.HoldToBlock;
                break;
            default:
                return;
        }

        Settings.Normalize();
        IsDirty = true;
    }

    /// <summary>
    /// Activates the selected row. Returns true when the caller should close.
    /// </summary>
    public bool Activate()
    {
        switch (SelectedId)
        {
            case "reset_defaults":
                Settings = new SettingsData().Normalize();
                IsDirty = true;
                return false;
            case "erase_save_data":
                // Two presses, and only while the row stays selected. A single
                // stray Accept must never be able to wipe a player's save.
                if (!EraseArmed)
                {
                    EraseArmed = true;
                    return false;
                }

                EraseArmed = false;
                EraseRequested = true;
                Settings = new SettingsData().Normalize();
                IsDirty = true;
                return true;
            case "vsync":
            case "reduced_flash":
            case "high_contrast":
            case "hold_to_block":
                Adjust(1);
                return false;
            default:
                return false;
        }
    }

    private static WindowDisplayMode CycleDisplayMode(
        WindowDisplayMode current,
        int sign)
    {
        var values = new[]
        {
            WindowDisplayMode.Windowed,
            WindowDisplayMode.Borderless,
            WindowDisplayMode.Fullscreen,
        };
        var index = Array.IndexOf(values, current);
        if (index < 0)
        {
            index = 0;
        }

        index = ((index + sign) % values.Length + values.Length) % values.Length;
        return values[index];
    }

    private void CycleResolution(int sign)
    {
        var index = -1;
        for (var candidate = 0; candidate < Resolutions.Length; candidate++)
        {
            if (Resolutions[candidate].Width == Settings.ResolutionWidth
                && Resolutions[candidate].Height == Settings.ResolutionHeight)
            {
                index = candidate;
                break;
            }
        }

        // An unlisted size, from a manual edit or a previous monitor, steps onto
        // the nearest listed one rather than jumping to an arbitrary end.
        if (index < 0)
        {
            index = 0;
            for (var candidate = 0; candidate < Resolutions.Length; candidate++)
            {
                if (Resolutions[candidate].Width <= Settings.ResolutionWidth)
                {
                    index = candidate;
                }
            }
        }
        else
        {
            index = Math.Clamp(index + sign, 0, Resolutions.Length - 1);
        }

        Settings.ResolutionWidth = Resolutions[index].Width;
        Settings.ResolutionHeight = Resolutions[index].Height;
    }

    // Deliberately not Godot's Mathf: this model stays engine-free so the
    // deterministic suite can exercise it without a scene tree.
    private static string Percent(float value) =>
        $"{(int)Math.Round(value * 100.0f, MidpointRounding.AwayFromZero)}%";
}
