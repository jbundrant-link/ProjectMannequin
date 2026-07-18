# Master Implementation Plan

## Professional Brawler Polish and Stage Ladder

This document is the complete Phase 0-7 product and implementation contract for
Project Mannequin. It preserves the full scope, locked decisions, completion
criteria, and deferred boundaries that were originally maintained in a chat
session's `plan.md`. Repository work must not depend on that session-memory copy.

## Roadmap Authority And Precedence

Use the following hierarchy whenever plans, backlogs, audits, or implementation
status disagree:

| Document or evidence                                                                                                                                                                                         | Responsibility                                                                                                                                                                     |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **[MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md)**                                                                                                                                           | Complete Phase 0-7 scope, locked product decisions, cross-phase sequencing, verification contract, and scope boundaries. This is the primary roadmap.                              |
| **[NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md)**                                                                                                                                               | Near-term tactical sequence: gamepad QA, combat spectacle, Hollow Archive, and eventual Foundry. It refines execution order but cannot replace, narrow, or waive this master plan. |
| **[ART_ASSET_COMPLETENESS_PLAN.md](ART_ASSET_COMPLETENESS_PLAN.md)**                                                                                                                                         | Mandatory content and art-completion gates. A mechanically complete phase remains incomplete while any applicable gate fails.                                                      |
| **[VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md)**                                                                                                                                                           | Authoritative visual language, canonical references, rejection rules, generation workflow, and approval rubric.                                                                    |
| **[PROJECT_HANDOFF.md](PROJECT_HANDOFF.md)**                                                                                                                                                                 | Current verified execution checkpoint and machine-transfer instructions. It is operational context only and cannot alter scope or waive a roadmap/completion gate.                  |
| **[ROGUELIKE_ANALYSIS.md](ROGUELIKE_ANALYSIS.md)**                                                                                                                                                           | Preserved design research, source-game takeaways, and proposed features. It is proposal evidence, not accepted scope.                                                              |
| **Historical boss source plans:** [intro/archetypes](implementation_plan_boss_intro.md), [DBFZ mechanics](implementation_plan_boss_mechanics.md), and [narrative/wall/death](implementation_plan_phase_4.md) | Original implementation proposals and design rationale. Their reconciled status lives in the backlog; they are not active roadmap authority.                                       |
| **[ROGUELIKE_BOSS_BACKLOG.md](ROGUELIKE_BOSS_BACKLOG.md)**                                                                                                                                                   | Historical gap register and implementation evidence. It may be stale and does not override current scope or completion gates.                                                      |
| **Audits, session transcripts, and agent reports**                                                                                                                                                           | Verification evidence and unresolved findings only. Promote durable conclusions into repository documentation, tests, or code before treating them as roadmap changes.             |

Precedence rules:

1. The master plan owns **what must ship** and all locked or deferred decisions.
2. The next implementation plan owns **what to do next** within that scope.
3. The art-completeness plan and visual-style bible own **whether content is
   complete and approvable**.
4. Analyses, backlogs, and audits may propose changes or identify defects, but
   they do not silently alter scope. Any accepted change must be reconciled into
   this master plan and the affected tactical or gate document.
5. A narrower document omitting a requirement does not cancel that requirement.
6. If a tactical gate is weaker than a locked master-plan gate, the stricter
   master-plan gate applies. In particular, no fourth world enters full production
   until Archive Nexus, World Warrior Sector, and Astral Battlefront all reach
   console-demo quality.

## Current Execution Focus - 2026-07-18

- The active master-plan phase is **Phase 5**.
- Archive Nexus and its shared UI quality bar are complete. The current slice is
  Tournament Grappler's original non-military style-v2 atlas, followed by
  Pavilion Circuit and the remaining World Warrior art-completeness pass.
- Grappler's approved identity plus idle/walk/dash/jump/misc sources survived the
  interrupted session. Its first attack pair remains rejected for weak
  shoulder-drive readability and requires a targeted choreography revision
  before composition. The exact continuation state lives in
  [PROJECT_HANDOFF.md](PROJECT_HANDOFF.md).
- The verified starting baseline is 146/146 world-run assertions plus 37/37
  stage-hazard assertions (183/183 aggregate). Dojo Approach, Dojo Rookie, and
  Pavilion Striker already have dual-resolution runtime approval.
- Gamepad QA, combat spectacle correctness, and Hollow Archive remain required
  tactical tracks in [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md).
  Their priority does not cancel or bypass the active Phase 5 completion gates.

Project Mannequin already has a deep deterministic fighter/brawler foundation.
The professional-quality pass will preserve all existing concepts while adding
the missing sensory feedback, reliable run architecture, Tekken Force-style
stage ladder, arcade mastery loop, polished presentation, environmental
interaction, controller parity, robust saves, options, and QA. Work remains
phased and juice-first after mandatory baseline recovery.

## Implementation Progress

Started 2026-07-09. Entries are cumulative and dated; current status must also
be checked against the linked completion-gate documents and live validation.

- Phase 0 baseline verified: clean build; Archive/World Warrior/Astral/Training/menu headless boots clean. Catalog is healthy (31 KB); earlier empty-file result was a read-tool false positive.
- Phase 0 implemented: persistent P1 keyboard/gamepad selector on Main Menu (`InputDevicePreferences`), selected-device assignment/reconnect/duplicate guard in `LocalInputManager`, controller UI router, missing joypad Jump binding (Misc1), controller pause/form-select/reward/route/end-screen input, pause/hub focus restoration, ArchiveHub back path fix, and prototype-facing copy cleanup.
- Input grammar baseline restored from the user-verified Ryu session: S/Down drives motion notation; C is only the crouching-normal modifier; S+attack remains a standing normal. Repeated directions still require release/change. Gamepad D-pad Down supports both motions and crouching normals, while analog Y remains lane-only.
- Phase 1 first slice implemented: pooled `CombatVfxManager` (36 effects) for strike/heavy/counter/parry/guard/armor/bounce/landing/KO; immediate event-driven actor flash; deterministic procedural combat SFX fallbacks + pitch variation; tiered combo praise; pooled 24-label floating feedback.
- Validation: all edited files diagnostics clean; combat/World Warrior/Astral VFX smoke logs clean with no errors/warnings.
- Phase 2 implemented: `WorldRunCatalog` derives 4 normalized stages/world from all 6 existing hordes + final boss; explicit `StageEncounterKind.Elite`; 9 named elites; final-only form unlock/form-swap; `MvpMissionSelection` stage resolution + 1-based smoke override; stage title/READY state and `StageIntroSequencer`; elite warning card.
- Transactional run foundation implemented: committed `RunStageCheckpoint`, atomic+backup `RunSaveStore`, carry health/meter/current+equipped forms/lives/cards/artifacts/score threshold, retry rollback, stage advancement, Continue Run, final completion cleanup, and actor hydration API.
- Phase 3 scoring backbone implemented: deterministic `RunScoreManager`, active-frame/par timing, hit/combo/parry/counter/defeat/damage/death telemetry, extra life every 25k, live score/time HUD, STAGE CLEAR tally + S/A/B/C/D, versioned atomic permanent best score/rank/time/world-completion persistence.
- Validation: 90/90 world-run assertions + 13/13 checkpoint assertions + 10/10 score assertions + 8/8 input + 12/12 combat acceptance = 133 pure passes; active Archive Stage 1 runtime smoke passes horde/horde/elite/results/no-unlock at tick 739; all 12 stages boot clean; Astral final loop passes. Legacy Ryu smoke now completes/unlocks but retains its pre-existing Hadouken-hit diagnostic flake.
- First Phase 5 environment slice implemented in Archive Stage 1: deterministic hazard runtime (`StaticPulse`/`LinearSweep`, delay/warn/active/repeat, movement, player/enemy masks, damage/knockback/hitstun), pooled world-space floor telegraph, 15/16 runtime-approved authored Intake Tram above the center-lane sweep with safe near/far lanes, hazard authoring validation, typed prop drops, health/score pickups, two tactical caches, and deterministic drop chance.
- Final validation now 147 pure passes (8 input + 12 acceptance + 90 ladder + 13 checkpoint + 10 score + 14 hazard) plus active ladder smoke confirms horde/horde/elite/results/hazard/no-unlock at tick 739; C#/scene diagnostics clean.
- Regression recovery gate complete: sequenced boss/elite HUD events restored; pause Move List restored above cinematic layers with correct S-motion/C-crouch labels and nested Escape; live Ryu jab -> strong -> Medium Hadouken route executes. Current matrix: 346 deterministic assertions, Move List/combat/boss/elite smokes green, and 12/12 stages boot clean.
- Phase 4a scene-flow foundation complete: persistent `SceneFlowCoordinator` autoload owns input lock, fade-out, branded loading card, scene replacement/reload, hold, and fade-in. Main Menu world/Continue/Hub, Hub menu/training, Pause retry/menu, and HUD next-stage/retry/restart-world/menu routes all use it. Added real `ArchiveHub.tscn` and lifecycle/route smokes; change, reload, world, hub, menu, and training routes pass.
- Phase 4b Archive Map complete (2026-07-17): the functional four-realm/four-stage/checkpoint model is now presented through a 15/16 illustrated Living Archive navigation chamber instead of the flat dashboard grid. Structural columns are unframed overlays, repeated stage cards sit on a cyan four-node route spine, realm selection updates accents and route state, and the overwrite modal keeps KEEP CURRENT RUN as the safe focused default. Exact initial/World-Warrior/confirmation captures at 720p, 1080p, and 21:9 pass with 16/16 map-model assertions, persistence isolation, and zero errors.
- Phase 4c results flow complete (2026-07-17): Stage Clear, World Complete, and Game Over share a 16/16 real-alpha Living Archive results plate with asymmetric rank/tally chambers, title rail, archive ribbon, and command ledge. Native data/actions remain authoritative; World Complete shows the real final-stage identity and form unlock, Game Over uses contained KO/recovery copy, and replay/restart confirmations retain the safe checkpoint focus. All 12 pure action assertions and seven route smokes pass, including double-action protection, stage advancement/preservation, final-run retirement, confirmations, and cancel. Exact mode/confirmation captures pass at 720p/1080p plus Stage Clear at 21:9. Procedural audio fallbacks now have explicit ownership and teardown, eliminating a reproducible Mono unsafe-reference exit crash.
- Phase 4d pause UI complete (2026-07-17): the default button column and generic nested panels are superseded by a 16/16 real-alpha Living Archive pause record with seven-slot navigation, active-form portrait/run summary, and a distinct right document chamber. Move List, Move Cards, Form Loadout, and Artifacts replace only the document content while navigation remains stable; empty early-run states are intentional. Ryu motion/crouch labels, nested Escape, focus, and final resume all remain verified. Exact five-view sets pass at 720p/1080p, with root/Move List also passing 21:9.
- Phase 4e form-select and Archive shared UI complete (2026-07-17): the generic translucent selector is superseded by a 16/16 real-alpha inheritance frame with a full-body champion aperture, six-window vault, centered current-form identity, movable native cursor, and loadout ribbon. A new nonpersistent UI smoke equips Blank/Ryu/Goku, captures current and selected Goku, cancels, reopens, confirms explicit Goku, and resolves the real deterministic swap; pure selector rules pass 4/4. Exact pairs pass at 720p/1080p/21:9 with clean procedural-audio teardown. All Archive Stage 1–4 content and shared UI gates are now complete at the `177/177` aggregate baseline; World Warrior art is next.
- Phase 5 Archive Stage 2 mechanics, hazard art, layered environment, and shared UI complete: optional per-encounter lane bounds with deterministic smooth transitions, runtime actor/spawn clamping, validator/debug-drawer support; Index Vaults funnels to +/-1.65, opens to +/-2.75 scan/cache chamber, then +/-2.4 elite room. Two non-overlapping neutral `StaticPulse` scan strips retain always-safe center/opposite responses beneath a 15/16 mirrored authored emitter/field family; risky health/meter caches and Cipher Captain background entrance are authored. A 15/16 production environment uses independent far backdrop, dark top-down floor, three-landmark midground, and split edge foreground assets with ordered parallax. Exact calm/live captures prove grounding, fighter separation, lane rhythm, hazards, elite/results/no-unlock at both target resolutions.
- Phase 5 Archive Stage 3 mechanics, hazard art, layered environment, and shared UI complete: reusable `FallingStrike` hazards retain projected circular tells and one impact/cycle beneath a 15/16 authored shelf/data-debris family with 4.4-unit warning descent. The moving center strip carries a 16/16 security-sweep emitter while preserving deterministic timing and safe near/far lanes. Target-independent `HazardZoneImpacted` and `PropExploded` events create a bounded 16/16 persistent decal/fragment family that refreshes per source and clears on encounter changes. Corruption Repository also has asymmetric score/health/meter caches, an all-team radial explosive canister with pooled VFX/audio, and DropIn Overseer Basalt. Its 15/16 production environment uses an asymmetric damaged-vault backdrop, 1.78-percent-light top-down floor, real-alpha three-landmark midground, and split edge containment remnants at ordered `0.72/0.90/1.12` parallax. Exact calm/live captures prove grounding, ivory separation, 4/4 warnings, active hazards, real damage, canister explosion, elite/results/no-unlock at 720p/1080p.
- Art-completeness correction (2026-07-14): mechanics-complete no longer means slice-complete. Audit proved Archive/World Warrior stage reuse, named-elite archetype-sheet reuse, and missing prop/pickup paths. Archive Stages 2-4 now have six technically unique environment images (individual backdrop + floor), and Archive gameplay has seven independent real-alpha sprites (health/meter/data caches, volatile canister, and health/meter/data pickups). Runtime wiring and tests require existing unique paths and distinct hashes rather than tint/procedural fallback. The first rendered capture caught and fixed stale 64px-placeholder metrics on the new 1024px props; alpha bounds now drive calibrated pixel sizes/ground offsets. Stage 2 and Stage 3 full ladder smokes pass mechanically.
- Visual-style correction (2026-07-14): technical integration is not style approval. Runtime captures prove the new Archive Stage 2-4 art and objects drift toward photorealistic/dense hard-surface sci-fi; the glossy HUD frame and flat Archive Map read as a different game from the accepted mannequin/Ryu/Goku anime/cel-shaded fighting-game art. `VISUAL_STYLE_BIBLE.md` now locks mannequin, Ryu, and Goku as canonical anchors, defines category-specific rendering rules and rejection criteria, mandates multi-reference style-locked prompts, pilot-first generation, a 14/16 manual rubric, and runtime cohesion review. The latest Archive art is reclassified as **functional temporary art / style replacement required**. No stage or phase is complete until both technical and style gates pass.
- Style-calibration pilots complete: Archive Raider, health cache, health pickup, lateral Index Vaults environment, Living Archive HUD kit, and strike VFX all pass raw style direction. The first Index Vaults corridor composition was rejected and superseded by a lateral belt-scroll revision, proving attractive output is not accepted when composition fails gameplay.
- First runtime style replacements integrated: `archive_health_cache_style_v2.png` uses measured 0.00114/788 metrics and passes 16/16 with 720p/1080p captures; `archive_health_pickup_style_v2.png` uses final 0.00038/672 metrics and passes 16/16 with isolated real-drop captures at 720p/1080p; `project_mannequin_strike_burst_style_v1.png` is pooled, presentation-only, keeps procedural fallback, and passes 16/16 at 720p/1080p. Archive Raider V2 concept passes 15/16 but remains out of runtime until six identity-locked animation sub-sheets preserve it. Validation: clean build, 133/133 world/hazard assertions, full combat smoke green with authored VFX captures at 720p/1080p, Stage 2 ladder smoke green with distinct cache/pickup captures at 720p/1080p, and all 13 style-manifest records resolve with zero missing references.
- Phase 5 Archive Stage 1 complete (2026-07-17): the rejected legacy neon-city pair is superseded by a 15/16 layered Intake Boulevard family with an asymmetric Living Archive backdrop, quiet dark top-down floor, real-alpha midground, and split edge foreground pieces. Accepted generation metadata retains the mandatory canonical reference stack; a visually viable one-reference GPT floor remains rejected provenance. Exact calm/live 720p and 1080p captures prove fighter grounding, ivory separation, ordered parallax, sharp sampling, Intake Tram operation, elite/results/no-unlock, and zero runtime errors. Current world-run coverage is 140/140; the combined world/hazard baseline is 177/177.
- Phase 5 Archive Stage 4 complete (2026-07-17): the rejected one-panel hard-surface shrine and concentric-ring floor are superseded by a 15/16 layered Knight's Reliquary family with an asymmetric ceremonial backdrop, 1.35-percent-light broken-seal floor, low real-alpha midground landmarks, and split edge oath rails at `0.72/0.90/1.12` parallax. The 16/16 unique Archive Knight 10×9 atlas replaces the tint-only mannequin combat visual for both the final boss and inherited form. A separate 16/16 real-alpha 4×2 intro atlas plus explicit illustrated mannequin/Knight portraits replace the legacy purple intro and procedural matchup cards. A 16/16 progressive phase-spectacle family consumes real wall-break events: phase 2 breaks the left oath tablets, phase 3 advances through the center plinth and right crystal seal, and distinct porcelain/crystal bursts remain presentation-only. Exact idle/cleave, matchup/intro, and burst/settled captures at 720p and 1080p prove full progression and zero errors.
- Phase 4 gameplay HUD and shared screens complete (2026-07-17): the rejected glossy lifebar frame is superseded by a 16/16 real-alpha Living Archive broadcast frame with warm porcelain plates, dark-plum joints, cyan player hierarchy, restrained coral boss hierarchy, blank native-control seats, and a compact center keystone. HUD portraits resolve the active form; bars and telemetry remain bounded. Archive Map, results, pause, and form-select each use distinct approved compositions from the same material grammar. Responsive, lifecycle, transactional, split-screen, and `177/177` aggregate gates pass with zero errors.
- World Warrior Dojo Approach environment complete (2026-07-17): the rejected shared tournament composite is superseded in Stage 1 by a 15/16 layered dusk-dojo family with a seamless side-on compound backdrop, quiet packed-earth floor, three real-alpha practice landmarks, and split shallow edge remnants at `0.72/0.90/1.12` parallax. Backdrop triptych/seam revisions, three failed floor geometries, generated-character midground contamination, and a tall foreground lantern were rejected before promotion. Exact calm/live 720p and 1080p ladders pass horde/horde/elite/results at tick 820 with four visual roots, four panels, sharp sampling, and zero errors. Current aggregate is 142 world plus 37 hazard assertions (179/179). Explicit Rookie/Striker/Grappler review and Pavilion Circuit are next.
- World Warrior Dojo Rookie complete (2026-07-17): runtime review rejected all three legacy base-enemy atlases despite mechanical validity. Rookie copied Ryu's white gi/red headband identity, Striker drifted into a generic tracksuit anime style, and Grappler used prohibited olive tactical kit while its named throw rendered as a punch. Rookie is now superseded by a 16/16 original indigo/saffron rushdown identity. Seven exact-count source sheets compose into a 60-frame real-alpha 10x9 atlas; Quick Palm maps all ten attack columns across exact 12/5/23 phase timing. Idle and active frame 14 pass complete Stage 1 ladders at 720p/1080p, focused world tests pass 144/144, and the aggregate passes 181/181. Original Striker is next, followed by Grappler and Pavilion Circuit.
- World Warrior Pavilion Striker complete (2026-07-17): the generic navy tracksuit atlas is superseded by a 16/16 original tall kick specialist in a vermilion pavilion jacket, indigo collar/calf wraps, saffron piping, charcoal trousers, and ivory shoes. Seven source sheets compose into a 60-frame real-alpha 10x9 atlas after an authored 80000-pixel component threshold excludes one black quadrant divider and two detached shoe artifacts. Turning Kick maps all ten attack columns across exact 16/5/27 phase timing; runtime frame 18 is a full horizontal kick. Idle/active Stage 2 ladders pass at 720p/1080p, focused world tests pass 146/146, and the aggregate passes 183/183. Original Grappler is next, followed by Pavilion Circuit.

## Canonical Visual-Style Lock Across All Remaining Phases

- Shared grammar: confident dark contours, broad 2-4-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, strong silhouettes, fighting-game lighting, and arcade readability.
- Reject photoreal/PBR, rusty/gritty military sci-fi, dense kit-bash microdetail, product-render loot, painterly concept drift, generic mobile-game art, and enterprise/dashboard UI.
- Every generation job uses at least two accepted mannequin/Ryu/Goku references; difficult cross-category work uses all three. Off-style assets may be referenced for layout only and are explicitly non-authoritative.
- Every asset family begins with one pilot. Raw art is reviewed beside accepted characters before cutout, import, processing, or batch generation.
- Style approval requires at least 14/16 on the visual-bible rubric, no automatic failure, explicit review metadata, and 1280x720 plus 1920x1080 runtime captures.
- Phase 1 VFX: graphic anime impact shapes, speed lines, cel smoke/debris, controlled bloom; no photoreal particle fog.
- Phase 4 UI: fighting-game broadcast package fused with the Living Archive - porcelain/angular plates, dark plum joints, cyan archive energy, realm accents, portraits, and dramatic hierarchy; no glossy PBR metal or software dashboard grids.
- Phase 5 content: cel-painted layered fighting-game stages, low-frequency floors, style-matched props/pickups/hazards, and unique elites; world motifs vary but rendering grammar does not.
- Phase 6 accessibility: preserve style while ensuring silhouettes, telegraphs, high-contrast modes, reduced flash, and scalable UI remain readable.
- Phase 7: consistency/optimization only; required style art must already be produced and approved inside each implementation slice.

## Mandatory Art-Completion Gates

A stage or phase cannot be marked complete until all applicable gates pass:

1. Unique stage identity: dedicated backdrop/floor or authored subzone panel set; no source-stage crop/tint presented as a new stage.
2. Unique named-elite runtime sheet/variant with distinct silhouette and animation-ready frames; display name, tint, aura, or stat changes alone do not qualify.
3. Every visible prop, obstacle, cache, canister, and pickup resolves to an existing bespoke sprite; unrelated types cannot share one sprite path and differ only by tint.
4. Every stage-defining hazard has themed set-piece art/effects beyond generic procedural telegraphs, while preserving deterministic collision and accessibility tells.
5. `ResourceLoader` path audit passes with no silent procedural fallback for release-facing assets.
6. Asset hashes are distinct wherever the design promises uniqueness; intentional repeats are documented.
7. Every generated asset records at least two canonical character references, model, prompt, and pilot lineage; no off-style asset seeds another family.
8. Every asset passes the visual-bible rubric at 14/16 or better with no automatic failure; technical correctness alone does not qualify.
9. Imported assets are visually inspected beside accepted characters for alpha, grounding, scale, seams, contour/value cohesion, detail density, contrast, and occlusion.
10. Calm and combat-dense captures pass at 1280x720 and 1920x1080; HUD/map/results also pass fighting-game visual-hierarchy review.
11. Art direction, pilot generation, approval, import, wiring, automated audit, and rendered capture are part of the same implementation slice - not deferred wholesale to a final polish phase.

## Locked Decisions

- Three per-world ladders: Archive Nexus, World Warrior Sector, Astral Battlefront; exactly 4 stages each.
- Stage structure: Stage 1 = two early hordes + named elite; Stage 2 = two mid hordes + named elite; Stage 3 = two late hordes + named elite; Final = short approach + current major boss + form unlock.
- Between stages: carry absolute health and meter unchanged, remaining lives, current form/loadout, move cards, artifacts, run score, and extra-life threshold state. Reset transient combat state, position, guard gauge, cooldowns, and encounter actors.
- Timer is score pressure only; it never causes failure in v1.
- Autosave at committed stage boundaries only; main menu offers Continue Run. Mid-stage app exit resumes from that stage's entry checkpoint.
- Public co-op/split-screen remains deferred; all new data/save/score APIs stay player-safe and existing co-op smoke tests remain.
- Personal-use game: no commercial licensing/replacement-art work.
- No fourth world until all three current worlds reach console-demo quality.

## Verified Current-State Corrections

These observations describe the baseline at plan creation. Completed fixes are
recorded in Implementation Progress and live code; they remain here to preserve
the reasoning behind the plan.

- `Scripts/Data/MvpWorldCatalog.cs` is healthy (31 KB) and contains Worlds, `FindWorld()`, `CreateMission()`, route builders, and `CreateAstralBattlefrontMission()`. An earlier editor read incorrectly reported it empty; clean source build and all world boots prove the catalog baseline.
- `Scenes/UI/MainMenu.tscn` is the configured startup menu. `ArchiveHubScene` previously sent Return to Main Menu to `Scenes/Main.tscn` (combat); Phase 0 now routes it correctly to the menu.
- `RunSessionManager` is a static process-lifetime singleton and already carries `CurrentWorldId`, `RemainingLives`, `CurrentStageIndex`, cards, and artifacts, but `CurrentStageIndex` was unused and health/meter/current form were not stored.
- `StageEncounterKind.Boss` previously implied full intro, `IsBoss` CPU behavior, automatic `Mission.BossFormId` unlock, and `AwaitingFormSwap`. Early named elites could not reuse this path unchanged.
- Reused minion forms typically own `ArcadeEnemyProfile`, while `IsBoss` actors only initialize `CpuFighterBrain`; naively spawning a minion archetype as Boss can leave it with no active AI.
- Breakable props were spawned and `RoleTags=breakable` deaths already created a pickup, but the drop was always `CreateMeterPickup()`; `StagePropData.SpawnsPickupOnBreak` and drop type were not honored.
- Hazards were simulated and HUD events existed, but no source mission definitions authored them.
- P1 was hardwired to keyboard; joypads were assigned P2+. Pause opened only with Escape; reward/route/result shortcuts were keyboard-centric; direct joypad polling omitted Jump.
- `CombatAudioManager` has a 16-player SFX pool and procedural intro stingers, but there is no Music bus/director, stage looping, crossfade, or authored audio asset set.
- `MvpHud` created/freed Label nodes for every floating number; new VFX/score feedback needed pooling to avoid GC hitches.
- Camera already supports smooth follow, dynamic zoom, lane-stable composition, air-height framing, boss focus, and shake. It needs tuning/accessibility/regression QA, not replacement.
- Current normal-stage topology is a forward X-axis corridor with global Z-lane bounds and X-triggered fight rooms. `VerticalSection` is a Y-height/wall-run climb mode (already used by Hollow Archive), not a Streets-of-Rage-style south turn. `RouteChoiceData` supports encounter jumps and is already used by Hollow Archive, but main-ladder branching would undermine comparable score/rank runs.
- Wall-break events already expand final-boss bounds and can swap every background panel to its destroyed texture/tint. This is a strong boss-arena transformation hook, but it is currently global rather than region-specific.

## Stage And Map Design Standard

- Core-ladder topology for v1 stays a readable forward belt-scroll route. No mandatory platforming, instant-death traversal pits, true 90-degree route turns, or branching in the three scored ladders. Existing route choices and vertical climbing remain available for Hollow Archive/optional later modes.
- Each normal stage follows: 5-10 second arrival reveal -> Horde A (teach/read) -> short traversal connector/breather -> Horde B (escalate/remix) -> recovery/cache pocket -> landmark elite arena -> STAGE CLEAR. The Final Stage follows: arrival reveal -> short pressure encounter -> dramatic approach -> transformed boss arena -> major boss/form unlock.
- Every stage has at least three visually distinct route subzones (approach, interior/escalation, elite/boss landmark), one memorable interactive set piece, one recovery decision, and a clear visual destination. Background panels already carry X ranges, so subzones should be authored with distinct panel art/lighting rather than one repeated hero image.
- Add per-encounter lane-bound overrides within the stage-global lane limits, with smooth transition rules, so routes can open wide, funnel narrow, and widen for elites without new topology. Do not place two consecutive arenas with identical width, entry choreography, and camera composition.
- Simulate turning a corner through perspective art, foreground wipes, lighting/music transitions, and lane-width changes while movement remains X-forward. A true path-following camera/route system is deferred unless later playtests show the visual turn is insufficient.
- Guidance is layered: architecture/road direction, lighting and color contrast, enemy entrances, an opening gate/barrier, and an in-world GO marker at the route edge. HUD GO remains a fallback rather than the only directional cue.
- Foreground occluders may sell depth but must fade/shift when they cover a player or telegraph for too long. Interactables and safe lanes must remain readable above background detail.
- Environmental storytelling progresses spatially: outer district -> controlled interior -> corrupted/elite territory -> champion arena. Each stage needs a landmark, stage-specific palette/music cue, authored enemy entrance, and evidence of the Archive/world conflict - not only generic decoration.
- Hazards use a teach -> escalate -> remix pattern. First exposure presents the telegraph with low enemy pressure; later use combines it with crowd control. Every hazard must have a visible spatial tell, unique audio cue, deterministic timing, at least one safe response, and a minimum authored reaction window. It cannot activate under a player spawn or cover the entire walkable lane without an off-window/escape route.
- Neutral hazards should optionally affect enemies so the player can bait/throw foes into them and earn environmental-KO score. Team masks, damage, hitstun/stun, knockback, activation cadence, movement path, and one-shot/repeat behavior are data-authored.
- Core v1 hazard families: pulsing floor zone (electric/corruption), lane sweeper (Archive tram/security beam/training log/ki strafe), telegraphed falling strike (debris/meteor), push zone (conveyor/wind/energy current), and explosive breakable AoE. Runtime state must be separate from immutable authored bounds.
- Breakable placement is tactical: route-side score caches, risky health/meter caches near pressure, explosive props for crowd control, and occasional destruction-state triggers. Ground-weapon pickup/durability is deliberately deferred because it competes with the six-button character-moveset identity; environmental combat comes first.
- Ledges and platform gaps are visual arena framing in v1, not player instant-death traversal. Enemy ring-outs may be revisited only after deterministic knockback, scoring, camera, and retry behavior are proven.

## Phase 0 - Recover A Trustworthy Baseline And Controller Parity

1. **DONE:** Verify the healthy `MvpWorldCatalog` baseline, capture its world/mission IDs, and clean-build current source.
2. **DONE:** Boot Archive, World Warrior, Astral, Training, and Main Menu from current source; all logs clean.
3. **DONE:** Fix `ArchiveHubScene`'s back action to load `Scenes/UI/MainMenu.tscn`.
4. **DONE:** Remove release-facing prototype text: Local 2.5D Fighting Sandbox Prototype, MVP Combat Foundation, P1 Blank Mannequin, Restart Prototype, and Prototype failed; replace with Archive language and actual form names.
5. **DONE:** Persistent selectable P1 keyboard/gamepad, first-connected fallback, reconnect recovery, duplicate-device prevention, and replay/smoke isolation.
6. **MOSTLY DONE:** Complete joypad combat verbs (including previously missing Jump), assigned-device UI router, pause/accept/cancel/navigation/reward actions. Dynamic platform glyph service remains with Phase 4/6 UI theming.
7. **DONE for current screens:** Main menu, hub, pause, form select, reward choice, route choice, and end panels accept keyboard/gamepad; P1 Start pauses; submenus/dialogs restore focus. Future results/confirmations inherit this router.

Phase 0 blocks every later phase.

## Phase 1 - Combat Fluidity, Impact VFX, Audio, And Runtime Budgets

1. Add a pooled `CombatVfxManager` consuming existing transient presentation events. Provide distinct, data-scaled effects for hit, heavy hit, counter/punish, guard spark, parry flash, launcher, wall/ground bounce, landing dust, armor absorb, weak-point burst, form swap, KO, and boss defeat.
2. Extend `CharacterVisualComponent` hit feedback with a brief white/color hit flash on actual damage. Preserve existing parry/guard-break/form effects and expose reduced-flash behavior.
3. Add damage/move-class hitstop tuning in `HitResolver`: short light freeze, stronger heavy/counter/KO freeze. Keep it deterministic and re-baseline timing-sensitive smoke tests. If timing regressions are too disruptive, use a presentation impact hold instead.
4. Add heavy-hit/KO screen punctuation, but route flash intensity and camera-shake scale through accessibility settings from day one.
5. Upgrade combo feedback in `MvpHud`: pooled floating points/damage, color/scale escalation, best-combo tracking, and praise tiers such as NICE/GREAT/SUPER/BREAK without obscuring combat.
6. Add bounce/splat/landing debris effects and review attack recovery/idle transitions, grounding, sprite scale, and frame timing. Prioritize visible snaps, foot sliding, missing get-up/guard-break reactions, and size changes - not simulation acceleration that would weaken input responsiveness.
7. Expand audio architecture: explicit Master/Music/SFX/UI buses; `StageAudioDirector` with two-player loop crossfades; stage, boss, results, and pause/duck states; priority/voice limiting; light/heavy/guard/parry/KO/pickup/UI/form/boss stingers; small deterministic-safe pitch/pan variation.
8. Pool floating labels and transient VFX. Define caps and graceful reuse rather than per-hit allocation/`QueueFree` churn.

Can run in parallel: VFX pooling, authored SFX/music scaffolding, and animation QA after event contracts are agreed.

## Phase 2 - Four-Stage World Ladder, State Transaction, And Continue Run

1. Add `WorldRunData` containing world metadata and a list of four `StageMissionData` instances. Extend `StageMissionData` with `StageNumber`, `StageTitle`, `StageSubtitle`, `ParTimeSeconds`, `IsFinalStage`, and `RequiresFormSwapToComplete`; avoid a redundant `RunStageData` wrapper unless later metadata cannot live on `StageMissionData`.
2. Implement `MvpWorldCatalog.CreateRun(worldId)`. Keep `CreateMission` compatibility for smoke/debug callers, but have `MvpMissionSelection.CreateSelectedMission()` resolve the stage at `RunSessionManager.CurrentStageIndex`. Add a validated stage-index environment override for smoke runs.
3. Append `StageEncounterKind.Elite`. Elite encounters use existing arcade-enemy AI, forced deterministic elite stats, a unique name, palette/aura, at least one signature modifier, a named health bar, and a short warning card. They do not use full CPU boss logic, grant forms, or enter `AwaitingFormSwap`.
4. Decouple boss progression from Kind alone. Only a final/form boss may set `UnlockableFormOnDefeat` and enter `AwaitingFormSwap`. Make objective text, intro style, unlock behavior, and stage completion derive from explicit elite/final metadata.
5. Split each recovered world route: preserve the six original horde encounters and their reward cadence, add three named elite closures, then a short final approach plus the existing Archive Knight/Ryu/Goku boss. Only the final stage uses the full five-phase intro and form inheritance.
6. Use a clean scene reload per stage, behind a transition/loading coordinator. This matches the current bootstrap architecture and safely resets combat actors/stage nodes while the run singleton hydrates persistent state.
7. Extend `RunSessionManager` with an explicit `RunState`/`StageCheckpoint` snapshot containing: active run flag, world/stage, lives, absolute health, meter, current form ID and equipped form IDs, cards, artifacts, score state, and extra-life threshold index. Add a narrow `CombatActor` hydration API that clamps restored health/meter to the restored form.
8. Carry health and meter unchanged as selected. At stage load reset transient state only: position/velocity, current move/combo, invulnerability, temporary cooldowns, guard gauge, encounter actors, and camera. Document max-health/form-change clamping rules.
9. Make stage progression transactional. At stage entry deep-copy a rollback snapshot. Rewards acquired during the stage are working state; Retry Stage restores the entry snapshot so cards/artifacts/score cannot duplicate. Successful clear finalizes results, increments stage index, commits the next-stage checkpoint, then writes autosave exactly once.
10. Add a versioned `RunSaveStore` separate from permanent progression/settings. Write atomically via temp + replace, retain a backup, recover from corruption, and expose Continue Run only for a valid committed checkpoint. Closing mid-stage resumes at stage entry; closing on results resumes at the next stage.
11. Add failure flow: Retry Stage (rollback checkpoint), Restart World, Return to Map/Menu (preserve committed checkpoint), and Abandon Run (confirmation + delete checkpoint). Starting another world while a run exists must prompt Continue/Overwrite/Cancel.
12. Final world completion preserves the existing unlock/shapeshift condition, commits permanent progress/high scores, marks the run complete, and removes the active run checkpoint.

## Phase 3 - Deterministic Arcade Scoring, Time Pressure, Results, And Rank

1. Add a pure deterministic `RunScoreManager` owned by `RunSessionManager`, never by HUD. Feed it authoritative simulation/director events exactly once per tick.
2. Track stage/run score, active gameplay frames, max combo, player damage dealt/taken, enemies/elites/bosses defeated, parries, counter/punish hits, deaths, breakables, pickups, clear bonus, and extra-life threshold progress. Exclude pauses, reward/results overlays, title cards, loading, and boss-intro freezes from clear time.
3. Define data-driven stage score/rank targets because stage lengths and Goku's final fight differ. `ParTimeSeconds` gives a diminishing time bonus but never fails the stage. Rank output is S/A/B/C/D with deterministic boundary tests.
4. Add live score, hi-score, next-extra-life threshold, stage timer/par indicator, and floating +points to the compact HUD. Award extra lives once per saved threshold and persist the next threshold to prevent reload farming.
5. Define immutable `StageResultsData` at clear: base score, combat/style bonus, time/par bonus, max combo, damage taken, deaths, parries, stage rank, cumulative run score, run rank, high-score comparison, and unlocks.
6. Results flow: freeze/lock gameplay -> boss/elite victory beat -> STAGE CLEAR tally/count-up -> rank reveal/stinger -> existing Archive reward/unlock summary -> commit next-stage checkpoint -> NEXT STAGE. Preserve the six existing horde reward choices; do not add duplicate elite rewards.
7. Persist per-world/per-stage best score, best rank, best time, and world completion in a versioned `MvpProgressData` schema. Add NEW RECORD feedback and safe migration from today's form/lore-only save.

## Phase 4 - Professional Game Flow, HUD/UI Theme, And Transitions

1. Add a persistent `SceneFlowCoordinator`/`ScreenTransition` layer to own fade-out, asynchronous/preloaded scene switch, loading/archive-map card, fade-in, and input lock. Replace raw `ChangeSceneToFile`/`ReloadCurrentScene` calls in `MainMenu`, `ArchiveHubScene`, `PauseMenu`, and `MvpHud`.
2. Add an optional PRESS START title gate, then a polished Archive Map with realm art, four stage nodes, lock/clear/current state, best ranks, and Continue Run. Do not expose debug/status-document actions in the normal player flow.
3. Add stage intro overlays: PROJECT MANNEQUIN / STAGE N / TITLE / subtitle -> READY. Major final bosses keep the full VS/cinematic/READY/FIGHT sequence; elites receive a short warning/name card.
4. Add a brief NOW LOADING/archive-map transition between stages and a boss-defeat celebration with controlled slow motion, triumph camera, unlock flourish, and skippable timing after first view.
5. Create a shared UI Theme/style catalog: deliberate font, nine-patch/panel styles, focus rings, button hover/press states, spacing grid, icons, and UI sounds. The current default Godot typography/buttons are a major amateur tell.
6. Reskin `MvpHud` while preserving its useful systems: actual form name; proper life icons; full use of lifebar frame art; compact world/stage/wave/hostile status; score/timer; centered combo praise; clear boss/elite bars; no debug-like sentence strips.
7. Convert completion, game-over, rewards, route choices, and confirmations to focusable modal controls. Raw R/M and 1/2/3 may remain optional shortcuts but never the only path and never act globally outside the relevant state.
8. Validate responsive layout and safe areas at minimum 1280x720, 1920x1080, 2560x1440, 21:9, window resize, and split-screen smoke viewports. Scale/clamp HUD panels instead of assuming one canvas size.

## Phase 5 - Authored Stage Layouts, Hazards, And Archive District Vertical Slice

1. Make Archive District the quality bar from title screen through Stage 4 results. Implement the stage/map design standard above before copying content to the other worlds.
2. Extend `StageEncounterData` with optional arena lane bounds (inside the mission-wide Z limits) and transition duration. Use these to alternate wide crowd-control spaces, narrow pressure corridors, asymmetric prop pockets, and roomy elite arenas while preserving forward X progression.
3. Expand `StageHazardZoneData` into immutable authored definitions plus per-encounter runtime state. Data must cover hazard family, activation delay/window, repeat cadence, movement start/end/pattern, team mask (`Players`, `Enemies`, `All`), damage, hitstun/stun, knockback, warning frames, telegraph VFX/audio/light IDs, and score attribution. Do not mutate authored bounds every frame.
4. Implement five reusable hazard behaviors: pulsing floor, moving lane sweeper, telegraphed falling strike, conveyor/wind push zone, and explosive-prop AoE. Use the existing projectile system for falling strikes where it gives better collision/impact behavior than a zone.
5. Build an in-world hazard-telegraph layer: floor decals/stripes, edge headlights/shadows, projected landing circles, charging light, particles, and spatial audio. HUD warning text is secondary. Telegraphs must remain visible under foreground art and reduced-flash mode.
6. Let neutral hazards affect enemies and award environmental-KO/style score. Author first exposure safely, then remix it with waves; never combine a new hazard with maximum crowd pressure on first appearance.
7. Formalize `StagePropData`: honor `SpawnsPickupOnBreak`, drop type/chance, on-break effect, explosion team mask/radius/knockback, destroyed visual, and optional stage-destruction trigger. Support health, meter, score/archive-data, and tightly scoped temporary buffs; stop unconditional meter drops.
8. Author tactical breakables and recovery pockets around the selected carry-health/meter rule. Put optional caches in near/far lane corners so curiosity and risk are rewarded without branching the route.
9. Add localized destroyed-panel groups rather than globally swapping every panel on any wall break. Boss phase changes may open the arena, destroy foreground/midground groups, shift lighting, and crossfade music without exposing black/seams.
10. Choreograph enemy entrances using existing Left/Right/Near/Far, WalkIn/DropIn/Foreground/Background/Ambush profiles: break through a door, descend from a roof, emerge from foreground, or surround after a gate closes. Each stage needs at least one signature entrance.
11. Add an in-world GO destination/gate-open effect at the right edge, short traversal breathers, and location transitions via panel art/foreground wipes. Main scored ladders remain linear; Hollow Archive retains route-choice and true vertical-climb experimentation.
12. Add foreground occluders, signs, crowd/ambient animation, landmarks, route-specific lighting, stage-specific music, and destruction states through the existing four stage layers. Occluders fade when hiding actors or telegraphs.
13. Preserve the six current horde reward decisions across Stages 1-3. Present reward cards as Archive extraction/data choices with gamepad focus and clear rarity/synergy feedback.
14. Add contextual tutorials only in the first Archive run: movement/depth, six attacks, jump, guard/parry, special/super, form swap, reward, breakable, hazard safe lane, and GO marker. Track first-run flags.
15. Archive Nexus concepts:
    - Stage 1, **Intake Boulevard**: open/wide approach -> two readable hordes -> Archive courier/tram sweeper taught with clear headlights/safe lane -> Scout elite at a glowing intake obelisk.
    - Stage 2, **Index Vaults**: alternating narrow stacks and wider cache pocket -> pulsing corruption/scan-floor strips -> Raider captain in a collapsed data-terminal chamber.
    - Stage 3, **Corruption Repository**: asymmetric breakable caches, falling shelf/data debris, then a moving security sweep remixed with enemies -> Bruiser overseer at the shattered vault landmark.
    - Final, **Knight's Reliquary**: short pressure approach -> quiet pre-boss reveal -> Archive Knight; later phases use controlled wall-break/destruction groups, debris, lighting, and music escalation rather than cheap phase-one hazard damage.
16. World Warrior concepts:
    - Stage 1, **Dojo Approach**: broad street/courtyard, minimal hazard pressure, breakable training props, Rookie champion at a dojo gate.
    - Stage 2, **Pavilion Circuit**: lane-width funnels, telegraphed rolling training-log sweeper/falling practice props that can hit either side, Striker champion beneath honor banners.
    - Stage 3, **Grand Tournament Floor**: spectator landmark, foreground crowd reactions, risky score caches, remixed lane sweeper or timed ring-side shockwave, Grappler title-holder in a wide arena.
    - Final, **Champion's Courtyard**: torch/weather/music transition and destructible arena panels, but no external damage hazard during the Ryu duel so its fighter fundamentals remain the climax.
17. Astral Battlefront concepts (reuse the existing Skyfall/Capsule/Energy Rail/Tournament Summit visual sequence):
    - Stage 1, **Skyfall Breach**: wide lanes and a slow telegraphed ki-strafe sweeper with safe depth lane; Saibaman/Scout elite at the mothership vista.
    - Stage 2, **Capsule Causeway**: projected falling-debris/meteor strikes and optional health cache near danger; Frieza elite at a broken capsule landmark.
    - Stage 3, **Energy Rail Convergence**: narrow/wide rhythm with energy-current push zones plus pulse-floor remix; Ki Captain/Heavy elite at the rail nexus.
    - Final, **Tournament Summit**: short aerial-pressure approach then Goku; phase-driven sky, lighting, debris, music, and localized arena destruction escalate the 12 forms. No player instant-death gaps.
18. Pace targets: normal stage 3-5 minutes, final 4-7 minutes, full realm 16-24 minutes. Review Goku phase repetition through grouped music/VFX/arena states and move-pattern escalation rather than removing forms.
19. Replicate only after Archive passes capture/QA. World Warrior and Astral must use distinct hazard language, entrance choreography, pickup balance, landmarks, music, and set pieces - not palette-swapped Archive layouts.
20. Explicitly defer ground-weapon pickups/durability, main-ladder branching, true south-turn path routing, traversal pits, and mandatory platforming. Reassess only after the forward ladder is polished and score-comparable.

## Phase 6 - Options, Accessibility, Save Robustness, And Meta Surfacing

1. Add `SettingsStore` separate from progression/run saves. Include Master/Music/SFX/UI volume, fullscreen/windowed, resolution/render scale, VSync, HUD scale/safe-area, shake intensity, reduced flash, high-contrast telegraphs, hold/toggle block, and control rebinding/reset.
2. Ensure visual cues never rely on color alone: icons/text/patterns accompany health, guard, hazard, parry, weak-point, and rank feedback. Reduced-flash/shake settings must affect Phase 1 effects globally.
3. Add input glyph switching based on active P1 device and graceful reconnect/fallback. Keep co-op device APIs compatible but do not expose public co-op modes in this pass.
4. Extend permanent-save robustness: schema version, migration, atomic writes, backup recovery, reset-data confirmation, and a brief save indicator after stage commits/unlocks.
5. Upgrade ArchiveHub to surface world ladder completion, best ranks/scores/times, unlocked forms/lore, active Continue Run, training access, and replay stage selection after world completion.
6. Add difficulty/assist hooks without requiring a full new mode: enemy damage/health presets, optional guard/parry timing assist, and tutorial recommendations. Full localization, screen-reader support, online play, and broad narrative expansion remain out of scope.

## Phase 7 - Final Animation, Camera, And Performance Pass

Art production is no longer deferred to this phase. Every Phase 5 content slice
must satisfy the mandatory art-completion gates as it is implemented. Phase 7
only performs cross-project consistency, final replacement/revision, and
optimization.

1. Audit the already-authored Archive/World Warrior/Astral stage bands, props, foregrounds, title/map art, elite variants, telegraph poses, and boss spectacle for visual consistency; revise failures rather than first creating required runtime art here. Personal-use Ryu/Goku art remains.
2. Run animation contact-sheet and 60-fps capture QA for player, nine enemy archetypes, three elites/world, and three final bosses: grounding, scale, walk cycles, attack recovery, get-up, block/parry, KO, and intro timing.
3. Tune camera profiles per stage/boss using existing follow/zoom/air framing; verify no exposed seams during boss arena expansion, no motion sickness, and accessible shake at zero.
4. Profile real gameplay at 1080p/60 and split-screen smoke. Budget: steady frame time below 16.7 ms on target hardware, no transition/combo hitch above 33 ms, bounded pooled floating texts/VFX, bounded `Sprite3D`/background panels, and priority-limited audio voices.
5. Add asset validation for every mission/form/move atlas: path exists, import valid, frame count/metrics sane, no corrupt `.ctex`, no missing audio, no uniqueness-required duplicate hash, and no orphan stage metadata.

## Relevant Files

- `Scripts/Data/MvpWorldCatalog.cs` - recover baseline, then author `WorldRunData`/`CreateRun` and 12 stages.
- `Scripts/Data/WorldMissionData.cs` - stage metadata, Elite kind, explicit final/form progression, per-encounter lane bounds, pickup/prop drops, hazard families/runtime parameters, and audio metadata.
- `Scripts/Data/MvpMissionSelection.cs` - select current run stage and stage-index smoke override.
- `Scripts/Progression/RunSessionManager.cs` - full run state, score manager, transactional stage snapshots.
- `Scripts/Progression/MvpProgressStore.cs` - schema migration, best scores/ranks, atomic permanent progress.
- New `RunSaveStore`/`SettingsStore` - boundary autosave/Continue Run and user settings.
- `Scripts/Core/MvpCombatBootstrap.cs` - hydrate form/health/meter/loadout and stage entry state.
- `Scripts/Combat/CombatActor.cs` - narrow run-resource hydration API; preserve private invariants.
- `Scripts/Stage/ArcadeEncounterDirector.cs` - Elite path, generic stage completion, explicit form unlock gate, score telemetry, per-encounter lane bounds, prop drops, and deterministic hazard runtime.
- `Scripts/Data/HazardRosterFactory.cs` - health/meter/score pickups, breakable and explosive prop archetypes.
- New `StageHazardPresentation`/pooled stage-effect controller - floor/sweeper/strike/push telegraphs, activation VFX/audio, and in-world GO marker.
- `Scripts/Core/CombatPresentationEvent.cs` - append new presentation-only event values late.
- `Scripts/Presentation/CharacterVisualComponent.cs` - actual hit flash and animation transition QA.
- `Scripts/Presentation/PrototypeStageView.cs` - existing camera/lighting hooks, route-layer transitions, localized destroyed groups, foreground occluder fading, victory focus, and effect settings.
- `Scripts/Presentation/CombatAudioManager.cs` plus new `StageAudioDirector` - authored SFX and music state/crossfade.
- `Scripts/UI/MvpHud.cs` - pooled feedback, score/timer, compact HUD, results and controller-safe modals.
- `Scripts/UI/MainMenu.cs` / `ArchiveHubScene.cs` / `PauseMenu.cs` / `FormSelectOverlay.cs` - navigation, Continue Run, confirmations, settings, device-safe focus.
- `Scripts/Input/LocalInputManager.cs` - P1 assignment, complete joypad mapping, reconnect/rebind support.
- `Scenes/UI/MainMenu.tscn` / `Scenes/Main.tscn` / `project.godot` - main flow, persistent transition/settings services, audio buses.
- `Scripts/Stage/StageMissionValidator.cs` - four-stage run validation, elite/final rules, per-encounter lane bounds, hazard-safe-lane/reaction-window checks, prop/drop integrity, and required metadata/assets.
- `Scripts/Debug/StageDebugDrawer.cs` - visualize authored encounter lanes, hazard paths/windows, safe lanes, prop pockets, and in-world guidance anchors during authoring.
- `Scripts/Debug/**` - ladder, score, save, input, presentation, map-layout, hazard, performance, and world smoke scenarios.

## Verification

1. Baseline gate: clean `dotnet build ProjectMannequin.csproj -nologo`; all three world boot/smoke runs pass after catalog recovery.
2. Determinism: presentation effects never mutate combat; new enum values append late; score/checkpoint fingerprints match replay; hitstop changes get explicit baseline updates.
3. Ladder: each world validates exactly four stages; each stage has one elite/final closure; only finals grant forms/require swap; stage clear advances index; final completes world.
4. Transaction/save: stage retry restores exact entry health/meter/form/lives/cards/artifacts/score; no duplicate rewards/extra lives; boundary save resumes correctly after app restart; corrupt saves recover from backup.
5. Score: pure tests cover time bonus, combo/parry bonuses, damage/death penalties, extra-life thresholds, ranks at boundaries, and cumulative world total.
6. Input/UI: complete run from boot through results using keyboard only and assigned gamepad only; include Jump, pause, form select, rewards, confirmations, Continue Run, and disconnect fallback.
7. Presentation smoke: stage title, READY, room lock/open, elite warning, boss intro, hit/guard/parry/armor/weakpoint VFX, reward, hazard warn/damage, in-world GO, victory, form unlock, results, and next-stage transition.
8. Map/layout validation: each normal stage has three visual subzones, one landmark, one interactive set piece, one recovery decision, valid lane overrides, and an elite/final closure; no prop or active hazard blocks the only route.
9. Hazard simulation: deterministic tests cover warning/active/off windows, moving sweeper path, pulse/falling/push/explosion families, team masks, enemy environmental KO score, retry reset, and stage-boundary cleanup. Validator enforces minimum reaction time, a safe lane/escape window, movement inside stage bounds, and no active spawn overlap.
10. Readability capture: verify telegraphs and safe lanes at all supported resolutions, with reduced flash, behind foreground occluders, during maximum enemy density, and at zero screen shake.
11. Manual capture: first three minutes of each realm, one elite, final boss intro/defeat, recovery pocket, breakable/drop, neutral hazard bait, landmark/set piece, stage clear, game over/retry, Continue Run; 1080p 16:9 plus resolution matrix.
12. Performance: sustained combo/VFX/hazard test produces no unbounded node growth or recurring GC hitch; stage transitions hide loading; music/SFX remain unclipped and prioritized.

## Scope Boundaries

- Included: professional single-player three-world experience, controller parity, boundary Continue Run, score/rank, stage ladder, environment richness, options/accessibility baseline.
- Deferred: public co-op/split-screen modes, online play, fourth world, timer-fail mode, ground-weapon inventory/durability, true path-turn routing, main-ladder branching, traversal pits/mandatory platforming, major narrative expansion, and full localization/screen-reader implementation.
- Preserve compatibility: existing Hollow Archive route choices/vertical climb and split-screen/isolation smoke tests remain green; run/score/hazard data is player-index/team-mask aware for future co-op.
