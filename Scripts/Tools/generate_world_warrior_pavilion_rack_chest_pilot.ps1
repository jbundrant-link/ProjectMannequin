param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

# Second World Warrior breakable-crate identity, for Pavilion Circuit.
#
# Reuses the proven Sparring Supply Crate path exactly: same model, same strict
# reference-role protocol, same chroma-green plate, same 2K square pilot.
#
# The design is deliberately the OPPOSITE silhouette to the approved Sparring
# Supply Crate. That crate is a low box, wider than tall; this one is an
# upright standing chest, taller than wide. Two breakable crates that share a
# silhouette would read as the same prop recoloured, which is exactly what the
# variant gate exists to prevent.
#
# Motifs come from the Pavilion Circuit art itself rather than being invented:
# vermilion lacquered posts, slate tiled pent roofs, indigo hanging banners,
# saffron paper lanterns, and the red circular medallion that repeats across
# that stage. The deck also carries visible practice-weapon racks, so a
# standing rack chest is what that stage would plausibly store.

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_rack_chest_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_pavilion_rack_chest_pilot_v1_job.json'

$supplyCrate = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_supply_crate_style_pilot_v1.png'
$scorePickup = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_score_pickup_style_pilot_v1.png'
$pavilionBackdrop = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_backdrop_style_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR PAVILION RACK CHEST, an original breakable stage prop viewed in a clean right-facing three-quarter angle, centered and fully visible.

The prop must read instantly as a sturdy, breakable upright pavilion equipment chest at gameplay size, clearly a standing rack cabinet that stores practice weapons for a tournament pavilion, and NOT a padded training post, barrel, treasure chest, wardrobe, sci-fi locker, shrine, or pickup item. Build one tall standing chest with this exact large-form hierarchy:
- one upright rectangular chest body, clearly TALLER THAN WIDE, standing on two short blocky timber feet that lift it just off the deck;
- one narrow slate-grey tiled pent roof capping the top, overhanging the front and both sides, with four thick tile courses and a simplified upturned front edge;
- two tall vertical vermilion lacquered corner posts framing the front face, each with three large bronze rivet studs;
- one broad warm-ivory plaster front panel between the posts, carrying one bold red circular medallion emblem at chest height with a simple four-spoke ring inside;
- one thick indigo cloth banner hanging from under the roof across the upper front, with two short blunt weighted tails and one bold horizontal saffron stripe;
- three blunt pale practice staves standing upright in an open saffron-lined slot along the right side panel, their rounded tops rising just above the roof line and attached to the chest;
- clear BREAK-READY structure: one thick diagonal split seam across the ivory front panel, one cracked and displaced tile at the left roof edge, and two short pale splinter notches along the lower right post, all cut into the material rather than floating.

Function must come from unmistakable breakable-equipment-chest cues: standing rack body, tiled cap, banded lacquer posts, exposed staves, and pre-scored break seams. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The tall upright body, tiled pent roof, vermilion posts, red medallion, indigo banner, and exposed staves must survive at that size. Keep the chest grounded, solid, and bottom-weighted, standing flat as if resting on a pavilion deck.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Sparring Supply Crate. Use ONLY its breakable-prop contour weight, large-form simplification, carved break-seam treatment, grounded stage-prop presentation, and warm-timber/ivory/vermilion/deep-indigo/saffron hierarchy. DO NOT copy its low wide box silhouette, overhanging flat lid, three horizontal plank slats, corner brackets, cargo strap knot, rope handle loop, hand-wrap bundle, or practice mitt. This new prop must be immediately distinguishable from it in silhouette.
2. Reference 2 is the approved World Warrior Judge's Laurel Fan. Use only its bold ceremonial tournament finish, thick ivory contours, vermilion lacquer, saffron trim, and clean designed material treatment. Do not copy its fan shape, ribs, pleats, handle, tassel, pickup scale, or silhouette.
3. Reference 3 is the approved Pavilion Circuit environment. Use only its warm dusk pavilion mood, lacquered post and tiled roof material language, paper-lantern warmth, indigo banner cloth, red medallion motif, and ordered value hierarchy. Do not reproduce architecture, buildings, decks, railings, lanterns, banners in place, mountains, or scene.
4-6. References 4-6 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the roof and stave tops, outside the banner tails, and below the full chest feet. No training dummy, padded post, barrel, low wide crate, treasure chest, gold, coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, hanging lantern, potion, gourd, drum, fan, blade, spear tip, letters, numbers, kanji, logo, label text, star icon, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, detached tiles, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-prop pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_pavilion_rack_chest_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $supplyCrate,
        $scorePickup,
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

    Write-Host 'GENERATE world_warrior_pavilion_rack_chest_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the pavilion rack chest pilot after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $payload = $raw | Out-String
    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $metadata) | Out-Null
    # WriteAllText rather than Set-Content: PowerShell 5.1 -Encoding UTF8 emits a
    # BOM, and json.loads rejects it with "Unexpected UTF-8 BOM" unless the
    # reader knows to use utf-8-sig. Provenance files get read by tooling that
    # should not have to know that.
    [System.IO.File]::WriteAllText(
        (Join-Path $projectRoot $metadata),
        $payload,
        (New-Object System.Text.UTF8Encoding($false)))

    # The CLI has returned two different shapes across versions: an object with
    # a nested results collection, and a top-level array of job records with
    # flat *_result_url fields. Handle both, and prefer the full-resolution
    # result over the compressed preview.
    $job = $payload | ConvertFrom-Json
    $record = if ($job -is [System.Array]) { $job[0] } else { $job }
    $url = $null
    foreach ($candidate in @(
            $record.result_url,
            $record.results.raw.url,
            $record.results.min.url,
            $record.min_result_url)) {
        if ($candidate) { $url = $candidate; break }
    }
    if (-not $url) {
        throw "Higgsfield returned no image URL. Job metadata: $metadata"
    }
    if ($record.status -and $record.status -ne 'completed') {
        throw "Higgsfield job did not complete (status=$($record.status))."
    }

    New-Item -ItemType Directory -Force -Path (Split-Path -Parent $output) | Out-Null
    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "WROTE $output"
}
finally {
    Pop-Location
}
