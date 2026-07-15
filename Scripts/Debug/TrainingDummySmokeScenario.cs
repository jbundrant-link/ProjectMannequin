using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;

namespace ProjectMannequin.DebugTools;

/// <summary>
/// Verifies that a spawned training dummy actually performs its configured
/// behavior through the live simulation (input buffer -> state machine), not
/// just in the pure unit tests. Observes the dummy for a fixed window and
/// asserts the setting-appropriate, self-driven signal fired. Hit-dependent
/// settings (guard-after-first-hit, reversal) need a live attacker and are
/// covered by <c>TrainingDummyTests</c> instead.
///
/// Enabled with PROJECT_MANNEQUIN_TRAINING_SMOKE_TEST=1 alongside
/// PROJECT_MANNEQUIN_TRAINING_DUMMY=&lt;setting&gt;.
/// </summary>
public sealed class TrainingDummySmokeScenario
{
    private const int SummaryTick = 150;

    private readonly GameSimulation _simulation;
    private readonly TrainingDummySetting _setting;
    private bool _summaryPrinted;
    private int _moveCount;
    private float _maxHeight;
    private bool _sawCrouch;
    private bool _sawBlock;
    private bool _sawParry;
    private bool _sawAttack;

    public TrainingDummySmokeScenario(GameSimulation simulation, TrainingDummySetting setting)
    {
        _simulation = simulation;
        _setting = setting;
    }

    public void CaptureAfterSimulation(int tick, IReadOnlyCollection<CombatPresentationEvent> events)
    {
        var dummy = GetDummy();
        if (dummy is null)
        {
            return;
        }

        _maxHeight = Mathf.Max(_maxHeight, dummy.SimPosition.Y);
        _sawCrouch |= dummy.State == CombatActorState.Crouching;
        _sawBlock |= dummy.State == CombatActorState.Blocking;
        _sawParry |= dummy.State == CombatActorState.Parrying;
        _sawAttack |= dummy.State == CombatActorState.Attacking;

        foreach (var presentationEvent in events)
        {
            if (presentationEvent.SourceActorId == dummy.ActorId
                && presentationEvent.Type == CombatPresentationEventType.MoveStarted)
            {
                _moveCount++;
                GD.Print($"[TrainingSmoke] tick {tick}: dummy MoveStarted {presentationEvent.Payload}");
            }
        }

        if (tick < SummaryTick || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var passed = _setting switch
        {
            TrainingDummySetting.Crouch => _sawCrouch,
            TrainingDummySetting.Jump => _maxHeight > 0.1f,
            TrainingDummySetting.GuardAll => _sawBlock,
            TrainingDummySetting.MashJab => _moveCount >= 2,
            TrainingDummySetting.ParryAttempt => _sawParry,
            TrainingDummySetting.Stand => !_sawBlock && !_sawParry && _moveCount == 0 && _maxHeight <= 0.1f,
            _ => true,
        };

        GD.Print(
            $"[TrainingSmoke] SUMMARY setting={_setting} passed={passed} moves={_moveCount} "
            + $"maxY={_maxHeight:0.00} crouch={_sawCrouch} block={_sawBlock} parry={_sawParry} attack={_sawAttack}");
        if (!passed)
        {
            GD.PushError($"[TrainingSmoke] Training dummy behavior '{_setting}' was not observed.");
        }
    }

    private CombatActor? GetDummy()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.ActorId == "training_dummy");
    }
}
