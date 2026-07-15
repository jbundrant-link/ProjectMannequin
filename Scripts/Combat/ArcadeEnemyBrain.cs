using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public enum ArcadeEnemyMode
{
    EnterStage,
    Approach,
    Attack,
    Retreat,
    Recover,
}

public sealed class ArcadeEnemyBrain
{
    private static readonly float[] LaneSlots = { -0.5f, 0.5f, -1.0f, 1.0f, 0.0f };

    private readonly CombatActor _actor;
    private readonly ArcadeEnemyProfileData _profile;
    private ArcadeEnemyMode _mode = ArcadeEnemyMode.Approach;
    private int _nextAttackTick = -1;
    private int _retreatUntilTick;
    private float _attackSide;
    private int _moveCursor;
    private ArcadeCrowdCoordinator? _crowdCoordinator;
    private EnemyEntryProfile _entryProfile;
    private Vector3 _entryTarget;
    private bool _isEnteringStage;

    public ArcadeEnemyBrain(CombatActor actor, ArcadeEnemyProfileData profile)
    {
        _actor = actor;
        _profile = profile;
    }

    public ArcadeEnemyMode Mode => _mode;
    public CpuFighterIntent CurrentIntent { get; private set; } = CpuFighterIntent.Observe;
    public string LastReason { get; private set; } = "entering stage";
    public string TargetActorId { get; private set; } = "";
    public int FormationSlotIndex { get; private set; }
    public bool IsEnteringStage => _isEnteringStage;

    public void SetCrowdCoordinator(ArcadeCrowdCoordinator coordinator)
    {
        _crowdCoordinator = coordinator;
    }

    public void ConfigureStageEntry(
        EnemyEntryProfile entryProfile,
        Vector3 entryTarget,
        int tick)
    {
        _entryProfile = entryProfile;
        _entryTarget = entryTarget;
        _isEnteringStage = entryProfile != EnemyEntryProfile.Ambush;
        if (_isEnteringStage)
        {
            _mode = ArcadeEnemyMode.EnterStage;
            return;
        }

        _actor.QueueEvent(
            Core.CombatPresentationEventType.EnemyEntryCompleted,
            tick,
            entryProfile.ToString());
    }

    public CpuFighterDecision Evaluate(int tick, IReadOnlyList<CombatActor> actors)
    {
        if (_isEnteringStage)
        {
            var toEntry = _entryTarget - _actor.SimPosition;
            toEntry.Y = 0.0f;
            if (toEntry.LengthSquared() <= 0.12f)
            {
                _isEnteringStage = false;
                _mode = ArcadeEnemyMode.Approach;
                _actor.QueueEvent(
                    Core.CombatPresentationEventType.EnemyEntryCompleted,
                    tick,
                    _entryProfile.ToString());
            }
            else
            {
                return Remember(new CpuFighterDecision(
                    CpuFighterIntent.Approach,
                    toEntry.Normalized() * _profile.ApproachSpeedMultiplier,
                    null,
                    0,
                    false,
                    $"entering via {_entryProfile}"));
            }
        }

        var target = SelectTarget(actors);
        if (target is null)
        {
            return Remember(CpuFighterDecision.Observe("no live player target"));
        }

        TargetActorId = target.ActorId;
        FormationSlotIndex = _crowdCoordinator?.GetFormationSlot(_actor, target)
            ?? _actor.AiSlotIndex;
        var signedDistanceX = target.SimPosition.X - _actor.SimPosition.X;
        var distanceX = Mathf.Abs(signedDistanceX);
        var signedDistanceZ = target.SimPosition.Z - _actor.SimPosition.Z;
        _actor.FacingRight = signedDistanceX >= 0.0f;

        if (Mathf.IsZeroApprox(_attackSide))
        {
            _attackSide = _actor.SimPosition.X >= target.SimPosition.X ? 1.0f : -1.0f;
            if (Mathf.IsZeroApprox(signedDistanceX))
            {
                _attackSide = FormationSlotIndex % 2 == 0 ? 1.0f : -1.0f;
            }
        }

        if (_nextAttackTick < 0)
        {
            _nextAttackTick = tick + Mathf.Max(0, _actor.AiInitialDelayFrames);
        }

        if (_mode == ArcadeEnemyMode.Retreat)
        {
            if (tick >= _retreatUntilTick || distanceX >= _profile.RetreatDistance)
            {
                _mode = ArcadeEnemyMode.Recover;
                _nextAttackTick = Mathf.Max(_nextAttackTick, tick + _profile.ReengageDelayFrames);
            }
            else
            {
                var awayX = -Mathf.Sign(Mathf.IsZeroApprox(signedDistanceX) ? _attackSide : signedDistanceX);
                var resetLane = GetSlotLane(target);
                var laneDirection = Mathf.Clamp(resetLane - _actor.SimPosition.Z, -1.0f, 1.0f);
                return Remember(new CpuFighterDecision(
                    CpuFighterIntent.Retreat,
                    new Vector3(awayX, 0.0f, laneDirection * 0.35f).Normalized()
                        * _profile.RetreatSpeedMultiplier,
                    null,
                    Mathf.Max(0, _retreatUntilTick - tick),
                    false,
                    "resetting after attack"));
            }
        }

        var desiredPosition = new Vector3(
            target.SimPosition.X + _attackSide * _profile.AttackRange,
            0.0f,
            GetSlotLane(target));
        var toSlot = desiredPosition - _actor.SimPosition;
        var slotDistanceX = Mathf.Abs(toSlot.X);
        var slotDistanceZ = Mathf.Abs(toSlot.Z);

        if (_mode == ArcadeEnemyMode.Recover && tick < _nextAttackTick)
        {
            if (distanceX < _profile.AttackRange * 0.72f)
            {
                var awayX = -Mathf.Sign(Mathf.IsZeroApprox(signedDistanceX) ? _attackSide : signedDistanceX);
                return Remember(new CpuFighterDecision(
                    CpuFighterIntent.Retreat,
                    new Vector3(awayX, 0.0f, Mathf.Sign(toSlot.Z) * 0.3f).Normalized()
                        * _profile.RetreatSpeedMultiplier,
                    null,
                    _nextAttackTick - tick,
                    false,
                    "holding reset distance"));
            }

            if (slotDistanceZ > _profile.LaneTolerance)
            {
                return Remember(MoveTowardSlot(toSlot, CpuFighterIntent.AlignLane, "circling into an open lane"));
            }

            return Remember(CpuFighterDecision.Observe("waiting before re-engaging"));
        }

        _mode = ArcadeEnemyMode.Approach;
        if (slotDistanceZ > _profile.LaneTolerance)
        {
            return Remember(MoveTowardSlot(toSlot, CpuFighterIntent.AlignLane, "aligning attack lane"));
        }

        if (slotDistanceX > _profile.PositionTolerance)
        {
            return Remember(MoveTowardSlot(toSlot, CpuFighterIntent.Approach, "moving into attack slot"));
        }

        if (tick < _nextAttackTick)
        {
            return Remember(CpuFighterDecision.Observe("attack stagger still active"));
        }

        var move = SelectMove();
        if (move is null)
        {
            return Remember(CpuFighterDecision.Observe("no legal ground attack"));
        }

        if (_crowdCoordinator?.TryReserveAttack(_actor, target) == false)
        {
            _nextAttackTick = tick + 6;
            return Remember(CpuFighterDecision.Observe("waiting for shared attack opening"));
        }

        _mode = ArcadeEnemyMode.Attack;
        return Remember(new CpuFighterDecision(
            CpuFighterIntent.Attack,
            Vector3.Zero,
            move,
            0,
            false,
            $"committing from slot {FormationSlotIndex}: {move.Id}"));
    }

    public void NotifyMoveStarted(MoveData move, int tick)
    {
        _mode = ArcadeEnemyMode.Attack;
    }

    public void NotifyMoveEnded(int tick)
    {
        _crowdCoordinator?.ReleaseAttack(_actor);
        _mode = ArcadeEnemyMode.Retreat;
        _retreatUntilTick = tick + Mathf.Max(1, _profile.RetreatFrames);
        _nextAttackTick = tick + Mathf.Max(1, _profile.ReengageDelayFrames);
    }

    public void NotifyInterrupted(int tick)
    {
        _crowdCoordinator?.ReleaseAttack(_actor);
        _mode = ArcadeEnemyMode.Recover;
        _nextAttackTick = tick + Mathf.Max(8, _profile.ReengageDelayFrames);
    }

    private CpuFighterDecision MoveTowardSlot(
        Vector3 toSlot,
        CpuFighterIntent intent,
        string reason)
    {
        var direction = new Vector3(toSlot.X, 0.0f, toSlot.Z).Normalized()
            * _profile.ApproachSpeedMultiplier;
        return new CpuFighterDecision(intent, direction, null, 0, false, reason);
    }

    private float GetSlotLane(CombatActor target)
    {
        var slot = LaneSlots[Math.Abs(FormationSlotIndex) % LaneSlots.Length];
        return target.SimPosition.Z + slot * _profile.SlotLaneSpacing;
    }

    private MoveData? SelectMove()
    {
        var moves = _actor.CurrentForm.Moves
            .Where(move => move.AllowGround && _actor.Meter >= move.MeterCost)
            .ToArray();
        if (moves.Length == 0)
        {
            return null;
        }

        return moves[_moveCursor++ % moves.Length];
    }

    private CombatActor? SelectTarget(IReadOnlyList<CombatActor> actors)
    {
        if (_crowdCoordinator is not null)
        {
            return _crowdCoordinator.SelectTarget(_actor, actors);
        }

        return actors
            .Where(actor => actor.TeamId != _actor.TeamId && !actor.IsDead &&
                            (!_actor.LockedTargetPlayerId.HasValue || actor.PlayerId == _actor.LockedTargetPlayerId.Value))
            .OrderBy(actor => actor.SimPosition.DistanceSquaredTo(_actor.SimPosition))
            .ThenBy(actor => actor.ActorId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private CpuFighterDecision Remember(CpuFighterDecision decision)
    {
        CurrentIntent = decision.Intent;
        LastReason = decision.Reason;
        return decision;
    }
}
