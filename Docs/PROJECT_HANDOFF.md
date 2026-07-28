# Project Mannequin Machine Handoff

Last verified: 2026-07-22

This is the current execution checkpoint for continuing Project Mannequin on
another machine or in a fresh agent session. It does not replace the roadmap or
waive any completion gate.

## Read First

Use these documents in this order:

1. [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md) - authoritative
   Phase 0-7 product scope and verification contract.
2. [ART_ASSET_COMPLETENESS_PLAN.md](ART_ASSET_COMPLETENESS_PLAN.md) - mandatory
   content and art gates.
3. [VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md) - visual language, rejection
   rules, generation workflow, and approval rubric.
4. [PROJECT_STATUS.md](PROJECT_STATUS.md) - broad implementation inventory and
   tuning backlog.
5. This file - exact continuation point and machine-transfer instructions.

If any summary in this file becomes stale, the live code/tests and the four
documents above remain authoritative.

## Git Transfer State

The last verified pre-handoff checkpoint was:

- Branch: `main`
- Commit: `f485e4c2f0c663e25158dbcb76a55de965225e3a`
- Remote: `origin` -> `https://github.com/jbundrant-link/ProjectMannequin.git`
- Remote divergence at verification: `0` behind, `0` ahead
- The current Phase 5 World Warrior worktree may contain intentional uncommitted
   work; do not discard or normalize it while establishing continuation state.

Before changing machines, review intended files through the Source Control view
or path-scoped Git queries, then commit and push them explicitly. Do not use an
unbounded status listing to establish continuation state in this asset-heavy
worktree.

Do not assume a clean local worktree means another machine can see the latest
work. Confirm that `git rev-list --left-right --count origin/main...HEAD` prints
`0 0` after the final push.

## Verified Project Checkpoint

The active master-plan phase is **Phase 5**.

This checkpoint describes the verified live worktree. It is clone-durable only
after the complete training-dummy, Vitality Gourd, Focus Drum, and Judge's Laurel
Fan pilots, production sprites, processing reports, runtime wiring,
dual-resolution drop/collection evidence, review metadata, and LFS rules are
committed and pushed together. Before relying on a fresh clone, verify all four
final sprites, generation/processing/capture tools, runtime logs, and
intact/drop/collection captures resolve through `git ls-files`, then confirm that
the containing commit exists on the remote. Every curated dummy, health, meter,
and score-pickup PNG must resolve to Git LFS.

Current automated baseline:

- Settings assertions: `34/34`
- World-run assertions: `247/247`
- Stage-hazard assertions: `60/60`
- Input grammar/assignment assertions: `34/34`
- Run-score assertions: `11/11`
- World/hazard aggregate: `307/307`
- Clean runtime evidence exists for approved World Warrior slices at both
  `1280x720` and `1920x1080`.

Completed quality bar:

- Archive Nexus Stages 1-4, base enemies, named elites, Archive Knight combat
  and intro, hazards, props/pickups, layered environments, destruction, HUD,
  Archive Map, results, pause, and form select are runtime approved.
- World Warrior Dojo Approach is `15/16` runtime approved.
- World Warrior Dojo Rookie style-v2 is `16/16` runtime approved.
- World Warrior Pavilion Striker style-v2 is `16/16` runtime approved.
- World Warrior Tournament Grappler style-v2 is `16/16` runtime approved.
- World Warrior Pavilion Circuit is `14/16` runtime approved.
- World Warrior Grand Tournament Floor is `15/16` runtime approved.
- World Warrior Champion's Courtyard is `15/16` runtime approved.
- World Warrior Dojo Prodigy Kenzo is `16/16` runtime approved in the live
  worktree.
- World Warrior Pavilion Ace Makoto is `16/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior Grand Grappler Tetsu is `16/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior breakable training dummy is `16/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior Vitality Gourd health pickup is `16/16` runtime approved in the
  live worktree, subject to the clone-durability gate above.
- World Warrior Focus Drum meter pickup is `16/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior Judge's Laurel Fan score pickup is `16/16` runtime approved in
  the live worktree, subject to the clone-durability gate above.
- World Warrior Sparring Supply Crate is `15/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior Pavilion Rack Chest is `15/16` runtime approved in the live
  worktree, subject to the clone-durability gate above.
- World Warrior Champion's Trophy Podium is `15/16` runtime approved in the
  live worktree, subject to the clone-durability gate above.
- World Warrior Champion's Lantern Urn is `15/16` runtime approved in the
  live worktree, subject to the clone-durability gate above. This is the
  fourth and final World Warrior breakable-crate variant.
- World Warrior Dojo Rolling Log and Falling Training Weight hazards are
  each `15/16` runtime approved in the live worktree, subject to the
  clone-durability gate above. These are World Warrior's first hazard zones
  of any kind.

The immediate unfinished slice is **Phase 6 item 1, `SettingsStore` and the
options surface**, under the master plan's
`Execution Sequencing Change - 2026-07-25`. That change pulls Phase 6 items 1-4
ahead of the remaining Phase 5 content art because they are pure code, they are
deterministically testable, and they decide whether anyone other than the
developer can play the build: there is currently no volume control, no control
rebinding, and no reduced-flash or shake setting.

All four **World Warrior training-crate variants** (Supply Crate, Rack Chest,
Trophy Podium, Lantern Urn) are now complete on the proven pilot/runtime path.
The full **Proportion Sizing Sweep** (see the dedicated section below) is
scheduled next, before batching rolling logs, falling practice props, crowds,
or destruction.

## Completed Since Previous Handoff

### Stage Presentation Architecture

- Stage data declares explicit `LegacyLayered`, `CompositeTraversal`,
  `BoundedArena`, and `FullFramePlates` presentation modes instead of forcing
  traversal and boss art through one transform path.
- `FullFramePlates` shows one complete source painting per scene, fitted to the
  camera, and draws no visible replacement floor: the painted floor inside the
  plate is the floor the player sees, and gameplay collision uses an invisible
  plane. Five stages use it - all four Astral stages and Champion's Courtyard.
- Each Astral stage renders its own `2704x1521` plate. Energy Rail previously
  used three plates, but two were byte-identical to the Skyfall and Tournament
  Summit masters, so Stage 3 displayed Stage 1 and Stage 4 scenery. The Astral
  plates remain pre-restyle interim art; see `Remaining Phase 5`.
- A plate must declare its painted walkable band through `GroundFarFraction` and
  `GroundNearFraction`. `Scripts/Stage/StageGroundProjection.cs` then solves the
  resting camera look height so world `y = 0` across the belt lands on that band,
  and `WorldRunTests` fails the stage if it does not. Aspect, dimension, and
  pixel-fidelity checks all pass while fighters hover in mid-air, which is how
  Energy Rail shipped with 72 percent of its belt depth over painted sky.
- Champion's Courtyard uses one complete exact-16:9 plate (`3840x2160`). The
  superseded bounded-arena build drew an opaque floor mesh that occluded roughly
  the lower third of the backdrop.
- Plates are produced by `Scripts/Tools/prepare_full_frame_plate.py`, which only
  pads to the target aspect by extending edge pixels. It never crops, so no
  source pixel is lost.
- Approval requires objective pixel evidence, not aspect or dimension checks.
  `Scripts/Tools/audit_stage_plate_fidelity.py` compares clean stage-only
  captures against the source PNGs and fails on MAE above `4.0` or Pearson
  correlation below `0.99`. Current report
  `Artifacts/stage_plate_fidelity_report.json` passes all eight pairs, worst MAE
  `3.4913`, lowest correlation `0.99507475`.
- Clean plates are exported with `PROJECT_MANNEQUIN_STAGE_PLATE_CAPTURE_PATH`
  plus `PROJECT_MANNEQUIN_FULL_FRAME_PLATE_INDEX`. Always clear every unused
  `PROJECT_MANNEQUIN_*` capture variable first: a stale capture path will
  overwrite a source asset.
- `StageMissionValidator` and `WorldRunTests` enforce exact 16:9 plates, plate
  ordering within stage bounds, empty legacy panel and segment lists, camera
  consistency, and belt grounding. Current world/hazard aggregate is `239/239`.
- The seven `LegacyLayered` stages keep their restyled backdrop, midground,
  foreground, and floor sets; that split is the approved production grammar, not
  a leftover. They were mis-framed rather than mis-authored: each now declares
  `GroundLineCenterFraction` `0.775`, the same belt row the plate stages solve
  to, and floor-anchored far layers grow until they cover the widest camera
  envelope. Before that the belt sat at row `0.62` with a floor plane running
  from `LaneMinZ - 12`, so the ground mesh took about two thirds of the frame and
  the restyled backdrop was reduced to a cropped strip.
- Floor materials must pass `Scripts/Tools/audit_stage_floor_materials.py`.
  Detail that runs in courses collapses into screen-wide stripes under the stage
  camera; three materials still fail, listed under `Open Stage Defects`.
- Do not mix production plates with flattened legacy composites.

### Tournament Grappler

- Accepted v1 identity/locomotion/misc sources were preserved.
- Weak v1 and first v2a attack attempts remain rejected provenance.
- Accepted v2 Shoulder Drive sources compose into
  `Assets/Sprites/Enemies/world_warrior_grappler_style_v2.png`.
- The atlas passes 60 populated/30 transparent-frame, alpha, grounding,
  padding, chroma, and prone-width audits.
- Runtime uses all columns `40..49` with exact
  `5/5/5/5/5/6/7/7/7/7` timing for `20/5/34` phases.
- Exact idle/active Stage 3 captures pass at `1280x720` and `1920x1080`;
  the visual review is `16/16`.

### Pavilion Circuit

- Stage 2 now uses five independent textures: lantern-pavilion backdrop,
  broad lacquered-deck floor, real-alpha judges/practice midground, and split
  knee-height edge remnants.
- Four floor candidates remain rejected provenance; the accepted deck was
  chosen only after runtime projection review.
- Runtime-specific vertical compression keeps architecture inside the fixed
  camera while preserving `0.72/0.90/1.12` parallax.
- Stage visual smoke passes four roots/four panels/floor/parallax/lighting/sharp
  sampling; Stage 2 ladders pass at both target resolutions.
- The Pavilion review is `14/16` and the current aggregate is `187/187`.

### Grand Tournament Floor

- Stage 3 now uses independent championship-arena backdrop, giant-slab floor,
   real-alpha trophy/judges/gallery midground, and split arena-edge remnants.
- Empty terraces preserve space for later spectator/crowd reaction layers.
- Camera-safe compression preserves the same ordered parallax as Pavilion.
- Stage visual smoke and complete Stage 3 ladders pass at both target
   resolutions; the review is `15/16`.

### Champion's Courtyard

- Stage 4 now uses five independent production textures: storm-dark final-duel
  backdrop, weathered flagstone floor, real-alpha midground, and split shallow
  foreground remnants.
- The family passes `15/16` with no automatic failure and is recorded as final
  in `VISUAL_STYLE_REVIEW_MANIFEST.json`.
- Exact final-duel captures pass at `1280x720` and `1920x1080`; focused world
  tests pass `154/154` and the aggregate passes `191/191`.
- The live Stage 4 contract preserves zero external hazards during the Ryu duel.
- The reusable `PushZone` hazard behavior is implemented and validator-tested;
  no production stage authors it yet. Astral Energy Rail remains its first
  planned content use.

### Dojo Prodigy Kenzo

- Kenzo is a distinct `16/16` named-elite archetype rather than a display-name
  or tint variant over the Dojo Rookie.
- Accepted identity-locked sources compose into a `2560x2304` real-alpha `10x9`
  atlas with 60 populated frames, 30 transparent reserves, baseline/padding
  compliance, and zero green spill.
- Master Palm preserves the base combat timing on unique frames; rejected fist
  and back-facing startup revisions remain versioned provenance.
- Exact clean idle/active Stage 1 captures pass at `1280x720` and `1920x1080`;
  focused world tests passed `158/158` at Kenzo completion and the subsequent
  world/hazard aggregate passed `209/209` before Makoto integration.

### Pavilion Ace Makoto

- Makoto is a distinct `16/16` precision-kick named elite rather than a display
  name or tint variant over the Pavilion Striker.
- The pilot-first seven-family source set preserves a high braided crest,
  asymmetrical vermilion mantle, split indigo pavilion panels, ivory chevron
  shin guards, and bronze crescent token.
- Walk v1-v3 remain rejected provenance. Accepted v4 supplies alternating
  contact/down/passing/up silhouettes and passes the six-distinct-pose gate.
- The accepted sources compose into a `2560x2304` real-alpha `10x9` atlas with
  60 populated frames, 30 transparent reserves, baseline/padding compliance,
  zero green spill, and SHA-256
  `f853cbd5dfe13f2fe1254f3a3de1c7ad4f83bc64462cb55ba780c65b362744e8`.
- Crescent Heel uses unique frames while preserving exact `16/5/27`
  startup/active/recovery timing over the Striker combat kit.
- Exact clean idle/active Stage 2 captures pass at `1280x720` and `1920x1080`;
  deterministic validation at Makoto completion passed `172/172` world plus `43/43` hazard
  assertions. The style review manifest records final approval and all 32
  Makoto PNGs resolve through Git LFS.

### Grand Grappler Tetsu

- Raw v1 established the distinct bald, twin-beard, square-seal identity but was
  framed inside the outer review band; raw v2 preserved the design but substituted
  white for chroma green. Both remain rejected provenance.
- Raw v3 restored chroma and preserved the approved identity. Deterministic
  normalization uniformly scales those reviewed pixels by `0.868023` onto a
  pure-green `2048x2048` canvas with `329/192/329/192` clearance.
- The fixed 230-pixel comparison reads distinctly beside Tournament Grappler and
  Makoto. The final visual manifest records `16/16`, no automatic failure, and
  `runtime_approved` with `finalApproval=true`.
- Walk v1 remains rejected two-pose shuffle provenance; guide-driven v2 passes
  eight distinct keyed silhouettes. Startup v1 remains rejected clipping
  provenance; v2 plus deterministic containment supplies the accepted five poses.
  Misc v1 remains rejected seven-figure provenance; v2 restores all eight poses.
- The accepted sources compose into a `2560x2304` real-alpha `10x9` atlas with
  60 populated frames, 30 transparent reserves, baseline/padding compliance,
  zero green spill, `237-244`-pixel prone widths, and SHA-256
  `dab2d5b9de4efd5f0ab1cf04bca2a8d7a785a7bb65ed0b0966bd8af369fe6168`.
- Iron Gate Clinch uses unique upright two-arm frames while preserving exact
  `20/5/34` startup/active/recovery timing over the Grappler combat kit.
- Exact clean idle/active Stage 3 captures pass at `1280x720` and `1920x1080`
  at deterministic tick `1093`; validation at Tetsu completion passed `176/176`
  world plus `43/43` hazard assertions. All 26 Tetsu PNGs resolve through Git LFS.

### World Warrior Training Dummy

- One nonhumanoid `16/16` breakable practice post establishes the World Warrior
  prop grammar: segmented charcoal timber, saffron upper target, warm-ivory
  torso pad, vermilion strike patch, indigo-capped side bars, bronze pegs, break
  seams, and a broad indigo/vermilion weighted base.
- Exact green-key processing preserves the `2048x2048` canvas and
  `475/91/1530/1992` alpha bounds, produces zero green spill, and calibrates the
  prop to `0.00147291/968` for a `2.8`-unit gameplay height.
- Dojo Approach authors exactly one `90`-HP nonthrowable dummy with a guaranteed
  Vitality Gourd drop; the sprite is distinct from every Archive cache.
- Clean intact and gourd-drop Stage 1 captures pass at `1280x720` and
  `1920x1080` at deterministic tick `817`, with zero errors or warnings.

### World Warrior Vitality Gourd

- One `16/16` double-lobed ivory calabash establishes the health-pickup grammar:
  saffron cork, three jade medicinal leaves, vermilion braided cord, leaf inlay,
  and deep-indigo three-petal cradle, with no cross/heart/crystal/text shortcut.
- Exact green-key processing preserves `321/55/1778/1996` alpha bounds, zero
  green spill, and calibrated `0.00038125/972` metrics for a `0.74`-unit pickup.
- The gourd remains readable at `64/80/96` pixels and is content-distinct from
  the Archive Vital.
- Real Stage 1 drop/collection captures pass at both target resolutions; normal
  overlap consumes the gourd immediately and restores health `120 -> 180`, exact
  25 percent of the mannequin's `240` maximum.
- Validation at Vitality Gourd completion passed `181/181` world plus `43/43`
  hazard assertions.

### World Warrior Focus Drum

- One `16/16` compact hourglass drum establishes the meter-pickup grammar:
  paired ivory drumheads, saffron resonance rings and beads, deep-indigo waist,
  plum grip, vermilion X-cords, and one attached streamer, with no Archive
  crystal/shard, lightning icon, potion, coin, text, or health cue.
- Exact green-key processing preserves `158/325/1890/1724` alpha bounds, zero
  green spill, and calibrated `0.00052895/700` metrics for a `0.74`-unit pickup.
- The drum remains readable at `64/80/96` pixels and is content-distinct from
  both the Vitality Gourd and Archive Meter Shard.
- Pavilion Circuit authors one additive `105`-HP training dummy with a guaranteed
  Focus Drum drop; Dojo's approved health dummy and Gourd remain unchanged.
- Real Stage 2 drop/collection captures pass at both target resolutions at tick
  `926`; overlap consumes the drum and applies the existing `200`-meter grant,
  clamped from `0 -> 100` by the mannequin form cap. Current validation passes
  `184/184` world plus `44/44` hazard assertions.

### World Warrior Judge's Laurel Fan

- One `16/16` broad ceremonial display fan establishes the score-pickup grammar:
  vermilion fan arc, ivory ribs, three blank stepped judge tabs, attached saffron
  laurels, indigo U-rest/pivot, and plum tassel, with no Archive prism, currency,
  coin, medal, trophy, crown, score text, or weapon silhouette.
- Exact green-key processing preserves `110/144/1931/1878` alpha bounds, zero
  green spill, and calibrated `0.00042676/854` metrics for a `0.74`-unit pickup.
- The fan remains readable at `64/80/96` pixels and is content-distinct from the
  Gourd, Focus Drum, and Archive Data.
- Grand Tournament authors one additive `120`-HP training dummy with a guaranteed
  fan drop; Dojo health and Pavilion meter paths remain unchanged.
- Pickup actors no longer count as enemy defeats, removing an erroneous extra
  `750` score while retaining the intended pickup grant. Real Stage 3
  drop/collection captures pass at both resolutions at tick `1021`; score rises
  exactly `1500 -> 2500`. Current validation passes `187/187` world,
  `45/45` hazard, and `11/11` RunScore assertions.

### World Warrior Sparring Supply Crate

- One `15/16` breakable supply crate establishes the World Warrior crate
  grammar: low wide timber body, overhanging flat lid, three plank slats,
  indigo lacquered corner brackets with bronze studs, vermilion cargo-strap
  knot, saffron rope handle, split seam, chipped corner, and a lid-mounted ivory
  wrap bundle with folded indigo mitt. Archive crate mechanics were reused as a
  template only; no Archive cache identity, tint, crystal, or sci-fi motif.
- Detail density scored `1` because plank grain plus rivet studs add mild
  clutter beside the cleaner pickups; cohesion, gameplay readability, and
  runtime fitness each scored `2`.
- Exact green-key processing preserves `110/227/1937/1872` alpha bounds, zero
  green spill, and calibrated `0.00079027/848` metrics for a `1.30`-unit prop.
- The crate stays readable at `96/128/160` pixels and is content-distinct from
  the training dummy and Archive caches.
- Dojo Approach adds one additive `70`-HP supply crate to its second encounter
  with a guaranteed Focus Drum drop; the three approved dummy props and every
  pickup path are unchanged. Intact, break-drop, and post-collection captures
  pass at both resolutions at tick `833` with meter rising `0 -> 100`. Current
  validation passes `190/190` world, `47/47` hazard, and `11/11` RunScore
  assertions.

### World Warrior Pavilion Rack Chest

- One `15/16` breakable rack chest is the second World Warrior crate identity,
  deliberately built as the opposite silhouette to the Sparring Supply Crate
  (measured content aspect `0.72` upright vs `1.11` squat) so the two crates
  cannot read as the same prop recoloured. Motifs are drawn from the Pavilion
  Circuit stage art itself: vermilion lacquered posts, a slate tiled pent roof,
  an indigo banner with a saffron stripe, the stage's repeating red medallion,
  and its practice weapon racks.
- Detail density scored `1` for the same reason as the Supply Crate; every
  other criterion scored `2`.
- Exact green-key processing preserves `314/49/1735/2009` alpha bounds and zero
  green spill. The initial pilot calibration (`0.00094388/985` for a
  `1.85`-unit prop) was found post-wiring to render far too small next to the
  player character once measured properly: `1.85` units is only **44.4%** of
  the mannequin's true rendered height (`~4.10` units, measured by alpha-content
  bounding box, not the smaller gameplay hurtbox). It was corrected to
  `0.00132653/985` for a `2.60`-unit prop, **63.4%** of true mannequin height,
  in the same band as the approved training dummy's `68.2%` and consistent
  with reading as standing furniture rather than a footstool. Ground offset
  pixels are unchanged; only `SpritePixelSize` moved, so no art was
  regenerated. `Scripts/Tools/measure_true_sprite_world_height.py` is the new
  reusable tool for this measurement, and `VISUAL_STYLE_BIBLE.md > Proportion
  and scale calibration` is the new mandatory gate.
- The chest stays readable at `96/128/160` pixels and is content-distinct from
  the Supply Crate, the Pavilion training dummy, and the Archive data cache.
- Pavilion Circuit adds one additive `85`-HP rack chest to its second encounter
  with a guaranteed Judge's Laurel Fan-style score drop, alongside the existing
  approved training dummy (meter drop) in its first encounter. This completes a
  cyclic pickup-type pattern across World Warrior's non-boss stages so far
  (Dojo: health + meter, Pavilion: meter + score). Intact, break-drop, and
  post-collection captures pass at both resolutions at tick `955` with score
  rising `6000 -> 7000`. A transient "N ObjectDB instances leaked at exit"
  shutdown warning reproduces on the already-approved Supply Crate script too
  and is a pre-existing environmental artifact, not a regression. Current
  validation passes `236/236` world, `49/49` hazard, and `11/11` RunScore
  assertions.

### World Warrior Champion's Trophy Podium

- One `15/16` breakable trophy podium is the third World Warrior crate
  identity: a stepped three-tier stone podium, deliberately a third distinct
  silhouette from both the low wide Sparring Supply Crate and the tall
  standing Pavilion Rack Chest (wide-based and stepped rather than a flat box
  or a vertical cabinet), verified in a three-way silhouette comparison.
  Motifs are drawn from the Grand Tournament Floor midground art itself: the
  dark charcoal-black stepped stone trophy plinth, warm-ivory stone tiers, a
  deep-indigo-and-vermilion ribbon sash, the pewter-silver trophy-cup shape
  (used as a fixed lid ornament), and small gold brazier-bowl accents.
- Detail density scored `1` for the same reason as the other two crates; every
  other criterion scored `2`.
- This pilot applied the corrected process **from the start**, per
  `Immediate Continuation Order` item 4: `target_world_height` (`2.30` units)
  was chosen against the canonical mannequin's true rendered height (`4.10`
  units) *before* processing, landing at **56.0%** — inside the `55-75%`
  standing-furniture/apparatus band from `VISUAL_STYLE_BIBLE.md > Proportion
  and scale calibration` — and confirmed correct in the post-wiring runtime
  capture with no adjustment needed, unlike the Rack Chest's post-hoc fix.
  Exact green-key processing preserves `23/191/1943/1911` alpha bounds and
  zero green spill, calibrating `0.00133721/887`.
- The podium stays readable at `96/128/160` pixels and is content-distinct
  from both approved crates, the training dummy, and the Archive data cache.
- Grand Tournament Floor adds one additive `95`-HP trophy podium to its second
  encounter with a guaranteed health drop, alongside the existing approved
  honor dummy (score drop) in its first encounter. This completes the
  three-stage cyclic pickup-type pattern across World Warrior's non-boss
  stages (Dojo: health + meter, Pavilion: meter + score, Grand Tournament:
  score + health). Intact, break-drop, and post-collection captures pass at
  both resolutions at tick `1052` with health rising `120 -> 180` (the
  established 25%-of-max Vitality Gourd grant). The same transient "N ObjectDB
  instances leaked at exit" shutdown warning already documented as a
  pre-existing environmental artifact reproduces here too and is not a
  regression. Current validation passes `240/240` world, `51/51` hazard, and
  `11/11` RunScore assertions.

### World Warrior Champion's Lantern Urn

- One `15/16` breakable lantern urn is the fourth and final World Warrior
  crate identity: a round ceremonial urn with an indigo lantern-dome cap,
  deliberately a fourth distinct silhouette from the low wide Sparring Supply
  Crate, the tall standing Pavilion Rack Chest, and the wide-based stepped
  Champion's Trophy Podium (round-bodied rather than a box, a cabinet, or a
  stepped stack), verified in a four-way silhouette comparison — box,
  cabinet, stepped pyramid, and round urn share no silhouette. Motifs are
  drawn from the Champion's Courtyard finale-arena plate itself: hanging
  lanterns and braziers, vermilion banners, warm timber/pale plaster tones,
  and the deep indigo tiled-roof color, expressed as a round barrel body, a
  lantern-dome cap with a brass finial, a vermilion banner sash, and two
  unlit brazier-bowl side handles.
- Detail density scored `1` for the same reason as the other three crates;
  every other criterion scored `2`.
- This pilot again applied the corrected process from the start:
  `target_world_height` (`2.45` units) was chosen against the canonical
  mannequin's true rendered height (`4.104` units) *before* processing,
  landing at **59.7%** — inside the `55-75%` standing-furniture/apparatus
  band, between the Trophy Podium's `56.0%` and the Rack Chest's `63.4%` —
  and confirmed correct in the post-wiring runtime capture with no adjustment
  needed. Exact green-key processing preserves `326/109/1702/1933` alpha
  bounds and zero green spill, calibrating `0.0013432/909`.
- The urn stays readable at `96/128/160` pixels and is content-distinct from
  all three other crates, the training dummy, and the Archive data cache.
- **Champion's Courtyard is structurally different from the other three World
  Warrior stages**: it is the final boss stage with a single boss encounter,
  not a first/second horde pair, so it never had a `AuthorStageSetPiece` call
  at all. This is the first prop ever authored onto a boss encounter. A new
  guarded call was added directly in the ladder-building path
  (`if (worldId == "world_warrior_sector") { AuthorChampionsCourtyardLanternUrn(finalEncounter); }`
  right after `finalEncounter.RouteChoices.Clear()`), reusing the same
  `StagePropData`/`SpawnProps` machinery that already runs unconditionally for
  every encounter kind including `Boss` — no engine changes were needed, and
  `StageMissionValidator.Validate` passed against the boss encounter with no
  special-casing.
- With the three-stage cyclic pickup pattern already complete (Dojo: health +
  meter, Pavilion: meter + score, Grand Tournament: score + health), this
  crate's drop type was a free choice; **Meter** was picked to give the player
  a resource boost heading into the final fight. The urn is a `105`-HP prop,
  continuing the escalating HP trend across all four crates (`70, 85, 95,
  105`).
- Intact, break-drop, and post-collection captures pass at both resolutions
  at tick `813` with **zero errors and zero warnings** (the transient
  "N ObjectDB instances leaked at exit" warning seen on other crate captures
  did not reproduce here), and meter rises exactly `0 -> 100`. Current
  validation passes `244/244` world, `53/53` hazard, and `11/11` RunScore
  assertions. **This completes all four World Warrior training-crate
  variants.**

### World Warrior Dojo Rolling Log and Falling Training Weight

- World Warrior's **first hazard zones of any kind** (`StageHazardZoneData`,
  not breakable props — every World Warrior stage before this had zero
  hazard zones). Two `15/16` hazards give Dojo Approach its own
  obstacle-course identity: the **Rolling Log** (`LinearSweep`, reusing the
  Archive Intake Tram's exact timing envelope — 24-frame delay, 72-frame
  warning, 96-frame active window, 264-frame repeat — at a gentler 70 DPS)
  and the **Falling Training Weight** (`FallingStrike`, the same behavior
  family as the Archive Repository Falling Shelf, 130 DPS, a finite
  20-frame impact window).
- Motifs are drawn directly from the Dojo Approach midground art itself: the
  aged warm-brown timber and rope-binding language from its two training
  posts becomes the log's rope wraps and brass end-caps; the aged stone and
  rope-cord language from its lantern base and hanging bell becomes the
  weight's rope-harnessed granite training weight with a worn strike-target
  mark. Detail density scored `1` for both, consistent with every other
  World Warrior prop/hazard; every other criterion scored `2`.
- Both applied the **Proportion and scale calibration** gate from the start,
  reasoned against authored stage geometry and function rather than the
  character-height bands (which apply to standing furniture, not hazards):
  Rolling Log `1.00` world-unit diameter (`24.4%` of the `4.104`-unit true
  mannequin height) reasoned as a thick, unmistakably substantial obstacle;
  Falling Weight `1.20` units (`29.2%`) reasoned slightly larger for clear
  incoming-danger readability. Both stay legible at `96/128/160` pixels and
  are verified content-distinct from each other and from the training dummy
  and supply crate in a four-way silhouette comparison.
- **Two tooling fixes found by building these**, documented in
  `/memories/repo/setup.md`:
  1. A Higgsfield `--prompt` body containing a literal quoted phrase (for
     example `readable "moving danger" presentation`) fails CLI argument
     parsing with `Error: Too many positional args`, even though the whole
     prompt is one PowerShell string. Isolated by binary search down to a
     158-character fragment. Fix: never use literal double-quoted emphasis
     inside a prompt body; use hyphenated-word emphasis instead. A stray
     em-dash (`--`) inside prompt text was also tested and is **not** the
     cause.
  2. `AuthorStageSetPiece` never runs for the final boss stage (confirmed
     again here, first found during the Lantern Urn); not relevant to Dojo
     Approach directly, but the same lesson: always re-verify the actual
     dispatch path before assuming a helper covers every stage shape.
- Wired additively: Rolling Log into Dojo Approach's first encounter
  (alongside the existing training dummy), Falling Weight into the second
  encounter (alongside the existing supply crate, offset in `Z` to avoid
  overlap). `StageMissionValidator.Validate` passes with both present.
  `StageHazardRuntime.Resolve` is exercised across the full
  dormant/warning/active/cooldown/repeat cycle for both, deterministically.
  A real runtime capture confirms the Rolling Log's authored warning text
  ("ROLLING LOG — CLEAR THE CENTER LANE") renders correctly in-engine
  alongside the dojo scene and fighters. The shared telegraph capture tool
  only captures the first hazard encountered per run, so the Falling
  Weight does not yet have its own dedicated screenshot — see `nextAction`
  in the manifest for a proposed tool extension. Deterministic validation
  passes `247/247` world, `60/60` hazard, and `11/11` RunScore assertions.

## Immediate Continuation Order

1. Do not restore or search an older Copilot session. Start from this checkpoint
   and the live files only.
2. Do not regenerate or revise Tetsu, the training dummy, Vitality Gourd, Focus
  Drum, Judge's Laurel Fan, the Sparring Supply Crate, the Pavilion Rack
  Chest, the Champion's Trophy Podium, the Champion's Lantern Urn, the Dojo
  Rolling Log, or the Dojo Falling Training Weight; retain their final
  assets, evidence, and hashes. The Pavilion Rack Chest's `SpritePixelSize`
  (only) was corrected post-approval from `1.85` to `2.60` world units after
  a measured-proportion review found the original pilot calibration far too
  small next to the player character; that corrected value is now final. The
  Trophy Podium (`2.30` units, `56.0%`), the Lantern Urn (`2.45` units,
  `59.7%`), the Rolling Log (`1.00` unit diameter, `24.4%`), and the Falling
  Weight (`1.20` units, `29.2%`) all applied the measured-proportion method
  from the start and needed no correction.
3. Do not fix proportion sizing piecemeal. The user confirmed the smallness
  issue is systemic ("the issue is in every stage... the train [Archive
  Intake Tram] is ridiculously small") and asked for one full sweep at the end
  of the current phase rather than scattered single-asset corrections. See
  the new `Full Proportion Sizing Sweep` section below for the trigger
  condition, confirmed offenders (including the Sparring Supply Crate at
  `31.7%`), already-checked assets, and full scope — read it before touching
  any more prop/character scale.
4. **All four World Warrior training-crate variants are now complete**:
  Sparring Supply Crate, Pavilion Rack Chest, Champion's Trophy Podium, and
  Champion's Lantern Urn. The Lantern Urn also proved out a new pattern:
  Champion's Courtyard is the final boss stage with a single boss encounter
  rather than a first/second horde pair, so it needed its own guarded
  `AuthorChampionsCourtyardLanternUrn` call in the ladder-building path
  instead of going through `AuthorStageSetPiece`. Reuse this same proven
  pilot/review/process/wire/test/capture path — including the **Proportion
  and scale calibration** gate measured from the start with
  `Scripts/Tools/measure_true_sprite_world_height.py` — for every future
  World Warrior prop, not just crates.
5. **Dojo Approach's Rolling Log and Falling Training Weight are done** —
  World Warrior's first hazard zones of any kind. Move to the rest of the
  `Remaining Phase 5 > World Warrior` queue: spectator/crowd layers and
  reactions, and localized tournament-arena destruction states. Apply the
  same pilot-first, runtime, dual-resolution, and proportion-sizing gates to
  each. Consider extending `capture_hazard_telegraph_review.ps1` to target a
  specific hazard by ID or sprite suffix (mirroring
  `PROJECT_MANNEQUIN_LADDER_PROP_SPRITE_SUFFIX` for props) — it currently
  only captures the first hazard encountered per run, so Dojo Approach's
  Falling Weight has no dedicated telegraph screenshot yet, only its full
  deterministic `StageHazardRuntime.Resolve` coverage.
6. Only after that queue is complete, run the **Full Proportion Sizing
  Sweep** (see the dedicated section below) — this was the user's explicit
  instruction: one full cross-world audit at the end of the phase, not
  piecemeal fixes. It covers the still-unaudited Sparring Supply Crate
  (`31.7%`) plus the confirmed-too-small Archive assets, and every
  character/prop/hazard not yet measured. Do not mark Phase 5 complete or
  start Astral art before this sweep closes out.
7. Do not start Astral art. Astral stages are on interim pre-restyle plates and
  are deliberately deferred; read `Remaining Phase 5 > Astral Battlefront` before
  touching them, and honour the hard gates listed there.
8. Treat `Open Stage Defects` as a known, measured backlog. Do not re-approve any
  stage listed there without first re-running the audit that failed it.
9. The stage presentation pass is finished and is not the current slice. Belt
  grounding, backdrop ground lines, floor materials, the contact shadow, and the
  Champion's Courtyard plate are all done and gated. The one piece deliberately
  left open is repainting the Pavilion and Grand Tournament backdrops with a
  ground line, which is parked as item 6 of `Remaining Phase 5 > World Warrior`.
10. Phase 6 item 1 is partly landed. `Scripts/Settings/SettingsData.cs` and
  `Scripts/Settings/SettingsStore.cs` hold the full settings model with range
  clamping, atomic writes plus backup recovery, and tolerant parsing that
  degrades a malformed file to usable defaults instead of blocking startup.
  Settings live in `user://project_mannequin_settings.json`, separate from
  progression, run, and input-device saves. `SettingsTests` covers it at `9/9`
  via `PROJECT_MANNEQUIN_SETTINGS_TEST=1`. Audio buses `Music`, `SFX`, and `UI`
  are created at startup if the project has not defined them, which also fixes
  players that referenced a non-existent `SFX` bus. Camera shake now scales by
  `ShakeIntensity` at the point of use, so zero is exactly zero.
11. Phase 6 item 1 is now closed. The options surface is split into
  `Scripts/Settings/OptionsModel.cs`, which is engine-free and holds every row
  definition, navigation, clamping, cycling, and value formatting, and
  `Scripts/UI/OptionsMenu.cs`, a thin `CanvasLayer` that renders rows and
  forwards input. The split exists so behaviour is covered by deterministic
  assertions rather than click-through. `SettingsTests` is now `16/16` and
  covers selection wrap at both ends, slider saturation, screen shake reaching
  exactly `0%`, toggles flipping on both Accept and Adjust, reset restoring
  defaults without closing, and an unlisted resolution stepping onto the
  supported ladder. The surface is reachable from the pause menu `Options`
  button and the Archive Map `OPTIONS` button. Two details are load-bearing and
  should not be "simplified": the overlay handles `_Input` rather than
  `_UnhandledInput`, because focused `Button` nodes on the menu underneath
  consume Accept during GUI input, which runs first; and the panel is centred
  with a `CenterContainer` rather than a `Center` anchor preset, because the
  preset resolves offsets before the content-driven size exists and pushes the
  panel off screen with the value column clipped. Both faults were caught only
  by capturing a real frame, not by the build or the assertions.
  `Scripts/Debug/OptionsMenuSmokeScenario.cs` plus
  `Scripts/Tools/capture_options_menu_review.ps1` re-run that check on demand
  under `PROJECT_MANNEQUIN_OPTIONS_UI_SMOKE_TEST=1`.
12. Phase 6 item 2, the accessibility contract, is landed.
  `Scripts/Settings/AccessibilityRuntime.cs` holds the whole policy as pure
  functions that take the setting as an argument, so the deterministic suite
  asserts it without a scene tree. Settings suite is now `25/25`.
  *** FLASH RATE FINDING: the character state strobes were hard square waves at
  5.9-9.1 Hz (form swap 80 ms, parry 65 ms, guard break 85 ms, super startup
  55 ms, hitstun 70 ms half periods) and the boss intro aura oscillated at
  4.8 Hz. WCAG 2.3.1 allows at most three flashes per second, so all six sat
  in the photosensitive band. RESOLVED 2026-07-25, split by risk profile: the
  boss intro aura is now 2.9 Hz (`AccessibilityRuntime.BossAuraFlickerRate`)
  because it is large-area, long-lived, and purely cosmetic, so slowing it costs
  nothing. The character state strobes are deliberately LEFT above the ceiling:
  they are character-sized rather than large-area, and each encodes timing, so
  at 3 Hz a twelve-frame parry window would show less than one full cycle and
  the cue would stop being readable. Degrading the mechanic for everyone is the
  wrong trade when `ReducedFlash` removes them entirely for players who need
  that. `ReducedFlash` now
  flattens every one of them: square waves hold on, scalars settle on their
  midpoint, smooth oscillations hold their centre, and combo impact flashes
  lerp 35% toward the resting colour instead of full white. Every period is
  declared in `AccessibilityRuntime.StrobePeriods` so the rates stay auditable
  in one place rather than as scattered literals.
  *** COLOUR-ONLY DEFECT: hazard telegraphs signalled warning versus active by
  hue alone, amber vs red and purple vs orange, which protanopes and
  deuteranopes cannot separate. Two colour-independent channels now carry the
  phase: opacity bands that provably never overlap (warning tops out at 0.32,
  active starts at 0.56; 0.48/0.80 under high contrast), and a countdown marker
  that contracts to nothing exactly as the hazard fires and does not exist once
  it is live. `HighContrastTelegraphs` raises both opacity and emission.
  Verified as pixels, not assertions: the high-contrast pass changes only rows
  483-547, the telegraph strip, raising mean luminance 140.2 to 153.8 and local
  contrast 59.0 to 64.1; and with reduced flash the band is 38x steadier
  run-to-run (delta 0.022 vs 0.854), which is what proves the setting actually
  reaches the renderer rather than only the model.
  `Scripts/Tools/capture_hazard_telegraph_review.ps1` re-runs both comparisons
  via `-HighContrast` and `-ReducedFlash`. Capture-only overrides live in
  `SettingsStore.ApplyCaptureOverrides` and are gated on
  `IsPersistenceDisabled`, so they can never apply in a player session.
13. Phase 6 item 3, input glyph switching and reconnect, is landed.
  `Scripts/Input/GamepadBindings.cs` is now the SINGLE source of truth for which
  pad button drives which action: `LocalInputManager.PollJoypad` reads it to
  build the input mask and `Scripts/Input/InputGlyphs.cs` reads the same table
  to label the UI, so a displayed glyph cannot claim a binding the pad does not
  have. `KeyboardCommandFormatter` was renamed `InputCommandFormatter` and
  `ToKeyboard` to `ToDisplayCommand`, because the class now emits pad glyphs and
  the old names were lies. Every label routes through
  `InputGlyphs.LabelForAction`, so all move-list rows and legends switch with no
  call site knowing which family is active. Input grammar suite is `32/32`.
  Godot reports face buttons by SDL POSITION, not printed name, which is the
  whole reason this exists: `JoyButton.A` is the bottom button, printed "A" on
  Xbox, "Cross" on PlayStation, and "B" on Nintendo. Unknown pads resolve to
  `GenericGamepad` and print SDL positions rather than guessing a brand.
  Three prompts hardcoded keyboard keys and were simply wrong on a pad:
  `ArcadeEncounterDirector` objective text for form swap and reward choice, and
  two `MvpHud` strings. All now use `InputGlyphs.UiActionLabel`. The device
  notice is deliberately NOT a simulation presentation event, because device
  changes are wall-clock and must never enter replay state; `LocalInputManager`
  exposes `ConsumeDeviceNotice` and the HUD drains it, so a pad dropping mid-run
  announces "CONTROLLER DISCONNECTED - KEYBOARD ACTIVE" instead of going silent.
  Verified as pixels: `Scripts/Tools/capture_move_list_glyphs.ps1 -Family ...`
  produced keyboard, PlayStation, and Xbox move lists; PlayStation and Xbox each
  differ from keyboard by mean delta 19.4 and 18.4 across ~362 rows of the
  panel, and differ from each other by 4.65. The keyboard move-list regression
  still passes unchanged.
  *** JUMP BINDING FIXED 2026-07-25. Jump had been on `JoyButton.Misc1`, the
  Xbox Series share / PS5 microphone / Switch capture button, which does not
  exist on Xbox One pads, DualShock 4, or most third-party controllers, so
  jumping was impossible on them. Jump now uses `Back` and the optional Assist
  action takes `Misc1`, because losing an optional assist call on an older pad
  is far less severe than losing jump. Up was NOT an option: this is a
  belt-scroller and Up is lane movement toward the backdrop
  (`CombatStateMachine` maps it to `direction.Z -= 1`). `InputGrammarTests` now
  asserts every core action sits on a universally present button.
  Grab and Block remain analog triggers rather than buttons, which is fine
  because triggers exist on every modern pad.
14. Phase 6 item 4, save robustness, is landed, which closes the whole
  Phase 6 items 1-4 playability block. `Scripts/Progression/SaveSchema.cs` holds
  the version policy as pure functions. Settings suite is now `33/33`.
  The two stores previously handled a version mismatch in opposite and equally
  wrong ways. `MvpProgressStore.Load` BLIND-RESTAMPED any file to the current
  version without transforming it, so an old save was relabelled as current and
  a NEWER save was silently downgraded and then written back, deleting fields
  this build does not know about. `RunSaveStore.TryLoadPath` did the reverse and
  DISCARDED any file whose version was not an exact match, so a schema bump
  would throw away a player's in-progress run with no explanation. Both now
  route through `SaveSchema.Evaluate`, which classifies Current, Migrated,
  FutureVersion, or Unsupported, and `MayOverwrite`, which makes a newer file
  strictly read-only. Migration steps are explicit and stamp the version only
  after they run. v1 to v2 of the progress schema was purely additive, so its
  step is documented as needing no transform rather than being faked.
  Reset-data did not exist at all. `Scripts/Progression/SaveDataReset.cs` erases
  all four save files, and their `.bak` and `.tmp` siblings, because leaving a
  backup would silently restore the data the player asked to delete. It is
  reachable from the options surface as `Erase All Save Data`, gated behind a
  two-press confirmation that disarms when the player navigates off the row, so
  a single stray Accept can never wipe a save. Verified as pixels in
  `Artifacts/StyleCalibration/options_menu_erase_confirm.png`.
  The save indicator is `SaveSchema.NotifySaved` plus a HUD label. Like the
  device notice, it is deliberately NOT simulation state: it must never affect a
  replay. Its predicate has a pure overload so the suite drives the clock, and
  it explicitly survives a backwards clock rather than wrapping the unsigned
  subtraction and pinning the indicator on forever.
15. `Stage Rendering And Depth Separation Pass` item 1, the per-actor grounding
  shadow, is landed. `CharacterVisualComponent` builds one soft radial quad per
  fighter, scaled by the node's boss/presentation scale, shrinking and fading
  with height off the ground so a jump reads as leaving the floor.
  *** LATENT ENGINE BUG FOUND AND FIXED, and it is bigger than the shadow.
  Full-frame stage plates are screen-filling ALPHA-BLENDED `Sprite3D`s
  (`AlphaCut.Disabled`, `Billboard.Enabled`) with no render priority, so they
  sorted LAST in the transparent pass and erased every alpha-blended object in
  the gameplay layer on all five plate stages. Character sprites survived only
  because they use `AlphaCut.Discard`, which puts them in the opaque pass.
  Any future transparent VFX would have been invisible there too. Plates now
  carry `RenderPriority = -8` and the shadow material carries `+1`.
  Diagnosis took a full bisect and is worth repeating rather than re-deriving:
  an opaque box on the same node rendered, the same node with the shadow
  material did not, the generated texture was verified correct by dumping it
  (centre alpha 0.976), position and orientation were verified by printing the
  global transform, and `AlphaScissor` rendered while `Alpha` did not, which
  isolated it to transparent-pass sorting rather than geometry.
  *** CAMERA-ANGLE LESSON: the stage camera sits only about 14.5 degrees above
  the horizon, so a floor blob is already squashed to roughly a quarter of its
  depth by perspective. Pre-squashing the world footprint as well collapsed the
  shadow to a nine-pixel sliver. The world footprint is now near-circular
  (`GroundShadowDepthRatio = 0.95`), the falloff exponent is 1.3 rather than
  squared because squaring concentrated the darkness exactly where the
  fighter's own feet occlude it, and the quad is nudged 0.30 toward the camera
  so the pool reads in front of the feet. Measured on Champion's Courtyard:
  6762 changed pixels, mannequin feet -3.72 mean luminance, Ryu -1.77.
  *** RETRACTED, was briefly recorded here as a defect and is NOT one. The blue
  sphere over the fighters on Champion's Courtyard is a projectile in flight.
  It looked static only because the ladder captures are deterministic and fired
  at the same tick every run. Re-probing with
  `PROJECT_MANNEQUIN_LADDER_CAPTURE_MINIMUM_ELAPSED` at 60, 300, and 700 shows
  it at columns 894, then 30, then two projectiles at once, at a constant row
  band, which is a flat-trajectory shot behaving correctly. Do not "fix" it.
  The only real observation is a content one: projectiles still use the default
  `MoveData.VisualColor` untextured sphere, so they are placeholder art, which
  belongs to the Phase 5 art queue rather than to rendering.
16. `Stage Rendering And Depth Separation Pass` item 2, the layer depth ramp,
  is landed. `PrototypeStageView.ResolveLayerDepthTint` applies aerial
  perspective per `StageVisualLayerKind`: Far (0.74, 0.80, 0.93), Midground
  (0.88, 0.91, 0.98), Gameplay untouched, Foreground (0.82, 0.85, 0.92), on the
  reasoning that foreground occluders sit between the light and the camera and
  should read as shadowed rather than brighter. Measured on Grand Tournament:
  near-to-far luminance separation 70.10 -> 90.90, and far-band warmth (R minus
  B) 31.95 -> 4.66, with the gameplay floor unchanged at 179 so readability is
  untouched. Archive Nexus stays legible: whole frame 72.75 -> 65.59 but the
  floor holds at 60.7 -> 60.2.
  *** ONLY RGB IS TOUCHED. Alpha must stay at the panel's authored opacity,
  because an alpha below 1 moves the panel into a render path where the backdrop
  composites over it and the whole layer vanishes. That is the bug fixed in
  item 5c.
  *** A DEPTH-SEPARATION GATE WAS TRIED AND DELIBERATELY REMOVED. Failing any
  stage whose near band is darker than its distant band flagged five stages that
  are composed correctly: Dojo Approach is dark dirt under a bright dusk sky,
  and the Archive interiors are dark floors under lit ceilings. Absolute
  luminance difference cannot separate a flat frame from a legitimately dark
  foreground, so `audit_stage_layer_visibility.py` now REPORTS luminance
  separation and colour temperature as context for a human and gates only on
  midground visibility, which is objective.
17. `Stage Rendering And Depth Separation Pass` item 3, the floor lighting
  decision, is settled: **the floor stays `Unshaded`, and the per-actor contact
  shadow remains the sole grounding channel.** This was measured rather than
  assumed. Promoting the floor material to `PerPixel` does produce real
  directional cast shadows from the fighters, visible as streaks following the
  sun, but only 9,952 pixels of them, while brightening 360,766 pixels of floor
  by +35 luma (66.35 to 101.83). The trade fails three ways: it would require
  re-approving every stage's floor brightness, which is art work currently
  blocked behind this very pass; it buys nothing on the five full-frame plate
  stages, where the gameplay floor is deliberately invisible so there is no
  surface to receive a shadow; and it spends the Phase 7 16.7 ms budget on
  shadow-mapping a full-screen plane for a cue the contact shadow already
  delivers on every presentation mode. The reasoning is recorded in the code at
  the material so it is not silently re-litigated. Reverting restored the
  original render exactly, 0 changed pixels.
  Note `WorldRunTests.FloorMaterialIsIsotropic` reads the floor TEXTURE file
  rather than the render, so it is unaffected by this decision either way.
18. `Stage Rendering And Depth Separation Pass` item 4, the key light contract,
  is landed. `StageMissionData.KeyLightPitchDegrees` and `KeyLightYawDegrees`
  declare the direction each stage is authored against; the sun consumes them
  instead of a hardcoded `(-48, -30, 0)`; and `Scripts/Stage/StageKeyLight.cs`
  publishes the active key so presentation code with no reference to the
  mission can align to it. The per-actor contact shadow now leans away from the
  declared key rather than sitting on a fixed offset. `WorldRunTests` is `210`
  and gates that every stage declares a usable direction, that the lean is
  away from the light and mirrors with it, that a low raking key throws further
  than an overhead one, that a key along the view axis produces no sideways
  lean, and that a malformed positive pitch cannot invert the lean.
  *** THE CONTRACT EXISTS BUT IS NOT YET DIFFERENTIATED. Every stage still uses
  the default -48/-30, which is exactly the previous hardcoded value, so nothing
  changed visually except the shadow lean. Setting real per-stage directions is
  an art-review decision: read the painted highlights and shadows in each
  backdrop and declare the direction they were painted for. That is the lock the
  Pavilion and Grand Tournament repaints and the Astral restyle are waiting on,
  so it should be done deliberately with the art in front of you rather than
  guessed from a heuristic.
19. `Stage Rendering And Depth Separation Pass` item 5 is HALF landed: far-layer
  softening is in, the vignette is NOT and needs a design call (see item 21).
  `PrototypeStageView.ApplyFarLayerHaze` enables depth fog on the stage
  `Environment`, coloured with the stage's own far colour. The layer depth tint
  from item 2 is a MULTIPLY, so it could only darken a distant layer and never
  reduce its contrast; aerial perspective is a BLEND toward the atmosphere, and
  that contrast collapse is what the eye actually reads as depth. Depth range
  comes from `StageGroundProjection.ResolveFarHazeRange`, anchored BEHIND the
  deepest standable lane position so haze can never touch a fighter. Skipped
  entirely on `FullFramePlates` stages, where the whole painting sits on one
  billboard at one depth and fog would wash it uniformly instead of grading it.
  Measured on Grand Tournament: fighter-to-backdrop separation 65.38 -> 72.52
  while the fighter itself only moved 134.91 -> 130.27, so the fight reads
  better rather than dimmer. Layer audit near/far separation 90.90 -> 102.57.
  Midground stayed visible on all 7 stages and the junction audit is still 0 gap
  pixels. `WorldRunTests` is `227`. Item 6 is folded into
  `Scripts/Tools/run_stage_layer_visibility_audit.ps1`.
20. CHECK-IN STATE. Commit `680bb74` on `main` holds Phase 6 items 1-4 and stage
  rendering pass items 1-5 plus all tooling: 296 files, 3.13 MB, no binaries,
  build 0/0, suites 34/34/227/11/47. It is LOCAL ONLY and has NOT been pushed.
  The commit boundary is deliberate: `Scripts/`, `Docs/`, `.github/`, root
  config and `Artifacts/*.json` are in; everything under `Assets/` is not.
  ART CHECK-IN IS NOW SIZED AND SAFE. `Scripts/Tools/audit_asset_usage.py`
  classifies every image by what actually references it. Of 199 untracked PNGs
  under `Assets/` only 51 (272.9 MB) are loaded by the game; 32 (227.7 MB) were
  referenced by nothing at all, verified as superseded pilots and intermediate
  walk-repair frames whose successors already shipped. Commit `acb37f0` closed
  a real hazard: only 14 of the 51 REQUIRED assets were LFS-covered, so an
  as-is commit would have put ~230 MB of required art into history raw. Now
  51/51. Commit `c364893` then ignored the working material rather than
  deleting or committing it, taking the pending art commit from 1,163 MB / 199
  files to 558 MB / 78 files.
  *** ROOT CAUSE OF THE OVERSIZED REPO: `Assets/Sprites/Concepts/` already has
  597 tracked binaries with only 12 in LFS, so about 563 MB of working material
  that nothing loads sits in history as raw blobs. That is the largest single
  reason `.git` is 2,491 MB. The ignore rules stop the growth but CANNOT shrink
  history - that needs `git-filter-repo`/BFG, which rewrites every later commit
  hash and breaks existing clones. Decide that deliberately, not in passing.
  *** STILL OPEN: the 216 `Artifacts/StyleCalibration` capture PNGs (320 MB)
  are explicitly UN-ignored by `.gitignore` negation rules, so git still offers
  them, but they have been deliberately left uncommitted as regenerable output
  of the now-committed capture scripts. That is inconsistent: either commit
  them or extend the ignore rules. 110 of the 216 are referenced by nothing.

  DECIDED 2026-07-26 (user): defer the art cleanup until the game is complete,
  then do it in one pass. IMPORTANT CORRECTION TO THAT PLAN - deleting files in
  a later commit does NOT clean the repository. A delete is just another
  commit; every earlier version of the blob stays in history, `.git` does not
  shrink, and a fresh clone still downloads all of it. The only thing that
  actually removes a blob is a history rewrite with `git-filter-repo` or BFG,
  which rewrites every later commit hash and invalidates existing clones, so it
  has to be a deliberate one-time operation with everyone re-cloning after.
  The ignore rules already landed mean NEW working material never enters
  history, so the debt has stopped growing; the end-of-project rewrite only has
  to deal with the ~563 MB of concept art already committed.

  DECIDED 2026-07-26 (user): commit more frequently, at each completed plan
  item rather than at the end of a phase. `Scripts/Tools/check_staged_assets.ps1`
  guards that cadence - it fails if any staged file at or over 1 MB is not
  routed through LFS, which is the exact mistake that built the oversized
  history. Run it before committing, or install it as a pre-commit hook. It is
  verified to block a raw binary and to pass an LFS-covered one.
21. `Stage Rendering And Depth Separation Pass` item 7, the frame-time check,
  is DONE and PASSES with very large headroom. `Scripts/Debug/FrameTimeProbe.cs`
  attaches only when `PROJECT_MANNEQUIN_FRAME_TIME_PROBE=1` and persistence is
  disabled; `Scripts/Tools/run_stage_frame_time_audit.ps1` sweeps 10 stages.
  Worst p95 is 0.84 ms against a 16.7 ms budget - about 5 per cent - with mean
  0.53-0.59 ms, 0 per cent of frames over budget, and 14-29 draw calls. So the
  contact shadow, depth tint, key light and depth fog together cost nothing
  meaningful at current scene complexity.
  *** TWO MEASUREMENT TRAPS, both hit and fixed, do not reintroduce them:
  (a) the probe MUST disable vsync and uncap FPS, or every stage reports the
  refresh interval and a real overrun stays invisible; (b) the sample window
  must be WALL TIME, not frame count. At 1500+ fps the first version's 90+240
  frames covered under 0.2 s and measured an empty arena before any enemy
  spawned. It now warms up 3 s and samples 4 s. The giveaway was `max` being
  an identical 1.39 ms on 8 of 10 stages; with a real window the maxima spread
  to 2.2-6.7 ms.
  Validated by a resolution sweep rather than assumed: cost rises monotonically
  0.52 -> 0.60 -> 0.79 -> 0.83 ms from 360p to 4K, so the probe genuinely sees
  GPU load. Scaling is sublinear, so these stages are draw-call bound, not
  pixel bound. Numbers are machine-specific and from a Debug build; treat them
  as a budget check, not a portable benchmark.
22. `Stage Rendering And Depth Separation Pass` is COMPLETE. The vignette
  landed as `Scripts/Presentation/StageVignette.cs`, a `CanvasLayer` on layer 0
  holding a generated vertical alpha gradient. The user chose TOP AND BOTTOM
  FALLOFF ONLY over a conventional radial vignette, because this is a
  belt-scroller and a radial falloff dims the left and right edges - exactly
  where a cornered player is fighting. The profile is a function of HEIGHT
  alone, so full width stays at full brightness by construction.
  Bands are placed from a measured frame, not by eye: fighters occupy about
  0.45-0.80 of frame height, so the top band fades out by 0.24 and the bottom
  band only starts at 0.88, over what is purely bright floor.
  MEASURED: fighter band -0.01 luma, left edge -0.01, right edge +0.02 at
  fighter height, so the action is untouched. Bottom edge -27.78, top edge
  -17.41. Implied per-row alpha matches the designed profile within 0.003.
  `WorldRunTests` is `232` and gates that alpha is zero across the whole
  fighter band, non-zero at both edges, monotonic inward, never above the
  declared strength, and clamped outside 0..1.
  *** THE TOP BAND IS MOSTLY INVISIBLE IN GAMEPLAY and that is CORRECT, not a
  bug. The vignette sits BELOW the HUD (layer 1) so the HUD is never dimmed,
  and the HUD panel covers about 95 per cent of the width at 0.14 height -
  verified, 1214 of 1280 columns unchanged. The bottom band does the visible
  work. If `HudScale` is ever wired up and the HUD shrinks, the top band will
  start to matter.
  It is excluded from `HideNonStagePresentation`, so clean plate captures are
  unaffected and the plate fidelity comparison against source art still holds.
  COST: +1 draw call and about +0.5 ms at p95 (worst p95 1.35 ms), so the whole
  pass now uses roughly 8 per cent of the 16.7 ms budget.
  The pass was added
  to the master plan on 2026-07-25 from external art review: the base art is
  fine, but the frame is not rendered, so foreground does not separate from
  background.
  This is a code gap and repainting cannot fix it. Verified in the renderer:
  `PrototypeStageView` builds a `DirectionalLight3D` with `ShadowEnabled = true`
  and `CharacterVisualComponent` sets `_sprite.Shaded = true`, but the floor
  material is `ShadingMode.Unshaded` so it CANNOT receive a shadow, and on the
  five `FullFramePlates` stages the gameplay floor is invisible so there is no
  receiving surface at all. Every backdrop and prop `Sprite3D` is
  `Shaded = false` with `CastShadow = Off`, and there is NO per-actor ground
  shadow anywhere in the codebase - the only authored occlusion is
  `CreateBackdropContactShadow`, which measured effective only on bright floors
  (pavilion -42.2, grand tournament -34.9) and negligible or inverted on dark
  Archive floors (reliquary -0.6, intake +15.4). Start with the per-actor
  contact shadow, because it is art-independent and works on plate stages, then
  the `StageVisualLayerKind` depth ramp, then the floor lighting decision, the
  per-stage key direction, far-layer softening, a numeric separation gate in
  `WorldRunTests`, and a frame-time check against the 16.7 ms budget.
23. Only then return to the Phase 5 art queue, starting with the training-crate
  variants. The Pavilion and Grand Tournament backdrop repaints and the Astral
  restyle must be authored against the key direction locked by item 15; any
  stage art produced before that lock is provisional. Nothing in that queue is
  cancelled and no completion gate is waived; only the order changed.
24. Run every deterministic suite with
  `Scripts/Tools/run_all_deterministic_suites.ps1`, which covers input grammar,
  settings, world run, run score, and stage hazards in one headless pass.

## Full Proportion Sizing Sweep

**Scheduled trigger: run this at the end of the current phase** — all four
World Warrior training-crate variants are now complete (Supply Crate, Rack
Chest, Trophy Podium, Lantern Urn); what remains before this sweep is the
rest of the `Remaining Phase 5 > World Warrior` queue (rolling logs, falling
practice props, crowds, destruction), and this sweep runs after that, before
Phase 5 is marked complete or Astral restyle begins. Do not fix these
piecemeal between now and then unless a specific asset blocks other work —
batch it into one pass with one deterministic re-run and one round of fresh
captures, the same way the Rack Chest fix on 2026-07-27 was done.

**Why this exists:** the Pavilion Rack Chest pilot was calibrated with a flat
2D proxy method that is not reliable evidence of true in-game size (Godot's
`Sprite3D.PixelSize` scales the full per-frame texture, not visible content,
and a character's gameplay hurtbox is intentionally smaller than its
rendered sprite). It shipped 44% too small next to the player character and
was corrected same-day (see `World Warrior Pavilion Rack Chest` above). The
user then reported the same kind of smallness in Archive stages ("the train
is ridiculously small") and asked for a full sweep at the end of the phase,
not spot fixes. Measuring confirmed it is systemic, not isolated to one
asset — see `/memories/repo/setup.md` for the full session log, summarized
here:

- **Confirmed too small (~40-44% of the mannequin's true rendered height,
  the same pattern the Rack Chest had before its fix)** — prioritize these
  first: Archive Health Cache (43.9%), Archive Meter Cache (44.1%), Archive
  Data Cache (44.0%), Archive Intake Tram (44.3%), Archive Repository Falling
  Shelf (40.7%), World Warrior Sparring Supply Crate (31.7%, already flagged
  above).
- **Measured and look correct already, no action expected unless the sweep's
  full pass finds otherwise:** Archive Volatile Canister (59.4%), Archive
  Repository Data Debris (65.0%), World Warrior Training Dummy (68.2%),
  Pavilion Rack Chest (63.4%, corrected), Champion's Trophy Podium (56.0%,
  calibrated correctly from the start), Champion's Lantern Urn (59.7%,
  calibrated correctly from the start). The last two confirm the corrected
  from-the-start methodology reliably produces the right scale without a
  post-hoc fix.
- **Needs case-by-case judgment, not the standing-furniture band:** Archive
  Repository Security Sweep (20.3%) is a low floor-level hazard by design and
  may be correct at that scale — do not blanket-resize hazards without
  checking their gameplay function first.
- **Sampled and looked like intentional design, not a bug:** the Archive
  enemy roster's weight-class height progression (Scout 90.2%, Raider 98.7%,
  Bruiser 114.4% of mannequin height) reads as a believable
  lightweight/medium/heavyweight spread. Re-verify during the sweep but do
  not assume this needs correction without new evidence.
- **Not sampled at all yet:** the full Astral roster and prop/hazard set, and
  every Archive/World Warrior enemy, elite, and boss beyond the three sampled
  above.

**Sweep scope (full enumeration required, not a sample):**

1. Every player-usable form, every standard enemy, every named elite, and
   every boss across Archive Nexus, World Warrior, and Astral.
2. Every prop, cache, canister, obstacle, pickup, and hazard/set-piece
   sprite across all three worlds, including ones already marked
   `runtime_approved` — approval predates this gate for most of them.
3. For each: measure true rendered height with
   `Scripts/Tools/measure_true_sprite_world_height.py` (alpha-content bbox
   height × `SpritePixelSize`, using an idle/neutral frame for animated
   sheets) and record the ratio against the canonical mannequin true height
   (~4.10 units).
4. Compare against the target bands in
   `VISUAL_STYLE_BIBLE.md > Proportion and scale calibration`: 55-75% for
   tall standing furniture/apparatus, 25-45% for low/squat floor-level
   objects, 12-20% for pickups. Judge hazards against their authored gameplay
   geometry and function, not a blanket band.
5. Produce one consolidated audit report (a single JSON or markdown table
   listing every asset, its measured ratio, and pass/fail) rather than ad hoc
   notes, so the sweep is auditable and repeatable. Consider extending
   `measure_true_sprite_world_height.py` with a batch/manifest mode driven by
   a generated list of `{path, pixel_size, hframes, vframes}` entries pulled
   from the roster/catalog factories, rather than invoking it one asset at a
   time by hand.
6. Apply corrections in one batch: for single-frame props/hazards, only
   `SpritePixelSize` needs to change (`SpriteGroundOffsetPixels` is fixed
   pixel-space and does not depend on world scale, confirmed during the Rack
   Chest fix) — no art regeneration required unless an asset's alpha content
   itself needs to change. Rebuild, re-run the full deterministic suite, and
   recapture runtime evidence for every asset whose `SpritePixelSize` moved.
7. Update `Docs/VISUAL_STYLE_REVIEW_MANIFEST.json` entries and this file with
   the corrected numbers, and close out this section once every category in
   the scope list has been measured and any real outliers corrected.

## Remaining Phase 5

### World Warrior

Complete these stage-by-stage with the same pilot, provenance, runtime, and
dual-resolution gates:

1. **DONE:** All four training-crate variants (Supply Crate, Rack Chest,
   Trophy Podium, Lantern Urn).
2. **DONE:** Rolling training-log and falling-practice-prop hazard art
   (Dojo Approach Rolling Log + Falling Training Weight, World Warrior's
   first hazard zones of any kind).
3. Spectator/crowd layers and reactions
4. Localized tournament-arena destruction states
5. Stage-specific entrances, landmarks, lane rhythm, recovery pockets, GO
   transitions, lighting, music, and set pieces
5c. **Midground layers were being composited behind the backdrop
  (fixed 2026-07-26).** A player reported "props behind the wall" on Pavilion
  Circuit. The midground panel there and on Grand Tournament Floor carried
  `Opacity = 0.94`, which pushed the whole layer into a render path where the
  Far backdrop drew over it. On Grand Tournament the midground contributed
  **0.47% of the frame**: a gong, a judges' pavilion, trophy plinths with
  braziers, and the championship trophy were fully authored and completely
  invisible. Setting `Opacity = 1.0` on both restored them, taking Grand
  Tournament to 14.50% and Pavilion to 16.12%.
  *** DO NOT assume opacity below 1.0 is the whole story. Dojo Approach also
  sits at 0.94 and draws 36.49%, so the alpha value alone is not sufficient to
  trigger it; it is an interaction with how far the backdrop panel is grown to
  cover the frame. The reliable signal is the measured contribution, not the
  authored value, which is why the audit measures rather than lints.
  `Scripts/Tools/run_stage_layer_visibility_audit.ps1` plus
  `audit_stage_layer_visibility.py` capture every layered stage normally and
  again with each layer hidden; the pixels that differ are exactly what that
  layer contributes. Current: dojo 36.49, pavilion 16.12, grand tournament
  14.50, intake 19.46, index vaults 21.82, corruption 27.56, reliquary 10.81.
  Gate fails below 1% midground.
  *** SEPARATE DEFECT, also fixed: raising panel opacity cannot fix
  see-through props, because that transparency lives in the TEXTURE's alpha.
  Pavilion's midground art carries 2.64% partial alpha, and the heavily
  transparent share showed the deck wall through stone plinths and drum
  emblems. `StageBackgroundPanelData.AlphaSolidify` remaps mid-range alpha to
  opaque while keeping a narrow soft band so silhouette edges stay
  anti-aliased. Pavilion uses 0.65. Every other midground carries 0.85-2.35%
  partial alpha, so the same treatment may be wanted there after review.
  *** IMPLEMENTATION TRAP in that remap: `Texture2D.GetImage()` returns every
  mip level concatenated, so walking the byte array with a 4-byte stride
  corrupts the smaller levels. Call `ClearMipmaps()` first and
  `GenerateMipmaps()` after. Use raw `GetData`/`SetData` with a 256-entry
  lookup table rather than `GetPixel`/`SetPixel`; these panels are
  multi-megapixel and per-pixel marshalling is a load hitch.
  Parallax layers now also declare `RenderPriority` (Far -6, Midground -3,
  Gameplay 0, Foreground 3) so layer order is explicit. That alone did NOT fix
  the suppression, but it removes the ambiguity.
  Diagnostics kept, both gated on `IsPersistenceDisabled`:
  `PROJECT_MANNEQUIN_TINT_LAYER` and `PROJECT_MANNEQUIN_HIDE_LAYER`. They are
  what actually cracked this; a tint proved the wall belonged to Far, and
  hiding Far proved the props existed.
5b. **Backdrop/floor junction is gated (2026-07-25), and the result is that
  there is no gap on any layered stage.** `Scripts/Tools/run_stage_junction_audit.ps1`
  plus `Scripts/Tools/audit_stage_backdrop_junction.py` render each stage TWICE
  with different forced clear colours and diff the two frames. A pixel that
  changes is showing the background and nothing else, so it is a genuine hole; a
  pixel that does not change is covered by geometry. All seven layered stages
  report 0 gap pixels.
  *** A FIRST VERSION OF THIS AUDIT WAS WRONG AND ITS NUMBERS ARE RETRACTED. It
  matched pixels against the declared clear colour in a single render and
  reported that five of seven stages seamed, with Pavilion Circuit at 100% of
  frame width. That was entirely false positives. These paintings contain flat
  dark horizontal lines - shadow gaps between deck levels, mortar courses - that
  sit within RGB distance 12 of the clear colour and are just as uniform across
  the frame. The Pavilion backdrop source alone has five rows that are 70-88%
  near-clear-colour. Neither colour distance nor run uniformity could separate
  painted art from a real hole; only the two-render differential can.
  Two other approaches were tried and rejected and should not be re-attempted:
  per-column painted-edge spread measures architectural silhouette rather than
  junction quality and scored Dojo Approach, the plinth reference, worse than
  Grand Tournament; and full-frame captures are useless because the HUD is
  itself a dark panel that dominates every stage identically.
  So what the player sees at the Grand Tournament wall base is NOT background
  leaking through. It is a hard cut: the painted wall has no plinth, base, or
  contact shading where it meets the floor, which is exactly the repaint already
  queued below.
6. Repaint the Pavilion and Grand Tournament backdrops with a painted ground
   line - plinths, bases, and contact shading at the foot of columns and
   doorways - so their architecture reads as standing on the floor rather than
   being cut off at it. Dojo Approach already does this and is the reference.
   The renderer's contact shadow narrowed the gap on these two bright-floor
   stages but cannot substitute for a painted base; see `Open Stage Defects`
   item 3 for the measurements. The four Archive backdrops have the same fault
   and should be picked up when Archive is next revisited.

Do not reuse the rejected tournament composite as three supposedly unique
stages. Do not treat Ryu's personal-use prototype art as redistributable
original roster art.

### Astral Battlefront

World Warrior is followed by the full Astral art-completeness pass:

1. Four distinct stage/floor compositions; route paintings may guide style but
   cannot be silently repeated as complete stage identities
2. Explicit base-enemy runtime style review and any required replacements
3. Unique Saibaman Alpha, Vanguard Commander Lyra, and Ki Captain Prime atlases
4. Capsule caches and independent Astral health, meter, and score pickups
5. Ki-strafe emitter/effects, meteor/debris, energy-current push, pulse-floor
   machinery, and tournament destruction states
6. Per-stage entrances, landmarks, recovery decisions, music/lighting, boss
   escalation, and dual-resolution runtime approval

No fourth-world full production begins until Archive, World Warrior, and Astral
all reach the master plan's console-demo quality gate.

#### Astral is deliberately deferred, not finished

Astral stage art has not had its style pass. The visual bible's replacement
order applies the approved grammar to World Warrior first, then Astral, and the
pending list still names Astral stages. Do not read the current Astral runtime
as an approval.

All four Astral stages currently render one full-frame scene plate built by
`Scripts/Tools/prepare_full_frame_plate.py` from the **pre-restyle**
`astral_route_*_higgsfield_v2` masters. That was a presentation repair for
fighters hovering above the painted floor, not an art pass. The manifest records
it as `astral_battlefront_interim_scene_plates` with status
`runtime_fidelity_approved`, which asserts only that the runtime reproduces the
source image. It is not `runtime_approved` and must not be counted as complete.

When the Astral pass begins, replace those four plates with restyled art and
re-review to `runtime_approved`.

#### Hard gates the Astral pass must satisfy

These come from defects already found and fixed elsewhere; they are cheap to
honour up front and expensive to retrofit.

1. **Declare the painted ground band.** Every plate sets `GroundFarFraction` and
   `GroundNearFraction`, and the stage must pass
   `FullFramePlatesGroundFighters` in `WorldRunTests`. Exact-16:9 checks and
   pixel-fidelity compares both pass while fighters hover in mid-air; only the
   projection in `Scripts/Stage/StageGroundProjection.cs` catches it. Energy Rail
   shipped with 72 percent of its belt depth over painted sky.
2. **Paint a deep enough floor.** The Astral belt is `6.4` world units, which
   needs roughly `0.16` of frame height of painted walkable ground at the
   resting camera. A shallower plaza cannot be calibrated into place.
3. **One painting per stage.** Energy Rail previously used three plates, two of
   which were byte-identical to the Skyfall and Tournament Summit masters, so
   Stage 3 displayed Stage 1 and Stage 4 scenery. Verify with
   `HaveDistinctContent` and by comparing pixels, not filenames.
4. **No stacked composites.** Source plates must pass
   `Scripts/Tools/audit_stage_plate_sources.py`; any interior flat strip wider
   than two percent of image height means a backdrop and a floor were stacked
   rather than painted as one scene.
5. **Isotropic floors only** if Astral keeps any layered stage. Floor materials
   must pass `Scripts/Tools/audit_stage_floor_materials.py` at `0.55` or better;
   detail that runs in courses collapses into screen-wide stripes under the
   stage camera.
6. **Pad, never crop or rescale.** Use `prepare_full_frame_plate.py` so the whole
   painting survives, then confirm with `audit_stage_plate_fidelity.py`.
7. **Capture and review both resolutions** with
   `Scripts/Tools/capture_stage_ground_probe.ps1` and
   `Scripts/Tools/capture_stage_only_frame.ps1`, and record the result in the
   review manifest.

## Open Stage Defects

Measured and reproducible. Items 1, 2, 5, and 6 are resolved and kept for
provenance. Items 3 and 4 remain open and are bounded by art rather than code.
None of these block the current World Warrior crate work, and none of them
concern Astral.

1. **Fixed: Archive Stage 3 black band.** The bar across Corruption Repository
   was the backdrop painting's own ground-contact shadow. `AlignBottomToFloor`
   anchored the image's bottom edge to world `y = 0`, which put that shadow
   above the gameplay ground line instead of below it. Backdrops now declare
   `GroundLineFraction`, the image row where their painted content ends, and are
   anchored by that row instead, so flat filler below it extends under the floor
   and is occluded. Measure with
   `Scripts/Tools/audit_stage_backdrop_ground_line.py` and verify with
   `Scripts/Tools/capture_layered_stage_only_frames.ps1`; all seven layered
   stages report zero dark bands and zero uncovered rows at the top of frame.

   The first attempt at this got the values badly wrong and is worth not
   repeating. A brightness-and-structure heuristic was used to find where the
   architecture stopped, but these backdrops pad with flat dark grey around
   luminance `20` rather than black, so the heuristic walked straight past the
   filler and kept eating real architecture: dojo was anchored at `0.696`
   against a true content edge of `0.7485`, pavilion at `0.780` against `0.9539`,
   grand tournament at `0.760` against `0.9769`, and Corruption Repository at
   `0.820` when it has no filler at all. Between `5` and `22` percent of each
   painting was pushed under the floor, which sheared the bases off columns,
   posts, and doorways. The detector now looks for flatness rather than
   darkness, which is what actually distinguishes filler from painted content.
2. **Fixed: floor materials that collapse into stripes.** Two wired materials
   failed the anisotropy gate and were regenerated as isotropic paving.
   `world_warrior_pavilion_floor_style_v2` scores `0.9629` where v1 scored
   `0.0703`, and `world_warrior_grand_tournament_floor_style_v2` scores `0.9392`
   where v1 scored `0.3584`. All seven wired floors now pass, between `0.9392`
   and `1.0947`. The quarter-turn mitigation and its `FloorTextureQuarterTurns`
   knob are removed. Worth remembering:
   `archive_index_vaults_floor_higgsfield_v1` was reported as a third failure,
   but Index Vaults never wired it - the stage uses
   `archive_index_vaults_floor_style_v2`, which already scored `1.0947`. Auditing
   by guessed filename produced that false positive, so `WorldRunTests` now
   measures the material each stage actually wires and fails that stage directly.
3. **Backdrops without painted ground contact still read as cut.** Dojo Approach
   paints plinths under its posts and reads as resting on the ground. The
   Pavilion, Grand Tournament, and all four Archive backdrops are elevations
   cropped at their bottom edge with no plinths or contact shading, so although
   the painting meets the floor exactly, columns and doorways terminate on a
   hard line.

   `CreateBackdropContactShadow` now lays a soft occlusion band on the ground
   against the far backdrop, strongest at the junction and fading forward over
   `3.2` world units. It only half solves the problem, and the numbers say why.
   Measured darkening at the junction against the floor ahead of it: Pavilion
   `-42.2` and Grand Tournament `-34.9`, where a bright floor gives the shadow
   something to bite into; Corruption Repository `-10.0`; Dojo `-3.5` and
   Knight's Reliquary `-0.6`; and Intake Boulevard `+15.4` and Index Vaults
   `+12.9`, where floor content varies more than the shadow darkens it. So the
   two bright World Warrior floors read as contact now, and the dark Archive
   floors are essentially unchanged. A shadow under a column that has no painted
   base still reads as a cut column, so closing this properly means repainting
   those backdrops with a ground line.
4. **Mirrored floor tiling is visible.** The renderer builds a two-by-two
   mirrored seamless tile. The symmetry axis is anchored in world space, so it
   slides as the camera pans rather than sitting permanently on screen: the
   left/right mean absolute difference across the Archive Stage 3 floor is
   `41.7` at the first encounter but `17.7` at the third, and the mirrored pink
   cracks are obvious at the latter. Replacing the mirror with an offset blend
   was tried and reverted: it cost `27` percent of floor contrast without
   reducing the symmetry. The real fix is floor materials authored to tile
   seamlessly on their own; the item 2 replacements improved isotropy but were
   not authored against a wrap contract, so the mirror is still required.
5. **Fixed: Champion's Courtyard plate seam.** The v1 plate was a backdrop
   stacked on a floor tile: a flat blue-grey strip `4.07` percent of image height
   at rows `0.630` to `0.670`, over pavement at a scale the architecture
   contradicted. `world_warrior_champions_courtyard_full_frame_plate_style_v2` is
   one continuous dusk courtyard painted in a single perspective, padded to exact
   `5504x3096` by `prepare_full_frame_plate.py`. It reports no interior flat
   band, and its painted walkable band `0.715` to `0.985` contains the projected
   belt `0.772` to `0.928` with margin on both sides. Generated by
   `Scripts/Tools/generate_champions_courtyard_plate_pilot.ps1`; v1 is retained
   as provenance.
6. **Fixed: unreferenced duplicates.** `astral_energy_rail_full_frame_plate_02.png`
   and `astral_energy_rail_full_frame_plate_03.png` duplicated the Skyfall and
   Tournament Summit plates byte for byte once each Astral stage took its own
   painting. Both are removed along with their import metadata, their stale
   clean-runtime captures, and their Git LFS attribute lines; the review manifest
   no longer cites them.

## Work After Phase 5

Phase 6 remains responsible for:

- A separate robust settings store
- Audio/display/render/HUD/shake/flash/control options
- Non-color-only readability and high-contrast telegraphs
- Input glyph switching, reconnect handling, and rebinding
- Versioned atomic saves, migration, backup recovery, and reset confirmation
- Archive Hub meta surfacing, replay access, and bounded assist/difficulty hooks

Phase 7 remains responsible for:

- Cross-project animation and visual-consistency QA
- Stage/boss camera tuning and zero-shake accessibility
- Real 1080p/60 and split-screen performance profiling
- Bounded VFX/text/audio/node pools and transition hitch checks
- Asset-path/import/frame/hash/orphan validation across all content

The subordinate [NEXT_IMPLEMENTATION_PLAN.md](NEXT_IMPLEMENTATION_PLAN.md) also
retains three required tactical tracks: gamepad-as-P1 QA sign-off, one complete
combat-spectacle vertical slice, and a real sequential-boss Hollow Archive.
Iron Fist Foundry remains gated as documented there and in the master plan.

## New Machine Setup

Required runtime tooling:

- Godot 4.7 .NET/Mono edition
- .NET 8 SDK, 64-bit
- Python environment with Pillow, NumPy, and SciPy for atlas composition/audits
- Higgsfield CLI for new generated art

The old machine used hardcoded paths under `C:\Users\Joseph Bundrant`. Those
paths are not portable. On the new machine:

1. Clone the repository and check out `main`.
2. Pull `origin/main` and verify the handoff file exists.
3. Install Godot and set a local `$godot` path in PowerShell commands.
4. Recreate/activate `.venv`; do not transfer the old virtual environment.
5. Install or locate `higgsfield.exe`, then update/localize the `$cli` path in
   generation scripts before running them.
6. Open the Godot editor once headlessly so new PNG resources import.
7. Run `dotnet build .\ProjectMannequin.csproj --no-restore`.
8. Run the focused world/hazard baseline before editing and require
  `199/199 + 47/47` at this checkpoint.

Machine-local Godot logs, progress saves, and environment variables are not
authoritative. Use `PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE=1` for evidence runs
and clear stale `PROJECT_MANNEQUIN_*` variables between scenarios.

## Non-Negotiable Guardrails

- Follow `.github/instructions/project-mannequin-visual-style.instructions.md`.
- Treat mannequin, Ryu, and Goku as rendering anchors, not identities to copy.
- Keep rejected sources and legacy assets as provenance; do not delete or
  silently overwrite them.
- A technically valid atlas is not visually approved.
- Generate one pilot before batching any new family.
- Require at least `14/16`, no automatic failure, explicit review metadata, and
  exact `1280x720` plus `1920x1080` runtime captures.
- Never mark a stage or phase complete while the art-completeness plan still has
  an applicable open gate.
- Aspect, dimension, and pixel-fidelity checks never prove that fighters stand on
  the painted floor. A stage is not approved until its belt grounding assertion
  and its floor material audit both pass.
- `runtime_fidelity_approved` is not `runtime_approved`. It asserts only that the
  runtime reproduces the source image, and must never be counted as a style pass.
- Do not weaken or remove deterministic tests merely to restore a green count.
- Verify Godot log contents and named assertions; wrappers have previously
  raced file creation or hidden stale-DLL execution.

## Fresh Session Prompt

Use this prompt on the new machine:

```text
Do not restore, search, or summarize any previous Copilot session. Do not run
git status. Read Docs/PROJECT_HANDOFF.md and continue only from its Immediate
Continuation Order and the live files. Champion's Courtyard, Dojo Prodigy
Kenzo, Pavilion Ace Makoto, Grand Grappler Tetsu, the World Warrior training
dummy, Vitality Gourd, Focus Drum, Judge's Laurel Fan, and the Sparring Supply
Crate are already runtime approved and must not be regenerated. The current
slice is the remaining World Warrior training-crate variants, built on the
proven Sparring Supply Crate path. Review each raw pilot before any hazard
batch. The starting baseline is 199/199 world plus 47/47 hazard, with
24/24 deterministic input and 11/11 RunScore assertions.
```
