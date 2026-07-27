using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace ProjectMannequin.Settings;

/// <summary>
/// Loads, persists, and applies <see cref="SettingsData"/>.
/// </summary>
/// <remarks>
/// Uses its own file so settings survive a progression reset or a corrupt run
/// save. Writes are atomic with a backup copy, and loads fall back to that
/// backup, because a half-written settings file that muted audio or forced an
/// unusable resolution would leave a player with no in-game way to recover.
/// </remarks>
public static class SettingsStore
{
    private const string SavePath = "user://project_mannequin_settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static SettingsData? _cached;

    /// <summary>Headless suites must not write a developer's real settings.</summary>
    public static bool IsPersistenceDisabled =>
        ProjectMannequin.Progression.MvpProgressStore.IsPersistenceDisabled;

    public static string SettingsSavePath => SavePath;

    public static SettingsData Current => _cached ??= Load();

    public static SettingsData Load()
    {
        var path = ProjectSettings.GlobalizePath(SavePath);
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate))
            {
                continue;
            }

            try
            {
                var data = ApplyCaptureOverrides(FromJson(File.ReadAllText(candidate)));
                _cached = data;
                return data;
            }
            catch (Exception exception)
                when (exception is IOException or UnauthorizedAccessException)
            {
                GD.PushWarning(
                    $"Could not load settings '{candidate}': {exception.Message}");
            }
        }

        _cached = ApplyCaptureOverrides(new SettingsData().Normalize());
        return _cached;
    }

    /// <summary>
    /// Lets capture tooling exercise an accessibility mode without touching a
    /// real settings file.
    /// </summary>
    /// <remarks>
    /// Gated on <see cref="IsPersistenceDisabled"/> so it can only ever apply
    /// inside a smoke or capture run, never in a player's session.
    /// </remarks>
    private static SettingsData ApplyCaptureOverrides(SettingsData data)
    {
        if (!IsPersistenceDisabled)
        {
            return data;
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_FORCE_HIGH_CONTRAST_TELEGRAPHS") == "1")
        {
            data.HighContrastTelegraphs = true;
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_FORCE_REDUCED_FLASH") == "1")
        {
            data.ReducedFlash = true;
        }

        return data;
    }

    public static void Save(SettingsData data)
    {
        var normalized = (data ?? new SettingsData()).Normalize();
        _cached = normalized;
        if (IsPersistenceDisabled)
        {
            return;
        }

        var path = ProjectSettings.GlobalizePath(SavePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var temporaryPath = path + ".tmp";
            var backupPath = path + ".bak";
            File.WriteAllText(temporaryPath, ToJson(normalized));
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception exception)
            when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not save settings: {exception.Message}");
        }
    }

    public static SettingsData ResetToDefaults()
    {
        var defaults = new SettingsData().Normalize();
        Save(defaults);
        return defaults;
    }

    /// <summary>Drops the cache so the next read comes from disk.</summary>
    public static void Invalidate() => _cached = null;

    public static string ToJson(SettingsData data) =>
        JsonSerializer.Serialize(data ?? new SettingsData(), JsonOptions);

    /// <summary>
    /// Parses settings JSON, falling back to defaults for anything unreadable.
    /// </summary>
    /// <remarks>
    /// Malformed or partial content yields normalized defaults rather than an
    /// exception, so a bad file degrades to a usable options surface instead of
    /// blocking startup.
    /// </remarks>
    public static SettingsData FromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new SettingsData().Normalize();
        }

        try
        {
            var data = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
            return (data ?? new SettingsData()).Normalize();
        }
        catch (JsonException)
        {
            return new SettingsData().Normalize();
        }
    }

    /// <summary>Pushes settings onto the audio buses and the window.</summary>
    public static void Apply(SettingsData data)
    {
        var normalized = (data ?? new SettingsData()).Normalize();
        _cached = normalized;
        ApplyAudio(normalized);
        ApplyWindow(normalized);
    }

    /// <summary>
    /// Applies only the audio buses.
    /// </summary>
    /// <remarks>
    /// Startup uses this rather than <see cref="Apply"/> because the window
    /// half of Apply would fight the capture viewport that the runtime capture
    /// tooling configures.
    /// </remarks>
    public static void ApplyAudio(SettingsData data)
    {
        SetBusVolume("Master", data.MasterVolume);
        SetBusVolume("Music", data.MusicVolume);
        SetBusVolume("SFX", data.SfxVolume);
        SetBusVolume("UI", data.UiVolume);
    }

    private static void SetBusVolume(string busName, float volume)
    {
        var index = EnsureBus(busName);
        if (index < 0)
        {
            return;
        }

        // Silence is muted outright: LinearToDb of zero is negative infinity,
        // which some drivers treat as an invalid volume rather than silence.
        AudioServer.SetBusMute(index, volume <= 0.0f);
        AudioServer.SetBusVolumeDb(
            index,
            Mathf.LinearToDb(Mathf.Max(0.0001f, volume)));
    }

    private static int EnsureBus(string busName)
    {
        var index = AudioServer.GetBusIndex(busName);
        if (index >= 0)
        {
            return index;
        }

        if (busName == "Master")
        {
            return AudioServer.BusCount > 0 ? 0 : -1;
        }

        // Players reference these buses by name, so create any that the project
        // has not defined instead of silently routing everything to Master.
        AudioServer.AddBus();
        index = AudioServer.BusCount - 1;
        AudioServer.SetBusName(index, busName);
        AudioServer.SetBusSend(index, "Master");
        return index;
    }

    private static void ApplyWindow(SettingsData data)
    {
        if (DisplayServer.GetName() == "headless")
        {
            return;
        }

        DisplayServer.WindowSetVsyncMode(data.VSyncEnabled
            ? DisplayServer.VSyncMode.Enabled
            : DisplayServer.VSyncMode.Disabled);

        switch (data.DisplayMode)
        {
            case WindowDisplayMode.Fullscreen:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                break;
            case WindowDisplayMode.Borderless:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(
                    DisplayServer.WindowFlags.Borderless,
                    true);
                break;
            default:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(
                    DisplayServer.WindowFlags.Borderless,
                    false);
                DisplayServer.WindowSetSize(new Vector2I(
                    data.ResolutionWidth,
                    data.ResolutionHeight));
                break;
        }
    }
}
