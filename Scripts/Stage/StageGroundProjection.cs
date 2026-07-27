using System;
using System.Linq;
using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Stage;

/// <summary>
/// Shared orthographic projection maths that ties the painted ground in a
/// full-frame stage plate to the gameplay ground plane.
/// </summary>
/// <remarks>
/// A full-frame plate is a screen-filling billboard, so the painting cannot
/// follow fighters. The only thing that decides whether a fighter reads as
/// standing on the painted floor is where the camera projects world
/// <c>y = 0</c> across the lane depth. Dimension, aspect, and pixel-fidelity
/// checks say nothing about that, which is why grounding needs its own maths
/// shared by the renderer and the test suite.
/// </remarks>
public static class StageGroundProjection
{
    /// <summary>Resting camera framing before follow, zoom, and shake.</summary>
    public readonly record struct RestingCameraProfile(
        float Height,
        float Depth,
        float LookHeight,
        float Size);

    /// <summary>Normalised frame rows covered by the gameplay ground plane.</summary>
    public readonly record struct GroundBand(float FarFraction, float NearFraction)
    {
        public float CenterFraction => (FarFraction + NearFraction) * 0.5f;

        public bool IsWithin(GroundBand painted, float margin) =>
            FarFraction >= painted.FarFraction + margin
            && NearFraction <= painted.NearFraction - margin;
    }

    /// <summary>
    /// Normalised frame row (0 = top, 1 = bottom) where world
    /// <c>(y = 0, z = laneZ)</c> lands for an orthographic camera that sits at
    /// <paramref name="profile"/> and looks at the lane centre.
    /// </summary>
    /// <summary>Depth range over which far-layer atmospheric haze ramps in.</summary>
    /// <remarks>
    /// Anchored to the walkable lane rather than to a world constant, because
    /// the one property that must never break is that haze cannot touch a
    /// fighter. A hazed fighter would lose contrast against the stage exactly
    /// when they walk to the back lane, which is a readability regression the
    /// player would feel as the game "greying out" during a fight. Begin is
    /// therefore placed strictly behind the deepest standable lane position,
    /// and end past the backdrop plane so the backdrop takes the full effect
    /// while the midground takes a partial amount.
    /// </remarks>
    public static (float Begin, float End) ResolveFarHazeRange(
        StageMissionData mission,
        float clearance,
        float span)
    {
        var profile = ResolveRestingCameraProfile(mission);

        // Lane Z is negative going away from the camera, so the deepest
        // standable point is LaneMinZ and its camera distance is Depth - LaneMinZ.
        var deepestFighterDistance = profile.Depth - mission.LaneMinZ;
        var begin = deepestFighterDistance + Mathf.Max(0.0f, clearance);
        return (begin, begin + Mathf.Max(0.01f, span));
    }

    public static float GroundFraction(RestingCameraProfile profile, float laneZ)
    {
        var forwardY = profile.LookHeight - profile.Height;
        var forwardZ = -profile.Depth;
        var length = MathF.Sqrt(forwardY * forwardY + forwardZ * forwardZ);
        if (length <= 0.0f || profile.Size <= 0.0f)
        {
            return 0.5f;
        }

        forwardY /= length;
        forwardZ /= length;
        // Up is forward rotated a quarter turn inside the YZ plane.
        var upY = -forwardZ;
        var upZ = forwardY;
        var offset = -profile.Height * upY + (laneZ - profile.Depth) * upZ;
        return 0.5f - offset / profile.Size;
    }

    /// <summary>Frame rows covered by the walkable lane depth.</summary>
    public static GroundBand ResolveGameplayGroundBand(
        RestingCameraProfile profile,
        float laneMinZ,
        float laneMaxZ) =>
        new(
            GroundFraction(profile, laneMinZ),
            GroundFraction(profile, laneMaxZ));

    /// <summary>
    /// Camera look height that places the projected lane centre on
    /// <paramref name="targetCenterFraction"/> of the frame.
    /// </summary>
    /// <remarks>
    /// With the camera looking at the lane centre the ground offset reduces to
    /// <c>-depth * lookHeight / length</c>, so requiring a target frame row
    /// collapses to a quadratic in the look height.
    /// </remarks>
    public static float SolveLookHeightForGroundCenter(
        float cameraHeight,
        float cameraDepth,
        float cameraSize,
        float targetCenterFraction,
        float fallbackLookHeight)
    {
        if (cameraDepth <= 0.0f || cameraSize <= 0.0f)
        {
            return fallbackLookHeight;
        }

        var ratio = (targetCenterFraction - 0.5f) * cameraSize / cameraDepth;
        if (ratio <= 0.0f)
        {
            return fallbackLookHeight;
        }

        var ratioSquared = ratio * ratio;
        var a = 1.0f - ratioSquared;
        var b = 2.0f * ratioSquared * cameraHeight;
        var c = -ratioSquared
            * (cameraHeight * cameraHeight + cameraDepth * cameraDepth);
        if (Mathf.IsZeroApprox(a))
        {
            return Mathf.IsZeroApprox(b) ? fallbackLookHeight : -c / b;
        }

        var discriminant = b * b - 4.0f * a * c;
        if (discriminant < 0.0f)
        {
            return fallbackLookHeight;
        }

        var solution = (-b + MathF.Sqrt(discriminant)) / (2.0f * a);
        return solution > 0.0f && solution < cameraHeight
            ? solution
            : fallbackLookHeight;
    }

    /// <summary>
    /// Painted walkable band declared by a stage's full-frame plates, or
    /// <see langword="null"/> when the stage has not been ground-calibrated.
    /// </summary>
    public static GroundBand? ResolvePaintedGroundBand(StageMissionData mission)
    {
        if (mission.PresentationMode != StagePresentationMode.FullFramePlates
            || mission.FullFramePlates.Count == 0
            || mission.FullFramePlates.Any(plate =>
                plate.GroundFarFraction <= 0.0f
                || plate.GroundNearFraction <= plate.GroundFarFraction
                || plate.GroundNearFraction > 1.0f))
        {
            return null;
        }

        // The camera cannot fit one lane band to disagreeing paintings, so the
        // usable band is the intersection of every plate the stage can show.
        return new GroundBand(
            mission.FullFramePlates.Max(plate => plate.GroundFarFraction),
            mission.FullFramePlates.Min(plate => plate.GroundNearFraction));
    }

    /// <summary>Resting camera framing the presentation layer starts from.</summary>
    public static RestingCameraProfile ResolveRestingCameraProfile(
        StageMissionData mission)
    {
        var isBoundedArena =
            mission.PresentationMode == StagePresentationMode.BoundedArena
            || (mission.PresentationMode == StagePresentationMode.FullFramePlates
                && mission.ArenaPresentation is not null);
        var usesTraversalCamera =
            mission.PresentationMode == StagePresentationMode.CompositeTraversal
            || (mission.PresentationMode == StagePresentationMode.FullFramePlates
                && mission.ArenaPresentation is null);
        var height = usesTraversalCamera ? 5.2f : 6.5f;
        var depth = usesTraversalCamera
            ? 14.5f
            : isBoundedArena ? 13.5f : 11.5f;
        var lookHeight = usesTraversalCamera
            ? 1.4f
            : isBoundedArena
                ? mission.ArenaPresentation?.CameraLookHeight ?? 3.0f
                : 1.15f;
        var size = isBoundedArena && mission.ArenaPresentation is not null
            ? mission.ArenaPresentation.CameraSize
            : mission.CameraBaseSize;
        var painted = ResolvePaintedGroundBand(mission);
        var targetCenter = painted?.CenterFraction
            ?? (mission.GroundLineCenterFraction > 0.0f
                ? mission.GroundLineCenterFraction
                : 0.0f);
        if (targetCenter > 0.0f)
        {
            lookHeight = SolveLookHeightForGroundCenter(
                height,
                depth,
                size,
                targetCenter,
                lookHeight);
        }

        return new RestingCameraProfile(height, depth, lookHeight, size);
    }
}
