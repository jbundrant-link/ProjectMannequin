using System;
using Godot;

namespace ProjectMannequin.Presentation;

/// <summary>
/// Generates short musical "stinger" cues entirely in code (no external audio
/// asset), used as a fallback for announcer cues such as ROUND 1 / FIGHT. A
/// designer can still assign real voice-over on the audio manager; these only
/// fill in when nothing is assigned.
/// </summary>
public static class ProceduralAudioFactory
{
    private const int MixRate = 44100;

    /// <summary>
    /// Builds a decaying chord stinger from the given frequencies.
    /// </summary>
    public static AudioStreamWav CreateStinger(float[] frequencies, float durationSeconds, float gain = 0.5f)
    {
        var sampleCount = Math.Max(1, (int)(MixRate * durationSeconds));
        var data = new byte[sampleCount * 2];
        var voices = Math.Max(1, frequencies.Length);

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)MixRate;
            var progress = i / (float)sampleCount;
            var envelope = Mathf.Pow(1.0f - progress, 2.2f); // fast percussive decay

            var sample = 0.0f;
            foreach (var frequency in frequencies)
            {
                sample += Mathf.Sin(Mathf.Tau * frequency * t);
            }

            sample = sample / voices * envelope * gain;
            var value = (short)(Mathf.Clamp(sample, -1.0f, 1.0f) * short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = data,
        };
    }

    /// <summary>
    /// Builds a short, deterministic combat impact from a descending tonal body
    /// and seeded noise crack. Frequency/noise balance separates punches,
    /// armored guards, and sharp parry cues until authored SFX are supplied.
    /// </summary>
    public static AudioStreamWav CreateImpact(
        float bodyFrequency,
        float durationSeconds,
        float noiseAmount,
        float gain,
        int seed)
    {
        var sampleCount = Math.Max(1, (int)(MixRate * durationSeconds));
        var data = new byte[sampleCount * 2];
        var random = new Random(seed);

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)MixRate;
            var progress = i / (float)sampleCount;
            var envelope = Mathf.Pow(1.0f - progress, 2.8f);
            var frequency = Mathf.Lerp(bodyFrequency * 1.28f, bodyFrequency * 0.72f, progress);
            var body = Mathf.Sin(Mathf.Tau * frequency * t);
            var harmonic = Mathf.Sin(Mathf.Tau * frequency * 2.17f * t) * 0.28f;
            var crackEnvelope = Mathf.Pow(1.0f - Mathf.Min(progress * 4.5f, 1.0f), 2.2f);
            var crack = (float)(random.NextDouble() * 2.0 - 1.0)
                * noiseAmount
                * crackEnvelope;
            var sample = Mathf.Clamp((body + harmonic + crack) * envelope * gain, -1.0f, 1.0f);
            var value = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = data,
        };
    }

    /// <summary>
    /// Builds a deep cinematic impact/slam (Audio_IntroSlam): a descending
    /// low-frequency sine sweep layered with a short seeded noise transient and
    /// a fast percussive decay. Fully deterministic (fixed RNG seed).
    /// </summary>
    public static AudioStreamWav CreateSlam(float durationSeconds = 0.55f, float gain = 0.85f)
    {
        var sampleCount = Math.Max(1, (int)(MixRate * durationSeconds));
        var data = new byte[sampleCount * 2];
        var random = new Random(20260708);

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)MixRate;
            var progress = i / (float)sampleCount;
            var bodyEnvelope = Mathf.Pow(1.0f - progress, 1.7f);
            // Sub-bass sweep 120Hz -> 42Hz for weighty "boom".
            var sweepFrequency = Mathf.Lerp(120.0f, 42.0f, progress);
            var body = Mathf.Sin(Mathf.Tau * sweepFrequency * t) * bodyEnvelope;
            // Sharp noise transient in the first ~35ms for the impact crack.
            var transientEnvelope = Mathf.Pow(1.0f - Mathf.Min(progress * 6.0f, 1.0f), 3.0f);
            var transient = (float)(random.NextDouble() * 2.0 - 1.0) * transientEnvelope * 0.5f;

            var sample = Mathf.Clamp((body + transient) * gain, -1.0f, 1.0f);
            var value = (short)(sample * short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = data,
        };
    }

    /// <summary>
    /// Builds a metallic gong/impact used under the FIGHT release: a set of
    /// inharmonic partials with a long, slow decay for a resonant tail.
    /// </summary>
    public static AudioStreamWav CreateGong(float baseFrequency = 196.0f, float durationSeconds = 0.9f, float gain = 0.6f)
    {
        var sampleCount = Math.Max(1, (int)(MixRate * durationSeconds));
        var data = new byte[sampleCount * 2];
        // Inharmonic ratios give the shimmering "metal" character.
        var partials = new[] { 1.0f, 1.48f, 2.31f, 2.79f, 3.92f };

        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)MixRate;
            var progress = i / (float)sampleCount;
            var envelope = Mathf.Pow(1.0f - progress, 1.1f);

            var sample = 0.0f;
            foreach (var ratio in partials)
            {
                sample += Mathf.Sin(Mathf.Tau * baseFrequency * ratio * t);
            }

            sample = sample / partials.Length * envelope * gain;
            var value = (short)(Mathf.Clamp(sample, -1.0f, 1.0f) * short.MaxValue);
            data[i * 2] = (byte)(value & 0xFF);
            data[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = MixRate,
            Stereo = false,
            Data = data,
        };
    }
}
