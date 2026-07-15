using System.Text;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;

namespace ProjectMannequin.DebugTools;

public static class RunCheckpointTests
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

        log.AppendLine("=== Run Checkpoint Tests ===");
        var session = RunSessionManager.Instance;
        session.StartNewRun("archive_nexus");
        var actor = new CombatActor
        {
            ActorId = "checkpoint_player",
            IsPlayerControlled = true,
            PlayerId = 1,
            TeamId = 1,
        };
        actor.Initialize(TestRosterFactory.CreateBlankMannequin());
        actor.ApplyHazardDamage(37, tick: 1);
        actor.AddMeter(42);
        session.EquippedMoveCards.Add("test_card");
        session.ActiveArtifacts.Add("test_artifact");
        session.EnsureStageEntryCheckpoint(actor);

        var expectedHealth = actor.Health;
        var expectedMeter = actor.Meter;
        var expectedLives = session.RemainingLives;
        session.EquippedMoveCards.Add("uncommitted_card");
        session.ActiveArtifacts.Add("uncommitted_artifact");
        session.RemainingLives = 1;
        session.RunScore = 9999;
        session.RestoreStageCheckpoint();

        Check(session.CurrentWorldId == "archive_nexus", "restores world ID");
        Check(session.CurrentStageIndex == 0, "restores stage index");
        Check(session.RemainingLives == expectedLives, "restores stage-entry lives");
        Check(session.EquippedMoveCards.Count == 1 && session.EquippedMoveCards[0] == "test_card",
            "rolls back uncommitted move cards");
        Check(session.ActiveArtifacts.Count == 1 && session.ActiveArtifacts[0] == "test_artifact",
            "rolls back uncommitted artifacts");
        Check(session.RunScore == 0, "rolls back uncommitted score");

        actor.HydrateRunResources(1, 0);
        session.ApplyToPlayer(actor);
        Check(actor.Health == expectedHealth, "hydrates committed health");
        Check(actor.Meter == expectedMeter, "hydrates committed meter");

        actor.ApplyHazardDamage(9, tick: 2);
        actor.AddMeter(8);
        var stageTwoHealth = actor.Health;
        var stageTwoMeter = actor.Meter;
        Check(session.AdvanceToNextStage(actor, stageCount: 4), "advances to next stage");
        Check(session.CurrentStageIndex == 1, "commits next stage index");
        session.RestoreStageCheckpoint();
        Check(session.PlayerHealth == stageTwoHealth && session.PlayerMeter == stageTwoMeter,
            "next-stage checkpoint carries health and meter unchanged");
        actor.HydrateRunResources(1, 0);
        session.ApplyToPlayer(actor);
        Check(actor.Health == stageTwoHealth && actor.Meter == stageTwoMeter,
            "next-stage actor hydrates committed resources");

        session.CompleteRun();
        Check(!session.HasActiveRun, "completing run clears active session");
        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
