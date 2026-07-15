using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Stage;

public readonly record struct StageLaneFrame(
    float MinZ,
    float MaxZ,
    float Progress);

/// <summary>
/// Pure deterministic interpolation for authored encounter lane depth. Smooth
/// step avoids snapping actors when a corridor funnels or opens into an arena.
/// </summary>
public static class StageLaneRuntime
{
    public static StageLaneFrame Resolve(
        float startMinZ,
        float startMaxZ,
        float missionMinZ,
        float missionMaxZ,
        StageEncounterData encounter,
        int elapsedFrames)
    {
        var targetMin = TargetMinZ(missionMinZ, encounter);
        var targetMax = TargetMaxZ(missionMaxZ, encounter);
        var duration = Mathf.Max(0, encounter.LaneTransitionFrames);
        var progress = duration == 0
            ? 1.0f
            : Mathf.Clamp(elapsedFrames / (float)duration, 0.0f, 1.0f);
        var eased = progress * progress * (3.0f - 2.0f * progress);
        return new StageLaneFrame(
            Mathf.Lerp(startMinZ, targetMin, eased),
            Mathf.Lerp(startMaxZ, targetMax, eased),
            progress);
    }

    public static float TargetMinZ(float missionMinZ, StageEncounterData encounter)
    {
        return encounter.UsesLaneBounds ? encounter.LaneMinZ : missionMinZ;
    }

    public static float TargetMaxZ(float missionMaxZ, StageEncounterData encounter)
    {
        return encounter.UsesLaneBounds ? encounter.LaneMaxZ : missionMaxZ;
    }
}
