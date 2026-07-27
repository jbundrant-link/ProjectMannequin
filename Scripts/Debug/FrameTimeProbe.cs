using System;
using System.Collections.Generic;
using Godot;

namespace ProjectMannequin.Debugging;

/// <summary>
/// Measures real per-frame cost of a rendered stage against the frame budget.
/// </summary>
/// <remarks>
/// Deliberately a standalone node rather than instrumentation inside
/// <c>PrototypeStageView</c>, so the production renderer keeps no measurement
/// state and the probe can be attached to anything later.
///
/// Two settings have to be forced or the numbers are meaningless. Vsync pins
/// every frame to the refresh interval, so a stage costing 3 ms and one
/// costing 15 ms both report 16.7 ms and the budget looks fine right up until
/// it is blown. An FPS cap does the same thing more coarsely. Both are
/// disabled here, which is why this must never run in a player session.
///
/// The first frames after a stage loads are dominated by shader compilation
/// and texture upload, so they are discarded rather than averaged in - they
/// describe loading, not steady-state rendering.
///
/// Windows are measured in WALL TIME, not frame count. With vsync off these
/// stages run past 1500 fps, so a few hundred frames would cover under a
/// fifth of a second and sample an empty arena before a single enemy has
/// spawned - reporting the cost of the cheapest moment in the stage as though
/// it were the whole stage. Seconds put the sample inside real combat.
/// </remarks>
public partial class FrameTimeProbe : Node
{
    /// <summary>Wall time discarded before sampling starts.</summary>
    private const double WarmupSeconds = 3.0;

    /// <summary>Wall time measured once warmed up.</summary>
    private const double SampleSeconds = 4.0;

    /// <summary>Guard against unbounded growth if a frame is pathologically fast.</summary>
    private const int MaxSamples = 60000;

    /// <summary>60 fps in milliseconds; the Phase 7 budget.</summary>
    private const double BudgetMilliseconds = 16.7;

    private readonly List<double> _samples = new(4096);
    private double _elapsed;
    private string _label = "stage";
    private bool _reported;

    public static FrameTimeProbe? AttachIfRequested(Node parent, string label)
    {
        if (!ProjectMannequin.Progression.MvpProgressStore.IsPersistenceDisabled)
        {
            return null;
        }
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_FRAME_TIME_PROBE") != "1")
        {
            return null;
        }
        if (DisplayServer.GetName() == "headless")
        {
            // The dummy driver does no rasterisation, so a frame time from it
            // would describe the harness rather than the stage.
            GD.Print("[FrameTime] SKIPPED reason=headless");
            return null;
        }

        var probe = new FrameTimeProbe { Name = "FrameTimeProbe", _label = label };
        parent.AddChild(probe);
        return probe;
    }

    public override void _Ready()
    {
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        Engine.MaxFps = 0;
        ProcessPriority = int.MaxValue;
    }

    public override void _Process(double delta)
    {
        if (_reported)
        {
            return;
        }

        _elapsed += delta;
        if (_elapsed <= WarmupSeconds)
        {
            return;
        }

        if (_samples.Count < MaxSamples)
        {
            _samples.Add(delta * 1000.0);
        }

        if (_elapsed < WarmupSeconds + SampleSeconds)
        {
            return;
        }

        Report();
    }

    private void Report()
    {
        _reported = true;
        if (_samples.Count == 0)
        {
            GD.Print($"[FrameTime] stage={_label} NO_SAMPLES");
            GetTree().Quit();
            return;
        }

        var ordered = _samples.ToArray();
        Array.Sort(ordered);

        var total = 0.0;
        foreach (var sample in ordered)
        {
            total += sample;
        }

        var over = 0;
        foreach (var sample in ordered)
        {
            if (sample > BudgetMilliseconds)
            {
                over++;
            }
        }

        var drawCalls = Performance.GetMonitor(
            Performance.Monitor.RenderTotalDrawCallsInFrame);
        var primitives = Performance.GetMonitor(
            Performance.Monitor.RenderTotalPrimitivesInFrame);

        GD.Print(
            $"[FrameTime] stage={_label} "
            + $"mean={total / ordered.Length:F3} "
            + $"p50={Percentile(ordered, 0.50):F3} "
            + $"p95={Percentile(ordered, 0.95):F3} "
            + $"p99={Percentile(ordered, 0.99):F3} "
            + $"max={ordered[^1]:F3} "
            + $"overBudgetPct={100.0 * over / ordered.Length:F2} "
            + $"budget={BudgetMilliseconds:F1} "
            + $"drawCalls={drawCalls:F0} "
            + $"primitives={primitives:F0} "
            + $"samples={ordered.Length}");

        GetTree().Quit();
    }

    private static double Percentile(double[] ordered, double fraction)
    {
        if (ordered.Length == 0)
        {
            return 0.0;
        }
        var index = (int)Math.Round(fraction * (ordered.Length - 1));
        return ordered[Math.Clamp(index, 0, ordered.Length - 1)];
    }
}
