param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
$identity = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_prodigy_kenzo_style_pilot_v1.png'
$activePoseReference = 'Artifacts/StyleCalibration/world_warrior_kenzo_master_palm_active_pose_reference_v3.png'
$choreography = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_attack_startup_sheet_style_v1.png'
$rookie = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_rookie_style_pilot_v1.png'
$mannequin = 'Assets/Sprites/Mannequin/mannequin_intro_higgsfield_v1.png'
$ryu = 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png'
$goku = 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png'
$output = 'Assets/Sprites/Concepts/StyleCalibration/world_warrior_dojo_prodigy_kenzo_attack_startup_sheet_style_v3.png'
$metadata = 'Artifacts/style_calibration_world_warrior_dojo_prodigy_kenzo_attack_startup_sheet_v3_job.json'

$prompt = @'
PROJECT MANNEQUIN STYLE LOCK. The first supplied image is the exact approved WORLD WARRIOR DOJO PRODIGY KENZO identity. Preserve the same original fighter in every pose: lean athletic build with upright master's posture; swept-back black hair held by one bright saffron-gold champion circlet; layered ceremonial sleeveless deep-indigo vest with gold edge trim over a warm-ivory underwrap; one bronze champion medallion centered on the chest; a long vermilion champion half-sash hanging from the left hip; wide warm-ivory forearm guards; charcoal wide pleated hakama-style trousers gathered by a saffron cord; and low ivory split-sole shoes. Keep the same face, proportions, costume geometry, color hierarchy, camera, and light.

The second supplied image is the exact approved ACTIVE-POSE CAMERA AND SILHOUETTE TARGET. Every generated pose must retain this right-facing front three-quarter view with Kenzo's face, chest, bronze medallion, ivory underwrap, and front vest trim visible. Build poses 1-4 progressively toward it and reproduce its fully extended open lead hand as pose 5. Never rotate to a rear three-quarter or back-facing view.

The third supplied image is choreography only. Copy its five-frame open-palm timing and increasingly extended lead arm, but never copy the Rookie's identity. The fourth image is the approved Dojo Rookie for shared World Warrior material grammar only. The final three images are rendering-finish anchors only. Do not copy any reference face, hair, costume, or identity.

Produce exactly FIVE distinct full-body MASTER PALM startup/active poses arranged as three figures on the top row and two on the bottom row, ordered left-to-right then top-to-bottom:
1. Upright open-palm master guard with both shoes grounded.
2. Lead shoulder rotates and the lead hand rises; the lead palm is visibly open.
3. Front foot steps and hips turn while the open lead palm travels forward.
4. Lead arm reaches nearly full extension with a flat vertical open palm facing right.
5. Match the second reference: strongest active silhouette with the lead arm fully extended to the right, elbow nearly locked, wrist aligned, and one unmistakable OPEN PALM with all five fingers visible. The rear fist remains compact at the ribs and the bronze chest medallion remains fully visible.

Every pose must face exactly right in the same front three-quarter side camera. Every lead hand must remain open. Maintain one consistent scale and baseline with generous green separation around every complete figure. Uniform pure chroma-green RGB 0,255,0 background. No back-facing pose, rear three-quarter pose, hidden chest, hidden medallion, closed lead fist, punch, hook, finger curl into a fist, kick, weapon, target, impact effect, speed line, motion blur, recovery pose, duplicate pose, white karate gi, red headband, bare feet, tracksuit, military gear, armor, photorealism, PBR, painterly blur, flat vector art, pixel art, shadow, floor, scene, text, labels, grid lines, border, touching figures, extra limbs, missing half-sash, or crop. 2K square animation source.
'@

Push-Location $projectRoot
try {
    if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
        Write-Host 'SKIP attack_startup_v3'
        return
    }

    foreach ($required in @($identity, $activePoseReference, $choreography, $rookie, $mannequin, $ryu, $goku)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    Write-Host 'GENERATE attack_startup_v3'
    $raw = & $cli generate create nano_banana_pro `
        --prompt $prompt `
        --image $identity `
        --image $activePoseReference `
        --image $choreography `
        --image $rookie `
        --image $mannequin `
        --image $ryu `
        --image $goku `
        --aspect_ratio '1:1' `
        --resolution '2k' `
        --wait `
        --wait-timeout '20m' `
        --json
    if ($LASTEXITCODE -ne 0) {
        throw 'Higgsfield failed for attack_startup_v3.'
    }

    $result = ($raw -join "`n") | ConvertFrom-Json
    $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
    $url = @($result)[0].result_url
    if ([string]::IsNullOrWhiteSpace($url)) {
        throw 'Higgsfield returned no result URL for attack_startup_v3.'
    }

    Invoke-WebRequest -Uri $url -OutFile $output
    Write-Host "SAVED $output"
}
finally {
    Pop-Location
}