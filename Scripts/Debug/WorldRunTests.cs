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
                var raider = TestRosterFactory.CreateArchiveRaider();
                var raiderTexture = ResourceLoader.Exists(raider.SpriteSheetPath)
                    ? GD.Load<Texture2D>(raider.SpriteSheetPath)
                    : null;
                Check(raider.SpriteSheetPath.EndsWith("archive_raider_style_v2.png")
                      && raiderTexture is not null
                      && raiderTexture.GetWidth() == 2560
                      && raiderTexture.GetHeight() == 2304
                      && raider.SpriteSheetColumns == 10
                      && raider.SpriteSheetRows == 9
                      && System.Math.Abs(raider.SpritePixelSize - 0.018f) < 0.00001f
                      && System.Math.Abs(raider.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !raider.TintSpriteSheet,
                    "Archive Raider uses the identity-locked calibrated 10x9 style-v2 atlas");
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
                Check(bruiser.SpriteSheetPath.EndsWith("archive_bruiser_style_v2.png")
                      && bruiserTexture is not null
                      && bruiserTexture.GetWidth() == 2560
                      && bruiserTexture.GetHeight() == 2304
                      && bruiser.SpriteSheetColumns == 10
                      && bruiser.SpriteSheetRows == 9
                      && System.Math.Abs(bruiser.SpritePixelSize - 0.0205f) < 0.00001f
                      && System.Math.Abs(bruiser.SpriteGroundOffsetPixels - 120.0f) < 0.01f
                      && !bruiser.TintSpriteSheet,
                    "Archive Bruiser uses the heavyweight identity-locked 10x9 style-v2 atlas");
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
                          "overseer_basalt_style_v1.png")
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
