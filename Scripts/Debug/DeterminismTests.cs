using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless tests for Task 11 determinism tooling: the state snapshot/compare
/// helpers detect divergence in exactly the fields the roadmap calls out
/// (health, meter, states, positions, combo counts, boss phases, RNG), and the
/// determinism audit documents every authoritative subsystem. Failures print
/// the produced diff so a regression is diagnosable.
///
/// Run with the environment flag PROJECT_MANNEQUIN_DETERMINISM_TEST=1.
/// </summary>
public static class DeterminismTests
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
                    log.Append("       ").Append(detail).Append('\n');
                }
            }
        }

        log.Append("=== Determinism Tests ===\n");

        // Audit coverage.
        var requiredSubsystems = new[]
        {
            "Movement", "Hit resolution", "Projectiles", "Hazards",
            "Boss / CPU AI", "Command parsing", "Replay capture",
        };
        var auditNames = DeterminismAudit.Entries.Select(entry => entry.Subsystem).ToHashSet();
        Check(
            requiredSubsystems.All(auditNames.Contains),
            "audit covers every authoritative subsystem",
            string.Join(",", auditNames));

        Check(
            DeterminismAudit.Entries
                .Where(entry => entry.Subsystem != "Presentation")
                .All(entry => entry.Deterministic),
            "all simulation subsystems are marked deterministic");

        Check(
            DeterminismAudit.Entries.Any(entry => entry.Subsystem == "Presentation" && !entry.Deterministic),
            "presentation is flagged as non-authoritative");

        Check(DeterminismAudit.BuildReport().Contains("Fixed-Tick Determinism Audit"), "audit report builds");

        // Snapshot equality and field-level diffs.
        var baseline = SnapshotWith(Baseline());
        Check(baseline.Matches(SnapshotWith(Baseline())), "identical snapshots match with no diff");

        void ExpectDiff(string label, ActorStateSnapshot changed, string expectedFragment)
        {
            var diff = baseline.Diff(SnapshotWith(changed));
            var found = diff.Any(entry => entry.Contains(expectedFragment));
            Check(found, label, $"diff=[{string.Join(" | ", diff)}]");
        }

        ExpectDiff("detects health divergence", Baseline() with { Health = 80 }, "health");
        ExpectDiff("detects meter divergence", Baseline() with { Meter = 42 }, "meter");
        ExpectDiff("detects state divergence", Baseline() with { State = CombatActorState.Hitstun }, "state");
        ExpectDiff("detects position divergence", Baseline() with { SimPosition = new Vector3(9, 0, 0) }, "pos");
        ExpectDiff("detects combo-count divergence", Baseline() with { ComboHitCount = 5 }, "combo");
        ExpectDiff("detects boss-phase divergence", Baseline() with { BossPhaseIndex = 2 }, "bossPhase");
        ExpectDiff("detects rng-state divergence", Baseline() with { CpuRandomState = 0xDEADBEEF }, "rng");

        // Game-level fields.
        var gameA = new GameStateSnapshot { Tick = 100, HitStopFramesRemaining = 0, Actors = new[] { Baseline() } };
        var gameB = new GameStateSnapshot { Tick = 101, HitStopFramesRemaining = 4, Actors = new[] { Baseline() } };
        var gameDiff = gameA.Diff(gameB);
        Check(
            gameDiff.Any(d => d.Contains("tick")) && gameDiff.Any(d => d.Contains("hitstop")),
            "detects tick and hitstop divergence",
            string.Join(" | ", gameDiff));

        // Actor set changes.
        var withActor = new GameStateSnapshot { Actors = new[] { Baseline() } };
        var withoutActor = new GameStateSnapshot { Actors = System.Array.Empty<ActorStateSnapshot>() };
        Check(
            withActor.Diff(withoutActor).Any(d => d.Contains("missing in other")),
            "detects a missing actor");

        // A near-identical position within tolerance must NOT diff (float noise).
        Check(
            baseline.Matches(SnapshotWith(Baseline() with { SimPosition = new Vector3(1.0000001f, 2.0f, 0.0f) })),
            "sub-epsilon position noise is not a divergence");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }

    private static ActorStateSnapshot Baseline()
    {
        return new ActorStateSnapshot(
            "player_1",
            Health: 100,
            Meter: 0,
            GuardGauge: 50,
            State: CombatActorState.Idle,
            FacingRight: true,
            SimPosition: new Vector3(1.0f, 2.0f, 0.0f),
            Velocity: Vector3.Zero,
            CurrentMoveId: "",
            CurrentMoveFrame: 0,
            ComboHitCount: 0,
            BossPhaseIndex: -1,
            InvincibilityFrames: 0,
            RemainingJumps: 1,
            CpuRandomState: 0x1234u);
    }

    private static GameStateSnapshot SnapshotWith(ActorStateSnapshot actor)
    {
        return new GameStateSnapshot
        {
            Tick = 100,
            Actors = new List<ActorStateSnapshot> { actor },
        };
    }
}
