using System.Collections.Generic;

namespace ProjectMannequin.Settings;

public enum WindowDisplayMode
{
    Windowed,
    Fullscreen,
    Borderless,
}

/// <summary>
/// Player-owned audio, video, accessibility, and control settings.
/// </summary>
/// <remarks>
/// Deliberately separate from progression and run saves: losing or resetting a
/// run must never cost a player their volume, accessibility, or binding choices,
/// and a corrupt progress file must not take the options surface down with it.
/// </remarks>
public sealed class SettingsData
{
    public const int CurrentSchemaVersion = 1;

    public const float MinimumRenderScale = 0.50f;
    public const float MaximumRenderScale = 2.00f;
    public const float MinimumHudScale = 0.75f;
    public const float MaximumHudScale = 1.50f;
    public const float MaximumHudSafeAreaInset = 0.10f;
    public const int MinimumResolutionWidth = 640;
    public const int MinimumResolutionHeight = 360;
    public const int MaximumResolutionWidth = 7680;
    public const int MaximumResolutionHeight = 4320;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.80f;
    public float SfxVolume { get; set; } = 1.0f;
    public float UiVolume { get; set; } = 0.90f;

    public WindowDisplayMode DisplayMode { get; set; } = WindowDisplayMode.Windowed;
    public int ResolutionWidth { get; set; } = 1920;
    public int ResolutionHeight { get; set; } = 1080;
    public float RenderScale { get; set; } = 1.0f;
    public bool VSyncEnabled { get; set; } = true;

    public float HudScale { get; set; } = 1.0f;
    public float HudSafeAreaInset { get; set; }

    // Accessibility. ShakeIntensity 0 must be fully honoured rather than merely
    // reduced, because motion sensitivity is the reason the setting exists.
    public float ShakeIntensity { get; set; } = 1.0f;
    public bool ReducedFlash { get; set; }
    public bool HighContrastTelegraphs { get; set; }

    public bool HoldToBlock { get; set; } = true;

    public Dictionary<string, string> ActionBindings { get; set; } = new();

    /// <summary>
    /// Forces every field into its supported range.
    /// </summary>
    /// <remarks>
    /// Applied on load rather than trusted, so a hand-edited, truncated, or
    /// older-schema file can never push the renderer, audio buses, or HUD into a
    /// state the options surface cannot represent or the player cannot undo.
    /// </remarks>
    public SettingsData Normalize()
    {
        SchemaVersion = CurrentSchemaVersion;

        MasterVolume = ClampUnit(MasterVolume);
        MusicVolume = ClampUnit(MusicVolume);
        SfxVolume = ClampUnit(SfxVolume);
        UiVolume = ClampUnit(UiVolume);

        if (DisplayMode is not (WindowDisplayMode.Windowed
            or WindowDisplayMode.Fullscreen
            or WindowDisplayMode.Borderless))
        {
            DisplayMode = WindowDisplayMode.Windowed;
        }

        ResolutionWidth = Clamp(
            ResolutionWidth,
            MinimumResolutionWidth,
            MaximumResolutionWidth);
        ResolutionHeight = Clamp(
            ResolutionHeight,
            MinimumResolutionHeight,
            MaximumResolutionHeight);
        RenderScale = Clamp(RenderScale, MinimumRenderScale, MaximumRenderScale);

        HudScale = Clamp(HudScale, MinimumHudScale, MaximumHudScale);
        HudSafeAreaInset = Clamp(HudSafeAreaInset, 0.0f, MaximumHudSafeAreaInset);

        ShakeIntensity = ClampUnit(ShakeIntensity);

        ActionBindings ??= new Dictionary<string, string>();
        return this;
    }

    public SettingsData Clone()
    {
        return new SettingsData
        {
            SchemaVersion = SchemaVersion,
            MasterVolume = MasterVolume,
            MusicVolume = MusicVolume,
            SfxVolume = SfxVolume,
            UiVolume = UiVolume,
            DisplayMode = DisplayMode,
            ResolutionWidth = ResolutionWidth,
            ResolutionHeight = ResolutionHeight,
            RenderScale = RenderScale,
            VSyncEnabled = VSyncEnabled,
            HudScale = HudScale,
            HudSafeAreaInset = HudSafeAreaInset,
            ShakeIntensity = ShakeIntensity,
            ReducedFlash = ReducedFlash,
            HighContrastTelegraphs = HighContrastTelegraphs,
            HoldToBlock = HoldToBlock,
            ActionBindings = new Dictionary<string, string>(
                ActionBindings ?? new Dictionary<string, string>()),
        };
    }

    private static float ClampUnit(float value) => Clamp(value, 0.0f, 1.0f);

    private static float Clamp(float value, float minimum, float maximum)
    {
        if (float.IsNaN(value))
        {
            return minimum;
        }

        return value < minimum ? minimum : value > maximum ? maximum : value;
    }

    private static int Clamp(int value, int minimum, int maximum) =>
        value < minimum ? minimum : value > maximum ? maximum : value;
}
