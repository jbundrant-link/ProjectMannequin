# Next Implementation Plan

Original plan: 2026-07-09  
Reviewed against the current codebase: 2026-07-14

## Review Outcome

The original order was sound, but implementation has moved on:

- **Gamepad-as-P1 is implemented.** Device assignment, GUID-based persistence,
  main-menu selection, disconnect fallback, and device-aware modal UI routing now
  exist. The remaining work is validation plus an explicit decision on left-stick
  menu navigation.
- **Combat spectacle is the next development priority.** Generic super camera,
  wash, audio, animation, and impact infrastructure already exists. The next pass
  should fix event parity and weak-point correctness before adding authored VFX.
- **Hollow Archive exists but is not yet a true boss-rush.** It contains a gate,
  an optional vertical encounter, and one champion boss. The encounter director
  currently completes a stage whenever any boss is defeated, so sequential bosses
  require an engine slice first.
- **The fourth-world slot is already defined.** `iron_fist_foundry` is a locked
  catalog realm with the identity “Launchers, stances, and heavy strikes.” Do not
  reopen theme selection; expand that contract when the content gate is met.
- **Current campaign structure is four stages per world, not one six-screen
  mission.** `WorldRunCatalog` derives three horde/horde/elite stages and one
  final-boss stage. New content must fit that structure and the mandatory art
  gates in `Docs/VISUAL_STYLE_BIBLE.md` and
  `Docs/ART_ASSET_COMPLETENESS_PLAN.md`.

Revised order:

1. Close gamepad-as-P1 validation.
2. Ship one complete combat-spectacle vertical slice.
3. Convert Hollow Archive into a real multi-boss mode and use it to prove the
   spectacle/readability systems.
4. Begin full Iron Fist Foundry production only after the content gate passes.

## 1. Gamepad-as-P1 Input — IMPLEMENTED, QA SIGN-OFF PENDING

### Current Implementation

- `LocalInputManager.TrySetAssignedDevice` assigns keyboard or a connected
  joypad, rejects duplicate live-device assignments, and preserves replay input
  authority.
- `InputDevicePreferences` persists the P1 choice separately under
  `user://project_mannequin_input_settings.json` and resolves unstable runtime
  IDs by joypad GUID.
- `LocalInputManager.RefreshDeviceAssignments` falls back safely when a selected
  pad disconnects.
- `UiInputRouter` filters pad events by the assigned P1 device while deliberately
  retaining keyboard as an emergency modal fallback.
- `MainMenu` exposes the P1 device selector.
- Form select, pause, rewards, route choices, results, and Archive Hub use the
  shared logical UI router.

### Remaining Bounded Work

1. **Decide left-stick UI parity**
   - `UiInputRouter.IsPressed` currently maps key and joypad-button events; modal
     navigation is D-pad-only even though combat movement reads the left stick.
   - Recommendation: add edge-triggered, dead-zoned left-stick navigation with a
     controlled repeat delay. If D-pad-only is intentional, state that explicitly
     in the controls UI and acceptance criteria.

2. **Add a deterministic assignment-policy suite**
   - Extract the connected-device/preference resolution into a pure policy that
     can be tested without physical hardware.
   - Cover keyboard default, explicit P1 joypad, duplicate-device rejection,
     GUID re-resolution, disconnect fallback, reconnect behavior, and replay not
     being replaced by live input.
   - Add synthetic `UiInputRouter` checks for assigned versus unassigned pad
     events and the intentional keyboard emergency fallback.

3. **Run a real-controller acceptance matrix**
   - Start from the main menu and select the pad without using a mouse.
   - Verify movement, lane/depth input, jump, all six attacks, block, grab, dash,
     motion inputs including `236HK`, pause, rewards, route choice, results, and
     form-select open/navigation/confirm/cancel.
   - Verify P2 remains on the next unclaimed pad in the split-screen scenario.
   - Verify the current `JoyButton.Misc1` jump mapping on the target controller;
     it is less portable than face/shoulder buttons and must not strand jump on
     common XInput pads.

### Acceptance

- A solo player can complete a stage from main menu to results using only the
  selected gamepad.
- P1 can open, navigate, confirm, and cancel the form-select overlay with the
  assigned gamepad.
- The documented menu-navigation policy matches reality: either D-pad plus
  edge-triggered left stick, or explicitly approved D-pad-only navigation.
- Keyboard-only play still works with the existing bindings.
- Keyboard emergency fallback remains available in every modal when a selected
  pad disconnects.
- P2+ device assignment remains predictable for split-screen and co-op tests.
- Replay playback and existing deterministic smoke tests do not read live device
  changes.

## 2. Combat Spectacle Polish — NEXT DEVELOPMENT PRIORITY

### Verified Baseline

- Generic `SuperStarted` presentation already provides camera focus in
  `PrototypeStageView`, HUD wash/announcement in `MvpHud`, and audio in
  `CombatAudioManager`.
- `CinematicSuperStarted` is emitted by `CombatActor` but currently has no
  presentation consumer. Cinematic supers therefore miss that generic camera,
  HUD, and audio path.
- The requested player `236HK` super scope is specifically `archive_burst` and
  `knight_archive_burst`. Both already have move-specific frame sequences. Ryu
  and Goku supers use different commands and are not part of this bounded slice.
- `CombatVfxManager` already supplies pooled strike, guard, parry, armor, KO, and
  explosion feedback; the authored strike burst is style-approved.
- Boss defeat and form unlock already reach the notification queue and results
  panel, but there is no dedicated portrait celebration and results can visually
  preempt a longer outro.
- Armor chip and weak-point damage math exist. `ArmorAbsorbed` already triggers an
  actor flash and pooled armor burst.
- Weak-point integration is not safe yet: body hurtboxes are inserted before
  move-authored weak-point boxes, while duplicate-hit keys ignore the target box.
  An overlapping body hurtbox can consume the hit before the weak-point
  multiplier is reached.

### Slice 2A: Super Event Correctness

1. Make `SuperStarted` and `CinematicSuperStarted` reach the same baseline camera,
   HUD, and audio choreography exactly once.
2. Give presentation events a stable move identifier without replacing the
   human-readable payload. Recommended: add an optional `MoveId` field to
   `CombatPresentationEvent` and populate it when a move starts.
3. Add a focused presentation test that starts one normal super and one cinematic
   super and asserts both receive one camera/HUD/audio cue.

### Slice 2B: `236HK` Archive Burst Pilot

1. Author one shared Archive Burst presentation cue for `archive_burst` and
   `knight_archive_burst`: super-freeze charge, shaped aura/ring, directional
   burst or sword-energy layer, active-frame impact, and short recovery trail.
2. Keep the existing move-frame animation as the first implementation. Replace
   or extend source animation only if runtime capture proves the silhouette is
   insufficient.
3. Centralize move-specific choreography in `CombatVfxManager` or a small
   super-presentation component. Do not scatter move-ID conditionals across HUD,
   camera, audio, and character visuals.
4. Start with a lightweight presentation cue ID. Promote it into a broader
   `MovePresentationData` profile only after a second distinct move family proves
   which fields are genuinely reusable.
5. Follow the visual-style workflow: pilot first, at least two canonical character
   references, 14/16 minimum, no automatic failure, explicit manifest entry, and
   calm/combat runtime captures at 1280×720 and 1920×1080.

### Slice 2C: Boss Defeat And Form-Unlock Outro

1. Define one authoritative timeline: KO impact, camera hold/wash, boss dissolve
   or collapse beat, form-unlock portrait reveal, then stage/world results.
2. Prevent `StageCompleted`/results presentation from covering the celebration.
   Recommendation: add a deterministic boss-outro director state or explicit
   completion delay rather than relying on unrelated wall-clock timers.
3. Reuse `FormSelectPortraitResolver` for the large unlock portrait and retain the
   results-panel text as an accessibility/fallback summary.
4. Add distinct defeat and unlock stingers through `CombatAudioManager`.
5. Verify persistence and unlock emission remain one-shot; presentation must not
   own progression state.

### Slice 2D: Armor, Weak Points, And Telegraphs

1. Fix target-box resolution before authoring weak points: when one attack overlaps
   both a normal hurtbox and weak-point box on the same actor, resolve the intended
   highest-priority target box exactly once.
2. Add `WeakPointHit` presentation feedback so multiplier damage is visible and
   audibly distinct; keep regular hit/counter semantics available for combo logic.
3. Add an integration test with overlapping body/weak-point boxes. The existing
   pure multiplier test is not sufficient.
4. Pilot one boss—Archive Knight or the Hollow Archive champion—with:
   - armor on a clearly committed attack window,
   - a weak point exposed during a punishable recovery window,
   - a readable pre-attack silhouette/color/audio tell,
   - throws still bypassing armor as currently designed.
5. Use existing warning events for stage hazards and entries, but use a dedicated
   move cue for boss attack tells rather than presenting every attack as a generic
   hazard warning.
6. Replace or augment procedural armor/weak-point effects with style-approved
   illustrated effects only after the mechanic/readability pilot passes.

### Acceptance

- Both normal and cinematic supers receive baseline spectacle exactly once.
- `archive_burst` and `knight_archive_burst` read as the same inherited super
  family before damage numbers appear: animation, camera, color, VFX, and audio
  agree.
- Boss defeat resolves as a reward beat before the results modal appears, and the
  unlocked form portrait/name are unmistakable.
- An overlapping weak point reliably wins the intended target-box resolution and
  emits distinct player-facing feedback.
- Armor and weak points are authored on at least one boss and covered by
  integration tests, not only pure damage helpers.
- Telegraphs give fair warning without slowing boss pacing into tutorial mode.
- Every new visual passes the visual bible and runtime capture gates.

## 3. New Content

### Current Foundation

- Core playable worlds now use four-stage runs. The verified structure is three
  horde/horde/elite stages followed by one final-boss/inheritance stage.
- `WorldRunTests` currently pass 101/101, including Archive uniqueness and
  resource-path checks.
- Hollow Archive is a secret mission with a branch to an aerial spire or directly
  to one champion boss. Calling it a boss-rush currently overstates the content.
- `iron_fist_foundry` already reserves the fourth-world identity and remains
  locked.
- Existing technical art is not automatically shippable. Several Archive,
  World Warrior, and Astral assets remain style-pending under the authoritative
  art-completeness plan.

Recommendation remains Hollow Archive first, but split it into an engine proof,
a mechanically complete rush, and an art-complete release pass.

### 3A. Hollow Archive: True Boss-Rush

#### Slice 3A-1: Sequential-Boss Contract

1. Add an explicit encounter-level continuation rule for boss/elite clears;
   default it to current behavior so official final bosses still end their stage.
2. On an intermediate boss clear, clean up the defeated actor, reopen/reframe the
   arena, advance through intermission or route choice, and reset boss intro/HUD/
   phase state for the next record.
3. Intermediate bosses must not emit the final form unlock, retire the run, or
   emit `StageCompleted`.
4. Add deterministic tests with two sequential bosses plus a final champion.

#### Slice 3A-2: Authored Rush Routes

1. Build each route around at least two boss records before the champion so both
   branches qualify as a rush.
2. Give the routes distinct decisions rather than only a different order:
   grounded duel versus vertical/aerial pressure, hazard schedule, recovery
   allowance, score multiplier, or deterministic reward tradeoff.
3. Use remixed phase pacing and arenas without mutating source boss data.
4. Make the champion the first complete armor/weak-point/telegraph showcase.
5. Define a deterministic reward appropriate for a secret mode. Avoid another
   boss-form unlock unless a genuinely new playable form exists.

#### Slice 3A-3: Release Art And Validation

1. Prototype mechanics with documented intentional reuse, but do not call the mode
   complete until its arena, champion identity, VFX, HUD treatment, and rewards
   pass the visual-style and art-completeness gates.
2. Add a dedicated Hollow Archive mission test and full smoke for both routes:
   boot, choices, every boss transition, champion, one stage-complete event,
   rewards, results, replay/determinism, and split-screen framing.

Acceptance:

- Each route contains multiple bosses and reaches the champion without an early
  stage-complete event.
- Each branch materially changes risk, movement, or resource decisions.
- Intermediate bosses do not unlock forms or retire the mode.
- The champion proves the spectacle/readability language from Track 2.
- The release version passes all visual-style, runtime-capture, and uniqueness
  requirements.

### 3B. Iron Fist Foundry: Fourth World/Boss

The world identity is already reserved:

- World ID: `iron_fist_foundry`.
- Combat identity: launchers, stances, and heavy strikes.
- Current stage label: Foundry Causeway.

Do not start by selecting a new theme. Start with a combat and content contract
that fits the existing four-stage run architecture:

1. Author a source mission with six horde encounters and one final boss so
   `WorldRunCatalog` can derive the standard ladder, or deliberately replace that
   derivation with a documented world-specific run factory.
2. Add four distinct stage/backdrop-floor identities and stage blueprints: three
   horde/horde/elite stages plus one boss stage.
3. Add at least three original horde archetypes, three silhouette-distinct named
   elites, one original boss kit, and one playable inherited form. Tint-only
   elites do not pass the art contract.
4. Add foundry-specific props, pickups, hazards, telegraphs, destruction states,
   lore, portraits, and results/map presentation.
5. Require one approved environment pilot and one approved enemy pilot before any
   batch generation. Use canonical mannequin/Ryu/Goku references and the visual
   style lock; do not use realistic industrial/PBR foundry art as the style source.
6. Add world-run, mission-validation, smoke, boss-loop, unlock, checkpoint,
   results, and rendered-capture coverage equivalent to the existing worlds.

Acceptance:

- All four stages express launcher/stance/heavy-strike play rather than reskinning
  an existing route.
- The three named elites and boss are visually distinct at gameplay scale.
- The boss introduces one new authored interaction while reusing the shared
  deterministic combat and presentation systems.
- World completion unlocks, persists, portraits, equips, and swaps into a usable
  original form.
- Every content slice passes the technical art gates, 14/16 visual rubric, and
  calm/combat captures at both target resolutions.

### Content Gate

Start full Iron Fist Foundry production only when these are true:

- Gamepad-as-P1 has automated policy coverage and a real-controller sign-off.
- Normal and cinematic super event parity is fixed.
- Archive Burst and boss-defeat/form-unlock spectacle have one style-approved,
  captured reference implementation.
- Weak-point overlap resolution is correct, and one boss has tested armor,
  weak-point, and telegraph authoring.
- Hollow Archive sequential-boss support is green, and the mode has either shipped
  or been explicitly deprioritized after the engine proof.
- The first Foundry environment and enemy pilots pass the visual-style gate before
  batch production begins.

## Validation Matrix

Every slice keeps these baselines green:

- Clean `dotnet build`.
- Determinism and replay suites.
- `PROJECT_MANNEQUIN_FORM_SELECT_TEST=1`.
- `PROJECT_MANNEQUIN_DEFENSE_TEST=1`.
- `PROJECT_MANNEQUIN_BOSS_MODE_TEST=1`.
- `PROJECT_MANNEQUIN_WORLD_RUN_TEST=1` — currently 101/101.
- Results-flow and boss-intro HUD smoke scenarios.

New suites or focused scenarios required by this plan:

- input assignment and logical UI routing,
- normal/cinematic super presentation parity,
- Archive Burst presentation cue,
- weak-point overlap plus `WeakPointHit`,
- boss-outro event ordering and unlock persistence,
- Hollow Archive sequential bosses and both-route completion.
