using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectMannequin.Data;

/// <summary>
/// Builds a readable frame-data report for a set of forms: startup, active,
/// recovery, best-case on-hit and on-block frame advantage, and cancel routes,
/// followed by any authoring-validation issues. Used by the Task 2 attack-data
/// pass to make combat data inspectable and to fail loudly on bad data.
/// </summary>
public static class FrameDataReport
{
    public static string Build(IEnumerable<CharacterData> forms)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Project Mannequin Frame-Data Report ===");

        var totalIssues = 0;
        foreach (var form in forms)
        {
            sb.AppendLine();
            sb.AppendLine($"# {form.Id} ({form.DisplayName}) - {form.Moves.Count} moves, meter {form.MaxMeter}");
            sb.AppendLine("  move               start active recov  onHit onBlk whiff cancels");

            foreach (var move in form.Moves)
            {
                var cancels = move.CancelIntoMoveIds.Count > 0
                    ? string.Join(",", move.CancelIntoMoveIds)
                    : "-";
                sb.AppendLine(
                    $"  {Truncate(move.Id, 18),-18} {move.StartupFrames,5} {move.ActiveFrames,6} {move.RecoveryFrames,5} "
                    + $"{Signed(FrameAdvantageOnHit(move)),6} {Signed(FrameAdvantageOnBlock(move)),5} "
                    + $"{WhiffRecovery(move),5}  {cancels}");
            }

            var issues = MoveValidation.ValidateForm(form);
            totalIssues += issues.Count;
            foreach (var issue in issues)
            {
                sb.AppendLine($"  ! {issue}");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"=== {totalIssues} validation issue(s) across all forms ===");
        return sb.ToString();
    }

    /// <summary>Best-case on-hit advantage assuming contact on the first active frame.</summary>
    public static int FrameAdvantageOnHit(MoveData move)
    {
        return move.HitstunFrames - ((move.ActiveFrames - 1) + move.RecoveryFrames);
    }

    /// <summary>Best-case on-block advantage assuming contact on the first active frame.</summary>
    public static int FrameAdvantageOnBlock(MoveData move)
    {
        return move.BlockstunFrames - ((move.ActiveFrames - 1) + move.RecoveryFrames);
    }

    /// <summary>
    /// Frames the attacker is committed after the first active frame on a whiff,
    /// i.e. the opponent's punish window.
    /// </summary>
    public static int WhiffRecovery(MoveData move)
    {
        return (move.ActiveFrames - 1) + move.RecoveryFrames;
    }

    private static string Signed(int value)
    {
        return value >= 0 ? $"+{value}" : value.ToString();
    }

    private static string Truncate(string value, int max)
    {
        return value.Length <= max ? value : value[..max];
    }
}
