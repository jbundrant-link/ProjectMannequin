param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$references = @(
    'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png',
    'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_opposite_sheet_v2.png',
    'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_sheet_v2.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
)

$commonPrompt = @'
Create ONE complete full-body screen-right-facing blank PROJECT MANNEQUIN walk pose centered on a square 2K source image.

REFERENCE ROLES ARE STRICT. Reference 1 is the exact mannequin identity authority: preserve its faceless warm-ivory porcelain head and plates, plum-brown joints and understructure, lean athletic proportions, segmented anatomy, palette, contour weight, and lighting. Reference 2 supplies ONLY passing/up walk mechanics, camera, and baseline; reverse its support and swing limb ownership as explicitly required below. Reference 3 supplies the light-leg contact/down half-cycle's body scale and camera only; do not copy its incorrect repeated light-leg-forward later poses. References 4 and 5 supply only canonical Project Mannequin cel-shaded fighting-game finish.

LEG IDENTITY IS ABSOLUTE:
- LEG A is the camera-near limb and remains LIGHT WARM IVORY continuously from hip through thigh, knee, shin, ankle, and foot.
- LEG B is the camera-far limb and remains DARK PLUM-GRAY continuously from hip through foot.
- In this pose the LIGHT LEG A is the planted support leg. The DARK LEG B is the moving swing leg. Never recolor or swap them at any joint.

Natural relaxed walk mechanics and opposite arm swing. Exactly one figure, two arms, two legs, and two feet. Preserve all joints. Same three-quarter side camera as the mannequin references. Uniform pure chroma-green RGB 0,255,0 background with large clearance around the complete figure. No floor, cast shadow, scenery, grid, border, text, labels, arrows, motion effects, extra figure, extra limb, missing limb, merged legs, crop, run, march, attack, or idle.

PROJECT MANNEQUIN STYLE LOCK: polished modern 2.5D fighting-game sprite illustration, controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, simplified porcelain materials, and an arcade-readable silhouette; no photorealism, PBR, grit, painterly blur, flat vector art, or pixel art.
'@

$jobs = @(
    [pscustomobject]@{
        Name = 'a_passing'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_a_passing_v2.png'
        Metadata = 'Artifacts/style_calibration_mannequin_walk_a_passing_v2_job.json'
        Pose = @'
POSE: LIGHT-SUPPORT PASSING. The light ivory foot is planted flat directly under the hips with the light knee slightly bent and carrying the body's weight. The dark plum-gray leg is off the ground, bent at the knee, and passes forward beside the planted light leg toward screen-right. The dark toe has visible ground clearance. This is a passing pose, not contact, down, high-knee march, or idle.
'@
    },
    [pscustomobject]@{
        Name = 'a_up'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_a_up_v2.png'
        Metadata = 'Artifacts/style_calibration_mannequin_walk_a_up_v2_job.json'
        Pose = @'
POSE: LIGHT-SUPPORT UP / HIGH POINT. The light ivory support leg is nearly straight and rises onto its light toe just behind the hips, lifting the body to the walk cycle's high point. The dark plum-gray swing knee advances forward toward screen-right and begins extending its dark lower leg toward the next dark-heel contact. Keep the dark foot airborne. This is a natural walk high point, not a knee strike, march, jump, or idle.
'@
    }
)

Push-Location $projectRoot
try {
    if (-not (Test-Path $cli)) {
        throw "Higgsfield CLI not found: $cli"
    }
    foreach ($reference in $references) {
        if (-not (Test-Path $reference)) {
            throw "Missing generation reference: $reference"
        }
    }

    foreach ($job in $jobs) {
        if ($SkipExisting -and (Test-Path $job.Output) -and (Test-Path $job.Metadata)) {
            Write-Host "SKIP $($job.Name)"
            continue
        }

        $prompt = $commonPrompt + "`n" + $job.Pose
        $arguments = @('generate', 'create', 'gpt_image_2', '--prompt', $prompt)
        foreach ($reference in $references) {
            $arguments += @('--image', $reference)
        }
        $arguments += @(
            '--aspect_ratio', '1:1',
            '--resolution', '2k',
            '--quality', 'high',
            '--wait',
            '--wait-timeout', '20m',
            '--json'
        )

        $raw = & $cli @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Higgsfield failed to generate $($job.Name)."
        }

        $result = ($raw -join "`n") | ConvertFrom-Json
        $result | ConvertTo-Json -Depth 10 | Set-Content $job.Metadata -Encoding utf8
        $url = @($result)[0].result_url
        if ([string]::IsNullOrWhiteSpace($url)) {
            throw "Higgsfield returned no result URL for $($job.Name)."
        }

        Invoke-WebRequest -Uri $url -OutFile $job.Output
        Write-Host "SAVED $($job.Output)"
    }
}
finally {
    Pop-Location
}