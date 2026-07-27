param(
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
if (-not (Test-Path $cli -PathType Leaf)) {
    $cli = 'higgsfield'
}

$mannequin = 'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$rejected = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_full_frame_plate_style_v1.png'
$identity = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_backdrop_style_v1.png'

$output = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_full_frame_plate_style_v2.png'
$metadata = 'Artifacts/style_calibration_world_warrior_champions_courtyard_full_frame_plate_style_v2_job.json'

# The v1 plate was a backdrop image stacked on a floor image: it carries a flat
# blue-grey strip 4.07 percent of image height at rows 0.630 to 0.670, and its
# floor is a top-down tile blown up to a scale the architecture contradicts.
# Scripts/Tools/audit_stage_plate_sources.py fails it. The prompt below attacks
# both faults and pins the walkable band the camera needs.
$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the supplied mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game presentation. High fidelity comes from composition, lighting, and finish, not PBR texture noise. Do not include characters. No photorealism, realistic PBR, microtexture noise, painterly blur, flat vector art, pixel art, generated text, symbols, logos, or UI.
'@

$subject = @'

Create one complete CHAMPION'S COURTYARD BOSS ARENA for Project Mannequin's World Warrior tournament finale, painted as a single continuous scene at dusk. Keep the established identity from the supplied courtyard reference: a walled tournament courtyard of warm timber and pale plaster halls, deep indigo tiled roofs, hanging lanterns and braziers, vermilion banners, dark pines and distant storm-lit mountains beyond the walls.

ONE CONTINUOUS PAINTING. This is a single scene observed from one camera in one perspective, not a backdrop pasted above a floor. The paving, the courtyard walls, and the buildings must share one horizon, one vanishing behaviour, and one lighting direction, and the ground must run continuously back to the base of the architecture. The previous version failed because a backdrop image and a top-down floor tile were stacked, leaving a flat grey strip straight across the frame and a pavement whose slab size contradicted the buildings.

ABSOLUTELY NO HORIZONTAL BAND. No flat strip, no solid bar, no blank gutter, no mist band, no gradient ribbon, no seam, no divider, and no abrupt tonal step may cross the frame at any height. Every row of the image must contain painted scene content.

FIGHTING GROUND CONTRACT. Reserve the lower third of the frame for the flat walkable stone courtyard the fighters occupy: the paving must begin no higher than 66 percent of image height and continue unbroken to the bottom edge. Keep that whole band clear, level, and uncluttered, with no steps, kerbs, walls, pits, water, railings, furniture, or props inside it. Paving slabs must read at human scale against the courtyard walls, roughly one stride across, with irregular interlocking joints rather than a regular grid, and they must recede naturally with the same perspective as the architecture.

CAMERA AND FRAMING. Exact 16:9 landscape. Symmetrically composed but not mirrored: vary the left and right architecture. Place the horizon and the main hall in the upper two thirds. Keep the extreme left and right edges quiet so camera shake and zoom never reveal an unfinished corner.

DEPTH AND READABILITY. Distant mountains and sky are broad and low contrast; courtyard walls, roofs, and banners are readable but quieter than fighters; the paving is the quietest surface in the frame so fighters, telegraphs, and pickups dominate.

The first supplied image is the REJECTED v1 plate: use it only for palette and general layout memory, and do not reproduce its stacked construction, its grey strip, or its oversized pavement. The second image is the approved courtyard identity reference for architecture and mood. The mannequin, Ryu, and Goku images are authoritative rendering-style anchors only.

NO character, enemy, fighter, crowd figure, silhouette, prop, pickup, hazard, HUD, text, letters, numbers, glyph, logo, border, frame, vignette, letterbox bar, panel seam, triptych, or split screen. Full-bleed single-scene production plate.
'@

$prompt = $styleLock + $subject

Push-Location $projectRoot
try {
    $references = @($rejected, $identity, $mannequin, $ryu, $goku)
    foreach ($required in $references) {
        if (-not (Test-Path $required -PathType Leaf)) {
            throw "Missing generation reference: $required"
        }
    }

    $arguments = @('generate', 'create', 'nano_banana_pro', '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', $reference)
    }
    $arguments += @(
        '--aspect_ratio', '16:9',
        '--resolution', '4k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )

    Write-Host 'GENERATE world_warrior_champions_courtyard_full_frame_plate_style_v2'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}
