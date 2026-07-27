using Godot;

namespace ProjectMannequin.Stage;

/// <summary>
/// The key light direction the current stage is authored against.
/// </summary>
/// <remarks>
/// Stage art and runtime lighting have to agree about where the light comes
/// from, or a painted highlight on a wall points one way while the fighter in
/// front of it is lit from another. The direction is declared per stage in
/// <c>StageMissionData</c>, applied to the sun by <c>PrototypeStageView</c>,
/// and published here so presentation code that has no reference to the mission
/// (a character's contact shadow, for example) can align to the same key.
///
/// Deliberately not simulation state: lighting is presentation only and must
/// never affect a replay.
/// </remarks>
public static class StageKeyLight
{
    /// <summary>Downward pitch in degrees. Negative points at the ground.</summary>
    public const float DefaultPitchDegrees = -48.0f;

    /// <summary>Yaw in degrees around the vertical axis.</summary>
    public const float DefaultYawDegrees = -30.0f;

    public static float PitchDegrees { get; private set; } = DefaultPitchDegrees;

    public static float YawDegrees { get; private set; } = DefaultYawDegrees;

    public static void Set(float pitchDegrees, float yawDegrees)
    {
        PitchDegrees = pitchDegrees;
        YawDegrees = yawDegrees;
    }

    public static void ResetToDefault() =>
        Set(DefaultPitchDegrees, DefaultYawDegrees);

    /// <summary>
    /// Horizontal offset a ground contact shadow should take, in world units.
    /// </summary>
    /// <remarks>
    /// Pure, so the deterministic suite can check the shadow leans away from
    /// the light rather than toward it. A shallower pitch throws the shadow
    /// further, which is why the pitch scales the result.
    /// </remarks>
    public static Vector2 ContactOffset(
        float yawDegrees,
        float pitchDegrees,
        float strength)
    {
        var yaw = Mathf.DegToRad(yawDegrees);

        // A steep overhead light drops the shadow underfoot; a low one rakes it
        // sideways. Clamped so a near-horizon key cannot fling it off screen.
        var pitch = Mathf.Clamp(Mathf.Abs(pitchDegrees), 5.0f, 90.0f);
        var rake = Mathf.Clamp(1.0f - (pitch / 90.0f), 0.0f, 1.0f);

        // Thrown away from the light, hence the negated sine.
        return new Vector2(
            -Mathf.Sin(yaw) * strength * (0.35f + rake),
            Mathf.Cos(yaw) * strength * rake);
    }
}
