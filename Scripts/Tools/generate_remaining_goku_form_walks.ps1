param(
    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$generator = Join-Path $PSScriptRoot 'generate_goku_form_walk.ps1'
$forms = @(
    [pscustomobject]@{ Key = 'false_super'; Name = 'Goku False Super Saiyan'; Identity = 'Goku with rough dull golden-blond spiky hair, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, focused anime face, and a faint thin pale-gold aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'ss1'; Name = 'Goku Super Saiyan'; Identity = 'Goku with tall upright bright golden-blond spiky hair, teal-green eyes, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, and a thin golden aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'ss2'; Name = 'Goku Super Saiyan 2'; Identity = 'Goku with rigid sharp golden-blond spiky hair, teal-green eyes, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, a thin golden aura rim, and tiny close blue sparks that never detach from the silhouette' },
    [pscustomobject]@{ Key = 'ss3'; Name = 'Goku Super Saiyan 3'; Identity = 'Goku with extremely long golden-blond hair flowing below the waist, no eyebrows, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, and a thin golden aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'ss4'; Name = 'Goku Super Saiyan 4'; Identity = 'Goku with wild long black hair, red-brown fur across torso and forearms, bare chest, torn dark-blue waist sash and pants, yellow eyes, and a thin red aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'god'; Name = 'Goku Super Saiyan God'; Identity = 'slender divine Goku with vivid red-pink spiky hair and red eyes, dark-blue undershirt beneath orange gi, blue belt, wristbands and boots, and a thin crimson aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'blue'; Name = 'Goku Super Saiyan Blue'; Identity = 'Goku with tall bright cyan-blue spiky hair and blue eyes, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, and a thin sky-blue aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'blue_kaioken'; Name = 'Goku Blue Kaioken'; Identity = 'Goku with tall bright cyan-blue spiky hair, blue eyes, orange sleeveless gi, dark-blue undershirt, belt, wristbands and boots, and a thin blue aura rim edged in restrained red hugging the silhouette' },
    [pscustomobject]@{ Key = 'ui_sign'; Name = 'Goku Ultra Instinct Sign'; Identity = 'Goku with black spiky hair carrying faint silver highlights, sharp silver-grey eyes, orange gi with dark-blue undershirt, belt, wristbands and boots, and a thin silver-blue aura rim hugging the silhouette' },
    [pscustomobject]@{ Key = 'instinct'; Name = 'Goku Mastered Ultra Instinct'; Identity = 'Goku with silver-white spiky hair, calm silver eyes, torn orange gi with dark-blue accents, muscular body, and a thin silver-white aura rim hugging the silhouette' }
)

foreach ($form in $forms) {
    $arguments = @{
        FormName = $form.Name
        IdentityPath = "Artifacts/StyleCalibration/GokuWalk/goku_$($form.Key)_identity.png"
        IdentityDescription = $form.Identity
        OutputPath = "Assets/Sprites/Concepts/StyleCalibration/goku_$($form.Key)_walk_sheet_v1.png"
        MetadataPath = "Artifacts/style_calibration_goku_$($form.Key)_walk_sheet_v1_job.json"
    }
    if ($SkipExisting) {
        $arguments.SkipExisting = $true
    }
    & $generator @arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to generate $($form.Name) walk."
    }
}