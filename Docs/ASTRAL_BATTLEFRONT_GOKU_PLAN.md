# Astral Battlefront: Goku Boss Plan

## Implementation Snapshot

The first playable Astral Battlefront slice is implemented.

Completed:

- Third selectable world with six horde gates and a dedicated Goku boss gate.
- Four distinct horde profiles: swarmer, ranged scout, armored heavy, and
  fighter-brain captain.
- Twelve-phase Goku boss using Base, Kaioken, False Super Saiyan, SS1, SS2,
  SS3, SS4, God, Blue, Blue Kaioken, UI Sign, and Mastered Ultra Instinct.
- Six-button standing, crouching, and aerial normals, special families,
  Kamehameha beams, supers, teleports, rushes, and anti-air attacks.
- Data-driven controlled flight with bounded height, duration, legal moves,
  landing recovery, and CPU decisions.
- Finite Instinct evades with charges, cooldowns, legal-state checks, and
  throw counterplay.
- Hand-anchored Kamehameha charge and beam presentation, plus dedicated
  animated ki blast and Spirit Bomb atlases in the fixed-tick projectile
  runtime.
- Boss defeat, persistent `goku_archive_form` unlock, and mission completion.
- Higgsfield-generated Shattered Skyway stage art.
- Separate Higgsfield-restyled `8x18` movement/normal atlases for all twelve
  forms, each using the same stable cell layout.
- Separate 55-frame signature atlases for all twelve forms covering
  Kamehameha, aerial beam, Dragon Rush, rising attack, Meteor Smash,
  teleport, flight pressure, Spirit Bomb, and Instinct Rush.
- A 17-frame Blue transformation and a 64-frame Mastered Instinct
  transformation, preserving the source transition lengths.
- M.U.G.E.N AIR frame ordering, authored durations, axis offsets, flips, and
  intentional invisible teleport frames in the offline extraction pipeline.
- Dedicated UI Sign and Mastered Instinct evade animation plus phase-specific
  move-atlas selection at runtime.
- Playable archive transformation cycling with `22HP`.
- Deterministic twelve-form Astral boss-loop smoke coverage.

Remaining post-MVP visual production:

- Segmented travel panels, parallax, destructible stage props, and dedicated
  horde-enemy sheets.
- Audio, animation preview clips, and cinematic super camera polish.

This plan adds a third playable arcade route without replacing either existing
mission. It uses the current `astral_battlefront` catalog slot and keeps the
same MVP loop:

1. Travel through a scrolling stage.
2. Defeat six gated hordes.
3. Enter a focused fighter-boss arena.
4. Defeat Goku.
5. Archive a balanced Goku form.

The M.U.G.E.N packages are motion, timing, and move-list references. Their
runtime code will not be imported or executed.

## Source Audit

### Capsule Corp Goku

Best primary source for motion and visual coverage.

- 6,100 SFF v2 sprites.
- 2,863 AIR actions and 27,039 frame references.
- Stable full-size proportions around a roughly 100x120 pixel base body.
- Six-frame idle, ten-frame walk, complete crouch attacks, and complete air
  attacks.
- Form-offset animation banks for Base, Super Saiyan, Super Saiyan 2,
  Super Saiyan 3, Super Saiyan God, Super Saiyan Blue, Ultra Instinct Sign,
  and Mastered Ultra Instinct.
- Commands include Dragon Rush, Vanish, Kamehameha variants, Kaioken,
  Meteor Smash, teleport, Spirit Bomb charging, ground specials, air
  attacks, and transformation/reversion commands.
- The included readme explicitly says automatic UI dodge was removed in the
  current version.

Use this package for:

- Base movement and normal attack timing.
- Crouch and air pose coverage.
- Kamehameha, teleport, rush, Kaioken, and Spirit Bomb references.
- Transformation silhouettes and transition timing.
- Authored aerial attack, chase, hover, and Sky Kamehameha pose references.

The active CNS files contain 308 state definitions using `physics = N`, with
261 of those marked as aerial states. These states suspend ordinary gravity
for specific attacks, chases, helpers, and cinematics. They do not expose a
persistent player-controlled free-flight command.

### GokuFullBootleg

Useful as a transformation and spectacle reference, not as final art or
runtime logic.

- 5,439 SFF v2 sprites.
- 1,152 AIR actions.
- Base art is small JUS/chibi scale, roughly 47x57 pixels.
- Includes authored sections for SS1, SS2, SS3, SSG, SSB, Limit Breaker, and
  MUI.
- Includes long transformation actions and a wide super catalog.
- Several form helpers are mislabeled or set helper variables instead of
  parent variables, so its transformation state logic is not a reliable
  implementation template.
- The SS4-labeled route mixes art sources and should not define the first
  Goku boss.

Use this package for:

- Transformation anticipation, hold, flash, and recovery beats.
- Aura staging and cinematic timing ideas.
- Additional super pose references when Capsule Corp lacks a useful pose.
- Extended aerial pursuit and no-gravity combo pose references.

The package contains 332 actual state definitions using `physics = N`, not
403 distinct flight states. Only 80 of those state definitions are marked as
aerial; the rest include common states, helpers, transformations, and
cinematic controllers. This supports authored air-combo inspiration but does
not prove a reusable free-flight controller.

### Migatte no Goku Mastered Ultra Instinct

Useful only for MUI pose and counter fantasy.

- 1,790 SFF v1 sprites.
- 225 AIR actions.
- Contains focused MUI normals and large super actions.
- Palette logic includes random 80 to 90 percent normal-attack immunity,
  permanent immunity on upper palettes, infinite meter, full healing,
  throw immunity, one-hit damage, and anti-KO behavior.

Do not copy its defensive logic. Our MUI phase must remain finite, readable,
and punishable.

This package contains 13 `physics = N` state definitions, 12 of which are
aerial. Its emphasis remains counterattacks and extreme palette behavior
rather than broad movement or persistent flight.

### Aerial Mechanics Conclusion

None of the three packages implements a clean, persistent free-flight command.
They create the Dragon Ball feeling through temporary no-gravity attacks,
launchers, pursuit states, teleports, air jumps, Vanish, Dragon Rush, and
aerial supers.

Our Flight Stance is therefore an authored Project Mannequin boss mechanic
inspired by that vocabulary. It must not be presented as a direct port of the
M.U.G.E.N logic.

## Corrections To The Earlier Plans

- Capsule Corp Goku is not only a Base-form source. It already contains the
  cleanest complete Base-to-MUI animation family.
- The implemented boss uses twelve concise health-gated transformations;
  Blue and MUI retain longer authored cinematics while intermediate forms use
  short power-up clips to preserve pacing.
- MUI should not receive passive permanent auto-dodge.
- SS4 is a full phase between SS3 and God with dedicated movement, normals,
  specials, aura, and tail animation.
- Do not create a parallel `DbzWorldCatalog.cs`. Add the mission to the
  existing `MvpWorldCatalog`.
- Do not create duplicate character-combat or AI components. Extend
  `CombatActor`, `BossPhaseData`, `CpuFighterBrain`, and the existing
  presentation layer.
- Do not make the new mission the default startup mission. Add it as the third
  selectable world.
- Do not rely on palette swaps for forms with different hair and body
  silhouettes. Each selected phase needs a real visual atlas.
- SFF extraction must remain an offline authoring step. Godot should load
  validated PNG atlases and declarative move data.
- Do not interpret every `physics = N` state as player-controlled flight.
  Helpers, projectiles, hit reactions, transformations, and cinematics also
  disable ordinary gravity.
- Do not make MUI flight-cancel into teleport approaches nearly unreactable.
  Flight, teleport, and Instinct evades need shared lockouts and visible
  recovery.

## Stage Identity

Catalog slot:

- World ID: `astral_battlefront`
- World name: `Astral Battlefront`
- Stage name: `Skybreak Tournament`
- Combat identity: aerial rushes, ki pressure, teleports, and beam supers

The route begins in a damaged tournament district and climbs toward a
cratered sky arena. It should visibly change as the player advances.

### Stage Segments

1. **Arrival Concourse**, X 0-30
   - Tournament gates, transport platforms, spectators evacuating.
   - Introduces fast melee enemies.
2. **Capsule Causeway**, X 30-62
   - Elevated city road, damaged vehicles, distant towers.
   - Introduces ranged ki crossfire and mixed waves.
3. **Shattered Ascent**, X 62-88
   - Broken rock, floating debris, energy fractures in the sky.
   - Adds aerial entries and the elite pre-boss wave.
4. **Skybreak Arena**, X 88-102
   - Tightened lane depth, destroyed tournament floor, open sky.
   - Dedicated Goku duel camera and transformation staging.

The current renderer stretches one background across the whole route.
Before generating final art, add segmented background data so three or four
Higgsfield panels can be placed without severe horizontal stretching.

Suggested data:

- `StageBackdropSegmentData`
  - texture path
  - X start/end
  - Y and Z placement
  - pixel size and scale
- Optional `StageParallaxLayerData`
  - texture path
  - parallax factor
  - X coverage

## Horde Design

The route keeps six gates but does not copy the exact spawn cadence of the
first two missions.

### Enemy Roles

1. **Saibaman**
   - Fast swarmer.
   - Short leap, low strike, and retreat.
   - Low health and high entry frequency.
2. **Frieza Force Scout**
   - Ranged zoner.
   - Single ki shot, lane reposition, and punishable charge.
   - Tries to maintain medium distance.
3. **Frieza Force Heavy**
   - Slow armor/space-control enemy.
   - Shoulder rush and heavy launcher.
   - Interruptible startup and high guard damage.
4. **Ki Captain**
   - One elite before the boss.
   - Combines a short rush string with a telegraphed beam.
   - Uses the CPU fighter brain rather than the basic arcade brain.

If the project is later distributed, these can be replaced by original
archive constructs with identical gameplay profiles.

### Encounter Rhythm

1. **Landing Party**
   - Two Saibamen from opposite horizontal edges.
2. **Crossfire Checkpoint**
   - Two Scouts using near/far lanes.
3. **Gravity Ambush**
   - Two Saibamen followed by one Heavy.
4. **Capsule Causeway**
   - Scout and Heavy first, then a delayed Saibaman flank.
5. **Fractured Plaza**
   - Four-enemy mixed wave with a maximum of three active.
6. **Final Challenger**
   - Ki Captain plus two delayed Scouts.
7. **Goku**
   - One boss in the dedicated Skybreak Arena.

### Destructible Terrain

Add a small number of deterministic breakables without turning the stage into
a physics simulation:

- Cracked tournament pillars.
- Loose boulders.
- Damaged transport capsules.
- Energy canisters.

Each breakable uses a fixed combat box, authored health, and a presentation
event when destroyed. The first version grants a small amount of meter
directly to the actor who breaks it. Physical meter-shard pickups and
simulated debris remain later work.

Breakables must never obstruct the boss arena, conceal a hostile attack, or
create random collision results.

## Goku Control Grammar

Goku uses the project's existing six-button layout:

- `LP`, `MP`, `HP`
- `LK`, `MK`, `HK`

System mechanics remain shared:

- `HP+HK`: Homing Dash.
- `MP+MK`: Parry.
- `LP+LK`: Dragon Rush while neutral; advancing guard while blocking.
- `B`: Block.
- `G`: Throw fallback.

### Normal Attacks

Author all eighteen posture normals:

- Six standing normals.
- Six crouching normals.
- Six air normals.

Core routes:

- `LP > MP > HP`
- `LK > MK > HK`
- `2LP > 2MP > 2HP`, with `2HP` as the launcher.
- Air `LP > MP > HP`
- Air `LK > MK > HK`
- Launcher jump-cancel into an aerial route.

### Specials

Initial boss/playable kit:

- `236LP/MP/HP`: Kamehameha family.
  - Light is fast and narrow.
  - Medium has more guard damage.
  - Heavy is slower, wider, and more punishable.
- `214LP/MP/HP`: Dragon Flash rush family.
  - Short advancing strike through multi-hit rush.
- `623LP/MP/HP`: Rising Dragon anti-air family.
  - Increasing invulnerability, height, damage, and recovery.
- `22LK/MK/HK`: Instant Transmission.
  - Retreat, same-side reposition, or cross-up variant.
  - Must validate the destination against arena bounds and pushboxes.
- `236LK/MK/HK`: Kaioken Assault family in Base phase.
- `214LK/MK/HK`: Meteor Smash / aerial dive family.
- `LP+LK`: Dragon Rush command grab.
- `214LP+LK`: enter Flight Stance when the current phase or form permits it.
- `4LP+LK` during Flight Stance: cancel flight and descend.

### Supers

- `236236P`: Super Kamehameha.
- `214214K`: Spirit Bomb.
- MUI-only meter option: Silver Counter, based on the MUI counter fantasy but
  activated as a real move rather than passive immunity.

Avoid mixed-character attacks such as Big Bang Attack in the initial Goku
kit even when they appear in a downloaded package.

## Controlled Flight Stance

Flight is a temporary combat stance, not permanent free movement. It becomes
available in the Super Saiyan Blue boss phase and remains available in MUI.

### Activation And Limits

- Command: `214LP+LK`.
- Ground activation has 12 startup frames and rises to the minimum flight
  height.
- Air activation arrests vertical momentum after the same visible startup.
- Base duration: 180 simulation frames.
- Height is clamped between 0.8 and 3.3 world units.
- Lane depth remains clamped to the current encounter.
- Taking a hit, using Dive Kick, using Sky Kamehameha, or reaching zero flight
  time ends the stance.
- Natural expiration causes 18 landing-recovery frames.
- Manual cancel uses `4LP+LK` and causes 10 landing-recovery frames.
- Flight cannot be activated during hitstun, blockstun, knockdown, guard
  break, another move, teleport recovery, an Instinct evade, or a cinematic.
- Goku cannot use ordinary standing guard while flying.

One confirmed flight attack may restore up to 24 frames once per activation.
It never fully resets the timer. This rewards engagement without enabling an
indefinite hover loop.

### Flight Movement

The project uses X for horizontal travel, Y for height, and Z for lane depth.
Flight preserves that coordinate contract:

- Left/right input controls X.
- Jump held rises on Y.
- Crouch held descends on Y.
- Up/down input retains reduced Z-lane steering.
- Diagonal X/Y movement is normalized.
- Flight acceleration and turn speed are authored constants.
- Flight movement cannot pass encounter bounds or push through another
  actor's pushbox.

This provides free aerial positioning without remapping the beat-em-up lane
controls or introducing a separate input mode.

### Restricted Flight Moveset

- `LP`: fast aerial jab.
- `MP`: advancing aerial palm.
- `HP`: Flight Rush, a horizontal pursuit punch.
- `LK`: downward light kick.
- `MK`: Flight Dive Kick; ends flight on use.
- `HK`: angled downward ki blast.
- `236P`: Sky Kamehameha; steep downward beam with long recovery that ends
  flight.
- `22K`: flight teleport. It shares a cooldown with ordinary Instant
  Transmission and cannot immediately cancel into an Instinct evade.
- `4LP+LK`: manual flight cancel.

Ground-only normals, Dragon Rush, Spirit Bomb, and ordinary guarding are
disabled until Goku lands.

### Boss AI Flight Rules

- Blue Goku may enter flight only at medium or long range.
- MUI Goku uses the same legal flight rules, not a faster hidden version.
- Flight activation receives a long decision cooldown so it cannot repeat
  immediately after landing.
- The CPU chooses among downward ki pressure, Flight Rush, Dive Kick, and Sky
  Kamehameha based on position and player recovery.
- Flight Cancel into teleport has an 18-frame shared lockout.
- The CPU may intentionally miss a flight conversion according to its normal
  mistake chance.
- Anti-air hits and hard knockdowns terminate flight.

The player can answer flight with a launcher/anti-air, lateral lane movement,
projectile parry, Homing Dash, or a punish during landing recovery.

## Boss Structure

Use three phases to match the original vertical-slice scope.

### Phase 1: Earth-Raised Fighter

Health: 100 to 67 percent.

- Visual: Base Goku.
- Teaches six-button strings, Kamehameha, anti-air, Dragon Rush, and Kaioken.
- Moderate aggression and deliberate pauses after beams.
- Kaioken is a move/super, not a permanent form.

### Phase 2: God Ki Unleashed

Health: 66 to 34 percent.

- Visual: Super Saiyan Blue.
- Adds Instant Transmission, faster air routes, Homing Dash confirms, EX
  Kamehameha, Meteor Smash, and Controlled Flight Stance.
- Increased movement speed and shorter decision cadence.
- Transformation lock must swap to a dedicated Blue atlas and aura.
- Flight is introduced with conservative AI usage and a full 180-frame timer.

### Phase 3: Autonomous Instinct

Health: 33 to 0 percent.

- Visual: Mastered Ultra Instinct.
- Adds Silver Counter, stronger anti-air selection, and limited Instinct
  evades.
- Retains Flight Stance but uses the same timer, recovery, and shared
  teleport/evade lockouts as Blue.
- Receives full meter once at phase entry.
- No permanent healing, immunity, infinite meter, or anti-KO behavior.

### Fair Instinct Rule

At phase start Goku receives three Instinct charges.

- An evade can trigger only from idle, walk, or guard-neutral recovery.
- It cannot trigger in startup, active frames, recovery, hitstun, knockdown,
  cinematic lock, or guard break.
- It consumes one charge and has a cooldown.
- It avoids one strike or projectile, but not throws or authored
  unblockables.
- The sidestep has visible startup, a fixed destination, and recovery.
- A successful evade opens a short optional counter window.
- The debug overlay displays charges and cooldown.

This gives the phase an Ultra Instinct identity while preserving player
counterplay through throws, feints, delayed attacks, and recovery punishes.

## Required Engine Extensions

### Flight Runtime

Add a dedicated `CombatActorState.Flying` and fixed-tick runtime fields:

- remaining flight frames
- flight activation cooldown
- flight hit-extension availability
- minimum/maximum flight height
- flight acceleration and speed
- shared teleport/Instinct lockout

Add a data-driven `FlightProfileData` to `CharacterData` rather than
hardcoding Goku checks in the state machine. It defines activation command,
duration, movement constants, legal move IDs, cancel recovery, and
phase/form availability.

`CombatStateMachine` owns activation, movement, move filtering, termination,
landing recovery, and pushbox/boundary validation. Presentation reads the
resulting state and does not control flight physics.

### Phase Visuals

Extend `BossPhaseData` with:

- `VisualVariantId`
- aura color/intensity
- optional transition animation ID
- optional damage multiplier
- optional phase mechanic ID

Add `CharacterVisualVariantData` to `CharacterData`:

- sprite atlas path
- columns and rows
- pixel size
- ground offset
- animation profile ID

`CharacterVisualComponent` must refresh on phase changes, not only when
`CurrentForm.Id` changes. Replace the hardcoded
`UsesWorldWarriorAtlas()` path check with a data-driven animation profile.

### Projectiles And Beams

Extend projectile presentation data with:

- visual type: orb, beam, disc, burst
- color and emission
- visual scale
- clash durability
- optional multi-hit interval
- optional stationary beam lifetime

Kamehameha should use a beam-shaped visual and deterministic combat box,
not the current generic sphere.

### Beam Clash Readiness

Add declarative clash metadata while building the beam runtime:

- clash class
- clash strength
- clash eligibility
- beam duration
- beam direction

A full Beam Clash contest is not required for the first Astral Battlefront
mission because the starting mannequin does not yet own a comparable beam.
The later system should trigger only when two eligible opposing beams overlap
in the same lane.

The contest should use a fixed timing window or capped input pulses rather
than unrestricted button mashing. Boss pressure must be deterministic by
difficulty, and the result must return both actors cleanly to ordinary combat.

### CPU Fighter Brain

Add:

- weighted move selection by tag and range
- projectile-aware approach/guard decisions
- teleport destination validation
- Homing Dash choice at long range
- Dragon Rush choice against repeated guarding
- phase mechanic hook for finite Instinct evade decisions
- bounded Flight Stance activation and flight-specific move selection
- shared cooldown decisions across flight, teleport, and Instinct evade

The CPU must continue to obey reaction frames, recovery, meter, phase move
pools, and authored mistakes.

### Stage Runtime

Add:

- segmented backdrops
- encounter-specific lane bounds
- optional boss duel camera flag
- optional aerial enemy entry height
- deterministic breakable definitions and destruction events

The boss arena may narrow lane depth, but it should remain 2.5D and allow
lane evasion against beams.

## Higgsfield Art Pipeline

Use Higgsfield only after the extraction and runtime contracts are stable.

### Character Pipeline

1. Extend the offline M.U.G.E.N audit tool with SFF v2 support.
2. Extract Capsule Corp AIR actions with original frame order, duration, and
   axis anchors.
3. Create action contact sheets for Base, Blue, and MUI.
4. Use the current game art plus the supplied FighterZ references as style
   anchors.
5. Generate one action at a time in small pose batches with
   `nano_banana_2`.
6. Preserve the source action's unique pose count. Do not reduce a ten-frame
   walk to four unrelated poses.
7. Normalize every output to one canonical standing height and foot
   baseline before atlas packing.
8. Generate separate Base, Blue, and MUI atlases so silhouettes are real,
   not palette approximations.
9. Produce GIF previews for idle, walk, jump, crouch, normals, specials,
   transformations, and supers before Godot integration.

Recommended first visual set:

- Base idle/walk/run/jump/crouch.
- Eighteen normals.
- Kamehameha, Dragon Flash, Rising Dragon, teleport, Kaioken, Meteor Smash.
- Dragon Rush and Homing Dash poses.
- Blue transformation, idle, movement, hover, flight travel, flight cancel,
  downward ki blast, Dive Kick, Flight Rush, Sky Kamehameha, and landing.
- MUI transformation, idle, movement, hover, flight travel, evade, counter,
  and super.

### Stage Pipeline

1. Generate four matching 16:9 stage panels with `gpt_image_2`.
2. Use a shared style reference and consistent horizon/ground-line prompt.
3. Keep the combat floor clear and readable.
4. Stitch or place panels through segmented backdrop data.
5. Generate distant sky/debris layers separately for restrained parallax.
6. Verify the character ground line and full leg visibility at every segment.

### Source Handling

- Keep raw M.U.G.E.N archives and extracted references outside exported game
  builds.
- Record creator/package names and unknown license status.
- Treat generated derivative assets as personal-use prototype material unless
  redistribution rights are confirmed.

## Unlock Design

Boss reward ID: `goku_archive_form`.

The playable archive form should be strong but not inherit boss cheats:

- Base visual and full six-button normals.
- Kamehameha, Dragon Flash, Rising Dragon, teleport, and Meteor Smash.
- Kaioken as a timed meter move.
- Super Kamehameha and Spirit Bomb.
- MUI represented initially as a cinematic super or short meter-limited
  awakening, not permanent auto-dodge.

Blue and full MUI playable variants can be added through later mastery
without blocking the stage MVP.

## File-Level Implementation Order

### Slice 1: General Runtime Contracts

- `Scripts/Data/CharacterData.cs`
  - Add visual variants and phase presentation/mechanic fields.
- `Scripts/Presentation/CharacterVisualComponent.cs`
  - Remove path-specific animation rules and support phase variants.
- `Scripts/Data/MoveData.cs`
  - Add projectile visual/beam fields.
- `Scripts/Combat/CombatProjectileManager.cs`
  - Render data-driven projectile shapes and beam lifetimes.
- `Scripts/Data/WorldMissionData.cs`
  - Add deterministic breakable definitions.

### Slice 2: Boss Mechanics

- `Scripts/Combat/CombatActor.cs`
  - Apply phase visual/mechanic runtime state and track flight resources.
- `Scripts/Combat/CombatStateMachine.cs`
  - Add Flight Stance, bounded Instinct evade state, and safe teleport
    movement.
- `Scripts/Combat/CpuFighterBrain.cs`
  - Add tagged weighted selection, projectile responses, flight decisions,
    and phase hooks.
- `Scripts/Debug/DebugOverlay.cs`
  - Display phase, Instinct charges, and cooldown.

### Slice 3: Stage Runtime And Mission

- `Scripts/Data/WorldMissionData.cs`
  - Add backdrop segments, encounter lane bounds, and aerial entry fields.
- `Scripts/Presentation/PrototypeStageView.cs`
  - Render segmented stage art and breakable presentation.
- `Scripts/Data/MvpWorldCatalog.cs`
  - Add the Astral Battlefront mission and make it selectable when complete.
- `Scripts/Stage/ArcadeEncounterDirector.cs`
  - Register the new enemies, Goku boss, and Goku unlock form.
- `Scripts/Core/MvpCombatBootstrap.cs`
  - Restore saved Goku unlocks.
- `Scripts/UI/MainMenu.cs`
  - Include the third mission and unlock status.

### Slice 4: Content

- `Scripts/Data/TestRosterFactory.cs`
  - Add the three horde roles, Ki Captain, Goku boss, and playable form.
- Prefer moving Goku construction into a focused
  `Scripts/Data/GokuRosterFactory.cs` rather than making
  `TestRosterFactory.cs` substantially larger.
- Add a `GokuMugenAnimationCatalog.cs` equivalent that maps extracted AIR
  actions and exact frame durations onto move data.

### Slice 5: Art And Integration

- Add the SFF v2 offline extraction path.
- Generate and process the three Goku atlases with Higgsfield.
- Generate and install the four stage panels.
- Generate flight pose families and breakable object art.
- Map every move and phase to validated atlas sequences.
- Add animation preview GIFs and baseline comparison sheets.

## Verification Gates

1. `dotnet build` passes before art generation.
2. Headless mission smoke reaches and clears all six horde gates.
3. Every enemy archetype demonstrates a distinct behavior.
4. Goku uses only phase-legal moves.
5. Base-to-Blue and Blue-to-MUI transitions change the actual atlas.
6. Kamehameha beam collision is lane-aware, blockable, parryable, and
   punishable.
7. Instant Transmission never places Goku outside the arena or inside a
   pushbox.
8. Instinct charges decrement, respect cooldown, fail against throws, and
   never activate during recovery or hitstun.
9. Flight lasts exactly its authored duration, respects height/lane bounds,
   ends on hit and authored attacks, and applies the correct cancel/landing
   recovery.
10. Flight moves replace ground moves without allowing illegal guarding,
    Dragon Rush, or Spirit Bomb.
11. CPU flight activation respects range, cooldown, mistake chance, and the
    teleport/Instinct shared lockout.
12. Breakables resolve through deterministic combat boxes and never obstruct
    the boss arena.
13. Boss defeat unlocks `goku_archive_form`, persists it, and allows form swap.
14. No-enemy visual captures confirm consistent height, feet baseline, and
    full-body framing for all three forms.
15. GIF previews confirm that walk, jump, crouch, air attacks, flight,
    Kamehameha, teleport, and transformations preserve the intended source
    frame flow.
16. The stage shows distinct travel landmarks instead of one visibly
    stretched background.

## Recommended Scope Lock

Build now:

- Six hordes and one Goku boss.
- Base, Super Saiyan Blue, and MUI boss visuals.
- One balanced playable Goku archive form.
- Core normals, six special families, Dragon Rush, two supers.
- Finite Instinct charges.
- Controlled Flight Stance with a restricted aerial move set.
- Deterministic stage breakables that grant direct meter.
- Segmented stage art.

Defer:

- SS1, SS2, SS3, SSG, SS4, and every intermediate form as playable variants.
- Beam Clash contest and cinematic beam struggles.
- Transform selection menus.
- Tag assists and 3v3 team rules.
- Multi-form mastery progression.

This scope proves the Dragon Ball realm fantasy while remaining compatible
with the original MVP and the current codebase.
