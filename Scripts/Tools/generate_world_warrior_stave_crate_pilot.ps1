param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_stave_crate_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_stave_crate_pilot_v1_job.json'
$supplyCrate = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_supply_crate_style_pilot_v1.png'
$trainingDummy = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_training_dummy_style_pilot_v1.png'
$pavilionBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR PRACTICE STAVE CRATE, an original breakable stage prop viewed in a clean right-facing three-quarter angle, centered and fully visible.

The prop must read instantly as an upright breakable storage crate for bamboo practice staves at gameplay size, and NOT a padded training post, a low lidded supply crate, a barrel, a treasure chest, a weapon rack of blades, or a pickup item. Build one tall narrow crate with this exact large-form hierarchy:
- one upright rectangular timber crate body, clearly TALLER THAN WIDE, roughly twice as tall as it is wide, with an OPEN TOP and no lid;
- four to five vertical warm-timber plank staves forming the visible front face, separated by deep carved shadow grooves, with simplified wood grain and no fine speckled detail;
- two thick warm-ivory lacquer binding bands wrapping the crate horizontally, one near the top rim and one near the middle, each with three large bronze rivet studs;
- one squat deep-indigo lacquered base plinth flaring slightly at the floor, giving the crate a heavy bottom-weighted stance;
- a tight upright bundle of five blunt bamboo practice staves rising out of the open top, with rounded blunt ends, slight fanning at the tips, and no blades, points, spearheads, or edges;
- one broad plum cloth wrap tied around the stave bundle just above the rim, with one bold knot and two short blunt tails;
- clear BREAK-READY structure: one thick vertical split seam running down the front-right plank, one chipped notch broken out of the top rim, and two short pale splinter marks along the middle binding band, all cut into the wood rather than floating;
- one broad saffron painted stripe running around the crate just above the base plinth.

Function must come from unmistakable breakable stave-storage cues: upright open-top construction, banded planks, protruding blunt staves, and pre-scored break seams. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall, where the tall narrow banded body, the fanned blunt stave bundle, the plum wrap knot, the indigo base plinth, and the vertical break seam must all survive.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Sparring Supply Crate. Use only its timber/lacquer material treatment, contour weight, bronze stud detailing, and warm-timber/ivory/vermilion/deep-indigo/saffron/plum hierarchy. DO NOT copy its low wide proportion, overhanging flat lid, three horizontal front slats, corner brackets, vermilion cargo strap knot, side rope handle loop, or lid bundle. This variant must be clearly a different crate.
2. Reference 2 is the approved World Warrior training dummy. Use only its breakable-prop grounding, large-form simplification, and stage-prop presentation. Do not copy its padded post, segmented spine, round strike pads, cross-arm shape, or base disc.
3. Reference 3 is the approved Pavilion Circuit environment. Use only its lantern-lit tournament pavilion mood, lacquered timber material language, and ordered value hierarchy. Do not reproduce architecture, decks, lanterns, banners, screens, railings, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the stave tips, outside the plum wrap tails, and below the full base plinth. No training dummy, padded post, supply crate, lid, barrel, treasure chest, gold, coins, gems, lock, padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, lantern, potion, gourd, drum, fan, sword, spear, blade, axe, banner, letters, numbers, kanji, logo, label text, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, detached planks, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-prop pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_stave_crate_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $supplyCrate,
        $trainingDummy,
        $pavilionBackdrop,
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

    Write-Host 'GENERATE world_warrior_stave_crate_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the stave crate pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for the stave crate pilot.'
    }
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}
