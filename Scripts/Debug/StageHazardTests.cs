using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
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
        var pushZone = new StageHazardZoneData
        {
            Id = "test_energy_current",
            Behavior = StageHazardBehavior.PushZone,
            Targets = StageHazardTargetMask.All,
            MinX = 10.0f,
            MaxX = 14.0f,
            MinZ = -1.0f,
            MaxZ = 1.0f,
            WarningLeadFrames = 60,
            ActiveFrames = 120,
            RepeatIntervalFrames = 240,
            DamagePerSecond = 0.0f,
            PushSpeedX = 3.0f,
            PushSpeedZ = -1.5f,
        };
        var pushStep = StageHazardRuntime.ResolvePushStep(pushZone);
        Check(System.Math.Abs(pushStep.X * GameConstants.TickRate - 3.0f) < 0.001f
              && System.Math.Abs(pushStep.Z * GameConstants.TickRate + 1.5f) < 0.001f,
            "push zone resolves deterministic per-tick displacement");
        var nonPushStep = StageHazardRuntime.ResolvePushStep(zone);
        Check(nonPushStep == Vector3.Zero,
            "non-push hazards resolve no continuous displacement");

        var archiveStage = WorldRunCatalog.CreateRun("archive_nexus").Stages[0];
        var tram = archiveStage.Encounters
            .SelectMany(encounter => encounter.HazardZones)
            .SingleOrDefault(hazard => hazard.Id == "archive_intake_tram");
        Check(tram is not null, "Archive Stage 1 authors intake tram set piece");
        Check(tram is { Behavior: StageHazardBehavior.LinearSweep, Targets: StageHazardTargetMask.All },
            "intake tram is a neutral moving sweeper");
        Check(tram is not null
              && tram.SpritePath.EndsWith("archive_intake_tram_style_v1.png")
              && ResourceLoader.Exists(tram.SpritePath)
              && System.Math.Abs(tram.SpritePixelSize - 0.00220f) < 0.00001f
              && System.Math.Abs(tram.SpriteGroundOffsetPixels - 419.5f) < 0.01f,
            "intake tram uses its calibrated authored hazard sprite");
        Check(tram is not null
              && tram.MinZ > archiveStage.LaneMinZ
              && tram.MaxZ < archiveStage.LaneMaxZ,
            "intake tram leaves near/far safe lanes");
        Check(StageMissionValidator.Validate(archiveStage).Count == 0,
            "authored stage passes hazard safety validation");
        archiveStage.Encounters[0].HazardZones.Add(pushZone);
        Check(StageMissionValidator.Validate(archiveStage).Count == 0,
            "authored push zone passes mission validation");
        pushZone.PushSpeedX = 0.0f;
        pushZone.PushSpeedZ = 0.0f;
        Check(StageMissionValidator.Validate(archiveStage).Any(error =>
                error.Contains("needs a non-zero push speed")),
            "mission validation rejects zero-force push zones");
        archiveStage.Encounters[0].HazardZones.Remove(pushZone);

        pushZone.PushSpeedX = 120.0f;
        pushZone.PushSpeedZ = 120.0f;
        pushZone.ActivationDelayFrames = 0;
        pushZone.WarningLeadFrames = 30;
        pushZone.ActiveFrames = 120;
        pushZone.RepeatIntervalFrames = 0;
        pushZone.Targets = StageHazardTargetMask.Players;
        archiveStage.StageIntroFrames = 0;
        archiveStage.CameraViewportWidth = archiveStage.StageMaxX - archiveStage.StageMinX;
        archiveStage.PlayerLeftScreenMargin = 0.0f;
        archiveStage.PlayerRightScreenMargin = 0.0f;
        var pushEncounter = archiveStage.Encounters[0];
        pushZone.MinX = pushEncounter.ArenaMinX;
        pushZone.MaxX = pushEncounter.ArenaMaxX;
        pushZone.MinZ = archiveStage.LaneMinZ;
        pushZone.MaxZ = archiveStage.LaneMaxZ;
        pushEncounter.HazardZones.Add(pushZone);

        var pushSimulation = new GameSimulation();
        var pushActorRoot = new Node3D();
        var pushedPlayer = new CombatActor
        {
            ActorId = "push_zone_player",
            IsPlayerControlled = true,
            PlayerId = 1,
            TeamId = 1,
        };
        pushedPlayer.Initialize(TestRosterFactory.CreateBlankMannequin());
        var maskedEnemy = new CombatActor
        {
            ActorId = "push_zone_enemy",
            TeamId = 2,
        };
        maskedEnemy.Initialize(TestRosterFactory.CreateWorldWarriorRookie());
        pushSimulation.RegisterActor(pushedPlayer);
        pushSimulation.RegisterActor(maskedEnemy);
        var pushDirector = new ArcadeEncounterDirector(
            pushSimulation,
            pushActorRoot,
            archiveStage);
        var pushEvents = new List<CombatPresentationEvent>();
        pushedPlayer.SimPosition = new Vector3(
            pushEncounter.TriggerX,
            0.0f,
            0.0f);
        pushDirector.UpdateBeforeSimulation(0, pushEvents);

        pushedPlayer.SimPosition = new Vector3(
            pushEncounter.ArenaMaxX - 0.01f,
            0.0f,
            archiveStage.LaneMaxZ - 0.01f);
        maskedEnemy.SimPosition = new Vector3(
            pushEncounter.ArenaMinX + 1.0f,
            0.0f,
            0.0f);
        var maskedEnemyStart = maskedEnemy.SimPosition;
        pushDirector.UpdateAfterSimulation(30, pushEvents);
        var playerPushPassed =
            System.Math.Abs(pushedPlayer.SimPosition.X - pushEncounter.ArenaMaxX) < 0.001f
            && System.Math.Abs(pushedPlayer.SimPosition.Z - archiveStage.LaneMaxZ) < 0.001f
            && maskedEnemy.SimPosition.IsEqualApprox(maskedEnemyStart);
        Check(playerPushPassed,
            "director applies player-targeted push zones and clamps arena lane bounds");
        if (!playerPushPassed)
        {
            log.Append("    player=").Append(pushedPlayer.SimPosition)
                .Append(" enemy=").Append(maskedEnemy.SimPosition)
                .Append(" enemyStart=").Append(maskedEnemyStart)
                .Append(" arena=").Append(pushEncounter.ArenaMinX)
                .Append("..").Append(pushEncounter.ArenaMaxX)
                .Append(" lane=").Append(pushDirector.CurrentLaneMinZ)
                .Append("..").Append(pushDirector.CurrentLaneMaxZ).Append('\n');
        }

        pushZone.Targets = StageHazardTargetMask.Enemies;
        pushZone.PushSpeedX = -120.0f;
        pushZone.PushSpeedZ = -120.0f;
        pushedPlayer.SimPosition = new Vector3(
            pushEncounter.ArenaMinX + 1.0f,
            0.0f,
            0.0f);
        maskedEnemy.SimPosition = new Vector3(
            pushEncounter.ArenaMinX + 0.01f,
            0.0f,
            archiveStage.LaneMinZ + 0.01f);
        var maskedPlayerStart = pushedPlayer.SimPosition;
        pushDirector.UpdateAfterSimulation(31, pushEvents);
        var enemyPushPassed =
            System.Math.Abs(maskedEnemy.SimPosition.X - pushEncounter.ArenaMinX) < 0.001f
            && System.Math.Abs(maskedEnemy.SimPosition.Z - archiveStage.LaneMinZ) < 0.001f
            && pushedPlayer.SimPosition.IsEqualApprox(maskedPlayerStart);
        Check(enemyPushPassed,
            "director applies enemy-targeted push zones without moving masked players");
        if (!enemyPushPassed)
        {
            log.Append("    enemy=").Append(maskedEnemy.SimPosition)
                .Append(" player=").Append(pushedPlayer.SimPosition)
                .Append(" playerStart=").Append(maskedPlayerStart)
                .Append(" arena=").Append(pushEncounter.ArenaMinX)
                .Append("..").Append(pushEncounter.ArenaMaxX)
                .Append(" lane=").Append(pushDirector.CurrentLaneMinZ)
                .Append("..").Append(pushDirector.CurrentLaneMaxZ).Append('\n');
        }

        pushEncounter.HazardZones.Remove(pushZone);
        foreach (var actor in pushSimulation.Actors.ToArray())
        {
            pushSimulation.UnregisterActor(actor);
            actor.Free();
        }
        pushActorRoot.Free();
        pushSimulation.Free();
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
          var farScan = vaultScans.Single(hazard => hazard.Id.EndsWith("far_scan"));
          var nearScan = vaultScans.Single(hazard => hazard.Id.EndsWith("near_scan"));
          Check(vaultScans.All(hazard =>
                  hazard.SpritePath.EndsWith("archive_index_scan_emitter_style_v1.png")
                  && hazard.FieldTexturePath.EndsWith("archive_index_scan_field_style_v1.png")
                  && ResourceLoader.Exists(hazard.SpritePath)
                  && ResourceLoader.Exists(hazard.FieldTexturePath)
                  && System.Math.Abs(hazard.SpritePixelSize - 0.00090f) < 0.00001f
                  && System.Math.Abs(hazard.SpriteGroundOffsetPixels - 935.0f) < 0.01f)
              && farScan is { SpriteAnchorX: 0.08f, SpriteFlipH: false, FieldFlipH: false }
              && nearScan is { SpriteAnchorX: 0.92f, SpriteFlipH: true, FieldFlipH: true },
            "Index Vault scans use mirrored authored emitters and directional field art");
          var farAtNinety = StageHazardRuntime.Resolve(farScan, 90);
          var nearAtNinety = StageHazardRuntime.Resolve(nearScan, 90);
          var farAtTwoForty = StageHazardRuntime.Resolve(farScan, 240);
          var nearAtTwoForty = StageHazardRuntime.Resolve(nearScan, 240);
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
        Check(fallingStrikes.Take(2).All(hazard =>
                    hazard.SpritePath.EndsWith("archive_repository_falling_shelf_style_v1.png")
                    && ResourceLoader.Exists(hazard.SpritePath)
                    && System.Math.Abs(hazard.SpritePixelSize - 0.00150f) < 0.00001f
                    && System.Math.Abs(hazard.SpriteGroundOffsetPixels - 546.5f) < 0.01f)
              && !fallingStrikes[0].SpriteFlipH
              && fallingStrikes[1].SpriteFlipH
              && fallingStrikes[2].SpritePath.EndsWith(
                  "archive_repository_data_debris_style_v1.png")
              && ResourceLoader.Exists(fallingStrikes[2].SpritePath)
              && System.Math.Abs(fallingStrikes[2].SpritePixelSize - 0.00170f) < 0.00001f
              && System.Math.Abs(fallingStrikes[2].SpriteGroundOffsetPixels - 747.0f) < 0.01f
              && fallingStrikes.All(hazard =>
                  System.Math.Abs(hazard.SpriteTravelHeight - 4.4f) < 0.01f),
            "Repository falling strikes use distinct calibrated shelf and data-debris art");
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
        Check(securitySweep is not null
              && securitySweep.SpritePath.EndsWith(
                  "archive_repository_security_sweep_style_v1.png")
              && ResourceLoader.Exists(securitySweep.SpritePath)
              && System.Math.Abs(securitySweep.SpritePixelSize - 0.00125f) < 0.00001f
                            && System.Math.Abs(securitySweep.SpriteGroundOffsetPixels - 300.5f) < 0.01f
                            && System.Math.Abs(securitySweep.SpriteAnchorX - 0.85f) < 0.01f
                            && securitySweep.SpriteFlipH,
            "repository security sweep uses its calibrated authored emitter");
        var sweepStart = StageHazardRuntime.Resolve(securitySweep!, 84);
        var sweepEnd = StageHazardRuntime.Resolve(securitySweep!, 173);
        Check(sweepStart.IsActive && sweepEnd.IsActive && sweepStart.MinX < sweepEnd.MinX,
            "repository security sweep crosses its chamber deterministically");
        var repositoryProps = archiveStageThree.Encounters
            .SelectMany(encounter => encounter.Props)
            .ToArray();
        var explosive = repositoryProps.Single(prop => prop.ExplodesOnBreak);
        Check(fallingStrikes.All(hazard =>
                    hazard.AftermathVisual is
                    {
                        DecalSizeX: 2.6f,
                        DecalSizeZ: 2.2f,
                        FragmentPixelSize: 0.00068f,
                    }
                    && hazard.AftermathVisual.DecalTexturePath.EndsWith(
                        "archive_repository_explosion_decal_style_v1.png")
                    && hazard.AftermathVisual.FragmentSpritePath.EndsWith(
                        "archive_repository_impact_fragments_style_v1.png")
                    && ResourceLoader.Exists(hazard.AftermathVisual.DecalTexturePath)
                    && ResourceLoader.Exists(hazard.AftermathVisual.FragmentSpritePath))
              && explosive.AftermathVisual is
              {
                  DecalSizeX: 4.0f,
                  DecalSizeZ: 3.4f,
                  FragmentPixelSize: 0.00068f,
              },
            "Repository impacts and explosion author persistent decal/fragment aftermath");
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
        player.ApplyHazardDamage(100, tick: 1);
        var damagedHealth = player.Health;
        var healthPickup = new CombatActor { ActorId = "health_pickup" };
        healthPickup.Initialize(HazardRosterFactory.CreateWorldWarriorHealthPickup());
        player.CollectPickup(healthPickup);
        Check(player.Health == damagedHealth + player.CurrentForm.MaxHealth / 4
              && healthPickup.IsDead,
            "World Warrior health pickup restores exactly 25 percent max health and consumes itself");
        var meterBefore = player.Meter;
        var meterPickup = new CombatActor { ActorId = "meter_pickup" };
        meterPickup.Initialize(HazardRosterFactory.CreateWorldWarriorMeterPickup());
        player.CollectPickup(meterPickup);
        Check(player.Meter == System.Math.Min(
                  meterBefore + 200,
                  player.CurrentForm.MaxMeter)
              && meterPickup.IsDead,
            "World Warrior meter pickup restores 200 meter up to the form cap and consumes itself");

        var dojoApproach = WorldRunCatalog.CreateRun("world_warrior_sector").Stages[0];
        var dojoCrateProp = dojoApproach.Encounters[1].Props
            .Single(prop => prop.ArchetypeId == "world_warrior_supply_crate");
        var supplyCrate = new CombatActor { ActorId = "world_warrior_supply_crate" };
        var supplyCrateForm = HazardRosterFactory.CreateWorldWarriorSupplyCrate();
        supplyCrateForm.MaxHealth = dojoCrateProp.Health;
        supplyCrate.Initialize(supplyCrateForm);
        supplyCrate.ApplyHazardDamage(dojoCrateProp.Health - 10, tick: 2);
        var crateSurvivedPartialBreak = !supplyCrate.IsDead;
        supplyCrate.ApplyHazardDamage(10, tick: 3);
        Check(crateSurvivedPartialBreak
              && supplyCrate.IsDead
              && dojoCrateProp.SpawnsPickupOnBreak
              && dojoCrateProp.DropType == StagePickupType.Meter
              && supplyCrate.CurrentForm.RoleTags.Contains("breakable"),
            "Dojo Approach supply crate survives partial damage then breaks into a meter drop");
        Check(StageMissionValidator.Validate(dojoApproach).Count == 0,
            "Dojo Approach passes hazard prop lane and spawn validation with the supply crate");

        var pavilionCircuit = WorldRunCatalog.CreateRun("world_warrior_sector").Stages[1];
        var pavilionRackChestProp = pavilionCircuit.Encounters[1].Props
            .Single(prop => prop.ArchetypeId == "world_warrior_pavilion_rack_chest");
        var pavilionRackChest = new CombatActor { ActorId = "world_warrior_pavilion_rack_chest" };
        var pavilionRackChestForm = HazardRosterFactory.CreateWorldWarriorPavilionRackChest();
        pavilionRackChestForm.MaxHealth = pavilionRackChestProp.Health;
        pavilionRackChest.Initialize(pavilionRackChestForm);
        pavilionRackChest.ApplyHazardDamage(pavilionRackChestProp.Health - 10, tick: 2);
        var rackChestSurvivedPartialBreak = !pavilionRackChest.IsDead;
        pavilionRackChest.ApplyHazardDamage(10, tick: 3);
        Check(rackChestSurvivedPartialBreak
              && pavilionRackChest.IsDead
              && pavilionRackChestProp.SpawnsPickupOnBreak
              && pavilionRackChestProp.DropType == StagePickupType.Score
              && pavilionRackChest.CurrentForm.RoleTags.Contains("breakable"),
            "Pavilion Circuit rack chest survives partial damage then breaks into a score drop");
        Check(StageMissionValidator.Validate(pavilionCircuit).Count == 0,
            "Pavilion Circuit passes hazard prop lane and spawn validation with the rack chest");

        var grandTournamentFloor = WorldRunCatalog.CreateRun("world_warrior_sector").Stages[2];
        var grandTournamentTrophyPodiumProp = grandTournamentFloor.Encounters[1].Props
            .Single(prop => prop.ArchetypeId
                == "world_warrior_grand_tournament_trophy_podium");
        var grandTournamentTrophyPodium = new CombatActor
        {
            ActorId = "world_warrior_grand_tournament_trophy_podium",
        };
        var grandTournamentTrophyPodiumForm = HazardRosterFactory
            .CreateWorldWarriorGrandTournamentTrophyPodium();
        grandTournamentTrophyPodiumForm.MaxHealth = grandTournamentTrophyPodiumProp.Health;
        grandTournamentTrophyPodium.Initialize(grandTournamentTrophyPodiumForm);
        grandTournamentTrophyPodium.ApplyHazardDamage(
            grandTournamentTrophyPodiumProp.Health - 10, tick: 2);
        var trophyPodiumSurvivedPartialBreak = !grandTournamentTrophyPodium.IsDead;
        grandTournamentTrophyPodium.ApplyHazardDamage(10, tick: 3);
        Check(trophyPodiumSurvivedPartialBreak
              && grandTournamentTrophyPodium.IsDead
              && grandTournamentTrophyPodiumProp.SpawnsPickupOnBreak
              && grandTournamentTrophyPodiumProp.DropType == StagePickupType.Health
              && grandTournamentTrophyPodium.CurrentForm.RoleTags.Contains("breakable"),
            "Grand Tournament trophy podium survives partial damage then breaks into a health drop");
        Check(StageMissionValidator.Validate(grandTournamentFloor).Count == 0,
            "Grand Tournament Floor passes hazard prop lane and spawn validation with the trophy podium");

        var session = RunSessionManager.Instance;
        session.StartNewRun("world_warrior_sector");
        var worldWarriorScorePickup = new CombatActor
        {
            ActorId = "world_warrior_score_pickup",
        };
        worldWarriorScorePickup.Initialize(
            HazardRosterFactory.CreateWorldWarriorScorePickup());
        player.CollectPickup(worldWarriorScorePickup);
        Check(session.ScoreManager.RunScore == 1000
              && worldWarriorScorePickup.IsDead,
            "World Warrior score pickup adds exactly 1000 points and consumes itself");
        session.CompleteRun();

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
