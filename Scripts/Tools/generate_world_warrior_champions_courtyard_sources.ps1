param(
    [switch]$SkipExisting,
    [switch]$GenerateFamily
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$dojo = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$pavilion = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$grandTournament = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_backdrop_style_v1.png'
$legacyTournament = 'Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the approved Champion's Courtyard pilot, World Warrior roster anchors, mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, simplified stylized materials, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Characters define rendering grammar and world palette only; do not include characters. High fidelity comes from composition, lighting, and finish, not PBR texture noise. No photorealism, PBR, gritty military sci-fi, dense kit-bash detail, microtexture noise, painterly blur, flat vector art, pixel art, people, fighters, text, letters, numbers, symbols, calligraphy, signs, logos, or UI.
'@

$prompt = @'
Produce ONE seamless continuous 21:9 side-on belt-scroll CHAMPION'S COURTYARD BACKDROP PILOT for Project Mannequin. This is World Warrior Stage 4: the quiet final-duel courtyard where Ryu waits after the public tournament. It must feel intimate, austere, weather-changed, and fighter-focused, never another public stadium, lantern circuit, or dojo approach.

Build one uninterrupted flat lateral elevation with three asymmetric but connected final-duel subzones at one horizon and scale. LEFT: a low cracked warm-ivory courtyard wall opening onto dark cypress trees, with one extinguished stone brazier and folded indigo rain cloth. CENTER-LEFT: a modest roofless champion shrine wall with two broad blank vermilion cloth panels, a shallow weathered stone bench, and one restrained saffron torch; no altar icon or text. RIGHT: a broken open training arcade with charcoal timber posts, a second torch, rain-dark plaster, and a far-edge champion entry opening beneath a wind-bent canopy. Connect the spaces through low damaged stone walls, shallow puddle-dark masonry bases, sparse torch rhythm, and distant storm mountains under a deep indigo cloud break. Use cool storm-blue and charcoal as the dominant hierarchy, warm ivory for readable wall planes, restrained vermilion cloth, and sparse saffron fire.

COMPOSITION FOR THE FIXED GAME CAMERA: keep every wall, roof remnant, torch, and courtyard landmark within the middle 46 percent of image height. Reserve the upper 32 percent for storm sky, distant mountains, and wind-shaped cloud bands. Reserve the lower 24 percent as a simple dark low-contrast wall/plinth behind fighters. Keep structures lower and quieter than Grand Tournament Floor; no giant close-up. Use broad lateral shapes, one consistent horizon, and at least three open sky gaps so the duel feels exposed to weather.

The first three images are approved Dojo, Pavilion, and Grand Tournament production backdrops. Match only their seamless side-on production format, controlled contours, broad cel-painted planes, and World Warrior material grammar. Do not copy Dojo's compound, Pavilion's lantern arcade, or Grand Tournament's public terraces/trophy architecture. The fourth image is the rejected Tournament District composite and is STRUCTURE/PALETTE REFERENCE ONLY; do not copy its people, signs, storefronts, floor, or composition. The next three images are approved original World Warrior roster anchors. The final three images are canonical mannequin, Ryu, and Goku rendering anchors; Ryu defines only final-duel seriousness and rendering finish, not a character to include.

PROJECT MANNEQUIN STYLE LOCK. Match the approved references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-three-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. High fidelity comes from composition, lighting, and finish, not PBR texture noise.

No floor, road, fighting lane, foreground rail, people, spectators, fighters, Ryu, crowd silhouettes, training dummies, gameplay props, active hazards, lightning strikes, projectiles, pickups, text, letters, numbers, calligraphy, signs, logos, UI, divider bars, panel seams, triptych framing, centered gate, centered staircase, mirror symmetry, corridor, tunnel, strong perspective convergence, photorealism, PBR, gritty military sci-fi, dense stone/roof microdetail, painterly blur, flat vector art, or baked character shadows. Full-bleed production backdrop with no border.
'@

$output = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_backdrop_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_champions_courtyard_backdrop_pilot_v1_job.json'

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
    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $Prompt)
    foreach ($reference in $References) {
        $arguments += @('--image', $reference)
    }
    $arguments += @('--aspect_ratio', $AspectRatio, '--resolution', '2k', '--wait', '--wait-timeout', '20m', '--json')
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

function Invoke-ChampionsCourtyardFamily {
    $pilot = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_backdrop_style_pilot_v1.png'
    $floorOutput = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_floor_style_v1.png'
    $midgroundSource = 'Artifacts/StyleCalibration/world_warrior_champions_courtyard_midground_source_v1.png'
    $midgroundOutput = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_midground_style_v1.png'
    $foregroundSource = 'Artifacts/StyleCalibration/world_warrior_champions_courtyard_foreground_source_v1.png'
    $foregroundCutout = 'Artifacts/StyleCalibration/world_warrior_champions_courtyard_foreground_cutout_v1.png'

    $floorPrompt = $styleLock + @'

Create one strict TOP-DOWN ORTHOGRAPHIC square WEATHERED COURTYARD FLOOR texture for Champion's Courtyard. Use exactly SIX broad staggered courses of huge rain-dark warm-ivory and blue-charcoal flagstones. Each stone must be enormous, spanning at least one quarter of the canvas width; use sparse irregular joints, never small tiles. Separate stones with thin deep-indigo mortar. Use cool storm-blue shadow planes, muted warm-ivory upper planes, and restrained plum-gray wet-value patches. Add broad cel-painted damp highlights only along a few inset stone edges, never mirror reflections. Keep the center low contrast and uncluttered for the final duel. The Champion's Courtyard pilot supplies storm-dark ivory/indigo/charcoal hierarchy. Grand Tournament's giant slabs are scale reference only; make this older, cracked, quieter, and more intimate.

No perspective convergence, horizon, wall, architecture, timber, dense paving network, small tiles, checkerboard, concentric geometry, circle, oval, arc, ring, spiral, radial sweep, centered emblem, letters, symbols, border, perimeter frame, vignette, puddle reflection, object, prop, hazard, lightning, baked character shadow, rubble, dense cracks, realistic grit, moss carpet, or microdetail. Side edges must be mirror-friendly and free of clipped focal objects. Full-bleed 1:1 production floor.
'@
    Invoke-NanoImage `
        -Name 'champions_courtyard_floor_v1' `
        -Prompt $floorPrompt `
        -AspectRatio '1:1' `
        -Output $floorOutput `
        -Metadata 'Artifacts/style_calibration_world_warrior_champions_courtyard_floor_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v1.png', 'Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)

    $midgroundPrompt = $styleLock + @'

Create one WIDE MIDGROUND LANDMARK SOURCE for Champion's Courtyard on a perfectly uniform neutral medium-gray background. Place exactly THREE separated, non-overlapping, fully visible side-on final-duel landmarks across the 21:9 canvas with generous empty gray gaps. LEFT: a low broken warm-ivory wall remnant with one extinguished stone brazier, folded indigo rain cloth, and a small wind-bent cypress. CENTER: a modest cracked champion shrine wall with one shallow bench, two blank vermilion cloth panels, and one restrained saffron torch; no icon, altar text, or weapon. RIGHT: a broken charcoal-timber training arcade fragment with a torn indigo canopy, rain-dark plaster base, and one small torch. Keep all three low, austere, asymmetrical, and at one side-on scale. Keep the entire lower 30 percent empty uniform gray so fighters and feet remain unobstructed. Entire silhouettes visible with no cropping.

No connected wall, floor, full backdrop, people, spectators, Ryu, crowd silhouettes, enemies, active hazards, lightning, loose weapons, crates, words, signs, glyphs, warning marks, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'champions_courtyard_midground_source_v1' `
        -Prompt $midgroundPrompt `
        -AspectRatio '21:9' `
        -Output $midgroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_champions_courtyard_midground_source_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_midground_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'champions_courtyard_midground_cutout_v1' `
        -SourcePath $midgroundSource `
        -Output $midgroundOutput `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_champions_courtyard_midground_cutout_v1_job.json'

    $foregroundPrompt = $styleLock + @'

Create one WIDE FOREGROUND EDGE SOURCE for Champion's Courtyard on a perfectly uniform neutral medium-gray background. Place exactly TWO disconnected shallow KNEE-HEIGHT edge remnants. LEFT: a low cracked ivory wall cap entering only from the extreme lower-left edge, with a charcoal stone footing, folded indigo cloth, and sparse cypress leaves. RIGHT: a low broken dark-timber threshold entering only from the extreme lower-right edge, with one torn vermilion tie, rain-dark stone base, and a small extinguished brazier. Keep the upper 60 percent perfectly empty gray, the central 65 percent perfectly empty gray, and both silhouettes entirely below the lower 40 percent. Both pieces must remain disconnected, asymmetric, and non-hazardous.

No tall torch, post, column, full wall, bridge, center object, floor, connected rail, people, characters, Ryu, props, active hazards, fire attack, text, symbols, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'champions_courtyard_foreground_source_v1' `
        -Prompt $foregroundPrompt `
        -AspectRatio '21:9' `
        -Output $foregroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_champions_courtyard_foreground_source_v1_job.json' `
        -References @($pilot, 'Artifacts/StyleCalibration/world_warrior_grand_tournament_foreground_cutout_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'champions_courtyard_foreground_cutout_v1' `
        -SourcePath $foregroundSource `
        -Output $foregroundCutout `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_champions_courtyard_foreground_cutout_v1_job.json'

    & '.\.venv\Scripts\python.exe' '.\Scripts\Tools\split_stage_edge_cutout.py' `
        $foregroundCutout `
        'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_foreground_left_style_v1.png' `
        'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_foreground_right_style_v1.png'
    if ($LASTEXITCODE -ne 0) {
        throw "Champion's Courtyard foreground split failed."
    }
}

Push-Location $projectRoot
try {
    if ($GenerateFamily) {
        Invoke-ChampionsCourtyardFamily
        return
    }

    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP champions_courtyard_backdrop_pilot_v1'
        return
    }

    $references = @($dojo, $pavilion, $grandTournament, $legacyTournament, $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    foreach ($required in $references) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE champions_courtyard_backdrop_pilot_v1'
    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', $reference)
    }
    $arguments += @('--aspect_ratio', '21:9', '--resolution', '2k', '--wait', '--wait-timeout', '20m', '--json')
    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for champions_courtyard_backdrop_pilot_v1.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for champions_courtyard_backdrop_pilot_v1.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}