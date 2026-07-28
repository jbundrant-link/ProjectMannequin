using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Godot;
using ProjectMannequin.Data;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

public static class WorldRunTests
{
    public static string Run()
    {
        var log = new StringBuilder();
        var passed = 0;
        var failed = 0;

        void Check(bool condition, string label, string detail = "")
        {
            if (condition)
            {
                passed++;
                log.Append("  PASS ").Append(label).Append('\n');
            }
            else
            {
                failed++;
                log.Append("  FAIL ").Append(label);
                if (!string.IsNullOrWhiteSpace(detail))
                {
                    log.Append(" — ").Append(detail);
                }
                log.Append('\n');
            }
        }

        log.AppendLine("=== World Run Tests ===");
        foreach (var worldId in new[] { "archive_nexus", "world_warrior_sector", "astral_battlefront" })
        {
            var legacyBefore = MvpWorldCatalog.CreateMission(worldId);
            var originalEncounterIds = legacyBefore.Encounters.Select(encounter => encounter.Id).ToArray();
            var run = WorldRunCatalog.CreateRun(worldId);

            Check(run.Stages.Count == 4, $"{worldId} has exactly four stages", $"count={run.Stages.Count}");
            var stageIds = new HashSet<string>();
            for (var stageIndex = 0; stageIndex < run.Stages.Count; stageIndex++)
            {
                var stage = run.Stages[stageIndex];
                var expectedNumber = stageIndex + 1;
                Check(stage.StageNumber == expectedNumber,
                    $"{worldId} stage {expectedNumber} number metadata");
                Check(stageIds.Add(stage.Id), $"{worldId} stage {expectedNumber} unique ID");
                Check(stage.Encounters.All(encounter =>
                        encounter.TriggerX >= stage.StageMinX
                        && encounter.TriggerX <= stage.StageMaxX
                        && encounter.ArenaMinX >= stage.StageMinX
                        && encounter.ArenaMaxX <= stage.StageMaxX),
                    $"{worldId} stage {expectedNumber} normalized encounter bounds");
                var missionErrors = StageMissionValidator.Validate(stage);
                Check(missionErrors.Count == 0,
                    $"{worldId} stage {expectedNumber} mission validation",
                    string.Join("; ", missionErrors));

                if (worldId is "archive_nexus" or "world_warrior_sector"
                    && stage.PresentationMode == StagePresentationMode.LegacyLayered)
                {
                    Check(FloorMaterialIsIsotropic(stage.FloorTexturePath, 0.55f),
                        $"{worldId} stage {expectedNumber} floor material survives the stage camera",
                        stage.FloorTexturePath);
                    // Stage art is authored against this key, so it has to be a
                    // usable direction rather than left at whatever a default
                    // happened to be. A key at or above the horizon would light
                    // the fighters from below.
                    Check(stage.KeyLightPitchDegrees is > -85.0f and < -10.0f
                          && stage.KeyLightYawDegrees is >= -180.0f and <= 180.0f,
                        $"{worldId} stage {expectedNumber} declares a usable key light direction");

                    // Far-layer haze must begin strictly BEHIND the deepest
                    // point a fighter can stand on. If it ever started closer,
                    // a player walking to the back lane would fade into the
                    // backdrop mid-fight.
                    var haze = ProjectMannequin.Stage.StageGroundProjection
                        .ResolveFarHazeRange(
                            stage,
                            ProjectMannequin.Presentation.PrototypeStageView
                                .FarHazeLaneClearance,
                            ProjectMannequin.Presentation.PrototypeStageView
                                .FarHazeSpan);
                    var deepestFighter = ProjectMannequin.Stage
                        .StageGroundProjection
                        .ResolveRestingCameraProfile(stage).Depth
                        - stage.LaneMinZ;
                    Check(haze.Begin > deepestFighter,
                        $"{worldId} stage {expectedNumber} haze starts behind the deepest fighter");
                    Check(haze.End > haze.Begin,
                        $"{worldId} stage {expectedNumber} haze ramps over a positive depth span");
                    var farPanel = stage.BackgroundPanels.Single(panel =>
                        panel.Layer == StageVisualLayerKind.Far);
                    var midgroundPanel = stage.BackgroundPanels.Single(panel =>
                        panel.Layer == StageVisualLayerKind.Midground);
                    var foregroundPanels = stage.BackgroundPanels.Where(panel =>
                        panel.Layer == StageVisualLayerKind.Foreground);
                    Check(farPanel.AlignBottomToFloor
                          && farPanel.RepeatHorizontally
                          && farPanel.ScaleYMultiplier <= 0.40f
                          && midgroundPanel.AlignBottomToFloor
                          && midgroundPanel.RepeatHorizontally
                          && midgroundPanel.CropTopFraction
                              + midgroundPanel.CropBottomFraction > 0.0f
                          && foregroundPanels.All(panel =>
                              !panel.AlignBottomToFloor
                              && !panel.RepeatHorizontally),
                        $"{worldId} stage {expectedNumber} uses grounded aspect-safe layers");
                }

                if (stageIndex < 3)
                {
                    Check(!stage.IsFinalStage && !stage.RequiresFormSwapToComplete,
                        $"{worldId} stage {expectedNumber} is non-final");
                    Check(string.IsNullOrWhiteSpace(stage.BossFormId),
                        $"{worldId} stage {expectedNumber} grants no form");
                    Check(stage.Encounters.Count == 3
                          && stage.Encounters[0].Kind == StageEncounterKind.Horde
                          && stage.Encounters[1].Kind == StageEncounterKind.Horde
                          && stage.Encounters[2].Kind == StageEncounterKind.Elite,
                        $"{worldId} stage {expectedNumber} uses horde/horde/elite rhythm");
                }
                else
                {
                    Check(stage.IsFinalStage && stage.RequiresFormSwapToComplete,
                        $"{worldId} final stage requires inheritance");
                    Check(!string.IsNullOrWhiteSpace(stage.BossFormId),
                        $"{worldId} final stage owns form reward");
                    Check(stage.Encounters.Count == 1
                          && stage.Encounters[0].Kind == StageEncounterKind.Boss,
                        $"{worldId} final stage owns one major boss");
                }
            }

            var legacyAfter = MvpWorldCatalog.CreateMission(worldId);
            Check(originalEncounterIds.SequenceEqual(legacyAfter.Encounters.Select(encounter => encounter.Id)),
                $"{worldId} run derivation does not mutate legacy mission");

            if (worldId == "archive_nexus")
            {
                var stageArtPaths = run.Stages
                    .Select(stage => stage.StageTexturePath)
                    .ToArray();
                var floorArtPaths = run.Stages
                    .Select(stage => stage.FloorTexturePath)
                    .ToArray();
                Check(stageArtPaths.Distinct().Count() == run.Stages.Count
                        && stageArtPaths.All(path => ResourceLoader.Exists(path)),
                    "Archive stages use four existing bespoke backdrops");
                Check(HaveDistinctContent(stageArtPaths),
                    "Archive stage backdrops have distinct content hashes");
                Check(floorArtPaths.Distinct().Count() == run.Stages.Count
                        && floorArtPaths.All(path => ResourceLoader.Exists(path)),
                    "Archive stages use four existing bespoke floors");
                Check(HaveDistinctContent(floorArtPaths),
                    "Archive stage floors have distinct content hashes");

                var indexVaults = run.Stages[1];
                var indexVaultLayerPaths = indexVaults.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(indexVaults.StageTexturePath.EndsWith(
                          "archive_index_vaults_backdrop_style_v2.png")
                      && indexVaults.FloorTexturePath.EndsWith(
                          "archive_index_vaults_floor_style_v2.png")
                                            && indexVaultLayerPaths.Length == 4
                      && indexVaultLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(indexVaultLayerPaths),
                                        "Index Vaults uses independent style-approved production layers");
                var indexVaultFar = indexVaults.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var indexVaultMidground = indexVaults.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var indexVaultForeground = indexVaults.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(indexVaultFar.ParallaxFactorX < indexVaultMidground.ParallaxFactorX
                      && indexVaultForeground.Length == 2
                      && indexVaultForeground.All(panel =>
                          indexVaultMidground.ParallaxFactorX < panel.ParallaxFactorX)
                      && indexVaultFar.PositionZ < indexVaultMidground.PositionZ
                      && indexVaultForeground.All(panel =>
                          indexVaultMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 5.5f),
                    "Index Vaults production layers preserve ordered depth and restrained foreground opacity");

                var intakeBoulevard = run.Stages[0];
                var intakeLayerPaths = intakeBoulevard.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(intakeBoulevard.StageTexturePath.EndsWith(
                          "archive_intake_boulevard_backdrop_style_v1.png")
                      && intakeBoulevard.FloorTexturePath.EndsWith(
                          "archive_intake_boulevard_floor_style_v1.png")
                      && intakeLayerPaths.Length == 4
                      && intakeLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(intakeLayerPaths),
                    "Intake Boulevard uses independent style-approved production layers");
                var intakeFar = intakeBoulevard.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var intakeMidground = intakeBoulevard.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var intakeForeground = intakeBoulevard.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(intakeFar.ParallaxFactorX < intakeMidground.ParallaxFactorX
                      && intakeForeground.Length == 2
                      && intakeForeground.All(panel =>
                          intakeMidground.ParallaxFactorX < panel.ParallaxFactorX
                          && intakeMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 5.0f),
                    "Intake Boulevard production layers preserve ordered depth and edge-bound foreground");

                var corruptionRepository = run.Stages[2];
                var repositoryLayerPaths = corruptionRepository.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(corruptionRepository.StageTexturePath.EndsWith(
                          "archive_corruption_repository_backdrop_style_v1.png")
                      && corruptionRepository.FloorTexturePath.EndsWith(
                          "archive_corruption_repository_floor_style_v1.png")
                      && repositoryLayerPaths.Length == 4
                      && repositoryLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(repositoryLayerPaths),
                    "Corruption Repository uses independent style-approved production layers");
                var repositoryFar = corruptionRepository.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var repositoryMidground = corruptionRepository.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var repositoryForeground = corruptionRepository.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(repositoryFar.ParallaxFactorX < repositoryMidground.ParallaxFactorX
                      && repositoryForeground.Length == 2
                      && repositoryForeground.All(panel =>
                          repositoryMidground.ParallaxFactorX < panel.ParallaxFactorX
                          && repositoryMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 5.0f),
                    "Corruption Repository production layers preserve ordered depth and edge-bound foreground");

                var knightsReliquary = run.Stages[3];
                var reliquaryLayerPaths = knightsReliquary.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(knightsReliquary.StageTexturePath.EndsWith(
                          "archive_knights_reliquary_backdrop_style_v1.png")
                      && knightsReliquary.FloorTexturePath.EndsWith(
                          "archive_knights_reliquary_floor_style_v1.png")
                      && reliquaryLayerPaths.Length == 4
                      && reliquaryLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(reliquaryLayerPaths),
                    "Knight's Reliquary uses independent style-approved production layers");
                var reliquaryFar = knightsReliquary.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var reliquaryMidground = knightsReliquary.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var reliquaryForeground = knightsReliquary.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(reliquaryFar.ParallaxFactorX < reliquaryMidground.ParallaxFactorX
                      && reliquaryForeground.Length == 2
                      && reliquaryForeground.All(panel =>
                          reliquaryMidground.ParallaxFactorX < panel.ParallaxFactorX
                          && reliquaryMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 4.8f),
                    "Knight's Reliquary production layers preserve ordered depth and edge-bound foreground");
                Check(reliquaryMidground.DestructionTexturePaths.Count == 2
                      && reliquaryMidground.DestructionTexturePaths.All(
                          path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(
                          reliquaryMidground.DestructionTexturePaths),
                    "Knight's Reliquary authors two progressive persistent destruction states");
                Check(reliquaryMidground.DestructionBurstTexturePaths.Count == 2
                      && reliquaryMidground.DestructionBurstTexturePaths.All(
                          path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(
                          reliquaryMidground.DestructionBurstTexturePaths)
                      && reliquaryMidground.DestructionBurstAnchorXs.SequenceEqual(
                          new[] { 0.22f, 0.78f })
                      && System.Math.Abs(
                          reliquaryMidground.DestructionBurstPixelSize - 0.0040f)
                          < 0.00001f
                      && System.Math.Abs(
                          reliquaryMidground.DestructionOverlayPixelSize - 0.0055f)
                          < 0.00001f,
                    "Knight's Reliquary authors distinct localized phase bursts");

                var propSpritePaths = run.Stages
                    .SelectMany(stage => stage.Encounters)
                    .SelectMany(encounter => encounter.Props)
                    .Select(prop => prop.SpritePath)
                    .Distinct()
                    .ToArray();
                Check(propSpritePaths.Length == 4
                        && propSpritePaths.All(path => ResourceLoader.Exists(path)),
                    "Archive caches and canister use four existing authored sprites");
                Check(HaveDistinctContent(propSpritePaths),
                    "Archive caches and canister have distinct content hashes");
                var healthCacheProps = run.Stages
                    .SelectMany(stage => stage.Encounters)
                    .SelectMany(encounter => encounter.Props)
                    .Where(prop => prop.SpawnsPickupOnBreak
                        && prop.DropType == StagePickupType.Health)
                    .ToArray();
                Check(healthCacheProps.Length > 0
                      && healthCacheProps.All(prop =>
                          prop.SpritePath.EndsWith("archive_health_cache_style_v2.png")
                          && System.Math.Abs(prop.SpritePixelSize - 0.00114f) < 0.00001f
                          && System.Math.Abs(prop.SpriteGroundOffsetPixels - 788.0f) < 0.01f),
                    "Archive health caches use the calibrated style-approved runtime asset");
                var meterCacheProps = run.Stages
                    .SelectMany(stage => stage.Encounters)
                    .SelectMany(encounter => encounter.Props)
                    .Where(prop => prop.SpawnsPickupOnBreak
                        && prop.DropType == StagePickupType.Meter)
                    .ToArray();
                Check(meterCacheProps.Length > 0
                      && meterCacheProps.All(prop =>
                          prop.SpritePath.EndsWith("archive_meter_cache_style_v2.png")
                          && System.Math.Abs(prop.SpritePixelSize - 0.00103f) < 0.00001f
                          && System.Math.Abs(prop.SpriteGroundOffsetPixels - 788.0f) < 0.01f),
                    "Archive meter caches use the calibrated style-approved runtime asset");
                var dataCacheProps = run.Stages
                    .SelectMany(stage => stage.Encounters)
                    .SelectMany(encounter => encounter.Props)
                    .Where(prop => prop.SpawnsPickupOnBreak
                        && prop.DropType == StagePickupType.Score)
                    .ToArray();
                Check(dataCacheProps.Length > 0
                      && dataCacheProps.All(prop =>
                          prop.SpritePath.EndsWith("archive_data_cache_style_v2.png")
                          && System.Math.Abs(prop.SpritePixelSize - 0.00136f) < 0.00001f
                          && System.Math.Abs(prop.SpriteGroundOffsetPixels - 674.0f) < 0.01f),
                    "Archive data caches use the calibrated style-approved runtime asset");
                var volatileCanister = run.Stages
                    .SelectMany(stage => stage.Encounters)
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.ArchetypeId == "prop_explosive_canister");
                Check(volatileCanister.SpritePath.EndsWith(
                          "archive_volatile_canister_style_v2.png")
                      && System.Math.Abs(volatileCanister.SpritePixelSize - 0.00143f) < 0.00001f
                      && System.Math.Abs(volatileCanister.SpriteGroundOffsetPixels - 854.0f) < 0.01f,
                    "Archive volatile canister uses the calibrated style-approved runtime asset");

                var healthPickup = HazardRosterFactory.CreateHealthPickup();
                var meterPickup = HazardRosterFactory.CreateMeterPickup();
                var dataPickup = HazardRosterFactory.CreateScorePickup();
                var pickupSpritePaths = new[]
                {
                    healthPickup.SpriteSheetPath,
                    meterPickup.SpriteSheetPath,
                    dataPickup.SpriteSheetPath,
                };
                Check(pickupSpritePaths.Distinct().Count() == pickupSpritePaths.Length
                        && pickupSpritePaths.All(path => ResourceLoader.Exists(path)),
                    "Archive pickup types use three existing independent sprites");
                Check(HaveDistinctContent(pickupSpritePaths),
                    "Archive pickup types have distinct content hashes");
                Check(healthPickup.SpriteSheetPath.EndsWith("archive_health_pickup_style_v2.png")
                      && System.Math.Abs(healthPickup.SpritePixelSize - 0.00038f) < 0.00001f
                      && System.Math.Abs(healthPickup.SpriteGroundOffsetPixels - 672.0f) < 0.01f,
                    "Archive health pickup uses the calibrated style-approved runtime asset");
                Check(meterPickup.SpriteSheetPath.EndsWith("archive_meter_pickup_style_v2.png")
                      && System.Math.Abs(meterPickup.SpritePixelSize - 0.00028f) < 0.00001f
                      && System.Math.Abs(meterPickup.SpriteGroundOffsetPixels - 876.0f) < 0.01f,
                    "Archive meter pickup uses the calibrated style-approved runtime asset");
                Check(dataPickup.SpriteSheetPath.EndsWith("archive_data_pickup_style_v2.png")
                      && System.Math.Abs(dataPickup.SpritePixelSize - 0.00036f) < 0.00001f
                      && System.Math.Abs(dataPickup.SpriteGroundOffsetPixels - 698.0f) < 0.01f,
                    "Archive data pickup uses the calibrated style-approved runtime asset");
                var raider = TestRosterFactory.CreateArchiveRaider();
                var raiderTexture = ResourceLoader.Exists(raider.SpriteSheetPath)
                    ? GD.Load<Texture2D>(raider.SpriteSheetPath)
                    : null;
                Check(raider.SpriteSheetPath.EndsWith("archive_raider_style_v3.png")
                      && raiderTexture is not null
                      && raiderTexture.GetWidth() == 2560
                      && raiderTexture.GetHeight() == 2304
                      && raider.SpriteSheetColumns == 10
                      && raider.SpriteSheetRows == 9
                      && System.Math.Abs(raider.SpritePixelSize - 0.018f) < 0.00001f
                      && System.Math.Abs(raider.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !raider.TintSpriteSheet,
                    "Archive Raider uses the identity-locked walk-repaired 10x9 atlas");
                var raiderAttack = raider.FindMove("archive_raider_attack");
                Check(raiderAttack is not null
                      && raiderAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && raiderAttack.AnimationFrameDurations.Count == 10
                      && raiderAttack.AnimationFrameDurations.Take(3).Sum()
                          == raiderAttack.StartupFrames
                      && raiderAttack.AnimationFrameDurations.Skip(3).Take(2).Sum()
                          == raiderAttack.ActiveFrames
                      && raiderAttack.AnimationFrameDurations.Skip(5).Sum()
                          == raiderAttack.RecoveryFrames,
                    "Archive Raider Cross animation timing matches startup active and recovery phases");
                var scout = TestRosterFactory.CreateArchiveScout();
                var scoutTexture = ResourceLoader.Exists(scout.SpriteSheetPath)
                    ? GD.Load<Texture2D>(scout.SpriteSheetPath)
                    : null;
                Check(scout.SpriteSheetPath.EndsWith("archive_scout_style_v2.png")
                      && scoutTexture is not null
                      && scoutTexture.GetWidth() == 2560
                      && scoutTexture.GetHeight() == 2304
                      && scout.SpriteSheetColumns == 10
                      && scout.SpriteSheetRows == 9
                      && System.Math.Abs(scout.SpritePixelSize - 0.0166f) < 0.00001f
                      && System.Math.Abs(scout.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !scout.TintSpriteSheet,
                    "Archive Scout uses the lightweight identity-locked 10x9 style-v2 atlas");
                var scoutAttack = scout.FindMove("archive_scout_attack");
                Check(scoutAttack is not null
                      && scoutAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && scoutAttack.AnimationFrameDurations.Count == 10
                      && scoutAttack.AnimationFrameDurations.Take(4).Sum()
                          == scoutAttack.StartupFrames
                      && scoutAttack.AnimationFrameDurations.Skip(4).Take(1).Sum()
                          == scoutAttack.ActiveFrames
                      && scoutAttack.AnimationFrameDurations.Skip(5).Sum()
                          == scoutAttack.RecoveryFrames,
                    "Archive Scout Jab animation timing matches startup active and recovery phases");
                var stageOneElite = run.Stages[0].Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageOneElite.DisplayName == "Index Warden Veyra"
                      && stageOneElite.Spawns.Single().ArchetypeId
                          == "index_warden_veyra"
                      && System.Math.Abs(stageOneElite.LightColorR - 0.72f) < 0.001f
                      && System.Math.Abs(stageOneElite.LightColorG - 0.92f) < 0.001f
                      && System.Math.Abs(stageOneElite.LightColorB - 1.0f) < 0.001f,
                    "Archive Stage 1 elite selects Veyra and her cyan-white arena light");
                var veyra = TestRosterFactory.CreateIndexWardenVeyra();
                var veyraTexture = ResourceLoader.Exists(veyra.SpriteSheetPath)
                    ? GD.Load<Texture2D>(veyra.SpriteSheetPath)
                    : null;
                Check(veyra.Id == "index_warden_veyra"
                      && veyra.DisplayName == "Index Warden Veyra"
                      && veyra.SpriteSheetPath.EndsWith(
                          "index_warden_veyra_style_v1.png")
                      && veyraTexture is not null
                      && veyraTexture.GetWidth() == 2560
                      && veyraTexture.GetHeight() == 2304
                      && veyra.SpriteSheetColumns == 10
                      && veyra.SpriteSheetRows == 9
                      && System.Math.Abs(veyra.SpritePixelSize - 0.0190f) < 0.00001f
                      && System.Math.Abs(veyra.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !veyra.TintSpriteSheet
                      && veyra.RoleTags.Contains("named_elite"),
                    "Index Warden Veyra uses a unique untinted animation-ready atlas");
                var veyraAttack = veyra.FindMove("index_warden_veyra_attack");
                Check(veyraAttack is not null
                      && veyraAttack.DisplayName == "Warden Decree"
                      && veyraAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && veyraAttack.AnimationFrameDurations.SequenceEqual(
                          new[] { 3, 3, 3, 3, 5, 4, 4, 4, 4, 8 })
                      && veyraAttack.AnimationFrameDurations.Take(4).Sum()
                          == veyraAttack.StartupFrames
                      && veyraAttack.AnimationFrameDurations.Skip(4).Take(1).Sum()
                          == veyraAttack.ActiveFrames
                      && veyraAttack.AnimationFrameDurations.Skip(5).Sum()
                          == veyraAttack.RecoveryFrames,
                    "Veyra Warden Decree preserves Scout combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        scout.SpriteSheetPath,
                        veyra.SpriteSheetPath,
                    }),
                    "Veyra atlas content is distinct from the base Scout");
                var stageTwoElite = run.Stages[1].Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageTwoElite.DisplayName == "Cipher Captain Rhune"
                      && stageTwoElite.Spawns.Single().ArchetypeId
                          == "cipher_captain_rhune"
                      && System.Math.Abs(stageTwoElite.LightColorR - 0.86f) < 0.001f
                      && System.Math.Abs(stageTwoElite.LightColorG - 0.92f) < 0.001f
                      && System.Math.Abs(stageTwoElite.LightColorB - 1.0f) < 0.001f
                      && System.Math.Abs(stageTwoElite.LightEnergyMultiplier - 1.10f)
                          < 0.001f,
                    "Archive Stage 2 elite selects Rhune and his cool-white arena light");
                var rhune = TestRosterFactory.CreateCipherCaptainRhune();
                var rhuneTexture = ResourceLoader.Exists(rhune.SpriteSheetPath)
                    ? GD.Load<Texture2D>(rhune.SpriteSheetPath)
                    : null;
                Check(rhune.Id == "cipher_captain_rhune"
                      && rhune.DisplayName == "Cipher Captain Rhune"
                      && rhune.SpriteSheetPath.EndsWith(
                          "cipher_captain_rhune_style_v1.png")
                      && rhuneTexture is not null
                      && rhuneTexture.GetWidth() == 2560
                      && rhuneTexture.GetHeight() == 2304
                      && rhune.SpriteSheetColumns == 10
                      && rhune.SpriteSheetRows == 9
                      && System.Math.Abs(rhune.SpritePixelSize - 0.0190f) < 0.00001f
                      && System.Math.Abs(rhune.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !rhune.TintSpriteSheet
                      && rhune.RoleTags.Contains("named_elite"),
                    "Cipher Captain Rhune uses a unique untinted animation-ready atlas");
                var rhuneAttack = rhune.FindMove("cipher_captain_rhune_attack");
                Check(rhuneAttack is not null
                      && rhuneAttack.DisplayName == "Cipher Cross"
                      && rhuneAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && rhuneAttack.AnimationFrameDurations.SequenceEqual(
                          new[] { 5, 5, 5, 2, 3, 4, 5, 5, 6, 8 })
                      && rhuneAttack.AnimationFrameDurations.Take(3).Sum()
                          == rhuneAttack.StartupFrames
                      && rhuneAttack.AnimationFrameDurations.Skip(3).Take(2).Sum()
                          == rhuneAttack.ActiveFrames
                      && rhuneAttack.AnimationFrameDurations.Skip(5).Sum()
                          == rhuneAttack.RecoveryFrames,
                    "Rhune Cipher Cross preserves Raider combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        raider.SpriteSheetPath,
                        rhune.SpriteSheetPath,
                    }),
                    "Rhune atlas content is distinct from the base Raider");
                var bruiser = TestRosterFactory.CreateArchiveBruiser();
                var bruiserTexture = ResourceLoader.Exists(bruiser.SpriteSheetPath)
                    ? GD.Load<Texture2D>(bruiser.SpriteSheetPath)
                    : null;
                Check(bruiser.SpriteSheetPath.EndsWith("archive_bruiser_style_v3.png")
                      && bruiserTexture is not null
                      && bruiserTexture.GetWidth() == 2560
                      && bruiserTexture.GetHeight() == 2304
                      && bruiser.SpriteSheetColumns == 10
                      && bruiser.SpriteSheetRows == 9
                      && System.Math.Abs(bruiser.SpritePixelSize - 0.0205f) < 0.00001f
                      && System.Math.Abs(bruiser.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !bruiser.TintSpriteSheet,
                    "Archive Bruiser uses the heavyweight walk-repaired 10x9 atlas");
                var bruiserAttack = bruiser.FindMove("archive_bruiser_attack");
                Check(bruiserAttack is not null
                      && bruiserAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && bruiserAttack.AnimationFrameDurations.Count == 10
                      && bruiserAttack.AnimationFrameDurations.Take(4).Sum()
                          == bruiserAttack.StartupFrames
                      && bruiserAttack.AnimationFrameDurations.Skip(4).Take(1).Sum()
                          == bruiserAttack.ActiveFrames
                      && bruiserAttack.AnimationFrameDurations.Skip(5).Sum()
                          == bruiserAttack.RecoveryFrames,
                    "Archive Bruiser Hammer animation timing matches startup active and recovery phases");
                var stageThreeElite = run.Stages[2].Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageThreeElite.DisplayName == "Overseer Basalt"
                      && stageThreeElite.Spawns.Single().ArchetypeId
                          == "overseer_basalt"
                      && System.Math.Abs(stageThreeElite.LightColorR - 0.92f) < 0.001f
                      && System.Math.Abs(stageThreeElite.LightColorG - 0.80f) < 0.001f
                      && System.Math.Abs(stageThreeElite.LightColorB - 1.0f) < 0.001f
                      && System.Math.Abs(stageThreeElite.LightEnergyMultiplier - 1.12f)
                          < 0.001f,
                    "Archive Stage 3 elite selects Basalt and his violet-white arena light");
                var basalt = TestRosterFactory.CreateOverseerBasalt();
                var basaltTexture = ResourceLoader.Exists(basalt.SpriteSheetPath)
                    ? GD.Load<Texture2D>(basalt.SpriteSheetPath)
                    : null;
                Check(basalt.Id == "overseer_basalt"
                      && basalt.DisplayName == "Overseer Basalt"
                      && basalt.SpriteSheetPath.EndsWith(
                          "overseer_basalt_style_v2.png")
                      && basaltTexture is not null
                      && basaltTexture.GetWidth() == 2560
                      && basaltTexture.GetHeight() == 2304
                      && basalt.SpriteSheetColumns == 10
                      && basalt.SpriteSheetRows == 9
                      && System.Math.Abs(basalt.SpritePixelSize - 0.0205f) < 0.00001f
                      && System.Math.Abs(basalt.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !basalt.TintSpriteSheet
                      && basalt.RoleTags.Contains("named_elite"),
                    "Overseer Basalt uses a unique untinted animation-ready atlas");
                var basaltAttack = basalt.FindMove("overseer_basalt_attack");
                Check(basaltAttack is not null
                      && basaltAttack.DisplayName == "Faultline Driver"
                      && basaltAttack.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && basaltAttack.AnimationFrameDurations.SequenceEqual(
                          new[] { 5, 5, 5, 5, 5, 5, 6, 7, 8, 8 })
                      && basaltAttack.AnimationFrameDurations.Take(4).Sum()
                          == basaltAttack.StartupFrames
                      && basaltAttack.AnimationFrameDurations.Skip(4).Take(1).Sum()
                          == basaltAttack.ActiveFrames
                      && basaltAttack.AnimationFrameDurations.Skip(5).Sum()
                          == basaltAttack.RecoveryFrames,
                    "Basalt Faultline Driver preserves Bruiser combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        bruiser.SpriteSheetPath,
                        basalt.SpriteSheetPath,
                    }),
                    "Basalt atlas content is distinct from the base Bruiser");
                var archiveKnight = TestRosterFactory.CreateTestBoss();
                var archiveKnightForm = TestRosterFactory.CreateArchiveKnightForm();
                var archiveKnightTexture = ResourceLoader.Exists(
                        archiveKnight.SpriteSheetPath)
                    ? GD.Load<Texture2D>(archiveKnight.SpriteSheetPath)
                    : null;
                Check(archiveKnight.SpriteSheetPath.EndsWith(
                          "archive_knight_style_v2.png")
                      && archiveKnightTexture is not null
                      && archiveKnightTexture.GetWidth() == 2560
                      && archiveKnightTexture.GetHeight() == 2304
                      && archiveKnight.SpriteSheetColumns == 10
                      && archiveKnight.SpriteSheetRows == 9
                      && System.Math.Abs(archiveKnight.SpritePixelSize - 0.018f)
                          < 0.00001f
                      && System.Math.Abs(
                          archiveKnight.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !archiveKnight.TintSpriteSheet,
                    "Archive Knight uses a unique untinted animation-ready atlas");
                Check(archiveKnightForm.SpriteSheetPath
                          == archiveKnight.SpriteSheetPath
                      && archiveKnightForm.SpriteSheetColumns
                          == archiveKnight.SpriteSheetColumns
                      && archiveKnightForm.SpriteSheetRows
                          == archiveKnight.SpriteSheetRows
                      && !archiveKnightForm.TintSpriteSheet,
                    "Archive Knight inherited form preserves the boss visual identity");
                var archiveKnightIntro = archiveKnight.IntroAnimation is not null
                    && ResourceLoader.Exists(archiveKnight.IntroAnimation.AtlasPath)
                    ? GD.Load<Texture2D>(archiveKnight.IntroAnimation.AtlasPath)
                    : null;
                Check(archiveKnight.IntroAnimation is not null
                      && archiveKnight.IntroAnimation.AtlasPath.EndsWith(
                          "archive_knight_intro_style_v1.png")
                      && archiveKnightIntro is not null
                      && archiveKnightIntro.GetWidth() == 1024
                      && archiveKnightIntro.GetHeight() == 512
                      && archiveKnight.IntroAnimation.AtlasColumns == 4
                      && archiveKnight.IntroAnimation.AtlasRows == 2
                      && System.Math.Abs(
                          archiveKnight.IntroAnimation.PixelSize - 0.017f) < 0.00001f
                      && System.Math.Abs(
                          archiveKnight.IntroAnimation.GroundOffsetPixels - 122.0f) < 0.01f,
                    "Archive Knight uses the identity-matched eight-frame intro atlas");
                Check(archiveKnight.SelectPortraitPath.EndsWith(
                          "archive_knight_portrait_style_v1.png")
                      && ResourceLoader.Exists(archiveKnight.SelectPortraitPath)
                      && archiveKnightForm.SelectPortraitPath
                          == archiveKnight.SelectPortraitPath,
                    "Archive Knight boss and inherited form share the approved portrait");
                var blankMannequin = TestRosterFactory.CreateBlankMannequin();
                Check(blankMannequin.SelectPortraitPath.EndsWith(
                          "mannequin_portrait_style_v1.png")
                      && ResourceLoader.Exists(blankMannequin.SelectPortraitPath),
                    "Blank Mannequin uses the canonical illustrated matchup portrait");
                Check(HaveDistinctContent(new[]
                    {
                        archiveKnight.SpriteSheetPath,
                        "res://Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png",
                    }),
                    "Archive Knight atlas content is distinct from the base mannequin");
                Check(ResourceLoader.Exists(
                        "res://Assets/Vfx/Combat/project_mannequin_strike_burst_style_v1.png"),
                    "authored Project Mannequin strike VFX exists");
            }

            if (worldId == "world_warrior_sector")
            {
                var dojoApproach = run.Stages[0];
                var dojoLayerPaths = dojoApproach.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(dojoApproach.StageTexturePath.EndsWith(
                          "world_warrior_dojo_backdrop_style_v1.png")
                      && dojoApproach.FloorTexturePath.EndsWith(
                          "world_warrior_dojo_floor_style_v1.png")
                      && dojoLayerPaths.Length == 4
                      && dojoLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(dojoLayerPaths),
                    "Dojo Approach uses independent style-approved production layers");
                var dojoFar = dojoApproach.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var dojoMidground = dojoApproach.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var dojoForeground = dojoApproach.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(dojoFar.ParallaxFactorX < dojoMidground.ParallaxFactorX
                      && dojoForeground.Length == 2
                      && dojoForeground.All(panel =>
                          dojoMidground.ParallaxFactorX < panel.ParallaxFactorX
                          && dojoMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 4.6f),
                    "Dojo Approach production layers preserve ordered depth and edge-bound foreground");
                var dojoTrainingProp = dojoApproach.Encounters
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.ArchetypeId
                        == "world_warrior_training_dummy");
                Check(dojoTrainingProp.Id == "dojo_approach_training_dummy"
                      && dojoTrainingProp.Health == 90
                      && !dojoTrainingProp.IsThrowable
                      && dojoTrainingProp.SpawnsPickupOnBreak
                      && dojoTrainingProp.DropType == StagePickupType.Health
                      && Mathf.IsEqualApprox(dojoTrainingProp.DropChance, 1.0f)
                      && dojoTrainingProp.SpritePath.EndsWith(
                          "world_warrior_training_dummy_style_v1.png")
                      && Mathf.IsEqualApprox(
                          dojoTrainingProp.SpritePixelSize,
                          0.00147291f)
                      && Mathf.IsEqualApprox(
                          dojoTrainingProp.SpriteGroundOffsetPixels,
                          968.0f),
                    "Dojo Approach authors one calibrated nonthrowable training dummy with a guaranteed health drop");
                var trainingDummy = HazardRosterFactory
                    .CreateWorldWarriorTrainingDummy();
                var trainingDummyTexture = ResourceLoader.Exists(
                        trainingDummy.SpriteSheetPath)
                    ? GD.Load<Texture2D>(trainingDummy.SpriteSheetPath)
                    : null;
                Check(trainingDummy.Id == "world_warrior_training_dummy"
                      && trainingDummy.DisplayName == "Training Dummy"
                      && trainingDummyTexture is not null
                      && trainingDummyTexture.GetWidth() == 2048
                      && trainingDummyTexture.GetHeight() == 2048
                      && !trainingDummy.TintSpriteSheet
                      && trainingDummy.RoleTags.Contains("breakable")
                      && trainingDummy.RoleTags.Contains("world_warrior")
                      && trainingDummy.RoleTags.Contains("training_prop"),
                    "World Warrior training dummy uses a unique real-alpha breakable prop sprite");
                Check(HaveDistinctContent(new[]
                    {
                        trainingDummy.SpriteSheetPath,
                        "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png",
                    }),
                    "World Warrior training dummy content is distinct from Archive caches");
                var dojoSupplyCrate = dojoApproach.Encounters
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.ArchetypeId
                        == "world_warrior_supply_crate");
                Check(dojoSupplyCrate.Id == "dojo_approach_supply_crate"
                      && dojoSupplyCrate.Health == 70
                      && dojoSupplyCrate.SpawnsPickupOnBreak
                      && dojoSupplyCrate.DropType == StagePickupType.Meter
                      && Mathf.IsEqualApprox(dojoSupplyCrate.DropChance, 1.0f)
                      && dojoSupplyCrate.SpritePath.EndsWith(
                          "world_warrior_supply_crate_style_v1.png")
                      && Mathf.IsEqualApprox(
                          dojoSupplyCrate.SpritePixelSize,
                          0.00079027f)
                      && Mathf.IsEqualApprox(
                          dojoSupplyCrate.SpriteGroundOffsetPixels,
                          848.0f)
                      && dojoApproach.Encounters[1].Props.Any(prop =>
                          prop.Id == "dojo_approach_supply_crate"),
                    "Dojo Approach authors one calibrated supply crate with a guaranteed meter drop");
                var supplyCrate = HazardRosterFactory
                    .CreateWorldWarriorSupplyCrate();
                var supplyCrateTexture = ResourceLoader.Exists(
                        supplyCrate.SpriteSheetPath)
                    ? GD.Load<Texture2D>(supplyCrate.SpriteSheetPath)
                    : null;
                Check(supplyCrate.Id == "world_warrior_supply_crate"
                      && supplyCrate.DisplayName == "Sparring Supply Crate"
                      && supplyCrateTexture is not null
                      && supplyCrateTexture.GetWidth() == 2048
                      && supplyCrateTexture.GetHeight() == 2048
                      && !supplyCrate.TintSpriteSheet
                      && supplyCrate.RoleTags.Contains("breakable")
                      && supplyCrate.RoleTags.Contains("world_warrior")
                      && supplyCrate.RoleTags.Contains("supply_prop"),
                    "World Warrior supply crate uses calibrated unique real-alpha breakable prop art");
                Check(HaveDistinctContent(new[]
                    {
                        supplyCrate.SpriteSheetPath,
                        trainingDummy.SpriteSheetPath,
                        "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png",
                    }),
                    "World Warrior supply crate content is distinct from the training dummy and Archive caches");
                var worldWarriorHealthPickup = HazardRosterFactory
                    .CreateWorldWarriorHealthPickup();
                var worldWarriorHealthTexture = ResourceLoader.Exists(
                        worldWarriorHealthPickup.SpriteSheetPath)
                    ? GD.Load<Texture2D>(worldWarriorHealthPickup.SpriteSheetPath)
                    : null;
                Check(worldWarriorHealthPickup.Id == "pickup_health"
                      && worldWarriorHealthPickup.DisplayName == "Vitality Gourd"
                      && worldWarriorHealthTexture is not null
                      && worldWarriorHealthTexture.GetWidth() == 2048
                      && worldWarriorHealthTexture.GetHeight() == 2048
                      && Mathf.IsEqualApprox(
                          worldWarriorHealthPickup.SpritePixelSize,
                          0.00038125f)
                      && Mathf.IsEqualApprox(
                          worldWarriorHealthPickup.SpriteGroundOffsetPixels,
                          972.0f)
                      && !worldWarriorHealthPickup.TintSpriteSheet
                      && worldWarriorHealthPickup.RoleTags.Contains("pickup")
                      && worldWarriorHealthPickup.RoleTags.Contains("world_warrior")
                      && worldWarriorHealthPickup.RoleTags.Contains("health_pickup"),
                    "World Warrior Vitality Gourd uses calibrated unique real-alpha pickup art");
                Check(HaveDistinctContent(new[]
                    {
                        worldWarriorHealthPickup.SpriteSheetPath,
                        "res://Assets/Sprites/Pickups/Archive/archive_health_pickup_style_v2.png",
                    }),
                    "World Warrior health pickup content is distinct from the Archive Vital");
                var pavilionCircuit = run.Stages[1];
                var pavilionFocusProp = pavilionCircuit.Encounters
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.Id == "pavilion_circuit_focus_dummy");
                Check(pavilionFocusProp.ArchetypeId == "world_warrior_training_dummy"
                      && pavilionFocusProp.Health == 105
                      && !pavilionFocusProp.IsThrowable
                      && pavilionFocusProp.SpawnsPickupOnBreak
                      && pavilionFocusProp.DropType == StagePickupType.Meter
                      && Mathf.IsEqualApprox(pavilionFocusProp.DropChance, 1.0f)
                      && pavilionFocusProp.SpritePath.EndsWith(
                          "world_warrior_training_dummy_style_v1.png"),
                    "Pavilion Circuit authors one training dummy with a guaranteed meter drop");
                var pavilionRackChestProp = pavilionCircuit.Encounters
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.ArchetypeId == "world_warrior_pavilion_rack_chest");
                Check(pavilionRackChestProp.Id == "pavilion_circuit_rack_chest"
                      && pavilionRackChestProp.Health == 85
                      && !pavilionRackChestProp.IsThrowable
                      && pavilionRackChestProp.SpawnsPickupOnBreak
                      && pavilionRackChestProp.DropType == StagePickupType.Score
                      && Mathf.IsEqualApprox(pavilionRackChestProp.DropChance, 1.0f)
                      && pavilionRackChestProp.SpritePath.EndsWith(
                          "world_warrior_pavilion_rack_chest_style_v1.png")
                      && Mathf.IsEqualApprox(
                          pavilionRackChestProp.SpritePixelSize,
                          0.00094388f)
                      && Mathf.IsEqualApprox(
                          pavilionRackChestProp.SpriteGroundOffsetPixels,
                          985.0f),
                    "Pavilion Circuit authors one rack chest with a guaranteed score drop");
                Check(pavilionCircuit.Encounters[1].Props.Any(
                        prop => prop.ArchetypeId == "world_warrior_pavilion_rack_chest"),
                    "Pavilion Circuit rack chest is placed in the second encounter");
                var pavilionRackChest = HazardRosterFactory
                    .CreateWorldWarriorPavilionRackChest();
                var pavilionRackChestTexture = ResourceLoader.Exists(
                        pavilionRackChest.SpriteSheetPath)
                    ? GD.Load<Texture2D>(pavilionRackChest.SpriteSheetPath)
                    : null;
                Check(pavilionRackChest.Id == "world_warrior_pavilion_rack_chest"
                      && pavilionRackChest.DisplayName == "Pavilion Rack Chest"
                      && pavilionRackChestTexture is not null
                      && pavilionRackChestTexture.GetWidth() == 2048
                      && pavilionRackChestTexture.GetHeight() == 2048
                      && !pavilionRackChest.TintSpriteSheet
                      && pavilionRackChest.RoleTags.Contains("breakable")
                      && pavilionRackChest.RoleTags.Contains("world_warrior")
                      && pavilionRackChest.RoleTags.Contains("supply_prop"),
                    "World Warrior Pavilion Rack Chest uses a calibrated unique 2K sprite");
                Check(HaveDistinctContent(new[]
                    {
                        pavilionRackChest.SpriteSheetPath,
                        supplyCrate.SpriteSheetPath,
                        trainingDummy.SpriteSheetPath,
                        "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png",
                    }),
                    "Pavilion Rack Chest content is distinct from the supply crate, training dummy, and Archive cache");
                var worldWarriorMeterPickup = HazardRosterFactory
                    .CreateWorldWarriorMeterPickup();
                var worldWarriorMeterTexture = ResourceLoader.Exists(
                        worldWarriorMeterPickup.SpriteSheetPath)
                    ? GD.Load<Texture2D>(worldWarriorMeterPickup.SpriteSheetPath)
                    : null;
                Check(worldWarriorMeterPickup.Id == "pickup_meter"
                      && worldWarriorMeterPickup.DisplayName == "Focus Drum"
                      && worldWarriorMeterTexture is not null
                      && worldWarriorMeterTexture.GetWidth() == 2048
                      && worldWarriorMeterTexture.GetHeight() == 2048
                      && Mathf.IsEqualApprox(
                          worldWarriorMeterPickup.SpritePixelSize,
                          0.00052895f)
                      && Mathf.IsEqualApprox(
                          worldWarriorMeterPickup.SpriteGroundOffsetPixels,
                          700.0f)
                      && !worldWarriorMeterPickup.TintSpriteSheet
                      && worldWarriorMeterPickup.RoleTags.Contains("pickup")
                      && worldWarriorMeterPickup.RoleTags.Contains("world_warrior")
                      && worldWarriorMeterPickup.RoleTags.Contains("meter_pickup"),
                    "World Warrior Focus Drum uses calibrated unique real-alpha pickup art");
                Check(HaveDistinctContent(new[]
                    {
                        worldWarriorMeterPickup.SpriteSheetPath,
                        worldWarriorHealthPickup.SpriteSheetPath,
                        "res://Assets/Sprites/Pickups/Archive/archive_meter_pickup_style_v2.png",
                    }),
                    "World Warrior meter pickup content is distinct from health and Archive meter art");
                var pavilionLayerPaths = pavilionCircuit.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(pavilionCircuit.StageTexturePath.EndsWith(
                          "world_warrior_pavilion_backdrop_style_v1.png")
                      && pavilionCircuit.FloorTexturePath.EndsWith(
                          "world_warrior_pavilion_floor_style_v2.png")
                      && pavilionLayerPaths.Length == 4
                      && pavilionLayerPaths.All(path => ResourceLoader.Exists(path))
                      && HaveDistinctContent(pavilionLayerPaths)
                      && HaveDistinctContent(new[]
                      {
                          pavilionCircuit.StageTexturePath,
                          dojoApproach.StageTexturePath,
                      }),
                    "Pavilion Circuit uses independent style-approved production layers");
                var pavilionFar = pavilionCircuit.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Far);
                var pavilionMidground = pavilionCircuit.BackgroundPanels.Single(panel =>
                    panel.Layer == StageVisualLayerKind.Midground);
                var pavilionForeground = pavilionCircuit.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(pavilionFar.ParallaxFactorX < pavilionMidground.ParallaxFactorX
                        && pavilionFar.ScaleYMultiplier <= 0.40f
                        && pavilionMidground.ScaleYMultiplier <= 0.26f
                    && pavilionMidground.AlignBottomToFloor
                      && pavilionForeground.Length == 2
                      && pavilionForeground.All(panel =>
                          pavilionMidground.ParallaxFactorX < panel.ParallaxFactorX
                          && pavilionMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 4.6f),
                    "Pavilion Circuit layers preserve ordered depth and edge-bound foreground");
                var grandTournament = run.Stages[2];
                var grandTournamentHonorProp = grandTournament.Encounters
                    .SelectMany(encounter => encounter.Props)
                    .Single(prop => prop.Id == "grand_tournament_honor_dummy");
                Check(grandTournamentHonorProp.ArchetypeId
                          == "world_warrior_training_dummy"
                      && grandTournamentHonorProp.Health == 120
                      && !grandTournamentHonorProp.IsThrowable
                      && grandTournamentHonorProp.SpawnsPickupOnBreak
                      && grandTournamentHonorProp.DropType == StagePickupType.Score
                      && Mathf.IsEqualApprox(
                          grandTournamentHonorProp.DropChance,
                          1.0f)
                      && grandTournamentHonorProp.SpritePath.EndsWith(
                          "world_warrior_training_dummy_style_v1.png"),
                    "Grand Tournament authors one training dummy with a guaranteed score drop");
                var worldWarriorScorePickup = HazardRosterFactory
                    .CreateWorldWarriorScorePickup();
                var worldWarriorScoreTexture = ResourceLoader.Exists(
                        worldWarriorScorePickup.SpriteSheetPath)
                    ? GD.Load<Texture2D>(worldWarriorScorePickup.SpriteSheetPath)
                    : null;
                Check(worldWarriorScorePickup.Id == "pickup_score"
                      && worldWarriorScorePickup.DisplayName == "Judge's Laurel Fan"
                      && worldWarriorScoreTexture is not null
                      && worldWarriorScoreTexture.GetWidth() == 2048
                      && worldWarriorScoreTexture.GetHeight() == 2048
                      && Mathf.IsEqualApprox(
                          worldWarriorScorePickup.SpritePixelSize,
                          0.00042676f)
                      && Mathf.IsEqualApprox(
                          worldWarriorScorePickup.SpriteGroundOffsetPixels,
                          854.0f)
                      && !worldWarriorScorePickup.TintSpriteSheet
                      && worldWarriorScorePickup.RoleTags.Contains("pickup")
                      && worldWarriorScorePickup.RoleTags.Contains("world_warrior")
                      && worldWarriorScorePickup.RoleTags.Contains("score_pickup"),
                    "World Warrior Judge's Laurel Fan uses calibrated unique real-alpha pickup art");
                Check(HaveDistinctContent(new[]
                    {
                        worldWarriorScorePickup.SpriteSheetPath,
                        worldWarriorHealthPickup.SpriteSheetPath,
                        worldWarriorMeterPickup.SpriteSheetPath,
                        "res://Assets/Sprites/Pickups/Archive/archive_data_pickup_style_v2.png",
                    }),
                    "World Warrior score pickup content is distinct from health meter and Archive score art");
                var grandTournamentLayerPaths = grandTournament.BackgroundPanels
                    .Select(panel => panel.TexturePath)
                    .ToArray();
                Check(grandTournament.StageTexturePath.EndsWith(
                          "world_warrior_grand_tournament_backdrop_style_v1.png")
                      && grandTournament.FloorTexturePath.EndsWith(
                          "world_warrior_grand_tournament_floor_style_v2.png")
                      && grandTournamentLayerPaths.Length == 4
                      && grandTournamentLayerPaths.All(path =>
                          ResourceLoader.Exists(path))
                      && HaveDistinctContent(grandTournamentLayerPaths)
                      && HaveDistinctContent(new[]
                      {
                          grandTournament.StageTexturePath,
                          dojoApproach.StageTexturePath,
                          pavilionCircuit.StageTexturePath,
                      }),
                    "Grand Tournament Floor uses independent style-approved production layers");
                var grandTournamentFar = grandTournament.BackgroundPanels.Single(
                    panel => panel.Layer == StageVisualLayerKind.Far);
                var grandTournamentMidground = grandTournament.BackgroundPanels.Single(
                    panel => panel.Layer == StageVisualLayerKind.Midground);
                var grandTournamentForeground = grandTournament.BackgroundPanels
                    .Where(panel => panel.Layer == StageVisualLayerKind.Foreground)
                    .ToArray();
                Check(grandTournamentFar.ParallaxFactorX
                          < grandTournamentMidground.ParallaxFactorX
                      && grandTournamentFar.ScaleYMultiplier <= 0.40f
                      && grandTournamentMidground.ScaleYMultiplier <= 0.26f
                      && grandTournamentMidground.AlignBottomToFloor
                      && grandTournamentForeground.Length == 2
                      && grandTournamentForeground.All(panel =>
                          grandTournamentMidground.ParallaxFactorX
                              < panel.ParallaxFactorX
                          && grandTournamentMidground.PositionZ < panel.PositionZ
                          && panel.Opacity < 0.8f
                          && panel.MaxX - panel.MinX <= 4.8f),
                    "Grand Tournament Floor layers preserve camera-safe depth and edge-bound foreground");
                var championsCourtyard = run.Stages[3];
                var courtyardArena = championsCourtyard.ArenaPresentation;
                var courtyardPlate = championsCourtyard.FullFramePlates.Count == 1
                    ? championsCourtyard.FullFramePlates[0]
                    : null;
                var courtyardPlateTexture = courtyardPlate is not null
                    && ResourceLoader.Exists(courtyardPlate.TexturePath)
                        ? GD.Load<Texture2D>(courtyardPlate.TexturePath)
                        : null;
                Check(championsCourtyard.PresentationMode
                          == StagePresentationMode.FullFramePlates
                      && courtyardArena is not null
                      && championsCourtyard.BackgroundPanels.Count == 0
                      && championsCourtyard.CompositeSegments.Count == 0
                      && courtyardPlate is not null
                      && courtyardPlate.TexturePath.EndsWith(
                          "world_warrior_champions_courtyard_full_frame_plate_style_v2.png")
                      && courtyardPlateTexture is not null
                      && courtyardPlateTexture.GetWidth() == 5504
                      && courtyardPlateTexture.GetHeight() == 3096
                      && courtyardPlateTexture.GetWidth() * 9
                          == courtyardPlateTexture.GetHeight() * 16
                      && Mathf.IsEqualApprox(courtyardArena.CameraSize, 9.0f)
                      && Mathf.IsEqualApprox(courtyardArena.CameraMaxSize, 9.0f)
                      && Mathf.IsEqualApprox(
                          courtyardArena.CenterX,
                          championsCourtyard.Encounters.Single().CameraLockX)
                      && Mathf.IsEqualApprox(
                          courtyardPlate.CenterX,
                          courtyardArena.CenterX),
                    "Champion's Courtyard displays one complete full-frame stage plate");
                Check(FullFramePlatesGroundFighters(championsCourtyard, 0.005f),
                    "Champion's Courtyard grounds fighters on its painted arena floor");
                Check(championsCourtyard.Encounters.All(encounter =>
                          encounter.HazardZones.Count == 0)
                      && Mathf.IsEqualApprox(
                          championsCourtyard.CameraBaseSize,
                          championsCourtyard.CameraMaxSize)
                      && championsCourtyard.CameraCinematicSize
                          < championsCourtyard.CameraBaseSize,
                    "Champion's Courtyard preserves fixed normal framing without external hazards");
                var rookie = TestRosterFactory.CreateWorldWarriorRookie();
                var rookieTexture = ResourceLoader.Exists(rookie.SpriteSheetPath)
                    ? GD.Load<Texture2D>(rookie.SpriteSheetPath)
                    : null;
                Check(rookie.SpriteSheetPath.EndsWith(
                          "world_warrior_rookie_style_v3.png")
                      && rookieTexture is not null
                      && rookieTexture.GetWidth() == 2560
                      && rookieTexture.GetHeight() == 2304
                      && rookie.SpriteSheetColumns == 10
                      && rookie.SpriteSheetRows == 9
                      && !rookie.TintSpriteSheet,
                    "Dojo Rookie uses the original identity-locked walk-repaired 10x9 atlas");
                var quickPalm = rookie.FindMove("world_warrior_rookie_attack");
                Check(quickPalm is not null
                      && quickPalm.AnimationFrameSequence.SequenceEqual(
                          new[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 })
                      && quickPalm.AnimationFrameDurations.SequenceEqual(
                          new[] { 3, 3, 3, 3, 5, 4, 4, 4, 4, 7 })
                      && quickPalm.AnimationFrameDurations.Sum()
                          == quickPalm.TotalFrames,
                    "Dojo Rookie Quick Palm animation timing matches startup active and recovery phases");
                var stageOneElite = dojoApproach.Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageOneElite.DisplayName == "Dojo Prodigy Kenzo"
                      && stageOneElite.Spawns.Single().ArchetypeId
                          == "world_warrior_dojo_prodigy_kenzo",
                    "World Warrior Stage 1 selects the distinct Kenzo archetype");
                var kenzo = TestRosterFactory.CreateWorldWarriorDojoProdigyKenzo();
                var kenzoTexture = ResourceLoader.Exists(kenzo.SpriteSheetPath)
                    ? GD.Load<Texture2D>(kenzo.SpriteSheetPath)
                    : null;
                Check(kenzo.Id == "world_warrior_dojo_prodigy_kenzo"
                      && kenzo.DisplayName == "Dojo Prodigy Kenzo"
                      && kenzo.SpriteSheetPath.EndsWith(
                          "world_warrior_dojo_prodigy_kenzo_style_v2.png")
                      && kenzoTexture is not null
                      && kenzoTexture.GetWidth() == 2560
                      && kenzoTexture.GetHeight() == 2304
                      && kenzo.SpriteSheetColumns == 10
                      && kenzo.SpriteSheetRows == 9
                      && System.Math.Abs(kenzo.SpritePixelSize - 0.0190f) < 0.00001f
                      && System.Math.Abs(kenzo.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !kenzo.TintSpriteSheet
                      && kenzo.ArcadeEnemyProfile?.Id
                                                    == "world_warrior_dojo_prodigy_kenzo_arcade_ai"
                      && kenzo.RoleTags.Contains("named_elite"),
                    "Dojo Prodigy Kenzo uses a unique untinted animation-ready atlas");
                var masterPalm = kenzo.FindMove(
                    "world_warrior_dojo_prodigy_kenzo_attack");
                Check(masterPalm is not null
                      && masterPalm.DisplayName == "Master Palm"
                      && masterPalm.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && masterPalm.AnimationFrameDurations.SequenceEqual(
                          new[] { 3, 3, 3, 3, 5, 4, 4, 4, 4, 7 })
                      && masterPalm.AnimationFrameDurations.Take(4).Sum()
                          == masterPalm.StartupFrames
                      && masterPalm.AnimationFrameDurations[4]
                          == masterPalm.ActiveFrames
                      && masterPalm.AnimationFrameDurations.Skip(5).Sum()
                          == masterPalm.RecoveryFrames,
                    "Kenzo Master Palm preserves Rookie combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        rookie.SpriteSheetPath,
                        kenzo.SpriteSheetPath,
                    }),
                    "Kenzo atlas content is distinct from the base Rookie");
                var striker = TestRosterFactory.CreateWorldWarriorStriker();
                var strikerTexture = ResourceLoader.Exists(striker.SpriteSheetPath)
                    ? GD.Load<Texture2D>(striker.SpriteSheetPath)
                    : null;
                Check(striker.SpriteSheetPath.EndsWith(
                          "world_warrior_striker_style_v3.png")
                      && strikerTexture is not null
                      && strikerTexture.GetWidth() == 2560
                      && strikerTexture.GetHeight() == 2304
                      && striker.SpriteSheetColumns == 10
                      && striker.SpriteSheetRows == 9
                      && !striker.TintSpriteSheet,
                    "Pavilion Striker uses the original identity-locked walk-repaired 10x9 atlas");
                var turningKick = striker.FindMove("world_warrior_striker_attack");
                Check(turningKick is not null
                      && turningKick.AnimationFrameSequence.SequenceEqual(
                          new[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 })
                      && turningKick.AnimationFrameDurations.SequenceEqual(
                          new[] { 4, 4, 4, 4, 5, 4, 5, 5, 6, 7 })
                      && turningKick.AnimationFrameDurations.Sum()
                          == turningKick.TotalFrames,
                    "Pavilion Striker Turning Kick timing matches startup active and recovery phases");
                var stageTwoElite = pavilionCircuit.Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageTwoElite.DisplayName == "Pavilion Ace Makoto"
                      && stageTwoElite.Spawns.Single().ArchetypeId
                          == "world_warrior_pavilion_ace_makoto",
                    "World Warrior Stage 2 selects the distinct Makoto archetype");
                var makoto = TestRosterFactory.CreateWorldWarriorPavilionAceMakoto();
                var makotoTexture = ResourceLoader.Exists(makoto.SpriteSheetPath)
                    ? GD.Load<Texture2D>(makoto.SpriteSheetPath)
                    : null;
                Check(makoto.Id == "world_warrior_pavilion_ace_makoto"
                      && makoto.DisplayName == "Pavilion Ace Makoto"
                      && makoto.SpriteSheetPath.EndsWith(
                          "world_warrior_pavilion_ace_makoto_style_v1.png")
                      && makotoTexture is not null
                      && makotoTexture.GetWidth() == 2560
                      && makotoTexture.GetHeight() == 2304
                      && makoto.SpriteSheetColumns == 10
                      && makoto.SpriteSheetRows == 9
                      && System.Math.Abs(makoto.SpritePixelSize - 0.0190f) < 0.00001f
                      && System.Math.Abs(makoto.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !makoto.TintSpriteSheet
                      && makoto.ArcadeEnemyProfile?.Id
                          == "world_warrior_pavilion_ace_makoto_arcade_ai"
                      && makoto.RoleTags.Contains("named_elite"),
                    "Pavilion Ace Makoto uses a unique untinted animation-ready atlas");
                var crescentHeel = makoto.FindMove(
                    "world_warrior_pavilion_ace_makoto_attack");
                Check(crescentHeel is not null
                      && crescentHeel.DisplayName == "Crescent Heel"
                      && crescentHeel.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && crescentHeel.AnimationFrameDurations.SequenceEqual(
                          new[] { 4, 4, 4, 4, 5, 4, 5, 5, 6, 7 })
                      && crescentHeel.AnimationFrameDurations.Take(4).Sum()
                          == crescentHeel.StartupFrames
                      && crescentHeel.AnimationFrameDurations[4]
                          == crescentHeel.ActiveFrames
                      && crescentHeel.AnimationFrameDurations.Skip(5).Sum()
                          == crescentHeel.RecoveryFrames,
                    "Makoto Crescent Heel preserves Striker combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        striker.SpriteSheetPath,
                        makoto.SpriteSheetPath,
                    }),
                    "Makoto atlas content is distinct from the base Striker");
                var grappler = TestRosterFactory.CreateWorldWarriorGrappler();
                var grapplerTexture = ResourceLoader.Exists(grappler.SpriteSheetPath)
                    ? GD.Load<Texture2D>(grappler.SpriteSheetPath)
                    : null;
                Check(grappler.SpriteSheetPath.EndsWith(
                          "world_warrior_grappler_style_v3.png")
                      && grapplerTexture is not null
                      && grapplerTexture.GetWidth() == 2560
                      && grapplerTexture.GetHeight() == 2304
                      && grappler.SpriteSheetColumns == 10
                      && grappler.SpriteSheetRows == 9
                      && !grappler.TintSpriteSheet,
                    "Tournament Grappler uses the original identity-locked walk-repaired 10x9 atlas");
                var shoulderDrive = grappler.FindMove(
                    "world_warrior_grappler_attack");
                Check(shoulderDrive is not null
                      && shoulderDrive.DisplayName == "Shoulder Drive"
                      && shoulderDrive.StartupFrames == 20
                      && shoulderDrive.ActiveFrames == 5
                      && shoulderDrive.RecoveryFrames == 34
                      && shoulderDrive.AnimationFrameSequence.SequenceEqual(
                          new[] { 40, 41, 42, 43, 44, 45, 46, 47, 48, 49 })
                      && shoulderDrive.AnimationFrameDurations.SequenceEqual(
                          new[] { 5, 5, 5, 5, 5, 6, 7, 7, 7, 7 })
                      && shoulderDrive.AnimationFrameDurations.Take(4).Sum()
                          == shoulderDrive.StartupFrames
                      && shoulderDrive.AnimationFrameDurations[4]
                          == shoulderDrive.ActiveFrames
                      && shoulderDrive.AnimationFrameDurations.Skip(5).Sum()
                          == shoulderDrive.RecoveryFrames
                      && shoulderDrive.AnimationFrameDurations.Sum()
                          == shoulderDrive.TotalFrames,
                    "Tournament Grappler Shoulder Drive timing matches startup active and recovery phases");
                var stageThreeElite = grandTournament.Encounters.Single(encounter =>
                    encounter.Kind == StageEncounterKind.Elite);
                Check(stageThreeElite.DisplayName == "Grand Grappler Tetsu"
                      && stageThreeElite.Spawns.Single().ArchetypeId
                          == "world_warrior_grand_grappler_tetsu",
                    "World Warrior Stage 3 selects the distinct Tetsu archetype");
                var tetsu = TestRosterFactory.CreateWorldWarriorGrandGrapplerTetsu();
                var tetsuTexture = ResourceLoader.Exists(tetsu.SpriteSheetPath)
                    ? GD.Load<Texture2D>(tetsu.SpriteSheetPath)
                    : null;
                Check(tetsu.Id == "world_warrior_grand_grappler_tetsu"
                      && tetsu.DisplayName == "Grand Grappler Tetsu"
                      && tetsu.SpriteSheetPath.EndsWith(
                          "world_warrior_grand_grappler_tetsu_style_v1.png")
                      && tetsuTexture is not null
                      && tetsuTexture.GetWidth() == 2560
                      && tetsuTexture.GetHeight() == 2304
                      && tetsu.SpriteSheetColumns == 10
                      && tetsu.SpriteSheetRows == 9
                      && System.Math.Abs(tetsu.SpritePixelSize - 0.018f) < 0.00001f
                      && System.Math.Abs(tetsu.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !tetsu.TintSpriteSheet
                      && tetsu.ArcadeEnemyProfile?.Id
                          == "world_warrior_grand_grappler_tetsu_arcade_ai"
                      && tetsu.RoleTags.Contains("named_elite"),
                    "Grand Grappler Tetsu uses a unique untinted animation-ready atlas");
                var ironGateClinch = tetsu.FindMove(
                    "world_warrior_grand_grappler_tetsu_attack");
                Check(ironGateClinch is not null
                      && ironGateClinch.DisplayName == "Iron Gate Clinch"
                      && ironGateClinch.AnimationFrameSequence.SequenceEqual(
                          Enumerable.Range(40, 10))
                      && ironGateClinch.AnimationFrameDurations.SequenceEqual(
                          new[] { 5, 5, 5, 5, 5, 6, 7, 7, 7, 7 })
                      && ironGateClinch.AnimationFrameDurations.Take(4).Sum()
                          == ironGateClinch.StartupFrames
                      && ironGateClinch.AnimationFrameDurations[4]
                          == ironGateClinch.ActiveFrames
                      && ironGateClinch.AnimationFrameDurations.Skip(5).Sum()
                          == ironGateClinch.RecoveryFrames,
                    "Tetsu Iron Gate Clinch preserves Grappler combat timing on unique frames");
                Check(HaveDistinctContent(new[]
                    {
                        grappler.SpriteSheetPath,
                        tetsu.SpriteSheetPath,
                    }),
                    "Tetsu atlas content is distinct from the base Grappler");
            }

            if (worldId == "astral_battlefront")
            {
                // Each stage paints its own ground, so the floor path must be
                // that stage's own plate at full plate resolution.
                Check(run.Stages.All(stage =>
                          stage.FullFramePlates.Count == 1
                          && stage.FloorTexturePath
                              == stage.FullFramePlates[0].TexturePath
                          && stage.StageTexturePath
                              == stage.FullFramePlates[0].TexturePath
                          && Mathf.IsZeroApprox(stage.FloorTextureTopFraction)
                          && ResourceLoader.Exists(stage.FloorTexturePath)
                          && GD.Load<Texture2D>(stage.FloorTexturePath) is
                              { } stageFloorTexture
                          && stageFloorTexture.GetWidth() == 2704
                          && stageFloorTexture.GetHeight() == 1521),
                    "Astral stages use approved floors including full-frame plate production art");

                var capsuleCausewayStage = run.Stages[1];
                Check(capsuleCausewayStage.PresentationMode
                          == StagePresentationMode.FullFramePlates
                      && capsuleCausewayStage.BackgroundPanels.Count == 0
                      && capsuleCausewayStage.CompositeSegments.Count == 0
                      && capsuleCausewayStage.FullFramePlates.Count == 1
                      && capsuleCausewayStage.FullFramePlates[0].TexturePath
                          == "res://Assets/Stages/AstralBattlefront/astral_capsule_causeway_full_frame_plate_01.png"
                      && FullFramePlatesGroundFighters(capsuleCausewayStage, 0.005f),
                    "Astral Capsule Causeway grounds fighters on one complete scene plate");

                // Every Astral stage now owns one complete scene painting.
                // Stage 3 previously reused Stage 1's and Stage 4's plates
                // byte-for-byte, which repeated landmarks across the ladder.
                var astralPlatePaths = run.Stages
                    .SelectMany(stage => stage.FullFramePlates)
                    .Select(plate => plate.TexturePath)
                    .ToArray();
                Check(run.Stages.All(stage =>
                          stage.PresentationMode
                              == StagePresentationMode.FullFramePlates
                          && stage.BackgroundPanels.Count == 0
                          && stage.CompositeSegments.Count == 0
                          && stage.FullFramePlates.Count == 1
                          && FullFramePlatesGroundFighters(stage, 0.005f))
                      && astralPlatePaths.SequenceEqual(new[]
                      {
                          "res://Assets/Stages/AstralBattlefront/astral_skyfall_breach_full_frame_plate_01.png",
                          "res://Assets/Stages/AstralBattlefront/astral_capsule_causeway_full_frame_plate_01.png",
                          "res://Assets/Stages/AstralBattlefront/astral_energy_rail_full_frame_plate_01.png",
                          "res://Assets/Stages/AstralBattlefront/astral_tournament_summit_full_frame_plate_01.png",
                      })
                      && HaveDistinctContent(astralPlatePaths),
                    "Astral stages each ground fighters on their own complete scene plate");

                Check(run.Stages.All(stage =>
                          stage.BackgroundPanels.Count == 0
                          && stage.CompositeSegments.Count == 0
                          && !stage.FloorTexturePath.EndsWith(
                              "astral_skyway_floor_style_v1.png")),
                    "Astral stages retired the layered route billboards and stopgap tiled floor");
            }
        }

        // --- Key light contract ------------------------------------------

        // A contact shadow that leans toward the light instead of away from it
        // is the giveaway that grounding and lighting disagree.
        var leftKey = ProjectMannequin.Stage.StageKeyLight.ContactOffset(
            -45.0f, -40.0f, 1.0f);
        var rightKey = ProjectMannequin.Stage.StageKeyLight.ContactOffset(
            45.0f, -40.0f, 1.0f);
        Check(leftKey.X > 0.0f && rightKey.X < 0.0f
              && Mathf.IsEqualApprox(leftKey.X, -rightKey.X),
            "the contact shadow leans away from the key light and mirrors with it");

        var overhead = ProjectMannequin.Stage.StageKeyLight.ContactOffset(
            -40.0f, -85.0f, 1.0f);
        var raking = ProjectMannequin.Stage.StageKeyLight.ContactOffset(
            -40.0f, -12.0f, 1.0f);
        Check(raking.Length() > overhead.Length(),
            "a low raking key throws the contact shadow further than an overhead one");

        Check(Mathf.IsZeroApprox(
                  ProjectMannequin.Stage.StageKeyLight.ContactOffset(
                      0.0f, -48.0f, 1.0f).X),
            "a key directly along the view axis produces no sideways lean");

        // Absolute pitch, so a malformed positive value cannot invert the lean.
        Check(ProjectMannequin.Stage.StageKeyLight.ContactOffset(-40.0f, 48.0f, 1.0f)
              == ProjectMannequin.Stage.StageKeyLight.ContactOffset(-40.0f, -48.0f, 1.0f),
            "key pitch sign cannot invert the contact lean");

        // The backdrop plane is wired around z = -6, so haze must still be
        // ramping there rather than having already saturated at the lane edge.
        Check(ProjectMannequin.Presentation.PrototypeStageView.FarHazeSpan
              > ProjectMannequin.Presentation.PrototypeStageView
                  .FarHazeLaneClearance,
            "haze ramps over a longer distance than its lane clearance");

        // A negative or zero span would make begin and end coincide and turn
        // the ramp into a hard edge across the backdrop.
        var hazeReferenceStage = WorldRunCatalog
            .CreateRun("world_warrior_sector").Stages[0];
        var degenerate = ProjectMannequin.Stage.StageGroundProjection
            .ResolveFarHazeRange(hazeReferenceStage, 1.5f, -5.0f);
        Check(degenerate.End > degenerate.Begin,
            "a malformed haze span cannot collapse the ramp to a hard edge");

        // Negative clearance would drag the haze forward onto the fighters.
        var clamped = ProjectMannequin.Stage.StageGroundProjection
            .ResolveFarHazeRange(hazeReferenceStage, -9.0f, 7.0f);
        Check(clamped.Begin >= ProjectMannequin.Stage.StageGroundProjection
                  .ResolveRestingCameraProfile(hazeReferenceStage).Depth
              - hazeReferenceStage.LaneMinZ,
            "negative haze clearance cannot pull haze onto the fighters");

        // --- Vignette shape -----------------------------------------------

        // THE reason this vignette is vertical rather than radial. A cornered
        // fighter is pinned to the left or right edge, so any horizontal
        // falloff dims the character who can least afford it. The profile is
        // a function of HEIGHT only, so width is untouched by construction;
        // what has to be gated is that it stays clear of the fighter band.
        var vignetteClear = true;
        for (var step = 0; step <= 40; step++)
        {
            var fraction = ProjectMannequin.Presentation.StageVignette.FighterBandTop
                + (ProjectMannequin.Presentation.StageVignette.FighterBandBottom
                   - ProjectMannequin.Presentation.StageVignette.FighterBandTop)
                  * (step / 40.0f);
            vignetteClear &= Mathf.IsZeroApprox(
                ProjectMannequin.Presentation.StageVignette.AlphaAt(fraction));
        }
        Check(vignetteClear,
            "the vignette never darkens any part of the fighter band");

        Check(ProjectMannequin.Presentation.StageVignette.AlphaAt(0.0f) > 0.0f
              && ProjectMannequin.Presentation.StageVignette.AlphaAt(1.0f) > 0.0f,
            "the vignette darkens both the top and the bottom edge");

        // Strongest at the very edge and fading inward, or it reads as a bar
        // rather than a falloff.
        var topFalls = true;
        for (var step = 0; step < 12; step++)
        {
            var near = ProjectMannequin.Presentation.StageVignette.AlphaAt(
                step / 12.0f * ProjectMannequin.Presentation.StageVignette.TopBandEnd);
            var far = ProjectMannequin.Presentation.StageVignette.AlphaAt(
                (step + 1) / 12.0f * ProjectMannequin.Presentation.StageVignette.TopBandEnd);
            topFalls &= near >= far;
        }
        Check(topFalls, "the vignette fades monotonically inward from the top edge");

        Check(ProjectMannequin.Presentation.StageVignette.AlphaAt(0.0f)
              <= ProjectMannequin.Presentation.StageVignette.TopStrength + 0.0001f
              && ProjectMannequin.Presentation.StageVignette.AlphaAt(1.0f)
                 <= ProjectMannequin.Presentation.StageVignette.BottomStrength + 0.0001f,
            "the vignette never exceeds its declared strength");

        // Out-of-range input must clamp rather than extrapolate into a fully
        // black frame.
        Check(Mathf.IsEqualApprox(
                  ProjectMannequin.Presentation.StageVignette.AlphaAt(-3.0f),
                  ProjectMannequin.Presentation.StageVignette.AlphaAt(0.0f))
              && Mathf.IsEqualApprox(
                  ProjectMannequin.Presentation.StageVignette.AlphaAt(4.0f),
                  ProjectMannequin.Presentation.StageVignette.AlphaAt(1.0f)),
            "an out-of-range vignette height clamps instead of extrapolating");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }

    /// <summary>
    /// True when a floor material's detail is not aligned to one axis.
    /// </summary>
    /// <remarks>
    /// The floor plane is viewed at a shallow angle, so a material painted in
    /// courses collapses into screen-wide stripes. This loads whatever the stage
    /// actually wires rather than a path guessed by hand: auditing by filename
    /// previously flagged a superseded asset and cleared none of the real ones.
    /// </remarks>
    private static bool FloorMaterialIsIsotropic(
        string floorTexturePath,
        float minimumRatio)
    {
        if (!ResourceLoader.Exists(floorTexturePath))
        {
            return false;
        }

        using var texture = GD.Load<Texture2D>(floorTexturePath);
        using var image = texture.GetImage();
        if (image is null)
        {
            return false;
        }

        if (image.IsCompressed())
        {
            image.Decompress();
        }

        // Every fourth pixel is plenty for a variance ratio and keeps the
        // deterministic suite fast on 2048-square materials.
        const int step = 4;
        var width = image.GetWidth();
        var height = image.GetHeight();
        if (width < step * 2 || height < step * 2)
        {
            return false;
        }

        var rowSpread = 0.0;
        var rowCount = 0;
        for (var y = 0; y < height; y += step)
        {
            var sum = 0.0;
            var sumSquares = 0.0;
            var samples = 0;
            for (var x = 0; x < width; x += step)
            {
                var value = image.GetPixel(x, y).Luminance;
                sum += value;
                sumSquares += value * value;
                samples++;
            }

            var mean = sum / samples;
            rowSpread += Math.Sqrt(Math.Max(0.0, sumSquares / samples - mean * mean));
            rowCount++;
        }

        var columnSpread = 0.0;
        var columnCount = 0;
        for (var x = 0; x < width; x += step)
        {
            var sum = 0.0;
            var sumSquares = 0.0;
            var samples = 0;
            for (var y = 0; y < height; y += step)
            {
                var value = image.GetPixel(x, y).Luminance;
                sum += value;
                sumSquares += value * value;
                samples++;
            }

            var mean = sum / samples;
            columnSpread += Math.Sqrt(Math.Max(0.0, sumSquares / samples - mean * mean));
            columnCount++;
        }

        var horizontal = rowSpread / Math.Max(1, rowCount);
        var vertical = columnSpread / Math.Max(1, columnCount);
        return vertical > 0.0001 && horizontal / vertical >= minimumRatio;
    }

    /// <summary>
    /// True when every fighter standing anywhere in the belt depth lands on the
    /// floor the stage plate actually paints.
    /// </summary>
    /// <remarks>
    /// Plate dimensions, aspect, and pixel fidelity all pass even when the
    /// painted floor sits well below the projected ground plane, which reads in
    /// game as fighters hovering in mid-air. Only this projection catches it.
    /// </remarks>
    private static bool FullFramePlatesGroundFighters(
        StageMissionData stage,
        float minimumMarginFraction)
    {
        var painted = StageGroundProjection.ResolvePaintedGroundBand(stage);
        if (painted is null)
        {
            return false;
        }

        var laneMinZ = stage.LaneMinZ;
        var laneMaxZ = stage.LaneMaxZ;
        foreach (var encounter in stage.Encounters.Where(
                     encounter => encounter.UsesLaneBounds))
        {
            laneMinZ = Mathf.Min(laneMinZ, encounter.LaneMinZ);
            laneMaxZ = Mathf.Max(laneMaxZ, encounter.LaneMaxZ);
        }

        var gameplay = StageGroundProjection.ResolveGameplayGroundBand(
            StageGroundProjection.ResolveRestingCameraProfile(stage),
            laneMinZ,
            laneMaxZ);
        return gameplay.IsWithin(painted.Value, minimumMarginFraction);
    }

    private static bool HaveDistinctContent(IEnumerable<string> resourcePaths)
    {
        var hashes = new HashSet<string>();
        foreach (var resourcePath in resourcePaths)
        {
            var absolutePath = ProjectSettings.GlobalizePath(resourcePath);
            if (!File.Exists(absolutePath))
            {
                return false;
            }

            using var stream = File.OpenRead(absolutePath);
            if (!hashes.Add(System.Convert.ToHexString(SHA256.HashData(stream))))
            {
                return false;
            }
        }

        return true;
    }
}
