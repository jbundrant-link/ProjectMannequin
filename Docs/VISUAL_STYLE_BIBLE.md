# Project Mannequin Visual Style Bible

## Authority

This document operationalizes the visual direction in [MasterPrompt.md](MasterPrompt.md), especially Section 4. It is mandatory for every generated or procedural character, enemy, stage, prop, pickup, hazard, VFX, HUD, menu, map, portrait, icon, and cinematic asset.

Within the roadmap hierarchy, [MASTER_IMPLEMENTATION_PLAN.md](MASTER_IMPLEMENTATION_PLAN.md)
owns product scope and sequencing while this document has final authority over
visual language and style approval. A tactical plan, backlog, audit, successful
import, or passing mechanics test cannot waive this rubric.

The accepted runtime look of the mannequin, Ryu, and Goku is the source of truth. New assets must look as though they belong in the same game when placed beside those characters. An asset can be unique, technically correct, and attractive while still failing the Project Mannequin style.

## One-sentence target

**A modern 2.5D fighting-game illustration rendered with bold anime/cel-shaded forms, high-impact cinematic lighting, and arcade-readable silhouettes—polished and dimensional, but never photorealistic, gritty-PBR, or software-dashboard flat.**

The intended blend is:

- modern fighting-game polish, weight, staging, and cinematic impact;
- expressive anime cel shading, speed, energy, and VFX;
- immediate classic beat-’em-up gameplay readability;
- dramatic camera composition without compromising the combat plane;
- varied crossover worlds unified by one rendering grammar.

High fidelity means strong anatomy, animation, composition, lighting, effects, and finish. It does **not** mean photographic materials, realistic industrial concept art, or dense surface noise.

## Canonical approved anchors

Use all applicable anchors when generating or reviewing art.

### Mannequin: Project identity and material language

- `Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png`
- `Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png`
- `Assets/Sprites/Mannequin/mannequin_sheet_higgsfield_v1.png`
- `Assets/Sprites/Mannequin/Diagnostics/mannequin_scale_comparison.png`

These establish warm porcelain colors, clean dark contour lines, simplified segmented materials, broad highlight shapes, and readable proportions.

### Ryu: grounded fighting-game rendering

- `Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png`
- `Assets/Sprites/Ryu/ryu_higgsfield_v4_sheet.png`
- `Assets/Sprites/Ryu/Diagnostics/ryu_move_scale_comparison_v3.png`

These establish muscular fighting-game anatomy, crisp silhouettes, cloth rendered in broad cel-shaded planes, controlled texture, and readable action poses.

### Goku: anime energy and color

- `Assets/Sprites/Goku/goku_intro_higgsfield_v1.png`
- `Assets/Sprites/Goku/goku_astral_higgsfield_v1_sheet.png`

These establish saturated color blocking, sharp silhouettes, graphic anime energy, and clean effect integration.

### Supporting environment anchors

- `Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png`
- `Assets/Stages/ArchiveDistrict/archive_district_stage_higgsfield_v1.png`

These are closer environment references because they use visible linework, simplified painted surfaces, broad readable architecture, and controlled cinematic lighting. They are rendering references—not permission to reuse their locations or composition.

### Assets that are not style anchors

Do not use these current assets as the sole style reference for new work:

- the dense-metal Archive Stage 2–4 environment pass;
- the legacy `*_higgsfield_v1` Archive cache, canister, and pickup pass;
- the glossy metallic HUD frame;
- the flat Archive Map dashboard skin;
- rusted/PBR enemy or prop art that conflicts with the accepted characters.

They may be supplied only as **layout, silhouette, function, or placement references**, with the prompt explicitly stating that their rendering style is not authoritative.

## Shared rendering grammar

Worlds may change palette, costume, architecture, weather, and cultural motifs. They must not change the underlying rendering grammar.

### Silhouette

- Readable at normal gameplay size before internal detail is considered.
- Strong outer contour and clear negative spaces.
- Exaggerate function: health, meter, score, explosive, elite, safe lane, and hazard emitter should be recognizable by shape.
- Avoid clusters of equally weighted small details.

### Linework

- Clean, deliberate dark contour lines around the primary silhouette.
- Selective interior lines describe planes and joints; they do not trace every scratch or panel.
- Line weight may vary for depth and emphasis.
- No thin CAD wireframe treatment, texture-generated edge noise, or sketchy painterly edges.

### Shading and highlights

- Two to four broad value bands per material.
- Clear lit plane, shadow plane, and restrained accent/rim light.
- Highlights are designed shapes, not noisy reflections.
- Limited soft blending may support form but cannot erase the cel-shaded structure.

### Materials

- Porcelain, cloth, skin, metal, glass, stone, and energy must read immediately through simplified treatment.
- Metal is cel-shaded metal—not photographic steel, brushed PBR, or rust-photo texture.
- Dirt, scratches, cracks, and wear are sparse accents, not full-surface noise.
- Energy uses graphic cores, shaped glows, and controlled bloom.

### Color

- Use large coherent color families and one or two high-energy accents.
- Preserve fighter separation from the stage.
- Archive Nexus: deep indigo/navy, warm porcelain, cyan, violet, and selective amber/magenta.
- World Warrior: warm sunset neutrals, red, white, black, lantern gold, and localized cool contrast.
- Astral: saturated orange/blue/purple, luminous cyan/white energy, and deep cosmic blues.
- Palette identity may vary; cel-shaded value structure does not.

### Detail hierarchy

1. Fighter and attack silhouette.
2. Enemy and hazard silhouette.
3. Interactable or collectible silhouette.
4. Arena navigation and floor tells.
5. Landmark shapes.
6. Secondary decoration.
7. Microdetail.

If microdetail competes with a fighter, telegraph, or pickup, remove it.

### Lighting and 2.5D depth

- Use a cinematic key light and readable rim light.
- Maintain value separation during effects and dark stage states.
- Art must feel dimensional even when delivered as sprites or planes.
- Use clear foreground, gameplay, midground, and background depth cues.
- Stage perspective must support the lane plane and camera.
- Background detail softens and simplifies with distance.

## Category standards

### Characters, enemies, bosses, and elites

- Match the mannequin/Ryu/Goku contour, value grouping, material simplification, and sprite scale.
- Preserve fighting-game anatomy and expressive poses.
- Named elites require distinct silhouettes, costume/armor language, head design, and accent motifs—not tint-only variants.
- Animation sheets must maintain identity, proportions, light direction, baseline, and scale across frames.
- Archive constructs should look like stylized animated combat designs, not realistic armored statues or photographed metal suits.

### Stages and floors

- Treat stages as cel-painted fighting-game sets, not standalone environment concept paintings.
- Use broad architectural masses, visible linework, selective texture, and graphic light shapes.
- Reserve the gameplay band for fighters, pickups, and telegraphs; lower contrast and detail directly behind combatants.
- Separate floor, gameplay/midground, backdrop, and optional foreground layers.
- Floors must be lower-frequency than backdrops.
- Do not bake enemies, pickups, gameplay props, warning decals, UI, or text into stage art.
- Avoid one-point concept corridors that contradict the side-on belt-scroll camera.
- Each stage needs at least three visually distinct subzones while keeping one stage palette.

### Props, caches, obstacles, and pickups

- Use a strong three-quarter gameplay view and one dominant silhouette.
- Apply the same contour weight and broad cel-shading as the characters.
- Use one dominant shape, one secondary shape, and one focal energy/icon cue.
- Pickups should be compact, bright, graphic fighting-game rewards—not miniature realistic products or generic mobile loot.
- Different functions require different silhouettes as well as different colors.
- Hazard props need readable inactive, charge, active, and aftermath states.

### Hazards and VFX

- Use tapered streaks, cel-shaded smoke, sharp impact stars/arcs, speed lines, shaped debris, and controlled bloom.
- Effects reinforce direction and hit strength; avoid amorphous photoreal fog.
- Procedural telegraphs may remain the deterministic accessibility layer, but themed illustrated emitters/effects must sit above them.
- Effects cannot obscure player feet, safe lanes, or attack silhouettes for extended periods.

### HUD, menus, map, and results

The interface is a fighting-game broadcast package fused with the Living Archive—not a desktop administration dashboard.

- Derive bold angular plates from the mannequin’s porcelain armor and dark joint seams.
- Pair Archive cyan with warm ivory/porcelain and dark plum/charcoal; realm colors are secondary accents.
- Use illustrated cel-shaded frame materials, not photoreal chrome, brushed metal, or glossy PBR glass.
- Use strong hierarchy: portraits/life first, boss identity second, meter/guard third, score/time/wave fourth, objective text last.
- Avoid long debug-like strips, tiny telemetry, equal-weight rectangles, excessive grid lines, and empty software panels.
- The Archive Map needs illustrated realm/stage art, route energy, champion silhouettes/portraits, and dimensional panels.
- Buttons and focus states use angular plates, impact wedges, archive traces, and clear gamepad focus—not generic rounded rectangles.
- Do not bake final text into generated UI art. Generate plates, masks, icons, separators, and backgrounds separately; render text in Godot.

## Explicit rejection list

Reject or regenerate art that reads as:

- photorealistic or semi-photorealistic industrial concept art;
- realistic PBR metal, rust, grime, brushed steel, or product visualization;
- dense kit-bashed hard-surface noise;
- painterly blur without controlled contours;
- enterprise dashboard, developer tool, or web-admin UI;
- generic mobile-game loot chest;
- flat vector, pixel art, low-poly, clay render, watercolor, or oil painting;
- inconsistent lighting or rendering inside a sprite sheet;
- background detail with equal contrast to fighters;
- generated text, gibberish signs, borders, or UI baked into gameplay art.

## Generation protocol

### Model routing

- **Characters, enemies, elites, props, pickups, portraits, and style-locked restyles:** Nano Banana Pro by default, using multiple canonical anchors.
- **Stage ideation:** an environment model may establish composition, but the result is not approved until generated/restyled with canonical style references.
- **HUD/UI concepts:** GPT Image 2 or Nano Banana Pro with canonical character anchors and a current screen capture.
- Model choice never overrides visual acceptance.

### Mandatory reference stack

Every generated-art job must include:

1. at least one approved mannequin/Ryu/Goku anchor;
2. a second approved character anchor from another world;
3. an approved category/environment anchor when available;
4. an old off-style asset only when needed for structure, explicitly labeled **structure only—do not copy its rendering style**.

Use all three primary character anchors for difficult cross-category work.

### Required prompt lock

Every prompt must include this intent, adapted only for asset type:

> PROJECT MANNEQUIN STYLE LOCK. Match the supplied mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-three-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Preserve the requested world and gameplay identity while matching this rendering grammar. High fidelity comes from anatomy, composition, lighting, and finish—not PBR texture noise. Do not produce photorealism, realistic industrial concept rendering, gritty military sci-fi, dense kit-bash greebles, microtexture noise, product visualization, generic mobile loot art, painterly blur, flat software-dashboard UI, flat vector art, pixel art, baked text, logos, or gibberish.

Then add the category-specific requirements from this bible.

### Pilot-first rule

Before generating a family or batch:

1. Generate one representative pilot.
2. Place it beside an approved character in a scale mockup or runtime capture.
3. Score it with the style rubric.
4. Revise until it passes.
5. Only then generate the remaining family.

A batch derived from an unapproved pilot is invalid even if every file imports successfully.

### Review sequence

1. Review raw output beside approved anchors before background removal, slicing, or wiring.
2. Reject immediately if it reads as a different game.
3. Validate silhouette at gameplay size and grayscale hierarchy.
4. Validate alpha, import, frame metrics, scale, and grounding.
5. Render in-game at 1280×720 and 1920×1080.
6. Capture calm composition and representative maximum combat density.
7. Record model, prompt, references, score, reviewer, and approval in [VISUAL_STYLE_REVIEW_MANIFEST.json](VISUAL_STYLE_REVIEW_MANIFEST.json).

## Style acceptance rubric

Score each criterion from 0–2:

| Criterion                        | 0                     | 1                   | 2                              |
| -------------------------------- | --------------------- | ------------------- | ------------------------------ |
| Cohesion with mannequin/Ryu/Goku | different game        | partial resemblance | immediately cohesive           |
| Contour language                 | absent/noisy/wrong    | inconsistent        | clean and matched              |
| Cel/value structure              | photoreal/flat/muddy  | mixed               | broad deliberate planes        |
| Silhouette/readability           | unclear               | adequate            | strong at gameplay size        |
| Detail density                   | micro-noise dominates | some clutter        | large forms dominate           |
| Material/lighting                | PBR/photo drift       | mixed               | stylized fighting-game volume  |
| Color/focal hierarchy            | uncontrolled/flat     | usable              | world-specific and intentional |
| Runtime/category fitness         | unusable/concept-only | revision needed     | production-ready               |

Passing requirements:

- total at least **14/16**;
- no criterion may score 0;
- cohesion, gameplay readability, and runtime/category fitness must each score 2;
- final approval requires an actual runtime capture.

Automatic failures include photoreal/PBR drift, no canonical reference used, unreadability at target scale, contaminated alpha, dashboard-like UI, stage detail that compromises combat, or missing manual approval.

## Current audit disposition — 2026-07-17

### Approved

- Mannequin runtime character art.
- Ryu runtime character art.
- Goku runtime character art and transformations.
- Archive health cache V2 runtime art.
- Archive health pickup V2 runtime art.
- Archive meter/data cache V2 runtime art.
- Archive meter/data pickup V2 runtime art.
- Archive volatile canister V2 runtime art and graphic cel detonation.
- Archive Scout V2 animated runtime atlas and Scout Jab sequence.
- Archive Raider V2 animated runtime atlas and Raider Cross sequence.
- Archive Bruiser V2 animated runtime atlas and Bruiser Hammer sequence.
- Index Warden Veyra, Cipher Captain Rhune, and Overseer Basalt unique animated elite atlases and signature attacks.
- Archive Intake Tram authored hazard sprite, warning/active treatment, and deterministic accessibility strip.
- Archive Index Vaults mirrored scan emitter/field family with warning/active treatment and deterministic accessibility strips.
- Archive Corruption Repository falling shelf/data-debris family with descending warning treatment and deterministic impact discs.
- Archive Corruption Repository security-sweep emitter with authored warning/active treatment over its deterministic moving strip.
- Archive Corruption Repository persistent explosion/falling-impact decal and fragment aftermath family.
- Archive Intake Boulevard layered environment: asymmetric far backdrop, low-frequency floor, real-alpha midground, and split edge foreground.
- Archive Index Vaults layered environment: far backdrop, low-frequency floor, three-landmark midground, and split edge foreground.
- Archive Corruption Repository layered environment: damaged-vault far backdrop, low-frequency fractured floor, three-landmark midground, and split containment-remnant foreground.
- Archive Knight's Reliquary layered environment: asymmetric ceremonial far backdrop, low-frequency broken-seal floor, low three-landmark midground, and split oath-rail foreground.
- Archive Knight unique animated combat atlas and inherited form: faceless crested helm, broken-arch oath pauldron, sternum memory core, and integrated forearm blade.
- Archive Knight identity-matched eight-frame intro atlas and illustrated mannequin/Knight matchup portraits.
- World Warrior Pavilion Ace Makoto unique animated named-elite atlas and Crescent Heel sequence.
- Archive Knight's Reliquary progressive phase destruction: left oath-tablet rupture, center-plinth collapse, right crystal-seal overload, and distinct localized bursts.
- Living Archive gameplay HUD: real-alpha porcelain/plum broadcast frame, cyan/coral side hierarchy, active-form bust portraits, contained native bars, concise telemetry, and responsive centered safe-area composition.
- Living Archive Archive Map: illustrated navigation chamber, distinct Archive/tournament/astral portal motifs, unframed structural columns, native realm controls, and a cyan four-node route spine.
- Living Archive results: asymmetric porcelain/plum record plate with a left rank medallion, right tally chamber, title rail, archive ribbon, and command ledge shared by Stage Clear, World Complete, and Game Over.
- Living Archive pause: asymmetric seven-slot navigation record plus a large right document chamber shared by active-form summary, Move List, Move Cards, Form Loadout, and Artifacts.
- Living Archive form-select: full-body champion aperture, six-window inheritance vault, centered current-form frame, movable native cursor, and loadout ribbon.
- Authored Project Mannequin strike-impact runtime VFX.

### Archive shared UI

- HUD, Archive Map, results, pause, and form-select are runtime approved. New screens extend their material grammar without copying an approved composition verbatim.

### Functional but not style-approved

The following are unique, imported, and mechanically wired, but drift toward photorealistic/dense hard-surface sci-fi or incompatible UI treatment. They remain temporary integration assets:

- Legacy Archive V1 cache, canister, and pickup family (`*_higgsfield_v1`); these are retained for provenance only and are no longer runtime-selected.

The rejected legacy backdrop/floor pairs for all four Archive stages, glossy metallic lifebar frame, and flat dashboard Archive Map skin remain only as provenance and are no longer runtime-selected.

The World Warrior tournament-district composite remains a supporting palette/architecture reference, but its runtime reuse across all four stages and as the floor fallback is rejected. Dojo Approach uses an approved layered dusk-dojo environment, Pavilion Circuit uses an approved independent lantern-pavilion/deck family, Grand Tournament Floor uses an approved empty championship-arena/giant-slab family, and Champion's Courtyard uses an approved storm-dark final-duel courtyard family. Dojo Prodigy Kenzo, Pavilion Ace Makoto, and Grand Grappler Tetsu use approved distinct 16/16 named-elite atlases. The breakable training dummy establishes the approved prop grammar, while the Vitality Gourd, Focus Drum, and Judge's Laurel Fan establish health, meter, and score pickup grammar; the 15/16 Sparring Supply Crate establishes the breakable-crate grammar, and the remaining training-crate variants are the next World Warrior art slice.

The World Warrior Dojo Rookie, Pavilion Striker, and Tournament Grappler are approved roster anchors. Rookie uses deep indigo cloth, saffron hierarchy, restrained vermilion accents, and a compact open-palm rushdown silhouette. Striker inverts that hierarchy through a vermilion pavilion jacket, indigo collar/calf wraps, and a tall horizontal-kick silhouette. Grappler adds a broad plum/indigo wrestling silhouette and a readable low Shoulder Drive without military kit. All three use simple repeatable cloth geometry without copying Ryu.

### Pending explicit review

- Other non-anchor bosses.
- World Warrior named elites and remaining stages.
- Astral enemies and stages.
- Remaining combat/hazard VFX, reward icons, portraits, and non-Archive menus.

Pending means unapproved—not implicitly accepted.

## Replacement order

1. **DONE:** Produce one cross-category calibration set: Archive enemy, cache, pickup, stage panel/floor pair, HUD plate, and VFX burst.
2. **DONE:** Approve the pilots beside mannequin/Ryu/Goku.
3. **DONE:** Restyle and dual-resolution validate all Archive caches, the volatile canister, pickups, and the canister detonation.
4. **DONE:** Replace Archive Scout, Raider, and Bruiser and create unique animation-ready Veyra, Rhune, and Basalt elites.
5. **DONE:** Restyle Archive Stages 1–4 with layered cel-painted subzones and lower surface noise; all four environments are runtime approved.
6. **DONE:** The lifebar/HUD, Archive Map, results, pause, and form-select families are runtime approved.
7. **DONE for Archive:** Complete the remaining themed Archive hazard art and VFX, including Reliquary phase spectacle.
8. **IN PROGRESS:** Apply the approved grammar—without copying Archive motifs—to World Warrior, then Astral.

## Definition of visually complete

An asset or screen is visually complete only when:

- it passes technical completeness gates;
- it passes the style rubric;
- [VISUAL_STYLE_REVIEW_MANIFEST.json](VISUAL_STYLE_REVIEW_MANIFEST.json) lists its anchors and review score;
- its runtime capture looks cohesive beside accepted characters;
- readability holds during representative combat/menu use;
- no temporary off-style asset remains represented as final.
