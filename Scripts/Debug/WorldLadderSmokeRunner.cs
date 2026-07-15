using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Stage;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Advances ladder smoke tests in small deterministic batches per process frame.
/// This keeps tests fast while still allowing Godot to flush spawned/freed nodes
/// and imported resources between batches.
/// </summary>
public partial class WorldLadderSmokeRunner : Node
{
    private const int TicksPerFrame = 8;
    private const int MaximumTicks = 6000;
    private GameSimulation? _simulation;
    private int _completedAtTick = -1;

    public void Initialize(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public override void _Process(double delta)
    {
        if (_simulation?.EncounterDirector is null)
        {
            return;
        }

        for (var index = 0; index < TicksPerFrame; index++)
        {
            if (_simulation.EncounterDirector.State == ArcadeStageState.Complete)
            {
                _completedAtTick = _completedAtTick < 0
                    ? _simulation.CurrentTick
                    : _completedAtTick;
                var needsLegacySmokeGrace = OS.GetEnvironment("PROJECT_MANNEQUIN_WORLD_WARRIOR_SMOKE_TEST") == "1"
                    || OS.GetEnvironment("PROJECT_MANNEQUIN_ASTRAL_SMOKE_TEST") == "1";
                var requiredGraceTicks = needsLegacySmokeGrace ? 900 : 1;
                if (_simulation.CurrentTick - _completedAtTick >= requiredGraceTicks)
                {
                    GetTree().Quit();
                    return;
                }
            }

            if (_simulation.CurrentTick >= MaximumTicks)
            {
                GD.PushError(
                    $"[FastSmoke] Runner timed out state={_simulation.EncounterDirector.State} "
                    + $"encounter={_simulation.EncounterDirector.CurrentEncounter.Id}.");
                GetTree().Quit();
                return;
            }

            _simulation.StepSimulation();
        }
    }
}
