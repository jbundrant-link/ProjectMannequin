# Castagne-Inspired Fighting-Game Roadmap

This roadmap adapts useful fighting-game architecture patterns from Castagne
while preserving Project Mannequin's Godot 4 C# fixed-tick combat simulation,
custom 2.5D combat boxes, Quiver-inspired side-scroller flow, and existing
boss/archive progression.

Reference:

- https://castagneengine.com/
- Reviewed site state: 2026-07-04

No Castagne source code, editor code, scripts, or assets are included. Castagne's
current public site describes the older Godot 3 version as usable and the newer
Rust version as still in progress, so this roadmap treats Castagne as a design
reference rather than a direct dependency or port target.

## Direction

- Keep Project Mannequin authoritative combat in C# and fixed simulation ticks.
- Use Castagne's strongest ideas: modular combat rules, precise attack data,
  designer-editable move definitions, input/motion tooling, combo tests,
  replayable validation, and frame-focused debugging.
- Do not let animation, Godot physics, or presentation events decide combat
  outcomes.
- Do not replace the completed side-scroller foundation; boss and duel rules sit
  on top of it.
- Keep the implementation clean-room: study Castagne concepts, then write
  original Project Mannequin systems.
- Treat external fighting-game terms as design shorthand only. The first
  Gemini-proposed feature slice maps to original Project Mannequin terms:
  Reflect Guard (Z-Reflect-style parry pushback), Phase Burst
  (Sparking-Blast-style boss transition burst), Rush Throw
  (Dragon-Rush-style guard break throw), and Animation Driver
  (AnimationTree/AnimationPlayer presentation control).

## Task 1: Fighting-Layer Architecture Contract

Status: Implemented

- Define where normal side-scroller combat ends and fighting-game boss/duel
  rules begin.
- Document which systems remain shared: `CombatActor`, `CombatStateMachine`,
  `MoveData`, `HitResolver`, projectiles, presentation events, HUD, and smoke
  scenarios.
- Add explicit mode flags for horde combat, boss duel combat, training, and
  future versus tests.
- Ensure boss-mode hooks cannot bypass the existing custom combat resolver.
- Define the ownership boundary between simulation, AI, camera, animation, HUD,
  and debug tooling.
- Add the Phase 5 feature-slice contract so Reflect Guard, Phase Burst, Rush
  Throw, and Animation Driver upgrades are integrated as modular fighting-layer
  systems instead of one-off edits to unrelated classes.
- Verify that Archive Knight, Ryu, and Goku still run through the same legal
  fixed-tick move pipeline.

Implemented via:

- `CombatMode` enum (`Horde`, `BossDuel`, `Training`, `Versus`) as the single
  fighting-layer rule-set identifier.
- `ArcadeEncounterDirector.IsBossDuelActive` and `ActiveCombatMode` as the single
  source of truth for boss-duel state.
- `GameSimulation.CurrentMode`, derived from the director, so every system runs
  through one shared fixed-tick pipeline and the single `HitResolver`.
- Debug overlay `Mode:` readout for manual verification.

## Task 2: Castagne-Style Attack Data Pass

Status: Implemented

- Audit existing `MoveData` fields against Castagne-style attack concepts:
  startup, active, recovery, hitstop, blockstop, hitstun, blockstun, guard type,
  invulnerability, armor, meter gain, chip, proration, and cancel rules.
- Add missing explicit fields only where the current data model is ambiguous.
- Separate gameplay attack data from animation/presentation names.
- Add authoring validation for illegal frame timings, impossible hitboxes,
  invalid cancel targets, and unsafe meter costs.
- Generate a frame-data report for every playable/boss form.
- Verify the report covers mannequin, Archive Knight, Ryu, and Goku moves.

Implemented via:

- `MoveData.BlockStopFrames` (default -1 = derive) to de-ambiguate blockstop,
  wired into `HitResolver` with a backward-compatible default.
- `MoveValidation` (pure/testable): flags illegal frame timings, impossible
  hitbox windows, invalid cancel targets, and unsafe meter costs.
- `FrameDataReport`: startup/active/recovery, best-case on-hit and on-block
  advantage, and cancel routes, followed by validation issues.
- `RosterCatalog.ReferenceForms()` covering mannequin, Archive Knight, Ryu, and
  Goku (boss and archive forms).
- Run headless with `PROJECT_MANNEQUIN_FRAME_DATA_REPORT=1`; current data reports
  0 validation issues across ~170 moves.
- Armor and chip damage are deferred to Task 4, where they gain behavior rather
  than becoming unused data fields.

## Task 3: Input Grammar And Motion Tooling

Status: Implemented

- Preserve the current bitmask input buffer and command interpreter.
- Add Castagne-style input grammar documentation for direction/button notation,
  command priority, leniency, charge windows, and simultaneous button rules.
- Add deterministic command-resolution tests for overlapping commands such as
  `236LP`, `236HP`, `22HP`, crouching normals, parry, block, and supers.
- Add debug readouts for consumed command, matched input history, and rejected
  higher-priority commands.
- Support future designer-authored command aliases without changing core input
  code.
- Verify keyboard and gamepad command behavior for one through four local
  players.

Implemented via:

- Full grammar reference documented on `CommandInterpreter` (notation, priority,
  leniency, simultaneous buttons, and the charge-motion limitation).
- `CommandInterpreter.Diagnose(...)` + `CommandCandidate` expose the ranked
  evaluation (consumed match and rejected higher-priority candidates) without
  touching the gameplay hot path.
- `InputGrammarTests` deterministic harness (8 cases: 236LP/LP, 236HP, 236HK
  super-by-priority, 22HP, crouch normals, consumption). Run headless with
  `PROJECT_MANNEQUIN_INPUT_GRAMMAR_TEST=1`; 8/8 pass.
- Debug overlay `Cmd P1:` readout showing the consumed command and rejected
  higher-priority notations for the human player.

## Task 4: Hit Resolution And Defensive Mechanics

Status: Implemented

- Make the resolver order explicit and testable: dead/invulnerable, armor,
  throw, throw tech, parry, block, clash, hit, counter-hit, punish-counter.
- Add Reflect Guard as a data-driven parry/reflect profile that can push the
  attacker away, reward meter, trigger distinct presentation, and still respect
  parry timing, recovery, and punish windows.
- Add Rush Throw as a throw/guard-break mechanic that beats passive blocking,
  has authored startup/range/lane rules, and supports future throw-tech windows.
- Add armor and weak-point rules for boss phases without granting illegal state
  skips.
- Add throws and throw-tech windows that work in 2.5D lanes.
- Extend projectile and beam clashes into authored clash rules without requiring
  rollback or network play.
- Add wall bounce, ground bounce, knockdown, wake-up, and air-recovery rules as
  data-driven mechanics.
- Verify all defensive mechanics through deterministic smoke tests and HUD/debug
  events.

Implemented so far:

- `HitResolution` module: `DefenseOutcome`, `DefenseSnapshot`, canonical
  `Classify(...)` precedence, and `CanonicalOrder`, documented as the order
  `HitResolver` implements. `HitResolver.Resolve` now carries that order as XML
  documentation.
- Data-driven Reflect Guard: `CharacterData.ParryReflectPushback` (-1 = derive)
  and `HitResolution.ResolveReflectPushback(...)`, wired into `ResolveParry`
  (preserves the historical 8 / 16 pushback by default).
- Confirmed Rush Throw's core already holds: throws bypass block/parry via
  `AttackHeight.Throw` in `CanBlockAttack`/`CanParry`.
- Data-driven armor: an active `ArmorBox` absorbs strikes to chip and the
  defender keeps its state (`CombatBoxDefinition.ArmorChipScale`,
  `CombatActor.HasActiveArmor`/`ApplyArmorChip`, `HitResolution.ResolveArmorChip`,
  `ArmorAbsorbed` event). Throws bypass armor.
- Data-driven weak-point: `CombatBoxDefinition.WeakPointDamageMultiplier` applied
  to `WeakPointBox` hits via `HitResolution.ResolveWeakPointDamage`. Both stay
  inert on current content (defaults preserve behavior).
- Data-driven knockdown and ground bounce: `MoveData.CausesGroundBounce`,
  `CausesHardKnockdown`, `KnockdownFrames` (-1 = derive) with
  `HitResolution.ResolveKnockdownFrames`/`ResolveGroundBounceVelocity`;
  `CombatActor` consumes a pending bounce at landing (emits `GroundBounce`),
  otherwise knocks down. Defaults preserve the historical 40-frame knockdown.
- Data-driven wall bounce: `MoveData.CausesWallBounce` +
  `HitResolution.ResolveWallBounceVelocity`; a wall-bounce-pending actor rebounds
  off the stage bounds in the director clamp (emits `WallBounce`).
- Wake-up invulnerability is now real and auto-expiring
  (`GameConstants.WakeupInvulnerabilityFrames` via `InvincibilityFrames`), fixing
  a latent bug where knockdown wake-up left actors permanently invulnerable.
- Air recovery (tech): while airborne and descending, a fresh button press lets
  a player recover from a launch with brief invulnerability (emits `AirRecovery`).
- Verified present: Rush Throw's authored throws (Ryu shoulder/back throw) bypass
  block/parry; the projectile/beam clash system (`ResolveProjectileClashes`,
  `ClashEligible`/`ClashStrength`).
- `DefensiveMechanicsTests` (pure) via `PROJECT_MANNEQUIN_DEFENSE_TEST=1`; 22/22
  pass. Existing combat smoke (`PROJECT_MANNEQUIN_COMBAT_SMOKE_TEST=1`) still
  passes, confirming no regression from the hitstun/wake-up changes.

Notes / minor follow-ups:

- `HitResolution.Classify` remains the tested specification of the canonical
  order; a textual reorder of the resolver is unnecessary because the defensive
  states are mutually exclusive and dead/invulnerable short-circuits in
  `ApplyHit`.
- Throw-tech is deferred: it needs a dedicated grab-interaction subsystem rather
  than the current unblockable-throw model, so it is out of scope for this
  defensive-mechanics pass.


## Task 5: Combo System, Proration, And Pressure Rules

Status: Implemented

- Turn the current combo scaling into an inspectable combo-rule module.
- Add per-move proration, minimum scaling, starter penalties, super floors, and
  repeat-move decay.
- Track combo states across ground, air, launch, bounce, projectile, and assist
  hits.
- Add frame-advantage and pressure reports for hit, block, whiff, and cancel
  outcomes.
- Add invalid-loop detection for repeated launchers, permanent hitstun, and
  infinite meter routes.
- Verify representative BnB routes for mannequin, Ryu, Goku, and boss archive
  forms.

Implemented via:

- `ComboRules` pure module: positional decay, starter proration (applied to
  follow-ups only), repeat-move decay, and a floor (raised for supers). Defaults
  reproduce the historical scaling exactly.
- `MoveData.ProrationScale` (default 1.0); `CombatActor` tracks the combo's
  starter proration and per-move repeat count and feeds `ComboRules`.
- Combo state already spans ground/air/launch/bounce/projectile via the shared
  `GetNextComboDamageScale`/`NotifySuccessfulHit` path.
- `FrameDataReport` now includes on-hit, on-block, and whiff (punish-window)
  columns; cancel routes are listed per move.
- `MoveValidation` flags infinite-combo risks: launcher self-cancel, launcher
  near-1.0 minimum scale, and out-of-range proration.
- `ComboRulesTests` via `PROJECT_MANNEQUIN_COMBO_TEST=1`; 12/12 pass (decay,
  floor, super floor, proration, repeat decay, and a monotonic BnB route). Frame
  data still reports 0 validation issues; combat smoke still passes.

Fixed during Task 5: the boss-duel smoke test previously reported
`counter=False punish=False`. Root cause was that the deterministic duel
scenario ran during the arcade director's `BossIntro`, which freezes the
simulation, so the counter/punish probe strikes never advanced. `StepSimulation`
now skips the boss-intro freeze while the boss-duel scenario is active; the test
passes (`counter=True punish=True`).

## Task 6: Boss Duel Mode And Arena Rules

Status: Implemented

- Add a dedicated boss-duel rules profile layered over completed side-scroller
  rooms.
- Support tighter arena bounds, facing locks, lane-depth constraints, camera
  composition, and duel-specific recovery pacing.
- Keep multi-player boss archetypes compatible with Classic, Isolated Duel, and
  Tag-Team encounters.
- Add Phase Burst as an authored boss-transition event that can create a burst
  hitbox, knock players away, pause or focus the camera, and apply phase-specific
  boss buffs through data rather than hardcoded health-bar logic.
- Add authored boss phase modules for armor, weak points, desperation supers,
  phase hazards, and team mechanics.
- Ensure boss rules still respect player lives, pickups, hazards, unlocks, and
  stage completion.
- Verify all official bosses can enter and exit duel mode cleanly.

Implemented so far:

- Data-driven Phase Burst: `BossPhaseData` now authors the transition burst
  (`TriggersPhaseBurst`, `PhaseBurstPushback`, `PhaseBurstHitstunFrames`,
  `PhaseBurstDamageBuffPercent`, `PhaseBurstDefenseBuffPercent`,
  `PhaseBurstBoundsExpansion`). `ArcadeEncounterDirector` reads these instead of
  hardcoded 25/25/25/40/8 values; `PhaseBurst.ResolvePushVelocity` computes the
  knock-away. Defaults reproduce the historical burst, so all bosses are
  unchanged.
- `BossDuelModeTests` via `PROJECT_MANNEQUIN_BOSS_MODE_TEST=1`; 5/5 pass.
- `CombatMode.BossDuel` (Task 1) already marks duel state via
  `ArcadeEncounterDirector.IsBossDuelActive`.
- Verified existing coverage: Classic / Isolated Duel / Tag-Team archetypes,
  target locks and `MergeAtPhase`, and armor/weak-point phase modules (Task 4).
  Astral (12 Goku phases), World Warrior, and the boss-duel scenario all pass,
  confirming bosses enter and exit cleanly and still respect unlocks and stage
  completion.
- Data-driven boss-duel rules profile (opt-in per encounter):
  `StageEncounterData.DuelFacingLock` and `DuelLaneHalfDepth`, applied by
  `ArcadeEncounterDirector.ApplyDuelRules` while `IsBossDuelActive`, with the
  pure `DuelRules` helper (facing toward the opponent, lane-depth clamp).
  Defaults are off so ordinary rooms are unchanged.
- `BossDuelModeTests` via `PROJECT_MANNEQUIN_BOSS_MODE_TEST=1`; 10/10 pass (Phase
  Burst push + duel facing/lane rules).

Notes / minor follow-ups:

- Duel-specific recovery pacing and camera zoom framing beyond the existing
  arena camera are a tuning follow-up.
- Desperation supers and phase hazards can be authored as explicit phase modules
  on top of the existing `BossPhaseData` and hazard systems.

## Task 7: Fighting AI MechMods

Status: Implemented

- Treat boss AI behaviors as modular Castagne-like mechanics rather than one
  monolithic decision function.
- Split current CPU decisions into reusable modules: neutral, anti-air, punish,
  guard, parry-bait, throw, projectile zoning, flight, armor phase, and super.
- Add a Rush Throw AI module that becomes more likely when an opponent has held
  block or remained defensive, but still obeys reaction delay, range, lane,
  startup, recovery, and mistake-chance fairness rules.
- Add a Phase Burst AI/pacing hook so bosses recover from transition bursts into
  intentional phase-specific neutral rather than attacking immediately.
- Add phase-authored AI module enablement and weighting.
- Add fairness validation so AI cannot read raw inputs, skip recovery, ignore
  meter, or create invulnerability outside authored move data.
- Add debug output for selected module, rejected modules, target state, and
  current phase modifiers.
- Verify Archive Knight, Ryu, and Goku retain distinct boss personalities.

Implemented so far:

- `CpuFighterBrain` is already a module chain of `EvaluateXxx` intents (neutral,
  guard, parry, anti-air, punish, approach, retreat, throw); `GetAvailableMoves`
  already enforces fairness (phase move pool + meter + legal state).
- Rush Throw module is now data-driven and held-block-scaled: the guard-break
  throw ramps from `RushThrowBaseChance` up to `+RushThrowMaxBonus` over
  `RushThrowRampFrames` as the opponent keeps blocking
  (`AiModules.ResolveRushThrowChance`, tracked via block-since tick).
- Phase Burst pacing hook: `BossPhaseData.PhaseBurstRecoveryFrames` extends the
  intentional-neutral window after a transition
  (`AiModules.ResolvePhaseNeutralUntil`, wired into `TryAdvanceBossPhase`).
- Fairness invariant `AiModules.IsMoveLegalForPhase` (matches the brain's
  existing phase-pool filtering); throw decisions now carry a `[RushThrow]`
  debug reason with the held-block frame count.
- Phase-authored module enablement: `BossPhaseData.DisabledAiModules` +
  `AiModules.IsModuleEnabled` gate the discretionary anti-air and Rush Throw
  modules per phase (defaults keep them enabled).
- Fixed the CPU fighter smoke test: its guard (tick 20) and anti-air (tick 100)
  probes ran during the director's BossIntro and were skipped by the
  `encounterActive` guard. Probes are now scheduled relative to encounter-active
  start; `block=True anti_air=True punish=True approach=True`.
- `AiModulesTests` via `PROJECT_MANNEQUIN_AI_TEST=1`; 14/14 pass. Defaults
  reproduce the historical behavior.

Notes / minor follow-ups:

- Per-phase module weighting is currently expressed through the existing
  `AggressionMultiplier` / `DefenseMultiplier`; a dedicated weighting table and
  formal selected/rejected-module debug output are refinements.
- Module enablement can extend to parry and jump-evade with the same pattern.

## Task 8: Training, Replay, And Combo Tests

Status: Implemented

- Add a training-mode scene or debug mode that can spawn a player, dummy, boss,
  and selected stage arena.
- Add dummy settings: stand, crouch, jump, guard all, guard after first hit,
  mash jab, parry attempt, reversal, and record/playback.
- Add deterministic replay capture for inputs, actor states, RNG seed, selected
  mission, selected forms, and frame count.
- Add combo unit tests inspired by Castagne's testability goal.
- Add regression tests for known boss routes, projectile clashes, blockstrings,
  guard breaks, throws, and wake-up states.
- Verify failures print frame numbers and move IDs, not only pass/fail text.

Implemented via:

- Combo unit tests: pure `ComboRouteSimulator` faithfully models the live combo
  scaling (starter proration to follow-ups, repeat-move decay per move id).
  `ComboRouteTests` verify representative BnB routes and, on failure, print the
  per-hit breakdown (position, move id, scale, damage) rather than a bare
  pass/fail. Run headless with `PROJECT_MANNEQUIN_COMBO_ROUTE_TEST=1`; 7/7 pass.
- Training mode + dummy settings: pure `TrainingDummyController` /
  `TrainingDummyBrain` (`Scripts/Combat/TrainingDummyController.cs`) resolve all
  eight authored behaviors (stand, crouch, jump, guard all, guard after first
  hit, mash jab, parry attempt, reversal) as ordinary player inputs, so the
  dummy flows through the exact same state machine as a human. `CombatActor`
  feeds a training input buffer when a `TrainingBrain` is attached. Launch with
  `PROJECT_MANNEQUIN_TRAINING_DUMMY=<setting>` to spawn a player + dummy in the
  Simulation Deck arena. `TrainingDummyTests`
  (`PROJECT_MANNEQUIN_TRAINING_TEST=1`) cover the decision logic (16/16), and
  `TrainingDummySmokeScenario` (`PROJECT_MANNEQUIN_TRAINING_SMOKE_TEST=1`)
  confirms the live wiring drives real moves/states (mash-jab, guard-all, and
  jump all verified). Record/playback of a training session is provided by the
  replay system below.
- Deterministic replay capture: pure `ReplayRecording` module in
  `Scripts/Core/ReplayRecording.cs` captures the RNG seed, mission/world,
  per-player forms, frame count, every tick's per-player input mask, and
  periodic simulation state fingerprints (FNV-1a over each actor's
  position/velocity/health/meter/state/move, floats quantized). It serializes
  to a compact, human-diffable text format (`ReplaySerializer`) and plays back
  through `ReplayInputSource`. `ReplayTests`
  (`PROJECT_MANNEQUIN_REPLAY_TEST=1`) cover round-trip, playback fidelity, and
  fingerprint determinism/sensitivity; 14/14 pass.
- Runtime record/play wiring: `PROJECT_MANNEQUIN_REPLAY_RECORD=<path>` (with
  optional `PROJECT_MANNEQUIN_REPLAY_FRAMES`) records a live session to disk;
  `PROJECT_MANNEQUIN_REPLAY_PLAY=<path>` overrides input with the recording and
  re-verifies the stored fingerprints tick-for-tick. A record→play run with
  matched setup reproduces every state check exactly (5/5, passed=True),
  confirming the simulation is fully deterministic across processes (no
  unseeded RNG: CPU brains use seeded xorshift, wave spawns use seeded
  `System.Random`).
- Regression coverage is provided by the existing headless smoke scenarios and
  is now green: boss routes (World Warrior, Astral, boss-duel), projectile/beam
  clashes (Astral), blockstrings and guard breaks (boss-duel), throws and
  counter/punish (boss-duel), and CPU behaviors (guard, anti-air, punish,
  approach). Several were fixed this pass (counter/punish and CPU guard/anti-air
  BossIntro-timing bugs). These logs already print ticks, phase ids, and move
  ids.

Notes / follow-ups:

- An in-game training-mode UI (settings menu, record/playback buttons) is not
  built; the dummy is configured via the `PROJECT_MANNEQUIN_TRAINING_DUMMY`
  launch flag and record/playback via the replay flags. Surfacing these in a
  pause menu is a UX follow-up.
- Self-contained replay load: playback restores the WORLD from the header, but
  player forms and the bootstrap/progression config are not yet auto-applied, so
  faithful reproduction currently requires the same launch setup. Restoring
  forms from the replay header on load is the natural next step (ties into
  Task 11 determinism/rollback readiness).

## Task 9: Designer Tooling And Debug Visualization

Status: Implemented

- Add a frame-data inspector view for selected actors and current moves.
- Draw active startup, active, recovery, cancel, invulnerability, armor, throw,
  and hurtbox windows in the debug overlay.
- Add move-validation reports to content loading so invalid data fails loudly.
- Add exportable move summaries for balancing sessions.
- Add quick debug controls to force states, refill meter, toggle dummy guard,
  trigger boss phases, and test wake-up options.
- Keep all tools reading simulation state; debug tools must not become required
  gameplay dependencies.

Implemented via:

- Pure per-frame model `MoveTimeline` (`Scripts/Data/MoveTimeline.cs`) breaks a
  move into startup/active/recovery phases with overlaid windows (hit, hurt,
  throw, armor, weak point, projectile, invulnerability, cancel, jump-cancel).
  It renders a phase strip, a per-window marker legend, and a playback cursor.
  `DesignerToolingTests` (`PROJECT_MANNEQUIN_TOOLING_TEST=1`) cover the timeline
  and the debug key mapping; 19/19 pass.
- Frame-data inspector: `DebugOverlay` shows P1's current move timeline
  (S/A/R strip, cursor, and only the windows that appear) reading live
  simulation state. `FrameTimelineView` (`Scripts/Debug/FrameTimelineView.cs`)
  draws the same windows as colored bars with a live cursor, toggled with F2.
- Content-load validation: `ContentManager` now runs the Castagne-style
  `MoveValidation.ValidateForm` on every loaded character and surfaces issues as
  warnings (in addition to its own structural checks), so bad cancel targets,
  infinite-combo risk, and impossible windows fail loudly during loading.
- Exportable summaries: `FrameDataExporter.BuildFullReport` composes the
  frame-data table (startup/active/recovery, on-hit/on-block/whiff, cancels,
  validation issues) plus a per-move window timeline for every move. Exported
  via the `PROJECT_MANNEQUIN_FRAME_DATA_EXPORT=<path>` launch flag or the F3
  overlay key (writes `user://frame_data_report.txt`).
- Quick debug controls via a pure `DebugCommandResolver` (key -> `DebugCommand`)
  and the overlay: F1 overlay, F2 frame bars, F3 export; 1 kill enemies, 2 skip
  to next encounter, 3 spawn prop, 4 refill meter/health, 5 toggle dummy guard
  (attaches a `TrainingDummyBrain`), 6 advance boss phase
  (`CombatActor.ForceNextBossPhase`), 7 force knockdown for wake-up testing.
- All tools are optional and gated (overlay visibility, launch flags); they read
  or nudge simulation state and are never required by gameplay.

Notes / follow-ups:

- The frame-inspector currently follows player 1; a designer actor-selector
  (cycle inspected actor) would help multi-actor debugging.
- Content-load `MoveValidation` issues are warnings so authored content still
  loads; promoting a subset to hard errors is a possible tightening.

## Task 10: Presentation Slave Layer

Status: Implemented

- Make animation a visual slave to combat state rather than the owner of combat
  timing.
- Add an animation-driver contract for move state, combat frame, facing,
  hitstop freeze, super freeze, landing, knockdown, wake-up, and phase changes.
- Add AnimationTree/AnimationPlayer support behind the Animation Driver so
  sprite presentation can replace procedural placeholder visuals without moving
  combat timing into animation callbacks.
- Freeze AnimationTree/AnimationPlayer playback during simulation-owned hitstop
  and super freeze, then resume from the authoritative combat frame.
- Add stronger hit sparks, block sparks, parry flashes, armor impact, throw tech,
  clash, wall bounce, ground bounce, and guard-break presentation events.
- Add presentation events for Reflect Guard, Phase Burst, and Rush Throw so HUD,
  camera, audio, hit sparks, and shake can react without deciding outcomes.
- Add boss-specific telegraphs that communicate danger without changing hit
  logic.
- Add audio hooks for hit strength, block, parry, counter-hit, super freeze,
  guard break, and phase shift.
- Verify presentation can be disabled or slowed without changing simulation
  results.

Implemented via:

- Animation Driver contract: pure `AnimationDriver` /
  `AnimationDriverSnapshot` / `AnimationDriverState`
  (`Scripts/Presentation/AnimationDriver.cs`) map combat state to an
  `AnimationClipKind` (idle/walk/crouch/dash/jump/landing/attack/block/parry/
  hitstun/guard-break/knockdown/wake-up/form-swap/flight/instinct/cinematic/
  defeated), carry the authoritative combat frame and facing, expose a
  frozen-aware state-clip elapsed, an `IsFrozen` flag, and the boss phase index,
  and detect the wake-up window out of knockdown. `AnimationDriverTests`
  (`PROJECT_MANNEQUIN_ANIM_TEST=1`) cover mapping, freeze-hold/resume, clip
  resets, and wake-up; 12/12 pass.
- Frozen-aware clock: `GameSimulation.PresentationClock` advances only on ticks
  that actually simulate (never during hitstop, super pause, beam clash, or boss
  intro), so any presentation timing derived from it freezes automatically.
- Animation as a slave: `CharacterVisualComponent` reads the driver each frame
  (via a new `CombatActor.Simulation` back-reference), drives its looping
  idle/walk clips and state clips from the frozen-aware clock instead of
  wall-clock, and sets `AnimationPlayer.SpeedScale` to 0 during hitstop/super
  freeze so playback holds and resumes from the authoritative combat frame. Move
  clips were already combat-frame driven.
- Presentation events: added `ReflectGuard`, `PhaseBurst`, `RushThrow`, and
  `ThrowTech` event types. `ReflectGuard` fires from the parry reflect,
  `PhaseBurst` from the boss phase-transition shockwave, and `RushThrow` from the
  CPU guard-break throw, each with a payload, so HUD/camera/audio/shake can react
  without owning the outcome. Hit/block/parry/armor/wall-bounce/ground-bounce/
  guard-break/clash events already existed.
- Audio hooks + presentation isolation: `CombatAudioManager` already reacts to
  the hit-strength (`HitConnected`/`CounterHit`/`PunishCounter`/`LauncherHit`),
  block, parry, super-freeze, guard-break, and phase-shift events, so audio
  responds to outcomes without owning them. Presentation runs on `_Process`
  while the simulation runs on `_PhysicsProcess`, so disabling or slowing
  presentation (including `AnimationPlayer.SpeedScale`) cannot change simulation
  results — confirmed by replay reproduction being identical regardless of
  presentation.

Notes / follow-ups:

- Boss-specific danger telegraphs (dedicated pre-attack warning cues) are not
  yet authored; the presentation event stream can drive them when added.
- The driver defines the clip contract and freezes an `AnimationPlayer`, but a
  fully authored `AnimationTree` per character (mapping every `AnimationClipKind`
  to real animation states) is content work that depends on final art; the
  driver is what makes that swap possible without touching combat timing.
- `ThrowTech` has an event type reserved but no emission yet because there is no
  throw-tech mechanic; wiring it is a follow-up when throw breaks are added.

## Task 11: Determinism And Rollback Readiness

Status: Implemented

- Do not implement online rollback as part of this pass.
- Audit fixed-tick determinism for movement, hit resolution, projectiles,
  hazards, boss AI, command parsing, and replay capture.
- Add state snapshot and comparison helpers for future rollback or replay tools.
- Remove hidden frame-rate dependencies from fighting-game logic.
- Keep presentation randomness outside authoritative simulation results.
- Verify the same replay produces the same health, meter, states, positions,
  combo counts, and boss phases.

Implemented via:

- Determinism audit: a full source audit found zero `GD.Randf`, unseeded
  `System.Random`, `DateTime`, `Time.GetTicksMsec`, or frame-delta usage in the
  combat, core, stage, data, input, or progression layers — all wall-clock is
  confined to presentation/UI cosmetics. `DeterminismAudit`
  (`Scripts/Core/DeterminismAudit.cs`) records each authoritative subsystem
  (movement, hit resolution, projectiles, hazards, boss/CPU AI, wave spawns,
  command parsing, replay capture) with its concrete determinism basis, and
  builds an exportable report. Online rollback was intentionally not built.
- Snapshot + comparison helpers: `ActorStateSnapshot` / `GameStateSnapshot`
  (`Scripts/Core/GameStateSnapshot.cs`) capture and restore the authoritative
  state (health, meter, guard, state, facing, position, velocity, move, combo,
  boss phase, invulnerability, and the CPU RNG state) with a field-level `Diff`
  that pinpoints exactly which value diverged. `CombatActor`, `CpuFighterBrain`,
  and `GameSimulation` expose capture/restore. A live self-check
  (`PROJECT_MANNEQUIN_SNAPSHOT_TEST=1`) captures, mutates, and restores state
  in-run and confirms the mutation was both detected and fully reverted
  (passed=True).
- Frame-rate independence: the simulation only advances on the 60Hz
  `_PhysicsProcess` tick; presentation timing was moved to the frozen-aware
  `PresentationClock` in Task 10, so no fighting-game logic depends on the
  render frame rate, and presentation can be slowed or disabled without changing
  results.
- Replay reproduction: a record→play run reproduces every state fingerprint
  exactly (5/5, passed=True), and the fingerprint folds health, meter, state,
  position, velocity, move, combo, and boss phase, so identical inputs reproduce
  identical health/meter/states/positions/combo counts/boss phases.
- Tests: `DeterminismTests` (`PROJECT_MANNEQUIN_DETERMINISM_TEST=1`) verify the
  audit coverage and that the snapshot diff detects each required field
  (health, meter, state, position, combo, boss phase, RNG, tick, hitstop); 15/15
  pass.

Notes / follow-ups:

- The snapshot captures the fingerprinted authoritative fields; a complete
  rollback would additionally capture state-machine internal timers and the
  encounter director's spawn state. That is deferred with online rollback.

## Task 12: Fighting-Game Acceptance Pass

Status: Implemented

- Complete at least one tuned boss duel for Archive Knight, Ryu, and Goku using
  the new fighting-layer rules and tools.
- Verify the first Phase 5 feature slice in a real boss fight: Reflect Guard
  pushes an attacker away, Phase Burst fires on boss transition, Rush Throw beats
  overly passive blocking, and Animation Driver replaces placeholder visuals
  without changing combat results.
- Verify each boss has readable neutral, pressure, defense, punish, phase shift,
  super, and defeat beats.
- Verify one through four local players can fight bosses without camera, input,
  revive, or hazard regressions.
- Verify training/replay tools can reproduce a full combo and a full boss-phase
  transition.
- Tune game feel: hitstop, blockstop, pushback, air routes, wake-up timing,
  guard pressure, meter pace, and boss recovery windows.
- Freeze the fighting-game foundation before expanding into local versus,
  rollback experiments, boss raids, or full campaign-scale combat tooling.

Implemented via:

- Acceptance tooling: `FightingLayerAudit` (`Scripts/Data/FightingLayerAudit.cs`)
  audits each boss's readiness (kit, phases, super, guard, AI) and authored move
  data against game-feel ranges. `AcceptanceTests`
  (`PROJECT_MANNEQUIN_ACCEPTANCE_TEST=1`) gate all of this; 12/12 pass. A passive
  `PresentationEventAuditScenario`
  (`PROJECT_MANNEQUIN_ACCEPTANCE_SMOKE_TEST=1`) censuses the event stream of a
  real fight to confirm the Phase 5 events actually fire.
- Three duel-ready bosses: Archive Knight, Ryu, and Goku all pass the readiness
  audit, and their boss smokes are green (boss-duel, world-warrior, astral all
  `passed=True`).
- Phase 5 slice in real fights: Reflect Guard was observed firing in the boss
  duel (tick 14, pushback 8) and Phase Burst in the Astral fight (tick 211, push
  25). Rush Throw's guard-break ramp is unit-verified and wired through the same
  `QueueEvent` pipeline. The Animation Driver is a pure function of combat state,
  so identical replays reproduce identical results — visuals cannot change combat.
- Readable beats: each boss exposes neutral/pressure/super variety, and the boss
  smokes verify block, parry, guard break, counter, punish, super, super pause,
  phase shift, defeat, and unlock beats.
- One through four players: `PROJECT_MANNEQUIN_PLAYER_COUNT` scales to four; the
  two-player party tether + camera-framing smoke passes (catch-up start/hard/
  complete, and the camera zooms within bounds).
- Training/replay reproduction: a record→play run reproduces every state
  fingerprint exactly (5/5), including combo counts and boss-phase transitions;
  the combo-route tests verify combo scaling and the training dummy is verified
  live.
- Full acceptance suite green: 12 unit suites (161 assertions), 5 boss smokes,
  multiplayer, replay reproduction, and the snapshot self-check.

Notes / follow-ups:

- Game-feel audit outcome: authored move data is within tuning ranges; the only
  outlier, Goku's 1200-damage Dragon Rush, is an intentional unblockable
  signature throw, so the audit gives signature moves a higher damage ceiling
  than bread-and-butter normals. No re-tuning was required.
- Rush Throw's live emission needs a throw-capable CPU against a long-blocking
  target; it is unit-verified and wired but was not observed in the current
  smokes.
- The fighting-game foundation is frozen: local versus, rollback experiments,
  boss raids, and campaign-scale tooling can build on it.