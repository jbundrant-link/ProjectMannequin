using Godot;

namespace ProjectMannequin.Settings;

/// <summary>
/// Shared accessibility policy for presentation code.
/// </summary>
/// <remarks>
/// Every function here is pure and takes the relevant setting as an argument
/// rather than reading <see cref="SettingsStore"/> itself, so the deterministic
/// suite can assert the policy without a scene tree. Call sites read the store.
/// </remarks>
public static class AccessibilityRuntime
{
    /// <summary>
    /// WCAG 2.3.1 general flash threshold: no more than three flashes per
    /// second. Used to classify the strobe periods declared in
    /// <see cref="StrobePeriods"/>.
    /// </summary>
    public const float MaximumSafeFlashesPerSecond = 3.0f;

    /// <summary>
    /// Angular rate for the boss intro aura flicker, in radians per
    /// millisecond. 0.018 is about 2.9 Hz, just under the WCAG ceiling.
    /// </summary>
    public const float BossAuraFlickerRate = 0.018f;

    /// <summary>
    /// Cycles per second for a sine oscillation at the given angular rate.
    /// </summary>
    public static float HertzForAngularRate(float angularRatePerMillisecond) =>
        angularRatePerMillisecond * 1000.0f / (2.0f * Mathf.Pi);

    /// <summary>
    /// Every square-wave strobe period in the game, in milliseconds per half
    /// cycle, kept in one place so flash rates are auditable rather than
    /// scattered as literals through the presentation layer.
    /// </summary>
    /// <remarks>
    /// These sit at 5.9-9.1 Hz, above the WCAG ceiling, and are deliberately
    /// left there. They are character-sized rather than large-area, and each
    /// one encodes timing: a parry window is about twelve frames, so at 3 Hz a
    /// player would see less than one full cycle and the cue would stop being
    /// readable. <see cref="ReducedFlash"/> removes them completely for players
    /// who need that, which is the correct trade rather than degrading the
    /// mechanic for everyone.
    /// </remarks>
    public static class StrobePeriods
    {
        public const int FormSwapMilliseconds = 80;
        public const int ParryMilliseconds = 65;
        public const int GuardBreakMilliseconds = 85;
        public const int SuperStartupMilliseconds = 55;
        public const int HitstunMilliseconds = 70;
    }

    /// <summary>
    /// Full on/off cycles per second for a square wave with the given half
    /// period. A half period of 80 ms is a 160 ms cycle, so 6.25 flashes/sec.
    /// </summary>
    public static float FlashesPerSecond(int halfPeriodMilliseconds) =>
        halfPeriodMilliseconds <= 0
            ? 0.0f
            : 1000.0f / (halfPeriodMilliseconds * 2.0f);

    /// <summary>
    /// Raw square-wave phase, ignoring any accessibility setting.
    /// </summary>
    public static bool StrobePhase(ulong milliseconds, int halfPeriodMilliseconds)
    {
        var halfPeriod = (ulong)Mathf.Max(1, halfPeriodMilliseconds);
        return milliseconds / halfPeriod % 2UL == 0UL;
    }

    /// <summary>
    /// Whether a strobed highlight should be drawn this frame. Reduced flash
    /// holds it permanently on, which keeps the state readable while removing
    /// the flicker entirely.
    /// </summary>
    public static bool StrobeOn(
        ulong milliseconds,
        int halfPeriodMilliseconds,
        bool reducedFlash) =>
        reducedFlash || StrobePhase(milliseconds, halfPeriodMilliseconds);

    /// <summary>
    /// Strobed brightness between <paramref name="low"/> and
    /// <paramref name="high"/>. Reduced flash settles on the midpoint so the
    /// element neither blinks nor changes its average brightness.
    /// </summary>
    public static float StrobeScalar(
        ulong milliseconds,
        int halfPeriodMilliseconds,
        float low,
        float high,
        bool reducedFlash)
    {
        if (reducedFlash)
        {
            return (low + high) * 0.5f;
        }

        return StrobePhase(milliseconds, halfPeriodMilliseconds) ? high : low;
    }

    /// <summary>
    /// Smooth 0..1 pulse for telegraphs. Reduced flash returns a flat 0.5.
    /// </summary>
    public static float Pulse(
        ulong milliseconds,
        float angularRatePerMillisecond,
        bool reducedFlash) =>
        reducedFlash
            ? 0.5f
            : 0.5f + (Mathf.Sin(milliseconds * angularRatePerMillisecond) * 0.5f);

    /// <summary>
    /// Smooth oscillation of <paramref name="amplitude"/> either side of
    /// <paramref name="center"/>. Reduced flash holds the centre value.
    /// </summary>
    public static float Oscillate(
        ulong milliseconds,
        float angularRatePerMillisecond,
        float center,
        float amplitude,
        bool reducedFlash) =>
        reducedFlash
            ? center
            : center + (amplitude * Mathf.Sin(milliseconds * angularRatePerMillisecond));

    /// <summary>
    /// Softens a one-shot impact flash toward the sprite's resting colour.
    /// </summary>
    /// <remarks>
    /// A single hit flash is harmless, but a combo lands them every few frames,
    /// which is a repetitive full-sprite flash in the 4-6 Hz range. Reduced
    /// flash keeps the hit legible at a fraction of the swing.
    /// </remarks>
    public static Color SoftenImpactFlash(Color flash, Color resting, bool reducedFlash) =>
        reducedFlash ? resting.Lerp(flash, 0.35f) : flash;

    /// <summary>
    /// Resolves a hazard telegraph's non-colour cues.
    /// </summary>
    /// <remarks>
    /// Warning and active must never be distinguishable by hue alone, because
    /// the default palettes are amber vs red and purple vs orange, which
    /// protanopes and deuteranopes cannot separate. Two colour-independent
    /// channels carry the phase instead: the opacity bands below do not
    /// overlap, and the countdown marker only exists while the hazard is still
    /// winding up.
    /// </remarks>
    public static TelegraphCue ResolveTelegraph(
        bool isWarning,
        float progress,
        float pulse,
        bool highContrast)
    {
        var clampedPulse = Mathf.Clamp(pulse, 0.0f, 1.0f);
        var clampedProgress = Mathf.Clamp(progress, 0.0f, 1.0f);

        if (isWarning)
        {
            var baseAlpha = highContrast ? 0.30f : 0.16f;
            var swing = highContrast ? 0.18f : 0.16f;
            return new TelegraphCue(
                Alpha: baseAlpha + (clampedPulse * swing),
                EmissionEnergy: highContrast ? 2.0f : 1.15f,
                CountdownVisible: true,
                // Contracts to nothing exactly as the hazard fires, so the
                // time remaining is legible without reading any colour.
                CountdownScale: Mathf.Max(0.0f, 1.0f - clampedProgress));
        }

        return new TelegraphCue(
            Alpha: highContrast ? 0.80f : 0.56f,
            EmissionEnergy: highContrast ? 3.0f : 1.8f,
            CountdownVisible: false,
            CountdownScale: 0.0f);
    }

    /// <summary>
    /// Highest alpha a warning telegraph can reach, and lowest an active one
    /// can fall to. Kept adjacent so the non-overlap is obvious here and
    /// enforced by the deterministic suite.
    /// </summary>
    public static float MaximumWarningAlpha(bool highContrast) =>
        highContrast ? 0.48f : 0.32f;

    public static float MinimumActiveAlpha(bool highContrast) =>
        highContrast ? 0.80f : 0.56f;
}

/// <summary>
/// Colour-independent presentation state for one hazard telegraph.
/// </summary>
public readonly record struct TelegraphCue(
    float Alpha,
    float EmissionEnergy,
    bool CountdownVisible,
    float CountdownScale);
