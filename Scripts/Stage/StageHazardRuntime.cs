using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Stage;

public enum StageHazardPhase
{
    Dormant,
    Warning,
    Active,
    Cooldown,
}

public readonly record struct StageHazardFrame(
    StageHazardPhase Phase,
    float MinX,
    float MaxX,
    float MinZ,
    float MaxZ,
    float Progress)
{
    public bool IsWarning => Phase == StageHazardPhase.Warning;
    public bool IsActive => Phase == StageHazardPhase.Active;
}

/// <summary>Pure deterministic hazard timing and movement resolution.</summary>
public static class StageHazardRuntime
{
    public static StageHazardFrame Resolve(StageHazardZoneData zone, int encounterElapsedFrames)
    {
        if (encounterElapsedFrames < zone.ActivationDelayFrames)
        {
            return Frame(zone, StageHazardPhase.Dormant, 0.0f);
        }

        var local = encounterElapsedFrames - zone.ActivationDelayFrames;
        if (zone.RepeatIntervalFrames > 0)
        {
            local %= zone.RepeatIntervalFrames;
        }

        if (local < zone.WarningLeadFrames)
        {
            var progress = zone.WarningLeadFrames <= 0
                ? 1.0f
                : local / (float)zone.WarningLeadFrames;
            return Frame(zone, StageHazardPhase.Warning, progress);
        }

        var activeElapsed = local - zone.WarningLeadFrames;
        if (zone.ActiveFrames <= 0 || activeElapsed < zone.ActiveFrames)
        {
            var progress = zone.ActiveFrames <= 1
                ? 1.0f
                : Mathf.Clamp(activeElapsed / (float)(zone.ActiveFrames - 1), 0.0f, 1.0f);
            return Frame(zone, StageHazardPhase.Active, progress);
        }

        return Frame(zone, StageHazardPhase.Cooldown, 1.0f);
    }

    private static StageHazardFrame Frame(
        StageHazardZoneData zone,
        StageHazardPhase phase,
        float progress)
    {
        var movementT = zone.Behavior == StageHazardBehavior.LinearSweep
            && phase == StageHazardPhase.Active
            ? Mathf.Clamp(progress, 0.0f, 1.0f)
            : 0.0f;
        var offsetX = zone.MovementOffsetX * movementT;
        var offsetZ = zone.MovementOffsetZ * movementT;
        return new StageHazardFrame(
            phase,
            zone.MinX + offsetX,
            zone.MaxX + offsetX,
            zone.MinZ + offsetZ,
            zone.MaxZ + offsetZ,
            progress);
    }
}
