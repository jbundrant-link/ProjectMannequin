using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

public sealed class BossDuelSmokeScenario
{
    private readonly GameSimulation _simulation;
    private int _startedTick = -1;
    private bool _blockRulesPassed;
    private bool _parryPassed;
    private bool _guardBreakPassed;
    private bool _comboScalingPassed;
    private bool _sawCounter;
    private bool _sawPunishCounter;
    private bool _sawSuper;
    private bool _sawSuperPause;
    private bool _summaryPrinted;

    public BossDuelSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public void UpdateBeforeSimulation(int tick)
    {
        var player = GetPlayer();
        var boss = GetBoss();
        if (player is null || boss is null)
        {
            return;
        }

        if (_startedTick < 0)
        {
            _startedTick = tick;
            player.IsVulnerable = false;
            boss.IsAiEnabled = false;
            PrepareActors(player, boss);
            GD.Print("[BossDuelSmoke] Deterministic duel scenario initialized.");
        }

        var elapsed = tick - _startedTick;
        switch (elapsed)
        {
            case 1:
                ProbeBlockRules(player, boss, tick);
                ProbeComboScaling(player, boss, tick);
                break;
            case 12:
                ProbeParry(player, boss, tick);
                break;
            case 32:
                ProbeGuardBreak(player, boss, tick);
                break;
            case 56:
                StartContextProbe(player, boss, tick, punish: false);
                break;
            case 82:
                StartContextProbe(player, boss, tick, punish: true);
                break;
            case 110:
                StartBossSuper(player, boss, tick);
                break;
        }

        _sawSuperPause |= _simulation.SuperPauseFramesRemaining > 0;
    }

    public void CaptureAfterSimulation(int tick, IReadOnlyCollection<CombatPresentationEvent> events)
    {
        _sawCounter |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.CounterHit);
        _sawPunishCounter |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.PunishCounter);
        _sawSuper |= events.Any(presentationEvent =>
            presentationEvent.Type == CombatPresentationEventType.SuperStarted
            && presentationEvent.Payload == "Archive Cataclysm");
        _sawSuperPause |= _simulation.SuperPauseFramesRemaining > 0;

        if (_startedTick < 0 || tick - _startedTick < 155 || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var passed = _blockRulesPassed
            && _parryPassed
            && _guardBreakPassed
            && _comboScalingPassed
            && _sawCounter
            && _sawPunishCounter
            && _sawSuper
            && _sawSuperPause;
        GD.Print(
            $"[BossDuelSmoke] SUMMARY passed={passed} block_mix={_blockRulesPassed} parry={_parryPassed} " +
            $"guard_break={_guardBreakPassed} scaling={_comboScalingPassed} counter={_sawCounter} " +
            $"punish={_sawPunishCounter} super={_sawSuper} super_pause={_sawSuperPause}");
        if (!passed)
        {
            GD.PushError("[BossDuelSmoke] One or more boss-duel mechanics were not observed.");
        }
    }

    private void ProbeBlockRules(CombatActor player, CombatActor boss, int tick)
    {
        PrepareActors(player, boss);
        var low = ProbeMove("probe_low", AttackHeight.Low);
        var overhead = ProbeMove("probe_overhead", AttackHeight.Overhead);

        boss.StateMachine.EnterBlocking(20, tick, crouching: false);
        var standingBlocksOverhead = boss.CanBlockAttack(player, overhead);
        var standingMissesLow = !boss.CanBlockAttack(player, low);

        boss.State = CombatActorState.Idle;
        boss.StateMachine.EnterBlocking(20, tick, crouching: true);
        var crouchingBlocksLow = boss.CanBlockAttack(player, low);
        var crouchingMissesOverhead = !boss.CanBlockAttack(player, overhead);
        _blockRulesPassed = standingBlocksOverhead
            && standingMissesLow
            && crouchingBlocksLow
            && crouchingMissesOverhead;
        boss.State = CombatActorState.Idle;
    }

    private void ProbeComboScaling(CombatActor player, CombatActor boss, int tick)
    {
        var move = ProbeMove("probe_scaling", AttackHeight.Mid);
        var firstScale = player.GetNextComboDamageScale(boss, tick, false, move);
        player.NotifySuccessfulHit(boss, tick, false);
        var secondScale = player.GetNextComboDamageScale(boss, tick + 1, true, move);
        _comboScalingPassed = Mathf.IsEqualApprox(firstScale, 1.0f)
            && secondScale < firstScale
            && secondScale >= move.MinimumDamageScale;
    }

    private void ProbeParry(CombatActor player, CombatActor boss, int tick)
    {
        PrepareActors(player, boss);
        var move = ProbeMove("probe_parry", AttackHeight.Mid);
        var started = player.StateMachine.TryBeginParry(tick);
        var resolved = player.StateMachine.ResolveParry(boss, move, tick);
        _parryPassed = started
            && resolved
            && boss.State == CombatActorState.Hitstun
            && player.Meter >= GameConstants.ParryMeterReward;
    }

    private void ProbeGuardBreak(CombatActor player, CombatActor boss, int tick)
    {
        PrepareActors(player, boss);
        boss.RestoreGuardGaugeToFull();
        boss.StateMachine.EnterBlocking(30, tick);
        var move = ProbeMove("probe_guard_break", AttackHeight.Mid);
        move.GuardDamage = Mathf.Max(1, boss.CurrentForm.MaxGuardGauge);
        var hit = new HitApplication(
            player,
            boss,
            move,
            10,
            10,
            8,
            new Vector3(1.0f, 0.0f, 0.0f),
            false);
        _guardBreakPassed = boss.ApplyBlockedHit(hit, tick)
            && boss.State == CombatActorState.GuardBreak
            && boss.GuardGauge == 0;
    }

    private static void StartContextProbe(
        CombatActor player,
        CombatActor boss,
        int tick,
        bool punish)
    {
        PrepareActors(player, boss);
        var targetMove = ProbeMove(
            punish ? "probe_recovery_target" : "probe_startup_target",
            AttackHeight.Mid);
        targetMove.StartupFrames = 12;
        targetMove.ActiveFrames = 1;
        targetMove.RecoveryFrames = 30;
        targetMove.CombatBoxes.Clear();
        boss.TryStartMove(targetMove, tick);
        if (punish)
        {
            boss.CurrentMoveFrame = targetMove.LastActiveFrame + 2;
        }

        var strike = ProbeMove(
            punish ? "probe_punish_strike" : "probe_counter_strike",
            AttackHeight.Mid);
        strike.StartupFrames = 0;
        strike.ActiveFrames = 4;
        strike.RecoveryFrames = 8;
        strike.CombatBoxes[0].StartFrame = 0;
        strike.CombatBoxes[0].EndFrame = 3;
        player.TryStartMove(strike, tick);
    }

    private static void StartBossSuper(CombatActor player, CombatActor boss, int tick)
    {
        PrepareActors(player, boss);
        player.SimPosition = new Vector3(91.0f, 0.0f, 0.0f);
        boss.AddMeter(boss.CurrentForm.MaxMeter);
        var super = boss.CurrentForm.FindMove("boss_archive_cataclysm");
        if (super is not null)
        {
            boss.TryStartMove(super, tick);
        }
    }

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private CombatActor? GetBoss()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsBoss && !actor.IsDead);
    }

    private static void PrepareActors(CombatActor player, CombatActor boss)
    {
        player.EndCurrentMove();
        boss.EndCurrentMove();
        player.SimPosition = new Vector3(94.0f, 0.0f, 0.0f);
        boss.SimPosition = new Vector3(95.0f, 0.0f, 0.0f);
        player.Velocity = Vector3.Zero;
        boss.Velocity = Vector3.Zero;
        player.FacingRight = true;
        boss.FacingRight = false;
        player.State = CombatActorState.Idle;
        boss.State = CombatActorState.Idle;
        boss.IsVulnerable = true;
        boss.InvincibilityFrames = 0;
    }

    private static MoveData ProbeMove(string id, AttackHeight attackHeight)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = id,
            StartupFrames = 1,
            ActiveFrames = 3,
            RecoveryFrames = 8,
            Damage = 10,
            HitstunFrames = 12,
            BlockstunFrames = 8,
            HitStopFrames = 2,
            AttackHeight = attackHeight,
            CombatBoxes = new List<CombatBoxDefinition>
            {
                new()
                {
                    Id = $"{id}_hit",
                    BoxType = CombatBoxType.Hitbox,
                    StartFrame = 1,
                    EndFrame = 3,
                    OffsetX = 0.55f,
                    OffsetY = 1.0f,
                    SizeX = 1.2f,
                    SizeY = 1.4f,
                    SizeZ = 1.2f,
                },
            },
        };
    }

}
