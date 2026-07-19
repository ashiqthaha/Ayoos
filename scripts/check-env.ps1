#requires -Version 5.1

$ErrorActionPreference = "Continue"

function Add-CheckResult {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Tool,

        [Parameter(Mandatory = $true)]
        [bool]$Installed,

        [Parameter(Mandatory = $true)]
        [string]$Version,

        [Parameter(Mandatory = $true)]
        [string]$Requirement,

        [Parameter(Mandatory = $true)]
        [bool]$Passed
    )

    $script:Results.Add([pscustomobject]@{
        Tool        = $Tool
        Installed   = if ($Installed) { "Yes" } else { "No" }
        Version     = $Version
        Requirement = $Requirement
        Result      = if ($Passed) { "PASS" } else { "FAIL" }
    }) | Out-Null
}

function Get-DockerDesktopDetails {
    $candidatePaths = @()

    if ($env:ProgramFiles) {
        $candidatePaths += (Join-Path $env:ProgramFiles "Docker\Docker\Docker Desktop.exe")
    }

    if (${env:ProgramFiles(x86)}) {
        $candidatePaths += (Join-Path ${env:ProgramFiles(x86)} "Docker\Docker\Docker Desktop.exe")
    }

    if ($env:LOCALAPPDATA) {
        $candidatePaths += (Join-Path $env:LOCALAPPDATA "Docker\Docker Desktop.exe")
    }

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path -LiteralPath $candidatePath) {
            $desktopFile = Get-Item -LiteralPath $candidatePath -ErrorAction SilentlyContinue
            $desktopVersion = $desktopFile.VersionInfo.ProductVersion

            if ([string]::IsNullOrWhiteSpace($desktopVersion)) {
                $desktopVersion = "Detected"
            }

            return [pscustomobject]@{
                Installed = $true
                Version   = $desktopVersion
            }
        }
    }

    $uninstallLocations = @(
        "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*",
        "HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\*"
    )

    foreach ($location in $uninstallLocations) {
        $desktopEntry = Get-ItemProperty -Path $location -ErrorAction SilentlyContinue |
            Where-Object { $_.DisplayName -like "Docker Desktop*" } |
            Select-Object -First 1

        if ($desktopEntry) {
            $desktopVersion = if ($desktopEntry.DisplayVersion) { $desktopEntry.DisplayVersion } else { "Detected" }

            return [pscustomobject]@{
                Installed = $true
                Version   = $desktopVersion
            }
        }
    }

    return [pscustomobject]@{
        Installed = $false
        Version   = "Not found"
    }
}

$Results = New-Object System.Collections.Generic.List[object]

Write-Host "ECare local development environment check" -ForegroundColor Cyan
Write-Host "Checking required command-line tools and Windows applications..."

# Node.js 20 or newer
$nodeCommand = Get-Command node -CommandType Application -ErrorAction SilentlyContinue
if ($nodeCommand) {
    $nodeVersion = ((& $nodeCommand.Source --version 2>&1 | Select-Object -First 1).ToString()).Trim()
    $nodePassed = $false

    if ($nodeVersion -match '^v?(?<major>\d+)\.') {
        $nodePassed = ([int]$Matches.major -ge 20)
    }

    Add-CheckResult -Tool "Node.js" -Installed $true -Version $nodeVersion -Requirement ">= 20" -Passed $nodePassed
} else {
    Add-CheckResult -Tool "Node.js" -Installed $false -Version "Not found" -Requirement ">= 20" -Passed $false
}

# .NET SDK 10.x (look at every installed SDK, not only the active default)
$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
if ($dotnetCommand) {
    $sdkVersions = @()
    $sdkOutput = @(& $dotnetCommand.Source --list-sdks 2>&1)

    foreach ($line in $sdkOutput) {
        $lineText = $line.ToString().Trim()
        if ($lineText -match '^(?<version>\d+\.\d+\.\d+(?:-[^\s]+)?)\s+\[') {
            $sdkVersions += $Matches.version
        }
    }

    $dotnet10Installed = $false
    foreach ($sdkVersion in $sdkVersions) {
        if ($sdkVersion -match '^(?<major>\d+)\.' -and [int]$Matches.major -eq 10) {
            $dotnet10Installed = $true
            break
        }
    }

    $dotnetVersionText = if ($sdkVersions.Count -gt 0) { $sdkVersions -join ", " } else { "No SDKs found" }
    Add-CheckResult -Tool ".NET SDK" -Installed ($sdkVersions.Count -gt 0) -Version $dotnetVersionText -Requirement "10.x" -Passed $dotnet10Installed
} else {
    Add-CheckResult -Tool ".NET SDK" -Installed $false -Version "Not found" -Requirement "10.x" -Passed $false
}

# Docker Desktop itself
$dockerDesktop = Get-DockerDesktopDetails
$dockerCommand = Get-Command docker -CommandType Application -ErrorAction SilentlyContinue
$dockerDesktopVersion = $dockerDesktop.Version

if (-not $dockerDesktop.Installed -and $dockerCommand) {
    $dockerCliVersion = ((& $dockerCommand.Source --version 2>&1 | Select-Object -First 1).ToString()).Trim()
    $dockerDesktopVersion = "Desktop not found; $dockerCliVersion"
}

Add-CheckResult -Tool "Docker Desktop" -Installed $dockerDesktop.Installed -Version $dockerDesktopVersion -Requirement "Installed" -Passed $dockerDesktop.Installed

# Docker Compose v2 is normally supplied by Docker Desktop.
$composeInstalled = $false
$composeVersion = "Not found"

if ($dockerCommand) {
    $dockerExecutable = $dockerCommand | Select-Object -First 1 -ExpandProperty Source
    $composeOutput = ((& $dockerExecutable compose version 2>&1 | Out-String)).Trim()
    if ($LASTEXITCODE -eq 0) {
        $composeInstalled = $true
        $composeVersion = $composeOutput -replace '^Docker Compose version\s+', ''
    }
}

# Also recognize the older standalone command when present.
if (-not $composeInstalled) {
    $legacyComposeCommand = Get-Command docker-compose -CommandType Application -ErrorAction SilentlyContinue
    if ($legacyComposeCommand) {
        $legacyComposeExecutable = $legacyComposeCommand | Select-Object -First 1 -ExpandProperty Source
        $legacyOutput = ((& $legacyComposeExecutable --version 2>&1 | Out-String)).Trim()
        if ($LASTEXITCODE -eq 0) {
            $composeInstalled = $true
            $composeVersion = $legacyOutput
        }
    }
}

Add-CheckResult -Tool "Docker Compose" -Installed $composeInstalled -Version $composeVersion -Requirement "Installed" -Passed $composeInstalled

# Git
$gitCommand = Get-Command git -CommandType Application -ErrorAction SilentlyContinue
if ($gitCommand) {
    $gitVersionOutput = ((& $gitCommand.Source --version 2>&1 | Select-Object -First 1).ToString()).Trim()
    $gitVersion = $gitVersionOutput -replace '^git version\s+', ''
    Add-CheckResult -Tool "Git" -Installed $true -Version $gitVersion -Requirement "Installed" -Passed $true
} else {
    Add-CheckResult -Tool "Git" -Installed $false -Version "Not found" -Requirement "Installed" -Passed $false
}

Write-Host ""
Write-Host "Environment summary" -ForegroundColor Cyan
$Results | Format-Table Tool, Installed, Version, Requirement, Result -AutoSize | Out-Host

$failedChecks = @($Results | Where-Object { $_.Result -eq "FAIL" })
if ($failedChecks.Count -eq 0) {
    Write-Host "All required development tools are ready." -ForegroundColor Green
    exit 0
}

$failedToolNames = ($failedChecks | ForEach-Object { $_.Tool }) -join ", "
Write-Host "Environment check failed for: $failedToolNames" -ForegroundColor Red
Write-Host "Run .\scripts\install-env.ps1 to install the supported missing prerequisites."
exit 1
