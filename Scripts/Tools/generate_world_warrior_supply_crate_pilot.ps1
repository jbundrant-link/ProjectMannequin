param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_supply_crate_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_supply_crate_pilot_v1_job.json'
$trainingDummy = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_training_dummy_style_pilot_v1.png'
$meterPickup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_meter_pickup_style_pilot_v1.png'
$dojoBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_dojo_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR SPARRING SUPPLY CRATE, an original breakable stage prop viewed in a clean right-facing three-quarter angle, centered and fully visible.

The prop must read instantly as a sturdy, breakable wooden tournament supply crate at gameplay size, clearly a low storage box for sparring equipment and NOT a padded training post, barrel, chest, treasure box, sci-fi container, or pickup item. Build one squat timber crate with this exact large-form hierarchy:
- one broad low rectangular crate body, clearly WIDER THAN TALL, with a heavy flat lid overhanging the front edge;
- three thick horizontal warm-timber plank slats across the visible front face, separated by deep carved shadow grooves, with simplified visible wood grain and no fine speckled detail;
- four chunky deep-indigo lacquered corner brackets capping the vertical edges, each with three large bronze rivet studs;
- one thick vermilion cloth strap running across the lid and down the front face, tied in one bold square knot with two short blunt tails;
- one broad saffron rope handle loop mounted on the right side panel;
- clear BREAK-READY structure: one thick diagonal split seam across the middle front slat, one chipped corner on the lower left, and two short pale splinter notches along the lid edge, all cut into the wood rather than floating;
- one rolled warm-ivory hand-wrap bundle and one folded deep-indigo practice mitt resting on top of the lid, tucked under the strap and attached to the crate.

Function must come from unmistakable breakable-supply-crate cues: heavy planks, banded corners, cargo strap, rope handle, and pre-scored break seams. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The wide plank body, indigo corner brackets, vermilion strap knot, saffron rope loop, and diagonal break seam must survive at that size. Keep the crate grounded, solid, and bottom-weighted, sitting flat as if resting on a dojo floor.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior training dummy. Use only its breakable-prop contour weight, large-form simplification, grounded stage-prop presentation, and warm-timber/ivory/vermilion/deep-indigo/saffron hierarchy. Do not copy its tall vertical padded post, segmented spine, round strike pads, cross-arm shape, base disc, or silhouette.
2. Reference 2 is the approved World Warrior Focus Drum. Use only its bold tournament-equipment finish, thick ivory contours, indigo lacquer, vermilion tension accents, saffron trim, and clean collectible material treatment. Do not copy its hourglass body, drumheads, resonance rings, cords, beads, tassel, pickup scale, or silhouette.
3. Reference 3 is the approved Dojo Approach environment. Use only its warm dusk dojo mood, training-hall material language, timber/paper/lacquer palette, and ordered value hierarchy. Do not reproduce architecture, walls, screens, floors, banners, lanterns, mountains, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the lid bundle and mitt, outside the rope handle and strap tails, and below the full crate base. No training dummy, padded post, barrel, treasure chest, gold, coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, lantern, potion, gourd, drum, fan, weapon, blade, letters, numbers, kanji, logo, label text, star icon, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, detached planks, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-prop pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_supply_crate_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $trainingDummy,
        $meterPickup,
        $dojoBackdrop,
        $mannequin,
        $ryu,
        $goku
    )
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
        '--aspect_ratio', '1:1',
        '--resolution', '2k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )

    Write-Host 'GENERATE world_warrior_supply_crate_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the supply crate pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the supply crate pilot.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}
