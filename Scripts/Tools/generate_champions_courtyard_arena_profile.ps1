param(
    [switch]$GenerateFloor,
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
if (-not (Test-Path $cli)) {
    $cli = 'higgsfield'
}

$compositionMaster = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_backdrop_style_v1.png'
$floorReference = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_floor_style_v1.png'
$worldWarriorEnvironment = 'Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$rejectedBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_backdrop_pilot_v2.png'
$backdropOutput = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_backdrop_pilot_v3.png'
$backdropMetadata = 'Artifacts/style_calibration_world_warrior_champions_courtyard_arena_backdrop_pilot_v3_job.json'
$rejectedFloor = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_floor_pilot_v1.png'
$floorOutput = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_arena_floor_pilot_v2.png'
$floorMetadata = 'Artifacts/style_calibration_world_warrior_champions_courtyard_arena_floor_pilot_v2_job.json'

$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the supplied mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Preserve World Warrior's warm ivory masonry, charcoal timber, indigo storm shadows, restrained vermilion cloth, jade foliage, and sparse saffron light. High fidelity comes from composition, lighting, and finish, not PBR texture noise. Do not include characters. No photorealism, realistic PBR, gritty military sci-fi, dense kit-bash detail, microtexture noise, painterly blur, flat vector art, pixel art, generated text, symbols, logos, or UI.
'@

$backdropPrompt = $styleLock + @'

Create one production FLOORLESS CHAMPION'S COURTYARD BOSS ARENA BACKDROP for Project Mannequin's final World Warrior duel against Ryu.

The first supplied image is the REJECTED V2 arena backdrop. Preserve its storm-night palette, moonlit mountain depth, central broken shrine wall, two restrained vermilion cloths, charcoal timber, cypress silhouettes, clean contour language, sparse saffron torch light, and floorless blue-gray lower mist. V2 fails only because its left wall/cypress group and right training arcade still touch and are cut by the canvas edges.

V3 CONTAINMENT EDIT: uniformly reduce every middle-distance courtyard object in V2 to 78 percent of its current size as one coherent group, then center that complete group horizontally in the canvas without changing internal proportions. This includes all walls, cypress trees, timber posts, cloths, torches, bench, canopy, and doorway. Keep the existing sky, moon, clouds, and distant mountains full-bleed behind it. After the edit, no courtyard object may enter the outermost 8 percent on either side. Repaint both outer margins with uninterrupted storm sky, distant mountain haze, and soft cypress haze only. The right arcade must end with a complete visible final post and wall edge; remove the cut black doorway and any architecture continuing beyond the frame. The left wall and every cypress silhouette must have visible sky clearance from the left edge. Preserve the floorless lower mist exactly.

CAMERA CONTRACT: exact 16:9 canvas at 4K. This complete canvas is the normal-fight camera frame and must be fully visible without runtime crop or nonuniform scaling. Keep all critical architecture inside the central 88 percent of width and central 86 percent of height. Leave natural storm sky, mountain haze, and low-contrast wall continuation around every edge so short boss-intro zooms can crop safely. Do not draw guides, safe-frame lines, translucent rectangles, borders, or tonal bands.

COMPOSITION: one coherent side-on courtyard at one horizon and scale. LEFT: a complete low broken ivory wall and dark cypress opening positioned well inside the frame. CENTER: a complete modest roofless champion shrine wall with two blank restrained vermilion cloth panels and one small saffron torch. RIGHT: a complete compact broken charcoal-timber training arcade with a short wind-bent canopy positioned well inside the frame. Connect the groups through low distant wall remnants and storm atmosphere, but preserve visible sky gaps between them. Keep every roof, post, wall, torch, tree, and cloth silhouette fully contained with visible clearance around it; nothing may touch or be cut by an edge.

EDGE AND FLOOR CONTRACT: keep the outermost left and right margins as continuous storm sky, distant mountains, cypress haze, and low wall continuation only. No torch, post, canopy, wall corner, or landmark may enter from an edge. Reserve the lower 35 percent as a calm, low-contrast vertical distant plinth and blue-gray storm mist behind fighter bodies. This lower band must contain NO horizontal platform top, paving, floor surface, road, perspective plane, curb, ledge, reflection, or walkable surface. Keep high-contrast landmarks above that band. Use no close foreground object. Keep the central fight area visually quiet and symmetrical enough for two fighters while the overall scene remains asymmetric and natural.

The second supplied environment image is rendering grammar only. The final mannequin, Ryu, and Goku references are authoritative style anchors only.

NO floor, road, paving, gameplay lane, platform surface, foreground ledge, near wall, close rail, giant arch, edge-cropped structure, character, Ryu, enemy, spectator, crowd, prop, pickup, hazard, cast character shadow, text, letters, numbers, calligraphy, glyphs, logo, UI, border, divider, triptych, centered corridor, strong one-point perspective, photorealism, or PBR. Full-bleed floorless 16:9 production backdrop with no frame.
'@

$floorPrompt = $styleLock + @'

Create one production CHAMPION'S COURTYARD BOSS ARENA FLOOR for Project Mannequin. EDIT the first supplied rejected V1 floor in place. Preserve only its weathered ivory, charcoal-indigo, blue-gray, and rare jade palette plus its clean contour language. V1 fails because it is a regular running-bond brick wall with continuous horizontal rows, repeated rectangular blocks, and long straight mortar lines.

V2 GEOMETRY REBUILD: replace every existing brick and mortar line with exactly 12 enormous asymmetric interlocking flagstone polygons. Use varied five-to-eight-sided shapes with staggered nonparallel joints. No two stones may share the same dimensions. No joint may stay straight for more than 14 percent of canvas width or height. No horizontal or vertical line may cross more than one stone boundary. Rotate individual stone axes subtly so the texture reads as a strict top-down arena surface from every camera direction, never as masonry or a wall. Keep the largest quiet ivory stones in the center and move darker stones toward scattered non-symmetric positions.

The texture will map ONCE across a square bounded boss-arena mesh; it is not a scrolling road and does not need a decorative border. Carry generic stone naturally through every edge. No continuous row, column, grid, ring, radial pattern, centered emblem, perimeter curb, blank frame, clipped focal crack, or vegetation stripe. Keep all high-energy cracks and moss away from the outermost 8 percent.

No perspective convergence, horizon, sky, wall, architecture, timber, platform edge, stairs, circle, text, symbol, prop, hazard, character, cast shadow, realistic grit, microdetail, or photographic material. Full-bleed top-down production floor.
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

Invoke-ImageGeneration `
    -Name 'world_warrior_champions_courtyard_arena_backdrop_pilot_v3' `
    -Prompt $backdropPrompt `
    -AspectRatio '16:9' `
    -Output $backdropOutput `
    -Metadata $backdropMetadata `
    -References @(
        $rejectedBackdrop,
        $compositionMaster,
        $worldWarriorEnvironment,
        $mannequin,
        $ryu,
        $goku
    )

if ($GenerateFloor) {
    Invoke-ImageGeneration `
        -Name 'world_warrior_champions_courtyard_arena_floor_pilot_v2' `
        -Prompt $floorPrompt `
        -AspectRatio '1:1' `
        -Output $floorOutput `
        -Metadata $floorMetadata `
        -References @(
            $rejectedFloor,
            $floorReference,
            $compositionMaster,
            'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v1.png',
            $mannequin,
            $ryu,
            $goku
        )
}
