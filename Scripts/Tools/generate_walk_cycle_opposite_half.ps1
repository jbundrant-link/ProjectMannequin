param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$IdentityPath,

    [Parameter(Mandatory = $true)]
    [string]$FirstHalfPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$MetadataPath,

    [Parameter(Mandatory = $true)]
    [string]$IdentityDescription,

    [ValidateSet('nano_banana_pro', 'gpt_image_2')]
    [string]$Model = 'nano_banana_pro',

    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$references = @(
    $IdentityPath,
    $FirstHalfPath,
    'Artifacts/mannequin_walk_choreography_b_half.png',
    'Assets/Sprites/Concepts/StyleCalibration/archive_knight_walk_opposite_sheet_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
)

$prompt = @"
Create ONLY the missing OPPOSITE-LEG HALF of the $Name walk cycle as exactly FOUR complete full-body poses in a clean 2 columns x 2 rows source sheet, ordered left-to-right then top-to-bottom.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact character identity authority. Preserve this exact character in every pose: $IdentityDescription. Keep the same face, head design, costume geometry, proportions, palette, materials, contour weight, and lighting.
2. Reference 2 contains ONLY the accepted four-pose first half-cycle. Preserve its exact character rendering, screen-right three-quarter camera, body scale, head size, and foot baseline as the preceding A-leg half. Do not repeat its same physical lead leg in the new poses.
3. Reference 3 is the controlling B-LEG anatomical timing map. GOLD A and CYAN B are persistent physical limbs. Reproduce its four row-major beats exactly: B heel contact, B down/compression, B planted while A passes, B toe support/high point while A advances. Do not render its colors, labels, circles, guide lines, stick geometry, or white background.
4. Reference 4 is an accepted four-pose opposite-leg walking-mechanics authority. Copy only its contact/down/passing/up flow. Do not copy the Archive Knight's identity, armor, asymmetry, weapon, colors, or materials.
5 and 6 supply only canonical Project Mannequin rendering finish: controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, fighting-game anatomy, and polished anime presentation. Do not copy either character's identity.

The four poses continue directly after reference 2's top row:
1. B-CONTACT: the physical leg opposite the first-half lead reaches farthest toward screen-right with heel contact while A trails behind on its toe.
2. B-DOWN: body compresses over the planted B foot while A lifts from behind.
3. B-PASSING: B remains planted under the hips while bent A passes forward with visible toe clearance.
4. B-UP: B rises onto its toe just behind the hips while A advances toward the next heel contact, flowing into the original frame 1.

VISIBLE LIMB REVERSAL IS MANDATORY. Compare new pose 1 directly with reference 2 pose 1: the opposite trouser leg must emerge in front at the crotch and connect to the forward shoe; the original lead leg must visibly recede behind. Reverse the arms too: the arm that swings forward in reference 2 pose 1 must swing behind in new pose 1, while its opposite arm swings forward. Apply the same physical swap to down, passing, and up. A different silhouette with the same lead leg or same forward arm is a failure.

Keep natural opposing arm swing. Preserve all asymmetric costume features on the same body side in every pose; alternate the physical legs without mirroring or swapping the character's costume, sash, accessories, face, or camera. All four silhouettes must be materially distinct. No repeated contact pose, idle stance, shuffle, march, run, jump, attack, guard, or knee strike.

Use one consistent body scale, head size, light direction, and identical foot baseline. Every complete figure must fit independently inside its implied quadrant with generous clearance on all sides. Exactly two arms, two legs, two feet, and all identity features in every frame.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, colored guide marks, motion effects, extra figure, extra limb, missing limb, merged legs, touching figures, or crop.

PROJECT MANNEQUIN STYLE LOCK: cohesive modern 2.5D fighting-game production, confident dark ink contours, broad cel-shaded value planes, clean designed highlights, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game finish. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square animation source.
"@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $OutputPath) -and (Test-Path $MetadataPath)) {
        Write-Host "SKIP $Name opposite half"
        return
    }

    if (-not (Test-Path $cli)) {
        throw "Higgsfield CLI not found: $cli"
    }
    foreach ($reference in $references) {
        if (-not (Test-Path $reference)) {
            throw "Missing generation reference: $reference"
        }
    }

    $resolvedReferences = $references | ForEach-Object { (Resolve-Path $_).Path }
    $arguments = @('generate', 'create', $Model, '--prompt', $prompt)
    foreach ($reference in $resolvedReferences) {
        $arguments += @('--image', $reference)
    }
    $arguments += @(
        '--aspect_ratio', '1:1',
        '--resolution', '2k',
        '--wait',
        '--wait-timeout', '20m',
        '--json'
    )
    if ($Model -eq 'gpt_image_2') {
        $arguments = $arguments[0..($arguments.Count - 2)] + @('--quality', 'high', '--json')
    }

    $raw = & $cli @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Higgsfield failed to generate $Name opposite half."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $metadataDirectory = Split-Path -Parent $MetadataPath
    if ($metadataDirectory) {
        New-Item -ItemType Directory -Force -Path $metadataDirectory | Out-Null
    }
    $result | ConvertTo-Json -Depth 10 | Set-Content $MetadataPath -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $Name opposite half."
    }

    $outputDirectory = Split-Path -Parent $OutputPath
    if ($outputDirectory) {
        New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null
    }
    Invoke-WebRequest -Uri $url -OutFile $OutputPath
    Write-Host "SAVED $OutputPath"
}
finally {
    Pop-Location
}