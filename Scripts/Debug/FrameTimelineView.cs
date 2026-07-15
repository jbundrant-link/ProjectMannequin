using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Draws the current move's frame-window timeline as colored bars: a
/// startup/active/recovery phase strip plus a thin row per overlaid window
/// (hit, throw, armor, invulnerability, cancel, ...), with a cursor at the live
/// playback frame. Purely a presentation of <see cref="MoveTimeline"/> data; it
/// owns no combat state and is safe to leave hidden.
/// </summary>
public partial class FrameTimelineView : Control
{
    private static readonly (MoveFrameWindow Window, Color Color)[] WindowColors =
    {
        (MoveFrameWindow.Hit, new Color(0.95f, 0.30f, 0.30f)),
        (MoveFrameWindow.Throw, new Color(0.90f, 0.35f, 0.85f)),
        (MoveFrameWindow.Armor, new Color(0.98f, 0.66f, 0.24f)),
        (MoveFrameWindow.Invulnerable, new Color(0.35f, 0.85f, 0.95f)),
        (MoveFrameWindow.Cancelable, new Color(0.40f, 0.90f, 0.45f)),
        (MoveFrameWindow.JumpCancelable, new Color(0.65f, 0.85f, 0.40f)),
        (MoveFrameWindow.WeakPoint, new Color(0.95f, 0.85f, 0.35f)),
        (MoveFrameWindow.Projectile, new Color(0.55f, 0.55f, 0.95f)),
    };

    private const float BarWidth = 620.0f;
    private const float PhaseHeight = 20.0f;
    private const float RowHeight = 8.0f;

    private MoveTimeline? _timeline;
    private int _currentFrame;

    public void SetTimeline(MoveTimeline? timeline, int currentFrame)
    {
        _timeline = timeline;
        _currentFrame = currentFrame;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (!Visible || _timeline is null || _timeline.TotalFrames == 0)
        {
            return;
        }

        var total = _timeline.TotalFrames;
        var cellWidth = Mathf.Max(3.0f, BarWidth / total);
        var y = 0.0f;

        for (var frame = 0; frame < total; frame++)
        {
            var info = _timeline.Frames[frame];
            var color = info.Phase switch
            {
                MoveFramePhase.Startup => new Color(0.30f, 0.50f, 0.95f),
                MoveFramePhase.Active => new Color(0.95f, 0.30f, 0.30f),
                _ => new Color(0.48f, 0.48f, 0.54f),
            };
            DrawRect(new Rect2(frame * cellWidth, y, cellWidth - 1.0f, PhaseHeight), color);
        }

        y += PhaseHeight + 3.0f;

        foreach (var (window, color) in WindowColors)
        {
            if (!_timeline.Frames.Any(info => info.Has(window)))
            {
                continue;
            }

            for (var frame = 0; frame < total; frame++)
            {
                if (_timeline.Frames[frame].Has(window))
                {
                    DrawRect(new Rect2(frame * cellWidth, y, cellWidth - 1.0f, RowHeight), color);
                }
            }

            y += RowHeight + 1.0f;
        }

        var cursorX = Mathf.Clamp(_currentFrame, 0, total - 1) * cellWidth + cellWidth * 0.5f;
        DrawLine(new Vector2(cursorX, -2.0f), new Vector2(cursorX, y), Colors.White, 1.5f);
    }
}
