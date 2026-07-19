#requires -Version 5.1

$ErrorActionPreference = "Continue"

function Test-DotNet10Sdk {
    $dotnetExecutables = @()
    $dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue

    if ($dotnetCommand) {
        $dotnetExecutables += $dotnetCommand.Source
    }

    if ($env:ProgramFiles) {
        $standardDotnetPath = Join-Path $env:ProgramFiles "dotnet\dotnet.exe"
        if (Test-Path -LiteralPath $standardDotnetPath) {
            $dotnetExecutables += $standardDotnetPath
        }
    }

    foreach ($dotnetExecutable in @($dotnetExecutables | Select-Object -Unique)) {
        $sdkOutput = @(& $dotnetExecutable --list-sdks 2>&1)
        foreach ($line in $sdkOutput) {
            if ($line.ToString().Trim() -match '^10\.\d+\.\d+(?:-[^\s]+)?\s+\[') {
                return $true
            }
        }
    }

    return $false
}

function Test-DockerDesktop {
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
            return $true
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
            return $true
        }
    }

    return $false
}

function Test-WingetPackageAvailable {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WingetPath,

        [Parameter(Mandatory = $true)]
        [string]$PackageId
    )

    $null = & $WingetPath show --id $PackageId --exact --source winget --accept-source-agreements 2>$null
    return ($LASTEXITCODE -eq 0)
}

function Install-WingetPackage {
    param(
        [Parameter(Mandatory = $true)]
        [string]$WingetPath,

        [Parameter(Mandatory = $true)]
        [string]$PackageId,

        [Parameter(Mandatory = $true)]
        [string]$DisplayName
    )

    Write-Host "Installing $DisplayName ($PackageId)..." -ForegroundColor Yellow
    $arguments = @(
        "install",
        "--id", $PackageId,
        "--exact",
        "--source", "winget",
        "--accept-package-agreements",
        "--accept-source-agreements"
    )

    & $WingetPath @arguments
    if ($LASTEXITCODE -eq 0) {
        Write-Host "$DisplayName installation completed." -ForegroundColor Green
        return $true
    }

    Write-Host "$DisplayName installation failed with winget exit code $LASTEXITCODE." -ForegroundColor Red
    return $false
}

Write-Host "ECare development prerequisite installer" -ForegroundColor Cyan
Write-Host "This script installs .NET SDK 10 and Docker Desktop only when they are missing."
Write-Host "Windows may display an administrator approval prompt during installation."
Write-Host ""

$wingetCommand = Get-Command winget -CommandType Application -ErrorAction SilentlyContinue
if (-not $wingetCommand) {
    Write-Host "winget was not found. Install or update App Installer, then run this script again." -ForegroundColor Red
    Write-Host "Manual instructions: https://learn.microsoft.com/windows/package-manager/winget/"
    exit 1
}

$wingetPath = $wingetCommand.Source
$installedSomething = $false
$hadFailure = $false

if (Test-DotNet10Sdk) {
    Write-Host "[SKIP] .NET SDK 10.x is already installed." -ForegroundColor Green
} else {
    $dotnetPackageId = "Microsoft.DotNet.SDK.10"

    Write-Host "Checking winget for $dotnetPackageId..."
    if (-not (Test-WingetPackageAvailable -WingetPath $wingetPath -PackageId $dotnetPackageId)) {
        $dotnetPackageId = "Microsoft.DotNet.SDK.Preview"
        Write-Host "NOTE: Microsoft.DotNet.SDK.10 is not available from winget; falling back to $dotnetPackageId." -ForegroundColor Yellow
    }

    if (Test-WingetPackageAvailable -WingetPath $wingetPath -PackageId $dotnetPackageId) {
        if (Install-WingetPackage -WingetPath $wingetPath -PackageId $dotnetPackageId -DisplayName ".NET SDK") {
            $installedSomething = $true
        } else {
            $hadFailure = $true
        }
    } else {
        Write-Host "Neither the .NET SDK 10 package nor the preview fallback is available from winget." -ForegroundColor Red
        $hadFailure = $true
    }
}

if (Test-DockerDesktop) {
    Write-Host "[SKIP] Docker Desktop is already installed." -ForegroundColor Green
} else {
    $dockerPackageId = "Docker.DockerDesktop"

    if (Install-WingetPackage -WingetPath $wingetPath -PackageId $dockerPackageId -DisplayName "Docker Desktop") {
        $installedSomething = $true
    } else {
        $hadFailure = $true
    }
}

Write-Host ""
if ($installedSomething) {
    Write-Host "Installation work is complete. Restart this terminal before using the newly installed tools." -ForegroundColor Cyan
    Write-Host "Docker Desktop also needs the WSL 2 Windows feature and may require a Windows reboot before first use."
} else {
    Write-Host "No new tools were installed. Already-installed prerequisites were left unchanged."
}

Write-Host "After restarting, run .\scripts\check-env.ps1 to verify the environment."

if ($hadFailure) {
    Write-Host "One or more installations did not complete; review the messages above or use the manual setup links." -ForegroundColor Red
    exit 1
}

exit 0
