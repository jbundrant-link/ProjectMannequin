# Art Asset Completeness Plan

## Purpose

A mechanics-complete stage is not art-complete. A stage, enemy, prop, obstacle, pickup, or hazard cannot be called finished while it relies on a missing path, procedural fallback, unrelated reused sprite, or tint-only identity.

Technical completeness is also not visual-style approval. A unique, imported, correctly scaled asset still fails if it does not match the accepted mannequin/Ryu/Goku rendering language. [VISUAL_STYLE_BIBLE.md](VISUAL_STYLE_BIBLE.md) is authoritative for style references, prompt locks, category rules, rejection criteria, and review scoring.

This plan is a release gate for every remaining content phase. Art direction, pilot generation, style review, import, runtime wiring, automated audit, and rendered capture belong to the same implementation slice.

## Completion states

| State | Meaning |
|---|---|
| Mechanics pending | Layout, combat, hazard, or progression behavior is incomplete. |
| Mechanics complete / art pending | Gameplay is deterministic and tested, but one or more art gates below fail. |
| Art technically integrated / style pending | Bespoke assets are imported and wired, but the style rubric has not passed. This is the current state of the latest Archive environment/prop pass. |
| Style approved / capture pending | Raw assets pass the visual bible, but runtime composition has not passed final QA. |
| Slice complete | Mechanics, technical art gates, style review, runtime capture, and readability checks all pass. |

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
- Named elites currently reuse their base archetype sheets and receive a gold tint/stat boost.
- The prior boulder, crate, and meter-pickup paths were missing.
- Health, meter, and score pickups previously referenced the same missing image and differed by tint.
- Hazard presentation currently relies mainly on procedural strips/discs.
- The latest Archive environments and gameplay objects solved missing/reused-path problems but drifted toward dense realistic hard-surface sci-fi rather than the accepted anime/cel-shaded fighting-game style.
- The current HUD frame uses glossy realistic metal, while the Archive Map reads as a software dashboard rather than a Living Archive fighting-game interface.

## Current Archive status

| Slice | Mechanics | Environment | Props/pickups | Elite | Hazard set-piece art | Capture | Status |
|---|---|---|---|---|---|---|---|
| Intake Boulevard | Complete | Explicit style review pending | Health cache approved; data object family pending | Reused Scout sheet | Tram art pending | Object capture passes; stage re-capture pending | Mechanics complete / style pending |
| Index Vaults | Complete | Style pilot approved; production layers pending | Health cache approved; health pickup integrated; meter family pending | Cipher Captain sheet pending | Scan-emitter/beam art pending | Object flow passes at 720p/1080p; environment still fails | Mechanics complete / style pending |
| Corruption Repository | Complete | Technically wired; style replacement required | Technically wired; style replacement required | Overseer Basalt sheet pending | Falling shelf/debris and security-emitter art pending | Functional capture exists; style failed | Mechanics complete / style pending |
| Knight's Reliquary | Existing boss loop | Technically wired; style replacement required | Not applicable yet | Archive Knight review pending | Localized destruction/spectacle art pending | Pending | Mechanics complete / style pending |

## Technically integrated Archive assets requiring style review/replacement

### Environments

- `archive_index_vaults_backdrop_higgsfield_v1.png`
- `archive_index_vaults_floor_higgsfield_v1.png`
- `archive_corruption_repository_backdrop_higgsfield_v1.png`
- `archive_corruption_repository_floor_higgsfield_v1.png`
- `archive_knights_reliquary_backdrop_higgsfield_v1.png`
- `archive_knights_reliquary_floor_higgsfield_v1.png`

### Legacy props and pickups still requiring replacement

- `archive_meter_cache_higgsfield_v1.png`
- `archive_data_cache_higgsfield_v1.png`
- `archive_volatile_canister_higgsfield_v1.png`
- `archive_meter_pickup_higgsfield_v1.png`
- `archive_data_pickup_higgsfield_v1.png`

### First style-calibrated runtime replacements

- `archive_health_cache_style_v2.png` — 16/16, runtime approved at 1280×720 and 1920×1080.
- `archive_health_pickup_style_v2.png` — 16/16, runtime approved at 1280×720 and 1920×1080 with isolated real-drop captures.
- `project_mannequin_strike_burst_style_v1.png` — 16/16, pooled authored strike VFX with procedural fallback; runtime approved at both target resolutions.

The legacy gameplay sprites remain independent real-alpha images and no longer use tinting to distinguish gameplay identity. That technical improvement remains valid, but the remaining images are not final because their dense realistic hard-surface treatment fails the style gate.

### Approved calibration directions awaiting production

- Archive Raider concept — 15/16; six animation sub-sheets and composed 10×9 atlas pending.
- Index Vaults lateral stage concept V2 — 15/16; separate backdrop/floor/midground/foreground production layers pending.
- Living Archive HUD kit concept — 15/16; modular component extraction and responsive runtime wiring pending.

## Production order

### Archive quality bar

1. **DONE:** Produce and approve the cross-category style calibration pilots beside mannequin/Ryu/Goku: Raider, health cache, health pickup, Index Vaults lateral set, HUD kit, and strike VFX.
2. **IN PROGRESS:** Complete the runtime calibration set. Health cache and strike VFX pass; health pickup needs isolated capture; Raider sheet, stage layers, and HUD components remain.
3. Expand approved cache/pickup grammar to meter/data and the volatile canister; never use the realistic pass as a style source.
4. Build the approved Raider 10×9 sheet, then produce unique animation-ready sheets for Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt.
5. Produce Intake Tram, scan emitter/beam, falling shelf/data debris, security sweep emitter, explosion decal, and impact fragments in graphic anime/cel-shaded treatment.
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
- World-run tests lock the approved health cache/pickup paths and measured runtime metrics.
- World-run tests require the authored strike VFX resource to exist.
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
