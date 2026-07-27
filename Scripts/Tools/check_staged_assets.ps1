<#
.SYNOPSIS
    Blocks large binaries from entering git history uncovered by LFS.

.DESCRIPTION
    This repository reached a 2.5 GB .git because roughly 563 MB of generated
    art was committed as raw, undeltifiable blobs. The cost is permanent: a
    blob cannot be removed from history without git-filter-repo or BFG, which
    rewrites every later commit hash and invalidates every existing clone.

    So the only cheap moment to catch this is BEFORE the commit. Run this
    against the staging area and it reports any staged file at or over the
    size threshold whose path is not routed through LFS.

    Exits non-zero when something is flagged, so it works as a pre-commit hook:
        Scripts/Tools/check_staged_assets.ps1

.PARAMETER ThresholdMB
    Size at or above which an unmanaged binary is treated as a problem.

.PARAMETER Fix
    Append the required LFS rules to .gitattributes instead of only reporting.
    The files must then be re-staged so they are stored as LFS pointers.
#>
[CmdletBinding()]
param(
    [double]$ThresholdMB = 1.0,
    [switch]$Fix
)

$ErrorActionPreference = 'Stop'

$repoRoot = (git rev-parse --show-toplevel 2>$null)
if (-not $repoRoot) {
    Write-Error 'Not inside a git repository.'
    exit 2
}
Push-Location $repoRoot
try {
    $staged = @(git diff --cached --name-only --diff-filter=ACM 2>$null |
        Where-Object { $_ })
    if ($staged.Count -eq 0) {
        Write-Host 'Nothing staged.'
        exit 0
    }

    $thresholdBytes = $ThresholdMB * 1MB
    $offenders = New-Object System.Collections.ArrayList

    foreach ($path in $staged) {
        $item = Get-Item -LiteralPath $path -ErrorAction SilentlyContinue
        if (-not $item -or $item.Length -lt $thresholdBytes) { continue }

        # Ask git itself rather than parsing .gitattributes, so precedence,
        # negations, and nested .gitattributes files are all honoured.
        $attr = git check-attr filter -- "$path" 2>$null
        if ($attr -match 'filter:\s*lfs') { continue }

        [void]$offenders.Add([PSCustomObject]@{
            Path = $path
            MB   = [math]::Round($item.Length / 1MB, 2)
        })
    }

    if ($offenders.Count -eq 0) {
        Write-Host "OK - every staged file at or over $ThresholdMB MB is LFS-managed."
        exit 0
    }

    $totalMB = [math]::Round(($offenders | Measure-Object MB -Sum).Sum, 1)
    Write-Host ''
    Write-Host "BLOCKED: $($offenders.Count) staged file(s), $totalMB MB, would enter history as raw blobs." -ForegroundColor Red
    $offenders | Sort-Object MB -Descending | Select-Object -First 20 |
        ForEach-Object { Write-Host ('  {0,8:N2} MB  {1}' -f $_.MB, $_.Path) }
    if ($offenders.Count -gt 20) {
        Write-Host "  ... and $($offenders.Count - 20) more"
    }

    if (-not $Fix) {
        Write-Host ''
        Write-Host 'Choose one:' -ForegroundColor Yellow
        Write-Host '  1. Needed by the game    -> re-run with -Fix, then re-stage the files.'
        Write-Host '  2. Working material only -> add it to .gitignore; it stays on disk.'
        Write-Host '  Scripts/Tools/audit_asset_usage.py tells you which case applies.'
        exit 1
    }

    $attributesPath = Join-Path $repoRoot '.gitattributes'
    $raw = if (Test-Path $attributesPath) {
        [System.IO.File]::ReadAllText($attributesPath)
    } else { '' }
    $newline = if ($raw -match "`r`n") { "`r`n" } else { "`n" }
    if ($raw -and -not $raw.EndsWith("`n")) { $raw += $newline }
    foreach ($offender in $offenders) {
        $raw += "$($offender.Path) filter=lfs diff=lfs merge=lfs -text$newline"
    }
    [System.IO.File]::WriteAllText($attributesPath, $raw)

    Write-Host ''
    Write-Host "Added $($offenders.Count) LFS rule(s) to .gitattributes." -ForegroundColor Green
    Write-Host 'Now re-stage those files so git stores them as LFS pointers:' -ForegroundColor Yellow
    Write-Host '  git add .gitattributes; git add --renormalize -- <paths>'
    exit 1
}
finally {
    Pop-Location
}
