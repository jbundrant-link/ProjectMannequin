param(
    [Parameter(Mandatory = $true)]
    [string]$SourcePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [Parameter(Mandatory = $true)]
    [string]$Prompt,

    [ValidateSet("1:1", "4:3", "3:4", "16:9", "9:16", "3:2", "2:3")]
    [string]$AspectRatio = "3:4",

    [ValidateSet("nano_banana_flash", "gpt_image_2")]
    [string]$Model = "nano_banana_flash",

    [ValidateSet("1k", "2k")]
    [string]$Resolution = "2k"
)

$ErrorActionPreference = "Stop"

$cli = "C:\Users\Joseph Bundrant\AppData\Local\Programs\higgsfield\higgsfield.exe"
$resolvedSource = (Resolve-Path -LiteralPath $SourcePath).Path
$resolvedOutput = [System.IO.Path]::GetFullPath(
    (Join-Path (Get-Location) $OutputPath)
)
$outputDirectory = Split-Path -Parent $resolvedOutput

if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

for ($attempt = 1; $attempt -le 3; $attempt++) {
    try {
        $arguments = @(
            "generate", "create", $Model,
            "--prompt", $Prompt,
            "--image", $resolvedSource,
            "--aspect_ratio", $AspectRatio,
            "--resolution", $Resolution
        )
        if ($Model -eq "gpt_image_2") {
            $arguments += @("--quality", "high")
        }
        $arguments += @(
            "--wait",
            "--wait-timeout", "20m",
            "--json"
        )

        $rawResult = & $cli @arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Higgsfield exited with code $LASTEXITCODE."
        }

        $jobs = ($rawResult -join [Environment]::NewLine) | ConvertFrom-Json
        $resultUrl = @($jobs)[0].result_url
        if ([string]::IsNullOrWhiteSpace($resultUrl)) {
            throw "Higgsfield completed without a result URL."
        }

        Invoke-WebRequest -Uri $resultUrl -OutFile $resolvedOutput
        Write-Output "Saved $resolvedOutput"
        exit 0
    }
    catch {
        if ($attempt -ge 3) {
            throw
        }

        Start-Sleep -Seconds (4 * $attempt)
    }
}
