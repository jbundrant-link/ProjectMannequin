# Roguelike Phase 4: Narrative, Wall Breaks, and Death Systems

This plan implements the final layer of the Roguelike Analysis: giving death meaning, advancing narrative across runs, and making boss phase transitions feel incredible.

> [!IMPORTANT]
> **Historical source plan.** This document records the proposal that initiated
> the narrative, wall-break, and death-system implementation. It is not the active
> roadmap. Current scope is owned by
> [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md), near-term work
> by [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md), and strict
> implementation status by
> [ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md).

## Implementation Reconciliation - 2026-07-16

| Original proposal | Current result | Remaining work |
| --- | --- | --- |
| Lore fragments and persistent meta-narrative | **Implemented.** Boss defeat can unlock authored lore through [MvpProgressStore.cs](../Scripts/Progression/MvpProgressStore.cs#L102) and [MvpHud.cs](../Scripts/UI/MvpHud.cs#L1554). | Hollow Archive's narrative unlock exists, but the mode still needs the sequential-boss structure in the tactical plan. |
| Death as a Teacher | **Partial; defect tracked.** Fatal move payloads, [DeathTipsCatalog.cs](../Scripts/Data/DeathTipsCatalog.cs), and death-tip UI exist. | The full Game Over results modal currently hides the dedicated death panel in [MvpHud.cs](../Scripts/UI/MvpHud.cs#L899). Repair belongs to Phase 4/6 results and accessibility work. |
| Wall breaks and boss transitions | **Implemented at engine level.** Phase changes can expand bounds, push actors, shake/focus the camera, emit wall-break events, and swap stage presentation. | Current background replacement is global and release art is incomplete. Phase 5 still requires localized destruction groups, seams QA, authored VFX/audio, and approved environment captures. |

The original Game Over question resolved toward a full results overlay with
focusable actions. The original background-swap question resolved toward using
destruction/background changes, but the current global implementation must still
be replaced or extended with localized groups before release.

## Original User Review Questions
> [!NOTE]
> Do you want the "GameOver" screen to be a full UI overlay with a button to "Return to Hub", or just text floating on the screen while the game resets automatically?

> [!NOTE]
> For Wall Breaks, I'm planning to shift the camera bounds and apply a screen-shake effect when a boss enters Phase 2. Should we also swap the background texture to simulate bursting into a new room?

## Proposed Changes

---

### [Lore Fragments & Meta-Narrative]
When you defeat a boss, you will unlock snippets of lore (Hades-style narrative).
#### [MODIFY] [MvpProgressStore.cs](../Scripts/Progression/MvpProgressStore.cs)
- Add `List<string> UnlockedLoreFragments`.
- Add `UnlockLore(string loreId)` and save it to disk.
#### [MODIFY] [MvpHud.cs](../Scripts/UI/MvpHud.cs)
- Listen for `ActorDefeated` on Bosses and grant a randomized or boss-specific lore piece, displaying a "LORE UNLOCKED" notification.

---

### [Death as a Teacher (GameOver)]
Instead of just a generic Game Over, the game will tell you *why* you died and how to avoid it next time.
#### [MODIFY] [CombatActor.cs](../Scripts/Combat/CombatActor.cs)
- Track the ID of the `MoveData` that delivered the fatal hit.
- Send this Move ID in the payload of the `GameOver` event.
#### [NEW] [DeathTipsCatalog.cs](../Scripts/Data/DeathTipsCatalog.cs)
- A dictionary mapping specific dangerous boss moves (e.g., "ryu_shinku_hadouken") to advice ("Jump over the Shinku Hadouken or Perfect Parry the first hit.").
#### [MODIFY] [MvpHud.cs](../Scripts/UI/MvpHud.cs)
- Display the tip from `DeathTipsCatalog` when rendering the Game Over state.

---

### [Wall Breaks & Boss Transitions]
When a boss hits 50% health, they transform. We will add a cinematic "Wall Break".
#### [MODIFY] [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs)
- Listen for `BossPhaseChanged`. 
- When triggered, momentarily expand the camera's MaxX bound, apply an impulse to the boss/player, and trigger a heavy camera shake (using Godot's RandomNumberGenerator for displacement).

## Original Verification Plan
1. Start a run, intentionally die to Ryu's Hadouken, and verify the Death Tip appears.
2. Defeat Ryu, verify "LORE UNLOCKED" appears, and check the Save Data.
3. Bring a boss to Phase 2, verify the camera shakes and expands to simulate a Wall Break.
