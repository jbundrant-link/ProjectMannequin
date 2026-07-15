using System.Collections.Generic;
using System.Text;

namespace ProjectMannequin.Core;

/// <summary>
/// A structured record of the fixed-tick determinism audit for the fighting
/// layer. Each entry names an authoritative subsystem and the concrete basis for
/// its determinism, so the guarantees are documented in code (and testable)
/// rather than assumed. This is documentation, not a gameplay dependency.
/// </summary>
public static class DeterminismAudit
{
    public readonly record struct AuditEntry(string Subsystem, bool Deterministic, string Basis);

    public static IReadOnlyList<AuditEntry> Entries { get; } = new[]
    {
        new AuditEntry(
            "Movement",
            true,
            "Integrated on the fixed 60Hz physics tick with constant per-tick math; no frame delta or wall-clock."),
        new AuditEntry(
            "Hit resolution",
            true,
            "Resolved over the stable registration-ordered actor list each tick; no randomness."),
        new AuditEntry(
            "Projectiles",
            true,
            "Spawned and advanced on the tick with tick-derived parameters; no wall-clock."),
        new AuditEntry(
            "Hazards",
            true,
            "Tick-driven damage and timers; no frame-rate coupling."),
        new AuditEntry(
            "Boss / CPU AI",
            true,
            "Xorshift RNG seeded from deterministic actor and profile ids (FNV of ActorId|ProfileId)."),
        new AuditEntry(
            "Wave spawns",
            true,
            "System.Random seeded from a tick-derived value; no unseeded randomness."),
        new AuditEntry(
            "Command parsing",
            true,
            "Reads the fixed-size input buffer by numeric frame offsets; no wall-clock."),
        new AuditEntry(
            "Replay capture",
            true,
            "Per-tick input masks and FNV-1a state fingerprints; cross-process reproduction verified."),
        new AuditEntry(
            "Presentation",
            false,
            "Cosmetic pulses/flashes use wall-clock and the frozen-aware presentation clock; never fed back into simulation."),
    };

    public static string BuildReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Fixed-Tick Determinism Audit ===");
        builder.AppendLine(
            "Authoritative simulation runs only on the 60Hz physics tick. No GD.Randf,");
        builder.AppendLine(
            "unseeded System.Random, DateTime, or Time.GetTicksMsec exists in the combat,");
        builder.AppendLine(
            "core, stage, data, input, or progression layers; wall-clock is confined to");
        builder.AppendLine("presentation and UI, which never influence simulation results.");
        builder.AppendLine();

        foreach (var entry in Entries)
        {
            builder.AppendLine($"  [{(entry.Deterministic ? "SIM " : "PRES")}] {entry.Subsystem}: {entry.Basis}");
        }

        builder.AppendLine();
        builder.AppendLine(
            "Snapshot helpers (GameStateSnapshot / ActorStateSnapshot) capture and compare");
        builder.AppendLine(
            "the authoritative state (health, meter, guard, state, position, velocity,");
        builder.AppendLine(
            "move, combo, boss phase, RNG) as the foundation for future rollback tooling.");
        return builder.ToString();
    }
}
