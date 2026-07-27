using System.Collections.Generic;
using System.Linq;
using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public static class RunScoreTests
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

        log.AppendLine("=== Run Score Tests ===");
        var session = RunSessionManager.Instance;
        session.StartNewRun("archive_nexus");
        session.ScoreManager.RestoreRunScore(24900);
        session.ScoreManager.BeginStage();

        var player = new CombatActor
        {
            ActorId = "score_player",
            IsPlayerControlled = true,
            PlayerId = 1,
            TeamId = 1,
        };
        player.Initialize(TestRosterFactory.CreateBlankMannequin());
        var elite = new CombatActor
        {
            ActorId = "score_elite",
            IsElite = true,
            TeamId = 2,
        };
        elite.Initialize(TestRosterFactory.CreateArchiveScout());
        var pickup = new CombatActor
        {
            ActorId = "score_pickup",
            TeamId = 0,
        };
        pickup.Initialize(HazardRosterFactory.CreateWorldWarriorScorePickup());
        var actors = new List<CombatActor> { player, elite, pickup };

        for (var frame = 0; frame < 120; frame++)
        {
            session.ScoreManager.AdvanceGameplayFrame(ArcadeStageState.StageIntro);
        }
        for (var frame = 0; frame < 300; frame++)
        {
            session.ScoreManager.AdvanceGameplayFrame(ArcadeStageState.EncounterActive);
        }
        Check(session.ScoreManager.ActiveGameplayFrames == 300,
            "timer excludes stage intro and counts gameplay frames");

        var mission = new StageMissionData
        {
            Id = "score_test_stage",
            StageNumber = 1,
            ParTimeSeconds = 10.0f,
            IsFinalStage = false,
            RankScoreS = 3000,
            RankScoreA = 2500,
            RankScoreB = 1800,
            RankScoreC = 1000,
        };
        var events = new List<CombatPresentationEvent>
        {
            new(CombatPresentationEventType.HitConnected, 100, player.ActorId, elite.ActorId, "test_hit|1000"),
            new(CombatPresentationEventType.ComboUpdated, 100, player.ActorId, elite.ActorId, "8"),
            new(CombatPresentationEventType.Parried, 100, player.ActorId, elite.ActorId, "perfect"),
            new(CombatPresentationEventType.ActorDefeated, 100, elite.ActorId),
            new(CombatPresentationEventType.ActorDefeated, 100, pickup.ActorId),
            new(CombatPresentationEventType.StageCompleted, 100, player.ActorId, Payload: mission.Id),
        };
        var output = new List<CombatPresentationEvent>(events);
        session.ScoreManager.CaptureEvents(
            100,
            events,
            actors,
            mission,
            session,
            output);

        var results = session.ScoreManager.LastStageResults;
        Check(results is not null, "stage completion produces immutable results");
        Check(results is { MaxCombo: 8, Parries: 1 },
            "results capture combo and parry telemetry");
        Check(results is { EnemiesDefeated: 1 },
            "pickup collection deaths do not count as enemy defeats or award defeat score");
        Check(results is { TimeBonus: > 0, ClearBonus: 3500 },
            "clear time under par awards deterministic time and clear bonuses");
        Check(results?.Rank == StageRank.S, "rank thresholds resolve S boundary");
        Check(results?.RunTotal == session.ScoreManager.RunScore && results.RunTotal > 24900,
            "stage bonuses accumulate into run total");
        Check(session.RemainingLives == 4 && session.ExtraLifeThresholdIndex == 1,
            "crossing score threshold awards exactly one extra life");
        Check(output.Any(evt => evt.Type == CombatPresentationEventType.StageResultsReady),
            "results-ready presentation event emitted");
        Check(output.Count(evt => evt.Type == CombatPresentationEventType.ExtraLifeAwarded) == 1,
            "extra-life presentation emitted once");

        var livesAfterFirstCapture = session.RemainingLives;
        session.ScoreManager.CaptureEvents(100, events, actors, mission, session, output);
        Check(session.RemainingLives == livesAfterFirstCapture,
            "same tick cannot duplicate score or extra life");

        session.CompleteRun();
        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
