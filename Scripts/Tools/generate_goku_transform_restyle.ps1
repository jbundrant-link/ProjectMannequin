param(
    [int]$MaxRetry = 6
)

# Restyle the two Goku transformation sheets (base->Blue, Blue->Ultra Instinct)
# to the new cel-shaded finish. These show a gradual hair-color transition, so
# the prompt preserves each cell's exact color while upgrading the rendering.

$ErrorActionPreference = "Stop"
$workspace = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
if (-not (Test-Path -LiteralPath $cli)) { $cli = "higgsfield" }
$style = (Resolve-Path (Join-Path $workspace "Assets\Sprites\Mannequin\mannequin_master_higgsfield_v1.png")).Path
$dir = Join-Path $workspace "Assets\Sprites\Goku"

$specs = @(
    @{ src = "goku_transform_blue_higgsfield_v1.png";     out = "goku_restyle_transform_blue.png";     aspect = "16:9"; rows = "3" },
    @{ src = "goku_transform_instinct_higgsfield_v1.png"; out = "goku_restyle_transform_instinct.png"; aspect = "3:4";  rows = "8" }
)

function New-TransformPrompt($rows) {
    return "The first image is the exact choreography to keep: Dragon Ball Z Goku mid-transformation. Preserve the EXACT hair color, eye color, pose, and transformation stage shown in each individual cell -- the hair color changes gradually from cell to cell across the sheet, so keep each cell's own exact color and do not unify them. Redraw every pose at high fidelity while preserving the identical grid of 8 columns and $rows rows, the identical pose inside each cell, and the identical left-to-right top-to-bottom order. Draw the character at the SAME consistent body size in every cell, both feet on a common ground baseline. Match ONLY the rendering finish of the second image: bold clean black outlines, smooth cel-shaded coloring with crisp highlight and shadow banding, polished modern 2.5D fighting-game sprite look. Draw each figure large and centered with a clearly rendered face and complete correctly connected limbs. Render the transformation energy as a controlled glow that hugs the body; it may be a little brighter for the power-up moment, but do NOT let it balloon into wide flames or a large envelope far beyond the silhouette. Use a uniform pure green #00FF00 background, keep clear empty margins between figures, and include no motion-blur streaks, no duplicated or extra limbs, no extra figures, and no text or labels."
}

foreach ($spec in $specs) {
    $outPath = Join-Path $dir $spec.out
    $srcPath = (Resolve-Path (Join-Path $dir $spec.src)).Path
    $prompt = New-TransformPrompt $spec.rows
    $ok = $false
    for ($a = 1; $a -le $MaxRetry; $a++) {
        try {
            $raw = & $cli generate create gpt_image_2 `
                --prompt $prompt --image $srcPath --image $style `
                --aspect_ratio $spec.aspect --resolution "2k" --quality "high" `
                --wait --wait-timeout "20m" --json 2>$null
            if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
            $url = (($raw -join "`n") | ConvertFrom-Json)[0].result_url
            if ([string]::IsNullOrWhiteSpace($url)) { throw "no url" }
            Invoke-WebRequest -Uri $url -OutFile $outPath
            Write-Host "OK   $($spec.out)"
            $ok = $true
            break
        }
        catch {
            if ($a -ge $MaxRetry) { Write-Host "FAIL $($spec.out) : $($_.Exception.Message)"; break }
            Start-Sleep -Seconds (8 * $a)
        }
    }
}
Write-Host "Transform restyle done."
