param(
    [switch]$SkipExisting,
    [switch]$GenerateFamily
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$dojo = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$legacyTournament = 'Assets/Stages/WorldWarrior/world_warrior_tournament_district_higgsfield_v2.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$grappler = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the supplied approved Pavilion pilot, World Warrior roster anchors, mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, simplified stylized materials, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. Characters define rendering grammar and world palette only; do not include characters. High fidelity comes from composition, lighting, and finish, not PBR texture noise. No photorealism, PBR, gritty military sci-fi, dense kit-bash detail, microtexture noise, painterly blur, flat vector art, pixel art, people, fighters, text, letters, numbers, symbols, calligraphy, signs, logos, or UI.
'@

$prompt = @'
Produce ONE seamless continuous 21:9 side-on belt-scroll PAVILION CIRCUIT BACKDROP PILOT for Project Mannequin. This is World Warrior Stage 2: an open lantern court where tournament challengers cycle through public pavilion matches. It must be unmistakably different from the enclosed cool-timber Dojo Approach.

Build one uninterrupted flat lateral elevation with three asymmetric but connected subzones at one horizon and scale: left, a low open-air training arcade with broad vermilion timber posts and indigo canopy cloth; center-left, an elevated open-sided tournament pavilion with a shallow judges' balcony, large circular ring motifs, and blank vertical honor banners; right, a receding but still side-on lantern colonnade and second smaller pavilion framed by dark pine silhouettes. Connect them through overlapping eaves, low walls, lantern chains, and distant violet mountain silhouettes. Use warm amber lantern light against muted plum twilight, vermilion structural accents, indigo fabric, warm ivory plaster, charcoal timber, restrained jade foliage, and saffron focal lights. Keep large architectural masses, controlled dark ink contours, broad two-to-four-step cel shading, simplified painted surfaces, clean graphic highlights, and clear atmospheric depth.

The first image is the approved Dojo Approach production backdrop: match only its seamless layered-stage format, contour control, broad cel-painted value planes, and low-detail gameplay band. Do not copy its building arrangement, cool palette, enclosed compound, or central practice hall. The second image is the rejected Tournament District composite and is STRUCTURE AND WORLD-PALETTE REFERENCE ONLY; do not copy its people, signs, floor, storefront arrangement, rendering defects, or composition. The next three images are the approved original World Warrior Rookie, Striker, and Grappler family anchors: use only their vermilion/indigo/saffron/ivory material hierarchy, not their bodies or costumes. The final three images are the canonical mannequin, Ryu, and Goku rendering-finish anchors.

PROJECT MANNEQUIN STYLE LOCK. Match all approved references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-three-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. High fidelity comes from composition, lighting, and finish, not PBR texture noise.

Reserve the lower 25 percent as a simple dark low-contrast architectural plinth behind fighters. No floor, road, fighting lane, foreground rail, people, spectators, fighters, silhouettes of people, training dummies, gameplay props, hazards, pickups, text, letters, numbers, symbols, calligraphy, signs, logos, UI, divider bars, panel seams, triptych framing, centered gate, centered staircase, mirror symmetry, corridor, tunnel, strong perspective convergence, baked shadows of characters, photorealism, PBR, gritty military sci-fi, dense roof-tile microdetail, painterly blur, or flat vector art. Full-bleed production backdrop with no border.
'@

$output = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_backdrop_pilot_v1_job.json'

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

function Invoke-PavilionFamily {
    $pilot = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_pilot_v1.png'
    $floorOutput = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v1.png'
    $midgroundSource = 'Artifacts/StyleCalibration/world_warrior_pavilion_midground_source_v1.png'
    $midgroundOutput = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_midground_style_v1.png'
    $foregroundSource = 'Artifacts/StyleCalibration/world_warrior_pavilion_foreground_source_v1.png'
    $foregroundCutout = 'Artifacts/StyleCalibration/world_warrior_pavilion_foreground_cutout_v1.png'

    $rejectedFloor = 'Artifacts/StyleCalibration/world_warrior_pavilion_floor_rejected_v4_runtime.png'
    $floorPrompt = $styleLock + @'

Replace the first supplied rejected smooth plum floor with one strict TOP-DOWN ORTHOGRAPHIC SQUARE PAVILION DECK texture designed to remain legible after perspective projection. Use exactly NINE very broad horizontal lacquered timber board bands spanning continuously from the left edge to the right edge. Alternate restrained deep-plum, muted vermilion-brown, and charcoal-plum value planes; separate adjacent bands with thin straight dark-indigo joints. Each board band is roughly one ninth of the image height. Add only broad two-step cel-painted wood value planes inside each band, with extremely subtle lateral color variation and no realistic grain. The board joints must run only left-to-right and remain evenly horizontal. Do not add vertical board-end seams, so horizontal texture repetition remains seamless. The Pavilion pilot supplies the warm vermilion/indigo/plum palette; the Dojo floor supplies only low detail and gameplay readability. Keep all contrast moderate so fighters, feet, pickups, and telegraphs dominate.

No perspective convergence, horizon, wall, architecture, floor border, perimeter frame, centered emblem, circle, oval, arc, ring, spiral, vortex, bullseye, radial sweep, grid, diagonal seam, vertical seam, object, stone, spark, leaf, cloud, smoke, stain silhouette, prop, hazard, baked character shadow, scratches, cracks, knots, realistic grain, nails, microdetail, or dense grit. Full-bleed 1:1 production floor; every horizontal board and joint reaches both side edges cleanly.
'@
    Invoke-NanoImage `
        -Name 'pavilion_floor_v1' `
        -Prompt $floorPrompt `
        -AspectRatio '1:1' `
        -Output $floorOutput `
        -Metadata 'Artifacts/style_calibration_world_warrior_pavilion_floor_v1_job.json' `
        -References @($rejectedFloor, $pilot, 'Assets/Stages/WorldWarrior/world_warrior_dojo_floor_style_v1.png', $rookie, $striker, $mannequin, $ryu, $goku)

    $midgroundPrompt = $styleLock + @'

Create one WIDE MIDGROUND LANDMARK SOURCE for Pavilion Circuit on a perfectly uniform neutral medium-gray background. Place exactly THREE separated, non-overlapping, fully visible side-on landmarks across the 21:9 canvas with generous empty gray gaps. LEFT: a low vermilion lantern arch with two blank indigo honor streamers and one compact ivory stone base. CENTER: a broad open judges' shelter with a shallow canopy, empty bench, two circular lacquer wall motifs, and restrained saffron lanterns; keep it low and lateral, never a tall building. RIGHT: paired padded practice posts beside a small warm lantern pedestal and a compact jade pine planter. All three share one scale, one side-on three-quarter fighting-stage perspective, the Pavilion pilot palette, broad cel-painted planes, and sparse detail. Keep the entire lower 28 percent empty uniform gray so fighters, feet, pickups, and hazards remain unobstructed. Entire silhouettes visible with no cropping.

No connected fence, backdrop wall, floor, platform extending between landmarks, characters, spectators, enemies, active hazards, loose weapons, crates, words, signs, glyphs, warning marks, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'pavilion_midground_source_v1' `
        -Prompt $midgroundPrompt `
        -AspectRatio '21:9' `
        -Output $midgroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_pavilion_midground_source_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_dojo_midground_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'pavilion_midground_cutout_v1' `
        -SourcePath $midgroundSource `
        -Output $midgroundOutput `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_pavilion_midground_cutout_v1_job.json'

    $foregroundPrompt = $styleLock + @'

Create one WIDE FOREGROUND EDGE SOURCE for Pavilion Circuit on a perfectly uniform neutral medium-gray background. Place exactly TWO disconnected shallow KNEE-HEIGHT edge remnants. LEFT: a low vermilion pavilion-rail footing entering only from the extreme lower-left edge, with one indigo cloth tie, a small ivory stone base, and restrained jade leaves. RIGHT: a low broken lantern-base plinth entering only from the extreme lower-right edge, with a dark timber cap, one small inset saffron light, a blank vermilion ribbon, and restrained leaves. Keep the upper 60 percent perfectly empty uniform gray, the central 65 percent perfectly empty uniform gray, and both silhouettes entirely below the lower 40 percent. Both pieces must be fully visible enough to cut out, must never connect, and must not mirror each other exactly.

No tall lantern, post, column, wall, bridge, center object, floor, connected rail, characters, props, hazards, text, symbols, checkerboard, transparency grid, gradient, or cast shadows on the gray background. The background must be one flat solid gray color for clean removal.
'@
    Invoke-NanoImage `
        -Name 'pavilion_foreground_source_v1' `
        -Prompt $foregroundPrompt `
        -AspectRatio '21:9' `
        -Output $foregroundSource `
        -Metadata 'Artifacts/style_calibration_world_warrior_pavilion_foreground_source_v1_job.json' `
        -References @($pilot, 'Assets/Stages/WorldWarrior/world_warrior_dojo_foreground_style_v1.png', $rookie, $striker, $grappler, $mannequin, $ryu, $goku)
    Invoke-BackgroundRemoval `
        -Name 'pavilion_foreground_cutout_v1' `
        -SourcePath $foregroundSource `
        -Output $foregroundCutout `
        -Metadata 'Artifacts/StyleCalibration/world_warrior_pavilion_foreground_cutout_v1_job.json'

    & '.\.venv\Scripts\python.exe' '.\Scripts\Tools\split_stage_edge_cutout.py' `
        $foregroundCutout `
        'Assets/Stages/WorldWarrior/world_warrior_pavilion_foreground_left_style_v1.png' `
        'Assets/Stages/WorldWarrior/world_warrior_pavilion_foreground_right_style_v1.png'
    if ($LASTEXITCODE -ne 0) {
        throw 'Pavilion foreground split failed.'
    }
}

Push-Location $projectRoot
try {
    if ($GenerateFamily) {
        Invoke-PavilionFamily
        return
    }

    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP pavilion_backdrop_pilot_v1'
        return
    }

    foreach ($required in @($dojo, $legacyTournament, $rookie, $striker, $grappler, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE pavilion_backdrop_pilot_v1'
    $raw = & $cli generate create nano_banana_pro `
        --prompt $prompt `
        --image $dojo `
        --image $legacyTournament `
        --image $rookie `
        --image $striker `
        --image $grappler `
        --image $mannequin `
        --image $ryu `
        --image $goku `
        --aspect_ratio '21:9' `
        --resolution '2k' `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for pavilion_backdrop_pilot_v1.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for pavilion_backdrop_pilot_v1.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}