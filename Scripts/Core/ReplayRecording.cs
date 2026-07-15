using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ProjectMannequin.Core;

/// <summary>
/// Deterministic replay format for Project Mannequin. A recording captures the
/// header metadata needed to reconstruct a session (RNG seed, selected mission,
/// selected forms, frame count) plus the per-tick input masks for every player
/// and periodic state fingerprints used to verify exact reproduction.
///
/// The model, serializer, fingerprint accumulator, and playback source are all
/// pure and free of Godot dependencies so they can be unit tested headlessly.
/// Runtime capture/playback wiring lives in <c>GameSimulation</c> and
/// <c>LocalInputManager</c>.
/// </summary>
public static class ReplayFormat
{
    public const int Version = 1;
    public const string Magic = "PMREPLAY";
}

/// <summary>Records which form a given player slot used for the session.</summary>
public sealed record ReplayFormEntry(int PlayerId, string FormId);

/// <summary>
/// One tick of recorded input. <see cref="Held"/> holds the raw
/// <see cref="InputButtons"/> mask (as a uint) for each player in slot order.
/// </summary>
public readonly record struct ReplayInputSample(int Tick, IReadOnlyList<uint> Held)
{
    public uint HeldForSlot(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex < Held.Count ? Held[slotIndex] : 0u;
    }
}

/// <summary>A deterministic fingerprint of the whole simulation at a tick.</summary>
public readonly record struct ReplayStateCheck(int Tick, ulong Fingerprint);

/// <summary>Immutable metadata describing how to reconstruct a session.</summary>
public sealed class ReplayHeader
{
    public int Version { get; init; } = ReplayFormat.Version;
    public uint Seed { get; init; }
    public int TickRate { get; init; } = GameConstants.TickRate;
    public string MissionId { get; init; } = "";
    public string WorldId { get; init; } = "";
    public int PlayerCount { get; init; }
    public int FrameCount { get; init; }
    public IReadOnlyList<ReplayFormEntry> Forms { get; init; } = Array.Empty<ReplayFormEntry>();
}

/// <summary>A complete replay: header, per-tick inputs, and state checks.</summary>
public sealed class ReplayRecording
{
    public ReplayRecording(
        ReplayHeader header,
        IReadOnlyList<ReplayInputSample> inputs,
        IReadOnlyList<ReplayStateCheck> stateChecks)
    {
        Header = header;
        Inputs = inputs;
        StateChecks = stateChecks;
    }

    public ReplayHeader Header { get; }
    public IReadOnlyList<ReplayInputSample> Inputs { get; }
    public IReadOnlyList<ReplayStateCheck> StateChecks { get; }
    public int FrameCount => Header.FrameCount;
}

/// <summary>
/// FNV-1a (64-bit) accumulator used to build deterministic state fingerprints.
/// Floats are quantized before folding so tiny presentation-only differences do
/// not change the hash while genuine simulation drift still does.
/// </summary>
public static class ReplayFingerprint
{
    private const ulong OffsetBasis = 14695981039346656037UL;
    private const ulong Prime = 1099511628211UL;
    public const float DefaultFloatQuantum = 0.001f;

    public static ulong Begin()
    {
        return OffsetBasis;
    }

    public static ulong Combine(ulong hash, ulong value)
    {
        for (var index = 0; index < 8; index++)
        {
            hash ^= value & 0xFF;
            hash *= Prime;
            value >>= 8;
        }

        return hash;
    }

    public static ulong CombineInt(ulong hash, int value)
    {
        return Combine(hash, unchecked((ulong)(uint)value));
    }

    public static ulong CombineBool(ulong hash, bool value)
    {
        return Combine(hash, value ? 1UL : 0UL);
    }

    public static ulong CombineFloat(ulong hash, float value, float quantum = DefaultFloatQuantum)
    {
        var quantized = (long)MathF.Round(value / quantum);
        return Combine(hash, unchecked((ulong)quantized));
    }

    public static ulong CombineString(ulong hash, string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return Combine(hash, 0UL);
        }

        foreach (var character in value)
        {
            hash = Combine(hash, character);
        }

        return hash;
    }
}

/// <summary>
/// Accumulates input samples and state checks during a live session and builds
/// an immutable <see cref="ReplayRecording"/>.
/// </summary>
public sealed class ReplayRecorder
{
    private readonly List<ReplayInputSample> _inputs = new();
    private readonly List<ReplayStateCheck> _checks = new();
    private readonly List<ReplayFormEntry> _forms = new();

    public uint Seed { get; set; }
    public int PlayerCount { get; set; }
    public string MissionId { get; set; } = "";
    public string WorldId { get; set; } = "";
    public int SampleCount => _inputs.Count;

    public void SetForms(IEnumerable<ReplayFormEntry> forms)
    {
        _forms.Clear();
        _forms.AddRange(forms);
    }

    public void RecordInputs(int tick, IReadOnlyList<uint> heldPerPlayer)
    {
        _inputs.Add(new ReplayInputSample(tick, heldPerPlayer.ToArray()));
    }

    public void RecordStateCheck(int tick, ulong fingerprint)
    {
        _checks.Add(new ReplayStateCheck(tick, fingerprint));
    }

    public ReplayRecording Build()
    {
        var header = new ReplayHeader
        {
            Version = ReplayFormat.Version,
            Seed = Seed,
            TickRate = GameConstants.TickRate,
            MissionId = MissionId,
            WorldId = WorldId,
            PlayerCount = PlayerCount,
            FrameCount = _inputs.Count,
            Forms = _forms.ToArray(),
        };

        return new ReplayRecording(header, _inputs.ToArray(), _checks.ToArray());
    }
}

/// <summary>
/// Resolves recorded input masks for playback. Player ids are 1-based to match
/// the rest of the input system.
/// </summary>
public sealed class ReplayInputSource
{
    private readonly Dictionary<int, uint[]> _byTick = new();

    public ReplayInputSource(ReplayRecording recording)
    {
        PlayerCount = recording.Header.PlayerCount;
        FrameCount = recording.Header.FrameCount;
        foreach (var sample in recording.Inputs)
        {
            _byTick[sample.Tick] = sample.Held.ToArray();
        }
    }

    public int PlayerCount { get; }
    public int FrameCount { get; }

    public bool HasTick(int tick)
    {
        return _byTick.ContainsKey(tick);
    }

    public InputButtons HeldFor(int tick, int playerId)
    {
        if (playerId >= 1
            && _byTick.TryGetValue(tick, out var masks)
            && playerId <= masks.Length)
        {
            return (InputButtons)masks[playerId - 1];
        }

        return InputButtons.None;
    }
}

/// <summary>
/// Serializes and parses <see cref="ReplayRecording"/> to a compact,
/// culture-invariant, line-based text format. The format is intentionally
/// human-readable so replays can be diffed and inspected.
/// </summary>
public static class ReplaySerializer
{
    private const string EmptyToken = "-";

    public static string Serialize(ReplayRecording recording)
    {
        var header = recording.Header;
        var builder = new StringBuilder();

        builder.Append(ReplayFormat.Magic).Append(' ').Append(header.Version).Append('\n');
        builder.Append("seed ").Append(header.Seed.ToString("X8", CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("tickrate ").Append(header.TickRate.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("mission ").Append(Encode(header.MissionId)).Append('\n');
        builder.Append("world ").Append(Encode(header.WorldId)).Append('\n');
        builder.Append("players ").Append(header.PlayerCount.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("frames ").Append(header.FrameCount.ToString(CultureInfo.InvariantCulture)).Append('\n');

        foreach (var form in header.Forms)
        {
            builder.Append("form ")
                .Append(form.PlayerId.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(Encode(form.FormId))
                .Append('\n');
        }

        foreach (var sample in recording.Inputs)
        {
            builder.Append("I ").Append(sample.Tick.ToString(CultureInfo.InvariantCulture));
            foreach (var mask in sample.Held)
            {
                builder.Append(' ').Append(mask.ToString("X", CultureInfo.InvariantCulture));
            }

            builder.Append('\n');
        }

        foreach (var check in recording.StateChecks)
        {
            builder.Append("S ")
                .Append(check.Tick.ToString(CultureInfo.InvariantCulture))
                .Append(' ')
                .Append(check.Fingerprint.ToString("X", CultureInfo.InvariantCulture))
                .Append('\n');
        }

        return builder.ToString();
    }

    public static ReplayRecording Parse(string text)
    {
        var version = ReplayFormat.Version;
        var tickRate = GameConstants.TickRate;
        var playerCount = 0;
        var frameCount = 0;
        var seed = 0u;
        var missionId = "";
        var worldId = "";
        var forms = new List<ReplayFormEntry>();
        var inputs = new List<ReplayInputSample>();
        var checks = new List<ReplayStateCheck>();

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case ReplayFormat.Magic:
                    version = ParseInt(parts, 1, version);
                    break;
                case "seed":
                    if (parts.Length > 1)
                    {
                        seed = uint.Parse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    }

                    break;
                case "tickrate":
                    tickRate = ParseInt(parts, 1, tickRate);
                    break;
                case "mission":
                    missionId = Decode(parts.Length > 1 ? parts[1] : EmptyToken);
                    break;
                case "world":
                    worldId = Decode(parts.Length > 1 ? parts[1] : EmptyToken);
                    break;
                case "players":
                    playerCount = ParseInt(parts, 1, playerCount);
                    break;
                case "frames":
                    frameCount = ParseInt(parts, 1, frameCount);
                    break;
                case "form":
                    if (parts.Length >= 3)
                    {
                        forms.Add(new ReplayFormEntry(ParseInt(parts, 1, 0), Decode(parts[2])));
                    }

                    break;
                case "I":
                    if (parts.Length >= 2)
                    {
                        var masks = new uint[parts.Length - 2];
                        for (var index = 2; index < parts.Length; index++)
                        {
                            masks[index - 2] = uint.Parse(
                                parts[index],
                                NumberStyles.HexNumber,
                                CultureInfo.InvariantCulture);
                        }

                        inputs.Add(new ReplayInputSample(ParseInt(parts, 1, 0), masks));
                    }

                    break;
                case "S":
                    if (parts.Length >= 3)
                    {
                        checks.Add(new ReplayStateCheck(
                            ParseInt(parts, 1, 0),
                            ulong.Parse(parts[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture)));
                    }

                    break;
            }
        }

        var header = new ReplayHeader
        {
            Version = version,
            Seed = seed,
            TickRate = tickRate,
            MissionId = missionId,
            WorldId = worldId,
            PlayerCount = playerCount,
            FrameCount = frameCount,
            Forms = forms,
        };

        return new ReplayRecording(header, inputs, checks);
    }

    private static int ParseInt(string[] parts, int index, int fallback)
    {
        return index < parts.Length
            && int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                ? value
                : fallback;
    }

    private static string Encode(string value)
    {
        return string.IsNullOrEmpty(value) ? EmptyToken : value;
    }

    private static string Decode(string token)
    {
        return token == EmptyToken ? "" : token;
    }
}
