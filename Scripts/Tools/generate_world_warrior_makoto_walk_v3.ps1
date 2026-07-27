param(
    [ValidateSet('all', 'a_contact', 'a_down', 'a_passing', 'a_up', 'b_contact', 'b_down', 'b_passing', 'b_up')]
    [string]$OnlyPhase = 'all',
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_style_pilot_v1.png'
$failedWalk = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_walk_sheet_style_v2.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$poseReferenceDirectory = 'Artifacts/StyleCalibration/WalkPoseReferences'

$identityDescription = @'
one exact tall athletic adult woman with warm medium skin; sharp composed face; huge high looped black braided crest with saffron bands; short asymmetrical vermilion pavilion mantle; fitted deep-indigo wrap top with warm-ivory diagonal chest panel; charcoal kick trousers; long split indigo pavilion panels; plum hand wraps; warm-ivory shin guards with vermilion chevrons; ivory split-sole shoes; and one bronze crescent token at the right hip
'@

$phases = @(
    [pscustomobject]@{ Slug='a_contact'; Reference='mannequin_a_phase_1.png'; Detail='A-CONTACT: camera-near A heel reaches farthest toward screen-right while camera-far B trails on its toe. Long grounded stride; opposite arm swings forward.' },
    [pscustomobject]@{ Slug='a_down'; Reference='mannequin_a_phase_2.png'; Detail='A-DOWN: body compresses over planted camera-near A foot; B lifts from behind. Bent support knee and low hips.' },
    [pscustomobject]@{ Slug='a_passing'; Reference='mannequin_a_phase_3.png'; Detail='A-PASSING: A plants nearly vertical under the hips while bent B passes forward with visible toe clearance. Compact gait, not a knee strike.' },
    [pscustomobject]@{ Slug='a_up'; Reference='mannequin_a_phase_4.png'; Detail='A-UP: A rises onto its toe just behind the hips while B knee advances naturally toward the next heel contact. This is a walk high point, not a guard pose.' },
    [pscustomobject]@{ Slug='b_contact'; Reference='mannequin_b_phase_5.png'; Detail='B-CONTACT: camera-far B leg visibly crosses forward and its heel reaches farthest toward screen-right while A trails on its toe. Arms reverse from A-contact.' },
    [pscustomobject]@{ Slug='b_down'; Reference='mannequin_b_phase_6.png'; Detail='B-DOWN: body compresses over planted camera-far B foot while A lifts from behind. B remains visibly shadowed and weight-bearing.' },
    [pscustomobject]@{ Slug='b_passing'; Reference='mannequin_b_phase_7.png'; Detail='B-PASSING: B plants nearly vertical under the hips while bent A passes forward with visible toe clearance. Compact gait, not a knee strike.' },
    [pscustomobject]@{ Slug='b_up'; Reference='mannequin_b_phase_8.png'; Detail='B-UP: B rises onto its toe just behind the hips while A knee advances toward the next A-contact. Preserve the opposite-leg support and reversed arm swing.' }
)

Push-Location $projectRoot
try {
    if (-not (Test-Path $cli)) {
        throw "Higgsfield CLI not found: $cli"
    }

    foreach ($phase in $phases) {
        if ($OnlyPhase -ne 'all' -and $OnlyPhase -ne $phase.Slug) {
            continue
        }

        $poseReference = Join-Path $poseReferenceDirectory $phase.Reference
        $output = "Assets/Sprites/Concepts/StyleCalibration/world_warrior_pavilion_ace_makoto_walk_$($phase.Slug)_pose_v3.png"
        $metadata = "Artifacts/style_calibration_world_warrior_pavilion_ace_makoto_walk_$($phase.Slug)_pose_v3_job.json"
        if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
            Write-Host "SKIP $($phase.Slug)"
            continue
        }

        foreach ($required in @($poseReference, $identity, $failedWalk, $striker, $ryu, $goku)) {
            if (-not (Test-Path $required)) {
                throw "Missing generation reference: $required"
            }
        }

        $prompt = @"
Create ONE complete full-body WORLD WARRIOR PAVILION ACE MAKOTO walk pose on uniform pure chroma-green RGB 0,255,0.

REFERENCE ROLES ARE STRICT:
1. Reference 1 is the controlling body-pose authority. Copy its hip, knee, ankle, support-foot, swing-foot, torso, and opposing-arm arrangement exactly while replacing the mannequin identity.
2. Reference 2 is the exact Makoto identity authority. Preserve exactly $identityDescription. Keep her face, braid geometry, female proportions, asymmetrical costume, crescent token, palette, lighting, and right-facing three-quarter camera.
3. Reference 3 is a FAILED choreography sheet used only for Makoto rendering scale and material continuity. Do not copy its repeated phases, raised-knee guards, or same-leg lead.
4. Reference 4 supplies only approved Pavilion material grammar. Do not copy the Striker's face, hair, body, pose, or costume.
5 and 6 supply only canonical Project Mannequin rendering finish.

$($phase.Detail)

LEG OWNERSHIP IS ABSOLUTE. Keep camera-near physical leg A one clear cel-value band lighter than camera-far physical leg B from hip through shoe. Do not swap that shading at the knee. Preserve all costume asymmetry on its original body side; never mirror Makoto. Natural relaxed grounded walk with opposite arm swing. Exactly one figure, two arms, two legs, two feet, and all identity anchors. Complete figure centered with generous green clearance. Same runtime-ready scale and baseline as reference 3.

No idle, guard, march, run, jump, attack, knee strike, floor, shadow, scenery, text, labels, grid, border, motion effect, extra figure, extra limb, missing limb, merged legs, or crop. PROJECT MANNEQUIN STYLE LOCK: controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, arcade-readable fighting-game anatomy, and polished modern 2.5D anime finish. No photorealism, PBR, grit, painterly blur, flat vector art, or pixel art. 2K square image.
"@

        $arguments = @('generate', 'create', 'gpt_image_2', '--prompt', $prompt)
        foreach ($reference in @($poseReference, $identity, $failedWalk, $striker, $ryu, $goku)) {
            $arguments += @('--image', (Resolve-Path $reference).Path)
        }
        $arguments += @('--aspect_ratio', '1:1', '--resolution', '2k', '--quality', 'high', '--wait', '--wait-timeout', '20m', '--json')
        $raw = & $cli @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Higgsfield failed for Makoto $($phase.Slug)."
        }

        $result = ($raw -join "`n") | ConvertFrom-Json
        $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
        $url = @($result)[0].result_url
        if ([string]::IsNullOrWhiteSpace($url)) {
            throw "Higgsfield returned no result URL for Makoto $($phase.Slug)."
        }
        Invoke-WebRequest -Uri $url -OutFile $output
        Write-Host "SAVED $output"
    }
}
finally {
    Pop-Location
}