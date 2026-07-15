using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Combat;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Progression;

/// <summary>
/// Manages assist calls — spawns a temporary "ghost" actor that performs
/// one assist attack and then despawns. Each form in the loadout can be
/// called as an assist with a per-slot cooldown.
/// </summary>
public sealed class AssistCallSystem
{
    private readonly Dictionary<string, int> _cooldowns = new();
    private readonly List<ActiveAssist> _activeAssists = new();

    public IReadOnlyList<ActiveAssist> ActiveAssists => _activeAssists;

    /// <summary>
    /// Try to call an assist for a given form. Returns true if the call was accepted.
    /// </summary>
    public bool TryCallAssist(
        CombatActor caller,
        AssistCallData assistData,
        AssistDirection direction,
        int tick)
    {
        if (_cooldowns.TryGetValue(assistData.FormId, out var cooldownEnd) && tick < cooldownEnd)
        {
            return false;
        }

        var move = direction switch
        {
            AssistDirection.Forward => assistData.ForwardAssist,
            AssistDirection.Down => assistData.DownAssist,
            _ => assistData.NeutralAssist,
        };

        var assist = new ActiveAssist
        {
            FormId = assistData.FormId,
            FormDisplayName = assistData.FormDisplayName,
            CallerActorId = caller.ActorId,
            CallerTeamId = caller.TeamId,
            SpawnPosition = caller.SimPosition + new Vector3(caller.FacingRight ? 1.5f : -1.5f, 0, 0),
            FacingRight = caller.FacingRight,
            Move = move,
            StartTick = tick,
            DurationFrames = GameConstants.AssistActiveDuration,
            StartupFrames = GameConstants.AssistStartupFrames,
        };

        _activeAssists.Add(assist);
        _cooldowns[assistData.FormId] = tick + GameConstants.AssistCooldownFrames;

        return true;
    }

    /// <summary>
    /// Called every simulation tick to update active assists.
    /// Returns presentation events for assists that started or completed.
    /// </summary>
    public void Update(int tick, IList<CombatPresentationEvent> events)
    {
        for (var i = _activeAssists.Count - 1; i >= 0; i--)
        {
            var assist = _activeAssists[i];
            var elapsed = tick - assist.StartTick;

            if (elapsed >= assist.DurationFrames)
            {
                events.Add(new CombatPresentationEvent(
                    CombatPresentationEventType.AssistCallCompleted,
                    tick,
                    assist.CallerActorId,
                    Payload: assist.FormDisplayName));
                _activeAssists.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Resolves each active assist's synthesized strike hitbox against enemy
    /// actors during its active window. The ghost carries no CombatBoxes, so a
    /// fixed AABB (see <see cref="GameConstants"/> AssistHitbox*) is projected in
    /// front of it. Mirrors the projectile pipeline — instinct-evade → parry →
    /// block → hit, once per target per assist — and returns hit-stop frames.
    /// </summary>
    public int ResolveHits(
        CombatActor caller,
        IReadOnlyList<CombatActor> actors,
        int tick,
        IList<CombatPresentationEvent> events)
    {
        var hitStopFrames = 0;
        foreach (var assist in _activeAssists)
        {
            if (!assist.IsInActiveWindow(tick))
            {
                continue;
            }

            var move = assist.Move;
            var facingSign = assist.FacingRight ? 1.0f : -1.0f;
            var center = assist.SpawnPosition + new Vector3(
                facingSign * GameConstants.AssistHitboxForwardOffset,
                GameConstants.AssistHitboxCenterY,
                0.0f);
            var halfExtents = new Vector3(
                GameConstants.AssistHitboxHalfWidth,
                GameConstants.AssistHitboxHalfHeight,
                GameConstants.AssistHitboxHalfDepth);

            foreach (var target in actors)
            {
                if (target.TeamId == assist.CallerTeamId
                    || target.State == CombatActorState.Dead
                    || !target.IsVulnerable
                    || target.InvincibilityFrames > 0
                    || assist.HitTargets.Contains(target.ActorId))
                {
                    continue;
                }

                var overlaps = target.ActiveBoxes.Any(box =>
                    (box.Definition.BoxType is CombatBoxType.Hurtbox or CombatBoxType.WeakPointBox)
                    && AabbOverlap(center, halfExtents, box.Center, box.HalfExtents));
                if (!overlaps)
                {
                    continue;
                }

                if (target.StateMachine.TryResolveInstinctEvade(caller, move, tick))
                {
                    assist.HitTargets.Add(target.ActorId);
                    continue;
                }

                if (target.StateMachine.ResolveParry(caller, move, tick))
                {
                    assist.HitTargets.Add(target.ActorId);
                    hitStopFrames = Mathf.Max(hitStopFrames, Mathf.Max(6, move.HitStopFrames + 2));
                    continue;
                }

                var targetWasComboVulnerable = target.State is CombatActorState.Hitstun
                        or CombatActorState.Knockdown
                    || target.SimPosition.Y > 0.001f;
                var damageScale = caller.GetNextComboDamageScale(target, tick, targetWasComboVulnerable, move);
                var phaseDamageScale = caller.CurrentBossPhase?.DamageMultiplier ?? 1.0f;
                var damage = Mathf.Max(1, Mathf.RoundToInt(move.Damage * damageScale * phaseDamageScale));
                var launchVelocity = new Vector3(
                    (move.LaunchX > 0.0f ? move.LaunchX : move.PushbackX) * facingSign,
                    move.LaunchY,
                    0.0f);
                var hit = new HitApplication(
                    caller,
                    target,
                    move,
                    damage,
                    move.HitstunFrames,
                    move.BlockstunFrames,
                    launchVelocity,
                    move.IsLauncher);

                if (target.CanBlockAttack(caller, move) && target.ApplyBlockedHit(hit, tick))
                {
                    assist.HitTargets.Add(target.ActorId);
                    caller.AddMeter(Mathf.Max(1, move.MeterGain / 3));
                    events.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.Blocked,
                        tick,
                        caller.ActorId,
                        target.ActorId,
                        $"{move.Id}|0"));
                    hitStopFrames = Mathf.Max(hitStopFrames, Mathf.Max(2, move.HitStopFrames - 2));
                    continue;
                }

                if (!target.ApplyHit(hit, tick))
                {
                    continue;
                }

                assist.HitTargets.Add(target.ActorId);
                caller.AddMeter(move.MeterGain);
                caller.NotifySuccessfulHit(target, tick, targetWasComboVulnerable);
                events.Add(new CombatPresentationEvent(
                    move.IsLauncher
                        ? CombatPresentationEventType.LauncherHit
                        : CombatPresentationEventType.HitConnected,
                    tick,
                    caller.ActorId,
                    target.ActorId,
                    $"{move.Id}|{damage}"));
                hitStopFrames = Mathf.Max(hitStopFrames, move.HitStopFrames);
            }
        }

        return hitStopFrames;
    }

    private static bool AabbOverlap(Vector3 centerA, Vector3 halfA, Vector3 centerB, Vector3 halfB)
    {
        return Mathf.Abs(centerA.X - centerB.X) <= halfA.X + halfB.X
            && Mathf.Abs(centerA.Y - centerB.Y) <= halfA.Y + halfB.Y
            && Mathf.Abs(centerA.Z - centerB.Z) <= halfA.Z + halfB.Z;
    }

    /// <summary>
    /// Returns the remaining cooldown frames for a given form, or 0 if ready.
    /// </summary>
    public int GetCooldownRemaining(string formId, int currentTick)
    {
        if (_cooldowns.TryGetValue(formId, out var cooldownEnd))
        {
            return Mathf.Max(0, cooldownEnd - currentTick);
        }
        return 0;
    }

    public void Reset()
    {
        _cooldowns.Clear();
        _activeAssists.Clear();
    }
}

public sealed class ActiveAssist
{
    public string FormId { get; set; } = "";
    public string FormDisplayName { get; set; } = "";
    public string CallerActorId { get; set; } = "";
    public int CallerTeamId { get; set; }
    public Vector3 SpawnPosition { get; set; }
    public bool FacingRight { get; set; }
    public MoveData Move { get; set; } = new();
    public int StartTick { get; set; }
    public int DurationFrames { get; set; }
    public int StartupFrames { get; set; }

    /// <summary>Targets already struck by this assist (one hit per target).</summary>
    public HashSet<string> HitTargets { get; } = new();

    /// <summary>
    /// True during the assist's damaging window: after startup, for the move's
    /// active frames. The ghost lingers (visually) until <see cref="DurationFrames"/>.
    /// </summary>
    public bool IsInActiveWindow(int tick)
    {
        var elapsed = tick - StartTick;
        return elapsed >= StartupFrames
            && elapsed < StartupFrames + Mathf.Max(1, Move.ActiveFrames);
    }
}
