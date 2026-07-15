using System;
using System.Collections.Generic;
using System.Linq;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;

namespace ProjectMannequin.UI;

public enum ArchiveMapWorldStatus
{
    Locked,
    Available,
    Active,
    Completed,
}

public enum ArchiveMapStageStatus
{
    Locked,
    Available,
    Active,
    Cleared,
    Replay,
}

public sealed record ArchiveMapStageView(
    string MissionId,
    int StageNumber,
    string Title,
    string Subtitle,
    float ParTimeSeconds,
    ArchiveMapStageStatus Status,
    string BestRank,
    int BestScore,
    int BestTimeFrames)
{
    public bool HasRecord => BestScore > 0 || BestTimeFrames > 0 || BestRank != "—";
}

public sealed record ArchiveMapWorldView(
    string WorldId,
    string DisplayName,
    string CombatIdentity,
    ArchiveMapWorldStatus Status,
    int ClearedStages,
    string BestRank,
    IReadOnlyList<ArchiveMapStageView> Stages);

/// <summary>
/// Pure Archive Map projection. UI consumes this model but never infers run or
/// progression state itself, keeping ACTIVE/CLEARED/LOCKED/REPLAY rules testable.
/// </summary>
public static class ArchiveMapModel
{
    public static ArchiveMapWorldView Build(
        ArchiveWorldData world,
        WorldRunData run,
        MvpProgressData progress,
        string activeWorldId,
        int activeStageIndex)
    {
        var worldLocked = world.Availability != ArchiveWorldAvailability.Playable;
        var worldActive = !worldLocked
            && string.Equals(activeWorldId, world.Id, StringComparison.Ordinal);
        var worldCompleted = !worldLocked && progress.CompletedWorldIds.Contains(world.Id);
        var currentStage = run.Stages.Count == 0
            ? 0
            : Math.Clamp(activeStageIndex, 0, run.Stages.Count - 1);
        var stages = new List<ArchiveMapStageView>(run.Stages.Count);
        var clearedCount = 0;
        var bestRank = StageRank.D;
        var hasBestRank = false;

        for (var index = 0; index < run.Stages.Count; index++)
        {
            var stage = run.Stages[index];
            var rankText = progress.BestStageRanks.TryGetValue(stage.Id, out var storedRank)
                ? storedRank.ToUpperInvariant()
                : "—";
            var bestScore = progress.BestStageScores.GetValueOrDefault(stage.Id);
            var bestTime = progress.BestStageTimesFrames.GetValueOrDefault(stage.Id);
            var hasRecord = rankText != "—" || bestScore > 0 || bestTime > 0;
            var impliedClearedByRun = worldActive && index < currentStage;
            var cleared = hasRecord || impliedClearedByRun || worldCompleted;
            if (cleared)
            {
                clearedCount++;
            }

            if (Enum.TryParse<StageRank>(rankText, true, out var parsedRank)
                && (!hasBestRank || parsedRank > bestRank))
            {
                bestRank = parsedRank;
                hasBestRank = true;
            }

            var status = ResolveStageStatus(
                worldLocked,
                worldActive,
                worldCompleted,
                currentStage,
                index,
                hasRecord);
            stages.Add(new ArchiveMapStageView(
                stage.Id,
                stage.StageNumber,
                stage.StageTitle,
                stage.StageSubtitle,
                stage.ParTimeSeconds,
                status,
                rankText,
                bestScore,
                bestTime));
        }

        var worldStatus = worldLocked
            ? ArchiveMapWorldStatus.Locked
            : worldActive
                ? ArchiveMapWorldStatus.Active
                : worldCompleted
                    ? ArchiveMapWorldStatus.Completed
                    : ArchiveMapWorldStatus.Available;
        return new ArchiveMapWorldView(
            world.Id,
            world.DisplayName,
            world.CombatIdentity,
            worldStatus,
            Math.Min(clearedCount, run.Stages.Count),
            hasBestRank ? bestRank.ToString() : "—",
            stages);
    }

    public static bool RequiresRunReplacement(
        string activeWorldId,
        string selectedWorldId,
        bool restartingSelectedWorld = false)
    {
        if (string.IsNullOrWhiteSpace(activeWorldId))
        {
            return false;
        }

        return restartingSelectedWorld
            || !string.Equals(activeWorldId, selectedWorldId, StringComparison.Ordinal);
    }

    private static ArchiveMapStageStatus ResolveStageStatus(
        bool worldLocked,
        bool worldActive,
        bool worldCompleted,
        int activeStageIndex,
        int stageIndex,
        bool hasRecord)
    {
        if (worldLocked)
        {
            return ArchiveMapStageStatus.Locked;
        }

        if (worldActive)
        {
            if (stageIndex < activeStageIndex)
            {
                return ArchiveMapStageStatus.Cleared;
            }

            return stageIndex == activeStageIndex
                ? ArchiveMapStageStatus.Active
                : ArchiveMapStageStatus.Locked;
        }

        if (worldCompleted)
        {
            return ArchiveMapStageStatus.Replay;
        }

        if (hasRecord)
        {
            return ArchiveMapStageStatus.Cleared;
        }

        return stageIndex == 0
            ? ArchiveMapStageStatus.Available
            : ArchiveMapStageStatus.Locked;
    }
}
