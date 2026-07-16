# Roguelike & Boss Encounter Backlog

## Authority And Freshness Notice

This is a historical gap register subordinate to
[MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md) and
[NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md). It records useful
implementation evidence, but its tier labels do not override current scope,
locked decisions, art gates, or visual approval. In this document, "wired" means
that a working implementation path exists; it does not necessarily mean the full
original design, release art, accessibility, or capture gates are complete.

The audited design input is preserved in
[ROGUELIKE_ANALYSIS.md](ROGUELIKE_ANALYSIS.md). This backlog records how the live
repository compares with that source; it is not a substitute for the source text.

Last roadmap reconciliation: 2026-07-15. Individual implementation entries below
retain their original review history unless explicitly corrected.

## 2026-07-15 Audit Reconciliation

A read-only Codex audit compared [ROGUELIKE_ANALYSIS.md](ROGUELIKE_ANALYSIS.md)
against the live repository and then ran focused headless validation. It
established the following:

- Clearly implemented: boss arena transitions/wall breaks, verticality and aerial
  combat, lore progression, limited lives, and the original input-accessibility
  criterion. Focused validation passed Assist 7/7, Run Reward 13/13, and Run Build
  27/27.
- Adapted or incomplete relative to the original inspiration: Move Card drafting
  has no hard carry cap or per-enemy drops; form swap is neutral-gated rather than
  a true mid-combo tag; corruption belongs to artifacts rather than inherited
  forms; random-pool engine support is not authored into the scored core worlds;
  the Archive Hub is functional but not yet the complete illustrated growing hub;
  and animation quality remains an active content gate.
- The Move Card offer cadence, deterministic core ladders, artifact-based
  corruption, and lack of mid-combo tagging are not silently promoted to required
  scope by this audit. They remain existing product choices unless accepted into
  the master plan.
- True defect: the Game Over results path hides the dedicated death-tip panel in
  `MvpHud.ShowGameOverResults()`. Track that repair with Phase 4/6 results and
  accessibility work.
- Hollow Archive has route choice, vertical traversal, and one champion. It is
  **not yet a true sequential boss rush**; the continuation contract and multi-boss
  routes remain in the tactical plan.
- Audit reports are evidence, not roadmap authority. Any accepted design change
  must be promoted into the master and tactical plans before implementation.

The audit itself changed no files. It verified repository state and ran focused
tests only.

### Original Boss Source-Plan Reconciliation

| Historical source plan | What it initiated | Current disposition |
| --- | --- | --- |
| [Dynamic Boss Encounter System](implementation_plan_boss_intro.md) | Boss-intro state/events, intro presentation, multiplayer boss archetypes, isolated duels, tag-team merge data, per-player spawning, and HP scaling. | Intro sequencing and archetype plumbing are implemented. Public co-op remains deferred; authored intro audio/animation and final multiplayer QA remain incomplete. |
| [DBFZ Boss Mechanics](implementation_plan_boss_mechanics.md) | Phase burst, Dragon Rush, and Z-Reflect/wake-up defense. | All three mechanics are implemented at engine/data level. Presentation art, telegraphs, audio, and broader content authoring remain open where required by the master plan. |
| [Roguelike Phase 4: Narrative, Wall Breaks, and Death Systems](implementation_plan_phase_4.md) | Lore fragments, Death as a Teacher, and cinematic wall-break transitions. | Lore and wall-break engines are implemented. The true Game Over death-tip panel remains defective; localized destruction and final wall-break art remain Phase 5 work. |

These source plans explain why the corresponding systems exist. Their unchecked
proposal lists and historical review questions do not supersede the strict audit
or current roadmap.

### Strict Status Semantics

- **Implemented** means the live repository contains the behavior that materially
  satisfies the takeaway.
- **Partial / adapted** means a useful implementation exists but differs from, or
  does not fully satisfy, the original inspiration.
- **Not code-verifiable** means code and planning evidence exist, but runtime art
  quality still requires manual review and capture gates.

### Priority 1-12 Validation

| Priority | Status | Verified implementation | Strict gap or scope decision |
| --- | --- | --- | --- |
| P1 - Run-Based Move Acquisition | **Partial / adapted** | Blank-mannequin starts, deterministic 1-of-3 rewards, Move Cards, and grid synergies are live in [MvpCombatBootstrap.cs](../Scripts/Core/MvpCombatBootstrap.cs#L60), [MoveCardCatalog.cs](../Scripts/Data/MoveCardCatalog.cs#L9), [RunRewardSystem.cs](../Scripts/Progression/RunRewardSystem.cs#L27), and [RunSessionManager.cs](../Scripts/Progression/RunSessionManager.cs#L234). | Cards are offered after horde clears rather than dropped by enemies, and [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs#L1341) appends them without a hard carry cap. The current master plan preserves the horde-offer cadence; enemy drops and a cap are not required unless separately accepted. |
| P2 - Form-Swap Tag System | **Partial / adapted** | Live form swap and directional assists exist in [CombatStateMachine.cs](../Scripts/Combat/CombatStateMachine.cs#L294), [AssistCallSystem.cs](../Scripts/Progression/AssistCallSystem.cs#L87), and [AssistCatalog.cs](../Scripts/Data/AssistCatalog.cs#L17). | Swap readiness is neutral/movement-gated rather than a true Marvel-style mid-combo tag-in. Mid-combo tagging is not silently added to master scope by this audit. |
| P3 - Artifact / Corruption System | **Partial / adapted** | Fifteen themed artifacts, cursed drawbacks, health drain, and active-artifact presentation exist in [ArtifactCatalog.cs](../Scripts/Data/ArtifactCatalog.cs#L10), [CombatActor.cs](../Scripts/Combat/CombatActor.cs#L840), and [PauseMenu.cs](../Scripts/UI/PauseMenu.cs#L714). | Corruption belongs to artifacts rather than inherited boss forms; boss-form corruption remains an unaccepted design variation. The pool is 15, not the inspiration's 100+ target. |
| P4 - Boss Arena Transitions / Wall Breaks | **Implemented at engine level** | Boss phases can trigger wall-break presentation, bounds expansion, camera transition, and phase-burst behavior in [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs#L572). | Phase 5 still requires localized destruction groups and approved environment art; global engine support alone does not complete the release-facing presentation. |
| P5 - Verticality And Aerial Combat | **Implemented** | Wall-run, wall-jump, vertical sections, route choices, and aerial hover enemies exist in [CombatStateMachine.cs](../Scripts/Combat/CombatStateMachine.cs#L1210), [HollowArchiveMission.cs](../Scripts/Data/HollowArchiveMission.cs#L35), and [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs#L1171). | The core scored ladders intentionally remain forward belt-scroll routes; vertical and branching traversal belongs to Hollow Archive and optional modes. |
| P6 - Randomized Encounter Composition | **Partial / adapted** | Random-pool data and deterministic selection infrastructure exist in [WorldMissionData.cs](../Scripts/Data/WorldMissionData.cs#L117) and [ArcadeEncounterDirector.cs](../Scripts/Stage/ArcadeEncounterDirector.cs#L1521); elite encounters are live. | No authored `UseRandomPool = true` data is present in current world content. Scored ladders are intentionally authored/fixed unless the master plan changes; optional modes may use random pools. |
| P7 - Persistent Archive Hub | **Partial** | Squad name, trophies, codex, progress-aware NPCs, and training access exist in [ArchiveHubScene.cs](../Scripts/UI/ArchiveHubScene.cs#L57). | The hub remains a functional UI scene rather than the complete illustrated, visibly growing mech-hub/trophy space. The visual and meta-surfacing upgrade belongs to master Phase 6. |
| P8 - Hades-Style Narrative Across Runs | **Implemented** | Lore fragments, codex reading, secret-ending detection, and hidden-world unlocks exist in [LoreCatalog.cs](../Scripts/Data/LoreCatalog.cs#L18), [ArchiveHubScene.cs](../Scripts/UI/ArchiveHubScene.cs#L255), and [MvpProgressStore.cs](../Scripts/Progression/MvpProgressStore.cs#L144). | Hollow Archive's narrative unlock exists, but its true sequential boss-rush structure remains incomplete. |
| P9 - Limited Lives System | **Implemented** | Runs carry limited lives; death consumes a life and final depletion triggers Game Over in [RunSessionManager.cs](../Scripts/Progression/RunSessionManager.cs#L39) and [CombatActor.cs](../Scripts/Combat/CombatActor.cs#L510). | The final-death teaching presentation has the separate P11 defect below. |
| P10 - Accessibility And Input Philosophy | **Implemented for the original criterion** | Motion grammar, quick-command compatibility, and authored auto-combo paths exist in [CommandInterpreter.cs](../Scripts/Input/CommandInterpreter.cs#L14), [MoveData.cs](../Scripts/Data/MoveData.cs#L91), and [TestRosterFactory.cs](../Scripts/Data/TestRosterFactory.cs#L60). | This does not complete master Phase 6 settings, glyphs, rebinding, reduced flash, high-contrast telegraphs, or assist options. |
| P11 - Death As A Teacher | **Partial; true defect** | Death tips and the death breakdown are implemented in [DeathTipsCatalog.cs](../Scripts/Data/DeathTipsCatalog.cs#L11) and [MvpHud.cs](../Scripts/UI/MvpHud.cs#L1649). | The true Game Over path calls `ShowGameOverResults()` and hides `_deathPanel` in [MvpHud.cs](../Scripts/UI/MvpHud.cs#L899), overriding the final teaching beat. Track the repair in Phase 4/6 results and accessibility work. |
| P12 - Quality Animation As Art Style | **Partial / ongoing** | The sprite-atlas pipeline, bespoke runtime sheets, art-completeness gates, and visual rubric are established in [ART_ASSET_COMPLETENESS_PLAN.md](ART_ASSET_COMPLETENESS_PLAN.md) and [VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md). | Completion is not code-verifiable alone. Each character and content slice still needs manual rubric approval and 1280x720 plus 1920x1080 runtime captures. Overseer Basalt is the active Phase 5 character-art slice. |

Audit summary: P4, P5, P8, P9, and the original P10 criterion are clearly
implemented. P1, P2, P3, P6, P7, P11, and P12 are implemented in adapted,
incomplete, defective, or still-reviewing form.

### Source-Inspiration Takeaway Validation

| Source | Takeaway | Status | Repository conclusion |
| --- | --- | --- | --- |
| Mad King Redemption | Corruption system | **Partial / adapted** | Cursed artifacts and HP drain exist, but corruption is attached to artifacts rather than absorbed boss forms. |
| Mad King Redemption | Randomized dungeons | **Partial / adapted** | Random-pool infrastructure exists, but current core-world content remains mostly authored and fixed. |
| Mad King Redemption | Four playable heroes | **Implemented** | Archived forms function as distinct unlockable heroes through [FormArchive.cs](../Scripts/Progression/FormArchive.cs#L13) and persistent progress. |
| Shot One Fighters | Custom moveset building | **Partial / adapted** | Move Card drafting and synergies work, but there are no per-enemy card drops or hard carry cap. |
| Shot One Fighters | 100+ artifacts / cursed builds | **Partial** | The live catalog has 15 artifacts, including cursed variants; it is a meaningful system but not the inspiration's content volume. [RunBuildTests.cs](../Scripts/Debug/RunBuildTests.cs#L75) enforces a pool of at least 14. |
| Shot One Fighters | Boss as gatekeeper | **Partial** | Bosses unlock forms and lore, but no dedicated path guarantees a signature move plus top artifact from every boss. |
| Shot One Fighters | Mech hub | **Partial** | Hub functionality exists, but the visually growing trophy-room/mech-hub promise remains Phase 6 work. |
| Steel Maiden | Two-button simplicity | **Partial / adapted** | Auto-combos and simplified commands provide approachability without replacing the six-button fighting-game model. |
| Steel Maiden | Builds shift every run | **Partial** | Drafted cards and artifacts vary runs, but franchise-specific power identity and build transformation remain uneven. |
| Steel Maiden | Frame-by-frame animation | **Not code-verifiable** | The pipeline and gates exist; only manual animation review and runtime captures can approve the result. |
| Marvel Tokon | 4v4 tag-team system | **Partial / adapted** | Forms and assists exist, but swaps are not true mid-combo tag-ins and public co-op remains deferred. |
| Marvel Tokon | Wall breaks and stage transitions | **Implemented at engine level** | Runtime wall-break/bounds/camera behavior exists; localized final art remains Phase 5 work. |
| Marvel Tokon | Directional assists | **Implemented** | Neutral, forward, and down assist variants are authored and validated. |
| Marvel Tokon | Dynamic team naming | **Implemented** | `SquadNameGenerator` is surfaced in the Archive Hub. |
| Marvel Tokon | Approachable plus deep | **Implemented for the criterion** | Dual input grammar and auto-combo routes coexist with the full fighting-game command layer. |
| SlashZero | Vertical and aerial combat | **Implemented** | Wall movement, aerial enemies, and vertical route content are live. |
| SlashZero | Hacking build system | **Partial / adapted** | Card adjacency synergies and reorder UI exist, but there is no hard-cap slotted inventory with strong spatial sacrifice. |
| SlashZero | Hades-style narrative | **Implemented** | Lore, codex, persistent unlocks, and the secret-world reveal are present. |
| SlashZero | Parkour / multi-path traversal | **Implemented in optional content** | Hollow Archive provides wall traversal and route choice; scored core ladders intentionally stay linear. |
| SlashZero | Character variety | **Implemented** | Forms provide differentiated movesets and roles sufficient to satisfy the takeaway. |
| Genre best practice | Meaningful choices over flat stats | **Partial** | Move Cards materially alter options; many artifacts remain primarily numeric modifiers. |
| Genre best practice | In-run progression / pick 1 of 3 | **Implemented** | Horde-clear reward offers are deterministic and immediately usable. |
| Genre best practice | Meta-progression via content unlocks | **Implemented** | Boss forms, lore, records, and hidden content persist across runs. |
| Genre best practice | Every death is a learning opportunity | **Partial; defect tracked** | Death tips exist, but the true Game Over result currently hides the dedicated teaching panel. |

Bottom line: the inspiration analysis is **substantially implemented**, but it is
not an honest 12-for-12 match to the original wording. The largest unresolved
differences are no mid-combo form tag, no Move Card cap, unauthored random pools
in current worlds, incomplete growing-hub presentation, and the Game Over
death-teacher regression.

Consolidated list of missing / incomplete items gathered from two analyses:

1. **[Roguelike Analysis](ROGUELIKE_ANALYSIS.md)** (Priorities P1-P12) - run-based
  move acquisition, form-swap tag, artifacts, wall breaks, verticality,
  randomized encounters, Archive Hub, lore, lives, dual input, death tips, and
  animation.
2. **Original boss implementation plans** -
  [Dynamic Boss Encounter System](implementation_plan_boss_intro.md),
  [DBFZ Boss Mechanics](implementation_plan_boss_mechanics.md), and
  [Roguelike Phase 4: Narrative, Wall Breaks, and Death Systems](implementation_plan_phase_4.md),
  plus related beam-clash and hazard-throwing work recorded by this audit.

Only the **missing or partially-implemented** items are tracked here. Everything
not listed (lives, death tips, form-swap, assists, wall-break transitions,
randomized wave pools, archive hub scaffold, phase burst, Dragon Rush data,
Z-Reflect pushback, beam-clash minigame, etc.) is already wired.

Original implementation review: 2026-07-05.

## Status legend

- ❌ Missing — no implementation exists.
- ⚠️ Partial — scaffolded or a related system exists, but not the full design.

## A. Roguelike Core Loop — *IMPLEMENTED (Tier 1)*

| ID | Item | Source | Status |
|----|------|--------|--------|
| A1 | `RunSessionManager.StartNewRun()` now called from `MainMenu.StartPrototype` | Roguelike (cross-cutting) | ✅ |
| A2 | In-run reward-choice UI: clear a horde encounter → pick 1 of 3 (moves/artifacts) | Roguelike P1 + P3 | ✅ |
| A3 | Move Card content library — 6 cards in `MoveCardCatalog` | Roguelike P1 | ✅ |
| A4 | Move Card acquisition on encounter clear (offer, not per-enemy drop) | Roguelike P1 | ✅ |

**How it works:** clearing a horde encounter puts the director in a new
`ArcadeStageState.AwaitingReward` state which freezes the simulation while the HUD
shows a centered reward panel. Pressing 1/2/3 calls
`ArcadeEncounterDirector.RequestRewardChoice`, applied on the next sim tick:
the Move Card is injected into the live player form (usable immediately) or the
Artifact is added to `RunSessionManager.ActiveArtifacts`. The roll
(`RunRewardSystem.RollOffer`) is deterministic and excludes already-owned cards.
Rewards are disabled under `--headless`/replay so automated tests are unaffected.
Covered by `PROJECT_MANNEQUIN_REWARD_TEST=1` (11/11 pass).

## B. Roguelike Build Depth — *IMPLEMENTED (Tier 2)*

| ID | Item | Source | Status |
|----|------|--------|--------|
| B1 | Move Card grid + adjacency synergies (`MoveCardGrid` / `MoveCardSynergyRules`) + in-run reorder UI (pause-menu grid with live synergy readout) | Roguelike P1 (SlashZero) | ✅ |
| B2 | Artifact library expanded to 15 across 3 world themes (`ArtifactCatalog`) | Roguelike P3 | ✅ |
| B3 | Corruption: `IsCursed` + per-second health drain on 3 cursed artifacts | Roguelike P3 | ✅ |
| B4 | Dynamic squad naming (`SquadNameGenerator`, shown in the Archive Hub) | Roguelike P2 | ✅ |

**Notes:** synergies (Juggle / Rushdown / Zoning / Overdrive / Reversal / Trap)
grant a run-wide damage bonus via `RunSessionManager.GetSynergyDamageBonusPercent`
(players only). The current catalog contains 15 artifacts. Artifact
damage/defense bonuses are correctly gated to players
(previously leaked to enemies). Covered by `PROJECT_MANNEQUIN_BUILD_TEST=1` (15/15).

## C. Meta-Progression & Narrative — *IMPLEMENTED (Tier 4)*

| ID | Item | Source | Status |
|----|------|--------|--------|
| C1 | Authored lore fragments (`LoreCatalog`, 6 across 3 boss worlds), unlocked on boss defeat | Roguelike P8 | ✅ |
| C2 | Progress-aware hub NPCs (Curator / Blacksmith / Lorekeeper / Oracle) | Roguelike P8 | ✅ |
| C3 | Secret-ending detection on full lore + hidden-challenger reveal (`TryUnlockSecretEnding`) | Roguelike P8 | ✅ |

**Correction (2026-07-15):** the hidden challenger is a playable secret-mode
foundation called "The Hollow Archive" (`HollowArchiveMission`), gated on
`SecretEndingUnlocked` and shown from the main flow. It branches through optional
vertical content to one final Archive Champion. It must not be called a true
boss-rush until the sequential-boss continuation contract and multiple bosses per
route in [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md) are complete.

## D. Movement & Stage Traversal — *Tier 4*

| ID | Item | Source | Status |
|----|------|--------|--------|
| D1 | Wall-run / wall-slide — hug a wall to climb briefly, then slow the fall | Roguelike P5 | ✅ |
| D2 | Wall-jump — kick off a wall and refresh an air jump | Roguelike P5 | ✅ |
| D3 | Vertical sections + **vertical camera follow** (`CameraCenterY` / `VerticalCeiling`) and **branching routes** (`RouteChoiceData`, `AwaitingRouteChoice`, HUD route panel), demoed in the Hollow Archive (gate → spire/vault → champion) | Roguelike P5 | ✅ |

## E. Encounter Variety & Co-op Scaling

| ID | Item | Source | Status |
|----|------|--------|--------|
| E1 | Elite enemy variants — +HP/+damage, gold tint + larger scale, HUD cue (`RollElite`) | Roguelike P6 | ✅ |
| E2 | `PerPlayerHPMultiplier` now applied at spawn (author >1.0 to enable co-op HP scaling) | Boss Plan 1 | ✅ |
| E3 | Isolated-Duel sub-arenas (`ApplyIsolatedDuelArenas`) + **split-screen** overlay (`SplitScreenOverlay` — one SubViewport + camera per player, sharing the World3D, gated to 2+ player duels) | Boss Plan 1 | ✅ |
| E4 | Player + boss intro poses + procedural announcer stingers (`ProceduralAudioFactory`) | Boss Plan 1 | ✅ |

## F. Combat Mechanic Polish — *IMPLEMENTED (Tier 3)*

| ID | Item | Source | Status |
|----|------|--------|--------|
| F1 | Beam clash winner empowerment — `ClashDamageMultiplier` 1.5x + `ClashUnblockable` (verified `beam=True`) | Beam Plan 2 | ✅ |
| F2 | Hazard throwing — already present in `HitResolver`: retag `TeamId` + 30f launch on launcher/throw | Beam Plan 2 | ✅ |
| F3 | CPU Z-Reflect on wake-up / hitstun-escape (`WakeupParryChance`) | DBFZ Plan 3 | ✅ |
| F4 | Dragon Rush scripted aerial follow-up (juggle/launcher chase after a launcher throw) | DBFZ Plan 3 | ✅ |

## Suggested prioritization tiers

- **Tier 1 — Unlocks the roguelike:** A1 → A2 → A3 → A4 — ✅ DONE
- **Tier 2 — Build variety & replayability:** B1, B2, B3, B4, E1, E2 — ✅ DONE
- **Tier 3 — Spectacle & combat feel:** E3, E4, F1, F2, F3, F4 — ✅ DONE
- **Tier 4 — Long-tail content:** C1, C2, C3, D1, D2 — ✅ DONE; D3 foundation (⚠️ vertical climb; branching routes deferred)

Items in group **A** are the highest leverage: every other roguelike system
(forms, lives, death tips, hub, artifacts-on-actor) is already wired and waiting
on the run loop being closed.

## Progress log

- 2026-07-05: Backlog created.
- 2026-07-05: Tier 1 (A1–A4) implemented — run start wiring, 6 Move Cards,
  deterministic reward roll (`RunRewardSystem`), `AwaitingReward` director state
  with sim freeze, and HUD reward panel + 1/2/3 selection. New test suite
  `RunRewardTests` (`PROJECT_MANNEQUIN_REWARD_TEST=1`, 11/11). Existing suites
  still green (Defensive 22, Combo 12, AiModules 14, Determinism 15).
- 2026-07-05: Added procedural reward icons (`ProceduralRewardIconFactory`) — the
  reward panel now renders a per-category icon (launcher / projectile / combo /
  special / ultimate / artifact) beside each option, drawn in code (no external
  art). `RunRewardTests` now 13/13 (adds icon-key + rasterization checks).
- 2026-07-05: Tier 2 (B1–B4, E1, E2) implemented — Move Card grid + adjacency
  synergies, 9 world-themed artifacts incl. 3 cursed (health-drain corruption),
  squad-name generator (shown in Archive Hub), elite enemy variants (buff + gold
  glow + scale), and `PerPlayerHPMultiplier` co-op scaling wired at spawn. Also
  fixed artifact stats leaking to enemies. New suite `RunBuildTests`
  (`PROJECT_MANNEQUIN_BUILD_TEST=1`, 15/15); all prior suites still green.
- 2026-07-05: Tier 3 (E3, E4, F1–F4) implemented — beam-clash winner empowerment
  (1.5x + unblockable), CPU wake-up Z-Reflect + Dragon Rush juggle follow-up,
  Isolated-Duel sub-arena band clamping, player intro pose, and procedural
  announcer stingers. F2 (hazard retag + launch) was already present in
  `HitResolver`. Verified via Astral smoke (`passed=True beam=True`, 12/12 phases)
  and all pure suites still green.
- 2026-07-05: Tier 4 (C1–C3, D1–D2, D3 foundation) implemented — authored
  `LoreCatalog` (6 fragments) unlocked on boss defeat, progress-aware hub NPCs +
  secret-ending detection with hidden-challenger reveal, and wall-run / wall-jump
  traversal (with a vertical-section climb extension). Build suite extended to
  19/19 (adds lore checks); Determinism still 15/15 (wall mechanics are
  input+bounds deterministic).
- 2026-07-05: Follow-up — B1 completed with an in-run Move Card grid UI in the
  pause menu: a 3-wide grid of the equipped cards with per-card reorder (◀/▶) and
  a live synergy readout, so players can arrange adjacency synergies mid-run.
- 2026-07-08: Optional items — (1) **playable hidden boss**: `HollowArchiveMission`,
  a secret world gated on `SecretEndingUnlocked` with a main-menu button; (2) **D3**
  vertical camera (`CameraCenterY`) + branching routes (`RouteChoiceData` /
  `AwaitingRouteChoice` + HUD route panel), demoed in the Hollow Archive; (3) **E3
  split-screen** (`SplitScreenOverlay`) for 2+ player isolated duels. Build clean;
  Determinism 15 / Reward 13 / Build 19 still green; hidden & branching missions
  boot without errors. Split-screen and route/vertical framing need an interactive
  session to verify visually (headless can't render / drive input).
- 2026-07-08: Refinements — split-screen now frames each half on the player **and
  their locked rival** (smoothed, auto-zoom to fit both); the Hollow Archive spire
  is now an aerial climb — enemies `HoverHeight`-hover at staggered heights
  (`EnemySpawnData.SpawnHeight`, held in `CombatActor.IntegrateMotion`, dropping
  only when hit), so the wall-run/wall-jump ascent has combat purpose. Build clean;
  Determinism 15 / Reward 13 / Build 19 green; hidden mission boots without errors.
- 2026-07-08: **Themed enemy art** — replaced the placeholder tinted-mannequin look
  for all 10 non-boss enemies with bespoke Higgsfield sprite sheets that fit each
  world: Archive (teal sentinel Scout / rust-iron Raider / obsidian Bruiser),
  World Warrior (white-gi Rookie / tracksuit Striker / wrestler Grappler), Astral
  (green Saibaman / two Frieza-force soldiers / blue-gold Ki Captain). Generated via
  the mannequin pipeline (per-animation sub-sheets restyled by `gpt_image_2`, then
  composited to the 10×9 layout by new `Scripts/Tools/compose_enemy_sheet.py` +
  `generate_enemy_sprites.ps1`); sources kept under `Assets/Sprites/Enemies/Source/`
  (`.gdignore`). Factories point each enemy at `res://Assets/Sprites/Enemies/{id}_higgsfield_v1.png`
  with safe fallback. All 10 import + boot clean; Determinism 15 / Reward 13 / Build 19 green.
