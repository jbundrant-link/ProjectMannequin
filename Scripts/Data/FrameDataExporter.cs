using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ProjectMannequin.Data;

/// <summary>
/// Composes an exportable, human-readable balancing report: the frame-data
/// table (startup/active/recovery, on-hit/on-block/whiff, cancels, validation
/// issues) followed by a per-move window timeline for every move. Used by the
/// designer export command and the frame-data export launch flag.
/// </summary>
public static class FrameDataExporter
{
    public static string BuildFullReport(IEnumerable<CharacterData> forms)
    {
        var formList = forms.ToList();
        var builder = new StringBuilder();
        builder.Append(FrameDataReport.Build(formList));
        builder.AppendLine();
        builder.AppendLine("=== Move Window Timelines ===");

        foreach (var form in formList)
        {
            builder.AppendLine();
            builder.AppendLine($"# {form.Id} ({form.DisplayName})");
            foreach (var move in form.Moves)
            {
                var timeline = MoveTimeline.Build(move);
                builder.AppendLine(
                    $"  {move.Id} - startup {timeline.StartupFrames}, active {timeline.ActiveFrames}, "
                    + $"recovery {timeline.RecoveryFrames}, total {timeline.TotalFrames}");
                builder.AppendLine($"    phase  : {timeline.ToPhaseStrip()}");
                foreach (var row in timeline.ToWindowRows())
                {
                    builder.AppendLine($"    {row}");
                }
            }
        }

        return builder.ToString();
    }
}
