using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public enum CpuFighterIntent
{
    Observe,
    Approach,
    AlignLane,
    Retreat,
    Guard,
    Parry,
    JumpEvade,
    Attack,
    AntiAir,
    Punish,
}

public readonly record struct CpuFighterDecision(
    CpuFighterIntent Intent,
    Vector3 MovementDirection,
    MoveData? Move,
    int DurationFrames,
    bool UseDash,
    string Reason)
{
    public static CpuFighterDecision Observe(string reason)
    {
        return new CpuFighterDecision(
            CpuFighterIntent.Observe,
            Vector3.Zero,
            null,
            0,
            false,
            reason);
    }
}

public sealed class CpuFighterBrain
{
    private readonly CombatActor _actor;
    private readonly CpuFighterProfileData _profile;
    private uint _randomState;
    private int _nextDecisionTick;
    private int _movementCommitmentUntilTick;
    private Vector3 _committedMovement;
    private CpuFighterIntent _committedMovementIntent;
    private string _observedThreatKey = "";
    private int _threatSeenTick = -1;
    private bool _threatResponseCommitted;
    private int _airborneSeenTick = -1;
    private bool _airResponseCommitted;
    private string _punishEvaluatedThreatKey = "";
    private string _lastMoveId = "";
    private int _targetBlockSinceTick = -1;
    private bool _wasIncapacitated;
    private int _dragonRushFollowupUntilTick;

    public CpuFighterBrain(CombatActor actor, CpuFighterProfileData profile)
    {
        _actor = actor;
        _profile = profile;
        _randomState = BuildSeed(actor.ActorId, profile.Id);
    }

    public CpuFighterIntent CurrentIntent { get; private set; } = CpuFighterIntent.Observe;
    public string LastReason { get; private set; } = "initializing";
    public string TargetActorId { get; private set; } = "";
    public int ReactionFramesRemaining { get; private set; }
    public int DecisionFramesRemaining { get; private set; }

    public CpuFighterDecision Evaluate(int tick, IReadOnlyList<CombatActor> actors)
    {
        var target = SelectTarget(actors);
        if (target is null)
        {
            TargetActorId = "";
            return Remember(CpuFighterDecision.Observe("no valid opponent"), tick);
        }

        TargetActorId = target.ActorId;
        _actor.FacingRight = target.SimPosition.X >= _actor.SimPosition.X;

        var incapacitatedNow = _actor.State is CombatActorState.Knockdown or CombatActorState.Hitstun;
        var justRecovered = _wasIncapacitated && !incapacitatedNow;
        _wasIncapacitated = incapacitatedNow;

        if (target.State is CombatActorState.Blocking or CombatActorState.Blockstun)
        {
            if (_targetBlockSinceTick < 0)
            {
                _targetBlockSinceTick = tick;
            }
        }
        else
        {
            _targetBlockSinceTick = -1;
        }

        var signedDistanceX = target.SimPosition.X - _actor.SimPosition.X;
        var distanceX = Mathf.Abs(signedDistanceX);
        var signedDistanceZ = target.SimPosition.Z - _actor.SimPosition.Z;
        var distanceZ = Mathf.Abs(signedDistanceZ);
        var towardX = Mathf.Sign(Mathf.IsZeroApprox(signedDistanceX) ? 1.0f : signedDistanceX);
        var towardZ = Mathf.Sign(Mathf.IsZeroApprox(signedDistanceZ) ? 1.0f : signedDistanceZ);
        var phase = _actor.CurrentBossPhase;
        var reactionFrames = Mathf.Max(3, _profile.ReactionFrames + (phase?.ReactionFrameModifier ?? 0));
        var aggression = Mathf.Clamp(
            _profile.Aggression * (phase?.AggressionMultiplier ?? 1.0f),
            0.0f,
            1.0f);
        var defenseChance = Mathf.Clamp(
            _profile.GuardChance * (phase?.DefenseMultiplier ?? 1.0f),
            0.0f,
            1.0f);

        // F3: Z-Reflect on wake-up / hitstun-escape to punish mindless mashing.
        if (justRecovered
            && _actor.SimPosition.Y <= 0.001f
            && NextFloat() < _profile.WakeupParryChance)
        {
            return Remember(new CpuFighterDecision(
                CpuFighterIntent.Parry,
                Vector3.Zero,
                null,
                GameConstants.ParryActiveFrames + GameConstants.ParryRecoveryFrames,
                false,
                "wake-up Z-Reflect vs mash"), tick);
        }

        // F4: after a Dragon Rush launch, chase the airborne opponent.
        if (tick < _dragonRushFollowupUntilTick
            && target.SimPosition.Y > 0.001f
            && _actor.SimPosition.Y <= 0.001f
            && _actor.State is CombatActorState.Idle or CombatActorState.Walking)
        {
            var followUp = SelectMove(distanceX, distanceZ, "launcher")
                ?? SelectMove(distanceX, distanceZ, "anti_air");
            _dragonRushFollowupUntilTick = 0;
            return Remember(
                followUp is not null
                    ? AttackDecision(followUp, CpuFighterIntent.Attack, "[DragonRush] juggle follow-up")
                    : new CpuFighterDecision(
                        CpuFighterIntent.JumpEvade,
                        new Vector3(towardX, 1.0f, 0.0f),
                        null,
                        12,
                        false,
                        "[DragonRush] aerial pursuit"),
                tick);
        }

        var threatDecision = EvaluateIncomingAttack(
            target,
            tick,
            reactionFrames,
            defenseChance,
            distanceX,
            distanceZ,
            towardX);
        if (threatDecision is not null)
        {
            return Remember(threatDecision.Value, tick);
        }

        if (AiModules.IsModuleEnabled(AiModules.ModuleAntiAir, phase?.DisabledAiModules))
        {
            var antiAirDecision = EvaluateAntiAir(
                target,
                tick,
                reactionFrames,
                distanceX,
                distanceZ);
            if (antiAirDecision is not null)
            {
                return Remember(antiAirDecision.Value, tick);
            }
        }

        var punishDecision = EvaluatePunish(target, tick, distanceX, distanceZ);
        if (punishDecision is not null)
        {
            return Remember(punishDecision.Value, tick);
        }

        if (_movementCommitmentUntilTick > tick)
        {
            return Remember(new CpuFighterDecision(
                _committedMovementIntent,
                _committedMovement,
                null,
                _movementCommitmentUntilTick - tick,
                false,
                "committed defensive movement"), tick);
        }

        if (distanceZ > _profile.LaneTolerance)
        {
            var laneDirection = new Vector3(
                distanceX > _profile.PreferredRangeMax ? towardX * 0.25f : 0.0f,
                0.0f,
                towardZ).Normalized();
            return Remember(new CpuFighterDecision(
                CpuFighterIntent.AlignLane,
                laneDirection,
                null,
                0,
                false,
                $"lane gap {distanceZ:0.00}"), tick);
        }

        DecisionFramesRemaining = Mathf.Max(0, _nextDecisionTick - tick);
        if (tick < _nextDecisionTick)
        {
            if (distanceX > _profile.PreferredRangeMax)
            {
                return Remember(Approach(towardX, distanceX), tick);
            }

            return Remember(CpuFighterDecision.Observe("waiting for next decision window"), tick);
        }

        _nextDecisionTick = tick + Mathf.Max(4, _profile.DecisionIntervalFrames);
        if (NextFloat() < _profile.MistakeChance)
        {
            return Remember(CpuFighterDecision.Observe("deliberate difficulty mistake"), tick);
        }

        if (distanceX > _profile.PreferredRangeMax)
        {
            var gapCloser = SelectMove(distanceX, distanceZ, "gap_closer");
            if (gapCloser is not null && NextFloat() < aggression)
            {
                return Remember(AttackDecision(gapCloser, CpuFighterIntent.Attack, "closing distance with an attack"), tick);
            }

            return Remember(Approach(towardX, distanceX), tick);
        }

        if (distanceX < _profile.PreferredRangeMin && NextFloat() < _profile.RetreatChance)
        {
            CommitMovement(
                tick,
                CpuFighterIntent.Retreat,
                new Vector3(-towardX, 0.0f, 0.0f),
                _profile.MovementCommitmentFrames);
            return Remember(new CpuFighterDecision(
                CpuFighterIntent.Retreat,
                _committedMovement,
                null,
                _profile.MovementCommitmentFrames,
                false,
                "reclaiming preferred spacing"), tick);
        }

        if (NextFloat() < aggression)
        {
            if (AiModules.IsModuleEnabled(AiModules.ModuleThrow, phase?.DisabledAiModules)
                && target.State is CombatActorState.Blocking or CombatActorState.Blockstun)
            {
                var throwMove = SelectMove(distanceX, distanceZ, "throw");
                var blockHeld = _targetBlockSinceTick >= 0 ? tick - _targetBlockSinceTick : 0;
                var rushThrowChance = AiModules.ResolveRushThrowChance(
                    _profile.RushThrowBaseChance,
                    blockHeld,
                    _profile.RushThrowMaxBonus,
                    _profile.RushThrowRampFrames);
                if (throwMove is not null && NextFloat() < rushThrowChance)
                {
                    if (throwMove.IsLauncher)
                    {
                        _dragonRushFollowupUntilTick = tick + 50;
                    }

                    _actor.QueueEvent(
                        CombatPresentationEventType.RushThrow,
                        tick,
                        $"{blockHeld}",
                        target.ActorId);
                    return Remember(AttackDecision(throwMove, CpuFighterIntent.Attack, $"[RushThrow] guard break after {blockHeld}f block"), tick);
                }
            }
            
            var move = SelectMove(distanceX, distanceZ);
            if (move is not null)
            {
                return Remember(AttackDecision(move, CpuFighterIntent.Attack, "weighted neutral attack"), tick);
            }
        }

        if (NextFloat() < defenseChance * 0.16f)
        {
            return Remember(new CpuFighterDecision(
                CpuFighterIntent.Guard,
                Vector3.Zero,
                null,
                _profile.GuardHoldFrames,
                false,
                "anticipatory neutral guard"), tick);
        }

        return Remember(CpuFighterDecision.Observe("holding neutral spacing"), tick);
    }

    public void NotifyMoveStarted(MoveData move, int tick)
    {
        _lastMoveId = move.Id;
        var phaseInterval = _actor.CurrentBossPhase?.AttackIntervalFrames ?? _profile.DecisionIntervalFrames;
        var recoveryPause = Mathf.Max(4, phaseInterval - move.TotalFrames);
        _nextDecisionTick = Mathf.Max(_nextDecisionTick, tick + move.TotalFrames + recoveryPause);
    }

    internal void ResetDecisionState(int tick)
    {
        _nextDecisionTick = tick;
        _movementCommitmentUntilTick = tick;
        _observedThreatKey = "";
        _threatSeenTick = -1;
        _threatResponseCommitted = false;
        _airborneSeenTick = -1;
        _airResponseCommitted = false;
        _punishEvaluatedThreatKey = "";
        ReactionFramesRemaining = 0;
        DecisionFramesRemaining = 0;
        _targetBlockSinceTick = -1;
    }

    private CpuFighterDecision? EvaluateIncomingAttack(
        CombatActor target,
        int tick,
        int reactionFrames,
        float defenseChance,
        float distanceX,
        float distanceZ,
        float towardX)
    {
        if (target.State != CombatActorState.Attacking || target.CurrentMove is null)
        {
            _observedThreatKey = "";
            _threatSeenTick = -1;
            _threatResponseCommitted = false;
            ReactionFramesRemaining = 0;
            return null;
        }

        var move = target.CurrentMove;
        var threatKey = $"{target.ActorId}:{target.CurrentMoveInstanceId}";
        if (_observedThreatKey != threatKey)
        {
            _observedThreatKey = threatKey;
            _threatSeenTick = tick;
            _threatResponseCommitted = false;
        }

        ReactionFramesRemaining = Mathf.Max(0, reactionFrames - (tick - _threatSeenTick));
        if (_threatResponseCommitted || ReactionFramesRemaining > 0)
        {
            return null;
        }

        var isBeforeOrActive = target.CurrentMoveFrame <= move.LastActiveFrame;
        var inThreatRange = distanceX <= EstimateReach(move) + 0.55f
            && distanceZ <= EstimateDepth(move) + 0.25f;
        if (!isBeforeOrActive || !inThreatRange)
        {
            return null;
        }

        _threatResponseCommitted = true;
        if (NextFloat() < defenseChance)
        {
            if (NextFloat() < 0.35f && _actor.SimPosition.Y <= 0.001f) // 35% chance to Z-Reflect instead of standard Guard
            {
                return new CpuFighterDecision(
                    CpuFighterIntent.Parry,
                    Vector3.Zero,
                    null,
                    GameConstants.ParryActiveFrames + GameConstants.ParryRecoveryFrames,
                    false,
                    $"reacted to {move.Id} with Z-Reflect");
            }
            
            return new CpuFighterDecision(
                CpuFighterIntent.Guard,
                Vector3.Zero,
                null,
                _profile.GuardHoldFrames,
                false,
                $"reacted to {move.Id} after {reactionFrames}f");
        }

        if (_actor.SimPosition.Y <= 0.001f && NextFloat() < _profile.JumpEvadeChance)
        {
            return new CpuFighterDecision(
                CpuFighterIntent.JumpEvade,
                new Vector3(-towardX, 0.0f, 0.0f),
                null,
                _profile.MovementCommitmentFrames,
                false,
                $"jump-evading {move.Id}");
        }

        CommitMovement(
            tick,
            CpuFighterIntent.Retreat,
            new Vector3(-towardX, 0.0f, 0.0f),
            _profile.MovementCommitmentFrames);
        return new CpuFighterDecision(
            CpuFighterIntent.Retreat,
            _committedMovement,
            null,
            _profile.MovementCommitmentFrames,
            false,
            $"failed guard check against {move.Id}; retreating");
    }

    private CpuFighterDecision? EvaluateAntiAir(
        CombatActor target,
        int tick,
        int reactionFrames,
        float distanceX,
        float distanceZ)
    {
        var targetAirborne = target.SimPosition.Y > 0.35f;
        if (!targetAirborne)
        {
            _airborneSeenTick = -1;
            _airResponseCommitted = false;
            return null;
        }

        if (_airborneSeenTick < 0)
        {
            _airborneSeenTick = tick;
        }

        if (_airResponseCommitted)
        {
            return CpuFighterDecision.Observe("tracking committed airborne response");
        }

        if (tick - _airborneSeenTick < reactionFrames)
        {
            return CpuFighterDecision.Observe("reading airborne trajectory");
        }

        if (tick < _nextDecisionTick)
        {
            return CpuFighterDecision.Observe("airborne threat inside decision cooldown");
        }

        _airResponseCommitted = true;
        if (NextFloat() >= _profile.AntiAirChance)
        {
            return CpuFighterDecision.Observe("anti-air opportunity intentionally missed");
        }

        var move = SelectMove(distanceX, distanceZ, "anti_air");
        return move is null
            ? CpuFighterDecision.Observe("no legal anti-air move in this phase")
            : AttackDecision(move, CpuFighterIntent.AntiAir, "opponent entered the air");
    }

    private CpuFighterDecision? EvaluatePunish(
        CombatActor target,
        int tick,
        float distanceX,
        float distanceZ)
    {
        if (tick < _nextDecisionTick
            || target.State != CombatActorState.Attacking
            || target.CurrentMove is null
            || target.CurrentMoveFrame <= target.CurrentMove.LastActiveFrame)
        {
            return null;
        }

        var recoveryFramesRemaining = target.CurrentMove.TotalFrames - target.CurrentMoveFrame;
        var threatKey = $"{target.ActorId}:{target.CurrentMoveInstanceId}";
        if (_punishEvaluatedThreatKey == threatKey || recoveryFramesRemaining <= 2)
        {
            return null;
        }

        _punishEvaluatedThreatKey = threatKey;
        if (NextFloat() >= _profile.PunishChance)
        {
            return null;
        }

        var move = GetAvailableMoves()
            .Where(candidate => candidate.StartupFrames + 2 <= recoveryFramesRemaining)
            .Where(candidate => CanReach(candidate, distanceX, distanceZ))
            .OrderBy(candidate => candidate.StartupFrames)
            .ThenByDescending(candidate => candidate.Damage)
            .FirstOrDefault();

        return move is null
            ? null
            : AttackDecision(move, CpuFighterIntent.Punish, $"{recoveryFramesRemaining}f punish window");
    }

    private CpuFighterDecision Approach(float towardX, float distanceX)
    {
        return new CpuFighterDecision(
            CpuFighterIntent.Approach,
            new Vector3(towardX, 0.0f, 0.0f),
            null,
            0,
            distanceX >= _profile.DashDistance,
            $"closing {distanceX:0.00} range");
    }

    private CpuFighterDecision AttackDecision(MoveData move, CpuFighterIntent intent, string reason)
    {
        return new CpuFighterDecision(
            intent,
            Vector3.Zero,
            move,
            0,
            false,
            $"{reason}: {move.Id}");
    }

    private MoveData? SelectMove(float distanceX, float distanceZ, string requiredTag = "")
    {
        var moves = GetAvailableMoves()
            .Where(move => string.IsNullOrWhiteSpace(requiredTag) || move.Tags.Contains(requiredTag))
            .Where(move => CanReach(move, distanceX, distanceZ))
            .ToArray();
        if (moves.Length == 0)
        {
            return null;
        }

        MoveData? bestMove = null;
        var bestScore = float.MinValue;
        foreach (var move in moves)
        {
            var reach = EstimateReach(move);
            var score = move.Damage * 0.24f
                - move.StartupFrames * 0.42f
                - move.RecoveryFrames * 0.08f
                - Mathf.Abs(reach - distanceX) * 1.8f
                + NextFloat() * 4.0f;

            if (move.Id == _lastMoveId)
            {
                score -= 7.0f;
            }

            if (move.Tags.Contains("gap_closer") && distanceX > _profile.PreferredRangeMax)
            {
                score += 12.0f;
            }

            if (score <= bestScore)
            {
                continue;
            }

            bestScore = score;
            bestMove = move;
        }

        return bestMove;
    }

    private IEnumerable<MoveData> GetAvailableMoves()
    {
        var enabledMoveIds = _actor.CurrentBossPhase?.EnabledMoveIds;
        var moves = enabledMoveIds is { Count: > 0 }
            ? enabledMoveIds
                .Select(_actor.CurrentForm.FindMove)
                .Where(move => move is not null)
                .Cast<MoveData>()
            : _actor.CurrentForm.Moves;

        var isAirborne = _actor.SimPosition.Y > 0.001f;
        return moves.Where(move =>
            move.CanStartFromAirState(isAirborne)
            && _actor.Meter >= move.MeterCost
            && _actor.StateMachine.CanStartMove(move));
    }

    private static bool CanReach(MoveData move, float distanceX, float distanceZ)
    {
        if (!move.CanStartAtRange(distanceX))
        {
            return false;
        }

        if (move.StartsFlight)
        {
            return distanceZ <= 1.3f;
        }

        return distanceX <= EstimateReach(move) + 0.35f
            && distanceZ <= EstimateDepth(move) + 0.2f;
    }

    private static float EstimateReach(MoveData move)
    {
        var boxReach = move.CombatBoxes
            .Where(box => box.BoxType is CombatBoxType.Hitbox or CombatBoxType.Grabbox or CombatBoxType.ProjectileBox)
            .Select(box => Mathf.Abs(box.OffsetX) + box.SizeX * 0.5f)
            .DefaultIfEmpty(0.8f)
            .Max();
        var travelFrames = Mathf.Max(0, move.ForwardVelocityEndFrame - move.ForwardVelocityStartFrame + 1);
        var travelReach = Mathf.Abs(move.ForwardVelocity) * travelFrames / 60.0f;
        var projectileReach = move.ProjectileSpawns
            .Select(projectile =>
                Mathf.Abs(projectile.OffsetX)
                + Mathf.Abs(projectile.VelocityX)
                * Mathf.Min(projectile.LifetimeFrames, 90)
                / GameConstants.TickRate)
            .DefaultIfEmpty(0.0f)
            .Max();
        return Mathf.Max(0.8f, Mathf.Max(boxReach + travelReach, projectileReach));
    }

    private static float EstimateDepth(MoveData move)
    {
        var boxDepth = move.CombatBoxes
            .Where(box => box.BoxType is CombatBoxType.Hitbox or CombatBoxType.Grabbox or CombatBoxType.ProjectileBox)
            .Select(box => Mathf.Abs(box.OffsetZ) + box.SizeZ * 0.5f)
            .DefaultIfEmpty(0.65f)
            .Max();
        var projectileDepth = move.ProjectileSpawns
            .Select(projectile => Mathf.Abs(projectile.OffsetZ) + projectile.SizeZ * 0.5f)
            .DefaultIfEmpty(0.65f)
            .Max();
        return Mathf.Max(boxDepth, projectileDepth);
    }

    private CombatActor? SelectTarget(IReadOnlyList<CombatActor> actors)
    {
        return actors
            .Where(actor => actor.TeamId != _actor.TeamId && !actor.IsDead &&
                            (!_actor.LockedTargetPlayerId.HasValue || actor.PlayerId == _actor.LockedTargetPlayerId.Value))
            .OrderBy(actor => actor.SimPosition.DistanceSquaredTo(_actor.SimPosition))
            .ThenBy(actor => actor.ActorId, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private void CommitMovement(
        int tick,
        CpuFighterIntent intent,
        Vector3 movement,
        int durationFrames)
    {
        _committedMovementIntent = intent;
        _committedMovement = movement.Normalized();
        _movementCommitmentUntilTick = tick + Mathf.Max(1, durationFrames);
    }

    private CpuFighterDecision Remember(CpuFighterDecision decision, int tick)
    {
        CurrentIntent = decision.Intent;
        LastReason = decision.Reason;
        DecisionFramesRemaining = Mathf.Max(0, _nextDecisionTick - tick);
        return decision;
    }

    private float NextFloat()
    {
        var value = _randomState;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        _randomState = value == 0 ? 0x9E3779B9u : value;
        return (_randomState & 0x00FFFFFFu) / 16777216.0f;
    }

    /// <summary>Captures the deterministic RNG state for rollback/replay tooling.</summary>
    public uint CaptureRandomState()
    {
        return _randomState;
    }

    /// <summary>Restores the deterministic RNG state from a snapshot.</summary>
    public void RestoreRandomState(uint state)
    {
        _randomState = state;
    }

    private static uint BuildSeed(string actorId, string profileId)
    {
        var hash = 2166136261u;
        foreach (var character in $"{actorId}|{profileId}")
        {
            hash ^= character;
            hash *= 16777619u;
        }

        return hash == 0 ? 0xA341316Cu : hash;
    }
}
