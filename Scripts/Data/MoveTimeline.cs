using System.Collections.Generic;
using System.Text;
using ProjectMannequin.Combat;

namespace ProjectMannequin.Data;

public enum MoveFramePhase
{
    Startup,
    Active,
    Recovery,
}

/// <summary>
/// The overlaid gameplay windows that can be simultaneously active on a single
/// frame of a move (a frame can be, say, Active + Cancelable + Invulnerable).
/// </summary>
[System.Flags]
public enum MoveFrameWindow
{
    None = 0,
    Hit = 1 << 0,
    Hurt = 1 << 1,
    Throw = 1 << 2,
    Armor = 1 << 3,
    WeakPoint = 1 << 4,
    Projectile = 1 << 5,
    Invulnerable = 1 << 6,
    Cancelable = 1 << 7,
    JumpCancelable = 1 << 8,
}

public readonly record struct MoveFrameInfo(int Frame, MoveFramePhase Phase, MoveFrameWindow Windows)
{
    public bool Has(MoveFrameWindow window)
    {
        return (Windows & window) != MoveFrameWindow.None;
    }
}

/// <summary>
/// A pure, per-frame breakdown of a move's startup/active/recovery phases and
/// its overlaid windows (hit, hurt, throw, armor, weak point, projectile,
/// invulnerability, cancel, jump-cancel). This is the data source for the
/// frame-data inspector and the debug overlay's window visualization, and it is
/// deterministic so it can be unit tested and exported for balancing.
/// </summary>
public sealed class MoveTimeline
{
    private MoveTimeline(
        string moveId,
        string displayName,
        int startupFrames,
        int activeFrames,
        int recoveryFrames,
        IReadOnlyList<MoveFrameInfo> frames)
    {
        MoveId = moveId;
        DisplayName = displayName;
        StartupFrames = startupFrames;
        ActiveFrames = activeFrames;
        RecoveryFrames = recoveryFrames;
        Frames = frames;
    }

    public string MoveId { get; }
    public string DisplayName { get; }
    public int StartupFrames { get; }
    public int ActiveFrames { get; }
    public int RecoveryFrames { get; }
    public int TotalFrames => Frames.Count;
    public IReadOnlyList<MoveFrameInfo> Frames { get; }

    public static MoveTimeline Build(MoveData move)
    {
        var startup = System.Math.Max(0, move.StartupFrames);
        var active = System.Math.Max(0, move.ActiveFrames);
        var recovery = System.Math.Max(0, move.RecoveryFrames);
        var total = startup + active + recovery;
        var lastActiveFrame = startup + active - 1;

        var frames = new List<MoveFrameInfo>(total);
        for (var frame = 0; frame < total; frame++)
        {
            var phase = frame < startup
                ? MoveFramePhase.Startup
                : frame <= lastActiveFrame
                    ? MoveFramePhase.Active
                    : MoveFramePhase.Recovery;

            var windows = MoveFrameWindow.None;
            if (frame < move.InvulnerableStartupFrames)
            {
                windows |= MoveFrameWindow.Invulnerable;
            }

            if (move.IsInCancelWindow(frame))
            {
                windows |= MoveFrameWindow.Cancelable;
            }

            if (move.IsInJumpCancelWindow(frame))
            {
                windows |= MoveFrameWindow.JumpCancelable;
            }

            foreach (var box in move.CombatBoxes)
            {
                if (!box.IsActiveOnFrame(frame))
                {
                    continue;
                }

                windows |= box.BoxType switch
                {
                    CombatBoxType.Hitbox => MoveFrameWindow.Hit,
                    CombatBoxType.Hurtbox => MoveFrameWindow.Hurt,
                    CombatBoxType.Grabbox => MoveFrameWindow.Throw,
                    CombatBoxType.ArmorBox => MoveFrameWindow.Armor,
                    CombatBoxType.WeakPointBox => MoveFrameWindow.WeakPoint,
                    CombatBoxType.ProjectileBox => MoveFrameWindow.Projectile,
                    _ => MoveFrameWindow.None,
                };
            }

            frames.Add(new MoveFrameInfo(frame, phase, windows));
        }

        return new MoveTimeline(move.Id, move.DisplayName, startup, active, recovery, frames);
    }

    public MoveFrameInfo FrameAt(int moveFrame)
    {
        if (Frames.Count == 0)
        {
            return new MoveFrameInfo(0, MoveFramePhase.Startup, MoveFrameWindow.None);
        }

        var clamped = System.Math.Clamp(moveFrame, 0, Frames.Count - 1);
        return Frames[clamped];
    }

    /// <summary>Phase letters per frame: S startup, A active, R recovery.</summary>
    public string ToPhaseStrip()
    {
        var builder = new StringBuilder(Frames.Count);
        foreach (var info in Frames)
        {
            builder.Append(info.Phase switch
            {
                MoveFramePhase.Startup => 'S',
                MoveFramePhase.Active => 'A',
                _ => 'R',
            });
        }

        return builder.ToString();
    }

    /// <summary>
    /// The phase strip with a caret under the current frame, e.g. for the live
    /// inspector: "SSSS AAA RRR" with the cursor marking playback position.
    /// </summary>
    public string ToCursorStrip(int currentFrame)
    {
        var builder = new StringBuilder(Frames.Count);
        for (var frame = 0; frame < Frames.Count; frame++)
        {
            builder.Append(frame == System.Math.Clamp(currentFrame, 0, System.Math.Max(0, Frames.Count - 1))
                ? '^'
                : '.');
        }

        return builder.ToString();
    }

    /// <summary>
    /// One marker row per window type that appears anywhere in the move, e.g.
    /// "hit    : ....HHH..........". Only present windows produce a row so the
    /// output stays compact.
    /// </summary>
    public IReadOnlyList<string> ToWindowRows()
    {
        var rows = new List<string>();
        AppendWindowRow(rows, "hit", MoveFrameWindow.Hit, 'H');
        AppendWindowRow(rows, "throw", MoveFrameWindow.Throw, 'T');
        AppendWindowRow(rows, "armor", MoveFrameWindow.Armor, 'M');
        AppendWindowRow(rows, "invuln", MoveFrameWindow.Invulnerable, 'I');
        AppendWindowRow(rows, "cancel", MoveFrameWindow.Cancelable, 'C');
        AppendWindowRow(rows, "jumpc", MoveFrameWindow.JumpCancelable, 'J');
        AppendWindowRow(rows, "weak", MoveFrameWindow.WeakPoint, 'W');
        AppendWindowRow(rows, "proj", MoveFrameWindow.Projectile, 'P');
        AppendWindowRow(rows, "hurt", MoveFrameWindow.Hurt, 'U');
        return rows;
    }

    private void AppendWindowRow(List<string> rows, string label, MoveFrameWindow window, char marker)
    {
        var present = false;
        var builder = new StringBuilder(Frames.Count);
        foreach (var info in Frames)
        {
            if (info.Has(window))
            {
                present = true;
                builder.Append(marker);
            }
            else
            {
                builder.Append('.');
            }
        }

        if (present)
        {
            rows.Add($"{label,-7}: {builder}");
        }
    }
}
