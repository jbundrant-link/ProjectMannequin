using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Godot;

namespace ProjectMannequin.Progression;

public static class MvpProgressStore
{
    private const string SavePath = "user://project_mannequin_mvp_progress.json";
    private const int CurrentSchemaVersion = 2;
    private static readonly string[] NonPersistentRunFlags =
    {
        "PROJECT_MANNEQUIN_COMBAT_SMOKE_TEST",
        "PROJECT_MANNEQUIN_BOSS_SMOKE_TEST",
        "PROJECT_MANNEQUIN_CPU_SMOKE_TEST",
        "PROJECT_MANNEQUIN_STAGE_SMOKE_TEST",
        "PROJECT_MANNEQUIN_SCROLL_SMOKE_TEST",
        "PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST",
        "PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST",
        "PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST",
        "PROJECT_MANNEQUIN_LADDER_SMOKE_TEST",
        "PROJECT_MANNEQUIN_WORLD_RUN_TEST",
        "PROJECT_MANNEQUIN_RUN_CHECKPOINT_TEST",
        "PROJECT_MANNEQUIN_RUN_SCORE_TEST",
        "PROJECT_MANNEQUIN_STAGE_HAZARD_TEST",
        "PROJECT_MANNEQUIN_BOSS_INTRO_HUD_SMOKE_TEST",
        "PROJECT_MANNEQUIN_MOVE_LIST_SMOKE_TEST",
        "PROJECT_MANNEQUIN_FORM_SELECT_UI_SMOKE_TEST",
        "PROJECT_MANNEQUIN_SCENE_FLOW_SMOKE_TEST",
        "PROJECT_MANNEQUIN_ARCHIVE_MAP_TEST",
        "PROJECT_MANNEQUIN_ARCHIVE_MAP_SMOKE_TEST",
        "PROJECT_MANNEQUIN_RESULTS_FLOW_TEST",
        "PROJECT_MANNEQUIN_RESULTS_FLOW_SMOKE_TEST",
        "PROJECT_MANNEQUIN_VISUAL_POSE_TEST",
        "PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE",
    };

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static bool IsPersistenceDisabled =>
        Array.Exists(NonPersistentRunFlags, flag => OS.GetEnvironment(flag) == "1");

    public static MvpProgressData Load()
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
                var data = JsonSerializer.Deserialize<MvpProgressData>(File.ReadAllText(candidate), JsonOptions);
                data ??= new MvpProgressData();
                data.UnlockedFormIds ??= new List<string>();
                data.UnlockedLoreFragments ??= new List<string>();
                data.BestStageScores ??= new Dictionary<string, int>();
                data.BestStageTimesFrames ??= new Dictionary<string, int>();
                data.BestStageRanks ??= new Dictionary<string, string>();
                data.CompletedWorldIds ??= new List<string>();
                data.SchemaVersion = CurrentSchemaVersion;
                return data;
            }
            catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
            {
                GD.PushWarning($"Could not load progress save '{candidate}': {exception.Message}");
            }
        }

        return new MvpProgressData();
    }

    public static bool HasUnlockedForm(string formId)
    {
        return Load().UnlockedFormIds.Contains(formId);
    }

    public static bool UnlockForm(string formId)
    {
        if (string.IsNullOrWhiteSpace(formId) || IsPersistenceDisabled)
        {
            return false;
        }

        var data = Load();
        if (data.UnlockedFormIds.Contains(formId))
        {
            return false;
        }

        data.UnlockedFormIds.Add(formId);
        Save(data);
        return true;
    }

    public static bool UnlockLore(string loreId)
    {
        if (string.IsNullOrWhiteSpace(loreId) || IsPersistenceDisabled)
        {
            return false;
        }

        var data = Load();
        if (data.UnlockedLoreFragments.Contains(loreId))
        {
            return false;
        }

        data.UnlockedLoreFragments.Add(loreId);
        Save(data);
        return true;
    }

    public static bool AllLoreCollected()
    {
        return AllLoreCollectedFrom(Load());
    }

    private static bool AllLoreCollectedFrom(MvpProgressData data)
    {
        var all = ProjectMannequin.Data.LoreCatalog.AllFragmentIds;
        if (all.Count == 0)
        {
            return false;
        }

        foreach (var id in all)
        {
            if (!data.UnlockedLoreFragments.Contains(id))
            {
                return false;
            }
        }

        return true;
    }

    public static bool TryUnlockSecretEnding()
    {
        if (IsPersistenceDisabled)
        {
            return false;
        }

        var data = Load();
        if (data.SecretEndingUnlocked || !AllLoreCollectedFrom(data))
        {
            return false;
        }

        data.SecretEndingUnlocked = true;
        Save(data);
        return true;
    }

    public static bool RecordStageResult(
        ProjectMannequin.Data.StageMissionData mission,
        StageResultsData result)
    {
        if (IsPersistenceDisabled)
        {
            return false;
        }

        var data = Load();
        var changed = false;
        if (!data.BestStageScores.TryGetValue(mission.Id, out var bestScore)
            || result.StageTotal > bestScore)
        {
            data.BestStageScores[mission.Id] = result.StageTotal;
            changed = true;
        }

        if (!data.BestStageTimesFrames.TryGetValue(mission.Id, out var bestTime)
            || bestTime <= 0
            || result.ActiveFrames < bestTime)
        {
            data.BestStageTimesFrames[mission.Id] = result.ActiveFrames;
            changed = true;
        }

        var previousRank = data.BestStageRanks.TryGetValue(mission.Id, out var rankText)
            && System.Enum.TryParse<StageRank>(rankText, out var parsedRank)
                ? parsedRank
                : StageRank.D;
        if (result.Rank > previousRank || !data.BestStageRanks.ContainsKey(mission.Id))
        {
            data.BestStageRanks[mission.Id] = result.Rank.ToString();
            changed = true;
        }

        if (mission.IsFinalStage && !data.CompletedWorldIds.Contains(mission.WorldId))
        {
            data.CompletedWorldIds.Add(mission.WorldId);
            changed = true;
        }

        if (changed)
        {
            Save(data);
        }

        return changed;
    }

    public static void Save(MvpProgressData data)
    {
        var path = ProjectSettings.GlobalizePath(SavePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        try
        {
            data.SchemaVersion = CurrentSchemaVersion;
            var temporaryPath = path + ".tmp";
            var backupPath = path + ".bak";
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(data, JsonOptions));
            if (File.Exists(path))
            {
                File.Copy(path, backupPath, overwrite: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            GD.PushWarning($"Could not save MVP progress: {exception.Message}");
        }
    }
}

public sealed class MvpProgressData
{
    public int SchemaVersion { get; set; } = 2;
    public List<string> UnlockedFormIds { get; set; } = new();
    public List<string> UnlockedLoreFragments { get; set; } = new();
    public Dictionary<string, int> BestStageScores { get; set; } = new();
    public Dictionary<string, int> BestStageTimesFrames { get; set; } = new();
    public Dictionary<string, string> BestStageRanks { get; set; } = new();
    public List<string> CompletedWorldIds { get; set; } = new();
    public bool SecretEndingUnlocked { get; set; }
}
