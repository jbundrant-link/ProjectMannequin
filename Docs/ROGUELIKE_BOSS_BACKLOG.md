# Roguelike & Boss Encounter Backlog

Consolidated list of missing / incomplete items gathered from two analyses:

1. **Roguelike Analysis** (Priorities P1–P12) — run-based move acquisition, form-swap
   tag, artifacts, wall breaks, verticality, randomized encounters, archive hub,
   lore, lives, dual input, death tips, animation.
2. **Boss / Combat Implementation Plans** — Dynamic Boss Encounter System,
   Beam Clashes & Hazard Throwing, and DBFZ Boss Mechanics.

Only the **missing or partially-implemented** items are tracked here. Everything
not listed (lives, death tips, form-swap, assists, wall-break transitions,
randomized wave pools, archive hub scaffold, phase burst, Dragon Rush data,
Z-Reflect pushback, beam-clash minigame, etc.) is already wired.

Last reviewed: 2026-07-05.

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
| B2 | Artifact library expanded to 9 across 3 world themes (`ArtifactCatalog`) | Roguelike P3 | ✅ |
| B3 | Corruption: `IsCursed` + per-second health drain on 3 cursed artifacts | Roguelike P3 | ✅ |
| B4 | Dynamic squad naming (`SquadNameGenerator`, shown in the Archive Hub) | Roguelike P2 | ✅ |

**Notes:** synergies (Juggle / Rushdown / Zoning / Overdrive / Reversal / Trap)
grant a run-wide damage bonus via `RunSessionManager.GetSynergyDamageBonusPercent`
(players only). Artifact damage/defense bonuses are now correctly gated to players
(previously leaked to enemies). Covered by `PROJECT_MANNEQUIN_BUILD_TEST=1` (15/15).

## C. Meta-Progression & Narrative — *IMPLEMENTED (Tier 4)*

| ID | Item | Source | Status |
|----|------|--------|--------|
| C1 | Authored lore fragments (`LoreCatalog`, 6 across 3 boss worlds), unlocked on boss defeat | Roguelike P8 | ✅ |
| C2 | Progress-aware hub NPCs (Curator / Blacksmith / Lorekeeper / Oracle) | Roguelike P8 | ✅ |
| C3 | Secret-ending detection on full lore + hidden-challenger reveal (`TryUnlockSecretEnding`) | Roguelike P8 | ✅ |

**Note:** the hidden challenger is now a **playable boss-rush** — "The Hollow Archive"
(`HollowArchiveMission`), a secret world gated on `SecretEndingUnlocked`, shown as a
main-menu button, branching to a final "Archive Champion" fight.

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
