using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Godot;
using ProjectMannequin.Core;

namespace ProjectMannequin.LocalInput;

public readonly record struct LocalInputDeviceOption(
    int DeviceId,
    string Label,
    string Kind,
    string Guid);

/// <summary>
/// Persists the preferred Player 1 input device independently from gameplay
/// progress. Gamepad runtime IDs are not stable between launches, so the saved
/// GUID is resolved back to the currently connected device each time.
/// </summary>
public static class InputDevicePreferences
{
    private const string SavePath = "user://project_mannequin_input_settings.json";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static InputPreferenceData? _cached;

    public static IReadOnlyList<LocalInputDeviceOption> AvailableP1Devices()
    {
        var devices = new List<LocalInputDeviceOption>
        {
            new(GameConstants.KeyboardDeviceId, "Keyboard", "keyboard", ""),
        };

        foreach (var deviceId in Input.GetConnectedJoypads())
        {
            devices.Add(new LocalInputDeviceOption(
                deviceId,
                FriendlyJoypadName(deviceId),
                "gamepad",
                Input.GetJoyGuid(deviceId)));
        }

        return devices;
    }

    public static int ResolveP1Device()
    {
        var preference = Load();
        return LocalInputAssignmentPolicy.ResolvePreferredP1Device(
            preference.DeviceKind,
            preference.JoyGuid,
            AvailableP1Devices());
    }

    public static string CurrentP1Label()
    {
        var resolved = ResolveP1Device();
        return resolved == GameConstants.KeyboardDeviceId
            ? "Keyboard"
            : FriendlyJoypadName(resolved);
    }

    public static void SelectP1Device(int deviceId)
    {
        if (deviceId == GameConstants.KeyboardDeviceId)
        {
            Save(new InputPreferenceData { DeviceKind = "keyboard" });
            return;
        }

        if (!Input.GetConnectedJoypads().Contains(deviceId))
        {
            return;
        }

        Save(new InputPreferenceData
        {
            DeviceKind = "gamepad",
            JoyGuid = Input.GetJoyGuid(deviceId),
            JoyName = Input.GetJoyName(deviceId),
        });
    }

    public static LocalInputDeviceOption CycleP1Device()
    {
        var devices = AvailableP1Devices();
        if (devices.Count == 0)
        {
            return new LocalInputDeviceOption(
                GameConstants.KeyboardDeviceId,
                "Keyboard",
                "keyboard",
                "");
        }

        var currentId = ResolveP1Device();
        var currentIndex = -1;
        for (var index = 0; index < devices.Count; index++)
        {
            if (devices[index].DeviceId == currentId)
            {
                currentIndex = index;
                break;
            }
        }

        var next = devices[(currentIndex + 1 + devices.Count) % devices.Count];
        SelectP1Device(next.DeviceId);
        return next;
    }

    public static void InvalidateCache()
    {
        _cached = null;
    }

    private static InputPreferenceData Load()
    {
        if (_cached is not null)
        {
            return _cached;
        }

        var path = ProjectSettings.GlobalizePath(SavePath);
        if (!File.Exists(path))
        {
            return _cached = new InputPreferenceData();
        }

        try
        {
            _cached = JsonSerializer.Deserialize<InputPreferenceData>(
                File.ReadAllText(path),
                JsonOptions) ?? new InputPreferenceData();
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not load input settings: {exception.Message}");
            _cached = new InputPreferenceData();
        }

        return _cached;
    }

    private static void Save(InputPreferenceData preference)
    {
        var path = ProjectSettings.GlobalizePath(SavePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            var temporaryPath = path + ".tmp";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(preference, JsonOptions));
            File.Move(temporaryPath, path, overwrite: true);
            _cached = preference;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not save input settings: {exception.Message}");
        }
    }

    private static string FriendlyJoypadName(int deviceId)
    {
        var name = Input.GetJoyName(deviceId);
        return string.IsNullOrWhiteSpace(name)
            ? $"Gamepad {deviceId + 1}"
            : name;
    }

    private sealed class InputPreferenceData
    {
        public string DeviceKind { get; set; } = "keyboard";
        public string JoyGuid { get; set; } = "";
        public string JoyName { get; set; } = "";
    }
}
