param(
    [ValidateSet('pavilion', 'grand_tournament', 'index_vaults')]
    [string]$Target = 'pavilion',
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

# The three floors being replaced fail Scripts/Tools/audit_stage_floor_materials.py
# because their detail runs in courses that line up with the camera's horizontal
# axis, so they collapse into screen-wide stripes. The prompt below attacks that
# directly: no rows, no courses, no gradients, and rotationally even detail.
$styleLock = @'
PROJECT MANNEQUIN STYLE LOCK. Match the supplied mannequin, Ryu, and Goku references as one cohesive modern 2.5D fighting-game production: confident dark ink contours, broad two-to-four-step cel shading, clean graphic highlights, stylized dimensional forms, saturated controlled accents, and polished anime fighting-game finish. High fidelity comes from shape design, value structure, and finish, not PBR texture noise or photographic grain. Do not include characters. No photorealism, realistic PBR, microtexture noise, painterly blur, flat vector art, pixel art, generated text, symbols, logos, or UI.
'@

$materialContract = @'

STRICT TOP-DOWN GAMEPLAY FLOOR MATERIAL. Produce a perfectly flat orthographic top-down 1:1 texture photographed straight down at ninety degrees. This is a tiling ground material, not a scene.

ISOTROPY IS THE HARD REQUIREMENT. The previous version of this floor failed because its detail ran in regular horizontal courses; under the stage camera those courses collapsed into screen-wide stripes and the ground read as a striped wall. The replacement must look equally plausible when rotated ninety degrees. Use an irregular interlocking layout with paving joints running in several different directions. Absolutely no continuous straight joint may cross the full width or full height. No brick courses, no running bond, no rows, no ranks, no planks, no parallel banding, no stripes, no concentric rings, no radial burst, no single dominant direction, no centred motif, no medallion, no border, no frame, no vignette, no corner ornament.

EVEN LIGHTING. Flat ambient illumination with no light source, no cast shadow direction, no gradient from any edge or corner, and no hotspot. Brightness must be uniform across the whole square so tiles do not reveal their boundaries.

SEAMLESS. The texture must tile edge to edge in both directions: content crossing the left edge continues exactly at the right edge, and content crossing the top edge continues exactly at the bottom edge.

CONTRAST BUDGET. Keep value range narrow and detail quiet so fighters, feet, pickups, and telegraphs dominate. Wear, grime, and cracks should be sparse, irregular, and scattered rather than patterned.
'@

$targets = @{
    'pavilion' = @{
        Output = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v2.png'
        Metadata = 'Artifacts/style_calibration_world_warrior_pavilion_floor_style_v2_job.json'
        Rejected = 'Assets/Stages/WorldWarrior/world_warrior_pavilion_floor_style_v1.png'
        Label = 'world_warrior_pavilion_floor_style_v2'
        Subject = @'

SUBJECT: the lantern-court paving of World Warrior's Pavilion Circuit. Irregular interlocking stone flags in warm sand and bone tones with muted clay-red accents, deep umber joints, occasional moss in isolated joint pockets, and sparse scattered grit. Keep the palette warm and low contrast.

The first supplied image is the REJECTED V1 floor. Reuse only its warm sand, bone, clay-red, and umber palette and its clean contour language. V1 fails because it is built from regular horizontal courses that read as stripes; do not reproduce its layout, banding, rows, or any part of its structure.
'@
    }
    'grand_tournament' = @{
        Output = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v2.png'
        Metadata = 'Artifacts/style_calibration_world_warrior_grand_tournament_floor_style_v2_job.json'
        Rejected = 'Assets/Stages/WorldWarrior/world_warrior_grand_tournament_floor_style_v1.png'
        Label = 'world_warrior_grand_tournament_floor_style_v2'
        Subject = @'

SUBJECT: the arena paving of World Warrior's Grand Tournament Floor. Irregular interlocking pale limestone flags in ivory and warm grey with cool slate joints, faint indigo mineral veining, sparse chipped edges, and scattered fine dust. Keep the palette bright, cool-leaning, and low contrast.

The first supplied image is the REJECTED V1 floor. Reuse only its ivory, warm grey, slate, and indigo palette and its clean contour language. V1 fails because its slabs form broad parallel bands that read as stripes; do not reproduce its layout, banding, rows, or any part of its structure.
'@
    }
    'index_vaults' = @{
        Output = 'Assets/Stages/ArchiveDistrict/archive_index_vaults_floor_higgsfield_v2.png'
        Metadata = 'Artifacts/style_calibration_archive_index_vaults_floor_higgsfield_v2_job.json'
        Rejected = 'Assets/Stages/ArchiveDistrict/archive_index_vaults_floor_higgsfield_v1.png'
        Label = 'archive_index_vaults_floor_higgsfield_v2'
        Subject = @'

SUBJECT: the catalog-stack decking of Archive Nexus' Index Vaults. Irregular interlocking dark indigo and slate composite panels with thin cyan data-seams in scattered joints, faint archive glyph etching too small to read as text, sparse scuffing, and occasional pale inlay fragments. Keep the palette deep, cool, and low contrast.

The first supplied image is the REJECTED V1 floor. Reuse only its indigo, slate, and cyan palette and its clean contour language. V1 fails because its panels form regular horizontal courses that read as stripes; do not reproduce its layout, banding, rows, or any part of its structure.
'@
    }
}

$plan = $targets[$Target]
$prompt = $styleLock + $plan.Subject + $materialContract

Push-Location $projectRoot
try {
    $references = @($plan.Rejected, $mannequin, $ryu, $goku)
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

    Write-Host "GENERATE $($plan.Label)"
    $raw = $null
    for ($attempt = 1; $attempt -le $RequestAttempts; $attempt++) {
        $raw = & $cli @arguments
        if ($LASTEXITCODE -eq 0) {
            break
        }
        if ($attempt -eq $RequestAttempts) {
            throw "Higgsfield failed for $($plan.Label) after $RequestAttempts attempts."
        }
        Write-Warning "Higgsfield transport failed; retrying attempt $($attempt + 1)/$RequestAttempts."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $plan.Metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $($plan.Label)."
    }
    Invoke-WebRequest -Uri $url -OutFile $plan.Output
    Write-Host "SAVED $($plan.Output)"
}
finally {
    Pop-Location
}
