using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;
using ProjectMannequin.Progression;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public static class StageHazardTests
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

        log.AppendLine("=== Stage Hazard Tests ===");
        var zone = new StageHazardZoneData
        {
            Id = "test_sweeper",
            Behavior = StageHazardBehavior.LinearSweep,
            Targets = StageHazardTargetMask.All,
            MinX = 10.0f,
            MaxX = 12.0f,
            MinZ = -0.5f,
            MaxZ = 0.5f,
            ActivationDelayFrames = 20,
            WarningLeadFrames = 60,
            ActiveFrames = 100,
            RepeatIntervalFrames = 220,
            MovementOffsetX = 8.0f,
        };

        Check(StageHazardRuntime.Resolve(zone, 0).Phase == StageHazardPhase.Dormant,
            "activation delay remains dormant");
        var warning = StageHazardRuntime.Resolve(zone, 50);
        Check(warning.IsWarning && warning.MinX == 10.0f,
            "warning phase telegraphs authored start bounds");
        var activeStart = StageHazardRuntime.Resolve(zone, 80);
        var activeEnd = StageHazardRuntime.Resolve(zone, 179);
        Check(activeStart.IsActive && activeEnd.IsActive,
            "active window resolves deterministically");
        Check(activeStart.MinX < activeEnd.MinX && System.Math.Abs(activeEnd.MinX - 18.0f) < 0.01f,
            "linear sweep reaches authored end offset");
        Check(StageHazardRuntime.Resolve(zone, 200).Phase == StageHazardPhase.Cooldown,
            "post-active interval is safe cooldown");
        Check(StageHazardRuntime.Resolve(zone, 240).Phase == StageHazardPhase.Warning,
            "repeat cadence returns to warning phase");
        Check(zone.Targets.HasFlag(StageHazardTargetMask.Players)
              && zone.Targets.HasFlag(StageHazardTargetMask.Enemies),
            "neutral hazard targets both teams");

        var archiveStage = WorldRunCatalog.CreateRun("archive_nexus").Stages[0];
        var tram = archiveStage.Encounters
            .SelectMany(encounter => encounter.HazardZones)
            .SingleOrDefault(hazard => hazard.Id == "archive_intake_tram");
        Check(tram is not null, "Archive Stage 1 authors intake tram set piece");
        Check(tram is { Behavior: StageHazardBehavior.LinearSweep, Targets: StageHazardTargetMask.All },
            "intake tram is a neutral moving sweeper");
        Check(tram is not null
              && tram.MinZ > archiveStage.LaneMinZ
              && tram.MaxZ < archiveStage.LaneMaxZ,
            "intake tram leaves near/far safe lanes");
        Check(StageMissionValidator.Validate(archiveStage).Count == 0,
            "authored stage passes hazard safety validation");
        var authoredProps = archiveStage.Encounters.SelectMany(encounter => encounter.Props).ToArray();
        Check(authoredProps.Any(prop => prop.DropType == StagePickupType.Health)
              && authoredProps.Any(prop => prop.DropType == StagePickupType.Score),
            "Archive Stage 1 authors health and score cache decisions");

        var archiveStageTwo = WorldRunCatalog.CreateRun("archive_nexus").Stages[1];
        var vaultFirst = archiveStageTwo.Encounters[0];
        var vaultSecond = archiveStageTwo.Encounters[1];
        var vaultElite = archiveStageTwo.Encounters[2];
        Check(vaultFirst is { UsesLaneBounds: true, LaneMinZ: -1.65f, LaneMaxZ: 1.65f }
              && vaultSecond is { UsesLaneBounds: true, LaneMinZ: -2.75f, LaneMaxZ: 2.75f }
              && vaultElite is { UsesLaneBounds: true, LaneMinZ: -2.40f, LaneMaxZ: 2.40f },
            "Index Vaults authors narrow wide and elite lane rhythm");
        var laneStart = StageLaneRuntime.Resolve(
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            vaultFirst,
            0);
        var laneMid = StageLaneRuntime.Resolve(
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            vaultFirst,
            15);
        var laneEnd = StageLaneRuntime.Resolve(
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            archiveStageTwo.LaneMinZ,
            archiveStageTwo.LaneMaxZ,
            vaultFirst,
            30);
        Check(laneStart.MinZ == -3.0f
              && laneMid.MinZ > laneStart.MinZ
              && System.Math.Abs(laneEnd.MinZ - -1.65f) < 0.01f,
            "encounter lane funnel interpolates deterministically");

        var vaultScans = vaultSecond.HazardZones
            .Where(hazard => hazard.Id.StartsWith("index_vault_"))
            .ToArray();
        Check(vaultScans.Length == 2
              && vaultScans.All(hazard => hazard.Behavior == StageHazardBehavior.StaticPulse)
              && vaultScans.All(hazard => hazard.Targets == StageHazardTargetMask.All),
            "Index Vaults authors two neutral pulse scans");
        var farAtNinety = StageHazardRuntime.Resolve(vaultScans.Single(hazard => hazard.Id.EndsWith("far_scan")), 90);
        var nearAtNinety = StageHazardRuntime.Resolve(vaultScans.Single(hazard => hazard.Id.EndsWith("near_scan")), 90);
          var farAtTwoForty = StageHazardRuntime.Resolve(vaultScans.Single(hazard => hazard.Id.EndsWith("far_scan")), 240);
          var nearAtTwoForty = StageHazardRuntime.Resolve(vaultScans.Single(hazard => hazard.Id.EndsWith("near_scan")), 240);
        Check(farAtNinety.IsActive && nearAtNinety.Phase == StageHazardPhase.Dormant
              && farAtTwoForty.Phase == StageHazardPhase.Cooldown && nearAtTwoForty.IsActive,
            "Index Vault scans alternate without simultaneous activation");
        Check(vaultScans.Max(hazard => hazard.MinZ) > 0.0f
              && vaultScans.Min(hazard => hazard.MaxZ) < 0.0f
              && vaultScans.All(hazard => hazard.MinZ > vaultSecond.LaneMinZ
                  || hazard.MaxZ < vaultSecond.LaneMaxZ),
            "Index Vault scans preserve a center or opposite-lane response");
        var vaultProps = archiveStageTwo.Encounters.SelectMany(encounter => encounter.Props).ToArray();
        Check(vaultProps.Any(prop => prop.DropType == StagePickupType.Health)
              && vaultProps.Any(prop => prop.DropType == StagePickupType.Meter),
            "Index Vaults authors risky health and meter caches");
        Check(vaultElite.Spawns.Single() is
            {
                EntryEdge: EnemyEntryEdge.FarLane,
                EntryProfile: EnemyEntryProfile.Background,
            },
            "Index Vault Raider captain emerges from background stacks");
        Check(StageMissionValidator.Validate(archiveStageTwo).Count == 0,
            "Index Vaults passes lane hazard prop and spawn validation");

        var archiveStageThree = WorldRunCatalog.CreateRun("archive_nexus").Stages[2];
        var repositoryFirst = archiveStageThree.Encounters[0];
        var repositorySecond = archiveStageThree.Encounters[1];
        var repositoryElite = archiveStageThree.Encounters[2];
        var fallingStrikes = repositoryFirst.HazardZones
            .Where(hazard => hazard.Behavior == StageHazardBehavior.FallingStrike)
            .OrderBy(hazard => hazard.ActivationDelayFrames)
            .ToArray();
        Check(fallingStrikes.Length == 3
              && fallingStrikes.All(hazard => hazard.Targets == StageHazardTargetMask.All),
            "Corruption Repository authors three neutral falling strikes");
        Check(StageHazardRuntime.Resolve(fallingStrikes[0], 70).IsActive
              && StageHazardRuntime.Resolve(fallingStrikes[1], 70).IsWarning
              && StageHazardRuntime.Resolve(fallingStrikes[2], 70).Phase == StageHazardPhase.Dormant
              && StageHazardRuntime.Resolve(fallingStrikes[0], 115).Phase == StageHazardPhase.Cooldown
              && StageHazardRuntime.Resolve(fallingStrikes[1], 115).IsActive
              && StageHazardRuntime.Resolve(fallingStrikes[2], 160).IsActive,
            "falling debris teaches staggered non-overlapping impacts");
        var securitySweep = repositorySecond.HazardZones.SingleOrDefault(hazard =>
            hazard.Id == "repository_security_sweep");
        Check(securitySweep is
            {
                Behavior: StageHazardBehavior.LinearSweep,
                Targets: StageHazardTargetMask.All,
            }, "Corruption Repository remixes a neutral security sweep");
        var sweepStart = StageHazardRuntime.Resolve(securitySweep!, 84);
        var sweepEnd = StageHazardRuntime.Resolve(securitySweep!, 173);
        Check(sweepStart.IsActive && sweepEnd.IsActive && sweepStart.MinX < sweepEnd.MinX,
            "repository security sweep crosses its chamber deterministically");
        var repositoryProps = archiveStageThree.Encounters
            .SelectMany(encounter => encounter.Props)
            .ToArray();
        var explosive = repositoryProps.Single(prop => prop.ExplodesOnBreak);
        Check(repositoryProps.Any(prop => prop.DropType == StagePickupType.Score)
              && repositoryProps.Any(prop => prop.DropType == StagePickupType.Health)
              && repositoryProps.Any(prop => prop.DropType == StagePickupType.Meter),
            "Corruption Repository authors asymmetric score health and meter caches");
        Check(explosive.ArchetypeId == "prop_explosive_canister"
              && explosive.ExplosionTargets == StageHazardTargetMask.All,
            "repository canister is an all-team explosive breakable");
        var explosionOrigin = new Vector3(10.0f, 0.0f, 0.0f);
        Check(StagePropRuntime.CanExplosionAffect(
                  explosive,
                  explosionOrigin,
                  new Vector3(12.0f, 0.0f, 0.0f),
                  targetIsPlayer: true)
              && StagePropRuntime.CanExplosionAffect(
                  explosive,
                  explosionOrigin,
                  new Vector3(8.0f, 0.0f, 0.0f),
                  targetIsPlayer: false)
              && !StagePropRuntime.CanExplosionAffect(
                  explosive,
                  explosionOrigin,
                  new Vector3(14.0f, 0.0f, 0.0f),
                  targetIsPlayer: false),
            "explosion mask and radius target both teams without leaking outward");
        var explosionPush = StagePropRuntime.ResolveExplosionKnockback(
            explosionOrigin,
            new Vector3(8.0f, 0.0f, 1.0f),
            explosive.ExplosionKnockback);
        Check(explosionPush.X < 0.0f && explosionPush.Z > 0.0f
              && System.Math.Abs(explosionPush.Length() - explosive.ExplosionKnockback) < 0.01f,
            "explosive canister pushes targets radially outward");
        Check(repositoryElite.Spawns.Single() is
            {
                EntryProfile: EnemyEntryProfile.DropIn,
                EntryHeight: 5.4f,
            }, "Corruption Repository Bruiser overseer drops into the elite arena");
        Check(StageMissionValidator.Validate(archiveStageThree).Count == 0,
            "Corruption Repository passes hazard prop lane and spawn validation");

        var player = new CombatActor
        {
            ActorId = "pickup_test_player",
            IsPlayerControlled = true,
            PlayerId = 1,
            TeamId = 1,
        };
        player.Initialize(TestRosterFactory.CreateBlankMannequin());
        player.ApplyHazardDamage(40, tick: 1);
        var damagedHealth = player.Health;
        var healthPickup = new CombatActor { ActorId = "health_pickup" };
        healthPickup.Initialize(HazardRosterFactory.CreateHealthPickup());
        player.CollectPickup(healthPickup);
        Check(player.Health > damagedHealth && healthPickup.IsDead,
            "health pickup restores player and consumes itself");

        var session = RunSessionManager.Instance;
        session.StartNewRun("archive_nexus");
        var scorePickup = new CombatActor { ActorId = "score_pickup" };
        scorePickup.Initialize(HazardRosterFactory.CreateScorePickup());
        player.CollectPickup(scorePickup);
        Check(session.ScoreManager.RunScore == 1000 && scorePickup.IsDead,
            "score pickup adds deterministic archive points");
        session.CompleteRun();

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
