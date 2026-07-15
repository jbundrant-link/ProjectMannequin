using System.Collections.Generic;
using System.Text;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless tests for the deterministic replay core: serializer round-trips,
/// input playback fidelity, and fingerprint determinism/sensitivity. Failures
/// print the offending values (expected vs actual, or the mismatching text) so
/// regressions are diagnosable, not just pass/fail.
///
/// Run with the environment flag PROJECT_MANNEQUIN_REPLAY_TEST=1.
/// </summary>
public static class ReplayTests
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
                log.Append("  FAIL ").Append(label).Append('\n');
                if (detail.Length > 0)
                {
                    log.Append("       ").Append(detail.Replace("\n", "\n       ")).Append('\n');
                }
            }
        }

        log.Append("=== Replay Tests ===\n");

        var recording = BuildSampleRecording();
        var serialized = ReplaySerializer.Serialize(recording);
        var parsed = ReplaySerializer.Parse(serialized);
        var reserialized = ReplaySerializer.Serialize(parsed);

        Check(
            serialized == reserialized,
            "serializer round-trips exactly",
            $"first:\n{serialized}\nsecond:\n{reserialized}");

        Check(
            parsed.Header.Seed == recording.Header.Seed
                && parsed.Header.TickRate == recording.Header.TickRate
                && parsed.Header.PlayerCount == recording.Header.PlayerCount
                && parsed.Header.FrameCount == recording.Header.FrameCount
                && parsed.Header.MissionId == recording.Header.MissionId
                && parsed.Header.WorldId == recording.Header.WorldId,
            "header fields survive round-trip",
            $"seed={parsed.Header.Seed:X} tickrate={parsed.Header.TickRate} "
                + $"players={parsed.Header.PlayerCount} frames={parsed.Header.FrameCount} "
                + $"mission={parsed.Header.MissionId} world={parsed.Header.WorldId}");

        Check(
            parsed.Header.Forms.Count == 2
                && parsed.Header.Forms[0].PlayerId == 1
                && parsed.Header.Forms[0].FormId == "mannequin_base"
                && parsed.Header.Forms[1].PlayerId == 2
                && parsed.Header.Forms[1].FormId == "archive_knight_form",
            "form entries survive round-trip",
            $"count={parsed.Header.Forms.Count}");

        var inputsMatch = parsed.Inputs.Count == recording.Inputs.Count;
        for (var index = 0; inputsMatch && index < parsed.Inputs.Count; index++)
        {
            var expected = recording.Inputs[index];
            var actual = parsed.Inputs[index];
            if (actual.Tick != expected.Tick
                || actual.Held.Count != expected.Held.Count
                || actual.HeldForSlot(0) != expected.HeldForSlot(0)
                || actual.HeldForSlot(1) != expected.HeldForSlot(1))
            {
                inputsMatch = false;
            }
        }

        Check(inputsMatch, "input samples survive round-trip", $"count={parsed.Inputs.Count}");

        Check(
            parsed.StateChecks.Count == recording.StateChecks.Count
                && parsed.StateChecks[0] == recording.StateChecks[0],
            "state checks survive round-trip",
            $"count={parsed.StateChecks.Count}");

        var source = new ReplayInputSource(parsed);
        var playbackFaithful = true;
        var playbackDetail = "";
        foreach (var sample in recording.Inputs)
        {
            var p1 = source.HeldFor(sample.Tick, 1);
            var p2 = source.HeldFor(sample.Tick, 2);
            if ((uint)p1 != sample.HeldForSlot(0) || (uint)p2 != sample.HeldForSlot(1))
            {
                playbackFaithful = false;
                playbackDetail = $"tick {sample.Tick}: got p1={(uint)p1:X} p2={(uint)p2:X}, "
                    + $"expected p1={sample.HeldForSlot(0):X} p2={sample.HeldForSlot(1):X}";
                break;
            }
        }

        Check(playbackFaithful, "playback reproduces recorded inputs", playbackDetail);

        Check(
            source.HeldFor(999999, 1) == InputButtons.None,
            "playback returns neutral for unknown ticks");

        Check(
            source.HeldFor(recording.Inputs[0].Tick, 99) == InputButtons.None,
            "playback returns neutral for unknown players");

        var hashA = FoldStateSample(1);
        var hashARepeat = FoldStateSample(1);
        var hashB = FoldStateSample(2);

        Check(hashA == hashARepeat, "fingerprint is deterministic", $"{hashA:X} vs {hashARepeat:X}");
        Check(hashA != hashB, "fingerprint reacts to state change", $"{hashA:X} vs {hashB:X}");

        var baseHash = ReplayFingerprint.Begin();
        var quantizedSame = ReplayFingerprint.CombineFloat(baseHash, 1.00003f);
        var quantizedBase = ReplayFingerprint.CombineFloat(baseHash, 1.0f);
        var quantizedFar = ReplayFingerprint.CombineFloat(baseHash, 2.0f);

        Check(
            quantizedSame == quantizedBase,
            "fingerprint quantizes sub-quantum float noise",
            $"{quantizedSame:X} vs {quantizedBase:X}");

        Check(
            quantizedBase != quantizedFar,
            "fingerprint separates real float drift",
            $"{quantizedBase:X} vs {quantizedFar:X}");

        var canonical = ReplaySerializer.Parse(CanonicalReplayText());
        Check(
            canonical.Header.Seed == 0xABCDEF01u
                && canonical.Header.WorldId == "archive_nexus"
                && canonical.Header.PlayerCount == 1
                && canonical.Header.FrameCount == 2
                && canonical.Inputs.Count == 2
                && canonical.Inputs[1].HeldForSlot(0) == 0x8u
                && canonical.StateChecks.Count == 1
                && canonical.StateChecks[0].Fingerprint == 0xDEADBEEFUL,
            "parses a hand-authored canonical replay",
            $"seed={canonical.Header.Seed:X} world={canonical.Header.WorldId} "
                + $"frames={canonical.Header.FrameCount} inputs={canonical.Inputs.Count}");

        var empty = new ReplayRecording(
            new ReplayHeader { PlayerCount = 1, FrameCount = 0, WorldId = "training_room" },
            System.Array.Empty<ReplayInputSample>(),
            System.Array.Empty<ReplayStateCheck>());
        var emptyRoundTrip = ReplaySerializer.Serialize(ReplaySerializer.Parse(ReplaySerializer.Serialize(empty)));
        var emptySource = new ReplayInputSource(empty);
        Check(
            emptyRoundTrip == ReplaySerializer.Serialize(empty)
                && emptySource.HeldFor(0, 1) == InputButtons.None,
            "empty recording round-trips and plays back safely");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }

    private static ReplayRecording BuildSampleRecording()
    {
        var recorder = new ReplayRecorder
        {
            Seed = 0x1234ABCDu,
            PlayerCount = 2,
            MissionId = "archive_nexus",
            WorldId = "archive_nexus",
        };

        recorder.SetForms(new[]
        {
            new ReplayFormEntry(1, "mannequin_base"),
            new ReplayFormEntry(2, "archive_knight_form"),
        });

        recorder.RecordInputs(1, new[] { (uint)(InputButtons.Right | InputButtons.Dash), 0u });
        recorder.RecordInputs(2, new[] { (uint)InputButtons.Jump, (uint)InputButtons.Block });
        recorder.RecordInputs(3, new[] { (uint)InputButtons.LightPunch, 0u });
        recorder.RecordInputs(4, new[] { 0u, (uint)(InputButtons.Crouch | InputButtons.HeavyKick) });

        recorder.RecordStateCheck(3, 0x0123456789ABCDEFUL);

        return recorder.Build();
    }

    private static ulong FoldStateSample(int healthDelta)
    {
        var hash = ReplayFingerprint.Begin();
        hash = ReplayFingerprint.CombineInt(hash, 42);
        hash = ReplayFingerprint.CombineBool(hash, true);
        hash = ReplayFingerprint.CombineInt(hash, 100 - healthDelta);
        hash = ReplayFingerprint.CombineFloat(hash, 3.5f);
        hash = ReplayFingerprint.CombineString(hash, "mannequin_base");
        return hash;
    }

    private static string CanonicalReplayText()
    {
        return string.Join('\n', new List<string>
        {
            "# hand-authored replay",
            "PMREPLAY 1",
            "seed ABCDEF01",
            "tickrate 60",
            "mission archive_nexus",
            "world archive_nexus",
            "players 1",
            "frames 2",
            "form 1 mannequin_base",
            "I 1 40",
            "I 2 8",
            "S 2 DEADBEEF",
        });
    }
}
