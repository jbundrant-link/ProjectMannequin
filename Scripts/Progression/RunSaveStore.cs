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

    /// <summary>
    /// Oldest run checkpoint this build can still migrate forward.
    /// </summary>
    private const int MinimumSupportedSchemaVersion = 1;
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

        if (!SaveSchema.MayOverwrite(SaveSchema.Evaluate(
                checkpoint.SchemaVersion,
                CurrentSchemaVersion,
                MinimumSupportedSchemaVersion)))
        {
            GD.PushWarning(
                $"Refusing to write active run save at schema v{checkpoint.SchemaVersion} "
                + $"with a build that writes v{CurrentSchemaVersion}. "
                + "The newer file is left intact.");
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
            SaveSchema.NotifySaved();
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

    /// <summary>
    /// Brings an older run checkpoint forward to the current schema.
    /// </summary>
    /// <remarks>
    /// v1 is currently the only shape, so this only stamps the version. Real
    /// steps go here as the checkpoint grows, and each one must be additive or
    /// explicitly transform the field it renames.
    /// </remarks>
    private static void Migrate(RunStageCheckpoint checkpoint)
    {
        checkpoint.SchemaVersion = CurrentSchemaVersion;
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
                || string.IsNullOrWhiteSpace(loaded.WorldId)
                || loaded.StageIndex < 0)
            {
                return false;
            }

            // Version mismatch used to discard the file outright, which threw
            // away a player's in-progress run on any schema bump. Migrate it
            // instead, and keep a newer build's file readable but read-only.
            var compatibility = SaveSchema.Evaluate(
                loaded.SchemaVersion,
                CurrentSchemaVersion,
                MinimumSupportedSchemaVersion);
            switch (compatibility)
            {
                case SaveCompatibility.Unsupported:
                    GD.PushWarning(
                        $"Active run save '{path}' is schema v{loaded.SchemaVersion}, "
                        + $"below the minimum supported v{MinimumSupportedSchemaVersion}. "
                        + "Discarding it.");
                    return false;
                case SaveCompatibility.FutureVersion:
                    SaveSchema.WarnFutureSave(
                        path, loaded.SchemaVersion, CurrentSchemaVersion);
                    break;
                case SaveCompatibility.Migrated:
                    Migrate(loaded);
                    break;
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
