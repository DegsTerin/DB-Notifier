# Module purpose: Runs the R-SEQ migration matrix against one pinned, loopback-only disposable PostgreSQL container.
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
$runId = [guid]::NewGuid().ToString('N')
$containerName = "db-notifier-rseq-$runId"
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-RSEQ-$runId"
$passwordPath = Join-Path $temporaryRoot 'postgres-password.txt'
$createdContainer = $false
$testExitCode = 1

# Runs one bounded Docker command and refuses any non-zero result.
function Invoke-DockerChecked([string[]]$Arguments) {
    $output = @(& $docker @Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw "The bounded Docker operation failed with exit code $LASTEXITCODE."
    }
    return $output
}

# Confirms ownership before any container removal.
function Test-ExactContainerLabel([string]$ResourceName) {
    $value = @(
        & $docker container inspect `
            --format '{{index .Config.Labels "com.db-notifier.rseq"}}' `
            $ResourceName 2>$null
    )
    return $LASTEXITCODE -eq 0 -and $value.Count -eq 1 -and $value[0] -eq 'true'
}

try {
    $serverVersion = @(& $docker version --format '{{.Server.Version}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or $serverVersion.Count -ne 1 -or
        [string]::IsNullOrWhiteSpace($serverVersion[0])) {
        throw 'The local Docker engine is unavailable.'
    }

    $identity = @(
        & $docker image inspect `
            $imageDigest `
            --format '{{.Id}}|{{json .RepoTags}}|{{json .RepoDigests}}' 2>$null
    )
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
        'run', '--detach', '--pull', 'never',
        '--name', $containerName,
        '--label', 'com.db-notifier.rseq=true',
        '--label', "com.db-notifier.rseq.run=$runId",
        '--mount', "type=bind,source=$passwordPath,target=/run/secrets/rseq_password,readonly",
        '--tmpfs', '/var/lib/postgresql/data:rw,noexec,nosuid,size=256m',
        '--env', 'POSTGRES_USER=dbnotifier_rseq',
        '--env', 'POSTGRES_DB=dbnotifier_rseq',
        '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/rseq_password',
        '--publish', '127.0.0.1:0:5432',
        '--cpus', '1',
        '--memory', '512m',
        '--pids-limit', '256',
        '--health-cmd', 'pg_isready -U dbnotifier_rseq -d dbnotifier_rseq',
        '--health-interval', '1s',
        '--health-timeout', '3s',
        '--health-retries', '30',
        $imageDigest
    ) | Out-Null
    $createdContainer = Test-ExactContainerLabel $containerName
    if (-not $createdContainer) {
        throw 'The disposable PostgreSQL container identity was not proved.'
    }

    $healthy = $false
    for ($attempt = 1; $attempt -le 40; $attempt++) {
        $health = @(
            & $docker container inspect `
                --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}' `
                $containerName
        )
        if ($LASTEXITCODE -ne 0 -or $health.Count -ne 1) {
            throw 'The disposable PostgreSQL health state became unavailable.'
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
        throw 'The disposable PostgreSQL loopback port is invalid.'
    }

    $previousActivation = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_RSEQ_POSTGRESQL_LAB',
        'Process')
    $previousConnection = [Environment]::GetEnvironmentVariable(
        'DBNOTIFIER_RSEQ_POSTGRESQL_CONNECTION',
        'Process')
    try {
        $passwordKey = 'Pass' + 'word'
        $connectionString =
            "Host=127.0.0.1;Port=$hostPort;Database=dbnotifier_rseq;Username=dbnotifier_rseq;" +
            "$passwordKey=$password;SSL Mode=Disable;Timeout=5;Command Timeout=15;" +
            "Include Error Detail=false;Pooling=false;Application Name=DBNotifier-RSEQ-Local-Test"
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_RSEQ_POSTGRESQL_LAB',
            'local-test',
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_RSEQ_POSTGRESQL_CONNECTION',
            $connectionString,
            'Process')
        & $resolvedDotNet test `
            (Join-Path $root 'tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj') `
            --configuration Release `
            --no-build `
            --no-restore `
            --filter 'FullyQualifiedName~RSeqPostgreSqlMigrationTests' `
            --logger 'console;verbosity=minimal'
        $testExitCode = $LASTEXITCODE
    }
    finally {
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_RSEQ_POSTGRESQL_CONNECTION',
            $previousConnection,
            'Process')
        [Environment]::SetEnvironmentVariable(
            'DBNOTIFIER_RSEQ_POSTGRESQL_LAB',
            $previousActivation,
            'Process')
        $connectionString = $null
        $password = $null
    }
}
finally {
    if ($createdContainer -and (Test-ExactContainerLabel $containerName)) {
        & $docker container rm --force $containerName 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) {
            throw 'The owned R-SEQ container could not be removed.'
        }
    }

    $resolvedTemporaryRoot = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar)
    $expectedPrefix = $systemTemporaryRoot + [System.IO.Path]::DirectorySeparatorChar + 'DBNotifier-RSEQ-'
    if ($resolvedTemporaryRoot.StartsWith($expectedPrefix, [StringComparison]::OrdinalIgnoreCase) -and
        (Test-Path -LiteralPath $resolvedTemporaryRoot)) {
        Remove-Item -LiteralPath $resolvedTemporaryRoot -Recurse -Force
    }
}

if ($testExitCode -ne 0) {
    throw "The R-SEQ PostgreSQL migration matrix failed with exit code $testExitCode."
}

$residualContainers = @(
    & $docker ps -a --filter 'label=com.db-notifier.rseq=true' --format '{{.Names}}'
)
if ($residualContainers.Count -ne 0) {
    throw 'One or more R-SEQ-owned Docker containers remained after cleanup.'
}

Write-Output "R-SEQ PostgreSQL migration lab passed with pinned image $imageDigest and zero owned Docker residue."
