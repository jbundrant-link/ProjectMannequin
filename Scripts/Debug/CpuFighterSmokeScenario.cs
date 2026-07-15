using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

public sealed class CpuFighterSmokeScenario
{
    private readonly GameSimulation _simulation;
    private bool _initialized;
    private bool _sawBlock;
    private bool _sawAntiAir;
    private bool _sawPunish;
    private bool _sawApproach;
    private bool _summaryPrinted;
    private CpuFighterIntent? _lastIntent;
    private int _encounterActiveStartTick = -1;

    public CpuFighterSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public void UpdateBeforeSimulation(int tick, bool encounterActive)
    {
        if (!encounterActive)
        {
            return;
        }

        var player = GetPlayer();
        var boss = GetBoss();
        if (player is null || boss?.CpuBrain is null)
        {
            return;
        }

        if (!_initialized)
        {
            _initialized = true;
            _encounterActiveStartTick = tick;
            Initialize(player, boss);
        }

        // Probe timing is relative to the encounter becoming active so the boss
        // intro (director BossIntro state) does not skip the early probes.
        switch (tick - _encounterActiveStartTick)
        {
            case 20:
                PrepareActors(player, boss, 1.55f);
                // Hold neutral choices until the probe has crossed the CPU's reaction window.
                boss.CpuBrain.ResetDecisionState(tick + 30);
                player.TryStartMove(CreateProbeMove(
                    "cpu_smoke_guard_probe",
                    startupFrames: 24,
                    activeFrames: 5,
                    recoveryFrames: 20,
                    reach: 1.45f), tick);
                GD.Print("[CpuSmoke] Guard probe started.");
                break;
            case 100:
                PrepareActors(player, boss, 1.55f);
                boss.CpuBrain.ResetDecisionState(tick);
                player.SimPosition = new Vector3(player.SimPosition.X, 1.2f, player.SimPosition.Z);
                player.Velocity = new Vector3(0.0f, 5.0f, 0.0f);
                player.State = CombatActorState.Jumping;
                GD.Print("[CpuSmoke] Anti-air probe started.");
                break;
            case 220:
                PrepareActors(player, boss, 1.70f);
                boss.CpuBrain.ResetDecisionState(tick);
                boss.StateMachine.EnterBlocking(10, tick);
                player.TryStartMove(CreateProbeMove(
                    "cpu_smoke_whiff_probe",
                    startupFrames: 3,
                    activeFrames: 1,
                    recoveryFrames: 32,
                    reach: 0.15f), tick);
                GD.Print("[CpuSmoke] Punish probe started.");
                break;
            case 320:
                PrepareActors(player, boss, 5.0f);
                boss.CpuBrain.ResetDecisionState(tick);
                GD.Print("[CpuSmoke] Spacing probe started.");
                break;
        }
    }

    public void CaptureAfterSimulation(int tick, ICollection<CombatPresentationEvent> events)
    {
        if (events.Any(presentationEvent => presentationEvent.Type == CombatPresentationEventType.Blocked))
        {
            _sawBlock = true;
        }

        var brain = GetBoss()?.CpuBrain;
        if (brain is not null && _lastIntent != brain.CurrentIntent)
        {
            _lastIntent = brain.CurrentIntent;
            GD.Print($"[CpuSmoke] tick {tick}: {brain.CurrentIntent} - {brain.LastReason}");
        }

        _sawAntiAir |= brain?.CurrentIntent == CpuFighterIntent.AntiAir;
        _sawPunish |= brain?.CurrentIntent == CpuFighterIntent.Punish;
        _sawApproach |= brain?.CurrentIntent == CpuFighterIntent.Approach;

        if (_encounterActiveStartTick < 0 || tick - _encounterActiveStartTick < 420 || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        GD.Print(
            $"[CpuSmoke] SUMMARY block={_sawBlock} anti_air={_sawAntiAir} punish={_sawPunish} approach={_sawApproach}");
        if (!_sawBlock || !_sawAntiAir || !_sawPunish || !_sawApproach)
        {
            GD.PushError("[CpuSmoke] One or more required CPU behaviors were not observed.");
        }
    }

    private static void Initialize(CombatActor player, CombatActor boss)
    {
        player.IsVulnerable = false;
        if (boss.CurrentForm.CpuProfile is not null)
        {
            boss.CurrentForm.CpuProfile.GuardChance = 1.0f;
            boss.CurrentForm.CpuProfile.AntiAirChance = 1.0f;
            boss.CurrentForm.CpuProfile.PunishChance = 1.0f;
            boss.CurrentForm.CpuProfile.MistakeChance = 0.0f;
        }

        if (boss.CurrentBossPhase is not null)
        {
            boss.CurrentBossPhase.DefenseMultiplier = 2.0f;
        }

        GD.Print("[CpuSmoke] Story CPU scenario initialized.");
    }

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private CombatActor? GetBoss()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsBoss && !actor.IsDead);
    }

    private static void PrepareActors(CombatActor player, CombatActor boss, float distance)
    {
        player.EndCurrentMove();
        boss.EndCurrentMove();
        player.SimPosition = new Vector3(94.0f, 0.0f, 0.0f);
        boss.SimPosition = new Vector3(player.SimPosition.X + distance, 0.0f, 0.0f);
        player.Velocity = Vector3.Zero;
        boss.Velocity = Vector3.Zero;
        player.FacingRight = true;
        boss.FacingRight = false;
    }

    private static MoveData CreateProbeMove(
        string id,
        int startupFrames,
        int activeFrames,
        int recoveryFrames,
        float reach)
    {
        return new MoveData
        {
            Id = id,
            DisplayName = id,
            StartupFrames = startupFrames,
            ActiveFrames = activeFrames,
            RecoveryFrames = recoveryFrames,
            Damage = 5,
            HitstunFrames = 10,
            BlockstunFrames = 12,
            HitStopFrames = 3,
            CombatBoxes = new List<CombatBoxDefinition>
            {
                new()
                {
                    Id = $"{id}_hitbox",
                    BoxType = CombatBoxType.Hitbox,
                    StartFrame = startupFrames,
                    EndFrame = startupFrames + activeFrames - 1,
                    OffsetX = reach * 0.5f,
                    OffsetY = 1.1f,
                    SizeX = reach,
                    SizeY = 1.0f,
                    SizeZ = 1.2f,
                },
            },
        };
    }
}
