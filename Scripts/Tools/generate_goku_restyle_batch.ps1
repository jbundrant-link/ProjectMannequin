param(
    [int]$MaxParallel = 4
)

# Round 2 restyle: fixes (A) inconsistent form heights caused by big auras and
# (B) Kamehameha/ki energy baked onto the specials sprites. Auras become thin
# rims (so every form reads as the same size), and specials are generated with
# empty hands so the beam/orb comes only from the FX system. Base movement is
# intentionally left as-is (no aura, already correct); base specials ARE
# regenerated. Outputs overwrite goku_restyle_<form>_<kind>.png.

$ErrorActionPreference = "Stop"

$workspace = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
if (-not (Test-Path -LiteralPath $cli)) { $cli = "higgsfield" }

$spriteDir = Join-Path $workspace "Assets\Sprites\Goku"
$style = (Resolve-Path (Join-Path $workspace "Assets\Sprites\Mannequin\mannequin_master_higgsfield_v1.png")).Path

# key, core, posture, specials source, identity, regenMovement flag
$forms = @(
    @{ key = "base";         core = "goku_core_actions_higgsfield_v1.png";      posture = "goku_posture_normals_higgsfield_v1.png";     specials = "goku_base_specials_higgsfield_v2.png";        identity = "tall spiky black hair, wearing an orange martial-arts gi with blue undershirt and belt, and no aura";                                                                            movement = $false },
    @{ key = "kaioken";      core = "goku_kaioken_core_higgsfield_v1.png";      posture = "goku_kaioken_posture_higgsfield_v1.png";      specials = "goku_kaioken_specials_higgsfield_v1.png";      identity = "tall spiky black hair, wearing an orange martial-arts gi with blue accents, with a thin red Kaioken aura rim hugging the body";                                                  movement = $true },
    @{ key = "false_super";  core = "goku_false_super_core_higgsfield_v1.png";  posture = "goku_false_super_posture_higgsfield_v1.png";  specials = "goku_false_super_specials_higgsfield_v1.png";  identity = "spiky rough dull golden-blond hair, wearing an orange martial-arts gi with blue accents, with a faint thin pale-gold aura rim";                                                   movement = $true },
    @{ key = "ss1";          core = "goku_ss1_core_higgsfield_v2.png";          posture = "goku_ss1_posture_higgsfield_v1.png";          specials = "goku_ss1_specials_higgsfield_v1.png";          identity = "tall upright spiky bright golden-blond hair and teal-green eyes, wearing an orange martial-arts gi with blue accents, with a thin golden aura rim hugging the body";              movement = $true },
    @{ key = "ss2";          core = "goku_ss2_core_higgsfield_v1.png";          posture = "goku_ss2_posture_higgsfield_v1.png";          specials = "goku_ss2_specials_higgsfield_v1.png";          identity = "rigid sharp spiky golden-blond hair and teal-green eyes, wearing an orange martial-arts gi with blue accents, with a thin golden aura rim and small blue electric sparks close to the body"; movement = $true },
    @{ key = "ss3";          core = "goku_ss3_core_higgsfield_v1.png";          posture = "goku_ss3_posture_higgsfield_v1.png";          specials = "goku_ss3_specials_higgsfield_v1.png";          identity = "very long golden-blond hair flowing past the waist with no eyebrows, wearing an orange martial-arts gi with blue accents, with a thin golden aura rim hugging the body";          movement = $true },
    @{ key = "ss4";          core = "goku_ss4_core_higgsfield_v1.png";          posture = "goku_ss4_posture_higgsfield_v1.png";          specials = "goku_ss4_specials_higgsfield_v1.png";          identity = "wild long black hair, red-brown fur across the torso and forearms, bare-chested with a torn dark-blue waist sash and pants, with a thin red aura rim";                            movement = $true },
    @{ key = "god";          core = "goku_god_core_higgsfield_v1.png";          posture = "goku_god_posture_higgsfield_v1.png";          specials = "goku_god_specials_higgsfield_v1.png";          identity = "spiky vivid red-pink hair and red eyes, wearing a snug dark-blue and orange divine gi, with a thin crimson aura rim hugging the body";                                            movement = $true },
    @{ key = "blue";         core = "goku_blue_core_higgsfield_v1.png";         posture = "goku_blue_posture_higgsfield_v1.png";         specials = "goku_blue_specials_higgsfield_v2.png";         identity = "tall spiky bright cyan-blue hair and blue eyes, wearing an orange martial-arts gi with blue accents, with a thin sky-blue aura rim hugging the body";                             movement = $true },
    @{ key = "blue_kaioken"; core = "goku_blue_kaioken_core_higgsfield_v1.png"; posture = "goku_blue_kaioken_posture_higgsfield_v1.png"; specials = "goku_blue_kaioken_specials_higgsfield_v1.png"; identity = "tall spiky bright cyan-blue hair, wearing an orange martial-arts gi with blue accents, with a thin blue aura rim tinted red at the edges, hugging the body";                       movement = $true },
    @{ key = "ui_sign";      core = "goku_ui_sign_core_higgsfield_v1.png";      posture = "goku_ui_sign_posture_higgsfield_v1.png";      specials = "goku_ui_sign_specials_higgsfield_v1.png";      identity = "spiky black hair with faint silver highlights and sharp silver-grey eyes, wearing an orange martial-arts gi with blue accents, with a thin silver-blue aura rim";                movement = $true },
    @{ key = "instinct";     core = "goku_instinct_core_higgsfield_v1.png";     posture = "goku_instinct_posture_higgsfield_v1.png";     specials = "goku_instinct_specials_higgsfield_v2.png";     identity = "spiky silver-white hair and calm silver eyes, wearing a torn orange martial-arts gi with blue accents, with a thin silver-white aura rim";                                        movement = $true }
)

function New-Prompt($identity, $rows, $isSpecials) {
    $base = "The first image is the exact choreography to keep. The character is Dragon Ball Z Goku with $identity. Redraw every pose at high fidelity while preserving the identical grid of 8 columns and $rows rows, the identical pose inside each cell, and the identical left-to-right top-to-bottom order. Keep this exact hair color, eye color, and outfit. Draw the character at the SAME consistent body size in every cell: identical head-to-heel height, both feet resting on a common ground baseline, filling a consistent portion of each cell so no pose looks larger or smaller than the others. Match ONLY the rendering finish of the second image: bold clean black outlines, smooth cel-shaded coloring with crisp highlight and shadow banding, polished modern 2.5D fighting-game sprite look. Draw each figure large and centered inside its own cell, with a clearly rendered face (eyes, eyebrows, mouth) and complete correctly connected limbs. Render any energy aura ONLY as a thin bright rim that hugs the body outline a few pixels beyond the silhouette; do NOT draw large flames, wide energy plumes, tall aura columns, or a big glowing envelope around the body. Use a uniform pure green #00FF00 background, keep clear empty margins between every figure, and include no motion-blur streaks, no duplicated or extra limbs, no extra figures, no text or labels."
    if ($isSpecials) {
        return $base + " The character's hands are completely EMPTY in every pose: do NOT draw any ki orb, energy ball, glowing sphere, Kamehameha beam, Spirit Bomb, blast, shockwave, or projectile in the hands or extending from the body, even in charging, cupped-hand, or firing poses. Show only the bare character body performing the motion."
    }
    return $base + " Do not draw any projectiles or detached energy anywhere."
}

# Build per-sheet specs: movement (core+posture) only when requested, specials always.
$specs = @()
foreach ($form in $forms) {
    if ($form.movement) {
        $specs += @{ name = "$($form.key)_core";    src = $form.core;    out = "goku_restyle_$($form.key)_core.png";    aspect = "3:4"; prompt = (New-Prompt $form.identity "9" $false) }
        $specs += @{ name = "$($form.key)_posture"; src = $form.posture; out = "goku_restyle_$($form.key)_posture.png"; aspect = "3:4"; prompt = (New-Prompt $form.identity "9" $false) }
    }
    $specs += @{ name = "$($form.key)_specials"; src = $form.specials; out = "goku_restyle_$($form.key)_specials.png"; aspect = "1:1"; prompt = (New-Prompt $form.identity "8" $true) }
}

$jobBlock = {
    param($cli, $prompt, $srcPath, $stylePath, $outPath, $aspect, $name)
    if (Test-Path -LiteralPath $outPath) { return "SKIP $name (exists)" }
    for ($attempt = 1; $attempt -le 6; $attempt++) {
        try {
            # Redirect stderr to null so transient CLI errors (e.g. HTTP 503)
            # surface only through a non-zero exit code and are retried here,
            # instead of leaking error records that would halt the parent.
            $raw = & $cli generate create gpt_image_2 `
                --prompt $prompt `
                --image $srcPath `
                --image $stylePath `
                --aspect_ratio $aspect `
                --resolution "2k" `
                --quality "high" `
                --wait `
                --wait-timeout "20m" `
                --json 2>$null
            if ($LASTEXITCODE -ne 0) { throw "exit $LASTEXITCODE" }
            $jobs = ($raw -join "`n") | ConvertFrom-Json
            $url = @($jobs)[0].result_url
            if ([string]::IsNullOrWhiteSpace($url)) { throw "no result url" }
            Invoke-WebRequest -Uri $url -OutFile $outPath
            return "OK   $name"
        }
        catch {
            if ($attempt -ge 6) { return "FAIL $name : $($_.Exception.Message)" }
            Start-Sleep -Seconds (8 * $attempt)
        }
    }
}

Write-Host "Dispatching $($specs.Count) restyle jobs (throttle $MaxParallel)..."
$running = @()
$results = @()
foreach ($spec in $specs) {
    while (@($running | Where-Object { $_.State -eq "Running" }).Count -ge $MaxParallel) {
        Start-Sleep -Milliseconds 500
        foreach ($done in @($running | Where-Object { $_.State -ne "Running" })) {
            $results += Receive-Job -Job $done -ErrorAction SilentlyContinue
            Remove-Job -Job $done
            $running = @($running | Where-Object { $_.Id -ne $done.Id })
        }
    }
    $srcPath = (Resolve-Path (Join-Path $spriteDir $spec.src)).Path
    $outPath = Join-Path $spriteDir $spec.out
    $running += Start-Job -ScriptBlock $jobBlock -ArgumentList $cli, $spec.prompt, $srcPath, $style, $outPath, $spec.aspect, $spec.name
}

while (@($running | Where-Object { $_.State -eq "Running" }).Count -gt 0) {
    Start-Sleep -Milliseconds 500
}
foreach ($done in $running) {
    $results += Receive-Job -Job $done -ErrorAction SilentlyContinue
    Remove-Job -Job $done
}

Write-Host "---- RESULTS ----"
$results | Sort-Object | ForEach-Object { Write-Host $_ }
$failed = @($results | Where-Object { $_ -like "FAIL*" })
Write-Host "Completed with $($failed.Count) failure(s)."
