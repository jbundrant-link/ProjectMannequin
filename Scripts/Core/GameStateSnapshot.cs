using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Core;

/// <summary>
/// An immutable capture of a single actor's authoritative simulation state.
/// Captures exactly the fields that determine combat outcomes (health, meter,
/// guard, state, facing, position, velocity, current move, combo, boss phase,
/// invulnerability, and the actor's deterministic RNG state) so it can be
/// restored or compared for rollback and replay tooling.
/// </summary>
public readonly record struct ActorStateSnapshot(
    string ActorId,
    int Health,
    int Meter,
    int GuardGauge,
    CombatActorState State,
    bool FacingRight,
    Vector3 SimPosition,
    Vector3 Velocity,
    string CurrentMoveId,
    int CurrentMoveFrame,
    int ComboHitCount,
    int BossPhaseIndex,
    int InvincibilityFrames,
    int RemainingJumps,
    uint CpuRandomState);

/// <summary>
/// An immutable capture of the whole simulation's authoritative state: the tick,
/// the frozen-aware presentation clock, the active freeze counters, and every
/// actor snapshot. Provides a field-level <see cref="Diff"/> so future rollback
/// or replay tools can pinpoint exactly which value diverged rather than only
/// seeing a hash mismatch.
/// </summary>
public sealed class GameStateSnapshot
{
    public int Tick { get; init; }
    public int PresentationClock { get; init; }
    public int HitStopFramesRemaining { get; init; }
    public int SuperPauseFramesRemaining { get; init; }
    public IReadOnlyList<ActorStateSnapshot> Actors { get; init; } = System.Array.Empty<ActorStateSnapshot>();

    public bool Matches(GameStateSnapshot other)
    {
        return Diff(other).Count == 0;
    }

    public IReadOnlyList<string> Diff(GameStateSnapshot other)
    {
        var diffs = new List<string>();

        if (Tick != other.Tick)
        {
            diffs.Add($"tick {Tick} -> {other.Tick}");
        }

        if (PresentationClock != other.PresentationClock)
        {
            diffs.Add($"presentationClock {PresentationClock} -> {other.PresentationClock}");
        }

        if (HitStopFramesRemaining != other.HitStopFramesRemaining)
        {
            diffs.Add($"hitstop {HitStopFramesRemaining} -> {other.HitStopFramesRemaining}");
        }

        if (SuperPauseFramesRemaining != other.SuperPauseFramesRemaining)
        {
            diffs.Add($"superpause {SuperPauseFramesRemaining} -> {other.SuperPauseFramesRemaining}");
        }

        var otherById = other.Actors.ToDictionary(actor => actor.ActorId);
        foreach (var actor in Actors)
        {
            if (!otherById.TryGetValue(actor.ActorId, out var otherActor))
            {
                diffs.Add($"{actor.ActorId}: present here, missing in other");
                continue;
            }

            CompareActor(actor, otherActor, diffs);
        }

        foreach (var otherActor in other.Actors)
        {
            if (Actors.All(actor => actor.ActorId != otherActor.ActorId))
            {
                diffs.Add($"{otherActor.ActorId}: only in other");
            }
        }

        return diffs;
    }

    private static void CompareActor(ActorStateSnapshot a, ActorStateSnapshot b, List<string> diffs)
    {
        var id = a.ActorId;
        if (a.Health != b.Health)
        {
            diffs.Add($"{id} health {a.Health} -> {b.Health}");
        }

        if (a.Meter != b.Meter)
        {
            diffs.Add($"{id} meter {a.Meter} -> {b.Meter}");
        }

        if (a.GuardGauge != b.GuardGauge)
        {
            diffs.Add($"{id} guard {a.GuardGauge} -> {b.GuardGauge}");
        }

        if (a.State != b.State)
        {
            diffs.Add($"{id} state {a.State} -> {b.State}");
        }

        if (a.FacingRight != b.FacingRight)
        {
            diffs.Add($"{id} facing {a.FacingRight} -> {b.FacingRight}");
        }

        if (!Approximately(a.SimPosition, b.SimPosition))
        {
            diffs.Add($"{id} pos {Format(a.SimPosition)} -> {Format(b.SimPosition)}");
        }

        if (!Approximately(a.Velocity, b.Velocity))
        {
            diffs.Add($"{id} vel {Format(a.Velocity)} -> {Format(b.Velocity)}");
        }

        if (a.CurrentMoveId != b.CurrentMoveId)
        {
            diffs.Add($"{id} move '{a.CurrentMoveId}' -> '{b.CurrentMoveId}'");
        }

        if (a.CurrentMoveFrame != b.CurrentMoveFrame)
        {
            diffs.Add($"{id} moveFrame {a.CurrentMoveFrame} -> {b.CurrentMoveFrame}");
        }

        if (a.ComboHitCount != b.ComboHitCount)
        {
            diffs.Add($"{id} combo {a.ComboHitCount} -> {b.ComboHitCount}");
        }

        if (a.BossPhaseIndex != b.BossPhaseIndex)
        {
            diffs.Add($"{id} bossPhase {a.BossPhaseIndex} -> {b.BossPhaseIndex}");
        }

        if (a.InvincibilityFrames != b.InvincibilityFrames)
        {
            diffs.Add($"{id} invuln {a.InvincibilityFrames} -> {b.InvincibilityFrames}");
        }

        if (a.RemainingJumps != b.RemainingJumps)
        {
            diffs.Add($"{id} jumps {a.RemainingJumps} -> {b.RemainingJumps}");
        }

        if (a.CpuRandomState != b.CpuRandomState)
        {
            diffs.Add($"{id} rng {a.CpuRandomState:X} -> {b.CpuRandomState:X}");
        }
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return a.DistanceSquaredTo(b) < 1e-6f;
    }

    private static string Format(Vector3 value)
    {
        return $"({value.X:0.###},{value.Y:0.###},{value.Z:0.###})";
    }
}
