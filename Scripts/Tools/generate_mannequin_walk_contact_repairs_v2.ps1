param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$references = @(
    'Assets/Sprites/Mannequin/mannequin_master_higgsfield_v1_transparent.png',
    'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_opposite_sheet_v2.png',
    'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png',
    'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png',
    'Assets/Sprites/Mannequin/higgsfield_walk_sheet_v1.png'
)

$commonPrompt = @'
Create ONE complete full-body screen-right-facing blank PROJECT MANNEQUIN walk pose centered on a square 2K source image.

REFERENCE ROLES ARE STRICT. Reference 1 is the exact mannequin identity authority: preserve its faceless warm-ivory porcelain head and plates, plum-brown joints and understructure, lean athletic proportions, simplified segmented anatomy, palette, contour weight, and lighting. In reference 2, ONLY the two bottom-row figures have the correct persistent leg ownership: the DARK PLUM-GRAY LEG is the support limb while the LIGHT WARM-IVORY LEG swings. Extend that exact limb identity into the requested pose. References 3 and 4 supply only canonical cel-shaded fighting-game finish. Reference 5 is a FAILURE EXAMPLE ONLY and must not be copied; it incorrectly makes the light leg lead repeatedly.

LEG IDENTITY IS ABSOLUTE. The requested forward/support leg must be visibly DARK PLUM-GRAY continuously from hip plate through thigh, knee, shin, ankle, and foot. The other leg must remain visibly LIGHT WARM-IVORY continuously from hip through foot. Do not swap their colors at the knee. Do not recolor the dark forward leg light. Do not put the light foot farthest toward screen-right.

Natural relaxed walk mechanics and opposite arm swing. Exactly one figure, two arms, two legs, and two feet. Preserve all joints. Same three-quarter side camera as the mannequin references. Uniform pure chroma-green RGB 0,255,0 background with large clearance around the complete figure. No floor, cast shadow, scenery, grid, border, text, labels, arrows, motion effects, extra figure, extra limb, missing limb, merged legs, crop, run, march, attack, or idle.

PROJECT MANNEQUIN STYLE LOCK: polished modern 2.5D fighting-game sprite illustration, controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, simplified porcelain materials, and an arcade-readable silhouette; no photorealism, PBR, grit, painterly blur, flat vector art, or pixel art.
'@

$jobs = @(
    [pscustomobject]@{
        Name = 'b_contact'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_b_contact_v2.png'
        Metadata = 'Artifacts/style_calibration_mannequin_walk_b_contact_v2_job.json'
        Pose = @'
POSE: DARK-LEG HEEL CONTACT. The dark plum-gray leg reaches forward across the body toward screen-right and is the farthest-forward limb; its dark heel touches the baseline and its dark toe angles slightly upward. The light ivory leg extends behind the hips with only its light toe touching the baseline. This is the exact limb-identity opposite of the old sheet's light-leg contact pose. Use a long but natural walking stride, not a split or lunge.
'@
    },
    [pscustomobject]@{
        Name = 'b_down'
        Output = 'Assets/Sprites/Concepts/StyleCalibration/mannequin_walk_b_down_v2.png'
        Metadata = 'Artifacts/style_calibration_mannequin_walk_b_down_v2_job.json'
        Pose = @'
POSE: DARK-LEG DOWN / COMPRESSION. The dark plum-gray foot is flat on the baseline slightly ahead of the hips, and the body lowers with a bent dark knee over that weight-bearing leg. The light ivory leg trails behind with its light heel lifted and light toe just leaving the baseline. This is one beat after dark-heel contact. It must not look like another contact pose, passing pose, idle, or light-leg support.
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