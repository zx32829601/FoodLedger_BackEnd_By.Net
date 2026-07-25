param(
    [switch]$SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent $PSScriptRoot
$EnvPath = Join-Path $RepoRoot ".env"
$DockerDesktopBinPath = Join-Path $env:LOCALAPPDATA "Programs\DockerDesktop\resources\bin"
$ComposeScriptPath = Join-Path $PSScriptRoot "Invoke-DockerCompose.ps1"
$DefaultApiHttpPort = "5062"

function Write-Step {
    param([string]$Message)

    Write-Host ""
    Write-Host "==> $Message"
}

function Ensure-DockerCli {
    $dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
    if ($null -ne $dockerCommand) {
        return
    }

    $dockerPath = Join-Path $DockerDesktopBinPath "docker.exe"
    if (Test-Path $dockerPath) {
        $env:Path = "$DockerDesktopBinPath;$env:Path"
        return
    }

    throw "Docker CLI was not found. Start Docker Desktop, or reinstall Docker Desktop and reopen the terminal."
}

function Ensure-LocalEnvFile {
    if (Test-Path $EnvPath) {
        return
    }

    throw ".env was not found. Copy .env.example to .env, then adjust local database settings before deploying."
}

function Get-ApiHttpPort {
    if (-not (Test-Path $EnvPath)) {
        return $DefaultApiHttpPort
    }

    foreach ($line in Get-Content -Encoding utf8 $EnvPath) {
        $trimmedLine = $line.Trim()
        if ($trimmedLine.Length -eq 0 -or $trimmedLine.StartsWith("#")) {
            continue
        }

        $parts = $trimmedLine.Split("=", 2)
        if ($parts.Length -eq 2 -and $parts[0].Trim() -eq "FOODLEDGER_API_HTTP_PORT") {
            $port = $parts[1].Trim().Trim('"').Trim("'")
            if (-not [string]::IsNullOrWhiteSpace($port)) {
                return $port
            }
        }
    }

    return $DefaultApiHttpPort
}

Push-Location $RepoRoot
try {
    Write-Step "Check Docker CLI"
    Ensure-DockerCli
    docker --version
    & $ComposeScriptPath -ArgumentList @("version")

    Write-Step "Check Docker daemon"
    docker info --format "{{.ServerVersion}}" | Out-Null

    Write-Step "Check local .env"
    Ensure-LocalEnvFile

    Write-Step "Validate docker-compose.yml"
    & $ComposeScriptPath -ArgumentList @("config", "--quiet")

    Write-Step "Start local deployment"
    if ($SkipBuild) {
        & $ComposeScriptPath -ArgumentList @("up", "--detach", "--remove-orphans")
    }
    else {
        & $ComposeScriptPath -ArgumentList @("up", "--build", "--detach", "--remove-orphans")
    }

    Write-Step "Show container status"
    & $ComposeScriptPath -ArgumentList @("ps")

    $apiHttpPort = Get-ApiHttpPort
    Write-Host ""
    Write-Host "Local deploy completed."
    Write-Host ("Swagger: http://localhost:{0}/swagger" -f $apiHttpPort)
}
finally {
    Pop-Location
}
