using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;

namespace ProjectMannequin.Combat;

public enum CombatActorState
{
    Idle,
    Walking,
    Crouching,
    Dashing,
    JumpStartup,
    Jumping,
    Landing,
    Attacking,
    Blocking,
    Parrying,
    Hitstun,
    Blockstun,
    GuardBreak,
    Knockdown,
    FormSwapping,
    CinematicLocked,
    HomingDash,
    Flying,
    InstinctEvade,
    Dead,
}

public enum BlockPosture
{
    Standing,
    Crouching,
}

public sealed class CombatStateMachine
{
    private readonly CombatActor _actor;
    private IReadOnlyList<CombatActor> _currentActors = System.Array.Empty<CombatActor>();
    private int _hitstunFrames;
    private int _blockstunFrames;
    private int _guardFrames;
    private int _formSwapFrames;
    private int _formSwapCooldownFrames;
    private string _pendingRequestedFormId = "";
    private int _cinematicLockFrames;
    private int _jumpStartupFrames;
    private int _landingFrames;
    private int _wallRunFramesRemaining;
    private int _dashFrames;
    private int _dashElapsedFrames;
    private int _homingDashFrames;
    private int _homingDashElapsedFrames;
    private int _parryActiveFrames;
    private int _parryRecoveryFrames;
    private int _guardBreakFrames;
    private int _flightFrames;
    private int _flightCooldownFrames;
    private int _flightSharedLockoutFrames;
    private int _pendingFlightLandingRecoveryFrames;
    private int _instinctCharges;
    private int _instinctCooldownFrames;
    private int _instinctEvadeFrames;
    private bool _flightHitExtensionAvailable;
    private int _lastConsumedAttackInputTick = -1;
    private Vector3 _jumpDirection;
    private Vector3 _dashDirection;
    private bool _isRunActive;
    private InputButtons _runDirectionButton;
    private string _queuedFormId = "";

    public CombatStateMachine(CombatActor actor)
    {
        _actor = actor;
    }

    public int HitstunFramesRemaining => _hitstunFrames;
    public int BlockstunFramesRemaining => _blockstunFrames;
    public int GuardFramesRemaining => _guardFrames;
    public int FormSwapCooldownFrames => _formSwapCooldownFrames;
    public int CinematicLockFramesRemaining => _cinematicLockFrames;
    public int JumpStartupFramesRemaining => _jumpStartupFrames;
    public int LandingFramesRemaining => _landingFrames;
    public int DashFramesRemaining => _dashFrames;
    public int HomingDashFramesRemaining => _homingDashFrames;
    public int ParryActiveFramesRemaining => _parryActiveFrames;
    public int ParryRecoveryFramesRemaining => _parryRecoveryFrames;
    public int GuardBreakFramesRemaining => _guardBreakFrames;
    public int FlightFramesRemaining => _flightFrames;
    public int FlightCooldownFramesRemaining => _flightCooldownFrames;
    public int FlightSharedLockoutFramesRemaining => _flightSharedLockoutFrames;
    public int InstinctCharges => _instinctCharges;
    public int InstinctCooldownFramesRemaining => _instinctCooldownFrames;
    public bool IsFlightActive { get; private set; }
    public BlockPosture CurrentBlockPosture { get; private set; } = BlockPosture.Standing;

    public void Update(
        int tick,
        PlayerInputBuffer? input,
        CommandInterpreter commandInterpreter,
        IReadOnlyList<CombatActor> actors)
    {
        _currentActors = actors;
        if (_formSwapCooldownFrames > 0)
        {
            _formSwapCooldownFrames--;
        }
        if (_flightCooldownFrames > 0)
        {
            _flightCooldownFrames--;
        }
        if (_flightSharedLockoutFrames > 0)
        {
            _flightSharedLockoutFrames--;
        }
        if (_instinctCooldownFrames > 0)
        {
            _instinctCooldownFrames--;
        }

        // Consume a pending Form Select request (set by the in-fight overlay via
        // RequestFormSwap). Handled inside the fixed-tick loop so the swap stays
        // deterministic; dropped silently if the state is no longer swap-ready.
        if (_pendingRequestedFormId.Length > 0)
        {
            var requestedFormId = _pendingRequestedFormId;
            _pendingRequestedFormId = "";
            if (requestedFormId != _actor.CurrentForm.Id
                && TryStartFormSwap(requestedFormId, tick))
            {
                return;
            }
        }

        if (IsFlightActive && !UpdateFlightTimer(tick))
        {
            return;
        }

        switch (_actor.State)
        {
            case CombatActorState.Dead:
                return;
            case CombatActorState.Hitstun:
                UpdateHitstun(input, tick);
                return;
            case CombatActorState.Knockdown:
                UpdateKnockdown(input, tick);
                return;
            case CombatActorState.Blockstun:
                UpdateBlockstun(tick, input);
                return;
            case CombatActorState.Blocking:
                UpdateBlocking(input);
                return;
            case CombatActorState.Parrying:
                UpdateParry();
                return;
            case CombatActorState.GuardBreak:
                UpdateGuardBreak(tick);
                return;
            case CombatActorState.FormSwapping:
                UpdateFormSwap(tick);
                return;
            case CombatActorState.CinematicLocked:
                UpdateCinematicLock();
                return;
            case CombatActorState.Dashing:
                UpdateDash(tick, input, commandInterpreter, actors);
                return;
            case CombatActorState.JumpStartup:
                UpdateJumpStartup();
                return;
            case CombatActorState.Jumping:
                UpdateAirborne(tick, input, commandInterpreter, actors);
                return;
            case CombatActorState.Landing:
                UpdateLanding();
                return;
            case CombatActorState.HomingDash:
                UpdateHomingDash(tick, input, commandInterpreter, actors);
                return;
            case CombatActorState.Flying:
                UpdateFlying(tick, input, commandInterpreter, actors);
                return;
            case CombatActorState.InstinctEvade:
                UpdateInstinctEvade();
                return;
            case CombatActorState.Attacking:
                UpdateAttack(tick, input, commandInterpreter);
                return;
        }

        if (input is null)
        {
            if (!_actor.IsAiEnabled)
            {
                _actor.Velocity = new Vector3(0.0f, _actor.Velocity.Y, 0.0f);
                _actor.State = _actor.SimPosition.Y > 0.001f
                    ? CombatActorState.Jumping
                    : CombatActorState.Idle;
                return;
            }

            if (_actor.CpuBrain is not null)
            {
                ApplyCpuDecision(_actor.CpuBrain.Evaluate(tick, actors), tick);
                return;
            }

            if (_actor.ArcadeBrain is not null)
            {
                ApplyCpuDecision(_actor.ArcadeBrain.Evaluate(tick, actors), tick);
                return;
            }

            var aiMove = _actor.GetAiRequestedMove(tick, actors);
            if (aiMove is not null)
            {
                _actor.TryStartMove(aiMove, tick);
            }

            return;
        }

        var parryButtons = InputButtons.MediumPunch | InputButtons.MediumKick;
        if (input.IsHeld(parryButtons)
            && input.JustPressed.HasAny(parryButtons)
            && !input.CurrentHeld.HasAny(InputButtons.Left | InputButtons.Right)
            && _actor.SimPosition.Y <= 0.001f)
        {
            if (TryBeginParry(tick))
            {
                return;
            }
        }

        if (input.IsHeld(InputButtons.Block) && _actor.SimPosition.Y <= 0.001f)
        {
            EnterBlocking(0, tick, input.IsHeld(InputButtons.Crouch));
            return;
        }

        // Form swapping is driven by the in-fight Form Select overlay: pressing
        // FormSwap opens the selector (handled in the UI layer), and the chosen
        // target is applied via RequestFormSwap. It no longer blind-cycles here.

        // Assist Call — summon an archived form's attack (Marvel Tōkon-style directional assists)
        if (input.WasJustPressed(InputButtons.Assist))
        {
            TryCallAssist(input, tick);
        }

        if (TryBeginHomingDash(input, tick))
        {
            return;
        }

        var requestedMove = ResolveRequestedMove(input, commandInterpreter);
        if (requestedMove is not null)
        {
            FaceNearestOpponent(actors);
            if (_actor.TryStartMove(requestedMove.Value.Move, tick))
            {
                ConsumeAttackInput(requestedMove.Value);
                return;
            }
        }

        if (TryBeginRun(input, tick))
        {
            return;
        }

        if (input.WasJustPressed(InputButtons.Jump) && _actor.SimPosition.Y <= 0.001f)
        {
            BeginJump(input, tick);
            return;
        }

        if (input.WasJustPressed(InputButtons.Dash) && _actor.SimPosition.Y <= 0.001f)
        {
            BeginDash(input, tick);
            return;
        }

        UpdateGroundMovement(input);
    }

    public bool TryStartFormSwap(string targetFormId, int tick)
    {
        if (!CanSwapForm(targetFormId))
        {
            return false;
        }

        _queuedFormId = targetFormId;
        _formSwapFrames = 0;
        _actor.ClearCurrentMove();
        _actor.Velocity = new Vector3(0.0f, _actor.Velocity.Y, 0.0f);
        _actor.State = CombatActorState.FormSwapping;
        _actor.QueueEvent(CombatPresentationEventType.FormSwapStarted, tick, targetFormId);
        return true;
    }

    /// <summary>
    /// Queues an explicit form-swap target chosen by the in-fight Form Select
    /// overlay. The request is consumed on the next simulation tick if the actor
    /// is in a swap-ready state; otherwise it is dropped. This keeps all combat
    /// mutation inside the deterministic tick loop.
    /// </summary>
    public void RequestFormSwap(string targetFormId)
    {
        _pendingRequestedFormId = targetFormId ?? "";
    }

    private void TryCallAssist(PlayerInputBuffer input, int tick)
    {
        // Find the first non-current form in the loadout that has assist data
        foreach (var formId in _actor.FormArchive.ActiveLoadout)
        {
            if (formId == _actor.CurrentForm.Id) continue;

            var assistData = ProjectMannequin.Progression.AssistCatalog.GetAssistData(formId);
            if (assistData is null) continue;

            // Determine direction: Forward+Assist, Down+Assist, or Neutral
            var direction = ProjectMannequin.Progression.AssistDirection.Neutral;
            if (input.IsHeld(InputButtons.Down) || input.IsHeld(InputButtons.Crouch))
            {
                direction = ProjectMannequin.Progression.AssistDirection.Down;
            }
            else if (input.IsHeld(_actor.FacingRight ? InputButtons.Right : InputButtons.Left))
            {
                direction = ProjectMannequin.Progression.AssistDirection.Forward;
            }

            if (_actor.AssistSystem.TryCallAssist(_actor, assistData, direction, tick))
            {
                _actor.QueueEvent(
                    CombatPresentationEventType.AssistCallStarted,
                    tick,
                    payload: assistData.FormDisplayName);
            }

            return; // Only call one assist per press
        }
    }

    public void EnterHitstun(int frames, int tick)
    {
        CancelFlightForInterruption(tick);
        _hitstunFrames = Mathf.Max(1, frames);
        _actor.ArcadeBrain?.NotifyInterrupted(tick);
        _actor.ClearCurrentMove();
        _actor.State = CombatActorState.Hitstun;
    }

    public void EnterKnockdown(int tick, int knockdownFrames = -1)
    {
        CancelFlightForInterruption(tick);
        _hitstunFrames = 0;
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.Knockdown;
        // Re-using hitstun variable for knockdown duration to save state.
        _hitstunFrames = knockdownFrames >= 0 ? knockdownFrames : 40;
    }

    public void EnterLanding(int tick)
    {
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _landingFrames = _pendingFlightLandingRecoveryFrames > 0
            ? _pendingFlightLandingRecoveryFrames
            : GameConstants.LandingRecoveryFrames;
        _pendingFlightLandingRecoveryFrames = 0;
        _actor.RemainingJumps = 1; // Assuming 1 double jump
        _actor.HasAirDashed = false;
        _actor.WallRunActive = false;
        _actor.State = CombatActorState.Landing;
        _actor.QueueEvent(CombatPresentationEventType.Landed, tick);
    }

    public bool EnterBlocking(int guardFrames, int tick, bool crouching = false)
    {
        if (_actor.State is CombatActorState.Dead
            or CombatActorState.Hitstun
            or CombatActorState.Blockstun
            or CombatActorState.Knockdown
            or CombatActorState.FormSwapping
            or CombatActorState.CinematicLocked
            or CombatActorState.JumpStartup
            or CombatActorState.Landing
            or CombatActorState.GuardBreak
            || _actor.SimPosition.Y > 0.001f)
        {
            return false;
        }

        _guardFrames = Mathf.Max(_guardFrames, guardFrames);
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        CurrentBlockPosture = crouching ? BlockPosture.Crouching : BlockPosture.Standing;
        _actor.State = CombatActorState.Blocking;
        return true;
    }

    public void EnterBlockstun(int frames, int tick)
    {
        _blockstunFrames = Mathf.Max(_blockstunFrames, Mathf.Max(1, frames));
        _actor.ClearCurrentMove();
        _actor.State = CombatActorState.Blockstun;
    }

    public void EnterDeath(int tick)
    {
        CancelFlightForInterruption(tick);
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.Dead;
        _actor.QueueEvent(CombatPresentationEventType.ActorDefeated, tick);
    }

    public void EnterGuardBreak(int frames, int tick)
    {
        _guardBreakFrames = Mathf.Max(1, frames);
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.GuardBreak;
        _actor.QueueEvent(CombatPresentationEventType.GuardBroken, tick);
    }

    public bool CanParry(MoveData move)
    {
        return _actor.State == CombatActorState.Parrying
            && _parryActiveFrames > 0
            && !move.Unparryable
            && move.AttackHeight != AttackHeight.Throw;
    }

    public bool TryBeginParry(int tick)
    {
        if (_actor.SimPosition.Y > 0.001f
            || _actor.State is not (CombatActorState.Idle or CombatActorState.Walking))
        {
            return false;
        }

        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _parryActiveFrames = GameConstants.ParryActiveFrames;
        _parryRecoveryFrames = GameConstants.ParryRecoveryFrames;
        _actor.State = CombatActorState.Parrying;
        _actor.QueueEvent(CombatPresentationEventType.ParryStarted, tick);
        return true;
    }

    public bool ResolveParry(CombatActor attacker, MoveData move, int tick)
    {
        if (!CanParry(move))
        {
            return false;
        }

        var perfect = _parryActiveFrames >= GameConstants.ParryActiveFrames - 2;
        _parryActiveFrames = 0;
        _parryRecoveryFrames = 0;
        _actor.State = CombatActorState.Idle;
        _actor.AddMeter(GameConstants.ParryMeterReward);
        
        // Reflect Guard: data-driven pushback applied to the parried attacker.
        var pushDirection = attacker.SimPosition.X >= _actor.SimPosition.X ? 1.0f : -1.0f;
        var pushVelocity = HitResolution.ResolveReflectPushback(
            _actor.CurrentForm.ParryReflectPushback,
            _actor.IsBoss);
        attacker.Velocity = new Godot.Vector3(pushDirection * pushVelocity, 0.0f, 0.0f);

        attacker.StateMachine.EnterHitstun(
            perfect ? GameConstants.PerfectParryRecoilFrames : GameConstants.StandardParryRecoilFrames,
            tick);
        _actor.QueueEvent(
            CombatPresentationEventType.Parried,
            tick,
            perfect ? "perfect" : "standard",
            attacker.ActorId);
        if (pushVelocity > 0.0f)
        {
            _actor.QueueEvent(
                CombatPresentationEventType.ReflectGuard,
                tick,
                pushVelocity.ToString(System.Globalization.CultureInfo.InvariantCulture),
                attacker.ActorId);
        }
        return true;
    }

    public void EnterCinematicLock(int frames, int tick, string payload)
    {
        CancelFlightForInterruption(tick);
        _cinematicLockFrames = Mathf.Max(1, frames);
        _actor.ClearCurrentMove();
        _actor.Velocity = Vector3.Zero;
        _actor.IsVulnerable = false;
        _actor.State = CombatActorState.CinematicLocked;
        _actor.QueueEvent(CombatPresentationEventType.BossPhaseChanged, tick, payload);
    }

    public bool CanStartMove(MoveData move)
    {
        if (IsFlightActive)
        {
            return move.AllowDuringFlight;
        }

        if (move.RequiresFlight)
        {
            return false;
        }

        return !move.StartsFlight || CanActivateFlight();
    }

    public void ApplyPhaseMechanics(BossPhaseData? phase)
    {
        _instinctCharges = Mathf.Max(0, phase?.InstinctCharges ?? 0);
        _instinctCooldownFrames = 0;
        if (phase?.FlightEnabled != true)
        {
            CancelFlightForInterruption(0);
        }
    }

    public bool TryResolveInstinctEvade(CombatActor attacker, MoveData move, int tick)
    {
        var phase = _actor.CurrentBossPhase;
        if (_instinctCharges <= 0
            || _instinctCooldownFrames > 0
            || IsFlightActive
            || move.Unblockable
            || move.AttackHeight == AttackHeight.Throw
            || _actor.State is not (CombatActorState.Idle
                or CombatActorState.Walking
                or CombatActorState.Blocking))
        {
            return false;
        }

        _instinctCharges--;
        _instinctCooldownFrames = Mathf.Max(1, phase?.InstinctCooldownFrames ?? 90);
        _instinctEvadeFrames = 18;
        _actor.ClearCurrentMove();
        _actor.IsVulnerable = false;
        var awayX = attacker.SimPosition.X >= _actor.SimPosition.X ? -2.4f : 2.4f;
        var laneDirection = attacker.SimPosition.Z >= _actor.SimPosition.Z ? -1.0f : 1.0f;
        _actor.Velocity = new Vector3(awayX, 0.0f, laneDirection * 4.2f);
        _actor.State = CombatActorState.InstinctEvade;
        _actor.QueueEvent(
            CombatPresentationEventType.InstinctEvaded,
            tick,
            move.Id,
            attacker.ActorId);
        return true;
    }

    public void RestoreFlightTime(int frames)
    {
        var profile = _actor.CurrentForm.FlightProfile;
        if (!IsFlightActive
            || !_flightHitExtensionAvailable
            || profile is null
            || frames <= 0)
        {
            return;
        }

        _flightFrames = Mathf.Min(
            profile.DurationFrames,
            _flightFrames + Mathf.Min(frames, profile.HitExtensionFrames));
        _flightHitExtensionAvailable = false;
    }

    public void ExitFlightForMove(int tick)
    {
        EndFlight(
            tick,
            _actor.CurrentForm.FlightProfile?.ManualCancelRecoveryFrames ?? 10);
    }

    private bool CanSwapForm(string targetFormId)
    {
        return IsFormSwapReadyState
            && _actor.FormArchive.CanUse(targetFormId)
            && _actor.Meter >= GameConstants.DefaultFormSwapMeterCost;
    }

    /// <summary>
    /// True when the player may open the in-fight Form Select overlay: not on
    /// swap cooldown, in a neutral-ish actionable state, and with at least two
    /// equipped forms to choose between.
    /// </summary>
    public bool CanOpenFormSelect =>
        IsFormSwapReadyState && _actor.FormArchive.ActiveLoadout.Count >= 2;

    private bool IsFormSwapReadyState =>
        _formSwapCooldownFrames <= 0
        && _actor.State is CombatActorState.Idle
            or CombatActorState.Walking
            or CombatActorState.Dashing
            or CombatActorState.Jumping;

    private void UpdateHitstun(PlayerInputBuffer? input, int tick)
    {
        // Air recovery (tech): while airborne and descending, a fresh button
        // press lets the player recover from the launch with brief invulnerability.
        var airRecoveryInputs = InputButtons.LightPunch | InputButtons.MediumPunch
            | InputButtons.HeavyPunch | InputButtons.LightKick | InputButtons.MediumKick
            | InputButtons.HeavyKick | InputButtons.Jump;
        if (input != null
            && _actor.SimPosition.Y > 0.001f
            && _actor.Velocity.Y <= 0.0f
            && !_actor.PendingGroundBounce
            && input.WasJustPressed(airRecoveryInputs))
        {
            _hitstunFrames = 0;
            _actor.PendingWallBounce = false;
            _actor.InvincibilityFrames = GameConstants.WakeupInvulnerabilityFrames;
            _actor.State = CombatActorState.Jumping;
            _actor.QueueEvent(CombatPresentationEventType.AirRecovery, tick);
            return;
        }

        _hitstunFrames--;
        if (_hitstunFrames <= 0)
        {
            _actor.State = _actor.SimPosition.Y > 0.0f ? CombatActorState.Jumping : CombatActorState.Idle;
        }
    }

    private void UpdateKnockdown(PlayerInputBuffer? input, int tick)
    {
        // Quick Rise mechanic (Okizeme mindgame)
        if (input != null && _hitstunFrames > 20)
        {
            // If they press Down or any attack button while knocked down, quick rise!
            var quickRiseInputs = InputButtons.Down | InputButtons.LightPunch | InputButtons.MediumPunch | InputButtons.HeavyPunch | InputButtons.LightKick | InputButtons.MediumKick | InputButtons.HeavyKick;
            if (input.WasJustPressed(quickRiseInputs))
            {
                _hitstunFrames = 15; // Cut the knockdown time down to 15 frames for a fast wake-up
            }
        }

        _hitstunFrames--;
        if (_hitstunFrames <= 0)
        {
            _actor.State = CombatActorState.Idle;
            // Wake-up invulnerability: brief, auto-expiring i-frames for reversals.
            _actor.InvincibilityFrames = GameConstants.WakeupInvulnerabilityFrames;
        }
    }

    private void UpdateBlocking(PlayerInputBuffer? input)
    {
        _actor.Velocity = Vector3.Zero;
        if (input is not null)
        {
            if (input.IsHeld(InputButtons.Block))
            {
                CurrentBlockPosture = input.IsHeld(InputButtons.Down)
                    ? BlockPosture.Crouching
                    : BlockPosture.Standing;
                return;
            }

            _actor.State = CombatActorState.Idle;
            return;
        }

        _guardFrames--;
        if (_guardFrames <= 0)
        {
            _guardFrames = 0;
            _actor.State = CombatActorState.Idle;
        }
    }

    private void UpdateBlockstun(int tick, PlayerInputBuffer? input)
    {
        var advancingGuardButtons = InputButtons.LightPunch | InputButtons.LightKick;
        if (input != null 
            && input.IsHeld(advancingGuardButtons) 
            && input.JustPressed.HasAny(advancingGuardButtons)
            && _actor.Meter >= 25 
            && _actor.LastAttacker is not null)
        {
            _actor.AddMeter(-25);
            _actor.QueueEvent(CombatPresentationEventType.ParryStarted, tick, _actor.LastAttacker.ActorId);
            
            var pushbackDirection = _actor.FacingRight ? 1.0f : -1.0f;
            _actor.LastAttacker.Velocity = new Vector3(
                pushbackDirection * 20.0f, 
                _actor.LastAttacker.Velocity.Y, 
                _actor.LastAttacker.Velocity.Z);
            
            // Instantly recover from blockstun after successful advancing guard
            _blockstunFrames = 0;
        }

        _blockstunFrames--;
        if (input is null && _guardFrames > 0)
        {
            _guardFrames--;
        }

        if (_blockstunFrames > 0)
        {
            return;
        }

        _blockstunFrames = 0;
        var shouldKeepGuarding = input?.IsHeld(InputButtons.Block) == true || _guardFrames > 0;
        if (input is not null)
        {
            CurrentBlockPosture = input.IsHeld(InputButtons.Crouch)
                ? BlockPosture.Crouching
                : BlockPosture.Standing;
        }

        _actor.State = shouldKeepGuarding ? CombatActorState.Blocking : CombatActorState.Idle;
    }

    private void UpdateParry()
    {
        _actor.Velocity = Vector3.Zero;
        if (_parryActiveFrames > 0)
        {
            _parryActiveFrames--;
            return;
        }

        _parryRecoveryFrames--;
        if (_parryRecoveryFrames > 0)
        {
            return;
        }

        _parryRecoveryFrames = 0;
        _actor.State = CombatActorState.Idle;
    }

    private void UpdateGuardBreak(int tick)
    {
        _actor.Velocity = Vector3.Zero;
        _guardBreakFrames--;
        if (_guardBreakFrames > 0)
        {
            return;
        }

        _guardBreakFrames = 0;
        _actor.RestoreGuardGaugeToFull();
        _actor.State = CombatActorState.Idle;
        _actor.QueueEvent(CombatPresentationEventType.GuardRecovered, tick);
    }

    private void UpdateFormSwap(int tick)
    {
        _formSwapFrames++;

        var completionFrame = GameConstants.FormSwapStartupFrames + GameConstants.FormSwapActiveFrames;
        if (_formSwapFrames == completionFrame)
        {
            var form = _actor.FormArchive.GetForm(_queuedFormId);
            if (form is not null)
            {
                _actor.SetForm(form);
                _actor.QueueEvent(CombatPresentationEventType.FormSwapCompleted, tick, form.Id);
            }
        }

        var totalFrames = completionFrame + GameConstants.FormSwapRecoveryFrames;
        if (_formSwapFrames >= totalFrames)
        {
            _formSwapCooldownFrames = GameConstants.DefaultFormSwapCooldownFrames;
            _queuedFormId = "";
            _actor.State = _actor.SimPosition.Y > 0.0f ? CombatActorState.Jumping : CombatActorState.Idle;
        }
    }

    private void UpdateCinematicLock()
    {
        _cinematicLockFrames--;
        if (_cinematicLockFrames > 0)
        {
            return;
        }

        _actor.IsVulnerable = true;
        _actor.State = CombatActorState.Idle;
    }

    private bool CanActivateFlight()
    {
        var profile = _actor.CurrentForm.FlightProfile;
        if (profile is null
            || IsFlightActive
            || _flightCooldownFrames > 0
            || _flightSharedLockoutFrames > 0)
        {
            return false;
        }

        return !_actor.IsBoss || _actor.CurrentBossPhase?.FlightEnabled == true;
    }

    private void BeginFlight(int tick)
    {
        var profile = _actor.CurrentForm.FlightProfile;
        if (profile is null || !CanActivateFlight())
        {
            _actor.EndCurrentMove();
            return;
        }

        IsFlightActive = true;
        _flightFrames = Mathf.Max(1, profile.DurationFrames);
        _flightHitExtensionAvailable = true;
        _actor.ClearCurrentMove();
        _actor.SimPosition = new Vector3(
            _actor.SimPosition.X,
            Mathf.Clamp(
                Mathf.Max(_actor.SimPosition.Y, profile.MinimumHeight),
                profile.MinimumHeight,
                profile.MaximumHeight),
            _actor.SimPosition.Z);
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.Flying;
        _actor.QueueEvent(CombatPresentationEventType.FlightStarted, tick);
    }

    private bool UpdateFlightTimer(int tick)
    {
        _flightFrames--;
        if (_flightFrames > 0)
        {
            return true;
        }

        EndFlight(
            tick,
            _actor.CurrentForm.FlightProfile?.NaturalLandingRecoveryFrames ?? 18);
        _actor.ClearCurrentMove();
        return false;
    }

    private void EndFlight(int tick, int landingRecoveryFrames)
    {
        if (!IsFlightActive)
        {
            return;
        }

        var profile = _actor.CurrentForm.FlightProfile;
        IsFlightActive = false;
        _flightFrames = 0;
        _flightHitExtensionAvailable = false;
        _flightCooldownFrames = Mathf.Max(
            _flightCooldownFrames,
            profile?.ActivationCooldownFrames ?? 240);
        _flightSharedLockoutFrames = Mathf.Max(
            _flightSharedLockoutFrames,
            profile?.SharedMobilityLockoutFrames ?? 18);
        _pendingFlightLandingRecoveryFrames = Mathf.Max(1, landingRecoveryFrames);
        _actor.Velocity = new Vector3(
            _actor.Velocity.X,
            -Mathf.Max(2.0f, profile?.VerticalSpeed ?? 5.4f),
            _actor.Velocity.Z);
        if (_actor.State != CombatActorState.Attacking)
        {
            _actor.State = CombatActorState.Jumping;
        }
        _actor.QueueEvent(CombatPresentationEventType.FlightEnded, tick);
    }

    private void CancelFlightForInterruption(int tick)
    {
        if (!IsFlightActive)
        {
            return;
        }

        IsFlightActive = false;
        _flightFrames = 0;
        _flightHitExtensionAvailable = false;
        _pendingFlightLandingRecoveryFrames = 0;
        _flightCooldownFrames = Mathf.Max(
            _flightCooldownFrames,
            _actor.CurrentForm.FlightProfile?.ActivationCooldownFrames ?? 240);
        _actor.QueueEvent(CombatPresentationEventType.FlightEnded, tick);
    }

    private void UpdateInstinctEvade()
    {
        _instinctEvadeFrames--;
        if (_instinctEvadeFrames > 0)
        {
            return;
        }

        _instinctEvadeFrames = 0;
        _actor.IsVulnerable = true;
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.Idle;
    }

    private void UpdateAttack(int tick, PlayerInputBuffer? input, CommandInterpreter commandInterpreter)
    {
        var currentMove = _actor.CurrentMove;
        if (currentMove is null)
        {
            _actor.EndCurrentMove();
            return;
        }

        if (input is not null
            && currentMove.IsInJumpCancelWindow(_actor.CurrentMoveFrame)
            && (!currentMove.JumpCancelOnHitOnly || _actor.CurrentMoveConnected)
            && input.WasPressedWithin(InputButtons.Jump, 4))
        {
            BeginJump(input, tick, skipStartup: true);
            return;
        }

        if (input is not null && currentMove.IsInCancelWindow(_actor.CurrentMoveFrame))
        {
            var targetMove = ResolveRequestedMove(input, commandInterpreter);
            if (targetMove is not null
                && currentMove.CanCancelInto(targetMove.Value.Move)
                && _actor.TryStartMove(targetMove.Value.Move, tick))
            {
                ConsumeAttackInput(targetMove.Value);
                return;
            }
        }

        ApplyMoveVelocity(currentMove);

        _actor.CurrentMoveFrame++;
        if (currentMove.InvulnerableStartupFrames > 0
            && _actor.CurrentMoveFrame >= currentMove.InvulnerableStartupFrames)
        {
            _actor.IsVulnerable = true;
        }

        if (_actor.CurrentMoveFrame >= currentMove.TotalFrames)
        {
            _actor.ArcadeBrain?.NotifyMoveEnded(tick);
            if (currentMove.StartsFlight)
            {
                BeginFlight(tick);
                return;
            }
            _actor.EndCurrentMove();
        }
    }

    private MoveRequest? ResolveRequestedMove(PlayerInputBuffer input, CommandInterpreter commandInterpreter)
    {
        var isAirborne = _actor.SimPosition.Y > 0.001f;
        var nearestOpponentRange = _currentActors
            .Where(actor => actor.TeamId != _actor.TeamId && !actor.IsDead)
            .Select(actor => Mathf.Abs(actor.SimPosition.X - _actor.SimPosition.X))
            .DefaultIfEmpty(float.MaxValue)
            .Min();
        var commandDefinitions = _actor.CurrentForm.Moves
            .Where(move => !string.IsNullOrWhiteSpace(move.InputCommand))
            .Where(move => move.CanStartFromAirState(isAirborne))
            .Where(CanStartMove)
            .Where(move => move.CanStartAtRange(nearestOpponentRange))
            .Select(move => commandInterpreter.Parse(
                move.Id,
                move.InputCommand,
                move.Priority,
                move.InputWindowFrames,
                move.DirectionLeniency))
            .ToArray();

        var command = commandInterpreter.FindBestCommand(
            commandDefinitions,
            input,
            _actor.FacingRight,
            _lastConsumedAttackInputTick);
        if (command is null)
        {
            return null;
        }

        var move = _actor.CurrentForm.FindMove(command.Value.Command.Name);
        if (move is not null && _actor.CurrentMove is not null && move.Id == _actor.CurrentMove.Id)
        {
            if (!string.IsNullOrWhiteSpace(_actor.CurrentMove.NextAutoComboMoveId))
            {
                var nextMove = _actor.CurrentForm.FindMove(_actor.CurrentMove.NextAutoComboMoveId);
                if (nextMove is not null)
                {
                    move = nextMove;
                }
            }
        }

        var inputTick = input.GetFrame(command.Value.InputAgeFrames).Tick;
        return move is null ? null : new MoveRequest(move, inputTick);
    }

    private void FaceNearestOpponent(IReadOnlyList<CombatActor> actors)
    {
        var target = actors
            .Where(actor => actor.TeamId != _actor.TeamId && !actor.IsDead
                && !CombatActor.AreDuelIsolated(_actor, actor))
            .OrderBy(actor => actor.SimPosition.DistanceSquaredTo(_actor.SimPosition))
            .FirstOrDefault();

        if (target is null)
        {
            return;
        }

        _actor.FacingRight = target.SimPosition.X >= _actor.SimPosition.X;
    }

    private void ApplyMoveVelocity(MoveData currentMove)
    {
        if (currentMove.IsForwardVelocityActive(_actor.CurrentMoveFrame))
        {
            var facingSign = _actor.FacingRight ? 1.0f : -1.0f;
            _actor.Velocity = new Vector3(currentMove.ForwardVelocity * facingSign, _actor.Velocity.Y, 0.0f);
            return;
        }

        if (currentMove.AllowAir && _actor.SimPosition.Y > 0.001f)
        {
            return;
        }

        _actor.Velocity = new Vector3(0.0f, _actor.Velocity.Y, 0.0f);
    }

    private void UpdateGroundMovement(PlayerInputBuffer input)
    {
        var held = input.CurrentHeld;
        var x = 0.0f;
        var z = 0.0f;

        if (held.HasAny(InputButtons.Left))
        {
            x -= 1.0f;
        }

        if (held.HasAny(InputButtons.Right))
        {
            x += 1.0f;
        }

        if (held.HasAny(InputButtons.Up))
        {
            z -= 1.0f;
        }

        if (held.HasAny(InputButtons.Down))
        {
            z += 1.0f;
        }

        if (!Mathf.IsZeroApprox(x))
        {
            _actor.FacingRight = x > 0.0f;
        }

        _actor.Velocity = new Vector3(
            x * _actor.CurrentForm.WalkSpeed,
            _actor.Velocity.Y,
            z * _actor.CurrentForm.WalkSpeed * GameConstants.DefaultLaneSpeedMultiplier);

        if (!Mathf.IsZeroApprox(x) || !Mathf.IsZeroApprox(z))
        {
            _actor.State = CombatActorState.Walking;
        }
        else if (held.HasAny(InputButtons.Crouch))
        {
            _actor.State = CombatActorState.Crouching;
        }
        else
        {
            _actor.State = CombatActorState.Idle;
        }
    }

    private void BeginDash(PlayerInputBuffer input, int tick)
    {
        _isRunActive = false;
        _dashDirection = ReadMovementDirection(input.CurrentHeld);
        if (_dashDirection.IsZeroApprox())
        {
            _dashDirection = new Vector3(_actor.FacingRight ? 1.0f : -1.0f, 0.0f, 0.0f);
        }
        else if (!Mathf.IsZeroApprox(_dashDirection.X))
        {
            _actor.FacingRight = _dashDirection.X > 0.0f;
        }

        _dashFrames = GameConstants.DashDurationFrames;
        _dashElapsedFrames = 0;
        _actor.Velocity = DashVelocity();
        _actor.State = CombatActorState.Dashing;
        _actor.QueueEvent(CombatPresentationEventType.DashStarted, tick);
    }

    private void UpdateDash(
        int tick,
        PlayerInputBuffer? input,
        CommandInterpreter commandInterpreter,
        IReadOnlyList<CombatActor> actors)
    {
        _dashElapsedFrames++;
        _dashFrames--;

        if (input is not null && _dashElapsedFrames >= GameConstants.DashAttackCancelFrame)
        {
            var requestedMove = ResolveRequestedMove(input, commandInterpreter);
            if (requestedMove is not null)
            {
                FaceNearestOpponent(actors);
                if (_actor.TryStartMove(requestedMove.Value.Move, tick))
                {
                    ConsumeAttackInput(requestedMove.Value);
                    return;
                }
            }
        }

        if (input is not null
            && _dashElapsedFrames >= GameConstants.DashJumpCancelFrame
            && input.WasPressedWithin(InputButtons.Jump, 3))
        {
            BeginJump(input, tick);
            return;
        }

        if (_dashFrames <= 0)
        {
            if (_isRunActive && input is not null && input.IsHeld(_runDirectionButton))
            {
                _dashFrames = 1;
            }
            else
            {
                _isRunActive = false;
                _actor.Velocity = Vector3.Zero;
                _actor.State = _actor.SimPosition.Y > 0.001f ? CombatActorState.Jumping : CombatActorState.Idle;
                return;
            }
        }

        _actor.Velocity = DashVelocity();
    }

    private Vector3 DashVelocity()
    {
        return new Vector3(
            _dashDirection.X * _actor.CurrentForm.DashSpeed,
            0.0f,
            _dashDirection.Z * _actor.CurrentForm.DashSpeed * GameConstants.DefaultLaneSpeedMultiplier);
    }

    private void BeginJump(PlayerInputBuffer input, int tick, bool skipStartup = false)
    {
        _actor.ClearCurrentMove();
        _wallRunFramesRemaining = _actor.InVerticalSection
            ? GameConstants.WallRunFrames * 2
            : GameConstants.WallRunFrames;
        _jumpDirection = ReadMovementDirection(input.CurrentHeld);
        if (!Mathf.IsZeroApprox(_jumpDirection.X))
        {
            _actor.FacingRight = _jumpDirection.X > 0.0f;
        }

        if (_actor.SimPosition.Y > 0.001f)
        {
            _actor.RemainingJumps--;
            var horizontalSpeed = _actor.CurrentForm.WalkSpeed * 0.92f;
            _actor.Velocity = new Vector3(
                _jumpDirection.X * horizontalSpeed,
                _actor.CurrentForm.JumpVelocity,
                _jumpDirection.Z * horizontalSpeed * GameConstants.DefaultLaneSpeedMultiplier);
            _actor.State = CombatActorState.Jumping;
            _actor.QueueEvent(CombatPresentationEventType.JumpStarted, tick);
            return;
        }

        _jumpStartupFrames = skipStartup ? 1 : GameConstants.JumpStartupFrames;
        _actor.Velocity = Vector3.Zero;
        _actor.State = CombatActorState.JumpStartup;
        _actor.QueueEvent(CombatPresentationEventType.JumpStarted, tick);
    }

    private void BeginWallJump(int tick)
    {
        // D2: push off the touched wall and refresh an air jump.
        var awayFromWall = _actor.NearLeftWall ? 1.0f : -1.0f;
        _actor.FacingRight = awayFromWall > 0.0f;
        var horizontalSpeed = _actor.CurrentForm.WalkSpeed * 1.05f;
        _actor.Velocity = new Vector3(
            awayFromWall * horizontalSpeed,
            _actor.CurrentForm.JumpVelocity * 0.95f,
            _actor.Velocity.Z);
        _actor.RemainingJumps = 1;
        _actor.HasAirDashed = false;
        _actor.WallRunActive = false;
        _wallRunFramesRemaining = _actor.InVerticalSection
            ? GameConstants.WallRunFrames * 2
            : GameConstants.WallRunFrames;
        _actor.State = CombatActorState.Jumping;
        _actor.QueueEvent(CombatPresentationEventType.JumpStarted, tick, "WallJump");
    }

    private bool TryBeginRun(PlayerInputBuffer input, int tick)
    {
        if (_actor.SimPosition.Y > 0.001f)
        {
            return false;
        }

        if (input.WasJustPressed(InputButtons.Left)
            && input.IsHeld(InputButtons.Left)
            && !input.IsHeld(InputButtons.Right | InputButtons.Up | InputButtons.Down)
            && input.WasPressedTwiceWithin(InputButtons.Left, GameConstants.RunDoubleTapWindowFrames))
        {
            BeginRun(-1.0f, tick);
            return true;
        }

        if (input.WasJustPressed(InputButtons.Right)
            && input.IsHeld(InputButtons.Right)
            && !input.IsHeld(InputButtons.Left | InputButtons.Up | InputButtons.Down)
            && input.WasPressedTwiceWithin(InputButtons.Right, GameConstants.RunDoubleTapWindowFrames))
        {
            BeginRun(1.0f, tick);
            return true;
        }

        return false;
    }

    private void BeginRun(float horizontalDirection, int tick)
    {
        _isRunActive = true;
        _runDirectionButton = horizontalDirection < 0.0f ? InputButtons.Left : InputButtons.Right;
        _dashDirection = new Vector3(horizontalDirection, 0.0f, 0.0f);
        _actor.FacingRight = horizontalDirection > 0.0f;
        _dashFrames = GameConstants.DashDurationFrames;
        _dashElapsedFrames = 0;
        _actor.Velocity = DashVelocity();
        _actor.State = CombatActorState.Dashing;
        _actor.QueueEvent(CombatPresentationEventType.DashStarted, tick);
    }

    private void UpdateJumpStartup()
    {
        _jumpStartupFrames--;
        if (_jumpStartupFrames > 0)
        {
            return;
        }

        var horizontalSpeed = _actor.CurrentForm.WalkSpeed * 0.92f;
        _actor.Velocity = new Vector3(
            _jumpDirection.X * horizontalSpeed,
            _actor.CurrentForm.JumpVelocity,
            _jumpDirection.Z * horizontalSpeed * GameConstants.DefaultLaneSpeedMultiplier);
        _actor.State = CombatActorState.Jumping;
    }

    private void UpdateAirborne(
        int tick,
        PlayerInputBuffer? input,
        CommandInterpreter commandInterpreter,
        IReadOnlyList<CombatActor> actors)
    {
        if (input is null)
        {
            return;
        }

        if (TryBeginHomingDash(input, tick))
        {
            return;
        }

        var requestedMove = ResolveRequestedMove(input, commandInterpreter);
        if (requestedMove is not null)
        {
            FaceNearestOpponent(actors);
            if (_actor.TryStartMove(requestedMove.Value.Move, tick))
            {
                ConsumeAttackInput(requestedMove.Value);
                return;
        }
    }

        if (!_actor.HasAirDashed)
        {
            if (input.WasJustPressed(InputButtons.Left)
                && input.WasPressedTwiceWithin(InputButtons.Left, GameConstants.RunDoubleTapWindowFrames))
            {
                _actor.HasAirDashed = true;
                BeginRun(-1.0f, tick);
                return;
            }

            if (input.WasJustPressed(InputButtons.Right)
                && input.WasPressedTwiceWithin(InputButtons.Right, GameConstants.RunDoubleTapWindowFrames))
            {
                _actor.HasAirDashed = true;
                BeginRun(1.0f, tick);
                return;
            }
        }

        // D2: Wall-jump — kick off a nearby wall while airborne.
        if (input.WasJustPressed(InputButtons.Jump) && (_actor.NearLeftWall || _actor.NearRightWall))
        {
            BeginWallJump(tick);
            return;
        }

        if (_actor.RemainingJumps > 0 && input.WasJustPressed(InputButtons.Jump))
        {
            BeginJump(input, tick, skipStartup: true);
            return;
        }

        // D1: Wall-run/slide — hug a nearby wall to climb briefly, then slow the fall.
        var huggingWall = (_actor.NearLeftWall && input.IsHeld(InputButtons.Left))
            || (_actor.NearRightWall && input.IsHeld(InputButtons.Right));
        if (huggingWall)
        {
            _actor.WallRunActive = true;
            if (_wallRunFramesRemaining > 0)
            {
                _wallRunFramesRemaining--;
                _actor.Velocity = new Vector3(_actor.Velocity.X, GameConstants.WallRunClimbSpeed, _actor.Velocity.Z);
            }
            else if (_actor.Velocity.Y < GameConstants.WallSlideFallSpeed)
            {
                _actor.Velocity = new Vector3(_actor.Velocity.X, GameConstants.WallSlideFallSpeed, _actor.Velocity.Z);
            }
        }
        else
        {
            _actor.WallRunActive = false;
        }

        var direction = ReadMovementDirection(input.CurrentHeld);
        var airSpeed = _actor.CurrentForm.WalkSpeed * GameConstants.DefaultAirControlMultiplier;
        var targetX = direction.X * airSpeed;
        var targetZ = direction.Z * airSpeed * GameConstants.DefaultLaneSpeedMultiplier;
        _actor.Velocity = new Vector3(
            Mathf.Lerp(_actor.Velocity.X, targetX, 0.16f),
            _actor.Velocity.Y,
            Mathf.Lerp(_actor.Velocity.Z, targetZ, 0.16f));
    }

    private void UpdateLanding()
    {
        _landingFrames--;
        if (_landingFrames > 0)
        {
            return;
        }

        _landingFrames = 0;
        _actor.State = CombatActorState.Idle;
    }

    private void UpdateFlying(
        int tick,
        PlayerInputBuffer? input,
        CommandInterpreter commandInterpreter,
        IReadOnlyList<CombatActor> actors)
    {
        if (input is null)
        {
            if (!_actor.IsAiEnabled)
            {
                _actor.Velocity = Vector3.Zero;
                return;
            }

            if (_actor.CpuBrain is not null)
            {
                ApplyCpuDecision(_actor.CpuBrain.Evaluate(tick, actors), tick);
            }
            return;
        }

        var requestedMove = ResolveRequestedMove(input, commandInterpreter);
        if (requestedMove is not null)
        {
            FaceNearestOpponent(actors);
            if (_actor.TryStartMove(requestedMove.Value.Move, tick))
            {
                ConsumeAttackInput(requestedMove.Value);
                return;
            }
        }

        var profile = _actor.CurrentForm.FlightProfile;
        if (profile is null)
        {
            EndFlight(tick, GameConstants.LandingRecoveryFrames);
            return;
        }

        var held = input.CurrentHeld;
        var x = held.HasAny(InputButtons.Left) ? -1.0f : 0.0f;
        if (held.HasAny(InputButtons.Right))
        {
            x += 1.0f;
        }
        var y = held.HasAny(InputButtons.Jump) ? 1.0f : 0.0f;
        if (held.HasAny(InputButtons.Crouch))
        {
            y -= 1.0f;
        }
        var z = held.HasAny(InputButtons.Up) ? -1.0f : 0.0f;
        if (held.HasAny(InputButtons.Down))
        {
            z += 1.0f;
        }

        var planar = new Vector2(x, y);
        if (planar.LengthSquared() > 1.0f)
        {
            planar = planar.Normalized();
        }
        var targetVelocity = new Vector3(
            planar.X * profile.HorizontalSpeed,
            planar.Y * profile.VerticalSpeed,
            z * profile.LaneSpeed);
        _actor.Velocity = _actor.Velocity.Lerp(
            targetVelocity,
            Mathf.Clamp(profile.Acceleration, 0.01f, 1.0f));
        if (!Mathf.IsZeroApprox(x))
        {
            _actor.FacingRight = x > 0.0f;
        }
    }

    private static Vector3 ReadMovementDirection(InputButtons held)
    {
        var direction = Vector3.Zero;
        if (held.HasAny(InputButtons.Left)) direction.X -= 1.0f;
        if (held.HasAny(InputButtons.Right)) direction.X += 1.0f;
        if (held.HasAny(InputButtons.Up)) direction.Z -= 1.0f;
        if (held.HasAny(InputButtons.Down)) direction.Z += 1.0f;
        return direction.IsZeroApprox() ? Vector3.Zero : direction.Normalized();
    }

    private void ConsumeAttackInput(MoveRequest request)
    {
        _lastConsumedAttackInputTick = Mathf.Max(_lastConsumedAttackInputTick, request.InputTick);
    }

    private void ApplyCpuDecision(CpuFighterDecision decision, int tick)
    {
        switch (decision.Intent)
        {
            case CpuFighterIntent.Guard:
                EnterBlocking(decision.DurationFrames, tick);
                return;
            case CpuFighterIntent.JumpEvade:
                if (_actor.SimPosition.Y <= 0.001f)
                {
                    var jumpMovementSpeed = _actor.CurrentForm.WalkSpeed;
                    _actor.Velocity = new Vector3(
                        decision.MovementDirection.X * jumpMovementSpeed,
                        _actor.CurrentForm.JumpVelocity,
                        decision.MovementDirection.Z * jumpMovementSpeed * GameConstants.DefaultLaneSpeedMultiplier);
                    _actor.State = CombatActorState.Jumping;
                }

                return;
            case CpuFighterIntent.Attack:
            case CpuFighterIntent.AntiAir:
            case CpuFighterIntent.Punish:
                if (decision.Move is not null)
                {
                    if (!_actor.TryStartMove(decision.Move, tick))
                    {
                        _actor.ArcadeBrain?.NotifyInterrupted(tick);
                    }
                }

                return;
        }

        var direction = decision.MovementDirection;
        if (direction.IsZeroApprox())
        {
            _actor.Velocity = IsFlightActive
                ? Vector3.Zero
                : new Vector3(0.0f, _actor.Velocity.Y, 0.0f);
            _actor.State = IsFlightActive
                ? CombatActorState.Flying
                : _actor.SimPosition.Y > 0.001f
                    ? CombatActorState.Jumping
                    : CombatActorState.Idle;
            return;
        }

        var speedMultiplier = _actor.CurrentBossPhase?.MovementSpeedMultiplier ?? 1.0f;
        var speed = decision.UseDash
            ? _actor.CurrentForm.DashSpeed
            : _actor.CurrentForm.WalkSpeed;
        _actor.Velocity = new Vector3(
            direction.X * speed * speedMultiplier,
            _actor.Velocity.Y,
            direction.Z * speed * GameConstants.DefaultLaneSpeedMultiplier * speedMultiplier);
        if (decision.UseDash)
        {
            _dashDirection = direction;
            _dashFrames = Mathf.Max(GameConstants.DashDurationFrames, decision.DurationFrames);
            _dashElapsedFrames = 0;
        }

        _actor.State = IsFlightActive
            ? CombatActorState.Flying
            : _actor.SimPosition.Y > 0.001f
                ? CombatActorState.Jumping
                : decision.UseDash
                    ? CombatActorState.Dashing
                    : CombatActorState.Walking;
    }

    private bool TryBeginHomingDash(PlayerInputBuffer input, int tick)
    {
        var homingButtons = InputButtons.HeavyPunch | InputButtons.HeavyKick;
        if (!input.IsHeld(homingButtons)
            || !input.JustPressed.HasAny(homingButtons)
            || _actor.State is not (CombatActorState.Idle or CombatActorState.Walking or CombatActorState.Jumping)
            || (_actor.SimPosition.Y > 0.001f && _actor.HasAirDashed))
        {
            return false;
        }

        if (_actor.SimPosition.Y > 0.001f)
        {
            _actor.HasAirDashed = true;
        }

        _actor.ClearCurrentMove();
        _homingDashFrames = GameConstants.HomingDashDurationFrames;
        _homingDashElapsedFrames = 0;
        _actor.State = CombatActorState.HomingDash;
        _actor.QueueEvent(CombatPresentationEventType.DashStarted, tick, "Homing Dash");
        return true;
    }

    private void UpdateHomingDash(int tick, PlayerInputBuffer? input, CommandInterpreter commandInterpreter, IReadOnlyList<CombatActor> actors)
    {
        _homingDashFrames--;
        _homingDashElapsedFrames++;
        var target = actors
            .Where(a => a.TeamId != _actor.TeamId && !a.IsDead)
            .OrderBy(a => a.SimPosition.DistanceSquaredTo(_actor.SimPosition))
            .FirstOrDefault();

        if (target is null || _homingDashFrames <= 0)
        {
            EndHomingDash();
            return;
        }

        if (input is not null && _homingDashElapsedFrames >= GameConstants.HomingDashAttackCancelFrame)
        {
            var requestedMove = ResolveRequestedMove(input, commandInterpreter);
            if (requestedMove is not null)
            {
                FaceNearestOpponent(actors);
                if (_actor.TryStartMove(requestedMove.Value.Move, tick))
                {
                    ConsumeAttackInput(requestedMove.Value);
                    return;
                }
            }
        }

        var delta = target.SimPosition - _actor.SimPosition;
        delta.Y += 1.0f; 
        
        if (delta.LengthSquared() < 1.0f)
        {
            EndHomingDash();
            return;
        }

        _actor.FacingRight = target.SimPosition.X >= _actor.SimPosition.X;
        _actor.Velocity = delta.Normalized() * GameConstants.HomingDashSpeed;
    }

    private void EndHomingDash()
    {
        _homingDashFrames = 0;
        _actor.Velocity = new Vector3(0.0f, _actor.Velocity.Y, 0.0f);
        _actor.State = _actor.SimPosition.Y > 0.001f
            ? CombatActorState.Jumping
            : CombatActorState.Idle;
    }

    private readonly record struct MoveRequest(MoveData Move, int InputTick);
}
