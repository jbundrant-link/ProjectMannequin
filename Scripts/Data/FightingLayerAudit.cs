using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectMannequin.Data;

/// <summary>
/// Acceptance-pass audit for the fighting layer: confirms each reference boss
/// has the readable beats a duel needs (a move kit, phase shifts, a super, a
/// guard gauge, and an AI profile) and that authored move data sits inside sane
/// game-feel ranges. Pure and deterministic so it can gate the acceptance tests
/// and be exported for tuning sessions.
/// </summary>
public static class FightingLayerAudit
{
    public readonly record struct BossReadiness(
        string BossId,
        string DisplayName,
        int MoveCount,
        int PhaseCount,
        int SuperCount,
        bool HasGuardGauge,
        bool HasCpuProfile)
    {
        // A duel-ready boss needs a varied kit (neutral/pressure/punish), at
        // least one phase shift, a super beat, a guard gauge for defense/guard
        // break, and an AI profile that drives its beats.
        public bool IsReady =>
            MoveCount >= 3
            && PhaseCount >= 1
            && SuperCount >= 1
            && HasGuardGauge
            && HasCpuProfile;
    }

    public static BossReadiness AuditBoss(CharacterData boss)
    {
        return new BossReadiness(
            boss.Id,
            boss.DisplayName,
            boss.Moves.Count,
            boss.BossPhases.Count,
            boss.Moves.Count(move => move.IsSuper),
            boss.MaxGuardGauge > 0,
            boss.CpuProfile is not null);
    }

    /// <summary>Flags authored move data that falls outside sane game-feel ranges.</summary>
    public static IReadOnlyList<string> AuditGameFeel(IEnumerable<CharacterData> forms)
    {
        var issues = new List<string>();
        foreach (var form in forms)
        {
            foreach (var move in form.Moves)
            {
                var label = $"{form.Id}.{move.Id}";
                CheckRange(issues, label, "startup", move.StartupFrames, 1, 90);
                CheckRange(issues, label, "hitstop", move.HitStopFrames, 0, 40);
                CheckRange(issues, label, "hitstun", move.HitstunFrames, 0, 120);
                CheckRange(issues, label, "blockstun", move.BlockstunFrames, 0, 90);

                // Supers, cinematic finishers, and tagged signature specials are
                // allowed to deal much higher damage than bread-and-butter normals.
                var isSignatureMove = move.IsSuper
                    || move.IsCinematicSuper
                    || move.Tags.Contains("super")
                    || move.Tags.Contains("special");
                var damageCeiling = isSignatureMove ? 2000 : 500;
                CheckRange(issues, label, "damage", move.Damage, 0, damageCeiling);

                if (move.PushbackX < 0.0f || move.PushbackX > 40.0f)
                {
                    issues.Add($"{label}: pushback {move.PushbackX:0.##} out of [0,40]");
                }
            }
        }

        return issues;
    }

    public static string BuildReport(IReadOnlyList<CharacterData> bosses, IEnumerable<CharacterData> allForms)
    {
        var builder = new StringBuilder();
        builder.AppendLine("=== Fighting-Layer Acceptance Audit ===");

        foreach (var boss in bosses)
        {
            var readiness = AuditBoss(boss);
            builder.AppendLine(
                $"  {(readiness.IsReady ? "OK " : "!! ")}{readiness.DisplayName}: "
                + $"moves={readiness.MoveCount} phases={readiness.PhaseCount} supers={readiness.SuperCount} "
                + $"guard={readiness.HasGuardGauge} ai={readiness.HasCpuProfile}");
        }

        var feelIssues = AuditGameFeel(allForms);
        builder.AppendLine();
        builder.AppendLine(feelIssues.Count == 0
            ? "  game feel: all authored moves within tuning ranges."
            : $"  game feel: {feelIssues.Count} out-of-range value(s):");
        foreach (var issue in feelIssues)
        {
            builder.AppendLine($"    ! {issue}");
        }

        return builder.ToString();
    }

    private static void CheckRange(List<string> issues, string label, string field, int value, int min, int max)
    {
        if (value < min || value > max)
        {
            issues.Add($"{label}: {field} {value} out of [{min},{max}]");
        }
    }
}
