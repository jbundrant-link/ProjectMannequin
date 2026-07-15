using System.Collections.Generic;
using System.Linq;
using Godot;
using ProjectMannequin.Core;
using ProjectMannequin.Data;

namespace ProjectMannequin.Combat;

public sealed class HitResolver
{
    private readonly HashSet<string> _resolvedHitKeys = new();

    /// <summary>
    /// Resolves all attack/target box overlaps for the tick. Per target, the
    /// defensive precedence follows <see cref="HitResolution.CanonicalOrder"/>:
    /// dead/invulnerable, instinct-evade, parry (Reflect Guard), block, then hit
    /// (which may be a counter-hit or punish-counter by timing). Throws bypass
    /// block/parry upstream via <c>AttackHeight.Throw</c>. Returns the hit-stop
    /// frames to apply this tick.
    /// </summary>
    public int Resolve(
        IReadOnlyList<CombatActor> actors,
        int tick,
        float cameraLeftX,
        float cameraRightX,
        IList<CombatPresentationEvent> presentationEvents)
    {
        var hitStopFrames = 0;
        var attackBoxes = actors
            .SelectMany(actor => actor.ActiveBoxes)
            .Where(box => box.Definition.BoxType is CombatBoxType.Hitbox or CombatBoxType.ProjectileBox or CombatBoxType.Grabbox)
            .ToArray();

        var targetBoxes = actors
            .SelectMany(actor => actor.ActiveBoxes)
            .Where(box => box.Definition.BoxType is CombatBoxType.Hurtbox or CombatBoxType.WeakPointBox)
            .ToArray();

        var projectileBoxes = actors
            .SelectMany(actor => actor.ActiveBoxes)
            .Where(box => box.Definition.BoxType == CombatBoxType.ProjectileBox)
            .ToArray();

        for (var i = 0; i < projectileBoxes.Length; i++)
        {
            for (var j = i + 1; j < projectileBoxes.Length; j++)
            {
                var projA = projectileBoxes[i];
                var projB = projectileBoxes[j];
                
                if (CanHit(projA, projB) && projA.Overlaps(projB))
                {
                    projA.Owner.ApplyHit(new HitApplication(projB.Owner, projA.Owner, projB.Owner.CurrentMove ?? new MoveData(), 9999, 0, 0, Vector3.Zero, false), tick);
                    projB.Owner.ApplyHit(new HitApplication(projA.Owner, projB.Owner, projA.Owner.CurrentMove ?? new MoveData(), 9999, 0, 0, Vector3.Zero, false), tick);
                    hitStopFrames = Mathf.Max(hitStopFrames, 4);
                }
            }
        }

        var bowlingPins = actors.Where(a => 
            !a.IsBoss 
            && !a.IsDead 
            && (a.State == CombatActorState.Hitstun || a.State == CombatActorState.Knockdown) 
            && a.Velocity.LengthSquared() > GameConstants.BowlingPinVelocityThreshold).ToArray();

        foreach (var pin in bowlingPins)
        {
            var pinHurtbox = pin.ActiveBoxes.FirstOrDefault(b => b.Definition.BoxType == CombatBoxType.Hurtbox);
            if (pinHurtbox is null) continue;

            foreach (var targetBox in targetBoxes)
            {
                if (targetBox.Owner == pin || targetBox.Owner.TeamId != pin.TeamId || targetBox.Owner.IsDead)
                    continue;

                if (pinHurtbox.Overlaps(targetBox))
                {
                    var duplicateKey = $"bowling_pin_{pin.ActorId}_{targetBox.Owner.ActorId}";
                    if (!_resolvedHitKeys.Add(duplicateKey))
                    {
                        continue;
                    }

                    var velocityInheritance = pin.Velocity * 0.4f;
                    var hit = new HitApplication(
                        pin,
                        targetBox.Owner,
                        new MoveData { Id = "bowling_pin_collision", Damage = GameConstants.BowlingPinCollateralDamage, IsLauncher = true },
                        GameConstants.BowlingPinCollateralDamage,
                        30,
                        10,
                        velocityInheritance,
                        true);

                    targetBox.Owner.ApplyHit(hit, tick);
                    pin.Velocity *= 0.6f;
                    hitStopFrames = Mathf.Max(hitStopFrames, 8);
                    
                    if (pin.CurrentForm.RoleTags.Contains("hazard"))
                    {
                        pin.ApplyHit(new HitApplication(targetBox.Owner, pin, new MoveData(), 50, 0, 0, Vector3.Zero, false), tick);
                    }
                }
            }
        }

        foreach (var attackBox in attackBoxes)
        {
            foreach (var targetBox in targetBoxes)
            {
                if (!CanHit(attackBox, targetBox))
                {
                    continue;
                }

                if (!attackBox.Overlaps(targetBox))
                {
                    continue;
                }

                var move = attackBox.Owner.CurrentMove;
                if (move is null)
                {
                    continue;
                }

                var duplicateKey = BuildDuplicateKey(attackBox, targetBox);
                if (!move.AllowDuplicateHits && !attackBox.Definition.AllowMultipleHits && !_resolvedHitKeys.Add(duplicateKey))
                {
                    continue;
                }

                var damage = attackBox.Definition.DamageOverride >= 0
                    ? attackBox.Definition.DamageOverride
                    : move.Damage;

                if (targetBox.Owner.StateMachine.TryResolveInstinctEvade(
                        attackBox.Owner,
                        move,
                        tick))
                {
                    continue;
                }

                if (targetBox.Owner.StateMachine.ResolveParry(attackBox.Owner, move, tick))
                {
                    hitStopFrames = Mathf.Max(
                        hitStopFrames,
                        Mathf.Max(6, move.HitStopFrames + 2));
                    continue;
                }

                var facingSign = attackBox.Owner.FacingRight ? 1.0f : -1.0f;
                var launchVelocity = new Vector3(
                    (move.LaunchX > 0.0f ? move.LaunchX : move.PushbackX) * facingSign,
                    move.LaunchY,
                    0.0f);

                var isTargetHazard = targetBox.Owner.CurrentForm.RoleTags.Contains("hazard");
                if (isTargetHazard && (move.IsLauncher || move.AttackHeight == AttackHeight.Throw))
                {
                    // Hazard Launching! Turn the hazard into a massive bowling pin for the attacker's team
                    targetBox.Owner.TeamId = attackBox.Owner.TeamId;
                    targetBox.Owner.Velocity = new Vector3(30.0f * facingSign, 4.0f, 0.0f);
                    targetBox.Owner.State = CombatActorState.Hitstun;
                    hitStopFrames = Mathf.Max(hitStopFrames, 12);
                    
                    presentationEvents.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.EnemyEntered,
                        tick,
                        targetBox.Owner.ActorId,
                        Payload: "HazardLaunched"));
                    continue; // Skip normal hit application
                }

                // Armor: an armored strike is absorbed to chip and the defender
                // keeps its current state. Throws bypass armor.
                if (move.AttackHeight != AttackHeight.Throw
                    && targetBox.Owner.HasActiveArmor(out var armorChipScale))
                {
                    var armorChip = HitResolution.ResolveArmorChip(damage, armorChipScale);
                    targetBox.Owner.ApplyArmorChip(armorChip, tick);
                    attackBox.Owner.AddMeter(Mathf.Max(1, move.MeterGain / 2));
                    hitStopFrames = Mathf.Max(hitStopFrames, move.HitStopFrames);
                    presentationEvents.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.ArmorAbsorbed,
                        tick,
                        targetBox.Owner.ActorId,
                        attackBox.Owner.ActorId,
                        $"{move.Id}|{armorChip}"));
                    continue;
                }

                var targetWasComboVulnerable = targetBox.Owner.State is CombatActorState.Hitstun
                    or CombatActorState.Knockdown
                    || targetBox.Owner.SimPosition.Y > 0.001f;
                var hitContext = ResolveHitContext(targetBox.Owner);
                var contextDamageScale = hitContext switch
                {
                    HitContext.Counter => 1.15f,
                    HitContext.PunishCounter => 1.25f,
                    _ => 1.0f,
                };
                var comboDamageScale = attackBox.Owner.GetNextComboDamageScale(
                    targetBox.Owner,
                    tick,
                    targetWasComboVulnerable,
                    move);
                var phaseDamageScale =
                    attackBox.Owner.CurrentBossPhase?.DamageMultiplier ?? 1.0f;
                var artifactDamageScale = 1.0f + (attackBox.Owner.DamageModifierPercent / 100f);
                damage = Mathf.Max(
                    1,
                    Mathf.RoundToInt(
                        damage
                        * contextDamageScale
                        * comboDamageScale
                        * phaseDamageScale
                        * artifactDamageScale));

                if (targetBox.Definition.BoxType == CombatBoxType.WeakPointBox)
                {
                    damage = HitResolution.ResolveWeakPointDamage(
                        damage,
                        targetBox.Definition.WeakPointDamageMultiplier);
                }

                var hitstunFrames = move.HitstunFrames + (hitContext switch
                {
                    HitContext.Counter => 4,
                    HitContext.PunishCounter => 6,
                    _ => 0,
                });

                var hit = new HitApplication(
                    attackBox.Owner,
                    targetBox.Owner,
                    move,
                    damage,
                    hitstunFrames,
                    move.BlockstunFrames,
                    launchVelocity,
                    move.IsLauncher);

                if (targetBox.Owner.CanBlockAttack(attackBox.Owner, move)
                    && targetBox.Owner.ApplyBlockedHit(hit, tick))
                {
                    attackBox.Owner.AddMeter(Mathf.Max(1, move.MeterGain / 3));
                    var blockStop = move.BlockStopFrames >= 0
                        ? move.BlockStopFrames
                        : Mathf.Max(2, move.HitStopFrames - 2);
                    hitStopFrames = Mathf.Max(hitStopFrames, blockStop);

                    // SF6 Style Corner Pushback
                    var isCornered = (targetBox.Owner.SimPosition.X <= cameraLeftX + 0.5f && launchVelocity.X < 0.0f)
                                  || (targetBox.Owner.SimPosition.X >= cameraRightX - 0.5f && launchVelocity.X > 0.0f);
                    if (isCornered)
                    {
                        targetBox.Owner.Velocity = new Vector3(0.0f, targetBox.Owner.Velocity.Y, targetBox.Owner.Velocity.Z);
                        attackBox.Owner.Velocity = new Vector3(-launchVelocity.X, attackBox.Owner.Velocity.Y, attackBox.Owner.Velocity.Z);
                    }

                    presentationEvents.Add(new CombatPresentationEvent(
                        CombatPresentationEventType.Blocked,
                        tick,
                        attackBox.Owner.ActorId,
                        targetBox.Owner.ActorId,
                        $"{move.Id}|0"));
                    continue;
                }

                if (!targetBox.Owner.ApplyHit(hit, tick))
                {
                    continue;
                }

                attackBox.Owner.AddMeter(move.MeterGain);
                if (targetBox.Owner.IsDead && targetBox.Owner.CurrentForm.RoleTags.Contains("hazard"))
                {
                    // Bonus meter for breaking a hazard
                    attackBox.Owner.AddMeter(150);
                }
                
                attackBox.Owner.NotifySuccessfulHit(targetBox.Owner, tick, targetWasComboVulnerable);
                hitStopFrames = Mathf.Max(
                    hitStopFrames,
                    move.HitStopFrames + (hitContext == HitContext.PunishCounter ? 2 : hitContext == HitContext.Counter ? 1 : 0));
                presentationEvents.Add(new CombatPresentationEvent(
                    ResolvePresentationEventType(move, hitContext),
                    tick,
                    attackBox.Owner.ActorId,
                    targetBox.Owner.ActorId,
                    $"{move.Id}|{damage}"));
            }
        }

        var pickups = actors.Where(a => a.CurrentForm.RoleTags.Contains("pickup") && !a.IsDead).ToArray();
        foreach (var pickup in pickups)
        {
            var pickupHurtbox = pickup.ActiveBoxes.FirstOrDefault(b => b.Definition.BoxType == CombatBoxType.Hurtbox);
            if (pickupHurtbox is null) continue;

            foreach (var actor in actors)
            {
                if (actor == pickup || actor.IsDead || actor.CurrentForm.RoleTags.Contains("pickup")) continue;

                var actorHurtbox = actor.ActiveBoxes.FirstOrDefault(b => b.Definition.BoxType == CombatBoxType.Hurtbox);
                if (actorHurtbox is null) continue;

                if (pickupHurtbox.Overlaps(actorHurtbox))
                {
                    actor.CollectPickup(pickup);
                    break;
                }
            }
        }

        return hitStopFrames;
    }

    private static HitContext ResolveHitContext(CombatActor target)
    {
        if (target.State != CombatActorState.Attacking || target.CurrentMove is null)
        {
            return HitContext.Normal;
        }

        return target.CurrentMoveFrame > target.CurrentMove.LastActiveFrame
            ? HitContext.PunishCounter
            : HitContext.Counter;
    }

    private static CombatPresentationEventType ResolvePresentationEventType(MoveData move, HitContext hitContext)
    {
        return hitContext switch
        {
            HitContext.PunishCounter => CombatPresentationEventType.PunishCounter,
            HitContext.Counter => CombatPresentationEventType.CounterHit,
            _ when move.IsLauncher => CombatPresentationEventType.LauncherHit,
            _ => CombatPresentationEventType.HitConnected,
        };
    }

    private static bool CanHit(CombatBox attackBox, CombatBox targetBox)
    {
        return attackBox.Owner != targetBox.Owner
            && attackBox.Owner.TeamId != targetBox.Owner.TeamId
            && !attackBox.Owner.IsDead
            && !targetBox.Owner.IsDead
            && !CombatActor.AreDuelIsolated(attackBox.Owner, targetBox.Owner);
    }

    private static string BuildDuplicateKey(CombatBox attackBox, CombatBox targetBox)
    {
        return $"{attackBox.Owner.ActorId}:{targetBox.Owner.ActorId}:{attackBox.MoveInstanceId}:{attackBox.Definition.Id}";
    }

    private enum HitContext
    {
        Normal,
        Counter,
        PunishCounter,
    }
}

public readonly record struct HitApplication(
    CombatActor Attacker,
    CombatActor Defender,
    Data.MoveData Move,
    int Damage,
    int HitstunFrames,
    int BlockstunFrames,
    Vector3 LaunchVelocity,
    bool IsLauncher);
