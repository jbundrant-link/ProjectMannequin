using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.DebugTools;

public sealed class CombatFeatureSmokeScenario
{
    private readonly GameSimulation _simulation;
    private bool _initialized;
    private bool _summaryPrinted;
    private bool _sawDash;
    private bool _sawAirLight;
    private bool _sawAirMedium;
    private bool _sawAirHeavy;
    private bool _sawCrouchAttack;
    private bool _sawGroundCombo;
    private bool _sawLauncherJumpCancel;
    private bool _sawLanding;
    private bool _sawUppercut;
    private bool _sawRyuJab;
    private bool _sawRyuStrong;
    private bool _sawRyuHadouken;
    private bool _sawRyuProjectile;
    private int _ryuRouteStep;
    private float _dashStartX;
    private float _maxJumpHeight;
    private int _launcherHitTick = -1;
    private int _impactCaptureRequestedTick = -1;
    private bool _impactCaptureSaved;

    public CombatFeatureSmokeScenario(GameSimulation simulation)
    {
        _simulation = simulation;
    }

    public void UpdateBeforeSimulation(int tick)
    {
        var player = GetPlayer();
        var dummy = GetDummy();
        if (player is null || dummy is null)
        {
            return;
        }

        if (!_initialized)
        {
            _initialized = true;
            player.IsVulnerable = false;
            dummy.IsAiEnabled = false;
            _dashStartX = player.SimPosition.X;
            GD.Print("[CombatSmoke] Mobility and combo scenario initialized.");
        }

        if (tick is 18 or 100 or 170 or 210 or 315 or 390)
        {
            ResetActors(player, dummy);
            if (tick == 390)
            {
                player.SetForm(TestRosterFactory.CreateWorldWarriorRyuForm(), resetHealth: true);
            }
        }

        if (tick is >= 18 and <= 82
            or >= 100 and <= 158
            or >= 170 and <= 198
            or >= 210 and <= 280
            or >= 315 and <= 360
            or >= 390 and <= 470)
        {
            var spacing = tick >= 390 ? 1.7f : 1.0f;
            dummy.SimPosition = new Vector3(player.SimPosition.X + spacing, 0.0f, player.SimPosition.Z);
            dummy.Velocity = Vector3.Zero;
        }
    }

    public void CaptureAfterSimulation(int tick, IReadOnlyCollection<CombatPresentationEvent> events)
    {
        var player = GetPlayer();
        if (player is null)
        {
            return;
        }

        _sawDash |= player.State == CombatActorState.Dashing
            && player.SimPosition.X > _dashStartX + 0.15f;
        _maxJumpHeight = Mathf.Max(_maxJumpHeight, player.SimPosition.Y);
        _sawAirLight |= player.CurrentMove?.Id == "mannequin_air_light";
        _sawAirMedium |= player.CurrentMove?.Id == "mannequin_air_medium";
        _sawAirHeavy |= player.CurrentMove?.Id == "mannequin_air_heavy";
        _sawCrouchAttack |= player.CurrentMove?.Posture == MovePosture.Crouching
            && player.CurrentMove.Id == "mannequin_crouch_light_kick";
        _sawUppercut |= player.CurrentMove?.Id == "mannequin_uppercut";
        _sawGroundCombo |= tick is >= 100 and <= 158 && player.ComboHitCount >= 3;
        if (player.CurrentMove?.Id == "ryu_jab")
        {
            _sawRyuJab = true;
            _ryuRouteStep = Mathf.Max(_ryuRouteStep, 1);
        }
        else if (player.CurrentMove?.Id == "ryu_strong" && _ryuRouteStep >= 1)
        {
            _sawRyuStrong = true;
            _ryuRouteStep = Mathf.Max(_ryuRouteStep, 2);
        }
        else if (player.CurrentMove?.Id == "ryu_hadouken_medium" && _ryuRouteStep >= 2)
        {
            _sawRyuHadouken = true;
            _ryuRouteStep = 3;
        }

        _sawRyuProjectile |= tick >= 390
            && (_simulation.Projectiles.Any(projectile => projectile.Owner == player)
                || events.Any(presentationEvent =>
                    presentationEvent.Type == CombatPresentationEventType.HitConnected
                    && presentationEvent.SourceActorId == player.ActorId
                    && presentationEvent.Payload.StartsWith("ryu_hadouken_medium|")));

        foreach (var presentationEvent in events)
        {
            if (presentationEvent.SourceActorId != player.ActorId)
            {
                continue;
            }

            if (presentationEvent.Type is CombatPresentationEventType.MoveStarted
                or CombatPresentationEventType.SuperStarted)
            {
                GD.Print(
                    $"[CombatSmoke] tick {tick}: {presentationEvent.Type} {presentationEvent.Payload}");
            }

            if (presentationEvent.Type == CombatPresentationEventType.LauncherHit)
            {
                _launcherHitTick = presentationEvent.Tick;
            }

            if (!_impactCaptureSaved
                && _impactCaptureRequestedTick < 0
                && tick >= 210
                && presentationEvent.Type is CombatPresentationEventType.HitConnected
                    or CombatPresentationEventType.LauncherHit
                && !string.IsNullOrWhiteSpace(
                    OS.GetEnvironment("PROJECT_MANNEQUIN_COMBAT_VFX_CAPTURE")))
            {
                _impactCaptureRequestedTick = tick;
            }

            if (presentationEvent.Type == CombatPresentationEventType.JumpStarted
                && _launcherHitTick >= 0
                && presentationEvent.Tick > _launcherHitTick)
            {
                _sawLauncherJumpCancel = true;
            }

            _sawLanding |= presentationEvent.Type == CombatPresentationEventType.Landed;
        }

        CaptureImpactFrameIfRequested(tick);

        if (tick < 480 || _summaryPrinted)
        {
            return;
        }

        _summaryPrinted = true;
        var fullJump = _maxJumpHeight >= 2.25f;
        var passed = _sawDash
            && fullJump
            && _sawAirLight
            && _sawAirMedium
            && _sawAirHeavy
            && _sawCrouchAttack
            && _sawGroundCombo
            && _sawLauncherJumpCancel
            && _sawUppercut
            && _sawRyuJab
            && _sawRyuStrong
            && _sawRyuHadouken
            && _sawRyuProjectile
            && _sawLanding;
        GD.Print(
            $"[CombatSmoke] SUMMARY passed={passed} dash={_sawDash} height={_maxJumpHeight:0.00} " +
            $"air={_sawAirLight}/{_sawAirMedium}/{_sawAirHeavy} crouch={_sawCrouchAttack} " +
            $"combo3={_sawGroundCombo} launcherJump={_sawLauncherJumpCancel} uppercut={_sawUppercut} " +
            $"ryuRoute={_sawRyuJab}/{_sawRyuStrong}/{_sawRyuHadouken} " +
            $"ryuProjectile={_sawRyuProjectile} landing={_sawLanding}");
        if (!passed)
        {
            GD.PushError("[CombatSmoke] One or more mobility/combo behaviors were not observed.");
        }
    }

    private void CaptureImpactFrameIfRequested(int tick)
    {
        var path = OS.GetEnvironment("PROJECT_MANNEQUIN_COMBAT_VFX_CAPTURE");
        if (_impactCaptureSaved
            || _impactCaptureRequestedTick < 0
            || tick < _impactCaptureRequestedTick + 2
            || string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var error = _simulation.GetViewport().GetTexture().GetImage().SavePng(path);
        if (error != Error.Ok)
        {
            GD.PushError($"[CombatSmoke] Could not save impact capture '{path}' ({error}).");
            return;
        }

        _impactCaptureSaved = true;
        GD.Print($"[CombatSmoke] Authored impact capture saved: {path}");
    }

    private CombatActor? GetPlayer()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.IsPlayerControlled);
    }

    private CombatActor? GetDummy()
    {
        return _simulation.Actors.FirstOrDefault(actor => actor.ActorId == "combat_smoke_dummy");
    }

    private static void ResetActors(CombatActor player, CombatActor dummy)
    {
        player.EndCurrentMove();
        player.SimPosition = new Vector3(10.0f, 0.0f, 0.0f);
        player.Velocity = Vector3.Zero;
        player.State = CombatActorState.Idle;

        dummy.EndCurrentMove();
        dummy.SimPosition = new Vector3(11.0f, 0.0f, 0.0f);
        dummy.Velocity = Vector3.Zero;
        dummy.State = CombatActorState.Idle;
        dummy.RestoreHealth(dummy.CurrentForm.MaxHealth);
    }
}
