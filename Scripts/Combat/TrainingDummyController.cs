using ProjectMannequin.Core;

namespace ProjectMannequin.Combat;

/// <summary>
/// Authored behaviors a training dummy can follow so a player can practice
/// against predictable opponents. Mirrors the standard training-mode dummy
/// options found in fighting games.
/// </summary>
public enum TrainingDummySetting
{
    Stand,
    Crouch,
    Jump,
    GuardAll,
    GuardAfterFirstHit,
    MashJab,
    ParryAttempt,
    Reversal,
}

/// <summary>
/// A pure snapshot of what the dummy needs to know this tick to choose an
/// action. Kept free of Godot types so behavior resolution is unit testable.
/// </summary>
public readonly record struct TrainingDummySituation(
    int Tick,
    CombatActorState State,
    bool Grounded,
    bool HasBeenHit,
    bool ActionableNow,
    bool RecoveredFromHit);

/// <summary>
/// Resolves a training dummy's per-tick input mask from its setting and the
/// current situation. Pure and deterministic: the same setting and situation
/// always yield the same mask. Behaviors are expressed as ordinary player
/// inputs so the dummy flows through the exact same state machine as a human.
/// </summary>
public static class TrainingDummyController
{
    public const int JumpPeriodFrames = 45;
    public const int MashPeriodFrames = 8;
    public const int ParryPeriodFrames = 20;

    public static InputButtons Resolve(TrainingDummySetting setting, in TrainingDummySituation situation)
    {
        return setting switch
        {
            TrainingDummySetting.Stand => InputButtons.None,
            TrainingDummySetting.Crouch => InputButtons.Crouch,
            TrainingDummySetting.Jump => situation.Grounded && OnBeat(situation.Tick, JumpPeriodFrames)
                ? InputButtons.Jump
                : InputButtons.None,
            TrainingDummySetting.GuardAll => InputButtons.Block,
            TrainingDummySetting.GuardAfterFirstHit => situation.HasBeenHit
                ? InputButtons.Block
                : InputButtons.None,
            TrainingDummySetting.MashJab => situation.ActionableNow && OnBeat(situation.Tick, MashPeriodFrames)
                ? InputButtons.LightPunch
                : InputButtons.None,
            TrainingDummySetting.ParryAttempt => situation.ActionableNow && OnBeat(situation.Tick, ParryPeriodFrames)
                ? InputButtons.MediumPunch | InputButtons.MediumKick
                : InputButtons.None,
            TrainingDummySetting.Reversal => situation.RecoveredFromHit
                ? InputButtons.HeavyPunch
                : InputButtons.None,
            _ => InputButtons.None,
        };
    }

    public static bool TryParse(string? raw, out TrainingDummySetting setting)
    {
        setting = TrainingDummySetting.Stand;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        switch (raw.Trim().ToLowerInvariant())
        {
            case "stand":
                setting = TrainingDummySetting.Stand;
                return true;
            case "crouch":
                setting = TrainingDummySetting.Crouch;
                return true;
            case "jump":
                setting = TrainingDummySetting.Jump;
                return true;
            case "guard_all":
            case "guardall":
            case "block":
                setting = TrainingDummySetting.GuardAll;
                return true;
            case "guard_after_first_hit":
            case "guardafterfirsthit":
            case "block_after_hit":
                setting = TrainingDummySetting.GuardAfterFirstHit;
                return true;
            case "mash_jab":
            case "mashjab":
            case "mash":
                setting = TrainingDummySetting.MashJab;
                return true;
            case "parry_attempt":
            case "parry":
                setting = TrainingDummySetting.ParryAttempt;
                return true;
            case "reversal":
            case "wakeup":
                setting = TrainingDummySetting.Reversal;
                return true;
            default:
                return false;
        }
    }

    private static bool OnBeat(int tick, int period)
    {
        return period > 0 && tick % period == 0;
    }
}

/// <summary>
/// Stateful driver that tracks the sticky flags a training dummy needs across
/// ticks (whether it has ever been hit, and whether it just recovered from a
/// hit reaction), then delegates to the pure <see cref="TrainingDummyController"/>.
/// Contains no Godot dependencies so it can be exercised in unit tests.
/// </summary>
public sealed class TrainingDummyBrain
{
    private bool _hasBeenHit;
    private bool _wasInHitReaction;

    public TrainingDummyBrain(TrainingDummySetting setting)
    {
        Setting = setting;
    }

    public TrainingDummySetting Setting { get; set; }

    public InputButtons Advance(int tick, CombatActorState state, bool grounded)
    {
        var inHitReaction = state
            is CombatActorState.Hitstun
            or CombatActorState.Blockstun
            or CombatActorState.GuardBreak
            or CombatActorState.Knockdown;
        if (inHitReaction)
        {
            _hasBeenHit = true;
        }

        var actionable = state
            is CombatActorState.Idle
            or CombatActorState.Walking
            or CombatActorState.Crouching
            or CombatActorState.Blocking
            or CombatActorState.Jumping;
        var recoveredFromHit = _wasInHitReaction && !inHitReaction;
        _wasInHitReaction = inHitReaction;

        var situation = new TrainingDummySituation(
            tick,
            state,
            grounded,
            _hasBeenHit,
            actionable,
            recoveredFromHit);
        return TrainingDummyController.Resolve(Setting, situation);
    }
}
