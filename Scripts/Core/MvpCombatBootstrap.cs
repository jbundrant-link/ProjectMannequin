using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Content;
using ProjectMannequin.Data;
using ProjectMannequin.Presentation;
using ProjectMannequin.Progression;
using ProjectMannequin.Stage;
using ProjectMannequin.DebugTools;
using ProjectMannequin.UI;

namespace ProjectMannequin.Core;

public partial class MvpCombatBootstrap : Node
{
    [Export] public NodePath SimulationPath { get; set; } = "GameSimulation";
    [Export] public NodePath ContentManagerPath { get; set; } = "ContentManager";
    [Export] public NodePath ActorRootPath { get; set; } = "Actors";

    public override void _Ready()
    {
        var simulation = GetNode<GameSimulation>(SimulationPath);
        var contentManager = GetNodeOrNull<ContentManager>(ContentManagerPath);
        var actorRoot = GetNode<Node3D>(ActorRootPath);
        var mission = MvpMissionSelection.CreateSelectedMission();
        var resultsFlowSmokeTest =
            OS.GetEnvironment("PROJECT_MANNEQUIN_RESULTS_FLOW_SMOKE_TEST") == "1";
        if ((OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_SMOKE_TEST") == "1"
                || resultsFlowSmokeTest)
            && !RunSessionManager.Instance.HasActiveRun)
        {
            RunSessionManager.Instance.StartNewRun(mission.WorldId);
            RunSessionManager.Instance.CurrentStageIndex = Mathf.Max(0, mission.StageNumber - 1);
        }

        // Opt-in split-screen QA: force the boss into a 2-player isolated duel so
        // the SplitScreenOverlay (needs BossType=IsolatedDuel + PerPlayer spawns)
        // actually activates. No effect on normal play.
        var splitScreenTest = OS.GetEnvironment("PROJECT_MANNEQUIN_SPLIT_SCREEN_TEST") == "1";
        if (splitScreenTest)
        {
            foreach (var encounter in mission.Encounters)
            {
                if (encounter.Kind == StageEncounterKind.Boss)
                {
                    encounter.BossType = BossEncounterType.IsolatedDuel;
                    encounter.SpawnPolicy = EnemySpawnPolicy.PerPlayer;
                    // P1 fights this stage's boss; P2 fights a different boss so the
                    // two isolated duels are visibly distinct.
                    encounter.IsolatedDuelBossArchetypeIds = new System.Collections.Generic.List<string>
                    {
                        "world_warrior_ryu_boss",
                        "astral_goku_boss",
                    };
                }
            }
        }

        contentManager?.ScanUserContent();

        var playerForm = TestRosterFactory.CreateBlankMannequin();
        foreach (var moveCardId in RunSessionManager.Instance.EquippedMoveCards)
        {
            var card = MoveCardCatalog.GetCard(moveCardId);
            if (card != null)
            {
                playerForm.Moves.Add(card.Move);
            }
        }

        var rawPlayerCount = OS.GetEnvironment("PROJECT_MANNEQUIN_PLAYER_COUNT");
        int playerCount = 1;
        if (!string.IsNullOrWhiteSpace(rawPlayerCount) && int.TryParse(rawPlayerCount, out var parsed))
        {
            playerCount = Mathf.Clamp(parsed, 1, GameConstants.MaxPlayers);
        }
        
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_CAMERA_SMOKE_TEST") == "1")
        {
            playerCount = Mathf.Max(playerCount, 2);
        }

        if (splitScreenTest)
        {
            playerCount = Mathf.Max(playerCount, 2);
        }

        CombatActor firstPlayer = null!;
        for (int i = 1; i <= playerCount; i++)
        {
            var pForm = i == 1 ? playerForm : TestRosterFactory.CreateBlankMannequin();
            var zOffset = (i - 1) * -0.6f;
            var xOffset = (i - 1) * -1.5f;
            var tint = i == 1 ? Colors.White : GameConstants.StandardPlayerColors[i - 1];

            var p = CombatActorFactory.CreateAndRegister(
                actorRoot,
                simulation,
                $"player_{i}",
                $"P{i} {pForm.DisplayName}",
                pForm,
                new Vector3(xOffset, 0.0f, zOffset),
                teamId: 1,
                playerId: i,
                isPlayer: true,
                isBoss: false,
                presentationTint: tint);
                
            if (i == 1) firstPlayer = p;
            
            if (OS.GetEnvironment("PROJECT_MANNEQUIN_CAMERA_SMOKE_TEST") == "1")
            {
                if (i == 1) 
                {
                    p.SimPosition = new Vector3(mission.StageMinX + 11.5f, 0.0f, 0.6f);
                    p.Position = p.SimPosition;
                }
                else if (i == 2)
                {
                    p.SimPosition = new Vector3(mission.StageMinX - 5.0f, 0.0f, -0.6f);
                    p.Position = p.SimPosition;
                    p.IsVulnerable = false;
                }
            }
        }
        var player = firstPlayer;

        var isolatedPoseTest =
            OS.GetEnvironment("PROJECT_MANNEQUIN_VISUAL_POSE_TEST") == "1";
        var combatSmokeTest = OS.GetEnvironment("PROJECT_MANNEQUIN_COMBAT_SMOKE_TEST") == "1";
        if (isolatedPoseTest)
        {
            player.SimPosition = new Vector3(10.0f, 0.0f, 0.0f);
            player.Position = player.SimPosition;
        }
        else if (combatSmokeTest)
        {
            player.SimPosition = new Vector3(10.0f, 0.0f, 0.0f);
            player.Position = player.SimPosition;

            var dummyForm = TestRosterFactory.CreateTrainingEnemy();
            dummyForm.MaxHealth = 5000;
            var dummy = CombatActorFactory.CreateAndRegister(
                actorRoot,
                simulation,
                "combat_smoke_dummy",
                "Combat Test Dummy",
                dummyForm,
                new Vector3(11.0f, 0.0f, 0.0f),
                teamId: 2,
                playerId: 0,
                isPlayer: false,
                isBoss: false,
                presentationTint: Colors.Salmon);
            dummy.IsAiEnabled = false;
        }
        else if (TrainingDummyController.TryParse(
            OS.GetEnvironment("PROJECT_MANNEQUIN_TRAINING_DUMMY"), out var trainingSetting))
        {
            player.SimPosition = new Vector3(4.0f, 0.0f, 0.0f);
            player.Position = player.SimPosition;

            var dummyForm = TestRosterFactory.CreateBlankMannequin();
            dummyForm.MaxHealth = 5000;
            var dummy = CombatActorFactory.CreateAndRegister(
                actorRoot,
                simulation,
                "training_dummy",
                "Training Dummy",
                dummyForm,
                new Vector3(6.5f, 0.0f, 0.0f),
                teamId: 2,
                playerId: 0,
                isPlayer: false,
                isBoss: false,
                presentationTint: Colors.Salmon);
            dummy.TrainingBrain = new TrainingDummyBrain(trainingSetting);
        }
        else if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_CPU_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_DUEL_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1"
            || splitScreenTest)
        {
            var bossEncounter = mission.Encounters.FindLast(
                encounter => encounter.Kind == StageEncounterKind.Boss);
            player.SimPosition = new Vector3(
                bossEncounter?.CameraLockX ?? mission.StageMaxX - 8.0f,
                0.0f,
                0.0f);
            player.Position = player.SimPosition;
        }
        else if (OS.GetEnvironment("PROJECT_MANNEQUIN_STAGE_SMOKE_TEST") == "1")
        {
            var firstTriggerX = mission.Encounters.Count > 0
                ? mission.Encounters[0].TriggerX
                : mission.StageMinX;
            player.SimPosition = new Vector3(firstTriggerX, 0.0f, 0.0f);
            player.Position = player.SimPosition;
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_CAMERA_SMOKE_TEST") != "1")
        {
            foreach (var p in simulation.Actors)
            {
                if (p.IsPlayerControlled && p.PlayerId > 1)
                {
                    p.SimPosition = new Vector3(
                        player.SimPosition.X - (p.PlayerId - 1) * 1.5f,
                        player.SimPosition.Y,
                        player.SimPosition.Z - (p.PlayerId - 1) * 0.6f);
                    p.Position = p.SimPosition;
                    if (splitScreenTest)
                    {
                        p.IsVulnerable = false;
                    }
                }
            }
        }

        if (!MvpProgressStore.IsPersistenceDisabled)
        {
            ApplySavedProgress(player);
        }

        var runSession = RunSessionManager.Instance;
        if (runSession.HasActiveRun
            && runSession.CurrentWorldId == mission.WorldId
            && !MvpProgressStore.IsPersistenceDisabled)
        {
            runSession.EnsureStageEntryCheckpoint(player);
            runSession.ScoreManager.BeginStage();
        }

        if (!combatSmokeTest && !isolatedPoseTest)
        {
            simulation.SetEncounterDirector(new ArcadeEncounterDirector(simulation, actorRoot, mission));
            if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_INTRO_HUD_SMOKE_TEST") == "1")
            {
                var hud = GetNode<MvpHud>("MvpHud");
                var intro = GetNode<BossIntroSequencer>("BossIntroSequencer");
                var scenario = new BossIntroHudSmokeScenario();
                scenario.Initialize(simulation, hud, intro);
                AddChild(scenario);
            }

            if (resultsFlowSmokeTest)
            {
                var scenario = new ResultsFlowSmokeScenario();
                scenario.Initialize(GetNode<MvpHud>("MvpHud"));
                AddChild(scenario);
            }

            var usesFastHeadlessSmokeRunner = DisplayServer.GetName() == "headless"
                && (OS.GetEnvironment("PROJECT_MANNEQUIN_LADDER_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1");
            if (usesFastHeadlessSmokeRunner)
            {
                var runner = new WorldLadderSmokeRunner();
                runner.Initialize(simulation);
                AddChild(runner);
            }
        }

        if (OS.GetEnvironment("PROJECT_MANNEQUIN_MOVE_LIST_SMOKE_TEST") == "1")
        {
            var scenario = new MoveListSmokeScenario();
            scenario.Initialize(simulation, GetNode<PauseMenu>("PauseMenu"));
            AddChild(scenario);
        }
    }

    private static void ApplySavedProgress(CombatActor player)
    {
        var progress = MvpProgressStore.Load();
        if (progress.UnlockedFormIds.Contains("archive_knight_form"))
        {
            player.FormArchive.UnlockForm(TestRosterFactory.CreateArchiveKnightForm());
        }

        if (progress.UnlockedFormIds.Contains("world_warrior_ryu_form"))
        {
            player.FormArchive.UnlockForm(TestRosterFactory.CreateWorldWarriorRyuForm());
        }

        if (progress.UnlockedFormIds.Contains("goku_archive_form"))
        {
            player.FormArchive.UnlockForm(GokuRosterFactory.CreateGokuArchiveForm());
        }
    }
}
