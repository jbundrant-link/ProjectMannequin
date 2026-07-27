param(
    [switch]$GenerateFloor,
    [switch]$GenerateRoutePack,
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
if (-not (Test-Path $cli)) {
    $cli = 'higgsfield'
}

$compositionMaster = 'Assets/Stages/AstralBattlefront/astral_route_03_energy_rail_higgsfield_v2.png'
$approachMaster = 'Assets/Stages/AstralBattlefront/astral_route_02_capsule_causeway_higgsfield_v2.png'
$summitMaster = 'Assets/Stages/AstralBattlefront/astral_route_04_tournament_summit_higgsfield_v2.png'
$archiveEnvironment = 'Assets/Stages/ArchiveDistrict/archive_district_stage_higgsfield_v1.png'
$worldWarriorEnvironment = 'Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$rejectedBackdrop = 'Assets/Stages/AstralBattlefront/astral_energy_rail_backdrop_overscan_pilot_v3.png'
$backdropOutput = 'Assets/Stages/AstralBattlefront/astral_energy_rail_backdrop_overscan_pilot_v4.png'
$backdropMetadata = 'Artifacts/style_calibration_astral_energy_rail_backdrop_overscan_pilot_v4_job.json'
$rejectedApproach = 'Assets/Stages/AstralBattlefront/astral_energy_rail_approach_backdrop_pilot_v1.png'
$approachOutput = 'Assets/Stages/AstralBattlefront/astral_energy_rail_approach_backdrop_pilot_v2.png'
$approachMetadata = 'Artifacts/style_calibration_astral_energy_rail_approach_backdrop_pilot_v2_job.json'
$rejectedSummit = 'Assets/Stages/AstralBattlefront/astral_energy_rail_summit_backdrop_pilot_v2.png'
$summitOutput = 'Assets/Stages/AstralBattlefront/astral_energy_rail_summit_backdrop_pilot_v3.png'
$summitMetadata = 'Artifacts/style_calibration_astral_energy_rail_summit_backdrop_pilot_v3_job.json'
$rejectedFloor = 'Assets/Stages/AstralBattlefront/astral_energy_rail_floor_pilot_v2.png'
$floorOutput = 'Assets/Stages/AstralBattlefront/astral_energy_rail_floor_pilot_v3.png'
$floorMetadata = 'Artifacts/style_calibration_astral_energy_rail_floor_pilot_v3_job.json'

$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the supplied mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Preserve Astral Battlefront's luminous cyan-white energy, porcelain-white ruins, saturated sky blue, restrained jade growth, and deep indigo storm contrast. High fidelity comes from composition, lighting, and finish, not PBR texture noise. Do not include characters. No photorealism, realistic PBR, gritty military sci-fi, dense kit-bash detail, microtexture noise, painterly blur, flat vector art, pixel art, generated text, symbols, logos, or UI.
'@

$backdropPrompt = $styleLock + @'

Create one production FLOORLESS ENERGY RAIL CONVERGENCE BACKDROP for Project Mannequin's side-scrolling Astral Battlefront Stage 3. Preserve the established stage identity and broad composition language: a storm-charged cyan vortex in the upper sky, broken elevated luminous energy rails, monumental white archive-like arches and pylons, floating garden islands, distant waterfall plateaus, and a clear left-to-right ascent toward the summit. Rebuild it as a true side-on fighting-game backdrop, not a complete scene painting.

The first supplied image is the composition-approved V3 backdrop. EDIT IT IN PLACE. Preserve its exact vortex size and location, the exact three complete arcade groups, two small floating waterfall islands, thin horizontal cyan rails, distant waterfall mist, palette, camera distance, and empty lower atmosphere. Do not redesign, move, resize, add, or remove any landmark. V3 needs one cleanup only: remove the two faint full-height vertical translucent guide bands in the sky and architecture. Repaint those bands with perfectly continuous neighboring sky, mist, rails, and architecture so no vertical rectangle, stripe, glow column, tonal step, or editing boundary remains anywhere. The second image is the flattened Stage 3 composition master and is identity reference only; do not reproduce its floor or near ledges.

OUTPUT AND CAMERA CONTRACT: 4:3 canvas at 4K. The central horizontal 16:9 band, from 12.5 percent to 87.5 percent of canvas height, is the protected gameplay-safe frame. Put every critical landmark, complete arch silhouette, rail junction, vortex focal point, and intended visible architecture inside that protected band. The upper and lower 12.5 percent are deliberate overscan continuation for camera zoom, shake, and multiplayer framing. Continue sky naturally into upper overscan. Continue only simple low-contrast distant masonry and atmospheric haze into lower overscan. Use one stable lateral horizon and consistent architectural scale. Keep the left and right edges compositionally quiet so adjacent route chunks can overlap without cutting a focal landmark.

LATERAL STAGE CONSTRUCTION: retain V3's flat side-on elevation and exactly three asymmetric middle-distance landmark groups. Every group remains isolated by visible sky gaps. Every arch, pylon, island, and rail segment remains fully contained. Energy rails remain thin distant horizontal light bands between complete pylons, never broad walkable surfaces, bridges, ramps, diagonals, or paths aimed toward the center. Keep the far left and right edges as smooth open atmospheric continuation without drawing visible guide boundaries.

DEPTH AND READABILITY: distant cloud and tiny floating-island masses are broad and low contrast; middle-distance ruined towers and waterfall plateaus are readable but quieter than fighters; complete energy-rail arches provide the main Stage 3 landmark rhythm. Reserve the lower 35 percent of the protected 16:9 band as uninterrupted calm pale-blue waterfall mist and atmospheric depth behind fighter bodies. No cliff face, island underside, arch base, rail, dark vegetation, or high-contrast silhouette may enter that calm band. Keep all architecture and rail junctions above it. All architecture must remain middle-distance or farther away.

The third and fourth images are environment rendering references only. Match their controlled contours, broad painted planes, and arcade readability without copying their locations. The final three mannequin, Ryu, and Goku images are authoritative rendering-style anchors only.

NO floor, road, paving, fighting lane, platform surface, foreground ledge, near cliff face, undergrowth apron, close-up arch, edge-cropped structure, crossing rail, X shape, diagonal bridge, ramp, path toward camera, character, enemy, vehicle, prop, pickup, hazard, cast character shadow, text, letters, numbers, glyphs, logo, UI, border, divider, triptych, hard panel seam, centered corridor, strong one-point perspective, photorealism, or PBR. Full-bleed floorless production backdrop with no frame.
'@

$floorPrompt = $styleLock + @'

Create one production ENERGY RAIL GAMEPLAY FLOOR texture for Project Mannequin's Astral Battlefront Stage 3. Use the flattened Stage 3 composition master only for material and palette. Produce a strict top-down orthographic 1:1 texture at 4K: enormous irregular interlocking warm porcelain-white causeway slabs with sparse deep-indigo joints, rare cyan energy fissures, restrained jade growth in isolated interior cracks, and broad cel-painted cool shadow planes. Keep contrast low so fighters, feet, pickups, and telegraphs dominate.

The first supplied image is the REJECTED V2 floor. Preserve only its ivory/cyan/indigo/jade palette, clean contour language, and broad low-detail material treatment. V2 fails because its regular horizontal brick courses read like a vertical wall under the orthographic stage camera, while its blank outer frame repeats as broad ivory bands. REMOVE the brick grid, rows, columns, and blank perimeter.

GEOMETRY CONTRACT: use 10 to 14 huge asymmetric polygonal slabs with staggered, nonparallel joints. No joint may remain straight for more than 15 percent of canvas width or height. No continuous horizontal line, vertical line, row, column, masonry course, or grid may cross the canvas. Vary slab dimensions and offsets so the material reads as a top-down arena surface from every rotation, never as a wall.

SEAM CONTRACT: carry quiet generic ivory stone naturally through all four edges with soft low-frequency tonal variation. Do not create a blank frame, perimeter strip, curb, outline, or sharp value transition near an edge. Keep every cyan fissure, dark joint intersection, plant, and focal crack away from the outermost 8 percent. Opposite edges must remain similar in average color and brightness for mirrored repetition.

No perspective convergence, horizon, sky, wall, architecture, platform edge, cliff face, undergrowth apron, stairs, circle, ring, emblem, radial pattern, checkerboard, text, symbol, prop, hazard, character, cast shadow, realistic grit, microdetail, or photographic material. Full-bleed top-down production floor.
'@

$approachPrompt = $styleLock + @'

Create the production FLOORLESS LEFT APPROACH BACKDROP companion for Project Mannequin's side-scrolling Astral Battlefront Stage 3. EDIT the first supplied rejected V1 approach in place. Preserve its exact blue-sky composition, curved relay arch, twin-pylon rail gate, small garden observatory, contour language, and architectural scale. Make only the cleanup changes below.

V1 CLEANUP: completely remove the full-height waterfall/glow wall touching the right edge. Repaint the rightmost 14 percent as uninterrupted soft blue sky and pale waterfall mist matching the approved V4 center backdrop's left edge. No vertical light column, waterfall, tonal stripe, cloud wall, rail endpoint, or architecture may touch that edge. Raise or dissolve every lower arch base, pylon base, broken slab, and vegetation silhouette into mist so no architecture or dark contour crosses below the fighter-band line at 61.25 percent of total canvas height. The entire region below that line must be calm pale-blue mist with only broad low-contrast cloud variation. Preserve the landmarks above that line.

CONTINUITY CONTRACT: use the approved V4 center backdrop as the authoritative camera distance, architectural scale, contour weight, sky blue, cyan rail thickness, white-porcelain material, mist level, and protected-frame geometry. Do not add, remove, duplicate, or redesign V1's landmark groups.

OUTPUT AND CAMERA CONTRACT: 4:3 canvas at 4K. The central horizontal 16:9 band from 12.5 percent to 87.5 percent of canvas height is the protected gameplay-safe frame. Keep every critical landmark and complete silhouette inside it. Upper and lower 12.5 percent are natural overscan. Reserve the lower 35 percent of the protected frame as uninterrupted calm pale-blue waterfall mist and low-contrast atmospheric depth. No architecture, island underside, rail, cliff, vegetation mass, or dark silhouette may enter that calm fighter band.

LATERAL FLOW: communicate left-to-right ascent only through increasing landmark refinement and thin horizontal rails. Use one flat side-on elevation, one stable horizon, and no perspective convergence. Keep both outer edges quiet. All arches and pylons must be fully contained with generous sky around them. Rails are thin distant light bands, never walkable platforms.

The second supplied image is the old Capsule Causeway composition master and is identity reference only. Do not reproduce its floor, road, near ledge, ramps, or complete-scene perspective. Environment and character references define rendering grammar only.

NO floor, road, paving, fighting lane, platform surface, foreground ledge, near cliff, undergrowth apron, cropped structure, repeated V4 landmark, giant vortex, crossing rail, X shape, diagonal bridge, ramp, path toward camera, character, enemy, vehicle, prop, pickup, hazard, cast shadow, text, symbol, logo, UI, border, panel seam, centered corridor, strong one-point perspective, photorealism, or PBR. Full-bleed floorless production backdrop.
'@

$summitPrompt = $styleLock + @'

Create the production FLOORLESS RIGHT SUMMIT-APPROACH BACKDROP companion for Project Mannequin's side-scrolling Astral Battlefront Stage 3. EDIT the first supplied rejected V2 summit approach in place. Preserve its continuous blue sky, seamless tonal field, left receiver arch, circular summit relay identity, slim crystal pylons, small floating islands, upper storm-cloud identity, contour language, and distant architectural scale. Make only the geometry cleanup below.

V2 GEOMETRY CLEANUP: the center circular summit relay is too large and too low, reading as a second gameplay platform. Uniformly reduce the complete relay and all three crystal pylons to 62 percent of their current size, preserving their exact proportions and identity. Move the reduced relay upward until its lowest visible porcelain or cyan contour ends at 57 percent of total canvas height, safely above the fighter-band line at 61.25 percent. Continue only pale mist beneath it. Do not leave a shadow, support, reflection, underside, vegetation, cyan glow, or dark contour below that limit. Keep the left receiver arch and right observatory unchanged above the fighter band. Keep all storm mass above 57 percent and repaint any remaining lower-right darkness as pale blue mist. Preserve V2's now-clean continuous sky; do not reintroduce rectangles, vertical bands, or horizontal value steps.

CONTINUITY CONTRACT: use the approved V4 center backdrop as the authoritative camera distance, architectural scale, contour weight, sky blue, cyan rail thickness, white-porcelain material, mist level, and protected-frame geometry. Do not add, remove, duplicate, or redesign V1's landmark groups.

OUTPUT AND CAMERA CONTRACT: 4:3 canvas at 4K. The central horizontal 16:9 band from 12.5 percent to 87.5 percent of canvas height is the protected gameplay-safe frame. Keep every critical landmark and complete silhouette inside it. Upper and lower 12.5 percent are natural overscan. Reserve the lower 35 percent of the protected frame as uninterrupted calm pale-blue waterfall mist and low-contrast atmospheric depth. No architecture, island underside, rail, cliff, vegetation mass, or dark silhouette may enter that calm fighter band.

LATERAL FLOW: imply approach to the summit through a restrained increase in distant cloud energy and architectural refinement, not larger foreground objects. Use one flat side-on elevation, one stable horizon, and no perspective convergence. Keep both outer edges quiet. Every arch, pylon, island, and rail junction must be fully contained with generous sky gaps. Rails remain thin distant horizontal light bands, never walkable platforms.

The second supplied image is the old Tournament Summit composition master and is identity reference only. Do not reproduce its floor, road, near ledge, ramps, or complete-scene perspective. Environment and character references define rendering grammar only.

NO floor, road, paving, fighting lane, platform surface, foreground ledge, near cliff, undergrowth apron, cropped structure, repeated V4 landmark, second giant vortex, crossing rail, X shape, diagonal bridge, ramp, path toward camera, character, enemy, vehicle, prop, pickup, hazard, cast shadow, text, symbol, logo, UI, border, panel seam, centered corridor, strong one-point perspective, photorealism, or PBR. Full-bleed floorless production backdrop.
'@

function Invoke-ImageGeneration {
    param(
        [string]$Name,
        [string]$Prompt,
        [string]$AspectRatio,
        [string]$Output,
        [string]$Metadata,
        [string[]]$References
    )

    if ($SkipExisting -and (Test-Path $Output) -and (Test-Path $Metadata)) {
        Write-Host "SKIP $Name"
        return
    }

    foreach ($reference in $References) {
        if (-not (Test-Path $reference)) {
            throw "Missing generation reference: $reference"
        }
    }

    Write-Host "GENERATE $Name"
    $arguments = @(
        'generate', 'create', 'nano_banana_pro',
        '--prompt', $Prompt
    )
    foreach ($reference in $References) {
        $arguments += @('--image', $reference)
    }
    $arguments += @(
        '--aspect_ratio', $AspectRatio,
        '--resolution', '4k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )

    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Higgsfield failed for $Name."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $Metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $Name."
    }

    Invoke-WebRequest -Uri $url -OutFile $Output
    Write-Host "SAVED $Output"
}

$backdropReferences = @(
    $rejectedBackdrop,
    $compositionMaster,
    $archiveEnvironment,
    $worldWarriorEnvironment,
    $mannequin,
    $ryu,
    $goku
)
Invoke-ImageGeneration `
    -Name 'astral_energy_rail_backdrop_overscan_pilot_v4' `
    -Prompt $backdropPrompt `
    -AspectRatio '4:3' `
    -Output $backdropOutput `
    -Metadata $backdropMetadata `
    -References $backdropReferences

if ($GenerateRoutePack) {
    $approachReferences = @(
        $rejectedApproach,
        $backdropOutput,
        $approachMaster,
        $archiveEnvironment,
        $worldWarriorEnvironment,
        $mannequin,
        $ryu,
        $goku
    )
    Invoke-ImageGeneration `
        -Name 'astral_energy_rail_approach_backdrop_pilot_v2' `
        -Prompt $approachPrompt `
        -AspectRatio '4:3' `
        -Output $approachOutput `
        -Metadata $approachMetadata `
        -References $approachReferences

    $summitReferences = @(
        $rejectedSummit,
        $backdropOutput,
        $summitMaster,
        $archiveEnvironment,
        $worldWarriorEnvironment,
        $mannequin,
        $ryu,
        $goku
    )
    Invoke-ImageGeneration `
        -Name 'astral_energy_rail_summit_backdrop_pilot_v3' `
        -Prompt $summitPrompt `
        -AspectRatio '4:3' `
        -Output $summitOutput `
        -Metadata $summitMetadata `
        -References $summitReferences
}

if ($GenerateFloor) {
    $floorReferences = @(
        $rejectedFloor,
        $compositionMaster,
        'Assets/Stages/ArchiveDistrict/archive_index_vaults_floor_style_v2.png',
        'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v1.png',
        $mannequin,
        $ryu,
        $goku
    )
    Invoke-ImageGeneration `
        -Name 'astral_energy_rail_floor_pilot_v3' `
        -Prompt $floorPrompt `
        -AspectRatio '1:1' `
        -Output $floorOutput `
        -Metadata $floorMetadata `
        -References $floorReferences
}