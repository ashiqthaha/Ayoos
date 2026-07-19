# Local development environment

ECare development targets Windows 11 and requires:

- Node.js 20 or newer
- .NET SDK 10.x
- Docker Desktop with Docker Compose
- Git

Node.js may already be installed, but the environment check verifies its version along with every other prerequisite.

## Check the environment

Open PowerShell and change to the repository root:

```powershell
cd "C:\Developement Workground\Ecare\hospital-management-emr-master\ecare-next"
```

Run the environment check:

```powershell
.\scripts\check-env.ps1
```

The script prints the detected version of each tool and finishes with a pass/fail summary table. It returns exit code `0` when every requirement passes and `1` when something is missing or has an unsupported version.

If PowerShell blocks local scripts, allow them only for the current PowerShell process and try again:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass
.\scripts\check-env.ps1
```

## Install missing prerequisites

The installer uses only `winget`. It installs .NET SDK 10 and Docker Desktop when missing and skips either tool when it is already present, so it is safe to run more than once.

```powershell
.\scripts\install-env.ps1
```

The script first tries the stable .NET package `Microsoft.DotNet.SDK.10`. If that package is unavailable in the configured winget source, it prints a note and falls back to `Microsoft.DotNet.SDK.Preview`. Docker Desktop is installed from `Docker.DockerDesktop`.

Installation may trigger a Windows administrator approval prompt. After installation, restart PowerShell so the updated command paths are available, then verify again:

```powershell
.\scripts\check-env.ps1
```

## Docker Desktop, WSL 2, and reboot

Docker Desktop on Windows uses the WSL 2 backend. Install or enable WSL 2 before starting Docker Desktop:

```powershell
wsl --install
```

Run that command from an administrator PowerShell window. Reboot Windows after enabling WSL 2 (or whenever Windows or Docker Desktop requests it), then launch Docker Desktop and finish its first-run setup. Docker Compose is included with current Docker Desktop installations and is checked with `docker compose version`.

For more detail, see Microsoft's [WSL installation guide](https://learn.microsoft.com/windows/wsl/install) and Docker's [Windows installation guide](https://docs.docker.com/desktop/setup/install/windows-install/).

## Manual installation fallback

Use these official downloads if winget is unavailable or an automated installation fails:

- [Node.js downloads](https://nodejs.org/en/download) — install a supported release that is version 20 or newer.
- [.NET 10 downloads](https://dotnet.microsoft.com/download/dotnet/10.0) — install the SDK, not only the runtime.
- [Docker Desktop for Windows](https://docs.docker.com/desktop/setup/install/windows-install/)
- [Git for Windows](https://git-scm.com/download/win)
- [winget / App Installer documentation](https://learn.microsoft.com/windows/package-manager/winget/)

Restart PowerShell after any manual installation and rerun `check-env.ps1`.
