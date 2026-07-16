# 🥊 Implementation Plan: Dynamic Boss Encounter System

To make our boss encounters feel incredibly hyped and unique across different franchises, we will implement a multi-layered boss system. This will cover cinematic fighting-game intros, as well as dynamic scaling mechanics for multiplayer co-op!

> [!IMPORTANT]
> **Historical source plan.** This proposal initiated the dynamic boss-intro and
> multiplayer-archetype work. It is preserved as provenance, not as the active
> roadmap. See [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md),
> [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md), and the strict
> reconciliation in [ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md).

## Implementation Reconciliation - 2026-07-16

| Original proposal | Current result | Remaining work or scope boundary |
| --- | --- | --- |
| Cinematic ROUND/READY/FIGHT state | **Implemented and expanded.** `BossIntro`, sequenced presentation events, frozen simulation, camera/HUD/audio consumers, and [BossIntroSequencer.cs](../Scripts/Presentation/BossIntroSequencer.cs) are live. | Combat spectacle still needs normal/cinematic super parity, authored stingers/VFX, and final capture QA. |
| Custom intro animations and announcer audio | **Adapted.** Player/boss intro poses and procedural announcer stingers exist. | Authored audio and final character-specific animation remain presentation work under Phases 1, 5, and 7. |
| Classic, Isolated Duel, and Tag Team archetypes | **Implemented at engine level** in [WorldMissionData.cs](../Scripts/Data/WorldMissionData.cs#L19) and [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs#L774), including per-player spawning, target locks, merge-phase plumbing, and HP scaling. | Public co-op remains deferred by the master plan. Existing split-screen and isolation tests are compatibility coverage, not a commitment to ship public multiplayer in this pass. |
| Split-screen isolated-duel presentation | **Implemented for 2+ player duel scenarios** through [SplitScreenOverlay.cs](../Scripts/Presentation/SplitScreenOverlay.cs). | Final visual, performance, and real-controller multiplayer QA remain later gates. |

The original review questions resolved toward custom intro poses plus procedural
audio fallbacks. Authored announcer audio remains future presentation work.

## Original User Review Questions
> [!NOTE]
> Do you want the player character and boss to perform custom "Intro Animations" (like cracking their knuckles or pointing) during the "ROUND 1" phase, or should they just stand in their idle poses?
>
> **Audio:** Do we have announcer audio files ("Round 1", "Fight") ready to use, or should I just implement the text visuals for now?

---

## Proposed Changes

### 1. Cinematic Fighting-Game Intro
Every boss fight will pause the action right as it starts, displaying the iconic "ROUND 1... FIGHT!" sequence.

#### [MODIFY] [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs) & [GameSimulation.cs](../Scripts/Core/GameSimulation.cs)
- Add a new state: `ArcadeStageState.BossIntro`.
- Emit `BossIntroStarted` (Shows "ROUND 1") and `BossIntroFight` (Shows "FIGHT!") events for the HUD.
- During `BossIntro`, combat physics and AI are completely frozen so nobody can attack prematurely.

### 2. Multiplayer Boss Encounter Archetypes
To ensure boss fights don't feel like a standard beat 'em up where 4 players just spam attacks on one enemy, we will define unique **Boss Encounter Archetypes** in our `StageEncounterData`. 

#### [MODIFY] [WorldMissionData.cs](../Scripts/Data/WorldMissionData.cs)
Add a `BossEncounterType` enum to dictate how the stage handles multiplayer scaling, along with config fields for fine-tuning:

- **`BossEncounterType`** (Classic, IsolatedDuels, TagTeam)
- **`SpawnPolicy`**: Dictates whether clones are spawned, or if adds are summoned based on player count.
- **`MergeAtPhase`**: For Tag-Team fights, the phase index where barriers drop and sub-arenas merge.
- **`PerPlayerHPMultiplier`**: Controls HP scaling, though we will *prefer mechanical scaling* (extra phases, adds, required interrupts) over massive HP sponges.

1. **Classic Beat 'em Up (1 vs All)**
   - The traditional style where all players fight a single, massive boss in a shared arena.
   - Mechanical scaling: Adds minions or unlocks AoE attacks when multiple players are present.
   
2. **Isolated Duels (1v1 x N)**
   - When the boss stage begins, the camera splits, or players are teleported to separate "sub-arenas".
   - Each player fights their own instance of the boss (or an assigned rival) in a strict 1v1 duel simultaneously.
   
3. **Dynamic Tag-Team / Merging Arenas**
   - A multi-round boss fight. 
   - **Round 1:** Players are in Isolated Duels.
   - **Round 2 (Phase Change):** Reaching `MergeAtPhase` causes barriers to drop, arenas to merge, and the bosses to tag-team together against the players in an all-out brawl!

#### [MODIFY] [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs)
- Implement logic to read the `BossEncounterType`.
- If `IsolatedDuels` is active and multiple players are present, dynamically spawn clone instances of the boss and assign `TargetPlayerId` locks so they only fight their specific rival.

## Original Verification Plan
1. We will build the foundational state machine for the Cinematic Intro ("ROUND 1... FIGHT!").
2. We will expand `StageEncounterData` to support the new Multiplayer Archetypes.
3. We will configure the Street Fighter and Dragon Ball stages to use the `Isolated Duels` and `Tag-Team` mechanics when playing in multiplayer, ensuring the 1v1 feel is never lost!

Do these dynamic multiplayer mechanics capture the creative vision you have for the boss encounters?
