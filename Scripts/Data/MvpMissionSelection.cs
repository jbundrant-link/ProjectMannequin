using Godot;
using ProjectMannequin.Progression;

namespace ProjectMannequin.Data;

public static class MvpMissionSelection
{
    public const string DefaultWorldId = "archive_nexus";
    public const string WorldOverrideEnvironmentVariable = "PROJECT_MANNEQUIN_WORLD_ID";
    public const string StageOverrideEnvironmentVariable = "PROJECT_MANNEQUIN_STAGE_INDEX";

    private static string _selectedWorldId = DefaultWorldId;

    public static string SelectedWorldId
    {
        get
        {
            var environmentOverride = OS.GetEnvironment(WorldOverrideEnvironmentVariable);
            if (!string.IsNullOrWhiteSpace(environmentOverride))
            {
                return environmentOverride;
            }

            // World-specific smoke tests select their own world so they are
            // self-contained and cannot accidentally run against the default
            // mission. An explicit PROJECT_MANNEQUIN_WORLD_ID still wins above.
            if (OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1")
            {
                return "world_warrior_sector";
            }

            if (OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1")
            {
                return "astral_battlefront";
            }

            // Training mode uses the empty simulation deck so the practice dummy
            // is the only opponent in the arena.
            if (!string.IsNullOrWhiteSpace(OS.GetEnvironment("PROJECT_MANNEQUIN_TRAINING_DUMMY")))
            {
                return "training_room";
            }

            return _selectedWorldId;
        }
    }

    public static bool TrySelectWorld(string worldId)
    {
        if (worldId == HollowArchiveMission.WorldId
            && ProjectMannequin.Progression.MvpProgressStore.Load().SecretEndingUnlocked)
        {
            _selectedWorldId = worldId;
            return true;
        }

        var world = MvpWorldCatalog.FindWorld(worldId);
        if (world is null || world.Availability != ArchiveWorldAvailability.Playable)
        {
            return false;
        }

        _selectedWorldId = world.Id;
        return true;
    }

    public static StageMissionData CreateSelectedMission()
    {
        if (SelectedWorldId == HollowArchiveMission.WorldId)
        {
            return HollowArchiveMission.Create();
        }

        if (SelectedWorldId == "training_room")
        {
            return MvpWorldCatalog.CreateTrainingRoomMission();
        }

        var run = WorldRunCatalog.CreateRun(SelectedWorldId);
        var stageIndex = ResolveSelectedStageIndex(run.Stages.Count);
        return run.Stages[stageIndex];
    }

    private static int ResolveSelectedStageIndex(int stageCount)
    {
        var explicitStage = OS.GetEnvironment(StageOverrideEnvironmentVariable);
        if (int.TryParse(explicitStage, out var authoredStageNumber))
        {
            // Human-facing environment override is 1-based: 1..4.
            return Mathf.Clamp(authoredStageNumber - 1, 0, stageCount - 1);
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_CPU_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_INTRO_HUD_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_SPLIT_SCREEN_TEST") == "1")
        {
            return stageCount - 1;
        }

        var session = RunSessionManager.Instance;
        return session.HasActiveRun && session.CurrentWorldId == SelectedWorldId
            ? Mathf.Clamp(session.CurrentStageIndex, 0, stageCount - 1)
            : 0;
    }
}
