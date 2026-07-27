using System;
using System.IO;
using Godot;
using ProjectMannequin.LocalInput;

namespace ProjectMannequin.Progression;

/// <summary>
/// Erases every persistent save file the game writes.
/// </summary>
/// <remarks>
/// Kept separate from the individual stores because the stores each own one
/// file and none of them should be able to delete another's. Callers must
/// route through a confirmation: this is irreversible and there is no undo.
/// </remarks>
public static class SaveDataReset
{
    /// <summary>
    /// Every save file the game owns. Backups are erased alongside their
    /// primary, otherwise a reset would silently restore itself from a .bak on
    /// the next load.
    /// </summary>
    public static readonly string[] SavePaths =
    {
        "user://project_mannequin_mvp_progress.json",
        "user://project_mannequin_active_run.json",
        "user://project_mannequin_settings.json",
        "user://project_mannequin_input_settings.json",
    };

    /// <summary>
    /// Deletes all save data. Returns the number of files actually removed.
    /// </summary>
    public static int EraseAll()
    {
        if (MvpProgressStore.IsPersistenceDisabled)
        {
            // A smoke or capture run must never wipe a developer's real saves.
            return 0;
        }

        var removed = 0;
        foreach (var savePath in SavePaths)
        {
            var path = ProjectSettings.GlobalizePath(savePath);
            foreach (var candidate in new[] { path, path + ".bak", path + ".tmp" })
            {
                try
                {
                    if (File.Exists(candidate))
                    {
                        File.Delete(candidate);
                        removed++;
                    }
                }
                catch (Exception exception)
                    when (exception is IOException or UnauthorizedAccessException)
                {
                    GD.PushWarning($"Could not erase save '{candidate}': {exception.Message}");
                }
            }
        }

        Settings.SettingsStore.Invalidate();
        InputDevicePreferences.InvalidateCache();
        return removed;
    }
}
