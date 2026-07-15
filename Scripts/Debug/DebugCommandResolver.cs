using Godot;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// The set of designer/debug actions the overlay can trigger. Kept as data so
/// the key mapping is pure and unit testable, and so the actions stay optional
/// tooling that only ever reads or nudges simulation state.
/// </summary>
public enum DebugCommand
{
    None,
    ToggleOverlay,
    ToggleFrameInspector,
    ExportFrameData,
    KillEnemies,
    SkipToNextEncounter,
    SpawnDebugProp,
    RefillResources,
    ToggleDummyGuard,
    AdvanceBossPhase,
    ForceKnockdown,
}

/// <summary>
/// Maps physical keys to <see cref="DebugCommand"/>s. Function keys are always
/// available; the number-row actions are intended to be gated behind the
/// overlay being visible by the caller.
/// </summary>
public static class DebugCommandResolver
{
    public static DebugCommand Resolve(Key key)
    {
        return key switch
        {
            Key.F1 => DebugCommand.ToggleOverlay,
            Key.F2 => DebugCommand.ToggleFrameInspector,
            Key.F3 => DebugCommand.ExportFrameData,
            Key.Key1 => DebugCommand.KillEnemies,
            Key.Key2 => DebugCommand.SkipToNextEncounter,
            Key.Key3 => DebugCommand.SpawnDebugProp,
            Key.Key4 => DebugCommand.RefillResources,
            Key.Key5 => DebugCommand.ToggleDummyGuard,
            Key.Key6 => DebugCommand.AdvanceBossPhase,
            Key.Key7 => DebugCommand.ForceKnockdown,
            _ => DebugCommand.None,
        };
    }

    /// <summary>
    /// True when the command is a global toggle available even while the overlay
    /// is hidden. Action commands require the overlay to be visible first.
    /// </summary>
    public static bool IsAlwaysAvailable(DebugCommand command)
    {
        return command is DebugCommand.ToggleOverlay
            or DebugCommand.ToggleFrameInspector
            or DebugCommand.ExportFrameData;
    }
}
