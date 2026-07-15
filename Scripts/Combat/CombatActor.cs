using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;
using ProjectMannequin.LocalInput;
using ProjectMannequin.Presentation;
using ProjectMannequin.Progression;

namespace ProjectMannequin.Combat;

public partial class CombatActor : Node3D
{
    private readonly List<CombatBox> _activeBoxes = new();
    private readonly List<CombatPresentationEvent> _pendingEvents = new();
    private readonly PlayerInputBuffer _trainingInputBuffer = new();
    private int _moveInstanceCounter;
    private int _nextAiMoveTick = -1;
    private int _aiMoveCursor;
    private int _lastGuardDamageTick = -10000;
    private float _guardRecoveryCarry;
    private int _playableVisualVariantIndex;

    public string ActorId { get; set; } = Guid.NewGuid().ToString("N");
    public int TeamId { get; set; }
    public int PlayerId { get; set; }
    public int AiSlotIndex { get; set; }
    public bool IsPlayerControlled { get; set; }
    public bool IsBoss { get; set; }
    public bool IsAiEnabled { get; set; } = true;
    public bool IsElite { get; set; }
    public bool NearLeftWall { get; set; }
    public bool NearRightWall { get; set; }
    public bool WallRunActive { get; set; }
    public bool InVerticalSection { get; set; }
    public float HoverHeight { get; set; }
    
    // Artifact Modifiers
    private int _temporaryDamageModifierPercent = 0;
    public int DamageModifierPercent
    {
        get
        {
            var total = _temporaryDamageModifierPercent;
            if (!IsPlayerControlled)
            {
                return total;
            }

            var session = ProjectMannequin.Progression.RunSessionManager.Instance;
            if (session == null) return total;
            
            foreach (var artId in session.ActiveArtifacts)
            {
                var artifact = ProjectMannequin.Progression.ArtifactCatalog.GetArtifact(artId);
                if (artifact != null) total += artifact.DamageModifierPercent;
            }

            total += session.GetSynergyDamageBonusPercent();
            return total;
        }
        set { _temporaryDamageModifierPercent = value; }
    }

    private int _temporaryDefenseModifierPercent = 0;
    public int DefenseModifierPercent
    {
        get
        {
            var total = _temporaryDefenseModifierPercent;
            if (!IsPlayerControlled)
            {
                return total;
            }

            var session = ProjectMannequin.Progression.RunSessionManager.Instance;
            if (session == null) return total;
            
            foreach (var artId in session.ActiveArtifacts)
            {
                var artifact = ProjectMannequin.Progression.ArtifactCatalog.GetArtifact(artId);
                if (artifact != null) total += artifact.DefenseModifierPercent;
            }
            return total;
        }
        set { _temporaryDefenseModifierPercent = value; }
    }

    public int MeterGainModifierPercent
    {
        get
        {
            if (!IsPlayerControlled) return 0;
            var session = ProjectMannequin.Progression.RunSessionManager.Instance;
            int total = 0;
            foreach (var artId in session.ActiveArtifacts)
            {
                var artifact = ProjectMannequin.Progression.ArtifactCatalog.GetArtifact(artId);
                if (artifact != null) total += artifact.MeterGainModifierPercent;
            }
            return total;
        }
    }

    public int InvincibilityFrames { get; set; }
    public bool IsVulnerable { get; set; } = true;
    public bool FacingRight { get; set; } = true;
    public bool IsDead => State == CombatActorState.Dead;
    public int AiInitialDelayFrames { get; set; } = 30;
    public int AiAttackIntervalFrames { get; set; } = 75;
    public Color PresentationTint { get; set; } = Colors.White;

    public Vector3 SimPosition { get; set; }
    public Vector3 Velocity { get; set; }
    public CombatActorState State { get; internal set; } = CombatActorState.Idle;
    public CharacterData CurrentForm { get; private set; } = new();
    public MoveData? CurrentMove { get; internal set; }
    public int CurrentMoveFrame { get; internal set; }
    public int CurrentMoveInstanceId { get; private set; }
    public int CurrentMoveStartedTick { get; private set; } = -1;
    public bool CurrentMoveConnected { get; private set; }
    public int ComboHitCount { get; private set; }
    public int Health { get; private set; }
    public int Meter { get; private set; }
    public int GuardGauge { get; private set; }
    public int RemainingJumps { get; set; }
    public bool HasAirDashed { get; set; }
    public CombatActor? LastAttacker { get; private set; }
    public bool PendingGroundBounce { get; internal set; }
    public bool PendingWallBounce { get; internal set; }
    public int PendingKnockdownFrames { get; internal set; } = -1;
    public int CurrentBossPhaseIndex { get; private set; } = -1;
    public BossPhaseData? CurrentBossPhase =>
        CurrentBossPhaseIndex >= 0 && CurrentBossPhaseIndex < CurrentForm.BossPhases.Count
            ? CurrentForm.BossPhases[CurrentBossPhaseIndex]
            : null;
    public string CurrentVisualVariantId =>
        CurrentBossPhase?.VisualVariantId
        ?? (CurrentForm.VisualVariants.Count > 0
            ? CurrentForm.VisualVariants[
                Mathf.Clamp(
                    _playableVisualVariantIndex,
                    0,
                    CurrentForm.VisualVariants.Count - 1)].Id
            : "");
    public CharacterVisualVariantData? CurrentVisualVariant =>
        CurrentForm.FindVisualVariant(CurrentVisualVariantId);
    public int? LockedTargetPlayerId { get; set; }

    /// <summary>
    /// True when <paramref name="a"/> and <paramref name="b"/> belong to different
    /// isolated duels and must not interact (hits or projectiles): one is an enemy
    /// locked to a specific player and the other is a <em>different</em> player.
    /// Enemies with no lock (shared co-op / single-player / Simpsons-style shared
    /// bosses) never isolate, so ordinary play is unaffected.
    /// </summary>
    public static bool AreDuelIsolated(CombatActor a, CombatActor b)
    {
        if (a.LockedTargetPlayerId is int lockedA && b.IsPlayerControlled && b.PlayerId != lockedA)
        {
            return true;
        }

        if (b.LockedTargetPlayerId is int lockedB && a.IsPlayerControlled && a.PlayerId != lockedB)
        {
            return true;
        }

        return false;
    }

    public bool IsFlightActive => StateMachine?.IsFlightActive == true;
    public int InstinctCharges => StateMachine?.InstinctCharges ?? 0;
    public FormArchive FormArchive { get; } = new();
    public AssistCallSystem AssistSystem { get; } = new();
    public CombatStateMachine StateMachine { get; private set; } = null!;
    public CpuFighterBrain? CpuBrain { get; private set; }
    public ArcadeEnemyBrain? ArcadeBrain { get; private set; }
    public TrainingDummyBrain? TrainingBrain { get; set; }
    public GameSimulation? Simulation { get; internal set; }
    public CharacterData? UnlockableFormOnDefeat { get; set; }
    public IReadOnlyList<CombatBox> ActiveBoxes => _activeBoxes;

    private CharacterVisualComponent? _visualComponent;
    private string _comboTargetActorId = "";
    private int _lastComboHitTick = -GameConstants.ComboDisplayFrames;
    private readonly Dictionary<string, int> _comboMoveUses = new();
    private float _comboStarterProration = 1.0f;

    public override void _Ready()
    {
        StateMachine ??= new CombatStateMachine(this);
        
        // Create visual representation if not already present
        if (_visualComponent == null)
        {
            _visualComponent = FindVisualComponent() ?? new CharacterVisualComponent
            {
                Name = "CharacterVisualComponent",
            };
        }

        if (_visualComponent.GetParent() is null)
        {
            AddChild(_visualComponent);
        }
    }

    public void Initialize(CharacterData startingForm)
    {
        CurrentForm = startingForm;
        _playableVisualVariantIndex = 0;
        Health = startingForm.MaxHealth;
        Meter = 0;
        GuardGauge = startingForm.MaxGuardGauge;
        _nextAiMoveTick = -1;
        _aiMoveCursor = 0;
        StateMachine = new CombatStateMachine(this);
        CurrentBossPhaseIndex = IsBoss && startingForm.BossPhases.Count > 0 ? 0 : -1;
        ApplyCurrentBossPhaseSettings();
        CpuBrain = !IsPlayerControlled && startingForm.CpuProfile is not null
            ? new CpuFighterBrain(this, startingForm.CpuProfile)
            : null;
        ArcadeBrain = !IsPlayerControlled && !IsBoss && startingForm.ArcadeEnemyProfile is not null
            ? new ArcadeEnemyBrain(this, startingForm.ArcadeEnemyProfile)
            : null;
        FormArchive.UnlockForm(startingForm);
        // Sync SimPosition with GlobalPosition if in tree, otherwise use Position
        SimPosition = IsInsideTree() ? GlobalPosition : Position;
        
        // Update visual color based on team
        if (_visualComponent != null)
        {
            _visualComponent.TintLoadedSpriteSheets = !IsPlayerControlled;
            var teamColor = PresentationTint != Colors.White
                ? PresentationTint
                : TeamId switch
            {
                1 => Colors.CornflowerBlue,  // Player team - blue
                2 => Colors.Salmon,          // Enemy team - red
                _ => Colors.Gray
            };
            _visualComponent.SetColor(teamColor);
        }
    }

    public bool TryStartMove(MoveData move, int tick)
    {
        var isAirborne = SimPosition.Y > 0.001f;
        if (State == CombatActorState.Dead
            || Meter < move.MeterCost
            || !move.CanStartFromAirState(isAirborne)
            || !StateMachine.CanStartMove(move))
        {
            return false;
        }

        if (move.EndsFlightOnStart)
        {
            StateMachine.ExitFlightForMove(tick);
        }

        Meter = Mathf.Max(0, Meter - move.MeterCost);
        if (move.CyclesVisualVariantOnStart)
        {
            CyclePlayableVisualVariant(tick);
        }
        CurrentMove = move;
        CurrentMoveFrame = 0;
        CurrentMoveInstanceId = ++_moveInstanceCounter;
        CurrentMoveStartedTick = tick;
        CurrentMoveConnected = false;
        if (!Mathf.IsZeroApprox(move.InitialVelocityY))
        {
            Velocity = new Vector3(Velocity.X, move.InitialVelocityY, Velocity.Z);
        }
        if (move.InvulnerableStartupFrames > 0)
        {
            IsVulnerable = false;
        }

        State = CombatActorState.Attacking;

        if (move.IsCinematicSuper)
        {
            QueueEvent(CombatPresentationEventType.CinematicSuperStarted, tick, PayloadForMove(move));
        }
        else if (move.IsSuper)
        {
            QueueEvent(CombatPresentationEventType.SuperStarted, tick, PayloadForMove(move));
        }
        else
        {
            QueueEvent(CombatPresentationEventType.MoveStarted, tick, PayloadForMove(move));
        }

        CpuBrain?.NotifyMoveStarted(move, tick);
        ArcadeBrain?.NotifyMoveStarted(move, tick);

        return true;
    }

    public void EndCurrentMove()
    {
        if (CurrentMove?.InvulnerableStartupFrames > 0)
        {
            IsVulnerable = true;
        }

        CurrentMove = null;
        CurrentMoveFrame = 0;
        CurrentMoveStartedTick = -1;
        CurrentMoveConnected = false;
        State = IsFlightActive
            ? CombatActorState.Flying
            : SimPosition.Y > 0.0f
                ? CombatActorState.Jumping
                : CombatActorState.Idle;
    }

    public void ClearCurrentMove()
    {
        if (CurrentMove?.InvulnerableStartupFrames > 0)
        {
            IsVulnerable = true;
        }

        CurrentMove = null;
        CurrentMoveFrame = 0;
        CurrentMoveStartedTick = -1;
        CurrentMoveConnected = false;
    }

    public void SetForm(CharacterData form, bool resetHealth = false)
    {
        CurrentForm = form;
        _playableVisualVariantIndex = 0;
        CurrentBossPhaseIndex = IsBoss && form.BossPhases.Count > 0 ? 0 : -1;
        ApplyCurrentBossPhaseSettings();
        CpuBrain = !IsPlayerControlled && form.CpuProfile is not null
            ? new CpuFighterBrain(this, form.CpuProfile)
            : null;
        ArcadeBrain = !IsPlayerControlled && !IsBoss && form.ArcadeEnemyProfile is not null
            ? new ArcadeEnemyBrain(this, form.ArcadeEnemyProfile)
            : null;
        if (resetHealth)
        {
            Health = form.MaxHealth;
        }
        else
        {
            Health = Mathf.Clamp(Health, 1, form.MaxHealth);
        }

        GuardGauge = Mathf.Clamp(GuardGauge, 0, form.MaxGuardGauge);
    }

    /// <summary>
    /// When a training-dummy behavior is attached, resolves this actor's input
    /// for the tick from that behavior and returns the backing buffer so the
    /// dummy flows through the normal player state-machine path. Returns
    /// <c>null</c> for actors without a training behavior (they fall back to
    /// their CPU/arcade brains).
    /// </summary>
    public PlayerInputBuffer? ResolveTrainingInput(int tick)
    {
        if (TrainingBrain is null)
        {
            return null;
        }

        var grounded = SimPosition.Y <= 0.001f;
        var mask = TrainingBrain.Advance(tick, State, grounded);
        _trainingInputBuffer.PushFrame(tick, mask);
        return _trainingInputBuffer;
    }

    /// <summary>
    /// Folds this actor's simulation-relevant state into a deterministic
    /// fingerprint used to verify exact replay reproduction. Deliberately
    /// excludes <see cref="ActorId"/> (a non-deterministic GUID) and any
    /// presentation-only fields; identity is folded via the stable
    /// <see cref="PlayerId"/>/<see cref="TeamId"/>/<see cref="IsBoss"/> keys.
    /// </summary>
    public ulong ComputeStateFingerprint(ulong hash)
    {
        hash = ReplayFingerprint.CombineInt(hash, PlayerId);
        hash = ReplayFingerprint.CombineInt(hash, TeamId);
        hash = ReplayFingerprint.CombineBool(hash, IsBoss);
        hash = ReplayFingerprint.CombineInt(hash, (int)State);
        hash = ReplayFingerprint.CombineBool(hash, FacingRight);
        hash = ReplayFingerprint.CombineInt(hash, Health);
        hash = ReplayFingerprint.CombineInt(hash, Meter);
        hash = ReplayFingerprint.CombineInt(hash, GuardGauge);
        hash = ReplayFingerprint.CombineInt(hash, InvincibilityFrames);
        hash = ReplayFingerprint.CombineInt(hash, RemainingJumps);
        hash = ReplayFingerprint.CombineFloat(hash, SimPosition.X);
        hash = ReplayFingerprint.CombineFloat(hash, SimPosition.Y);
        hash = ReplayFingerprint.CombineFloat(hash, SimPosition.Z);
        hash = ReplayFingerprint.CombineFloat(hash, Velocity.X);
        hash = ReplayFingerprint.CombineFloat(hash, Velocity.Y);
        hash = ReplayFingerprint.CombineFloat(hash, Velocity.Z);
        hash = ReplayFingerprint.CombineString(hash, CurrentMove?.Id ?? "");
        hash = ReplayFingerprint.CombineInt(hash, CurrentMoveFrame);
        hash = ReplayFingerprint.CombineInt(hash, CurrentBossPhaseIndex);
        return hash;
    }

    /// <summary>Captures this actor's authoritative state for rollback/replay tooling.</summary>
    public ActorStateSnapshot CaptureSnapshot()
    {
        return new ActorStateSnapshot(
            ActorId,
            Health,
            Meter,
            GuardGauge,
            State,
            FacingRight,
            SimPosition,
            Velocity,
            CurrentMove?.Id ?? "",
            CurrentMoveFrame,
            ComboHitCount,
            CurrentBossPhaseIndex,
            InvincibilityFrames,
            RemainingJumps,
            CpuBrain?.CaptureRandomState() ?? 0u);
    }

    /// <summary>Restores this actor's authoritative state from a snapshot.</summary>
    public void RestoreSnapshot(ActorStateSnapshot snapshot)
    {
        Health = snapshot.Health;
        Meter = snapshot.Meter;
        GuardGauge = snapshot.GuardGauge;
        State = snapshot.State;
        FacingRight = snapshot.FacingRight;
        SimPosition = snapshot.SimPosition;
        Velocity = snapshot.Velocity;
        CurrentMove = string.IsNullOrEmpty(snapshot.CurrentMoveId)
            ? null
            : CurrentForm.Moves.FirstOrDefault(move => move.Id == snapshot.CurrentMoveId);
        CurrentMoveFrame = snapshot.CurrentMoveFrame;
        ComboHitCount = snapshot.ComboHitCount;
        CurrentBossPhaseIndex = snapshot.BossPhaseIndex;
        InvincibilityFrames = snapshot.InvincibilityFrames;
        RemainingJumps = snapshot.RemainingJumps;
        CpuBrain?.RestoreRandomState(snapshot.CpuRandomState);
    }

    private void CyclePlayableVisualVariant(int tick)
    {
        if (IsBoss || CurrentForm.VisualVariants.Count <= 1)
        {
            return;
        }

        _playableVisualVariantIndex =
            (_playableVisualVariantIndex + 1) % CurrentForm.VisualVariants.Count;
        QueueEvent(
            CombatPresentationEventType.FormSwapCompleted,
            tick,
            CurrentVisualVariantId);
    }

    public void RefreshCombatBoxes(int tick)
    {
        _activeBoxes.Clear();

        if (State == CombatActorState.Dead)
        {
            return;
        }

        var crouching = CurrentMove?.Posture == MovePosture.Crouching;
        var hurtbox = crouching ? CurrentForm.CrouchingHurtbox : CurrentForm.Hurtbox;
        var pushbox = crouching ? CurrentForm.CrouchingPushbox : CurrentForm.Pushbox;
        _activeBoxes.Add(new CombatBox(this, hurtbox, "body", 0, tick));
        _activeBoxes.Add(new CombatBox(this, pushbox, "body", 0, tick));

        if (CurrentMove is null)
        {
            return;
        }

        foreach (var definition in CurrentMove.GetBoxesForFrame(CurrentMoveFrame))
        {
            _activeBoxes.Add(new CombatBox(this, definition, CurrentMove.Id, CurrentMoveInstanceId, tick));
        }
    }

    public bool ApplyHit(HitApplication hit, int tick)
    {
        if (State == CombatActorState.Dead || !IsVulnerable || InvincibilityFrames > 0)
        {
            return false;
        }

        var defensePercent = 100 - DefenseModifierPercent;
        var finalDamage = Mathf.RoundToInt(hit.Damage * (defensePercent / 100f));
        
        Health = Mathf.Max(0, Health - finalDamage);
        AddMeter(GameConstants.MeterGainOnDamageTaken);
        Velocity = hit.LaunchVelocity;

        if (Health <= 0)
        {
            var session = ProjectMannequin.Progression.RunSessionManager.Instance;
            if (IsPlayerControlled && session.RemainingLives > 0)
            {
                session.LoseLife();
                Health = CurrentForm.MaxHealth;
                InvincibilityFrames = 180; // 3 seconds of I-frames
                QueueEvent(CombatPresentationEventType.PlayerLifeLost, tick, $"{hit.Move.Id}|{hit.Move.DisplayName}");
            }
            else
            {
                if (IsPlayerControlled)
                {
                    QueueEvent(CombatPresentationEventType.GameOver, tick, payload: $"{hit.Move.Id}|{hit.Move.DisplayName}");
                }
                StateMachine.EnterDeath(tick);
            }
            return true;
        }

        if (TryAdvanceBossPhase(tick))
        {
            return true;
        }

        PendingGroundBounce = hit.Move.CausesGroundBounce;
        PendingWallBounce = hit.Move.CausesWallBounce;
        PendingKnockdownFrames = HitResolution.ResolveKnockdownFrames(
            hit.Move.CausesHardKnockdown,
            hit.Move.KnockdownFrames);
        StateMachine.EnterHitstun(hit.HitstunFrames, tick);
        return true;
    }

    public bool CanBlockAttack(CombatActor attacker, MoveData move)
    {
        if (move.Unblockable
            || move.AttackHeight == AttackHeight.Throw
            || SimPosition.Y > 0.001f
            || State is not (CombatActorState.Blocking or CombatActorState.Blockstun))
        {
            return false;
        }

        var attackerIsRight = attacker.SimPosition.X >= SimPosition.X;
        if (FacingRight != attackerIsRight)
        {
            return false;
        }

        return move.AttackHeight switch
        {
            AttackHeight.Low => StateMachine.CurrentBlockPosture == BlockPosture.Crouching,
            AttackHeight.Overhead => StateMachine.CurrentBlockPosture == BlockPosture.Standing,
            _ => true,
        };
    }

    public bool ApplyBlockedHit(HitApplication hit, int tick)
    {
        if (State == CombatActorState.Dead
            || !IsVulnerable
            || !CanBlockAttack(hit.Attacker, hit.Move))
        {
            return false;
        }

        LastAttacker = hit.Attacker;
        var pushVelocity = new Vector3(
            hit.LaunchVelocity.X * 0.32f,
            0.0f,
            hit.LaunchVelocity.Z * 0.32f);
        Velocity = pushVelocity;
        AddMeter(Mathf.Max(1, GameConstants.MeterGainOnDamageTaken / 2));
        if (CurrentForm.MaxGuardGauge > 0)
        {
            var guardDamage = hit.Move.GuardDamage > 0
                ? hit.Move.GuardDamage
                : Mathf.Max(6, Mathf.CeilToInt(hit.Damage * 0.7f));
            GuardGauge = Mathf.Max(0, GuardGauge - guardDamage);
            _lastGuardDamageTick = tick;
            _guardRecoveryCarry = 0.0f;
            if (GuardGauge == 0)
            {
                StateMachine.EnterGuardBreak(CurrentForm.GuardBreakFrames, tick);
                return true;
            }
        }

        StateMachine.EnterBlockstun(hit.BlockstunFrames, tick);
        return true;
    }

    public void AddMeter(int amount)
    {
        var multiplier = 1.0f + (MeterGainModifierPercent / 100f);
        var finalAmount = Mathf.RoundToInt(amount * multiplier);
        Meter = Mathf.Min(CurrentForm.MaxMeter, Meter + finalAmount);
    }

    /// <summary>
    /// Debug/designer helper: forces the boss into its next phase by dropping
    /// health to the next phase's threshold and running the normal advance path.
    /// Returns false when there is no next phase. Reads and nudges simulation
    /// state only; not used by gameplay.
    /// </summary>
    public bool ForceNextBossPhase(int tick)
    {
        if (!IsBoss
            || CurrentBossPhaseIndex < 0
            || CurrentBossPhaseIndex + 1 >= CurrentForm.BossPhases.Count)
        {
            return false;
        }

        var nextThreshold = CurrentForm.BossPhases[CurrentBossPhaseIndex + 1].HealthThreshold;
        var targetHealth = Mathf.Max(1, Mathf.FloorToInt(nextThreshold * CurrentForm.MaxHealth));
        if (Health > targetHealth)
        {
            Health = targetHealth;
        }

        return TryAdvanceBossPhase(tick);
    }

    public void CollectPickup(CombatActor pickup)
    {
        if (IsDead) return;

        if (pickup.CurrentForm.Id == "pickup_meter")
        {
            AddMeter(200); // 2 bars
        }
        else if (pickup.CurrentForm.Id == "pickup_health")
        {
            RestoreHealth(Mathf.Max(1, CurrentForm.MaxHealth / 4));
        }
        else if (pickup.CurrentForm.Id == "pickup_score")
        {
            ProjectMannequin.Progression.RunSessionManager.Instance.ScoreManager.AwardPickupScore(1000);
        }

        pickup.Health = 0;
        pickup.State = CombatActorState.Dead;
    }

    public bool ApplyHazardDamage(int amount, int tick)
    {
        if (amount <= 0
            || State == CombatActorState.Dead
            || !IsVulnerable
            || InvincibilityFrames > 0)
        {
            return false;
        }

        Health = Mathf.Max(0, Health - amount);
        if (Health <= 0)
        {
            StateMachine.EnterDeath(tick);
        }

        return true;
    }

    /// <summary>
    /// True when an authored armor box is active this frame; outputs the armor
    /// box's chip scale. Armor lets the actor plow through a strike while taking
    /// chip, without changing its state.
    /// </summary>
    public bool HasActiveArmor(out float chipScale)
    {
        foreach (var box in _activeBoxes)
        {
            if (box.Definition.BoxType == CombatBoxType.ArmorBox)
            {
                chipScale = box.Definition.ArmorChipScale;
                return true;
            }
        }

        chipScale = 0.0f;
        return false;
    }

    /// <summary>
    /// Applies armor chip damage without inflicting hitstun. The actor keeps its
    /// current move/state; if chip is lethal it still dies.
    /// </summary>
    public bool ApplyArmorChip(int amount, int tick)
    {
        if (amount <= 0 || State == CombatActorState.Dead)
        {
            return false;
        }

        Health = Mathf.Max(0, Health - amount);
        if (Health <= 0)
        {
            StateMachine.EnterDeath(tick);
        }

        return true;
    }

    public int RestoreHealth(int amount)
    {
        if (amount <= 0 || IsDead)
        {
            return 0;
        }

        var previousHealth = Health;
        Health = Mathf.Min(CurrentForm.MaxHealth, Health + amount);
        return Health - previousHealth;
    }

    /// <summary>
    /// Restores only run-persistent resources at a stage boundary. The caller
    /// must invoke this on a freshly initialized actor; transient combat state
    /// (combo, velocity, guard, cooldowns) intentionally remains reset.
    /// </summary>
    public void HydrateRunResources(int health, int meter)
    {
        if (State == CombatActorState.Dead)
        {
            State = CombatActorState.Idle;
        }

        Health = Mathf.Clamp(health, 1, CurrentForm.MaxHealth);
        Meter = Mathf.Clamp(meter, 0, CurrentForm.MaxMeter);
    }

    public void Revive()
    {
        if (State == CombatActorState.Dead)
        {
            State = CombatActorState.Idle;
            Health = CurrentForm.MaxHealth;
        }
    }

    public void NotifySuccessfulHit(CombatActor target, int tick, bool targetWasComboVulnerable)
    {
        CurrentMoveConnected = true;
        var continuesCombo = _comboTargetActorId == target.ActorId
            && targetWasComboVulnerable
            && tick - _lastComboHitTick <= GameConstants.ComboDisplayFrames;
        ComboHitCount = continuesCombo ? ComboHitCount + 1 : 1;
        _comboTargetActorId = target.ActorId;
        _lastComboHitTick = tick;

        if (!continuesCombo)
        {
            _comboMoveUses.Clear();
            _comboStarterProration = CurrentMove?.ProrationScale ?? 1.0f;
        }

        var moveId = CurrentMove?.Id ?? "";
        _comboMoveUses[moveId] = (_comboMoveUses.TryGetValue(moveId, out var uses) ? uses : 0) + 1;

        if (ComboHitCount >= 2)
        {
            QueueEvent(
                CombatPresentationEventType.ComboUpdated,
                tick,
                ComboHitCount.ToString(),
                target.ActorId);
        }

        StateMachine.RestoreFlightTime(CurrentMove?.FlightTimeRestoreFrames ?? 0);
    }

    public void UpdateComboState(int tick)
    {
        if (ComboHitCount == 0 || tick - _lastComboHitTick <= GameConstants.ComboDisplayFrames)
        {
            return;
        }

        ComboHitCount = 0;
        _comboTargetActorId = "";
        _comboMoveUses.Clear();
        _comboStarterProration = 1.0f;
    }

    public float GetNextComboDamageScale(
        CombatActor target,
        int tick,
        bool targetWasComboVulnerable,
        MoveData move)
    {
        var continuesCombo = _comboTargetActorId == target.ActorId
            && targetWasComboVulnerable
            && tick - _lastComboHitTick <= GameConstants.ComboDisplayFrames;
        var nextHit = continuesCombo ? ComboHitCount + 1 : 1;
        var starterProration = continuesCombo ? _comboStarterProration : move.ProrationScale;
        var repeatUses = continuesCombo && _comboMoveUses.TryGetValue(move.Id, out var uses) ? uses : 0;
        return ComboRules.ResolveDamageScale(
            nextHit,
            move.MinimumDamageScale,
            move.IsSuper,
            starterProration,
            repeatUses);
    }

    public void UpdateTimers(int tick)
    {
        if (InvincibilityFrames > 0)
        {
            InvincibilityFrames--;
        }

        ApplyCorruptionDrain(tick);

        AssistSystem.Update(tick, _pendingEvents);
    }

    private void ApplyCorruptionDrain(int tick)
    {
        if (!IsPlayerControlled || State == CombatActorState.Dead || tick % GameConstants.TickRate != 0)
        {
            return;
        }

        var session = ProjectMannequin.Progression.RunSessionManager.Instance;
        if (session == null)
        {
            return;
        }

        var drain = 0;
        foreach (var artId in session.ActiveArtifacts)
        {
            var artifact = ProjectMannequin.Progression.ArtifactCatalog.GetArtifact(artId);
            if (artifact is { IsCursed: true })
            {
                drain += artifact.HealthDrainPerSecond;
            }
        }

        if (drain <= 0)
        {
            return;
        }

        Health = Mathf.Max(1, Health - drain);
        QueueEvent(CombatPresentationEventType.CorruptionDrain, tick, drain.ToString());
    }

    public void UpdateGuardGauge(int tick)
    {
        if (CurrentForm.MaxGuardGauge <= 0
            || GuardGauge >= CurrentForm.MaxGuardGauge
            || State == CombatActorState.GuardBreak
            || tick - _lastGuardDamageTick < CurrentForm.GuardRecoveryDelayFrames)
        {
            return;
        }

        _guardRecoveryCarry += CurrentForm.GuardRecoveryPerSecond / GameConstants.TickRate;
        var recovery = Mathf.FloorToInt(_guardRecoveryCarry);
        if (recovery <= 0)
        {
            return;
        }

        _guardRecoveryCarry -= recovery;
        GuardGauge = Mathf.Min(CurrentForm.MaxGuardGauge, GuardGauge + recovery);
    }

    public void RestoreGuardGaugeToFull()
    {
        GuardGauge = CurrentForm.MaxGuardGauge;
        _guardRecoveryCarry = 0.0f;
    }

    public void IntegrateMotion(int tick)
    {
        var delta = 1.0f / GameConstants.TickRate;
        var wasAirborne = SimPosition.Y > 0.001f || Velocity.Y > 0.001f;
        SimPosition += Velocity * delta;

        var hovering = HoverHeight > 0.001f
            && State is not (CombatActorState.Hitstun or CombatActorState.Knockdown or CombatActorState.Dead);
        if (hovering)
        {
            SimPosition = new Vector3(SimPosition.X, HoverHeight, SimPosition.Z);
            Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
        }
        else if (!IsFlightActive && (SimPosition.Y > 0.0f || Velocity.Y > 0.0f))
        {
            var gravityScale = WallRunActive ? 0.25f : 1.0f;
            Velocity += Vector3.Down * CurrentForm.Gravity * gravityScale * delta;
        }

        if (IsFlightActive && CurrentForm.FlightProfile is { } flight)
        {
            SimPosition = new Vector3(
                SimPosition.X,
                Mathf.Clamp(SimPosition.Y, flight.MinimumHeight, flight.MaximumHeight),
                SimPosition.Z);
        }

        if (SimPosition.Y <= 0.0f)
        {
            var landed = wasAirborne && Velocity.Y < 0.0f;
            var fallSpeed = Mathf.Abs(Velocity.Y);
            SimPosition = new Vector3(SimPosition.X, 0.0f, SimPosition.Z);
            if (Velocity.Y < 0.0f)
            {
                Velocity = new Vector3(Velocity.X, 0.0f, Velocity.Z);
            }

            if (landed)
            {
                if (State == CombatActorState.Hitstun)
                {
                    if (PendingGroundBounce)
                    {
                        PendingGroundBounce = false;
                        var bounceY = HitResolution.ResolveGroundBounceVelocity(fallSpeed);
                        Velocity = new Vector3(Velocity.X * 0.5f, bounceY, Velocity.Z);
                        SimPosition = new Vector3(SimPosition.X, 0.02f, SimPosition.Z);
                        QueueEvent(CombatPresentationEventType.GroundBounce, tick);
                    }
                    else
                    {
                        StateMachine.EnterKnockdown(tick, PendingKnockdownFrames);
                    }
                }
                else if (State is CombatActorState.Jumping or CombatActorState.Attacking)
                {
                    StateMachine.EnterLanding(tick);
                }
            }
        }
    }

    public void UpdatePresentationTransform()
    {
        GlobalPosition = SimPosition;
    }

    public MoveData? GetAiRequestedMove(int tick, IReadOnlyList<CombatActor> actors)
    {
        if (!IsAiEnabled || IsPlayerControlled || State is not (CombatActorState.Idle or CombatActorState.Walking or CombatActorState.Dashing))
        {
            return null;
        }

        var target = actors
            .Where(actor => actor.TeamId != TeamId && !actor.IsDead
                && !AreDuelIsolated(this, actor))
            .OrderBy(actor => actor.SimPosition.DistanceSquaredTo(SimPosition))
            .FirstOrDefault();

        if (target is null)
        {
            return null;
        }

        FacingRight = target.SimPosition.X >= SimPosition.X;

        var phase = CurrentBossPhase;
        var attackRange = phase?.AttackRange ?? 2.1f;
        var distanceX = Mathf.Abs(target.SimPosition.X - SimPosition.X);
        var distanceZ = Mathf.Abs(target.SimPosition.Z - SimPosition.Z);
        if (distanceX > attackRange || distanceZ > 1.3f)
        {
            var direction = (target.SimPosition - SimPosition).Normalized();
            var speedMultiplier = phase?.MovementSpeedMultiplier ?? 1.0f;
            Velocity = new Vector3(
                direction.X * CurrentForm.WalkSpeed * 0.55f * speedMultiplier,
                Velocity.Y,
                direction.Z * CurrentForm.WalkSpeed * 0.35f * speedMultiplier);
            State = CombatActorState.Walking;
            return null;
        }

        Velocity = new Vector3(0.0f, Velocity.Y, 0.0f);
        State = CombatActorState.Idle;
        if (_nextAiMoveTick < 0)
        {
            _nextAiMoveTick = tick + Mathf.Max(0, AiInitialDelayFrames);
        }

        if (tick < _nextAiMoveTick)
        {
            return null;
        }

        var movePool = phase is { EnabledMoveIds.Count: > 0 }
            ? phase.EnabledMoveIds
                .Select(CurrentForm.FindMove)
                .Where(move => move is not null)
                .Cast<MoveData>()
                .ToArray()
            : CurrentForm.Moves.ToArray();
        var move = movePool.Length > 0
            ? movePool[_aiMoveCursor++ % movePool.Length]
            : null;
        if (move is not null)
        {
            var interval = phase?.AttackIntervalFrames ?? AiAttackIntervalFrames;
            _nextAiMoveTick = tick + Mathf.Max(30, interval);
        }

        return move;
    }

    private bool TryAdvanceBossPhase(int tick)
    {
        if (!IsBoss || CurrentForm.BossPhases.Count == 0 || CurrentBossPhaseIndex < 0)
        {
            return false;
        }

        var healthRatio = (float)Health / Mathf.Max(1, CurrentForm.MaxHealth);
        var nextPhaseIndex = CurrentBossPhaseIndex;
        while (nextPhaseIndex + 1 < CurrentForm.BossPhases.Count
               && healthRatio <= CurrentForm.BossPhases[nextPhaseIndex + 1].HealthThreshold)
        {
            nextPhaseIndex++;
        }

        if (nextPhaseIndex == CurrentBossPhaseIndex)
        {
            return false;
        }

        CurrentBossPhaseIndex = nextPhaseIndex;
        _aiMoveCursor = 0;
        ApplyCurrentBossPhaseSettings();
        var phase = CurrentBossPhase!;
        AddMeter(phase.MeterGrant);
        RestoreGuardGaugeToFull();
        var neutralUntil = AiModules.ResolvePhaseNeutralUntil(
            tick,
            phase.TransitionFrames,
            phase.PhaseBurstRecoveryFrames);
        _nextAiMoveTick = neutralUntil;
        CpuBrain?.ResetDecisionState(neutralUntil);
        StateMachine.EnterCinematicLock(
            phase.TransitionFrames,
            tick,
            $"{CurrentBossPhaseIndex + 1}|{phase.DisplayName}");
        if (OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_SMOKE_TEST") == "1"
            || OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1")
        {
            var logPrefix = OS.GetEnvironment("PROJECT_MANNEQUIN_BOSS_AI_FIGHT_TEST") == "1"
                ? "[BossAiFight]"
                : "[BossSmoke]";
            GD.Print(
                $"{logPrefix} Phase {CurrentBossPhaseIndex + 1} ({phase.Id}) at {Health}/{CurrentForm.MaxHealth} HP, tick {tick}.");
        }

        return true;
    }

    private void ApplyCurrentBossPhaseSettings()
    {
        StateMachine?.ApplyPhaseMechanics(CurrentBossPhase);
        if (CurrentBossPhase is null)
        {
            return;
        }

        AiAttackIntervalFrames = CurrentBossPhase.AttackIntervalFrames;
    }

    public IReadOnlyList<CombatPresentationEvent> DrainEvents()
    {
        var drained = _pendingEvents.ToArray();
        _pendingEvents.Clear();
        return drained;
    }

    internal void QueueEvent(CombatPresentationEventType type, int tick, string payload = "", string targetActorId = "")
    {
        _pendingEvents.Add(new CombatPresentationEvent(type, tick, ActorId, targetActorId, payload));
    }

    private static string PayloadForMove(MoveData move)
    {
        return string.IsNullOrWhiteSpace(move.DisplayName) ? move.Id : move.DisplayName;
    }

    private CharacterVisualComponent? FindVisualComponent()
    {
        foreach (var child in GetChildren())
        {
            if (child is CharacterVisualComponent visualComponent)
            {
                return visualComponent;
            }
        }

        return null;
    }
}
