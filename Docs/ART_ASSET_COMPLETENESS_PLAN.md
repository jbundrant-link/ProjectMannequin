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

| State                                      | Meaning                                                                                                                                    |
| ------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Mechanics pending                          | Layout, combat, hazard, or progression behavior is incomplete.                                                                             |
| Mechanics complete / art pending           | Gameplay is deterministic and tested, but one or more art gates below fail.                                                                |
| Art technically integrated / style pending | Bespoke assets are imported and wired, but the style rubric has not passed. This remains the state of the latest Archive environment pass. |
| Style approved / capture pending           | Raw assets pass the visual bible, but runtime composition has not passed final QA.                                                         |
| Slice complete                             | Mechanics, technical art gates, style review, runtime capture, and readability checks all pass.                                            |

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
11. **Performance** — new art stays within texture-memory and draw-call budgets and does not introduce recurring frame hitches.

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
- World Warrior Sector currently derives four ladder stages from the same tournament art source.
- Astral Battlefront has four route paintings, but repeats route images and uses hero imagery as its floor fallback.
- Archive named elites previously reused base archetype sheets with a gold tint/stat boost; Veyra, Rhune, and Basalt now use distinct animation-ready atlases. Other worlds remain pending.
- The prior boulder, crate, and meter-pickup paths were missing.
- Health, meter, and score pickups previously referenced the same missing image and differed by tint.
- Hazard presentation currently relies mainly on procedural strips/discs.
- The first Archive environment and gameplay-object replacement pass solved missing/reused-path problems but drifted toward dense realistic hard-surface sci-fi. The seven gameplay objects have since been superseded at runtime by the approved style-v2 family; the Stage 2–4 environments still require replacement.
- The current HUD frame uses glossy realistic metal, while the Archive Map reads as a software dashboard rather than a Living Archive fighting-game interface.

## Current Archive status

| Slice                 | Mechanics          | Environment                                     | Props/pickups                                                   | Elite                                            | Hazard set-piece art                                              | Capture                                                                                               | Status                             |
| --------------------- | ------------------ | ----------------------------------------------- | --------------------------------------------------------------- | ------------------------------------------------ | ----------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- | ---------------------------------- |
| Intake Boulevard      | Complete           | Explicit style review pending                   | Health and data caches/pickups runtime approved                 | Scout and Index Warden Veyra runtime approved    | Tram art pending                                                  | Scout/Jab, Veyra/Decree, and real cache/drop flows pass at 720p/1080p; environment still pending      | Mechanics complete / style pending |
| Index Vaults          | Complete           | Style pilot approved; production layers pending | Health and meter caches/pickups runtime approved                | Raider and Cipher Captain Rhune runtime approved | Scan-emitter/beam art pending                                     | Raider/Cross, Rhune/Cipher Cross, and cache/drop flows pass at 720p/1080p; environment still fails    | Mechanics complete / style pending |
| Corruption Repository | Complete           | Technically wired; style replacement required   | Health/meter/data family and volatile canister runtime approved | Bruiser and Overseer Basalt runtime approved     | Falling shelf/debris, security emitter, and aftermath art pending | Bruiser/Hammer, Basalt/Faultline, and canister detonation pass at 720p/1080p; environment still fails | Mechanics complete / style pending |
| Knight's Reliquary    | Existing boss loop | Technically wired; style replacement required   | Not applicable yet                                              | Archive Knight review pending                    | Localized destruction/spectacle art pending                       | Pending                                                                                               | Mechanics complete / style pending |

## Technically integrated Archive assets requiring style review/replacement

### Environments

- `archive_index_vaults_backdrop_higgsfield_v1.png`
- `archive_index_vaults_floor_higgsfield_v1.png`
- `archive_corruption_repository_backdrop_higgsfield_v1.png`
- `archive_corruption_repository_floor_higgsfield_v1.png`
- `archive_knights_reliquary_backdrop_higgsfield_v1.png`
- `archive_knights_reliquary_floor_higgsfield_v1.png`

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

All seven style-v2 objects are independent real-alpha images with function-specific silhouettes and no tint-only identity. The legacy images remain in the repository for provenance but no longer seed or supply runtime presentation.

Final Archive enemy/object verification on 2026-07-16: clean build, 124/124 world-run assertions, 32/32 stage-hazard assertions, Stage 1/2/3 ladder summaries passing, and dual-resolution captures for all object functions, base enemies, and named elites.

### Completed animated enemy and elite replacement

- Archive Raider — 16/16 runtime approved. Six identity-locked final source sheets compose through authored connected-silhouette anchors into `archive_raider_style_v2.png`; rejected rusty sources and failed revisions remain provenance only.
- Archive Scout and Bruiser — 16/16 runtime approved at lightweight/heavyweight scales with phase-aligned Scout Jab and Bruiser Hammer rows.
- Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt — 16/16 unique named-elite atlases with distinct silhouettes, attacks, room lights, hashes, and dual-resolution runtime evidence; no tint-only identity remains in Archive Stages 1–3.
- Atlas contract — 10×9 at 256-pixel cells; rows 0–5 map locomotion, signature attack, and defensive/defeat states; rows 6–8 remain transparent.

### Approved calibration directions awaiting production

- Index Vaults lateral stage concept V2 — 15/16; separate backdrop/floor/midground/foreground production layers pending.
- Living Archive HUD kit concept — 15/16; modular component extraction and responsive runtime wiring pending.

## Production order

### Archive quality bar

1. **DONE:** Produce and approve the cross-category style calibration pilots beside mannequin/Ryu/Goku: Raider, health cache, health pickup, Index Vaults lateral set, HUD kit, and strike VFX.
2. **DONE:** Complete the first runtime calibration set with the strike VFX and all seven Archive caches, pickups, and canister assets at both target resolutions.
3. **DONE:** Expand the approved health-family grammar to meter/data and revise the volatile canister until its explosive identity is unambiguous.
4. **DONE:** Replace all three base Archive enemies and produce unique animation-ready sheets for Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt.
5. **IN PROGRESS:** Produce Intake Tram, scan emitter/beam, falling shelf/data debris, security sweep emitter, explosion decal, and impact fragments in graphic anime/cel-shaded treatment.
6. Add foreground/subzone variants and landmarks where a single backdrop still reads as repetition during traversal.
7. Replace the lifebar frame and reskin HUD, Archive Map, results, pause, and form-select around the approved illustrated fighting-game Archive UI kit.
8. Add localized Reliquary destruction variants and boss-phase spectacle.
9. Run technical and visual-style gates and promote each Archive slice independently.

### World Warrior

1. Four unique environment/floor identities: Dojo Approach, Pavilion Circuit, Grand Tournament Floor, Champion's Courtyard.
2. Unique sheets for Dojo Prodigy Kenzo, Pavilion Ace Makoto, and Grand Grappler Tetsu.
3. Training crates/dummies, health/meter/score pickups, rolling log, falling practice prop, spectator/crowd layers, and arena destruction.
4. Preserve the same contour/value grammar as the accepted characters while using World Warrior-specific warm tournament motifs.
5. Runtime capture and style audit before any slice is marked complete.

### Astral

1. Dedicated stage/floor composition for each ladder stage; route paintings may inform style but cannot be silently repeated as complete stage identities.
2. Unique sheets for Saibaman Alpha, Vanguard Commander Lyra, and Ki Captain Prime.
3. Capsule caches, astral pickups, ki-strafe emitter/effects, meteor/debris, energy current, pulse-floor machinery, and tournament destruction states.
4. Preserve the same contour/value grammar as the accepted characters while using Astral-specific saturated anime energy motifs.
5. Runtime capture and style audit before any slice is marked complete.

## Automated enforcement

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
