param(
    [int]$MaxParallel = 5
)

$ErrorActionPreference = "Stop"

$workspace = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$cli = Join-Path $env:LOCALAPPDATA "Programs\Higgsfield\higgsfield.exe"
if (-not (Test-Path -LiteralPath $cli)) {
    $cli = "higgsfield"
}

$styleReference = Join-Path $workspace "Assets\Sprites\Ryu\Higgsfield\V5References\ryu_v4_design_anchor.png"
$auditRoot = Join-Path $workspace "Assets\Sprites\Ryu\MugenAudit"
$outputRoot = Join-Path $workspace "Assets\Sprites\Ryu\Higgsfield\V5Generated"
New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null

$specs = @(
    @{
        Name = "shoulder_throw_action_810"
        Action = 810
        Count = 16
        Reference = "sequence"
        Description = "a close grab flowing into a shoulder throw and recovery"
    },
    @{
        Name = "back_throw_action_820_v2"
        Action = 820
        Count = 12
        Reference = "sequence"
        Description = "a close grab flowing into a backward sacrifice throw and recovery"
    },
    @{
        Name = "collarbone_breaker_action_900"
        Action = 900
        Count = 11
        Reference = "unique"
        Description = "a forward-stepping overhead double-fist strike and recovery"
    },
    @{
        Name = "solar_plexus_action_910"
        Action = 910
        Count = 14
        Reference = "unique"
        Description = "a forward-stepping two-hit body punch and recovery"
    },
    @{
        Name = "shoryuken_action_1120_v2"
        Action = 1120
        Count = 11
        Reference = "unique"
        Description = "a heavy rising uppercut from crouched startup through airborne ascent"
    },
    @{
        Name = "tatsumaki_action_1220"
        Action = 1220
        Count = 13
        Reference = "unique"
        Description = "a ground spinning hurricane kick pose library from startup through landing"
    },
    @{
        Name = "air_tatsumaki_action_1320"
        Action = 1320
        Count = 15
        Reference = "unique"
        Description = "an airborne spinning hurricane kick pose library through descent"
    },
    @{
        Name = "joudan_action_1420"
        Action = 1420
        Count = 14
        Reference = "unique"
        Description = "a forward-driving high blade side kick and recovery"
    },
    @{
        Name = "shin_shoryuken_start_action_3000_v2"
        Action = 3000
        Count = 13
        Reference = "unique"
        Description = "the opening rising strike sequence of a cinematic triple uppercut"
    },
    @{
        Name = "shin_shoryuken_finish_action_3010"
        Action = 3010
        Count = 9
        Reference = "unique"
        Description = "the close-range finishing uppercut and airborne recovery"
    },
    @{
        Name = "shinku_hadouken_action_3100_a"
        Action = 3100
        Count = 12
        Reference = "chunk_a"
        ReferencePath = "Assets\Sprites\Ryu\Higgsfield\V5References\action_3100_shinku_hadouken_chunk_a.png"
        Description = "the first half of a long super projectile charge and release"
    },
    @{
        Name = "shinku_hadouken_action_3100_b"
        Action = 3100
        Count = 12
        Reference = "chunk_b"
        ReferencePath = "Assets\Sprites\Ryu\Higgsfield\V5References\action_3100_shinku_hadouken_chunk_b.png"
        Description = "the second half of a long super projectile release and recovery"
    }
)

function Get-ReferencePath($spec) {
    if ($spec.ContainsKey("ReferencePath")) {
        return Join-Path $workspace $spec.ReferencePath
    }
    $actionName = switch ($spec.Action) {
        810 { "shoulder_throw" }
        820 { "back_throw" }
        900 { "collarbone_breaker" }
        910 { "solar_plexus_strike" }
        1120 { "heavy_shoryuken" }
        1220 { "heavy_tatsumaki" }
        1320 { "air_heavy_tatsumaki" }
        1420 { "heavy_joudan" }
        3000 { "shin_shoryuken_start" }
        3010 { "shin_shoryuken_finish" }
        3100 { "shinku_hadouken" }
    }
    $folder = if ($spec.Reference -eq "unique") {
        "UniquePoseReferences"
    } else {
        "ActionReferences"
    }
    return Join-Path $auditRoot "$folder\action_$($spec.Action)_$($actionName)_$($spec.Reference).png"
}

function Start-GenerationJob($spec) {
    $referencePath = Get-ReferencePath $spec
    if (-not (Test-Path -LiteralPath $referencePath)) {
        throw "Missing pose reference: $referencePath"
    }

    $outputPath = Join-Path $outputRoot "$($spec.Name).png"
    if (Test-Path -LiteralPath $outputPath) {
        Write-Host "Skipping existing $($spec.Name)"
        return $null
    }

    $columns = if ($spec.Count -le 16) {
        [Math]::Ceiling($spec.Count / 2.0)
    } else {
        8
    }
    $rows = [Math]::Ceiling($spec.Count / [double]$columns)
    $prompt = "Use the first reference as the sole choreography and the second reference only as the character design. Restyle exactly $($spec.Count) complete full-body poses showing $($spec.Description), preserving the source order and arranging them in exactly $columns columns by $rows rows. Keep one consistent body scale and baseline. White sleeveless gi, black belt, red gloves and red headband, muscular anatomy, clean modern 2.5D fighting-game sprite illustration. Uniform pure #00FF00 background, clear space between figures, complete limbs, no labels, projectiles, impact effects, or extra figures."

    return Start-Job -ArgumentList @(
        $cli,
        $prompt,
        $referencePath,
        $styleReference,
        $outputPath,
        $spec.Name
    ) -ScriptBlock {
        param($cliPath, $promptText, $posePath, $stylePath, $destination, $name)
        $raw = & $cliPath generate create gpt_image_2 `
            --prompt $promptText `
            --image $posePath `
            --image $stylePath `
            --aspect_ratio "16:9" `
            --resolution "2k" `
            --quality "high" `
            --wait `
            --wait-timeout "20m" `
            --json
        if ($LASTEXITCODE -ne 0) {
            throw "$name generation failed: $raw"
        }
        $result = ($raw -join "`n") | ConvertFrom-Json
        $url = $result[0].result_url
        if ([string]::IsNullOrWhiteSpace($url)) {
            throw "$name returned no result URL"
        }
        Invoke-WebRequest -Uri $url -OutFile $destination
        [PSCustomObject]@{
            Name = $name
            Url = $url
            Output = $destination
        }
    }
}

$manifest = @()
for ($start = 0; $start -lt $specs.Count; $start += $MaxParallel) {
    $end = [Math]::Min($start + $MaxParallel - 1, $specs.Count - 1)
    $batch = @()
    for ($index = $start; $index -le $end; $index++) {
        $job = Start-GenerationJob $specs[$index]
        if ($null -ne $job) {
            $batch += $job
        }
    }

    if ($batch.Count -eq 0) {
        continue
    }

    Write-Host "Waiting for batch of $($batch.Count) generations..."
    $batch | Wait-Job | Out-Null
    foreach ($job in $batch) {
        try {
            $manifest += Receive-Job -Job $job -ErrorAction Stop
        }
        finally {
            Remove-Job -Job $job -Force
        }
    }
}

$manifestPath = Join-Path $outputRoot "generation_manifest.json"
$manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $manifestPath -Encoding UTF8
Write-Host "Saved $manifestPath"
