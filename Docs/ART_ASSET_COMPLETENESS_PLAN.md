# Art Asset Completeness Plan

## Roadmap Authority

This document is a mandatory completion gate under
[MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md). The master plan
owns product scope and sequencing; this document determines whether a content
slice is technically and artistically complete. Neither a tactical plan nor a
backlog can waive these gates by omitting them.

## Purpose

A mechanics-complete stage is not art-complete. A stage, enemy, prop, obstacle, pickup, or hazard cannot be called finished while it relies on a missing path, procedural fallback, unrelated reused sprite, or tint-only identity.

Technical completeness is also not visual-style approval. A unique, imported, correctly scaled asset still fails if it does not match the accepted mannequin/Ryu/Goku rendering language. [VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md) is authoritative for style references, prompt locks, category rules, rejection criteria, and review scoring.

This plan is a release gate for every remaining content phase. Art direction, pilot generation, style review, import, runtime wiring, automated audit, and rendered capture belong to the same implementation slice.

## Completion states

| State                                      | Meaning                                                                                         |
| ------------------------------------------ | ----------------------------------------------------------------------------------------------- |
| Mechanics pending                          | Layout, combat, hazard, or progression behavior is incomplete.                                  |
| Mechanics complete / art pending           | Gameplay is deterministic and tested, but one or more art gates below fail.                     |
| Art technically integrated / style pending | Bespoke assets are imported and wired, but the style rubric has not passed.                     |
| Style approved / capture pending           | Raw assets pass the visual bible, but runtime composition has not passed final QA.              |
| Slice complete                             | Mechanics, technical art gates, style review, runtime capture, and readability checks all pass. |

## Hard art gates

A stage or phase is complete only when every applicable requirement passes:

1. **Unique stage identity** — a dedicated backdrop/floor pair or authored subzone panel set exists. A crop, flip, tint, or repeated source-stage image is not a new stage.
2. **Unique elite identity** — each named elite uses a distinct runtime sheet or animation-ready visual variant with a clearly different silhouette. Name, stats, gold tint, and aura alone do not qualify.
3. **Bespoke gameplay objects** — every visible prop, cache, canister, obstacle, and pickup has an existing intentional sprite. Different gameplay types cannot share one image and differ only by tint.
4. **Themed hazard art** — each stage-defining set piece has themed art/effects beyond generic procedural warning geometry. Deterministic collision and accessible telegraphs remain authoritative.
5. **No silent fallback** — all release-facing `res://` paths resolve through `ResourceLoader`; missing art is a validation failure.
6. **Uniqueness audit** — hashes must differ where uniqueness is promised. Intentional repeats must be named and documented.
7. **Import QA** — alpha, frame count, dimensions, sampling, grounding offset, pixel size, and compression are validated.
8. **Canonical style references** — every generated-art job uses approved mannequin/Ryu/Goku anchors; an off-style asset is never the sole reference for another asset.
9. **Style rubric** — every asset passes the visual bible at 14/16 or better, with no automatic-fail condition and explicit review metadata.
10. **Runtime cohesion QA** — inspect beside approved characters at 1280×720 and 1920×1080 for style cohesion, seams, scale, contrast, occlusion, telegraph readability, and detail noise.
11. **Proportion sizing** — every character, enemy, prop, and pickup has its true rendered world height measured with `Scripts/Tools/measure_true_sprite_world_height.py` (alpha-content bounding box × pixel size, never the gameplay hurtbox or an assumed frame size) and compared against the canonical mannequin/Ryu/Goku true height. The result must land in the target ratio band from `VISUAL_STYLE_BIBLE.md > Proportion and scale calibration`, and every roster member of a comparable weight class must land within a similar band of each other. A flat 2D proxy comparison alone does not satisfy this gate — an actual runtime capture confirmation is required. **Retroactive:** assets already marked `runtime_approved` below (including the whole Archive quality bar) were approved before this gate existed and are not exempt — see `Docs/PROJECT_HANDOFF.md > Full Proportion Sizing Sweep` for the scheduled cross-world audit and its confirmed findings so far (several Archive caches and the Intake Tram already measure too small).
12. **Lighting contract** — stage and backdrop art authored after the `Stage Rendering And Depth Separation Pass` must agree with the per-stage key direction locked by that pass. Painted lighting that contradicts the runtime key is a failure, not a style preference. Art produced before that lock is provisional and is re-reviewed against it.
13. **Performance** — new art stays within texture-memory and draw-call budgets and does not introduce recurring frame hitches.

## Mandatory style workflow

1. Start each asset family with one pilot; do not batch a family from an unapproved pilot.
2. Use the style-lock prompt and canonical reference stack from the visual bible.
3. Review the raw pilot next to mannequin, Ryu, and Goku before processing or wiring.
4. Reject photoreal/PBR, gritty hard-surface, generic mobile-loot, painterly-concept, and dashboard-UI drift even when the image is attractive.
5. Approve contour language, broad cel/value planes, silhouette, detail density, material treatment, and color hierarchy.
6. Only after raw-art approval: remove backgrounds, process sheets, import, calibrate metrics, and wire runtime paths.
7. Capture calm and combat-dense states, then record final approval in [VISUAL_STYLE_REVIEW_MANIFEST.json](VISUAL_STYLE_REVIEW_MANIFEST.json).

## Verified audit baseline

- Archive Nexus previously derived four ladder stages from one district art source.
- World Warrior Sector previously derived all four ladder stages from one tournament composite; Dojo Approach, Pavilion Circuit, Grand Tournament Floor, and Champion's Courtyard now use four independent runtime-approved layered environment families.
- Astral Battlefront has four route paintings, but repeats route images and uses hero imagery as its floor fallback.
- Archive named elites previously reused base archetype sheets with a gold tint/stat boost; Veyra, Rhune, and Basalt now use distinct animation-ready atlases. World Warrior Kenzo, Makoto, and Tetsu are also complete; the Astral elites remain pending.
- The prior boulder, crate, and meter-pickup paths were missing.
- Health, meter, and score pickups previously referenced the same missing image and differed by tint.
- Hazard presentation still uses procedural strips/discs as its deterministic accessibility layer, with runtime-approved Intake Tram, Index Vault scans, Repository falling/sweep emitters, and persistent aftermath art layered above it. The reusable deterministic `PushZone` runtime and validation contract are implemented; its first production-authored art/use remains in the Astral pass.
- The first Archive environment and gameplay-object replacement pass solved missing/reused-path problems but drifted toward dense realistic hard-surface sci-fi. The seven gameplay objects, all four Archive environments, the Archive Knight combat/intro package, Reliquary phase spectacle, gameplay HUD, Archive Map, results, pause, and form-select have since been superseded at runtime by approved style-v2 families. Archive art and shared UI are complete.
- The gameplay HUD now uses the approved Living Archive broadcast frame and active-form portraits; the Archive Map uses an illustrated navigation chamber with native route and checkpoint controls.

## Current Archive status

| Slice                 | Mechanics           | Environment                          | Props/pickups                                                   | Elite                                                                   | Hazard set-piece art                                                                 | Capture                                                                                                                                              | Status         |
| --------------------- | ------------------- | ------------------------------------ | --------------------------------------------------------------- | ----------------------------------------------------------------------- | ------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------- | -------------- |
| Intake Boulevard      | Complete            | Layered environment runtime approved | Health and data caches/pickups runtime approved                 | Scout and Index Warden Veyra runtime approved                           | Intake Tram runtime approved                                                         | Replacement calm/live environment and full ladder pass at 720p/1080p; legacy failure retained as provenance                                          | Slice complete |
| Index Vaults          | Complete            | Layered environment runtime approved | Health and meter caches/pickups runtime approved                | Raider and Cipher Captain Rhune runtime approved                        | Mirrored scan emitter/field family runtime approved                                  | Calm/live environment, scans, Raider/Cross, Rhune/Cipher Cross, and cache/drop flows pass at 720p/1080p                                              | Slice complete |
| Corruption Repository | Complete            | Layered environment runtime approved | Health/meter/data family and volatile canister runtime approved | Bruiser and Overseer Basalt runtime approved                            | Falling shelf/data debris, security sweep, and persistent aftermath runtime approved | Calm/live environment, shelf/debris impacts, security sweep, aftermath, canister detonation, Bruiser/Hammer, and Basalt/Faultline pass at 720p/1080p | Slice complete |
| Knight's Reliquary    | Complete final loop | Layered environment runtime approved | Not applicable yet                                              | Unique Archive Knight atlas and identity-matched intro runtime approved | Two progressive destruction states and localized phase bursts runtime approved       | Clear/combat environment, Knight idle/cleave, matchup/intro peak, phase-2/phase-3 burst/settled states, and full progression pass at 720p/1080p      | Slice complete |

## Superseded Archive assets retained for provenance

### Legacy environments

- `archive_district_backdrop_band_higgsfield_v1.png`
- `archive_district_floor_higgsfield_v1.png`
- `archive_index_vaults_backdrop_higgsfield_v1.png`
- `archive_index_vaults_floor_higgsfield_v1.png`
- `archive_corruption_repository_backdrop_higgsfield_v1.png`
- `archive_corruption_repository_floor_higgsfield_v1.png`
- `archive_knights_reliquary_backdrop_higgsfield_v1.png`
- `archive_knights_reliquary_floor_higgsfield_v1.png`

These remain structure/provenance references and are not selected by the runtime Archive catalog.

### Superseded legacy props and pickups

- `archive_health_cache_higgsfield_v1.png`
- `archive_meter_cache_higgsfield_v1.png`
- `archive_data_cache_higgsfield_v1.png`
- `archive_volatile_canister_higgsfield_v1.png`
- `archive_health_pickup_higgsfield_v1.png`
- `archive_meter_pickup_higgsfield_v1.png`
- `archive_data_pickup_higgsfield_v1.png`

These remain only as provenance/structure references and are not selected by the runtime Archive catalog or hazard factory.

### First style-calibrated runtime replacements

- `archive_health_cache_style_v2.png` — 16/16, runtime approved at 1280×720 and 1920×1080.
- `archive_meter_cache_style_v2.png` — 16/16, runtime approved with `SpritePixelSize=0.00103` and ground offset `788`.
- `archive_data_cache_style_v2.png` — 16/16, runtime approved with `SpritePixelSize=0.00136` and ground offset `674`.
- `archive_volatile_canister_style_v2.png` — V1 rejected for health-like iconography; revised V2 is 16/16 with `SpritePixelSize=0.00143`, ground offset `854`, and real pre-break/detonation captures.
- `archive_health_pickup_style_v2.png` — 16/16, runtime approved with `SpritePixelSize=0.00038`, ground offset `672`, and refreshed isolated real-drop captures.
- `archive_meter_pickup_style_v2.png` — 16/16, runtime approved with `SpritePixelSize=0.00028`, ground offset `876`, and isolated real-drop captures.
- `archive_data_pickup_style_v2.png` — 16/16, runtime approved with `SpritePixelSize=0.00036`, ground offset `698`, and isolated real-drop captures.
- `project_mannequin_strike_burst_style_v1.png` — 16/16, pooled authored strike VFX with procedural fallback; runtime approved at both target resolutions.
- Archive canister detonation — 16/16 pooled graphic cel effect with tapered rays, impact-star cores, shaped debris, immediate intact-sprite cleanup, readable damage text, and render-timed captures at both target resolutions.
- `archive_raider_style_v2.png` — 16/16 imported 2560×2304 real-alpha 10×9 atlas with 40 authored poses, stable baseline/scale, untinted porcelain colors, phase-aligned ten-frame Raider Cross, and live idle/attack captures at both target resolutions.
- `archive_scout_style_v2.png` — 16/16 lightweight 10×9 atlas with phase-aligned Scout Jab and dual-resolution live captures.
- `archive_bruiser_style_v2.png` — 16/16 heavyweight 10×9 atlas with phase-aligned Bruiser Hammer and dual-resolution live captures.
- `index_warden_veyra_style_v1.png` — 16/16 unique Stage 1 elite atlas with Warden Decree, halo-preserving pose extraction, authored cyan-white room light, and dual-resolution live captures.
- `cipher_captain_rhune_style_v1.png` — 16/16 unique Stage 2 elite atlas with Cipher Cross, authored cool-white room light, and dual-resolution live captures.
- `overseer_basalt_style_v1.png` — 16/16 unique Stage 3 elite atlas with Faultline Driver, authored violet-white room light, and dual-resolution live captures.
- `archive_knight_style_v1.png` — 16/16 unique final-boss and inherited-form atlas with 60 populated frames, matched row scales, stable broken-arch pauldron/forearm blade identity, and dual-resolution idle/cleave captures.
- `archive_knight_intro_style_v1.png` plus explicit Knight/mannequin portraits — 16/16 identity-matched eight-frame intro and illustrated matchup package with a settled VS/title/taunt card, authored frame-2 power peak, and dual-resolution lifecycle captures.
- Reliquary phase destruction — 16/16 progressive left-to-right arena damage with distinct real-alpha phase-2/phase-3 persistent states, localized porcelain/crystal bursts, and real `broken_seal`/`last_archive` event captures.
- `project_mannequin_hud_frame_style_v2.png` — 16/16 real-alpha Living Archive broadcast frame with active-form portraits, contained native bars, concise split telemetry, centered 16:9 safe-area behavior, and exact 720p/1080p/21:9/4:3 plus two-player split-screen evidence.
- Archive Map — 15/16 illustrated Living Archive navigation chamber with native realm selection, a four-node cyan route spine, responsive 720p/1080p/21:9 layouts, and safe overwrite confirmation.
- Results family — 16/16 real-alpha asymmetric rank/tally plate shared by Stage Clear, World Complete, and Game Over, with seven transactional route smokes and responsive 720p/1080p/21:9 evidence.
- Pause family — 16/16 real-alpha asymmetric record plate shared by root, Move List, Move Cards, Form Loadout, and Artifacts, with nested Escape/resume and responsive exact captures.
- Form-select — 16/16 real-alpha inheritance selector with a full-body preview, centered current-form vault, movable native cursor, explicit Goku confirmation, and responsive exact captures.
- `archive_intake_tram_style_v1.png` — 15/16 real-alpha themed hazard layered above the procedural warning strip; calibrated to `SpritePixelSize=0.00220`, ground offset `419.5`, and a four-unit collision footprint with dual-resolution warning/active captures.
- `archive_index_scan_emitter_style_v1.png` and `archive_index_scan_field_style_v1.png` — 15/16 mirrored Stage 2 hazard family; real-alpha emitter uses `SpritePixelSize=0.00090`, ground offset `935`, inset anchors `0.08/0.92`, and shared directional field UVs above the procedural scan strips.
- `archive_repository_falling_shelf_style_v1.png` and `archive_repository_data_debris_style_v1.png` — 15/16 Stage 3 falling-strike family; mirrored shelves use `0.00150/546.5`, center debris uses `0.00170/747`, and all descend `4.4` world units above the procedural impact discs.
- `archive_repository_security_sweep_style_v1.png` — 16/16 hovering linear-sweep emitter; calibrated to `0.00125/300.5`, right-facing with X anchor `0.85`, and layered above the moving procedural warning/active strip.
- `archive_repository_explosion_decal_style_v1.png` and `archive_repository_impact_fragments_style_v1.png` — 16/16 persistent aftermath family shared by canister explosions and falling impacts; source-specific decals pair with a `0.00068/432.5` low fragment pile and clear on encounter changes.
- Intake Boulevard layered environment — 15/16 runtime-approved asymmetric far backdrop, dark top-down floor, real-alpha midground, and split entrance/exit foreground pieces with ordered parallax and exact calm/live captures.
- Index Vaults layered environment — 15/16 runtime-approved far backdrop, dark top-down floor, three-landmark real-alpha midground, and split entrance/exit foreground rails with ordered parallax and exact calm/live captures.
- Corruption Repository layered environment — 15/16 runtime-approved asymmetric damaged-vault backdrop, 1.78-percent-light top-down floor, three-landmark real-alpha midground, and split containment-remnant foreground with ordered parallax and exact calm/live captures.
- Knight's Reliquary layered environment — 15/16 runtime-approved asymmetric ceremonial backdrop, 1.35-percent-light broken-seal floor, low three-landmark real-alpha midground, and split oath-rail foreground with ordered parallax and exact clear/combat captures.

All seven style-v2 objects are independent real-alpha images with function-specific silhouettes and no tint-only identity. The legacy images remain in the repository for provenance but no longer seed or supply runtime presentation.

Current Archive enemy/object/hazard/environment/UI verification on 2026-07-17: clean build, 140/140 world-run assertions plus 37/37 stage-hazard assertions (177/177 aggregate), 16/16 Archive Map model assertions, 12/12 results-flow model assertions, 4/4 form-select model assertions, seven results route smokes, complete pause and form-select UI smokes, Stage 1–4 ladder summaries, and responsive runtime evidence for every approved Archive content/UI family.

Current cross-world verification after the World Warrior Sparring Supply Crate, Pavilion Rack Chest, Grand Tournament Champion's Trophy Podium, Champion's Lantern Urn, Dojo Rolling Log, Dojo Falling Training Weight, Judge's Laurel Fan, walk-cycle QA expansion, and director-level PushZone integration: 247/247 world-run assertions plus 60/60 stage-hazard assertions (307/307 aggregate), with exact Rookie, Striker, Grappler, Kenzo, Makoto, Tetsu, training dummy, supply crate, rack chest, trophy podium, lantern urn, rolling log, falling weight, health pickup, meter pickup, score pickup, Dojo, Pavilion, Grand Tournament, and Champion's Courtyard evidence passing at 720p and 1080p.

### Completed animated enemy and elite replacement

- Archive Raider — 16/16 runtime approved. Six identity-locked final source sheets compose through authored connected-silhouette anchors into `archive_raider_style_v2.png`; rejected rusty sources and failed revisions remain provenance only.
- Archive Scout and Bruiser — 16/16 runtime approved at lightweight/heavyweight scales with phase-aligned Scout Jab and Bruiser Hammer rows.
- Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt — 16/16 unique named-elite atlases with distinct silhouettes, attacks, room lights, hashes, and dual-resolution runtime evidence; no tint-only identity remains in Archive Stages 1–3.
- Archive Knight — 16/16 unique final-boss and inherited-form atlas with 60 populated frames, exact identity across grouped source families, clean Archive Cleave evidence, and no tint-only mannequin fallback.
- Archive Knight intro — 16/16 eight-frame real-alpha atlas and explicit illustrated matchup portraits with the approved Knight identity, stable peak/ready cadence, and no legacy purple or procedural-card fallback.
- World Warrior Dojo Rookie — 16/16 original indigo/saffron rushdown identity with seven style-locked source sheets, 60 populated atlas frames, exact Quick Palm phase timing, and dual-resolution Stage 1 evidence. The Ryu-derived legacy atlas remains provenance only.
- World Warrior Pavilion Striker — 16/16 original vermilion/indigo kick specialist with 60 populated frames, exact Turning Kick phase timing, and dual-resolution Stage 2 evidence. Deterministic component filtering excludes generated source-sheet dividers and stray shoes without touching intended figures.
- World Warrior Tournament Grappler — 16/16 original plum/indigo heavyweight wrestler with 60 populated frames, exact Shoulder Drive phase timing, and dual-resolution Stage 3 evidence. Weak v1 and v2a attack sources remain rejected provenance.
- World Warrior Dojo Prodigy Kenzo — 16/16 distinct named-elite archetype with a unique 60-frame atlas, exact Master Palm timing, unique arcade-profile identity, and dual-resolution Stage 1 evidence. Fist-bearing v1 and back-facing v2 startup sources remain rejected provenance.
- World Warrior Pavilion Ace Makoto — 16/16 distinct named-elite archetype with a unique 60-frame atlas, exact Crescent Heel timing, unique arcade-profile identity, and dual-resolution Stage 2 evidence. Three failed walk revisions remain rejected provenance; accepted v4 passes the six-distinct-pose gate.
- World Warrior Grand Grappler Tetsu — 16/16 distinct named-elite archetype with a unique 60-frame atlas, exact Iron Gate Clinch timing, unique arcade-profile identity, and dual-resolution Stage 3 evidence. Rejected pilot, two-pose walk, clipped startup, and seven-figure misc revisions remain provenance; accepted walk v2 passes eight distinct poses.
- World Warrior training dummy — 16/16 nonhumanoid breakable practice post with unique real-alpha art, calibrated 2.8-unit runtime scale, 90 HP, guaranteed Vitality Gourd health drop, and clean intact/gourd-drop evidence at both target resolutions.
- World Warrior Vitality Gourd — 16/16 independent health pickup with unique real-alpha calabash art, calibrated 0.74-unit runtime scale, guaranteed training-dummy drop, exact 25-percent healing, and clean drop/collection evidence at both target resolutions.
- World Warrior Focus Drum — 16/16 independent meter pickup with unique real-alpha hourglass-drum art, calibrated 0.74-unit runtime scale, guaranteed Pavilion training-dummy drop, capped 200-meter grant, and clean drop/collection evidence at both target resolutions.
- World Warrior Judge's Laurel Fan — 16/16 independent score pickup with unique real-alpha ceremonial-fan art, calibrated 0.74-unit runtime scale, guaranteed Grand Tournament training-dummy drop, exact 1000-point grant, and clean drop/collection evidence at both target resolutions.
- World Warrior Sparring Supply Crate — 15/16 independent breakable crate with unique real-alpha timber/lacquer art, calibrated 1.30-unit runtime scale (measured 31.7% of the canonical mannequin's true rendered height), 70 HP, guaranteed Focus Drum drop in the Dojo Approach second encounter, and clean intact/break-drop/collection evidence at both target resolutions. **Flagged for a proportion-sizing audit**: it was calibrated with the old flat-proxy method rather than a measured true-height ratio; needs re-review against the `Proportion and scale calibration` gate before its next revision.
- World Warrior Pavilion Rack Chest — 15/16 independent breakable crate with unique real-alpha lacquered/tiled-roof art, calibrated 2.60-unit runtime scale (measured 63.4% of the canonical mannequin's true rendered height, corrected from an initial too-small 1.85-unit/44.4% pilot calibration), 85 HP, guaranteed score drop in the Pavilion Circuit second encounter, and clean intact/break-drop/collection evidence at both target resolutions.
- World Warrior Grand Tournament Champion's Trophy Podium — 15/16 independent breakable crate with unique real-alpha stepped-stone/trophy-cup art, calibrated 2.30-unit runtime scale (measured 56.0% of the canonical mannequin's true rendered height, chosen correctly from the start via the measured Proportion and scale calibration gate, needing no post-hoc correction), 95 HP, guaranteed health drop in the Grand Tournament Floor second encounter, and clean intact/break-drop/collection evidence at both target resolutions. Completes the three-stage cyclic pickup-type pattern (Dojo: health + meter, Pavilion: meter + score, Grand Tournament: score + health).
- World Warrior Champion's Lantern Urn — 15/16 independent breakable crate with unique real-alpha round ceremonial-urn/lantern-dome art, calibrated 2.45-unit runtime scale (measured 59.7% of the canonical mannequin's true rendered height, chosen correctly from the start, needing no post-hoc correction), 105 HP, guaranteed meter drop authored directly onto the Champion's Courtyard final boss encounter (the first prop ever placed on a boss encounter, since that stage has no first/second horde pair), and clean intact/break-drop/collection evidence at both target resolutions with zero errors/warnings. This is the fourth and final World Warrior training-crate variant.
- World Warrior Dojo Rolling Log — 15/16 World Warrior's first HAZARD zone (not a breakable prop): a rope-bound rolling log, LinearSweep behavior reusing the Archive Intake Tram's timing envelope, calibrated 1.00-unit diameter (24.4% of true mannequin height, reasoned against authored stage geometry rather than the character-height bands), wired into Dojo Approach's first encounter alongside the training dummy. A real runtime capture confirms its authored warning text renders correctly in-engine.
- World Warrior Dojo Falling Training Weight — 15/16 World Warrior's second hazard zone: a rope-harnessed granite training weight, FallingStrike behavior matching the Archive Repository Falling Shelf's family, calibrated 1.20-unit scale (29.2%), wired into Dojo Approach's second encounter alongside the supply crate. Both hazards are verified content-distinct from each other and from the existing dummy/crate in a four-way silhouette comparison, and both are fully covered by deterministic StageHazardRuntime phase-transition assertions.
- Atlas contract — 10×9 at 256-pixel cells; rows 0–5 map locomotion, signature attack, and defensive/defeat states; rows 6–8 remain transparent.

### Completed shared UI production

- Living Archive HUD kit — 16/16 runtime approved with active-form portrait resolution, responsive safe-area composition, and no legacy glossy-frame selection.
- Archive Map — 15/16 runtime approved with an illustrated chamber, unframed structural columns, native realm/route controls, responsive exact captures, and safe checkpoint replacement focus.
- Results — 16/16 runtime approved with one reusable real-alpha plate, asymmetric rank/tally hierarchy, mode-specific Stage/World/KO treatment, safe confirmation focus, and clean procedural-audio teardown.
- Pause — 16/16 runtime approved with one reusable real-alpha record plate, seven-slot navigation, active-form record, four nested document views, and verified Escape/resume behavior.
- Form-select — 16/16 runtime approved with one six-vault inheritance selector, active-form/current-cursor separation, cancel/reopen/confirm behavior, and deterministic explicit-target swap.

## Production order

### Archive quality bar

**Proportion-sizing debt (2026-07-27):** every item below was approved before
the `Proportion sizing` gate (item 11 above) existed. Measured audits already
confirm the Archive Health/Meter/Data Cache and the Archive Intake Tram render
noticeably too small next to the player character (~40-44% of true mannequin
height instead of a believable ~55%+ for standing objects). Do not re-open
this list item by item — it is in scope for the single scheduled
`Docs/PROJECT_HANDOFF.md > Full Proportion Sizing Sweep` and should be
corrected there in one batch pass, not piecemeal.

1. **DONE:** Produce and approve the cross-category style calibration pilots beside mannequin/Ryu/Goku: Raider, health cache, health pickup, Index Vaults lateral set, HUD kit, and strike VFX.
2. **DONE:** Complete the first runtime calibration set with the strike VFX and all seven Archive caches, pickups, and canister assets at both target resolutions.
3. **DONE:** Expand the approved health-family grammar to meter/data and revise the volatile canister until its explosive identity is unambiguous.
4. **DONE:** Replace all three base Archive enemies and produce unique animation-ready sheets for Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt.
5. **DONE:** Runtime-approve Intake Tram, Index Vaults scans, and Corruption Repository falling shelf/data debris, security sweep, explosion decal, and impact fragments.
6. **DONE:** Intake Boulevard, Index Vaults, Corruption Repository, and Knight's Reliquary production layers are runtime approved with ordered parallax and dual-resolution evidence.
7. **DONE:** Replace the legacy Archive Knight intro atlas/cards with the approved combat identity and explicit illustrated portraits.
8. **DONE:** Add localized Reliquary destruction variants and boss-phase spectacle.
9. **DONE:** Replace the lifebar frame and reskin the gameplay HUD around the approved illustrated fighting-game Archive UI kit.
10. **DONE:** Reskin the Archive Map around an illustrated Living Archive navigation chamber while preserving checkpoint and focus safety.
11. **DONE:** Reskin Stage Clear, World Complete, Game Over, and replay/restart confirmations around one approved responsive results plate.
12. **DONE:** Reskin pause root, Move List, Move Cards, Form Loadout, and Artifacts around one approved responsive record plate.
13. **DONE:** Complete form-select as the final Archive shared-UI gate.
14. **DONE:** Run technical and visual-style gates and promote each Archive slice independently.
15. **DONE:** Replace the rejected Dojo Approach reuse with an independent layered dusk-dojo environment and exact dual-resolution runtime evidence.
16. **DONE:** Explicitly review the World Warrior Rookie/Striker/Grappler runtime atlases and replace the rejected Ryu-derived Rookie with an original 16/16 style-v2 atlas.
17. **DONE:** Replace the generic tracksuit Striker with an original 16/16 kick-focused style-v2 atlas and a real horizontal Turning Kick.
18. **DONE:** Replace Grappler with an original non-military tournament-wrestling identity and readable Shoulder Drive.
19. **DONE:** Replace Pavilion Circuit reuse with independent layered pavilion/deck production art and dual-resolution runtime evidence.

### World Warrior

**Blocked until the lighting key is locked:** the Pavilion and Grand Tournament
backdrop repaints with a painted ground line wait on the
`Stage Rendering And Depth Separation Pass`. Repainting them against the current
flat, unlit composite would have to be redone once the key direction and depth
ramp exist, which is the same rework the camera-contract lock was created to
prevent. Everything else in this queue is unblocked.

1. **DONE:** Dojo Approach, Pavilion Circuit, Grand Tournament Floor, and Champion's Courtyard have four unique runtime-approved environment/floor identities.
2. **DONE:** Dojo Rookie, Pavilion Striker, Tournament Grappler, Dojo Prodigy Kenzo, Pavilion Ace Makoto, and Grand Grappler Tetsu are 16/16 runtime approved with unique animation-ready atlases.
3. **DONE:** The training dummy, all four World Warrior breakable crates (Sparring Supply Crate, Pavilion Rack Chest, Grand Tournament Champion's Trophy Podium, Champion's Lantern Urn), the Vitality Gourd, Focus Drum, and Judge's Laurel Fan pickups, and World Warrior's first two hazard zones (Dojo Rolling Log, Dojo Falling Training Weight) are all runtime approved. The Rack Chest's initial calibration read far too small next to the player character (measured 44% of true mannequin height) and was corrected to 63%; the Trophy Podium (56%), Lantern Urn (60%), Rolling Log (24%), and Falling Weight (29%) all applied the measured **Proportion and scale calibration** gate from the start and needed no correction. The Sparring Supply Crate (measured 32%) still needs a proportion re-review, scheduled as part of the `Full Proportion Sizing Sweep` in `PROJECT_HANDOFF.md`. Spectator/crowd layers and arena destruction remain pending.
4. Preserve the same contour/value grammar as the accepted characters while using World Warrior-specific warm tournament motifs.
5. Runtime capture and style audit before any slice is marked complete.

### Astral

**Blocked until the lighting key is locked.** The four Astral stages currently
run on interim pre-restyle plates, so the restyle is the largest remaining art
job in the project. It is also the one most exposed to the lighting contract:
authoring a full world of stage art against a flat, unlit composite and then
introducing shadows and a depth ramp would invalidate the whole pass. This queue
resumes after the `Stage Rendering And Depth Separation Pass`.

1. Dedicated stage/floor composition for each ladder stage; route paintings may inform style but cannot be silently repeated as complete stage identities.
2. Unique sheets for Saibaman Alpha, Vanguard Commander Lyra, and Ki Captain Prime.
3. Capsule caches, astral pickups, ki-strafe emitter/effects, meteor/debris, energy current, pulse-floor machinery, and tournament destruction states.
4. Preserve the same contour/value grammar as the accepted characters while using Astral-specific saturated anime energy motifs.
5. Runtime capture and style audit before any slice is marked complete.

## Automated enforcement

- `Scripts/Tools/run_stage_junction_audit.ps1` gates the backdrop/floor junction by rendering each stage twice with different clear colours and diffing, so only real holes register and painted dark lines do not. All 7 layered stages currently report 0 gap pixels: the background never shows through geometry. What reads in play as a bad junction is the missing plinth/base at the wall foot, not a hole.
- `Scripts/Tools/run_stage_layer_visibility_audit.ps1` measures how much of each parallax layer a stage actually draws, by capturing it again with each layer hidden. It caught Grand Tournament Floor rendering only 0.47% of its midground, hiding a gong, a judges' pavilion, and the championship trophy entirely. Fails below 1% midground.
- `StageMissionValidator` rejects missing stage, floor, background-panel, and prop paths.
- World-run tests require four distinct existing Archive backdrop paths and four distinct existing floor paths.
- World-run tests require all four authored Archive prop sprites and three independent pickup sprites.
- World-run tests lock all seven approved style-v2 object paths and measured runtime metrics.
- World-run tests lock the Raider atlas path, imported dimensions, 10×9 layout, calibrated metrics, no-tint contract, frames 40–49, and exact startup/active/recovery animation-duration sums.
- World-run tests require the authored strike VFX resource to exist.
- Stage-hazard tests lock the real all-team canister radius/mask/knockback behavior; Stage 3 smoke additionally requires both `PropExploded` and real explosion-damage evidence.
- Add hash-based duplicate checks for all uniqueness-required stage, elite, prop, and pickup groups.
- Add sprite-sheet frame/metric validation when named-elite sheets are integrated.
- Keep visual approval manual and explicit: hashes, paths, dimensions, and imports cannot prove style.
- Store the model, prompt, canonical references, rubric score, reviewer, and runtime capture for every approved generated asset.

## Definition of done

A content slice may be marked complete only after:

- deterministic tests pass;
- all release-facing paths resolve;
- uniqueness-required hashes differ;
- no procedural character fallback appears;
- no tint-only gameplay identity remains;
- every generated asset used canonical style references;
- every asset passes the visual style rubric and has explicit approval metadata;
- a representative encounter, recovery pocket, set piece, elite/boss, and results transition are captured;
- the capture is cohesive beside mannequin/Ryu/Goku and visually approved at target resolutions.
