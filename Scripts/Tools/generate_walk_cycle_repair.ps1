param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$IdentityPath,

    [Parameter(Mandatory = $true)]
    [string]$CurrentWalkPath,

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
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v2.png',
    'Artifacts/mannequin_walk_choreography_guide.png',
    $CurrentWalkPath,
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
)

$prompt = @"
Create a corrected production WALK-CYCLE SOURCE SHEET for $Name.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the exact character identity authority. Preserve this exact character in all eight poses: $IdentityDescription. Keep the same face/head design, costume geometry, asymmetry, proportions, palette, materials, contour weight, and lighting.
2. Reference 2 is the accepted Project Mannequin WALK CHOREOGRAPHY authority. Reproduce its exact eight sequential gait beats, complete leg alternation, opposing arm swing, 4-columns-by-2-rows organization, screen-right camera, spacing, and common foot baseline. Do not copy the mannequin's body, armor, colors, or identity.
3. Reference 3 is an anatomical timing map. GOLD A and CYAN B are persistent physical limbs. Follow its row-major order exactly: A-contact, A-down, A-passing, A-up, B-contact, B-down, B-passing, B-up. Do not render its colors, labels, circles, guide lines, stick geometry, or white background.
4. Reference 4 is a FAILURE/STYLE-CONTINUITY reference. Preserve its character rendering and scale, but reject its repeated poses, repeated same-leg lead, incomplete passing phases, and weak stride. Do not copy its choreography.
5 and 6 supply only canonical Project Mannequin rendering finish: controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, fighting-game anatomy, and polished anime presentation. Do not copy either character's identity.

Produce EXACTLY EIGHT separate complete full-body figures in one square 4 columns x 2 rows sheet, read left-to-right across the top row then the bottom row. All face screen-right in one consistent three-quarter side camera. Top row: A heel contact, down/compression, A planted while B passes, A toe support/high point while B advances. Bottom row: B heel contact, down/compression, B planted while A passes, B toe support/high point while A advances back into frame 1. The leg that leads in frame 5 must be physically opposite the leg that leads in frame 1. Natural opposing arm swing. At least six materially different silhouettes; no duplicated pose.

LOWER-BODY GEOMETRY IS ABSOLUTE. In each contact pose, one heel must visibly reach farthest forward while the opposite toe trails. In each down pose, the planted support knee must visibly compress. In each passing pose, one support leg must be near-vertical under the hips while the opposite bent foot is visibly airborne beside it. In each up pose, the support heel must be raised behind the hips while the opposite knee advances. Do not substitute standing, crouching, guarding, or repeated long-stride poses for these phases. Keep the camera-far physical leg one cel-shadow band darker through the full hip-to-foot chain so the lead-leg swap remains readable.

Use one consistent body scale, head size, light direction, and identical foot baseline. Every figure must fit independently inside its implied cell with generous clearance on all sides. Exactly two arms, two legs, two feet, and all identity features in every frame. Natural grounded walk only: no idle, shuffle, march, run, jump, attack, guard, or knee strike.

Uniform pure chroma-green RGB 0,255,0 background. No floor, cast shadow, scenery, grid, dividers, border, text, labels, numbers, arrows, colored guide marks, motion effects, extra figure, extra limb, missing limb, merged legs, touching figures, or crop.

PROJECT MANNEQUIN STYLE LOCK: cohesive modern 2.5D fighting-game production, confident dark ink contours, broad cel-shaded value planes, clean designed highlights, saturated controlled accents, strong arcade-readable silhouettes, and polished anime fighting-game finish. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square animation source.
"@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $OutputPath) -and (Test-Path $MetadataPath)) {
        Write-Host "SKIP $Name"
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
        throw "Higgsfield failed to generate $Name."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $metadataDirectory = Split-Path -Parent $MetadataPath
    if ($metadataDirectory) {
        New-Item -ItemType Directory -Force -Path $metadataDirectory | Out-Null
    }
    $result | ConvertTo-Json -Depth 10 | Set-Content $MetadataPath -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $Name."
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