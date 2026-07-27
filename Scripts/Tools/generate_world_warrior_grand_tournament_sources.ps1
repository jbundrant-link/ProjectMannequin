param(
    [switch]$SkipExisting,
    [switch]$GenerateFamily
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$dojo = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$pavilion = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$legacyTournament = 'Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the approved Grand Tournament pilot, World Warrior roster anchors, mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, simplified stylized materials, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Characters define rendering grammar and world palette only; do not include characters. High fidelity comes from composition, lighting, and finish, not PBR texture noise. No photorealism, PBR, gritty military sci-fi, dense kit-bash detail, microtexture noise, painterly blur, flat vector art, pixel art, people, fighters, text, letters, numbers, symbols, calligraphy, signs, logos, or UI.
'@

$prompt = @'
Produce ONE seamless continuous 21:9 side-on belt-scroll GRAND TOURNAMENT FLOOR BACKDROP PILOT for Project Mannequin. This is World Warrior Stage 3: a monumental open-air championship arena for the final qualifier and heavyweight title-holder. It must read instantly as a wide public tournament stadium, never another dojo compound or lantern pavilion.

Build one uninterrupted flat lateral elevation with three asymmetric but connected championship subzones at one horizon and scale. LEFT: a broad low contestants' gate cut into warm-ivory tournament stone, with one heavy vermilion canopy and dark-indigo curtain opening. CENTER-LEFT: tiered EMPTY spectator terraces with large simplified stone seating bands, restrained saffron torch bowls, and blank vermilion/indigo honor cloth; leave the seats empty because spectators will be a separate runtime reaction layer. RIGHT: a low championship judges' dais and trophy-plinth silhouette beneath a shallow curved roof, followed by a second open tier and champion entry arch at the far edge. Connect all zones through low stone retaining walls, dark timber rails, torch rhythm, and distant mountain silhouettes under a deep indigo-to-plum evening sky.

COMPOSITION FOR THE FIXED GAME CAMERA: keep all roofs, terraces, gates, and landmark masses within the middle 50 percent of image height. Reserve the upper 28 percent for open sky and distant silhouettes. Reserve the lower 24 percent as a simple dark low-contrast arena retaining wall behind fighters. Do not let a single building fill the full image height. Use broad lateral shapes and one consistent horizon; no frontal corridor or central staircase. The arena must feel wider and more ceremonial than Pavilion Circuit without becoming a giant close-up.

The first image is the approved Dojo Approach backdrop: match only its seamless side-on production format, contour control, broad cel-painted value planes, and low-detail gameplay band. Do not copy its enclosed compound, cool timber arrangement, or practice-hall identity. The second image is approved Pavilion Circuit: match only its World Warrior color/material grammar; do not copy its lantern arcade, open pavilion arrangement, circular wall motifs, or close architectural scale. The third image is the rejected Tournament District composite and is STRUCTURE AND CHAMPIONSHIP-PALETTE REFERENCE ONLY; do not copy its people, signs, storefront, floor, rendering defects, or composition. The next three images are the approved original World Warrior Rookie, Striker, and Grappler family anchors; use only their vermilion/indigo/saffron/ivory hierarchy. The final three images are the canonical mannequin, Ryu, and Goku rendering-finish anchors.

PROJECT MANNEQUIN STYLE LOCK. Match the approved references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-three-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. High fidelity comes from composition, lighting, and finish, not PBR texture noise.

No floor, road, fighting lane, foreground rail, people, spectators, fighters, crowd silhouettes, training dummies, gameplay props, hazards, pickups, text, letters, numbers, calligraphy, signs, logos, UI, divider bars, panel seams, triptych framing, centered gate, centered staircase, mirror symmetry, corridor, tunnel, strong perspective convergence, photorealism, PBR, gritty military sci-fi, dense roof/stone microdetail, painterly blur, flat vector art, or baked character shadows. Full-bleed production backdrop with no border.
'@

$output = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_backdrop_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_grand_tournament_backdrop_pilot_v1_job.json'

function Invoke-NanoImage {
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

    foreach ($required in $References) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
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
        '--resolution', '2k',
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

function Invoke-BackgroundRemoval {
    param(
        [string]$Name,
        [string]$SourcePath,
        [string]$Output,
        [string]$Metadata
    )

    if ($SkipExisting -and (Test-Path $Output) -and (Test-Path $Metadata)) {
        Write-Host "SKIP $Name"
        return
    }

    Write-Host "CUTOUT $Name"
    $raw = & $cli generate create image_background_remover `
        "--image-references=$SourcePath" `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw "Higgsfield background removal failed for $Name."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $Metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no cutout URL for $Name."
    }

    Invoke-WebRequest -Uri $url -OutFile $Output
    Write-Host "SAVED $Output"
}

function Invoke-GrandTournamentFamily {
    $pilot = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_backdrop_style_pilot_v1.png'
    $floorOutput = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v1.png'
    $midgroundSource = 'Artifacts/StyleCalibration/world_warrior_grand_tournament_midground_source_v1.png'
    $midgroundOutput = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_midground_style_v1.png'
    $foregroundSource = 'Artifacts/StyleCalibration/world_warrior_grand_tournament_foreground_source_v1.png'
    $foregroundCutout = 'Artifacts/StyleCalibration/world_warrior_grand_tournament_foreground_cutout_v1.png'

    $floorPrompt = $styleLock + @'

Create one strict TOP-DOWN ORTHOGRAPHIC square CHAMPIONSHIP STONE FLOOR texture for Grand Tournament Floor. Use exactly SEVEN broad horizontal courses of enormous warm-ivory and cool blue-gray tournament stone slabs spanning the canvas. Separate courses with thin charcoal-indigo mortar. Within each course place only two or three huge staggered slab joints, all straight and sparse; never form a small-tile grid. Alternate restrained warm-ivory, muted plum-gray, and cool blue-gray value planes. Add broad two-step cel-painted stone shading, minimal edge wear, and no realistic grit. Keep the central gameplay region calm and lower contrast. The Grand Tournament pilot supplies monumental ivory stone, indigo night, vermilion cloth, and saffron torch hierarchy. The Pavilion deck is material contrast only; do not copy timber boards.

No perspective convergence, horizon, wall, architecture, timber, dense paving network, small tiles, checkerboard, concentric geometry, circle, oval, arc, ring, spiral, radial sweep, centered emblem, letters, symbols, border, perimeter frame, vignette, object, prop, hazard, baked character shadow, rubble, cracks, realistic grain, or microdetail. Side edges must be mirror-friendly and free of clipped focal objects. Full-bleed 1:1 production floor.
'@
    Invoke-NanoImage `
        -Name 'grand_tournament_floor_v1' `
        -Prompt $floorPrompt `
        -AspectRatio '1:1' `
        -Output $floorOutput `
        -Metadata 'Artifacts/style_calibration_world_warrior_grand_tournament_floor_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v1.png', 'Assets/Stages/WorldWarrior/world_warrior_dojo_floor_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)

    $midgroundPrompt = $styleLock + @'

Create one WIDE MIDGROUND LANDMARK SOURCE for Grand Tournament Floor on a perfectly uniform neutral medium-gray background. Place exactly THREE separated, non-overlapping, fully visible side-on championship landmarks across the 21:9 canvas with generous empty gray gaps. LEFT: a low champion trophy plinth with one large blank cup silhouette, folded vermilion and indigo ribbons, and two compact saffron torch bowls. CENTER: a broad but low empty judges' table under a shallow dark-indigo canopy, with three empty high-backed seats and warm-ivory stone base; no people. RIGHT: a compact front-row spectator gallery fragment with two empty stepped seating bands, one tournament gong, and a short vermilion rail. All three share one scale, one lateral fighting-stage perspective, monumental ivory/charcoal stone, controlled vermilion cloth, and sparse saffron lights. Keep the entire lower 28 percent empty uniform gray so fighters, feet, pickups, and hazards remain unobstructed. Entire silhouettes visible with no cropping.

No connected wall, floor, full backdrop, people, spectators, crowd silhouettes, enemies, active hazards, loose weapons, crates, words, signs, glyphs, warning marks, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'grand_tournament_midground_source_v1' `
        -Prompt $midgroundPrompt `
        -AspectRatio '21:9' `
        -Output $midgroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_grand_tournament_midground_source_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_pavilion_midground_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'grand_tournament_midground_cutout_v1' `
        -SourcePath $midgroundSource `
        -Output $midgroundOutput `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_grand_tournament_midground_cutout_v1_job.json'

    $foregroundPrompt = $styleLock + @'

Create one WIDE FOREGROUND EDGE SOURCE for Grand Tournament Floor on a perfectly uniform neutral medium-gray background. Place exactly TWO disconnected shallow KNEE-HEIGHT arena-edge remnants. LEFT: a low broken warm-ivory stone crowd barrier entering only from the extreme lower-left edge, with one dark-indigo rail cap, one blank vermilion cloth fold, and a small saffron torch bowl no taller than a fighter knee. RIGHT: a low championship rope-and-plinth remnant entering only from the extreme lower-right edge, with a charcoal stone footing, indigo rope, folded vermilion ribbon, and one small trophy-finial silhouette. Keep the upper 60 percent perfectly empty uniform gray, the central 65 percent perfectly empty uniform gray, and both silhouettes entirely below the lower 40 percent. Both pieces must remain disconnected and asymmetric.

No tall torch, post, column, wall, bridge, center object, floor, connected rail, people, crowd, characters, props, hazards, text, symbols, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'grand_tournament_foreground_source_v1' `
        -Prompt $foregroundPrompt `
        -AspectRatio '21:9' `
        -Output $foregroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_grand_tournament_foreground_source_v1_job.json' `
        -References @($pilot, 'Artifacts/StyleCalibration/world_warrior_pavilion_foreground_cutout_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'grand_tournament_foreground_cutout_v1' `
        -SourcePath $foregroundSource `
        -Output $foregroundCutout `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_grand_tournament_foreground_cutout_v1_job.json'

    & '.\.venv\Scripts\python.exe' '.\Scripts\Tools\split_stage_edge_cutout.py' `
        $foregroundCutout `
        'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_foreground_left_style_v1.png' `
        'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_foreground_right_style_v1.png'
    if ($LASTEXITCODE -ne 0) {
        throw 'Grand Tournament foreground split failed.'
    }
}

Push-Location $projectRoot
try {
    if ($GenerateFamily) {
        Invoke-GrandTournamentFamily
        return
    }

    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP grand_tournament_backdrop_pilot_v1'
        return
    }

    foreach ($required in @($dojo, $pavilion, $legacyTournament, $rookie, $striker, $grappler, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE grand_tournament_backdrop_pilot_v1'
    $arguments = @(
        'generate', 'create', 'nano_banana_pro',
        '--prompt', $prompt
    )
    foreach ($reference in @($dojo, $pavilion, $legacyTournament, $rookie, $striker, $grappler, $mannequin, $ryu, $goku)) {
        $arguments += @('--image', $reference)
    }
    $arguments += @(
        '--aspect_ratio', '21:9',
        '--resolution', '2k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )
    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for grand_tournament_backdrop_pilot_v1.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for grand_tournament_backdrop_pilot_v1.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}