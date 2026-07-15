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
                Check(StageMissionValidator.Validate(stage).Count == 0,
                    $"{worldId} stage {expectedNumber} mission validation");

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
                Check(ResourceLoader.Exists(
                        "res://Assets/Vfx/Combat/project_mannequin_strike_burst_style_v1.png"),
                    "authored Project Mannequin strike VFX exists");
            }
        }

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
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
