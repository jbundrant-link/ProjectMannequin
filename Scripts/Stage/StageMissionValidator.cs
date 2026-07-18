using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Stage;

public static class StageMissionValidator
{
    public static IReadOnlyList<string> Validate(StageMissionData mission)
    {
        var errors = new List<string>();
        if (mission.StageMaxX <= mission.StageMinX)
        {
            errors.Add(
                $"Mission '{mission.Id}' has invalid stage bounds "
                + $"[{mission.StageMinX}, {mission.StageMaxX}].");
        }

        if (mission.CameraBaseSize <= 0.0f
            || mission.CameraMaxSize < mission.CameraBaseSize
            || mission.CameraHorizontalPadding < 0.0f
            || mission.CameraFollowSharpness <= 0.0f
            || mission.CameraZoomSharpness <= 0.0f
            || mission.CameraCinematicSize <= 0.0f
            || mission.CameraCinematicRecoverySeconds <= 0.0f)
        {
            errors.Add($"Mission '{mission.Id}' has invalid camera composition settings.");
        }

        if (mission.PartySoftSeparationX <= 0.0f
            || mission.PartyHardSeparationX <= mission.PartySoftSeparationX
            || mission.PartyCatchUpSpeed <= 0.0f)
        {
            errors.Add($"Mission '{mission.Id}' has invalid party tether settings.");
        }

        if (mission.ThreePlayerShakeScale < 0.0f
            || mission.ThreePlayerShakeScale > 1.0f
            || mission.FourPlayerShakeScale < 0.0f
            || mission.FourPlayerShakeScale > mission.ThreePlayerShakeScale)
        {
            errors.Add($"Mission '{mission.Id}' has invalid multiplayer shake scaling.");
        }

        if (mission.FarColorR < 0.0f
            || mission.FarColorG < 0.0f
            || mission.FarColorB < 0.0f
            || mission.FloorColorR < 0.0f
            || mission.FloorColorG < 0.0f
            || mission.FloorColorB < 0.0f
            || mission.LaneAccentR < 0.0f
            || mission.LaneAccentG < 0.0f
            || mission.LaneAccentB < 0.0f)
        {
            errors.Add($"Mission '{mission.Id}' has invalid stage presentation colors.");
        }

        if (string.IsNullOrWhiteSpace(mission.StageTexturePath)
            || !ResourceLoader.Exists(mission.StageTexturePath))
        {
            errors.Add($"Mission '{mission.Id}' has missing stage art '{mission.StageTexturePath}'.");
        }
        var effectiveFloorTexturePath = string.IsNullOrWhiteSpace(mission.FloorTexturePath)
            ? mission.StageTexturePath
            : mission.FloorTexturePath;
        if (string.IsNullOrWhiteSpace(effectiveFloorTexturePath)
            || !ResourceLoader.Exists(effectiveFloorTexturePath))
        {
            errors.Add($"Mission '{mission.Id}' has missing floor art '{effectiveFloorTexturePath}'.");
        }

        ValidateBackgroundPanels(mission, errors);
        var previousTriggerX = mission.StageMinX;
        foreach (var encounter in mission.Encounters)
        {
            if (encounter.ArenaMaxX <= encounter.ArenaMinX)
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' has invalid fight-room bounds "
                    + $"[{encounter.ArenaMinX}, {encounter.ArenaMaxX}].");
                continue;
            }

            if (encounter.TriggerX < previousTriggerX)
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' trigger {encounter.TriggerX} is behind "
                    + $"the previous trigger {previousTriggerX}.");
            }

            if (encounter.TriggerX < encounter.ArenaMinX
                || encounter.TriggerX > encounter.ArenaMaxX)
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' trigger {encounter.TriggerX} is outside "
                    + $"its fight room [{encounter.ArenaMinX}, {encounter.ArenaMaxX}].");
            }

            if (encounter.CameraLockX < encounter.ArenaMinX
                || encounter.CameraLockX > encounter.ArenaMaxX)
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' camera lock {encounter.CameraLockX} is outside "
                    + $"its fight room [{encounter.ArenaMinX}, {encounter.ArenaMaxX}].");
            }

            if (encounter.CameraOrthographicSize < 0.0f
                || encounter.CameraOrthographicSize > mission.CameraMaxSize)
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' has invalid camera size "
                    + $"{encounter.CameraOrthographicSize}.");
            }

            if (encounter.CameraTransitionFrames < 0)
            {
                errors.Add($"Encounter '{encounter.Id}' has negative camera transition frames.");
            }

            if (encounter.LaneTransitionFrames < 0)
            {
                errors.Add($"Encounter '{encounter.Id}' has negative lane transition frames.");
            }

            if (encounter.UsesLaneBounds
                && (encounter.LaneMinZ >= encounter.LaneMaxZ
                    || encounter.LaneMinZ < mission.LaneMinZ
                    || encounter.LaneMaxZ > mission.LaneMaxZ))
            {
                errors.Add(
                    $"Encounter '{encounter.Id}' has invalid lane bounds "
                    + $"[{encounter.LaneMinZ}, {encounter.LaneMaxZ}] inside mission "
                    + $"[{mission.LaneMinZ}, {mission.LaneMaxZ}].");
            }

            var encounterLaneMin = StageLaneRuntime.TargetMinZ(mission.LaneMinZ, encounter);
            var encounterLaneMax = StageLaneRuntime.TargetMaxZ(mission.LaneMaxZ, encounter);

            if (encounter.GateReleaseDelayFrames < 0)
            {
                errors.Add($"Encounter '{encounter.Id}' has a negative gate release delay.");
            }

            if (encounter.PlayerBoundaryInset < 0.0f
                || encounter.PlayerBoundaryInset * 2.0f
                >= encounter.ArenaMaxX - encounter.ArenaMinX)
            {
                errors.Add($"Encounter '{encounter.Id}' has an invalid player boundary inset.");
            }

            if (encounter.MaxSimultaneousAttackers <= 0
                || encounter.MaxAttackersPerPlayer <= 0
                || encounter.MaxAttackersPerPlayer > encounter.MaxSimultaneousAttackers)
            {
                errors.Add($"Encounter '{encounter.Id}' has invalid crowd attack limits.");
            }

            if (encounter.LightColorR < 0.0f
                || encounter.LightColorG < 0.0f
                || encounter.LightColorB < 0.0f
                || encounter.LightEnergyMultiplier <= 0.0f
                || encounter.LightTransitionSeconds <= 0.0f)
            {
                errors.Add($"Encounter '{encounter.Id}' has invalid lighting values.");
            }

            foreach (var zone in encounter.HazardZones)
            {
                if (zone.MinX >= zone.MaxX || zone.MinZ >= zone.MaxZ)
                {
                    errors.Add($"Encounter '{encounter.Id}' has hazard zone with invalid bounds.");
                }
                if (zone.DamagePerSecond < 0)
                {
                    errors.Add($"Encounter '{encounter.Id}' has hazard zone with negative DPS.");
                }
                if (zone.Targets == StageHazardTargetMask.None)
                {
                    errors.Add($"Encounter '{encounter.Id}' has hazard '{zone.Id}' with no targets.");
                }
                if (zone.WarningLeadFrames < 30
                    || zone.ActivationDelayFrames < 0
                    || zone.ActiveFrames < 0
                    || zone.RepeatIntervalFrames < 0
                    || (zone.RepeatIntervalFrames > 0
                        && zone.RepeatIntervalFrames
                        < zone.WarningLeadFrames + zone.ActiveFrames))
                {
                    errors.Add($"Encounter '{encounter.Id}' hazard '{zone.Id}' has unsafe timing values.");
                }
                if (zone.Behavior == StageHazardBehavior.FallingStrike
                    && zone.ActiveFrames <= 0)
                {
                    errors.Add($"Encounter '{encounter.Id}' falling strike '{zone.Id}' needs a finite impact window.");
                }
                if (!string.IsNullOrWhiteSpace(zone.SpritePath)
                    && (!ResourceLoader.Exists(zone.SpritePath)
                        || zone.SpritePixelSize <= 0.0f
                        || zone.SpriteGroundOffsetPixels < 0.0f
                        || zone.SpriteTravelHeight < 0.0f
                        || zone.SpriteAnchorX < 0.0f
                        || zone.SpriteAnchorX > 1.0f
                        || zone.SpriteAnchorZ < 0.0f
                        || zone.SpriteAnchorZ > 1.0f))
                {
                    errors.Add(
                        $"Encounter '{encounter.Id}' hazard '{zone.Id}' has invalid authored sprite metrics or path.");
                }
                if (!string.IsNullOrWhiteSpace(zone.FieldTexturePath)
                    && !ResourceLoader.Exists(zone.FieldTexturePath))
                {
                    errors.Add(
                        $"Encounter '{encounter.Id}' hazard '{zone.Id}' has missing field texture '{zone.FieldTexturePath}'.");
                }

                var movedMinX = System.Math.Min(zone.MinX, zone.MinX + zone.MovementOffsetX);
                var movedMaxX = System.Math.Max(zone.MaxX, zone.MaxX + zone.MovementOffsetX);
                var movedMinZ = System.Math.Min(zone.MinZ, zone.MinZ + zone.MovementOffsetZ);
                var movedMaxZ = System.Math.Max(zone.MaxZ, zone.MaxZ + zone.MovementOffsetZ);
                if (movedMinX < encounter.ArenaMinX - 0.01f
                    || movedMaxX > encounter.ArenaMaxX + 0.01f
                    || movedMinZ < encounterLaneMin - 0.01f
                    || movedMaxZ > encounterLaneMax + 0.01f)
                {
                    errors.Add($"Encounter '{encounter.Id}' hazard '{zone.Id}' leaves its authored arena/lane bounds.");
                }

                var coversEveryLane = movedMinZ <= encounterLaneMin + 0.01f
                    && movedMaxZ >= encounterLaneMax - 0.01f;
                if (coversEveryLane && zone.ActiveFrames <= 0)
                {
                    errors.Add($"Encounter '{encounter.Id}' hazard '{zone.Id}' has no permanent safe lane or off window.");
                }
                ValidateAftermathVisual(
                    $"Encounter '{encounter.Id}' hazard '{zone.Id}'",
                    zone.AftermathVisual,
                    errors);
            }
            
            foreach (var prop in encounter.Props)
            {
                if (prop.Health <= 0)
                {
                    errors.Add($"Encounter '{encounter.Id}' has a prop with zero or negative health.");
                }
                if (prop.PositionX < encounter.ArenaMinX
                    || prop.PositionX > encounter.ArenaMaxX
                    || prop.PositionZ < encounterLaneMin
                    || prop.PositionZ > encounterLaneMax)
                {
                    errors.Add($"Encounter '{encounter.Id}' prop '{prop.Id}' is outside arena/lane bounds.");
                }
                if (prop.DropChance < 0.0f || prop.DropChance > 1.0f)
                {
                    errors.Add($"Encounter '{encounter.Id}' prop '{prop.Id}' has invalid drop chance.");
                }
                if (prop.ExplodesOnBreak
                    && (prop.ExplosionTargets == StageHazardTargetMask.None
                        || prop.ExplosionRadius <= 0.0f
                        || prop.ExplosionDamage <= 0
                        || prop.ExplosionKnockback < 0.0f
                        || prop.ExplosionHitstunFrames < 0))
                {
                    errors.Add($"Encounter '{encounter.Id}' prop '{prop.Id}' has invalid explosion values.");
                }
                if (string.IsNullOrWhiteSpace(prop.SpritePath)
                    || !ResourceLoader.Exists(prop.SpritePath))
                {
                    errors.Add(
                        $"Encounter '{encounter.Id}' prop '{prop.Id}' has missing sprite '{prop.SpritePath}'.");
                }
                ValidateAftermathVisual(
                    $"Encounter '{encounter.Id}' prop '{prop.Id}'",
                    prop.AftermathVisual,
                    errors);
            }

            var authoredSpawns = encounter.Waves.Count > 0
                ? encounter.Waves.SelectMany(wave => wave.Spawns)
                : encounter.Spawns;
            foreach (var spawn in authoredSpawns)
            {
                if (spawn.LaneZ < encounterLaneMin || spawn.LaneZ > encounterLaneMax)
                {
                    errors.Add(
                        $"Encounter '{encounter.Id}' spawn '{spawn.DisplayName}' targets lane "
                        + $"{spawn.LaneZ} outside [{encounterLaneMin}, {encounterLaneMax}].");
                }
            }

            ValidateWaves(encounter, errors);
            previousTriggerX = encounter.TriggerX;
        }

        return errors;
    }

    private static void ValidateAftermathVisual(
        string owner,
        StageAftermathVisualData? visual,
        ICollection<string> errors)
    {
        if (visual is null)
        {
            return;
        }

        var decalValid = !string.IsNullOrWhiteSpace(visual.DecalTexturePath)
            && ResourceLoader.Exists(visual.DecalTexturePath)
            && visual.DecalSizeX > 0.0f
            && visual.DecalSizeZ > 0.0f
            && visual.DecalOpacity is > 0.0f and <= 1.0f;
        var fragmentsValid = !string.IsNullOrWhiteSpace(visual.FragmentSpritePath)
            && ResourceLoader.Exists(visual.FragmentSpritePath)
            && visual.FragmentPixelSize > 0.0f
            && visual.FragmentGroundOffsetPixels >= 0.0f;
        if (!decalValid || !fragmentsValid)
        {
            errors.Add($"{owner} has invalid aftermath art or metrics.");
        }
    }

    private static void ValidateBackgroundPanels(
        StageMissionData mission,
        ICollection<string> errors)
    {
        foreach (var panel in mission.BackgroundPanels)
        {
            if (string.IsNullOrWhiteSpace(panel.TexturePath)
                || panel.MaxX <= panel.MinX
                || panel.ParallaxFactorX < 0.0f
                || panel.ParallaxFactorX > 1.5f
                || panel.Opacity <= 0.0f
                || panel.Opacity > 1.0f
                || panel.CropTopFraction < 0.0f
                || panel.CropBottomFraction < 0.0f
                || panel.CropTopFraction + panel.CropBottomFraction >= 0.9f)
            {
                errors.Add($"Mission '{mission.Id}' has an invalid background panel.");
            }
            else if (!ResourceLoader.Exists(panel.TexturePath))
            {
                errors.Add(
                    $"Mission '{mission.Id}' has missing background panel art '{panel.TexturePath}'.");
            }

            if (panel.DestructionTexturePaths.Any(path =>
                    string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path)))
            {
                errors.Add(
                    $"Mission '{mission.Id}' has missing progressive destruction panel art.");
            }

            if (panel.DestructionBurstTexturePaths.Count > 0
                && (panel.DestructionBurstPixelSize <= 0.0f
                    || panel.DestructionBurstTexturePaths.Any(path =>
                        string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
                    || (panel.DestructionBurstAnchorXs.Count > 0
                        && panel.DestructionBurstAnchorXs.Count
                            != panel.DestructionBurstTexturePaths.Count)
                    || panel.DestructionBurstAnchorXs.Any(anchor =>
                        anchor < 0.0f || anchor > 1.0f)))
            {
                errors.Add(
                    $"Mission '{mission.Id}' has invalid progressive destruction burst art or metrics.");
            }

            if (panel.DestructionOverlayPixelSize < 0.0f)
            {
                errors.Add(
                    $"Mission '{mission.Id}' has invalid progressive destruction overlay metrics.");
            }
        }
    }

    private static void ValidateWaves(
        StageEncounterData encounter,
        ICollection<string> errors)
    {
        if (encounter.Waves.Count > 0 && encounter.Spawns.Count > 0)
        {
            errors.Add(
                $"Encounter '{encounter.Id}' defines both Waves and legacy Spawns. "
                + "Use only one format.");
        }

        if (encounter.Waves.Count == 0)
        {
            if (encounter.Spawns.Count == 0
                && (!encounter.UseRandomPool
                    || encounter.RandomArchetypePool.Count == 0
                    || encounter.RandomSpawnCount <= 0))
            {
                errors.Add($"Encounter '{encounter.Id}' has no configured enemies.");
            }

            ValidateSpawns(encounter.Id, encounter.Spawns, errors);
            return;
        }

        var waveIds = new HashSet<string>();
        foreach (var wave in encounter.Waves)
        {
            if (string.IsNullOrWhiteSpace(wave.Id))
            {
                errors.Add($"Encounter '{encounter.Id}' contains a wave with no ID.");
            }
            else if (!waveIds.Add(wave.Id))
            {
                errors.Add($"Encounter '{encounter.Id}' contains duplicate wave ID '{wave.Id}'.");
            }

            if (wave.StartDelayFrames < 0
                || wave.MaxActiveEnemies < 0
                || wave.HealthReward < 0
                || wave.MeterReward < 0)
            {
                errors.Add($"Wave '{wave.Id}' in encounter '{encounter.Id}' has negative values.");
            }

            var hasRandomEnemies = wave.UseRandomPool
                && wave.RandomArchetypePool.Count > 0
                && wave.RandomSpawnCount > 0;
            if (wave.Spawns.Count == 0 && !hasRandomEnemies)
            {
                errors.Add($"Wave '{wave.Id}' in encounter '{encounter.Id}' has no enemies.");
            }

            ValidateSpawns($"{encounter.Id}/{wave.Id}", wave.Spawns, errors);
        }
    }

    private static void ValidateSpawns(
        string ownerId,
        IEnumerable<EnemySpawnData> spawns,
        ICollection<string> errors)
    {
        foreach (var spawn in spawns)
        {
            if (spawn.SpawnDelayFrames < 0
                || spawn.WarningLeadFrames < 0
                || spawn.EntryDistance < 0.0f
                || spawn.EntryHeight < 0.0f)
            {
                errors.Add(
                    $"Spawn '{spawn.ArchetypeId}' in '{ownerId}' has invalid entry values.");
            }

            if (spawn.EntryProfile == EnemyEntryProfile.DropIn
                && spawn.EntryHeight <= 0.0f)
            {
                errors.Add(
                    $"Drop-in spawn '{spawn.ArchetypeId}' in '{ownerId}' needs entry height.");
            }
        }
    }
}
