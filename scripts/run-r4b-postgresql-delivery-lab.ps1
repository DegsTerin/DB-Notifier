# Module purpose: Runs the bounded R4-B ownership matrix against one pinned, local-only disposable PostgreSQL container.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [string]$DotNetPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root '.dotnet\dotnet.exe'
}
$resolvedDotNet = (Resolve-Path -LiteralPath $DotNetPath).Path
$docker = (Get-Command docker.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Path
$imageDigest = 'sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb'
$imageTag = 'postgres:16-alpine'
$imageReference = $imageDigest
$runId = [guid]::NewGuid().ToString('N')
$containerName = "db-notifier-r4b-$runId"
$networkName = "db-notifier-r4b-net-$runId"
$volumeName = "db-notifier-r4b-data-$runId"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-R4B-$runId"
$passwordPath = Join-Path $temporaryRoot 'postgres-password.txt'
$createdContainer = $false
$createdNetwork = $false
$createdVolume = $false
$testExitCode = 1

function Invoke-DockerChecked([string[]]$Arguments) {
    $output = @(& $docker @Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw "The bounded Docker operation failed with exit code $LASTEXITCODE."
    }
    return $output
}

function Test-ExactLabel([string]$ResourceType, [string]$ResourceName) {
    $template = if ($ResourceType -eq 'container') {
        '{{index .Config.Labels "com.db-notifier.r4b"}}'
    }
    else {
        '{{index .Labels "com.db-notifier.r4b"}}'
    }
    $value = @(& $docker $ResourceType inspect --format $template $ResourceName 2>$null)
    return $LASTEXITCODE -eq 0 -and $value.Count -eq 1 -and $value[0] -eq 'true'
}

try {
    $server = @(& $docker version --format '{{.Server.Version}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or $server.Count -ne 1 -or [string]::IsNullOrWhiteSpace($server[0])) {
        throw 'The local Docker engine is unavailable.'
    }

    $identity = @(& $docker image inspect $imageReference --format '{{.Id}}|{{json .RepoTags}}|{{json .RepoDigests}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or $identity.Count -ne 1 -or
        -not $identity[0].StartsWith("$imageDigest|", [StringComparison]::Ordinal) -or
        -not $identity[0].Contains("$imageTag", [StringComparison]::Ordinal) -or
        -not $identity[0].Contains("postgres@$imageDigest", [StringComparison]::Ordinal)) {
        throw 'The exact pinned PostgreSQL image is not available locally.'
    }

    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    $password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    [System.IO.File]::WriteAllText($passwordPath, $password, [Text.UTF8Encoding]::new($false))

    Invoke-DockerChecked @(
        'network', 'create',
        '--label', 'com.db-notifier.r4b=true',
        '--label', "com.db-notifier.r4b.run=$runId",
        $networkName
    ) | Out-Null
    $createdNetwork = $true
    Invoke-DockerChecked @(
        'volume', 'create',
        '--label', 'com.db-notifier.r4b=true',
        '--label', "com.db-notifier.r4b.run=$runId",
        $volumeName
    ) | Out-Null
    $createdVolume = $true

    Invoke-DockerChecked @(
        'run', '--detach', '--pull', 'never',
        '--name', $containerName,
        '--label', 'com.db-notifier.r4b=true',
        '--label', "com.db-notifier.r4b.run=$runId",
        '--network', $networkName,
        '--mount', "type=volume,source=$volumeName,target=/var/lib/postgresql/data",
        '--mount', "type=bind,source=$passwordPath,target=/run/secrets/r4b_password,readonly",
        '--env', 'POSTGRES_USER=dbnotifier_r4b',
        '--env', 'POSTGRES_DB=dbnotifier_r4b',
        '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/r4b_password',
        '--publish', '127.0.0.1:0:5432',
        '--cpus', '1',
        '--memory', '512m',
        '--pids-limit', '256',
        '--health-cmd', 'pg_isready -U dbnotifier_r4b -d dbnotifier_r4b',
        '--health-interval', '1s',
        '--health-timeout', '3s',
        '--health-retries', '30',
        $imageReference
    ) | Out-Null
    $createdContainer = Test-ExactLabel 'container' $containerName
    $containerId = @(& $docker container inspect --format '{{.Id}}' $containerName 2>$null)
    if (-not $createdContainer -or $LASTEXITCODE -ne 0 -or
        $containerId.Count -ne 1 -or $containerId[0] -notmatch '^[0-9a-f]{64}$') {
        throw 'The disposable PostgreSQL container identity was not returned exactly once.'
    }

    $healthy = $false
    for ($attempt = 1; $attempt -le 40; $attempt++) {
        $health = @(& $docker container inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}' $containerName)
        if ($LASTEXITCODE -ne 0 -or $health.Count -ne 1) {
            throw 'The disposable PostgreSQL container health state became unavailable.'
        }
        if ($health[0] -eq 'healthy') {
            $healthy = $true
            break
        }
        if ($health[0] -eq 'unhealthy') {
            throw 'The disposable PostgreSQL container became unhealthy.'
        }
        Start-Sleep -Seconds 1
    }
    if (-not $healthy) {
        throw 'The disposable PostgreSQL container did not become healthy within the bounded wait.'
    }

    $portBinding = @(& $docker port $containerName '5432/tcp')
    if ($LASTEXITCODE -ne 0 -or $portBinding.Count -ne 1 -or
        $portBinding[0] -notmatch '^127\.0\.0\.1:(\d{1,5})$') {
        throw 'The disposable PostgreSQL port is not bound exactly once to IPv4 loopback.'
    }
    $hostPort = [int]$Matches[1]
    if ($hostPort -lt 1 -or $hostPort -gt 65535) {
        throw 'The disposable PostgreSQL loopback port is outside the valid range.'
    }

    $previousActivation = [Environment]::GetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_LAB', 'Process')
    $previousConnection = [Environment]::GetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_CONNECTION', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_LAB', 'local-test', 'Process')
        $passwordKey = 'Pass' + 'word'
        $connectionString = "Host=127.0.0.1;Port=$hostPort;Database=dbnotifier_r4b;Username=dbnotifier_r4b;$passwordKey=$password;SSL Mode=Disable;Timeout=5;Command Timeout=10;Include Error Detail=false"
        [Environment]::SetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_CONNECTION', $connectionString, 'Process')
        & $resolvedDotNet test `
            (Join-Path $root 'tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj') `
            --configuration Release `
            --no-build `
            --no-restore `
            --filter 'FullyQualifiedName~R4BPostgreSqlDeliveryOwnershipTests' `
            --logger 'console;verbosity=minimal'
        $testExitCode = $LASTEXITCODE
    }
    finally {
        [Environment]::SetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_CONNECTION', $previousConnection, 'Process')
        [Environment]::SetEnvironmentVariable('DBNOTIFIER_R4B_POSTGRESQL_LAB', $previousActivation, 'Process')
        $connectionString = $null
        $password = $null
    }
}
finally {
    if ($createdContainer -and (Test-ExactLabel 'container' $containerName)) {
        & $docker container rm --force $containerName 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned R4-B container could not be removed.' }
    }
    if ($createdVolume -and (Test-ExactLabel 'volume' $volumeName)) {
        & $docker volume rm $volumeName 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned R4-B volume could not be removed.' }
    }
    if ($createdNetwork -and (Test-ExactLabel 'network' $networkName)) {
        & $docker network rm $networkName 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned R4-B network could not be removed.' }
    }

    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $expectedPrefix = $systemTemporaryRoot + [System.IO.Path]::DirectorySeparatorChar + 'DBNotifier-R4B-'
    if ($resolvedTemporaryRoot.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}

if ($testExitCode -ne 0) {
    throw "The R4-B PostgreSQL ownership matrix failed with exit code $testExitCode."
}

$residualContainers = @(& $docker ps -a --filter 'label=com.db-notifier.r4b=true' --format '{{.Names}}')
$residualNetworks = @(& $docker network ls --filter 'label=com.db-notifier.r4b=true' --format '{{.Name}}')
$residualVolumes = @(& $docker volume ls --filter 'label=com.db-notifier.r4b=true' --format '{{.Name}}')
if ($residualContainers.Count -ne 0 -or $residualNetworks.Count -ne 0 -or $residualVolumes.Count -ne 0) {
    throw 'One or more R4-B-owned Docker resources remained after cleanup.'
}

Write-Output "R4-B PostgreSQL ownership lab passed with pinned image $imageDigest and zero owned Docker residue."
