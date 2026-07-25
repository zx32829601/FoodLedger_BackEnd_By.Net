param(
    [Parameter(ValueFromRemainingArguments = $true)]
    [string[]]$ComposeArguments
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($null -eq $ComposeArguments -or $ComposeArguments.Length -eq 0) {
    throw "Docker Compose arguments are required."
}

$dockerComposeCommand = Get-Command docker-compose -ErrorAction SilentlyContinue
if ($null -ne $dockerComposeCommand) {
    & $dockerComposeCommand.Source @ComposeArguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose failed with exit code $LASTEXITCODE."
    }

    return
}

$dockerCommand = Get-Command docker -ErrorAction SilentlyContinue
if ($null -eq $dockerCommand) {
    throw "Docker CLI was not found."
}

& $dockerCommand.Source compose @ComposeArguments
if ($LASTEXITCODE -ne 0) {
    throw "docker compose failed with exit code $LASTEXITCODE."
}
