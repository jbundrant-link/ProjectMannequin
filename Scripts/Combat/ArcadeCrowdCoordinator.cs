using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectMannequin.Combat;

public sealed class ArcadeCrowdCoordinator
{
    private readonly Dictionary<string, string> _targetAssignments = new();
    private readonly Dictionary<string, int> _formationSlots = new();
    private readonly Dictionary<string, string> _attackReservations = new();

    private int _maxSimultaneousAttackers = 2;
    private int _maxAttackersPerTarget = 1;

    public int ActiveAttackReservationCount => _attackReservations.Count;

    public void Configure(int maxSimultaneousAttackers, int maxAttackersPerTarget)
    {
        _maxSimultaneousAttackers = Math.Max(1, maxSimultaneousAttackers);
        _maxAttackersPerTarget = Math.Max(1, maxAttackersPerTarget);
        _targetAssignments.Clear();
        _formationSlots.Clear();
        _attackReservations.Clear();
    }

    public CombatActor? SelectTarget(
        CombatActor actor,
        IReadOnlyList<CombatActor> actors)
    {
        Cleanup(actors);
        var candidates = actors
            .Where(candidate =>
                candidate.TeamId != actor.TeamId
                && candidate.IsPlayerControlled
                && !candidate.IsDead
                && (!actor.LockedTargetPlayerId.HasValue
                    || candidate.PlayerId == actor.LockedTargetPlayerId.Value))
            .ToArray();
        if (candidates.Length == 0)
        {
            return null;
        }

        if (_targetAssignments.TryGetValue(actor.ActorId, out var assignedTargetId))
        {
            var assignedTarget = candidates.FirstOrDefault(candidate =>
                candidate.ActorId == assignedTargetId);
            if (assignedTarget is not null)
            {
                return assignedTarget;
            }
        }

        var target = candidates
            .OrderBy(candidate => _targetAssignments.Values.Count(
                targetId => targetId == candidate.ActorId))
            .ThenBy(candidate => candidate.SimPosition.DistanceSquaredTo(actor.SimPosition))
            .ThenBy(candidate => candidate.ActorId, StringComparer.Ordinal)
            .First();
        _targetAssignments[actor.ActorId] = target.ActorId;
        AssignFormationSlot(actor.ActorId, target.ActorId);
        return target;
    }

    public int GetFormationSlot(CombatActor actor, CombatActor target)
    {
        if (!_targetAssignments.TryGetValue(actor.ActorId, out var targetId)
            || targetId != target.ActorId)
        {
            _targetAssignments[actor.ActorId] = target.ActorId;
            _formationSlots.Remove(actor.ActorId);
        }

        return AssignFormationSlot(actor.ActorId, target.ActorId);
    }

    public bool TryReserveAttack(CombatActor actor, CombatActor target)
    {
        if (_attackReservations.TryGetValue(actor.ActorId, out var reservedTargetId))
        {
            return reservedTargetId == target.ActorId;
        }

        if (_attackReservations.Count >= _maxSimultaneousAttackers
            || _attackReservations.Values.Count(
                targetId => targetId == target.ActorId) >= _maxAttackersPerTarget)
        {
            return false;
        }

        _attackReservations[actor.ActorId] = target.ActorId;
        return true;
    }

    public void ReleaseAttack(CombatActor actor)
    {
        _attackReservations.Remove(actor.ActorId);
    }

    public void ReleaseActor(CombatActor actor)
    {
        _attackReservations.Remove(actor.ActorId);
        _targetAssignments.Remove(actor.ActorId);
        _formationSlots.Remove(actor.ActorId);
    }

    private int AssignFormationSlot(string actorId, string targetId)
    {
        if (_formationSlots.TryGetValue(actorId, out var existingSlot))
        {
            return existingSlot;
        }

        var usedSlots = _formationSlots
            .Where(pair => _targetAssignments.TryGetValue(pair.Key, out var assignedTarget)
                && assignedTarget == targetId)
            .Select(pair => pair.Value)
            .ToHashSet();
        var slot = 0;
        while (usedSlots.Contains(slot))
        {
            slot++;
        }

        _formationSlots[actorId] = slot;
        return slot;
    }

    private void Cleanup(IReadOnlyList<CombatActor> actors)
    {
        var liveActorIds = actors
            .Where(actor => !actor.IsDead)
            .Select(actor => actor.ActorId)
            .ToHashSet();
        foreach (var actorId in _targetAssignments.Keys
                     .Where(actorId => !liveActorIds.Contains(actorId))
                     .ToArray())
        {
            _targetAssignments.Remove(actorId);
            _formationSlots.Remove(actorId);
            _attackReservations.Remove(actorId);
        }

        foreach (var actorId in _attackReservations
                     .Where(pair =>
                         !liveActorIds.Contains(pair.Key)
                         || !liveActorIds.Contains(pair.Value))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            _attackReservations.Remove(actorId);
        }
    }
}
