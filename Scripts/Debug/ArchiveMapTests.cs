using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;
using ProjectMannequin.UI;

namespace ProjectMannequin.DebugTools;

public static class ArchiveMapTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label)
        {
            if (condition)
            {
                passed++;
                log.Append("  PASS ").Append(label).Append('\n');
            }
            else
            {
                failed++;
                log.Append("  FAIL ").Append(label).Append('\n');
            }
        }

        log.AppendLine("=== Archive Map Tests ===");
        var world = MvpWorldCatalog.FindWorld("archive_nexus")!;
        var run = WorldRunCatalog.CreateRun(world.Id);
        var empty = new MvpProgressData();
        var fresh = ArchiveMapModel.Build(world, run, empty, "", 0);
        Check(fresh.Status == ArchiveMapWorldStatus.Available,
            "fresh playable world is available");
        Check(fresh.Stages.Count == 4,
            "core world projects four map stages");
        Check(fresh.Stages[0].Status == ArchiveMapStageStatus.Available
            && fresh.Stages.Skip(1).All(stage => stage.Status == ArchiveMapStageStatus.Locked),
            "fresh route exposes stage one and locks later stages");

        var active = ArchiveMapModel.Build(world, run, empty, world.Id, 1);
        Check(active.Status == ArchiveMapWorldStatus.Active,
            "matching committed run marks world active");
        Check(active.Stages[0].Status == ArchiveMapStageStatus.Cleared
            && active.Stages[1].Status == ArchiveMapStageStatus.Active
            && active.Stages[2].Status == ArchiveMapStageStatus.Locked,
            "active stage two route resolves cleared active locked sequence");
        Check(active.ClearedStages == 1,
            "active checkpoint implies prior stage clear");

        var recorded = new MvpProgressData
        {
            BestStageRanks = new Dictionary<string, string>
            {
                [run.Stages[0].Id] = "A",
                [run.Stages[1].Id] = "S",
            },
            BestStageScores = new Dictionary<string, int>
            {
                [run.Stages[0].Id] = 18400,
            },
            BestStageTimesFrames = new Dictionary<string, int>
            {
                [run.Stages[0].Id] = 7200,
            },
        };
        var history = ArchiveMapModel.Build(world, run, recorded, "", 0);
        Check(history.Stages[0] is { Status: ArchiveMapStageStatus.Cleared, BestRank: "A", BestScore: 18400, BestTimeFrames: 7200 },
            "stage record projects rank score and time");
        Check(history.BestRank == "S" && history.ClearedStages == 2,
            "world summary resolves highest rank and cleared count");

        recorded.CompletedWorldIds.Add(world.Id);
        var completed = ArchiveMapModel.Build(world, run, recorded, "", 0);
        Check(completed.Status == ArchiveMapWorldStatus.Completed,
            "completed world receives completed badge");
        Check(completed.Stages.All(stage => stage.Status == ArchiveMapStageStatus.Replay),
            "completed route exposes all stages as replay history");
        Check(completed.ClearedStages == 4,
            "completed world reports four cleared stages");

        var lockedWorld = MvpWorldCatalog.FindWorld("iron_fist_foundry")!;
        var lockedRun = new WorldRunData
        {
            WorldId = lockedWorld.Id,
            DisplayName = lockedWorld.DisplayName,
            Stages = run.Stages,
        };
        var locked = ArchiveMapModel.Build(lockedWorld, lockedRun, empty, "", 0);
        Check(locked.Status == ArchiveMapWorldStatus.Locked
            && locked.Stages.All(stage => stage.Status == ArchiveMapStageStatus.Locked),
            "locked realm locks every route node");

        Check(!ArchiveMapModel.RequiresRunReplacement("", world.Id),
            "starting without active run needs no overwrite confirmation");
        Check(!ArchiveMapModel.RequiresRunReplacement(world.Id, world.Id),
            "continuing selected active world needs no overwrite confirmation");
        Check(ArchiveMapModel.RequiresRunReplacement(world.Id, "astral_battlefront"),
            "switching worlds requires overwrite confirmation");
        Check(ArchiveMapModel.RequiresRunReplacement(world.Id, world.Id, restartingSelectedWorld: true),
            "restarting active world requires overwrite confirmation");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
