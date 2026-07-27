param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_style_pilot_v1.png'
$activePoseReference = 'Artifacts/StyleCalibration/world_warrior_grappler_shoulder_drive_active_pose_reference_v2a.png'
$startupOutput = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_attack_startup_sheet_style_v2.png'
$recoveryOutput = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_grappler_attack_recovery_sheet_style_v2.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$striker = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_striker_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'

$identityLock = @'
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR TOURNAMENT GRAPPLER identity. Preserve the same original massive heavyweight in every pose: broad trapezoid torso, thick neck, huge forearms and thighs, short dense black curls with one silver forelock, clean-shaven square jaw, sleeveless deep-plum wrap tunic with one indigo diagonal inner panel, wide woven saffron belt with compact side knot, loose brick-red knee-length wrestling trousers with indigo gussets, charcoal shin wraps, low warm-ivory soft wrestling shoes, dark-indigo wrist wraps, and one bronze-rimmed vermilion lacquer token centered on the belt. Keep the same face, heavyweight proportions, costume geometry, color hierarchy, side-on camera, and light. The third and fourth images are World Warrior family-material references only. The final three images are rendering-finish references only: confident dark contours, broad two-to-four-band cel shading, simplified cloth materials, designed highlights, stylized dimensional forms, saturated controlled accents, arcade-readable anatomy, and polished anime fighting-game presentation. Do not copy any reference character's face, hair, costume, or identity. High fidelity comes from anatomy, composition, lighting, and finish, not PBR texture noise.

Every figure must be complete, face right, and remain separated on uniform pure chroma-green RGB 0,255,0. No olive drab, camouflage, tactical vest, suspenders, cargo trousers, utility pockets, knee pads, combat boots, military gear, armor, wrestling singlet, championship text, photorealism, PBR, gritty military sci-fi, painterly blur, flat vector art, pixel art, weapon, aura, impact effect, target, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing belt or token, duplicate pose, or crop.
'@

$startupPrompt = @'
The second image is the exact approved active-pose choreography target. Its low forward body angle, planted rear-leg extension, deep front knee, shoulder-first silhouette, open body-lock arms, and strict right-facing three-quarter side view are mandatory. Build poses 1-4 progressively toward it and reproduce that active silhouette as pose 5. Never turn the character's back toward the camera.

Produce exactly FIVE distinct full-body SHOULDER DRIVE startup/active poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom. This is one continuous wrestling body-lock entry, not five guards:
1. Low open-hand grappling guard, feet wide, knees bent.
2. Clear level change: hips and head drop, lead foot steps, both palms turn inward.
3. Penetration step: torso pitches forward, lead shoulder passes ahead of hips, arms open in a wide C-shape around an imaginary waist.
4. Launch: remain in the same right-facing three-quarter side view; front knee bends near 90 degrees, rear leg lengthens and pushes hard, torso stays below 45 degrees, head stays outside beside the imaginary torso, and both open arms curve inward. This must be one step before pose 5, not a run, turn, or back view.
5. Match the second reference's strongest active silhouette: a very low, long shoulder-first drive to the right. Torso is nearly horizontal, lead shoulder visibly far ahead of hips, front knee deeply bent, rear leg nearly straight and fully driving, both arms visibly curved inward as an unclasped body lock with palms facing each other. The head is safely beside the imaginary opponent's torso, never centered in front.

The fifth silhouette must read instantly as a committed double-leg/body-lock entry at gameplay size. Poses 3-5 must all face exactly right with both hands open and no sudden camera rotation. Maintain one consistent character scale and generous green margin around every complete figure. No upright reach, standing punch, closed striking fist, back-facing pose, running arm swing, kick, target figure, throw victim, recovery pose, contact, dust, speed line, or motion blur. 2K square animation source.
'@

$recoveryPrompt = @'
The second image is the exact approved active-pose choreography target. Reproduce that low right-facing shoulder-first body-lock silhouette as pose 1, then unwind its momentum gradually without changing camera angle or inventing a new strike.

Produce exactly FIVE distinct full-body SHOULDER DRIVE recovery poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom:
1. Match the second reference: very low forward torso below 35 degrees, lead shoulder ahead of hips, rear leg extended, arms still curved inward around an imaginary waist.
2. Momentum settles but remains low: rear foot begins stepping underneath, torso remains below 45 degrees, hips stay low, elbows open and palms separate.
3. Release: torso rises only halfway from the level change, head comes up, feet remain staggered and wide, both open hands remain forward.
4. Reset: shoulders square, hands return to open grappling guard, knees stay bent.
5. Neutral restored: the original low forward-pitched guard with both shoes planted and belt token centered.

The first three poses must visibly preserve and then dissipate the low forward drive; poses 1-2 cannot be generic guards, and pose 3 cannot be fully upright. Every pose remains in the same right-facing three-quarter side view. Maintain one consistent character scale and generous green margin around every complete figure. No back-facing pose, running arm swing, new attack, punch, target, throw victim, impact, dust, speed line, or effect. 2K square animation source.
'@

function Invoke-GrapplerSheet {
    param(
        [string]$Name,
        [string]$Choreography,
        [string]$Prompt,
        [string]$Output,
        [string]$Metadata
    )

    if ($SkipExisting -and (Test-Path $Output) -and (Test-Path $Metadata)) {
        Write-Host "SKIP $Name"
        return
    }

    foreach ($required in @($identity, $Choreography, $rookie, $striker, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host "GENERATE $Name"
    $fullPrompt = $identityLock + "`n" + $Prompt
    $raw = & $cli generate create nano_banana_pro `
        --prompt $fullPrompt `
        --image $identity `
        --image $Choreography `
        --image $rookie `
        --image $striker `
        --image $mannequin `
        --image $ryu `
        --image $goku `
        --aspect_ratio '1:1' `
        --resolution '2k' `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw "Higgsfield failed for $Name."
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $Metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw "Higgsfield returned no result URL for $Name."
    }

    Invoke-WebRequest -Uri $url -OutFile $Output
    Write-Host "SAVED $Output"
}

Push-Location $projectRoot
try {
    Invoke-GrapplerSheet `
        -Name 'attack_startup_v2' `
        -Choreography $activePoseReference `
        -Prompt $startupPrompt `
        -Output $startupOutput `
        -Metadata 'Artifacts/style_calibration_world_warrior_grappler_attack_startup_sheet_v2_job.json'

    Invoke-GrapplerSheet `
        -Name 'attack_recovery_v2' `
        -Choreography $activePoseReference `
        -Prompt $recoveryPrompt `
        -Output $recoveryOutput `
        -Metadata 'Artifacts/style_calibration_world_warrior_grappler_attack_recovery_sheet_v2_job.json'
}
finally {
    Pop-Location
}