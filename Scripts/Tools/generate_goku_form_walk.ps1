param(
    [Parameter(Mandatory = $true)]
    [string]$FormName,

    [Parameter(Mandatory = $true)]
    [string]$IdentityPath,

    [Parameter(Mandatory = $true)]
    [string]$IdentityDescription,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$MetadataPath,

    [ValidateSet('nano_banana_pro', 'gpt_image_2')]
    [string]$Model = 'nano_banana_pro',

    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$references = @(
    'Assets/Sprites/Concepts/StyleCalibration/goku_base_walk_sheet_pilot_v2.png',
    $IdentityPath,
    'Artifacts/mannequin_walk_choreography_guide.png',
    'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
)

$prompt = @"
Restyle the first supplied production walk sheet into $FormName while preserving its animation and layout exactly.

Reference 1 is the absolute choreography, camera, scale, spacing, baseline, and frame-order authority. Preserve the identical eight full-body poses in the identical 4 columns x 2 rows cells, read left-to-right then top-to-bottom. Do not replace, mirror, reorder, crop, or simplify any pose. Keep the exact contact/down/passing/up then opposite-leg contact/down/passing/up mechanics and opposing arm swing.

Reference 2 is the exact form identity authority. Redraw every figure as $IdentityDescription. Preserve the exact form hair, face, eye color, outfit, aura treatment, proportions, palette, and materials in every frame. Reference 3 reinforces persistent A/B limb ownership only; do not render guide colors, labels, circles, lines, or stick geometry. References 4 and 5 supply only canonical Project Mannequin rendering finish.

Use the same restrained thin aura rim visible in the identity reference; no large flames, glow columns, energy envelope, sparks crossing cells, or detached effects. Keep every complete figure centered inside its original implied cell with the same generous pure-green margin. Uniform pure chroma-green RGB 0,255,0 background. No floor, shadow, scene, grid, divider, border, text, labels, numbers, arrows, projectile, motion effect, extra figure, extra limb, missing limb, touching figures, or crop.

PROJECT MANNEQUIN STYLE LOCK: controlled dark contours, broad two-to-four-step cel shading, clean designed highlights, polished modern 2.5D anime fighting-game anatomy and silhouettes. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square animation source.
"@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $OutputPath) -and (Test-Path $MetadataPath)) {
        Write-Host "SKIP $FormName walk"
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

    $arguments = @('generate', 'create', $Model, '--prompt', $prompt)
    foreach ($reference in $references) {
        $arguments += @('--image', (Resolve-Path $reference).Path)
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
        throw "Higgsfield failed to generate $FormName walk."
    }
    $result = ($raw -join "`n") | ConvertFrom-Json
    $metadataDirectory = Split-Path -Parent $MetadataPath
    if ($metadataDirectory) {
        New-Item -ItemType Directory -Force -Path $metadataDirectory | Out-Null
    }
    $result | ConvertTo-Json -Depth 10 | Set-Content $MetadataPath -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $FormName walk."
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