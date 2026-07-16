# Implementation Plan: DBFZ Boss Mechanics

Based on the Dragon Ball FighterZ mechanics, the following features will dramatically improve the Boss encounters by giving them powerful tools to break the player's momentum and force defensive play.

> [!IMPORTANT]
> **Historical source plan.** This proposal initiated the phase-burst, Dragon
> Rush, and Reflect work. It is retained as provenance and does not override
> [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md),
> [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md), or the audit in
> [ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md).

## Implementation Reconciliation - 2026-07-16

| Original proposal | Current result | Remaining work or resolved decision |
| --- | --- | --- |
| Sparking-style phase burst | **Implemented.** [PhaseBurst.cs](../Scripts/Combat/PhaseBurst.cs), phase-authored pushback/hitstun/buffs/bounds expansion, and presentation events are live. | It does not heal the boss; the implementation resolved toward an offensive get-off-me burst with temporary damage/defense buffs. Authored burst art/audio still belongs to spectacle polish. |
| Dragon Rush throw/launcher | **Implemented.** Goku owns `goku_dragon_rush`, throw-height collision, launch behavior, and CPU pressure integration in [GokuRosterFactory.cs](../Scripts/Data/GokuRosterFactory.cs#L1233) and [CpuFighterBrain.cs](../Scripts/Combat/CpuFighterBrain.cs). | Animation, telegraph, and cross-roster authoring remain content-specific rather than a missing engine system. |
| Z-Reflect pushback and wake-up use | **Implemented.** Parry resolution applies recoil and CPU profiles expose `WakeupParryChance` in [CombatStateMachine.cs](../Scripts/Combat/CombatStateMachine.cs#L463) and [CpuFighterProfileData.cs](../Scripts/Data/CpuFighterProfileData.cs#L31). | Final readability and effect styling remain presentation gates. |

The original healing question resolved to **no boss heal** in the current data
contract. Any later healing behavior would be a new design change requiring
master-plan reconciliation.

## Proposed Mechanics

### 1. Sparking Blast (Boss Phase Burst)
Currently, when a boss changes phases, they just play an animation. We will turn this into a DBFZ-style "Sparking Blast" Burst.
- **Mechanic:** When a boss enters a new phase (e.g., dropping below a health threshold), they will instantly emit an unblockable Burst shockwave that pushes the player half-screen away.
- **Buff:** The Boss will gain a "Sparking" status effect (a `DamageModifierPercent` and `DefenseModifierPercent` buff) for a set duration, surrounded by a glowing aura.

### 2. Dragon Rush (Unblockable Launcher)
If the player is turtling (holding block) against the boss, the boss needs a way to open them up.
- **Mechanic:** Add a new `dragon_rush` move to the Boss roster data. It will use `AttackHeight = AttackHeight.Throw` to bypass guarding.
- **Effect:** Upon landing, it triggers a massive launcher effect, allowing the Boss AI to follow up with an aerial combo (or a Super Dash).
- **AI Integration:** The `CpuFighterBrain` will be updated to randomly select `dragon_rush` when the player is locked down in blockstun.

### 3. Z-Reflect (Frame-1 Pushback)
Right now, our Parry system just stops an attack. DBFZ's Z-Reflect aggressively pushes the attacker away.
- **Mechanic:** We will update `ResolveParry` in `CombatStateMachine.cs`. If a boss successfully parries an attack, it will violently push the player backwards horizontally.
- **AI Integration:** Bosses will have a random chance to trigger a Z-Reflect on wake-up or when escaping hitstun to interrupt players who mindlessly mash attacks.

## Proposed Changes

### [MODIFY] `Scripts/Combat/CombatStateMachine.cs`
- Add a new `SparkingBlast` unblockable blast logic when entering `BossPhaseChanged`.
- Update `ResolveParry` to add knockback velocity to the attacker.

### [MODIFY] `Scripts/Combat/CpuFighterBrain.cs`
- Add decision logic to use `AttackHeight.Throw` moves (Dragon Rush) when the opponent is blocking.
- Add wake-up parry chance.

### [MODIFY] `Scripts/Data/GokuRosterFactory.cs` (or equivalent Boss Factory)
- Add `dragon_rush` command throw data to the Boss moveset with the `Launcher` property.

## Original User Review Question
> [!IMPORTANT]
> Do you want the **Sparking Blast** to also heal the Boss's health slightly (like it does in DBFZ), or should it purely be an offensive buff / "get off me" explosion?
