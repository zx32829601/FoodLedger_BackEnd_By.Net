param(
    [Parameter(Mandatory = $true)]
    [string[]]$ArgumentList
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($null -eq $ArgumentList -or $ArgumentList.Length -eq 0) {
    throw "Docker Compose arguments are required."
}

$dockerComposeCommand = Get-Command docker-compose -ErrorAction SilentlyContinue
if ($null -ne $dockerComposeCommand) {
    & $dockerComposeCommand.Source @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose failed with exit code $LASTEXITCODE."
    }

    return
}

$dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
if ($null -eq $dockerCommand) {
    throw "Docker CLI was not found."
}

& $dockerCommand.Source compose @ArgumentList
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit code $LASTEXITCODE."
}
