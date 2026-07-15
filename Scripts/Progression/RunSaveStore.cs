using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace ProjectMannequin.Progression;

public sealed class RunStageCheckpoint
{
    public int SchemaVersion { get; set; } = 1;
    public string WorldId { get; set; } = "";
    public int StageIndex { get; set; }
    public int RemainingLives { get; set; } = 3;
    public int PlayerHealth { get; set; } = -1;
    public int PlayerMeter { get; set; }
    public string CurrentFormId { get; set; } = "blank_mannequin";
    public List<string> EquippedFormIds { get; set; } = new();
    public List<string> EquippedMoveCards { get; set; } = new();
    public List<string> ActiveArtifacts { get; set; } = new();
    public int RunScore { get; set; }
    public int ExtraLifeThresholdIndex { get; set; }
}

/// <summary>
/// Stores only committed stage-entry checkpoints. Writes are atomic and retain a
/// last-known-good backup so a crash can never convert a valid run into corrupt
/// JSON. Mid-stage mutations intentionally remain memory-only.
/// </summary>
public static class RunSaveStore
{
    private const string SavePath = "user://project_mannequin_active_run.json";
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static bool TryLoad(out RunStageCheckpoint checkpoint)
    {
        if (MvpProgressStore.IsPersistenceDisabled)
        {
            checkpoint = new RunStageCheckpoint();
            return false;
        }

        var path = ProjectSettings.GlobalizePath(SavePath);
        if (TryLoadPath(path, out checkpoint))
        {
            return true;
        }

        return TryLoadPath(path + ".bak", out checkpoint);
    }

    public static bool Save(RunStageCheckpoint checkpoint)
    {
        if (MvpProgressStore.IsPersistenceDisabled || string.IsNullOrWhiteSpace(checkpoint.WorldId))
        {
            return false;
        }

        checkpoint.SchemaVersion = CurrentSchemaVersion;
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
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(checkpoint, JsonOptions));
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not save active run: {exception.Message}");
            return false;
        }
    }

    public static void Delete()
    {
        if (MvpProgressStore.IsPersistenceDisabled)
        {
            return;
        }

        foreach (var suffix in new[] { "", ".tmp", ".bak" })
        {
            var path = ProjectSettings.GlobalizePath(SavePath + suffix);
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                GD.PushWarning($"Could not delete active run save: {exception.Message}");
            }
        }
    }

    private static bool TryLoadPath(string path, out RunStageCheckpoint checkpoint)
    {
        checkpoint = new RunStageCheckpoint();
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<RunStageCheckpoint>(
                File.ReadAllText(path),
                JsonOptions);
            if (loaded is null
                || loaded.SchemaVersion != CurrentSchemaVersion
                || string.IsNullOrWhiteSpace(loaded.WorldId)
                || loaded.StageIndex < 0)
            {
                return false;
            }

            loaded.EquippedFormIds ??= new List<string>();
            loaded.EquippedMoveCards ??= new List<string>();
            loaded.ActiveArtifacts ??= new List<string>();
            checkpoint = loaded;
            return true;
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not load active run save '{path}': {exception.Message}");
            return false;
        }
    }
}
