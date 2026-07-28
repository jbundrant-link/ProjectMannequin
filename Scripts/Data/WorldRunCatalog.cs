using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ProjectMannequin.Data;

/// <summary>
/// Derives four short arcade stages from each existing full-route mission. The
/// original MvpWorldCatalog remains authoritative and backwards-compatible for
/// debug/smoke callers; ladder stages are deep-cloned before normalization so
/// run-time phase bursts or other mutations never leak into catalog data.
/// </summary>
public static class WorldRunCatalog
{
    private static readonly JsonSerializerOptions CloneOptions = new()
    {
        IncludeFields = true,
    };

    private static readonly Dictionary<string, StageBlueprint[]> Blueprints = new()
    {
        ["archive_nexus"] = new[]
        {
            new StageBlueprint("Intake Boulevard", "The Archive opens its outer gates", "index_warden_veyra", "Index Warden Veyra", 210.0f),
            new StageBlueprint("Index Vaults", "Raiders hold the catalog stacks", "cipher_captain_rhune", "Cipher Captain Rhune", 225.0f),
            new StageBlueprint("Corruption Repository", "The deepest records fight back", "overseer_basalt", "Overseer Basalt", 240.0f),
            new StageBlueprint("Knight's Reliquary", "Champion record: Archive Knight", "", "", 420.0f),
        },
        ["world_warrior_sector"] = new[]
        {
            new StageBlueprint("Dojo Approach", "The opening qualifier", "world_warrior_dojo_prodigy_kenzo", "Dojo Prodigy Kenzo", 210.0f),
            new StageBlueprint("Pavilion Circuit", "Challengers fill the lantern court", "world_warrior_pavilion_ace_makoto", "Pavilion Ace Makoto", 225.0f),
            new StageBlueprint("Grand Tournament Floor", "Only the strongest remain", "world_warrior_grand_grappler_tetsu", "Grand Grappler Tetsu", 240.0f),
            new StageBlueprint("Champion's Courtyard", "Final challenge: Ryu", "", "", 360.0f),
        },
        ["astral_battlefront"] = new[]
        {
            new StageBlueprint("Skyfall Breach", "Invaders descend through the shattered sky", "astral_saibaman", "Saibaman Alpha", 225.0f),
            new StageBlueprint("Capsule Causeway", "The Frieza Force blocks the ascent", "astral_frieza_scout", "Vanguard Commander Lyra", 240.0f),
            new StageBlueprint("Energy Rail Convergence", "Power gathers at the summit rail", "astral_ki_captain", "Ki Captain Prime", 270.0f),
            new StageBlueprint("Tournament Summit", "Final challenge: Goku", "", "", 540.0f),
        },
    };

    public static WorldRunData CreateRun(string worldId)
    {
        var source = MvpWorldCatalog.CreateMission(worldId);
        if (!Blueprints.TryGetValue(worldId, out var blueprints))
        {
            source.StageNumber = 1;
            source.StageTitle = source.DisplayName;
            source.StageSubtitle = source.WorldDisplayName;
            source.IsFinalStage = true;
            source.RequiresFormSwapToComplete = !string.IsNullOrWhiteSpace(source.BossFormId);
            return new WorldRunData
            {
                WorldId = worldId,
                DisplayName = source.WorldDisplayName,
                Stages = new List<StageMissionData> { source },
            };
        }

        var hordes = source.Encounters
            .Where(encounter => encounter.Kind == StageEncounterKind.Horde)
            .Take(6)
            .ToArray();
        var finalBoss = source.Encounters.LastOrDefault(
            encounter => encounter.Kind == StageEncounterKind.Boss);
        if (hordes.Length != 6 || finalBoss is null)
        {
            throw new InvalidOperationException(
                $"World '{worldId}' must contain six horde encounters followed by one boss before it can form a ladder.");
        }

        var stages = new List<StageMissionData>(4);
        for (var stageIndex = 0; stageIndex < 3; stageIndex++)
        {
            var firstSource = hordes[stageIndex * 2];
            var secondSource = hordes[stageIndex * 2 + 1];
            var sourceStartX = firstSource.TriggerX - 12.0f;
            var first = Clone(firstSource);
            var second = Clone(secondSource);
            ShiftEncounter(first, -sourceStartX);
            ShiftEncounter(second, -sourceStartX);
            first.RouteChoices.Clear();
            second.RouteChoices.Clear();

            var eliteArenaMin = second.ArenaMaxX + 2.0f;
            var elite = CreateEliteEncounter(
                source,
                blueprints[stageIndex],
                stageIndex + 1,
                eliteArenaMin);
            AuthorStageSetPiece(source.WorldId, stageIndex, first, second, elite);
            var stageMaxX = elite.ArenaMaxX + 3.0f;
            var stage = CreateStageShell(
                source,
                blueprints[stageIndex],
                stageIndex + 1,
                sourceStartX,
                sourceStartX + stageMaxX,
                stageMaxX,
                isFinal: false);
            stage.Encounters = new List<StageEncounterData> { first, second, elite };
            stages.Add(stage);
        }

        var finalSourceStartX = finalBoss.TriggerX - 14.0f;
        var finalEncounter = Clone(finalBoss);
        ShiftEncounter(finalEncounter, -finalSourceStartX);
        finalEncounter.RouteChoices.Clear();
        var finalStageMaxX = MathF.Max(26.0f, finalEncounter.ArenaMaxX + 3.0f);
        var finalStage = CreateStageShell(
            source,
            blueprints[3],
            stageNumber: 4,
            sourceStartX: finalSourceStartX,
            sourceEndX: finalSourceStartX + finalStageMaxX,
            stageMaxX: finalStageMaxX,
            isFinal: true);
        finalStage.BossFormId = source.BossFormId;
        finalStage.Encounters = new List<StageEncounterData> { finalEncounter };
        if (finalStage.ArenaPresentation is not null)
        {
            finalStage.ArenaPresentation.CenterX = finalEncounter.CameraLockX;
            if (finalStage.FullFramePlates.Count == 1)
            {
                finalStage.FullFramePlates[0].CenterX = finalEncounter.CameraLockX;
            }
        }
        stages.Add(finalStage);

        return new WorldRunData
        {
            WorldId = source.WorldId,
            DisplayName = source.WorldDisplayName,
            Stages = stages,
        };
    }

    private static StageMissionData CreateStageShell(
        StageMissionData source,
        StageBlueprint blueprint,
        int stageNumber,
        float sourceStartX,
        float sourceEndX,
        float stageMaxX,
        bool isFinal)
    {
        var stage = Clone(source);
        stage.Id = $"{source.Id}_stage_{stageNumber}";
        stage.DisplayName = blueprint.Title;
        stage.StageNumber = stageNumber;
        stage.StageTitle = blueprint.Title;
        stage.StageSubtitle = blueprint.Subtitle;
        stage.ParTimeSeconds = blueprint.ParTimeSeconds;
        var thresholdScale = isFinal ? 1.55f : 1.0f + (stageNumber - 1) * 0.12f;
        stage.RankScoreS = (int)(20000 * thresholdScale);
        stage.RankScoreA = (int)(15000 * thresholdScale);
        stage.RankScoreB = (int)(10000 * thresholdScale);
        stage.RankScoreC = (int)(6000 * thresholdScale);
        stage.StageIntroFrames = 108;
        stage.StageIntroReadyFrame = 72;
        stage.IsFinalStage = isFinal;
        stage.RequiresFormSwapToComplete = isFinal;
        stage.BossFormId = isFinal ? source.BossFormId : "";
        stage.StageMinX = 0.0f;
        stage.StageMaxX = stageMaxX;
        stage.BackgroundPanels = source.BackgroundPanels
            .Where(panel => panel.MaxX >= sourceStartX && panel.MinX <= sourceEndX)
            .Select(panel =>
            {
                var clone = Clone(panel);
                clone.MinX -= sourceStartX;
                clone.MaxX -= sourceStartX;
                return clone;
            })
            .ToList();
        stage.CompositeSegments = source.CompositeSegments
            .Where(segment =>
                segment.MaxX >= sourceStartX
                && segment.MinX <= sourceEndX)
            .Select(segment =>
            {
                var clone = Clone(segment);
                clone.MinX -= sourceStartX;
                clone.MaxX -= sourceStartX;
                return clone;
            })
            .ToList();
        ApplyBespokeStageArt(stage);
        stage.Encounters.Clear();
        return stage;
    }

    /// <summary>
    /// Frame row the belt centre sits on for layered stages. Matches the row the
    /// scene-plate stages solve to, so fighters stand at the same height across
    /// the game and the restyled backdrops keep the upper frame.
    /// </summary>
    private const float LayeredGroundLineFraction = 0.775f;

    private static void ApplyBespokeStageArt(StageMissionData stage)
    {
        if (stage.WorldId == "astral_battlefront")
        {
            if (stage.StageNumber == 1)
            {
                ApplySkyfallBreachTraversalProductionArt(stage);
            }
            else if (stage.StageNumber == 2)
            {
                ApplyCapsuleCausewayTraversalProductionArt(stage);
            }
            else if (stage.StageNumber == 3)
            {
                ApplyEnergyRailTraversalProductionArt(stage);
            }
            else if (stage.StageNumber == 4)
            {
                ApplyTournamentSummitProductionArt(stage);
            }

            return;
        }

        if (stage.WorldId == "world_warrior_sector")
        {
            if (stage.StageNumber == 1)
            {
                ApplyDojoApproachProductionArt(stage);
            }
            else if (stage.StageNumber == 2)
            {
                ApplyPavilionCircuitProductionArt(stage);
            }
            else if (stage.StageNumber == 3)
            {
                ApplyGrandTournamentFloorProductionArt(stage);
            }
            else if (stage.StageNumber == 4)
            {
                ApplyChampionsCourtyardProductionArt(stage);
            }

            return;
        }

        if (stage.WorldId != "archive_nexus")
        {
            return;
        }

        if (stage.StageNumber == 1)
        {
            ApplyIntakeBoulevardProductionArt(stage);
            return;
        }

        if (stage.StageNumber == 2)
        {
            ApplyIndexVaultsProductionArt(stage);
            return;
        }

        if (stage.StageNumber == 3)
        {
            ApplyCorruptionRepositoryProductionArt(stage);
            return;
        }

        if (stage.StageNumber == 4)
        {
            ApplyKnightsReliquaryProductionArt(stage);
            return;
        }

        var artStem = stage.StageNumber switch
        {
            4 => "archive_knights_reliquary",
            _ => "",
        };
        if (string.IsNullOrEmpty(artStem))
        {
            return;
        }

        stage.StageTexturePath =
            $"res://Assets/Stages/ArchiveDistrict/{artStem}_backdrop_higgsfield_v1.png";
        stage.FloorTexturePath =
            $"res://Assets/Stages/ArchiveDistrict/{artStem}_floor_higgsfield_v1.png";
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = stage.StageNumber == 4 ? 26.0f : 24.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = stage.StageTexturePath,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 4.55f,
                PositionZ = -6.0f,
                ScaleYMultiplier = stage.StageNumber == 4 ? 0.90f : 0.95f,
            },
        };
    }

    private static void ApplyDojoApproachProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = WorldWarriorDojoBackdropTexture;
        stage.FloorTexturePath = WorldWarriorDojoFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = WorldWarriorDojoBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                GroundLineFraction = 0.748f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorDojoMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 0.94f,
                ScaleYMultiplier = 0.52f,
                CropTopFraction = 0.044f,
                CropBottomFraction = 0.404f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorDojoForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 4.6f,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
            new()
            {
                TexturePath = WorldWarriorDojoForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 4.6f,
                MaxX = stage.StageMaxX,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
        };
    }

    private static void ApplyPavilionCircuitProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = WorldWarriorPavilionBackdropTexture;
        stage.FloorTexturePath = WorldWarriorPavilionFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = WorldWarriorPavilionBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                GroundLineFraction = 0.954f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorPavilionMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 1.0f,
                // The painted props carry mid-range alpha across their stone
                // plinths and drum emblems, so the deck wall showed through
                // them even at full panel opacity.
                AlphaSolidify = 0.65f,
                ScaleYMultiplier = 0.26f,
                CropTopFraction = 0.130f,
                CropBottomFraction = 0.308f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorPavilionForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 4.6f,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
            new()
            {
                TexturePath = WorldWarriorPavilionForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 4.6f,
                MaxX = stage.StageMaxX,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
        };
    }

    private static void ApplyGrandTournamentFloorProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = WorldWarriorGrandTournamentBackdropTexture;
        stage.FloorTexturePath = WorldWarriorGrandTournamentFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 12.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = WorldWarriorGrandTournamentBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                GroundLineFraction = 0.977f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorGrandTournamentMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 1.0f,
                ScaleYMultiplier = 0.26f,
                CropTopFraction = 0.108f,
                CropBottomFraction = 0.300f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = WorldWarriorGrandTournamentForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 4.8f,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
            new()
            {
                TexturePath = WorldWarriorGrandTournamentForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 4.8f,
                MaxX = stage.StageMaxX,
                PositionY = 0.85f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.32f,
            },
        };
    }

    private static void ApplyChampionsCourtyardProductionArt(StageMissionData stage)
    {
        stage.PresentationMode = StagePresentationMode.FullFramePlates;
        stage.StageTexturePath = WorldWarriorChampionsCourtyardFullFramePlate;
        stage.FloorTexturePath = WorldWarriorChampionsCourtyardFullFramePlate;
        stage.FloorTextureTopFraction = 0.0f;
        stage.CameraBaseSize = 9.0f;
        stage.CameraMaxSize = 9.0f;
        stage.CameraCinematicSize = 7.6f;
        stage.BackgroundPanels.Clear();
        stage.CompositeSegments.Clear();
        stage.FullFramePlateTransitionWidth = 2.0f;
        stage.FullFramePlates = new List<StageFullFramePlateData>
        {
            new()
            {
                TexturePath = WorldWarriorChampionsCourtyardFullFramePlate,
                CenterX = 0.0f,
                PositionZ = -6.0f,
                GroundFarFraction = 0.715f,
                GroundNearFraction = 0.985f,
            },
        };
        stage.ArenaPresentation = new StageArenaPresentationData
        {
            BackdropTexturePath = WorldWarriorChampionsCourtyardFullFramePlate,
            FloorTexturePath = WorldWarriorChampionsCourtyardFullFramePlate,
            BackdropWorldWidth = 16.0f,
            BackdropPositionZ = -6.0f,
            FloorWorldSize = 22.0f,
            FloorNearZ = 6.0f,
            CameraLookHeight = 3.0f,
            CameraSize = 9.0f,
            CameraMaxSize = 9.0f,
            CinematicCameraSize = 7.6f,
        };
    }

    private static void ApplyIntakeBoulevardProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = ArchiveIntakeBoulevardBackdropTexture;
        stage.FloorTexturePath = ArchiveIntakeBoulevardFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = ArchiveIntakeBoulevardBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                CropBottomFraction = 0.041f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveIntakeBoulevardMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 0.95f,
                ScaleYMultiplier = 0.58f,
                CropTopFraction = 0.077f,
                CropBottomFraction = 0.200f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveIntakeBoulevardForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 5.0f,
                PositionY = 1.15f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.70f,
                ScaleYMultiplier = 0.40f,
            },
            new()
            {
                TexturePath = ArchiveIntakeBoulevardForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 5.0f,
                MaxX = stage.StageMaxX,
                PositionY = 1.15f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.70f,
                ScaleYMultiplier = 0.40f,
            },
        };
    }

    private static void ApplyIndexVaultsProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = ArchiveIndexVaultsBackdropTexture;
        stage.FloorTexturePath = ArchiveIndexVaultsFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = ArchiveIndexVaultsBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveIndexVaultsMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 0.95f,
                ScaleYMultiplier = 0.56f,
                CropTopFraction = 0.008f,
                CropBottomFraction = 0.264f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveIndexVaultsForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 5.5f,
                PositionY = 1.3f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.72f,
                ScaleYMultiplier = 0.42f,
            },
            new()
            {
                TexturePath = ArchiveIndexVaultsForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 5.5f,
                MaxX = stage.StageMaxX,
                PositionY = 1.3f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.72f,
                ScaleYMultiplier = 0.42f,
            },
        };
    }

    private static void ApplyCorruptionRepositoryProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = ArchiveCorruptionRepositoryBackdropTexture;
        stage.FloorTexturePath = ArchiveCorruptionRepositoryFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = ArchiveCorruptionRepositoryBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveCorruptionRepositoryMidgroundTexture,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 0.95f,
                ScaleYMultiplier = 0.58f,
                CropTopFraction = 0.054f,
                CropBottomFraction = 0.273f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveCorruptionRepositoryForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 5.0f,
                PositionY = 1.05f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.70f,
                ScaleYMultiplier = 0.36f,
            },
            new()
            {
                TexturePath = ArchiveCorruptionRepositoryForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 5.0f,
                MaxX = stage.StageMaxX,
                PositionY = 1.05f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.70f,
                ScaleYMultiplier = 0.36f,
            },
        };
    }

    private static void ApplyKnightsReliquaryProductionArt(StageMissionData stage)
    {
        stage.GroundLineCenterFraction = LayeredGroundLineFraction;
        stage.StageTexturePath = ArchiveKnightsReliquaryBackdropTexture;
        stage.FloorTexturePath = ArchiveKnightsReliquaryFloorTexture;
        stage.FloorTextureTopFraction = 0.0f;
        stage.FloorTextureTileWidth = 9.0f;
        stage.BackgroundPanels = new List<StageBackgroundPanelData>
        {
            new()
            {
                TexturePath = ArchiveKnightsReliquaryBackdropTexture,
                Layer = StageVisualLayerKind.Far,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -6.0f,
                ParallaxFactorX = 0.72f,
                ScaleYMultiplier = 0.40f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveKnightsReliquaryMidgroundTexture,
                DestructionTexturePaths = new List<string>
                {
                    ArchiveReliquaryPhase2MidgroundTexture,
                    ArchiveReliquaryPhase3MidgroundTexture,
                },
                DestructionBurstTexturePaths = new List<string>
                {
                    ArchiveReliquaryPhase2BurstTexture,
                    ArchiveReliquaryPhase3BurstTexture,
                },
                DestructionBurstAnchorXs = new List<float> { 0.22f, 0.78f },
                DestructionBurstPositionY = 1.2f,
                DestructionBurstPositionZ = -4.1f,
                DestructionBurstPixelSize = 0.0040f,
                DestructionOverlayPositionY = 1.45f,
                DestructionOverlayPositionZ = -4.3f,
                DestructionOverlayPixelSize = 0.0055f,
                Layer = StageVisualLayerKind.Midground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMaxX,
                PositionY = 0.0f,
                PositionZ = -4.8f,
                ParallaxFactorX = 0.90f,
                Opacity = 0.95f,
                ScaleYMultiplier = 0.52f,
                CropTopFraction = 0.031f,
                CropBottomFraction = 0.320f,
                AlignBottomToFloor = true,
                RepeatHorizontally = true,
            },
            new()
            {
                TexturePath = ArchiveKnightsReliquaryForegroundLeftTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMinX,
                MaxX = stage.StageMinX + 4.8f,
                PositionY = 1.0f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.34f,
            },
            new()
            {
                TexturePath = ArchiveKnightsReliquaryForegroundRightTexture,
                Layer = StageVisualLayerKind.Foreground,
                MinX = stage.StageMaxX - 4.8f,
                MaxX = stage.StageMaxX,
                PositionY = 1.0f,
                PositionZ = 4.2f,
                ParallaxFactorX = 1.12f,
                Opacity = 0.68f,
                ScaleYMultiplier = 0.34f,
            },
        };
    }

    private static StageEncounterData CreateEliteEncounter(
        StageMissionData source,
        StageBlueprint blueprint,
        int stageNumber,
        float arenaMinX)
    {
        const float arenaWidth = 14.0f;
        var triggerX = arenaMinX + 6.0f;
        return new StageEncounterData
        {
            Id = $"{source.WorldId}_stage_{stageNumber}_elite",
            DisplayName = blueprint.EliteName,
            Kind = StageEncounterKind.Elite,
            TriggerX = triggerX,
            ArenaMinX = arenaMinX,
            ArenaMaxX = arenaMinX + arenaWidth,
            CameraLockX = arenaMinX + arenaWidth * 0.56f,
            CameraOrthographicSize = 8.8f,
            LocksFightRoom = true,
            CameraTransitionFrames = 24,
            GateReleaseDelayFrames = 40,
            MaxActiveEnemies = 1,
            MaxSimultaneousAttackers = 1,
            MaxAttackersPerPlayer = 1,
            LightColorR = 1.0f,
            LightColorG = 0.78f,
            LightColorB = 0.34f,
            LightEnergyMultiplier = 1.16f,
            Spawns = new List<EnemySpawnData>
            {
                new()
                {
                    ArchetypeId = blueprint.EliteArchetypeId,
                    DisplayName = blueprint.EliteName,
                    EntryEdge = EnemyEntryEdge.Right,
                    EntryProfile = EnemyEntryProfile.WalkIn,
                    WarningLeadFrames = 35,
                    EntryDistance = 2.2f,
                    OffsetX = 0.0f,
                    LaneZ = 0.0f,
                },
            },
        };
    }

    private static void ShiftEncounter(StageEncounterData encounter, float deltaX)
    {
        encounter.TriggerX += deltaX;
        encounter.ArenaMinX += deltaX;
        encounter.ArenaMaxX += deltaX;
        encounter.CameraLockX += deltaX;
        foreach (var prop in encounter.Props)
        {
            prop.PositionX += deltaX;
        }

        foreach (var zone in encounter.HazardZones)
        {
            zone.MinX += deltaX;
            zone.MaxX += deltaX;
        }
    }

    private static void ApplySkyfallBreachTraversalProductionArt(
        StageMissionData stage)
    {
        ApplyAstralScenePlate(stage, AstralSkyfallBreachFullFramePlate01);
    }

    private static void ApplyTournamentSummitProductionArt(
        StageMissionData stage)
    {
        ApplyAstralScenePlate(stage, AstralTournamentSummitFullFramePlate01);
    }

    private static void ApplyCapsuleCausewayTraversalProductionArt(
        StageMissionData stage)
    {
        ApplyAstralScenePlate(stage, AstralCapsuleCausewayFullFramePlate01);
    }

    /// <summary>
    /// Points an Astral stage at the one complete scene painting authored for
    /// it. The declared band is the paved plaza every Astral route master
    /// shares, measured from the plate's lower half.
    /// </summary>
    private static void ApplyAstralScenePlate(
        StageMissionData stage,
        string platePath)
    {
        stage.PresentationMode = StagePresentationMode.FullFramePlates;
        stage.StageTexturePath = platePath;
        stage.FloorTexturePath = platePath;
        stage.FloorTextureTopFraction = 0.0f;
        stage.BackgroundPanels.Clear();
        stage.CompositeSegments.Clear();
        stage.FullFramePlateTransitionWidth = 2.0f;
        stage.FullFramePlates = new List<StageFullFramePlateData>
        {
            new()
            {
                TexturePath = platePath,
                CenterX = (stage.StageMinX + stage.StageMaxX) * 0.5f,
                PositionZ = -6.0f,
                GroundFarFraction = 0.695f,
                GroundNearFraction = 0.855f,
            },
        };
    }

    private static void ApplyEnergyRailTraversalProductionArt(
        StageMissionData stage)
    {
        stage.PresentationMode = StagePresentationMode.FullFramePlates;
        stage.StageTexturePath = AstralEnergyRailFullFramePlate01;
        stage.FloorTexturePath = AstralEnergyRailFullFramePlate01;
        stage.FloorTextureTopFraction = 0.0f;
        stage.BackgroundPanels.Clear();
        stage.CompositeSegments.Clear();
        stage.FullFramePlateTransitionWidth = 2.0f;
        stage.FullFramePlates = new List<StageFullFramePlateData>
        {
            new()
            {
                TexturePath = AstralEnergyRailFullFramePlate01,
                CenterX = (stage.StageMinX + stage.StageMaxX) * 0.5f,
                PositionZ = -6.0f,
                GroundFarFraction = 0.695f,
                GroundNearFraction = 0.855f,
            },
        };
    }

    private static void AuthorStageSetPiece(
        string worldId,
        int stageIndex,
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        if (worldId == "world_warrior_sector")
        {
            if (stageIndex == 0)
            {
                AuthorDojoApproachTrainingProp(firstEncounter);
                AuthorDojoApproachSupplyCrate(secondEncounter);
            }
            else if (stageIndex == 1)
            {
                AuthorPavilionCircuitTrainingProp(firstEncounter);
                AuthorPavilionCircuitRackChest(secondEncounter);
            }
            else if (stageIndex == 2)
            {
                AuthorGrandTournamentTrainingProp(firstEncounter);
                AuthorGrandTournamentTrophyPodium(secondEncounter);
            }

            return;
        }

        if (worldId != "archive_nexus")
        {
            return;
        }

        if (stageIndex == 1)
        {
            AuthorIndexVaults(firstEncounter, secondEncounter, eliteEncounter);
            return;
        }

        if (stageIndex == 2)
        {
            AuthorCorruptionRepository(firstEncounter, secondEncounter, eliteEncounter);
            return;
        }

        if (stageIndex != 0)
        {
            return;
        }

        eliteEncounter.LightColorR = 0.72f;
        eliteEncounter.LightColorG = 0.92f;
        eliteEncounter.LightColorB = 1.0f;
        eliteEncounter.LightEnergyMultiplier = 1.08f;

        firstEncounter.Props.Add(new StagePropData
        {
            Id = "archive_intake_health_cache",
            PositionX = firstEncounter.ArenaMaxX - 2.2f,
            PositionZ = 2.15f,
            Health = 80,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "archive_intake_data_cache",
            PositionX = secondEncounter.ArenaMinX + 2.2f,
            PositionZ = -2.15f,
            Health = 95,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = ArchiveDataCacheSprite,
            SpritePixelSize = ArchiveDataCachePixelSize,
            SpriteGroundOffsetPixels = 674.0f,
        });

        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "archive_intake_tram",
            Behavior = StageHazardBehavior.LinearSweep,
            Targets = StageHazardTargetMask.All,
            SpritePath = ArchiveIntakeTramSprite,
            SpritePixelSize = ArchiveIntakeTramPixelSize,
            SpriteGroundOffsetPixels = 419.5f,
            MinX = secondEncounter.ArenaMinX + 0.6f,
            MaxX = secondEncounter.ArenaMinX + 4.6f,
            MinZ = -0.72f,
            MaxZ = 0.72f,
            ActivationDelayFrames = 24,
            WarningLeadFrames = 72,
            ActiveFrames = 96,
            RepeatIntervalFrames = 264,
            MovementOffsetX = 8.0f,
            WarningText = "INTAKE TRAM — CLEAR THE CENTER LANE",
            DamagePerSecond = 90.0f,
            KnockbackX = 9.0f,
            HitstunFrames = 18,
            ActiveDuringBoss = false,
        });
    }

    private static void AuthorDojoApproachTrainingProp(
        StageEncounterData firstEncounter)
    {
        firstEncounter.Props.Add(new StagePropData
        {
            Id = "dojo_approach_training_dummy",
            ArchetypeId = "world_warrior_training_dummy",
            PositionX = firstEncounter.ArenaMinX + 2.2f,
            PositionZ = 2.15f,
            Health = 90,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = WorldWarriorTrainingDummySprite,
            SpritePixelSize = WorldWarriorTrainingDummyPixelSize,
            SpriteGroundOffsetPixels = 968.0f,
        });
    }

    private static void AuthorDojoApproachSupplyCrate(
        StageEncounterData secondEncounter)
    {
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "dojo_approach_supply_crate",
            ArchetypeId = "world_warrior_supply_crate",
            PositionX = secondEncounter.ArenaMinX + 2.6f,
            PositionZ = -1.85f,
            Health = 70,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = WorldWarriorSupplyCrateSprite,
            SpritePixelSize = WorldWarriorSupplyCratePixelSize,
            SpriteGroundOffsetPixels = 848.0f,
        });
    }

    private static void AuthorPavilionCircuitTrainingProp(
        StageEncounterData firstEncounter)
    {
        firstEncounter.Props.Add(new StagePropData
        {
            Id = "pavilion_circuit_focus_dummy",
            ArchetypeId = "world_warrior_training_dummy",
            PositionX = firstEncounter.ArenaMinX + 2.2f,
            PositionZ = -2.15f,
            Health = 105,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = WorldWarriorTrainingDummySprite,
            SpritePixelSize = WorldWarriorTrainingDummyPixelSize,
            SpriteGroundOffsetPixels = 968.0f,
        });
    }

    private static void AuthorPavilionCircuitRackChest(
        StageEncounterData secondEncounter)
    {
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "pavilion_circuit_rack_chest",
            ArchetypeId = "world_warrior_pavilion_rack_chest",
            PositionX = secondEncounter.ArenaMinX + 2.6f,
            PositionZ = -1.85f,
            Health = 85,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = WorldWarriorPavilionRackChestSprite,
            SpritePixelSize = WorldWarriorPavilionRackChestPixelSize,
            SpriteGroundOffsetPixels = 985.0f,
        });
    }

    private static void AuthorGrandTournamentTrainingProp(
        StageEncounterData firstEncounter)
    {
        firstEncounter.Props.Add(new StagePropData
        {
            Id = "grand_tournament_honor_dummy",
            ArchetypeId = "world_warrior_training_dummy",
            PositionX = firstEncounter.ArenaMinX + 2.2f,
            PositionZ = 2.15f,
            Health = 120,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = WorldWarriorTrainingDummySprite,
            SpritePixelSize = WorldWarriorTrainingDummyPixelSize,
            SpriteGroundOffsetPixels = 968.0f,
        });
    }

    private static void AuthorGrandTournamentTrophyPodium(
        StageEncounterData secondEncounter)
    {
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "grand_tournament_trophy_podium",
            ArchetypeId = "world_warrior_grand_tournament_trophy_podium",
            PositionX = secondEncounter.ArenaMinX + 2.6f,
            PositionZ = -1.85f,
            Health = 95,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = WorldWarriorGrandTournamentTrophyPodiumSprite,
            SpritePixelSize = WorldWarriorGrandTournamentTrophyPodiumPixelSize,
            SpriteGroundOffsetPixels = 887.0f,
        });
    }

    private static void AuthorIndexVaults(
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        eliteEncounter.LightColorR = 0.86f;
        eliteEncounter.LightColorG = 0.92f;
        eliteEncounter.LightColorB = 1.0f;
        eliteEncounter.LightEnergyMultiplier = 1.10f;

        // Compressed catalog stacks teach close-range crowd control, then open
        // into a wider scan chamber with risky cache pockets.
        firstEncounter.UsesLaneBounds = true;
        firstEncounter.LaneMinZ = -1.65f;
        firstEncounter.LaneMaxZ = 1.65f;
        firstEncounter.LaneTransitionFrames = 30;
        secondEncounter.UsesLaneBounds = true;
        secondEncounter.LaneMinZ = -2.75f;
        secondEncounter.LaneMaxZ = 2.75f;
        secondEncounter.LaneTransitionFrames = 30;
        eliteEncounter.UsesLaneBounds = true;
        eliteEncounter.LaneMinZ = -2.40f;
        eliteEncounter.LaneMaxZ = 2.40f;
        eliteEncounter.LaneTransitionFrames = 30;

        secondEncounter.Props.Add(new StagePropData
        {
            Id = "index_vault_health_cache",
            PositionX = secondEncounter.ArenaMinX + 2.2f,
            PositionZ = 2.35f,
            Health = 105,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "index_vault_meter_cache",
            PositionX = secondEncounter.ArenaMaxX - 2.2f,
            PositionZ = -2.35f,
            Health = 105,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = ArchiveMeterCacheSprite,
            SpritePixelSize = ArchiveMeterCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });

        // Alternating far/near scan strips never overlap. A center corridor is
        // always safe, while the off-window permits deliberate cache access.
        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "index_vault_far_scan",
            Behavior = StageHazardBehavior.StaticPulse,
            Targets = StageHazardTargetMask.All,
            SpritePath = ArchiveIndexScanEmitterSprite,
            SpritePixelSize = ArchiveIndexScanEmitterPixelSize,
            SpriteGroundOffsetPixels = 935.0f,
            SpriteAnchorX = 0.08f,
            SpriteAnchorZ = 0.5f,
            FieldTexturePath = ArchiveIndexScanFieldTexture,
            MinX = secondEncounter.ArenaMinX + 0.8f,
            MaxX = secondEncounter.ArenaMaxX - 0.8f,
            MinZ = -2.55f,
            MaxZ = -0.45f,
            ActivationDelayFrames = 20,
            WarningLeadFrames = 60,
            ActiveFrames = 90,
            RepeatIntervalFrames = 340,
            WarningText = "INDEX SCAN — FAR LANE CHARGING",
            DamagePerSecond = 72.0f,
            KnockbackZ = 4.0f,
            HitstunFrames = 14,
            ActiveDuringBoss = false,
        });
        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "index_vault_near_scan",
            Behavior = StageHazardBehavior.StaticPulse,
            Targets = StageHazardTargetMask.All,
            SpritePath = ArchiveIndexScanEmitterSprite,
            SpritePixelSize = ArchiveIndexScanEmitterPixelSize,
            SpriteGroundOffsetPixels = 935.0f,
            SpriteAnchorX = 0.92f,
            SpriteAnchorZ = 0.5f,
            SpriteFlipH = true,
            FieldTexturePath = ArchiveIndexScanFieldTexture,
            FieldFlipH = true,
            MinX = secondEncounter.ArenaMinX + 0.8f,
            MaxX = secondEncounter.ArenaMaxX - 0.8f,
            MinZ = 0.45f,
            MaxZ = 2.55f,
            ActivationDelayFrames = 110,
            WarningLeadFrames = 60,
            ActiveFrames = 90,
            RepeatIntervalFrames = 340,
            WarningText = "INDEX SCAN — NEAR LANE CHARGING",
            DamagePerSecond = 72.0f,
            KnockbackZ = -4.0f,
            HitstunFrames = 14,
            ActiveDuringBoss = false,
        });

        var eliteSpawn = eliteEncounter.Spawns.Single();
        eliteSpawn.EntryEdge = EnemyEntryEdge.FarLane;
        eliteSpawn.EntryProfile = EnemyEntryProfile.Background;
        eliteSpawn.LaneZ = -1.85f;
        eliteSpawn.EntryDistance = 2.6f;
        eliteSpawn.WarningLeadFrames = 48;
    }

    private static void AuthorCorruptionRepository(
        StageEncounterData firstEncounter,
        StageEncounterData secondEncounter,
        StageEncounterData eliteEncounter)
    {
        eliteEncounter.LightColorR = 0.92f;
        eliteEncounter.LightColorG = 0.80f;
        eliteEncounter.LightColorB = 1.0f;
        eliteEncounter.LightEnergyMultiplier = 1.12f;

        firstEncounter.UsesLaneBounds = true;
        firstEncounter.LaneMinZ = -2.60f;
        firstEncounter.LaneMaxZ = 2.60f;
        firstEncounter.LaneTransitionFrames = 28;
        secondEncounter.UsesLaneBounds = true;
        secondEncounter.LaneMinZ = -2.30f;
        secondEncounter.LaneMaxZ = 2.30f;
        secondEncounter.LaneTransitionFrames = 28;
        eliteEncounter.UsesLaneBounds = true;
        eliteEncounter.LaneMinZ = -2.55f;
        eliteEncounter.LaneMaxZ = 2.55f;
        eliteEncounter.LaneTransitionFrames = 30;

        firstEncounter.Props.Add(new StagePropData
        {
            Id = "repository_data_cache",
            PositionX = firstEncounter.ArenaMinX + 1.8f,
            PositionZ = 2.25f,
            Health = 115,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Score,
            DropChance = 1.0f,
            SpritePath = ArchiveDataCacheSprite,
            SpritePixelSize = ArchiveDataCachePixelSize,
            SpriteGroundOffsetPixels = 674.0f,
        });
        firstEncounter.Props.Add(new StagePropData
        {
            Id = "repository_volatile_canister",
            ArchetypeId = "prop_explosive_canister",
            PositionX = firstEncounter.ArenaMaxX - 2.4f,
            PositionZ = -1.25f,
            Health = 72,
            IsThrowable = false,
            SpawnsPickupOnBreak = false,
            ExplodesOnBreak = true,
            ExplosionTargets = StageHazardTargetMask.All,
            ExplosionRadius = 3.2f,
            ExplosionDamage = 55,
            ExplosionKnockback = 8.0f,
            ExplosionHitstunFrames = 18,
            SpritePath = ArchiveVolatileCanisterSprite,
            SpritePixelSize = ArchiveVolatileCanisterPixelSize,
            SpriteGroundOffsetPixels = 854.0f,
            AftermathVisual = CreateRepositoryAftermathVisual(4.0f, 3.4f),
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "repository_health_cache",
            PositionX = secondEncounter.ArenaMinX + 2.0f,
            PositionZ = -2.05f,
            Health = 120,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Health,
            DropChance = 1.0f,
            SpritePath = ArchiveHealthCacheSprite,
            SpritePixelSize = ArchiveHealthCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });
        secondEncounter.Props.Add(new StagePropData
        {
            Id = "repository_meter_cache",
            PositionX = secondEncounter.ArenaMaxX - 2.0f,
            PositionZ = 2.05f,
            Health = 120,
            IsThrowable = false,
            SpawnsPickupOnBreak = true,
            DropType = StagePickupType.Meter,
            DropChance = 1.0f,
            SpritePath = ArchiveMeterCacheSprite,
            SpritePixelSize = ArchiveMeterCachePixelSize,
            SpriteGroundOffsetPixels = 788.0f,
        });

        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_far",
            centerX: firstEncounter.ArenaMinX + 3.0f,
            centerZ: -1.55f,
            delay: 20,
            warning: "FALLING SHELF — FAR POCKET",
            spritePath: ArchiveRepositoryFallingShelfSprite,
            spritePixelSize: ArchiveRepositoryFallingShelfPixelSize,
            spriteGroundOffsetPixels: 546.5f);
        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_near",
            centerX: firstEncounter.ArenaMinX + 8.0f,
            centerZ: 1.50f,
            delay: 65,
            warning: "FALLING SHELF — NEAR POCKET",
            spritePath: ArchiveRepositoryFallingShelfSprite,
            spritePixelSize: ArchiveRepositoryFallingShelfPixelSize,
            spriteGroundOffsetPixels: 546.5f,
            spriteFlipH: true);
        AddRepositoryFallingStrike(
            firstEncounter,
            "repository_debris_center",
            centerX: firstEncounter.ArenaMaxX - 2.4f,
            centerZ: 0.0f,
            delay: 110,
            warning: "DATA DEBRIS — CENTER IMPACT",
            spritePath: ArchiveRepositoryDataDebrisSprite,
            spritePixelSize: ArchiveRepositoryDataDebrisPixelSize,
            spriteGroundOffsetPixels: 747.0f);

        secondEncounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = "repository_security_sweep",
            Behavior = StageHazardBehavior.LinearSweep,
            Targets = StageHazardTargetMask.All,
            SpritePath = ArchiveRepositorySecuritySweepSprite,
            SpritePixelSize = ArchiveRepositorySecuritySweepPixelSize,
            SpriteGroundOffsetPixels = 300.5f,
            SpriteAnchorX = 0.85f,
            SpriteFlipH = true,
            MinX = secondEncounter.ArenaMinX + 0.7f,
            MaxX = secondEncounter.ArenaMinX + 2.5f,
            MinZ = -0.70f,
            MaxZ = 0.70f,
            ActivationDelayFrames = 30,
            WarningLeadFrames = 54,
            ActiveFrames = 90,
            RepeatIntervalFrames = 260,
            MovementOffsetX = 10.8f,
            WarningText = "CORRUPTION SWEEP — CLEAR THE CENTER",
            DamagePerSecond = 86.0f,
            KnockbackX = 9.0f,
            HitstunFrames = 19,
            ActiveDuringBoss = false,
        });

        var eliteSpawn = eliteEncounter.Spawns.Single();
        eliteSpawn.EntryEdge = EnemyEntryEdge.FarLane;
        eliteSpawn.EntryProfile = EnemyEntryProfile.DropIn;
        eliteSpawn.LaneZ = 0.0f;
        eliteSpawn.EntryHeight = 5.4f;
        eliteSpawn.WarningLeadFrames = 48;
    }

    private static void AddRepositoryFallingStrike(
        StageEncounterData encounter,
        string id,
        float centerX,
        float centerZ,
        int delay,
        string warning,
        string spritePath,
        float spritePixelSize,
        float spriteGroundOffsetPixels,
        bool spriteFlipH = false)
    {
        encounter.HazardZones.Add(new StageHazardZoneData
        {
            Id = id,
            Behavior = StageHazardBehavior.FallingStrike,
            Targets = StageHazardTargetMask.All,
            SpritePath = spritePath,
            SpritePixelSize = spritePixelSize,
            SpriteGroundOffsetPixels = spriteGroundOffsetPixels,
            SpriteTravelHeight = 4.4f,
            SpriteFlipH = spriteFlipH,
            AftermathVisual = CreateRepositoryAftermathVisual(
                2.6f,
                2.2f,
                spriteFlipH),
            MinX = centerX - 1.2f,
            MaxX = centerX + 1.2f,
            MinZ = centerZ - 1.0f,
            MaxZ = centerZ + 1.0f,
            ActivationDelayFrames = delay,
            WarningLeadFrames = 45,
            ActiveFrames = 20,
            RepeatIntervalFrames = 270,
            WarningText = warning,
            DamagePerSecond = 140.0f,
            KnockbackX = 5.5f,
            HitstunFrames = 24,
            ActiveDuringBoss = false,
        });
    }

    private static StageAftermathVisualData CreateRepositoryAftermathVisual(
        float decalSizeX,
        float decalSizeZ,
        bool fragmentFlipH = false)
    {
        return new StageAftermathVisualData
        {
            DecalTexturePath = ArchiveRepositoryExplosionDecalTexture,
            DecalSizeX = decalSizeX,
            DecalSizeZ = decalSizeZ,
            DecalOpacity = 0.72f,
            FragmentSpritePath = ArchiveRepositoryImpactFragmentsSprite,
            FragmentPixelSize = 0.00068f,
            FragmentGroundOffsetPixels = 432.5f,
            FragmentOffsetX = 0.22f,
            FragmentOffsetZ = 0.12f,
            FragmentFlipH = fragmentFlipH,
        };
    }

    private static T Clone<T>(T source)
    {
        return JsonSerializer.Deserialize<T>(
                   JsonSerializer.Serialize(source, CloneOptions),
                   CloneOptions)
               ?? throw new InvalidOperationException($"Could not clone {typeof(T).Name}.");
    }

    private const string ArchiveHealthCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_health_cache_style_v2.png";
    private const string WorldWarriorTrainingDummySprite =
        "res://Assets/Sprites/Props/WorldWarrior/world_warrior_training_dummy_style_v1.png";
    private const float WorldWarriorTrainingDummyPixelSize = 0.00147291f;
    private const string WorldWarriorSupplyCrateSprite =
        "res://Assets/Sprites/Props/WorldWarrior/world_warrior_supply_crate_style_v1.png";
    private const float WorldWarriorSupplyCratePixelSize = 0.00079027f;
    private const string WorldWarriorPavilionRackChestSprite =
        "res://Assets/Sprites/Props/WorldWarrior/world_warrior_pavilion_rack_chest_style_v1.png";
    private const float WorldWarriorPavilionRackChestPixelSize = 0.00132653f;
    private const string WorldWarriorGrandTournamentTrophyPodiumSprite =
        "res://Assets/Sprites/Props/WorldWarrior/world_warrior_grand_tournament_trophy_podium_style_v1.png";
    private const float WorldWarriorGrandTournamentTrophyPodiumPixelSize = 0.00133721f;
    private const string ArchiveMeterCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_meter_cache_style_v2.png";
    private const string ArchiveDataCacheSprite =
        "res://Assets/Sprites/Props/Archive/archive_data_cache_style_v2.png";
    private const string ArchiveVolatileCanisterSprite =
        "res://Assets/Sprites/Props/Archive/archive_volatile_canister_style_v2.png";
    private const string ArchiveIntakeTramSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_intake_tram_style_v1.png";
    private const string ArchiveIndexScanEmitterSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_index_scan_emitter_style_v1.png";
    private const string ArchiveIndexScanFieldTexture =
        "res://Assets/Sprites/Hazards/Archive/archive_index_scan_field_style_v1.png";
    private const string ArchiveRepositoryFallingShelfSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_repository_falling_shelf_style_v1.png";
    private const string ArchiveRepositoryDataDebrisSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_repository_data_debris_style_v1.png";
    private const string ArchiveRepositorySecuritySweepSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_repository_security_sweep_style_v1.png";
    private const string ArchiveRepositoryExplosionDecalTexture =
        "res://Assets/Sprites/Hazards/Archive/archive_repository_explosion_decal_style_v1.png";
    private const string ArchiveRepositoryImpactFragmentsSprite =
        "res://Assets/Sprites/Hazards/Archive/archive_repository_impact_fragments_style_v1.png";
    private const string AstralEnergyRailTraversalBackdropChunk01 =
        "res://Assets/Stages/AstralBattlefront/astral_energy_rail_traversal_backdrop_style_v1_01.png";
    private const string AstralEnergyRailTraversalBackdropChunk02 =
        "res://Assets/Stages/AstralBattlefront/astral_energy_rail_traversal_backdrop_style_v1_02.png";
    private const string AstralEnergyRailTraversalBackdropChunk03 =
        "res://Assets/Stages/AstralBattlefront/astral_energy_rail_traversal_backdrop_style_v1_03.png";
    private const string AstralEnergyRailFloorTexture =
        "res://Assets/Stages/AstralBattlefront/astral_energy_rail_floor_style_v1.png";
    private const string AstralSkyfallBreachFullFramePlate01 =
        "res://Assets/Stages/AstralBattlefront/astral_skyfall_breach_full_frame_plate_01.png";
    private const string AstralTournamentSummitFullFramePlate01 =
        "res://Assets/Stages/AstralBattlefront/astral_tournament_summit_full_frame_plate_01.png";
    private const string AstralCapsuleCausewayFullFramePlate01 =
        "res://Assets/Stages/AstralBattlefront/astral_capsule_causeway_full_frame_plate_01.png";
    private const string AstralEnergyRailFullFramePlate01 =
        "res://Assets/Stages/AstralBattlefront/astral_energy_rail_full_frame_plate_01.png";
    private const string ArchiveIndexVaultsBackdropTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_index_vaults_backdrop_style_v2.png";
    private const string ArchiveIndexVaultsFloorTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_index_vaults_floor_style_v2.png";
    private const string ArchiveIndexVaultsMidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_index_vaults_midground_style_v2.png";
    private const string ArchiveIndexVaultsForegroundLeftTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_index_vaults_foreground_left_style_v2.png";
    private const string ArchiveIndexVaultsForegroundRightTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_index_vaults_foreground_right_style_v2.png";
    private const string ArchiveIntakeBoulevardBackdropTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_intake_boulevard_backdrop_style_v1.png";
    private const string ArchiveIntakeBoulevardFloorTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_intake_boulevard_floor_style_v1.png";
    private const string ArchiveIntakeBoulevardMidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_intake_boulevard_midground_style_v1.png";
    private const string ArchiveIntakeBoulevardForegroundLeftTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_intake_boulevard_foreground_left_style_v1.png";
    private const string ArchiveIntakeBoulevardForegroundRightTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_intake_boulevard_foreground_right_style_v1.png";
    private const string ArchiveCorruptionRepositoryBackdropTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_corruption_repository_backdrop_style_v1.png";
    private const string ArchiveCorruptionRepositoryFloorTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_corruption_repository_floor_style_v1.png";
    private const string ArchiveCorruptionRepositoryMidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_corruption_repository_midground_style_v1.png";
    private const string ArchiveCorruptionRepositoryForegroundLeftTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_corruption_repository_foreground_left_style_v1.png";
    private const string ArchiveCorruptionRepositoryForegroundRightTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_corruption_repository_foreground_right_style_v1.png";
    private const string ArchiveKnightsReliquaryBackdropTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_knights_reliquary_backdrop_style_v1.png";
    private const string ArchiveKnightsReliquaryFloorTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_knights_reliquary_floor_style_v1.png";
    private const string ArchiveKnightsReliquaryMidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_knights_reliquary_midground_style_v1.png";
    private const string ArchiveKnightsReliquaryForegroundLeftTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_knights_reliquary_foreground_left_style_v1.png";
    private const string ArchiveKnightsReliquaryForegroundRightTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_knights_reliquary_foreground_right_style_v1.png";
    private const string ArchiveReliquaryPhase2MidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_reliquary_phase2_midground_style_v1.png";
    private const string ArchiveReliquaryPhase3MidgroundTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_reliquary_phase3_midground_style_v1.png";
    private const string ArchiveReliquaryPhase2BurstTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_reliquary_phase2_burst_style_v1.png";
    private const string ArchiveReliquaryPhase3BurstTexture =
        "res://Assets/Stages/ArchiveDistrict/archive_reliquary_phase3_burst_style_v1.png";
    private const string WorldWarriorDojoBackdropTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png";
    private const string WorldWarriorDojoFloorTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_dojo_floor_style_v1.png";
    private const string WorldWarriorDojoMidgroundTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_dojo_midground_style_v1.png";
    private const string WorldWarriorDojoForegroundLeftTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_dojo_foreground_left_style_v1.png";
    private const string WorldWarriorDojoForegroundRightTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_dojo_foreground_right_style_v1.png";
    private const string WorldWarriorPavilionBackdropTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png";
    private const string WorldWarriorPavilionFloorTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v2.png";
    private const string WorldWarriorPavilionMidgroundTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_pavilion_midground_style_v1.png";
    private const string WorldWarriorPavilionForegroundLeftTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_pavilion_foreground_left_style_v1.png";
    private const string WorldWarriorPavilionForegroundRightTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_pavilion_foreground_right_style_v1.png";
    private const string WorldWarriorGrandTournamentBackdropTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_grand_tournament_backdrop_style_v1.png";
    private const string WorldWarriorGrandTournamentFloorTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v2.png";
    private const string WorldWarriorGrandTournamentMidgroundTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_grand_tournament_midground_style_v1.png";
    private const string WorldWarriorGrandTournamentForegroundLeftTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_grand_tournament_foreground_left_style_v1.png";
    private const string WorldWarriorGrandTournamentForegroundRightTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_grand_tournament_foreground_right_style_v1.png";
    private const string WorldWarriorChampionsCourtyardArenaBackdropTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_backdrop_style_v1.png";
    private const string WorldWarriorChampionsCourtyardArenaFloorTexture =
        "res://Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_floor_style_v1.png";
    private const string WorldWarriorChampionsCourtyardFullFramePlate =
        "res://Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_full_frame_plate_style_v2.png";
    private const float ArchiveHealthCachePixelSize = 0.00114f;
    private const float ArchiveMeterCachePixelSize = 0.00103f;
    private const float ArchiveDataCachePixelSize = 0.00136f;
    private const float ArchiveVolatileCanisterPixelSize = 0.00143f;
    private const float ArchiveIntakeTramPixelSize = 0.00220f;
    private const float ArchiveIndexScanEmitterPixelSize = 0.00090f;
    private const float ArchiveRepositoryFallingShelfPixelSize = 0.00150f;
    private const float ArchiveRepositoryDataDebrisPixelSize = 0.00170f;
    private const float ArchiveRepositorySecuritySweepPixelSize = 0.00125f;

    private readonly record struct StageBlueprint(
        string Title,
        string Subtitle,
        string EliteArchetypeId,
        string EliteName,
        float ParTimeSeconds);
}
