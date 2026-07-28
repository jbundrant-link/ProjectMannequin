param(
    [switch]$SkipExisting,
    [ValidateRange(1, 3)]
    [int]$RequestAttempts = 3
)

# Fourth and final World Warrior breakable-crate identity, for Champion's
# Courtyard (the final boss stage).
#
# Reuses the proven Sparring Supply Crate / Pavilion Rack Chest / Grand
# Tournament Champion's Trophy Podium path exactly: same model, same strict
# reference-role protocol, same chroma-green plate, same 2K square pilot.
#
# The design is deliberately a FOURTH, distinct silhouette from all three
# approved crates. The Supply Crate is a low wide box. The Pavilion Rack
# Chest is a tall upright standing cabinet. The Trophy Podium is a wide-based
# stepped, tiered stack. This one is a ROUND, CYLINDRICAL CEREMONIAL LANTERN
# URN, the only curved/round-bodied silhouette of the four.
#
# Motifs come from the Champion's Courtyard plate art itself rather than
# being invented: the hanging lanterns and braziers, the vermilion banners,
# the warm timber and pale plaster halls, and the deep indigo tiled roofs
# that define the finale arena.

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_champions_courtyard_lantern_urn_style_pilot_v1.png'
$metadata = 'Artifacts/style_calibration_world_warrior_champions_courtyard_lantern_urn_pilot_v1_job.json'

$supplyCrate = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_supply_crate_style_pilot_v1.png'
$rackChest = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_rack_chest_style_pilot_v1.png'
$trophyPodium = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grand_tournament_trophy_podium_style_pilot_v1.png'
$courtyardPlate = 'Assets/Stages/WorldWarrior/world_warrior_champions_courtyard_full_frame_plate_style_v2.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. Produce ONE approved-quality full-object IDENTITY / FUNCTION PILOT of the WORLD WARRIOR CHAMPION'S COURTYARD CEREMONIAL LANTERN URN, an original breakable stage prop viewed in a clean right-facing three-quarter angle, centered and fully visible.

The prop must read instantly as a sturdy, breakable ceremonial storage urn at gameplay size, clearly a round lantern-topped vessel that belongs in the tournament finale courtyard, and NOT a padded training post, low wide crate, standing wardrobe, stepped stone podium, barrel of liquid, treasure chest, altar, or pickup item. Build one ROUND, CYLINDRICAL structure with this exact large-form hierarchy:
- one round barrel-bodied urn, wider in the middle than at top or base, standing upright on a short dark aged-timber ring foot;
- warm pale plaster-and-timber banding around the urn body, matching the courtyard halls;
- one deep-indigo tiled dome cap on top, shaped like a small hanging-lantern roof, with a short brass finial spike at its peak;
- one vermilion cloth banner sash wrapped once around the urn's widest point, with one loose frayed tail hanging down the front;
- two small unlit brazier-bowl handles/loops set into the sides of the urn body, rendered as flat graphic ornaments rather than active flame effects;
- clear BREAK-READY structure: one thick diagonal crack running across the round body, one chipped notch at the base ring, and one frayed torn edge on the banner tail, all cut into the material rather than floating.

Function must come from unmistakable breakable-ceremonial-urn cues: the round barrel body, the lantern-dome cap with finial, the banner sash, and pre-scored break seams. Use only a few large components with thick contours. It must remain legible when reduced to roughly 96, 128, and 160 pixels tall. The round silhouette, dome cap, and banner sash must survive at that size. Keep the urn grounded, solid, and bottom-weighted, standing flat as if resting on the courtyard stone.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the approved World Warrior Sparring Supply Crate. Use ONLY its breakable-prop contour weight, large-form simplification, carved break-seam treatment, and grounded stage-prop presentation. DO NOT copy its low wide box silhouette, overhanging flat lid, plank slats, corner brackets, cargo strap knot, rope handle, or practice mitt.
2. Reference 2 is the approved World Warrior Pavilion Rack Chest. Use ONLY its breakable-prop contour weight and carved break-seam treatment. DO NOT copy its tall upright standing-cabinet silhouette, tiled pent roof, vermilion corner posts, red medallion, indigo banner, or exposed practice staves.
3. Reference 3 is the approved Grand Tournament Champion's Trophy Podium. Use ONLY its breakable-prop contour weight and carved break-seam treatment. DO NOT copy its stepped tiered stone podium silhouette, trophy-cup lid, or gold brazier-bowl corner emblems.
   This new prop must be immediately distinguishable in silhouette from ALL THREE of the above — it must be ROUND and CYLINDRICAL, never a box, a tall cabinet, or a stepped stack.
4. Reference 4 is the approved Champion's Courtyard finale arena plate. Use only its hanging lanterns and braziers, vermilion banners, warm timber and pale plaster tones, and deep indigo tiled roof color. Do not reproduce the courtyard walls, halls, mountains, pines, or full scene.
5-7. References 5-7 are the canonical mannequin, Ryu, and Goku rendering-finish anchors. Match their confident dark contours, broad two-to-four-band cel shading, clean designed highlights, simplified materials, saturated controlled accents, and arcade-readable 2.5D fighting-game volume. Do not copy any character's face, body, costume, anatomy, or identity.

One complete centered prop on uniform pure chroma-green RGB 0,255,0 with generous uninterrupted green margin on every side, including above the dome cap finial, outside the banner tail, and below the full base ring. No training dummy, padded post, low wide crate, tall standing cabinet, stepped stone podium, tiled pent roof, vermilion posts, treasure chest, gold coins, gems, lock, hinge padlock, keyhole, Archive crystal, shard, cube, chip, data cache, sci-fi panel, glowing seam, potion, gourd, drum, fan, blade, spear tip, letters, numbers, kanji, logo, label text, star icon, face, eyes, person, creature, floating orb, aura, detached ring, rays, sparks, active flame/fire effect, debris cloud, floor, cast shadow, scenery, room, architecture, border, grid, duplicate object, photorealism, PBR, painterly blur, flat vector art, pixel art, or crop. 2K square stage-prop pilot.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP world_warrior_champions_courtyard_lantern_urn_pilot_v1'
        return
    }
    if (-not (Test-Path $cli -PathType Leaf)) {
        throw "Missing Higgsfield CLI: $cli"
    }

    $references = @(
        $supplyCrate,
        $rackChest,
        $trophyPodium,
        $courtyardPlate,
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

    Write-Host 'GENERATE world_warrior_champions_courtyard_lantern_urn_pilot_v1'
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for the champions courtyard lantern urn pilot after $RequestAttempts attempts."
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
