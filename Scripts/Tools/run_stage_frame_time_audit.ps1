<#
.SYNOPSIS
    Measures rendered frame cost per stage against the 16.7 ms budget.

.DESCRIPTION
    Closes item 7 of the stage rendering pass. The pass added a per-actor
    contact shadow, a per-layer depth tint, per-stage key lighting, and
    atmospheric depth fog - all of which cost real GPU time on every frame, so
    the pass is not finished until that cost is known rather than assumed.

    Deliberately does NOT pass --headless. The headless display driver does no
    rasterisation, so it would report the harness rather than the renderer.
    The probe also disables vsync; with it on, a stage costing 3 ms and one
    costing 15 ms both report the refresh interval and a real overrun stays
    invisible until it is far too late.

    Numbers are machine-specific. Compare runs on the same machine; treat the
    absolute value as a budget check, not a portable benchmark.
#>
[CmdletBinding()]
param(
    [string]$Resolution = '1280x720',
    [string]$ReportPath = 'Artifacts/stage_frame_time_report.json'
)

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)

$godot = $env:GODOT4
if (-not $godot -or -not (Test-Path $godot)) {
    Write-Error 'GODOT4 is not set to a valid executable.'
    exit 2
}

# A stale capture variable can redirect output over a source asset, so the
# environment is cleared rather than trusted.
Get-ChildItem Env: | Where-Object { $_.Name -like 'PROJECT_MANNEQUIN_*' } |
    ForEach-Object { Remove-Item "Env:$($_.Name)" -ErrorAction SilentlyContinue }

$stages = @(
    @{ World = 'world_warrior_sector'; Index = 1 },
    @{ World = 'world_warrior_sector'; Index = 2 },
    @{ World = 'world_warrior_sector'; Index = 3 },
    @{ World = 'world_warrior_sector'; Index = 4 },
    @{ World = 'world_archive_nexus';  Index = 1 },
    @{ World = 'world_archive_nexus';  Index = 2 },
    @{ World = 'world_archive_nexus';  Index = 3 },
    @{ World = 'world_archive_nexus';  Index = 4 },
    @{ World = 'world_astral_battlefront'; Index = 1 },
    @{ World = 'world_astral_battlefront'; Index = 3 }
)

$results = New-Object System.Collections.ArrayList
$budget = 16.7

foreach ($stage in $stages) {
    $env:PROJECT_MANNEQUIN_DISABLE_PROGRESS_SAVE = '1'
    $env:PROJECT_MANNEQUIN_FRAME_TIME_PROBE = '1'
    $env:PROJECT_MANNEQUIN_WORLD_ID = $stage.World
    $env:PROJECT_MANNEQUIN_STAGE_INDEX = "$($stage.Index)"

    $output = & $godot --path $projectRoot --audio-driver Dummy `
        --resolution $Resolution 'res://Scenes/Main.tscn' 2>&1 |
        Select-String -Pattern '\[FrameTime\]'

    if (-not $output) {
        Write-Host ("{0,-26} stage {1}  NO SAMPLE" -f $stage.World, $stage.Index)
        continue
    }

    $line = $output[0].ToString()
    $fields = @{}
    foreach ($match in [regex]::Matches($line, '(\w+)=([\-\d\.]+)')) {
        $fields[$match.Groups[1].Value] = [double]$match.Groups[2].Value
    }
    if (-not $fields.ContainsKey('p95')) {
        Write-Host ("{0,-26} stage {1}  UNPARSED: {2}" -f $stage.World, $stage.Index, $line)
        continue
    }

    [void]$results.Add([PSCustomObject]@{
        World         = $stage.World
        Stage         = $stage.Index
        MeanMs        = $fields['mean']
        P50Ms         = $fields['p50']
        P95Ms         = $fields['p95']
        P99Ms         = $fields['p99']
        MaxMs         = $fields['max']
        OverBudgetPct = $fields['overBudgetPct']
        DrawCalls     = $fields['drawCalls']
    })
}

Remove-Item Env:PROJECT_MANNEQUIN_FRAME_TIME_PROBE -ErrorAction SilentlyContinue

if ($results.Count -eq 0) {
    Write-Error 'No stage produced a frame time sample.'
    exit 1
}

Write-Host ''
Write-Host ('{0,-26} {1,5} {2,8} {3,8} {4,8} {5,8} {6,9} {7,7}' -f `
    'world', 'stage', 'mean', 'p50', 'p95', 'max', 'over%', 'draws')
foreach ($row in $results) {
    Write-Host ('{0,-26} {1,5} {2,8:F2} {3,8:F2} {4,8:F2} {5,8:F2} {6,9:F2} {7,7:F0}' -f `
        $row.World, $row.Stage, $row.MeanMs, $row.P50Ms, $row.P95Ms, $row.MaxMs,
        $row.OverBudgetPct, $row.DrawCalls)
}

$results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $ReportPath -Encoding UTF8
Write-Host ''
Write-Host "report=$ReportPath"

# p95 is the gate rather than the mean: an average inside budget still stutters
# if one frame in twenty misses, and stutter is what a player actually feels.
$failed = @($results | Where-Object { $_.P95Ms -gt $budget })
if ($failed.Count -gt 0) {
    Write-Host ''
    Write-Host "FAIL $($failed.Count) stage(s) exceed the $budget ms budget at p95:" -ForegroundColor Red
    $failed | ForEach-Object {
        Write-Host ('  {0} stage {1}  p95={2:F2} ms' -f $_.World, $_.Stage, $_.P95Ms)
    }
    exit 1
}

$worst = ($results | Sort-Object P95Ms -Descending)[0]
Write-Host ("OK {0} stage(s) within the {1} ms budget. Worst p95 {2:F2} ms ({3} stage {4})." -f `
    $results.Count, $budget, $worst.P95Ms, $worst.World, $worst.Stage) -ForegroundColor Green
exit 0
