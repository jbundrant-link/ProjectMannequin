param(
    [Parameter(Mandatory = $true)]
    [string]$Name,

    [Parameter(Mandatory = $true)]
    [string]$IdentityPath,

    [Parameter(Mandatory = $true)]
    [string]$FirstHalfPath,

    [Parameter(Mandatory = $true)]
    [string]$PoseReferenceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputDirectory,

    [Parameter(Mandatory = $true)]
    [string]$MetadataDirectory,

    [Parameter(Mandatory = $true)]
    [string]$FilePrefix,

    [Parameter(Mandatory = $true)]
    [string]$IdentityDescription,

    [ValidateSet('nano_banana_pro', 'gpt_image_2')]
    [string]$Model = 'gpt_image_2',

    [ValidateSet('all', 'b_contact', 'b_down', 'b_passing', 'b_up')]
    [string]$OnlyPhase = 'all',

    [switch]$SkipExisting
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cli = Join-Path $env:LOCALAPPDATA 'Programs\Higgsfield\higgsfield.exe'
$phases = @(
    [pscustomobject]@{
        Frame = 5
        Slug = 'b_contact'
        Detail = 'B-CONTACT: the dark-shaded B trouser leg emerges in front at the crotch and reaches farthest toward screen-right with heel contact; A trails behind on its toe. Reverse both arms relative to the A-contact reference.'
    },
    [pscustomobject]@{
        Frame = 6
        Slug = 'b_down'
        Detail = 'B-DOWN: the dark-shaded B foot is planted slightly ahead and carries the lowered body; A lifts from behind. Keep a visible bent support knee and reverse both arms relative to A-down.'
    },
    [pscustomobject]@{
        Frame = 7
        Slug = 'b_passing'
        Detail = 'B-PASSING: the dark-shaded B leg is planted vertically under the hips while bent A passes forward with visible toe clearance. This is a compact passing pose, not a high-knee march.'
    },
    [pscustomobject]@{
        Frame = 8
        Slug = 'b_up'
        Detail = 'B-UP: the dark-shaded B support leg rises onto its toe just behind the hips while A advances toward the next heel contact. Keep A airborne and make this flow naturally into A-contact.'
    }
)

Push-Location $projectRoot
try {
    if (-not (Test-Path $cli)) {
        throw "Higgsfield CLI not found: $cli"
    }
    foreach ($required in @($IdentityPath, $FirstHalfPath)) {
        if (-not (Test-Path $required)) {
            throw "Missing generation reference: $required"
        }
    }

    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    New-Item -ItemType Directory -Force -Path $MetadataDirectory | Out-Null

    foreach ($phase in $phases) {
        if ($OnlyPhase -ne 'all' -and $phase.Slug -ne $OnlyPhase) {
            continue
        }
        $poseReference = Join-Path $PoseReferenceDirectory "mannequin_b_phase_$($phase.Frame).png"
        $output = Join-Path $OutputDirectory "$FilePrefix`_$($phase.Slug).png"
        $metadata = Join-Path $MetadataDirectory "$FilePrefix`_$($phase.Slug)_job.json"
        if ($SkipExisting -and (Test-Path $output) -and (Test-Path $metadata)) {
            Write-Host "SKIP $($phase.Slug)"
            continue
        }
        if (-not (Test-Path $poseReference)) {
            throw "Missing pose reference: $poseReference"
        }

        $prompt = @"
Create ONE complete full-body $Name walk pose on a uniform pure chroma-green RGB 0,255,0 background.

Reference 1 is the exact controlling body pose and has highest priority. Copy its joint arrangement, leg crossing, support foot, swing foot, torso lean, and opposing arm relationship exactly, while replacing the mannequin identity and materials. Reference 2 is the exact character identity authority. Preserve exactly: $IdentityDescription. Reference 3 is the accepted first half-cycle and supplies only character scale, screen-right three-quarter camera, rendering, lighting, and baseline. References 4 and 5 supply only canonical Project Mannequin rendering finish.

$($phase.Detail)

The B leg must remain visibly 15 percent darker than A across the trouser folds from crotch to shoe so physical limb ownership remains readable. Preserve asymmetric costume features on their original body side; do not mirror the character, face, sash, or costume. Natural grounded walk only. Exactly one figure, two arms, two legs, two feet, and all identity features. Complete figure centered with generous green clearance. No floor, shadow, scenery, text, labels, grid, border, motion effects, extra figure, extra limb, missing limb, merged legs, crop, idle, run, jump, attack, guard, or knee strike.

PROJECT MANNEQUIN STYLE LOCK: controlled dark contours, broad two-to-four-step cel shading, clean graphic highlights, fighting-game anatomy, polished modern 2.5D anime finish. No photorealism, PBR, grit, microtexture, painterly blur, flat vector art, or pixel art. 2K square image.
"@

        $references = @(
            (Resolve-Path $poseReference).Path,
            (Resolve-Path $IdentityPath).Path,
            (Resolve-Path $FirstHalfPath).Path,
            (Resolve-Path 'Assets/Sprites/Ryu/ryu_intro_higgsfield_v1.png').Path,
            (Resolve-Path 'Assets/Sprites/Goku/goku_intro_higgsfield_v1.png').Path
        )
        $arguments = @('generate', 'create', $Model, '--prompt', $prompt)
        foreach ($reference in $references) {
            $arguments += @('--image', $reference)
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
            throw "Higgsfield failed to generate $($phase.Slug)."
        }
        $result = ($raw -join "`n") | ConvertFrom-Json
        $result | ConvertTo-Json -Depth 10 | Set-Content $metadata -Encoding utf8
        $url = @($result)[0].result_url
        if ([string]::IsNullOrWhiteSpace($url)) {
            throw "Higgsfield returned no result URL for $($phase.Slug)."
        }
        Invoke-WebRequest -Uri $url -OutFile $output
        Write-Host "SAVED $output"
    }
}
finally {
    Pop-Location
}