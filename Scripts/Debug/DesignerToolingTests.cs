using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Headless tests for the Task 9 designer tooling: the pure per-frame
/// <see cref="MoveTimeline"/> (startup/active/recovery phases plus overlaid
/// hit/armor/throw/invuln/cancel windows) and the debug command key mapping.
/// Failures print the offending strip or mapping so authoring bugs are
/// diagnosable.
///
/// Run with the environment flag PROJECT_MANNEQUIN_TOOLING_TEST=1.
/// </summary>
public static class DesignerToolingTests
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

        log.Append("=== Designer Tooling Tests ===\n");

        var move = new MoveData
        {
            Id = "test_move",
            DisplayName = "Test Move",
            StartupFrames = 4,
            ActiveFrames = 3,
            RecoveryFrames = 10,
            InvulnerableStartupFrames = 5,
            CancelStartFrame = 4,
            CancelEndFrame = 6,
            CombatBoxes = new List<CombatBoxDefinition>
            {
                new() { Id = "hit", BoxType = CombatBoxType.Hitbox, StartFrame = 4, EndFrame = 6 },
                new() { Id = "armor", BoxType = CombatBoxType.ArmorBox, StartFrame = 0, EndFrame = 3 },
                new() { Id = "throw", BoxType = CombatBoxType.Grabbox, StartFrame = 4, EndFrame = 4 },
            },
        };

        var timeline = MoveTimeline.Build(move);

        Check(timeline.TotalFrames == 17, "timeline spans startup+active+recovery", $"total={timeline.TotalFrames}");

        Check(
            timeline.FrameAt(0).Phase == MoveFramePhase.Startup
                && timeline.FrameAt(3).Phase == MoveFramePhase.Startup,
            "startup frames are classified");

        Check(
            timeline.FrameAt(4).Phase == MoveFramePhase.Active
                && timeline.FrameAt(6).Phase == MoveFramePhase.Active,
            "active frames are classified");

        Check(
            timeline.FrameAt(7).Phase == MoveFramePhase.Recovery
                && timeline.FrameAt(16).Phase == MoveFramePhase.Recovery,
            "recovery frames are classified");

        Check(
            timeline.FrameAt(4).Has(MoveFrameWindow.Hit)
                && timeline.FrameAt(6).Has(MoveFrameWindow.Hit)
                && !timeline.FrameAt(7).Has(MoveFrameWindow.Hit),
            "hit window matches active hitbox frames");

        Check(
            timeline.FrameAt(0).Has(MoveFrameWindow.Armor)
                && timeline.FrameAt(3).Has(MoveFrameWindow.Armor)
                && !timeline.FrameAt(4).Has(MoveFrameWindow.Armor),
            "armor window matches armor box frames");

        Check(
            timeline.FrameAt(4).Has(MoveFrameWindow.Throw)
                && !timeline.FrameAt(5).Has(MoveFrameWindow.Throw),
            "throw window matches grab box frames");

        Check(
            timeline.FrameAt(0).Has(MoveFrameWindow.Invulnerable)
                && timeline.FrameAt(4).Has(MoveFrameWindow.Invulnerable)
                && !timeline.FrameAt(5).Has(MoveFrameWindow.Invulnerable),
            "invulnerability covers the first N frames only");

        Check(
            timeline.FrameAt(4).Has(MoveFrameWindow.Cancelable)
                && timeline.FrameAt(6).Has(MoveFrameWindow.Cancelable)
                && !timeline.FrameAt(3).Has(MoveFrameWindow.Cancelable),
            "cancel window is honored");

        var phaseStrip = timeline.ToPhaseStrip();
        Check(
            phaseStrip == "SSSSAAARRRRRRRRRR",
            "phase strip renders S/A/R per frame",
            $"strip={phaseStrip}");

        var windowRows = timeline.ToWindowRows();
        Check(
            windowRows.Any(row => row.StartsWith("hit"))
                && windowRows.Any(row => row.StartsWith("armor"))
                && windowRows.Any(row => row.StartsWith("throw"))
                && windowRows.All(row => !row.StartsWith("weak")),
            "window rows list only the windows that appear",
            string.Join(" | ", windowRows));

        Check(
            timeline.ToCursorStrip(5)[5] == '^' && timeline.ToCursorStrip(5).Count(c => c == '^') == 1,
            "cursor strip marks exactly the current frame");

        var emptyTimeline = MoveTimeline.Build(new MoveData
        {
            Id = "empty",
            StartupFrames = 0,
            ActiveFrames = 0,
            RecoveryFrames = 0,
        });
        Check(
            emptyTimeline.TotalFrames == 0 && emptyTimeline.ToPhaseStrip().Length == 0,
            "zero-length move produces an empty timeline safely");

        // Debug command key mapping.
        Check(DebugCommandResolver.Resolve(Key.F1) == DebugCommand.ToggleOverlay, "F1 toggles the overlay");
        Check(DebugCommandResolver.Resolve(Key.F2) == DebugCommand.ToggleFrameInspector, "F2 toggles the frame inspector");
        Check(DebugCommandResolver.Resolve(Key.Key4) == DebugCommand.RefillResources, "Key 4 refills resources");
        Check(DebugCommandResolver.Resolve(Key.Key6) == DebugCommand.AdvanceBossPhase, "Key 6 advances the boss phase");
        Check(DebugCommandResolver.Resolve(Key.Z) == DebugCommand.None, "unmapped keys resolve to None");
        Check(
            DebugCommandResolver.IsAlwaysAvailable(DebugCommand.ToggleOverlay)
                && !DebugCommandResolver.IsAlwaysAvailable(DebugCommand.KillEnemies),
            "only the overlay toggle is available while hidden");

        log.Append($"=== {passed} passed, {failed} failed ===");
        return log.ToString();
    }
}
