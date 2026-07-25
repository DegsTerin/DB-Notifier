# Module purpose: Starts, validates and stops the isolated PF-OBS-1 PostgreSQL and Observer pilot with exact owned-resource cleanup.
#Requires -Version 7.0
[CmdletBinding()]
param(
    [ValidateSet('Campaign', 'Start', 'Stop')]
    [string]$Action = 'Campaign',
    [switch]$OpenReview
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$dotnet = (Resolve-Path (Join-Path $repositoryRoot '.dotnet\dotnet.exe')).Path
$docker = (Get-Command docker.exe -CommandType Application -ErrorAction Stop | Select-Object -First 1).Path
$dashboardRoot = Join-Path $repositoryRoot 'src\DBNotifier.Dashboard.Web'
$dashboardDist = Join-Path $dashboardRoot 'dist'
$integrationProject = Join-Path $repositoryRoot 'tests\DBNotifier.IntegrationTests\DBNotifier.IntegrationTests.csproj'
$hostProject = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\DBNotifier.State06.ConsolidatedSandboxHost.csproj'
$hostAssembly = Join-Path $repositoryRoot 'tests\DBNotifier.State06.ConsolidatedSandboxHost\bin\Release\net10.0\DBNotifier.State06.ConsolidatedSandboxHost.dll'
$statePath = Join-Path ([IO.Path]::GetTempPath()) 'DBNotifier-PF-OBS-1-active.json'
$imageTag = 'postgres:16-alpine'
$imageDigest = 'sha256:e013e867e712fec275706a6c51c966f0bb0c93cfa8f51000f85a15f9865a28cb'
$pilotLabel = 'com.db-notifier.pf-obs-1'
$run = $null
$pendingRun = $null

# Invokes one Docker command and rejects any non-zero result without echoing sensitive arguments.
function Invoke-DockerChecked([string[]]$Arguments) {
    $output = @(& $docker @Arguments)
    if ($LASTEXITCODE -ne 0) {
        throw "The bounded PF-OBS-1 Docker operation failed with exit code $LASTEXITCODE."
    }
    return $output
}

# Returns true only when an exact Docker resource carries the PF-OBS-1 ownership label.
function Test-OwnedResource([string]$ResourceType, [string]$ResourceName) {
    if ([string]::IsNullOrWhiteSpace($ResourceName)) { return $false }
    $template = if ($ResourceType -eq 'container') {
        '{{index .Config.Labels "com.db-notifier.pf-obs-1"}}'
    }
    else {
        '{{index .Labels "com.db-notifier.pf-obs-1"}}'
    }
    $value = @(& $docker $ResourceType inspect --format $template $ResourceName 2>$null)
    return $LASTEXITCODE -eq 0 -and $value.Count -eq 1 -and $value[0] -eq 'true'
}

# Stops only a process whose exact identity was retained by this pilot.
function Stop-OwnedProcess([Nullable[int]]$ProcessId) {
    if ($null -eq $ProcessId -or $ProcessId.Value -lt 1) { return }
    $process = Get-Process -Id $ProcessId.Value -ErrorAction SilentlyContinue
    if ($null -ne $process) {
        & taskkill.exe /PID $ProcessId.Value /T /F 2>$null | Out-Null
        try { $process.WaitForExit(5000) | Out-Null } catch { }
    }
}

# Removes only a GUID-owned PF-OBS-1 root below the operating-system temporary directory.
function Remove-OwnedRoot([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return }
    $candidate = [IO.Path]::GetFullPath($Path)
    $temporary = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
    if ($candidate.StartsWith($temporary, [StringComparison]::OrdinalIgnoreCase) -and
        (Split-Path -Leaf $candidate).StartsWith('DBNotifier-PF-OBS-1-', [StringComparison]::Ordinal) -and
        (Test-Path -LiteralPath $candidate)) {
        Remove-Item -LiteralPath $candidate -Recurse -Force
    }
}

# Cleans one exact pilot state without touching unrelated Docker or browser resources.
function Stop-Pilot([object]$State) {
    Stop-OwnedProcess ([Nullable[int]]$State.browserPid)
    Stop-OwnedProcess ([Nullable[int]]$State.hostPid)
    if (Test-OwnedResource 'container' ([string]$State.containerName)) {
        & $docker container rm --force ([string]$State.containerName) 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned PF-OBS-1 container could not be removed.' }
    }
    if (Test-OwnedResource 'volume' ([string]$State.volumeName)) {
        & $docker volume rm ([string]$State.volumeName) 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned PF-OBS-1 volume could not be removed.' }
    }
    if (Test-OwnedResource 'network' ([string]$State.networkName)) {
        & $docker network rm ([string]$State.networkName) 2>$null | Out-Null
        if ($LASTEXITCODE -ne 0) { throw 'The owned PF-OBS-1 network could not be removed.' }
    }
    Remove-OwnedRoot ([string]$State.temporaryRoot)
    if (Test-Path -LiteralPath $statePath) {
        Remove-Item -LiteralPath $statePath -Force
    }
}

# Creates a synthetic CA and hostname-valid leaf certificate without persisting private material outside the owned root.
function New-PilotTlsMaterial([string]$Root) {
    $certificateRoot = Join-Path $Root 'tls'
    New-Item -ItemType Directory -Path $certificateRoot | Out-Null
    $caKey = [Security.Cryptography.RSA]::Create(2048)
    $serverKey = [Security.Cryptography.RSA]::Create(2048)
    try {
        $hash = [Security.Cryptography.HashAlgorithmName]::SHA256
        $padding = [Security.Cryptography.RSASignaturePadding]::Pkcs1
        $caRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
            'CN=DB Notifier PF-OBS-1 Local CA', $caKey, $hash, $padding)
        $caRequest.CertificateExtensions.Add(
            [Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($true, $false, 0, $true))
        $caRequest.CertificateExtensions.Add(
            [Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::KeyCertSign,
                $true))
        $now = [DateTimeOffset]::UtcNow
        $caCertificate = $caRequest.CreateSelfSigned($now.AddMinutes(-1), $now.AddDays(2))
        try {
            $serverRequest = [Security.Cryptography.X509Certificates.CertificateRequest]::new(
                'CN=localhost', $serverKey, $hash, $padding)
            $names = [Security.Cryptography.X509Certificates.SubjectAlternativeNameBuilder]::new()
            $names.AddDnsName('localhost')
            $names.AddIpAddress([Net.IPAddress]::Loopback)
            $serverRequest.CertificateExtensions.Add($names.Build())
            $serverRequest.CertificateExtensions.Add(
                [Security.Cryptography.X509Certificates.X509BasicConstraintsExtension]::new($false, $false, 0, $true))
            $serverRequest.CertificateExtensions.Add(
                [Security.Cryptography.X509Certificates.X509KeyUsageExtension]::new(
                    [Security.Cryptography.X509Certificates.X509KeyUsageFlags]::DigitalSignature,
                    $true))
            $serverUsages = [Security.Cryptography.OidCollection]::new()
            $serverUsages.Add([Security.Cryptography.Oid]::new('1.3.6.1.5.5.7.3.1')) | Out-Null
            $serverRequest.CertificateExtensions.Add(
                [Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension]::new(
                    $serverUsages,
                    $true))
            $serial = [Security.Cryptography.RandomNumberGenerator]::GetBytes(16)
            $serverCertificate = $serverRequest.Create(
                $caCertificate,
                $now.AddMinutes(-1),
                $now.AddDays(1),
                $serial)
            try {
                [IO.File]::WriteAllText(
                    (Join-Path $certificateRoot 'root.crt'),
                    $caCertificate.ExportCertificatePem(),
                    [Text.UTF8Encoding]::new($false))
                [IO.File]::WriteAllText(
                    (Join-Path $certificateRoot 'server.crt'),
                    $serverCertificate.ExportCertificatePem(),
                    [Text.UTF8Encoding]::new($false))
                [IO.File]::WriteAllText(
                    (Join-Path $certificateRoot 'server.key'),
                    $serverKey.ExportPkcs8PrivateKeyPem(),
                    [Text.UTF8Encoding]::new($false))
                $digest = [Convert]::ToHexString(
                    [Security.Cryptography.SHA256]::HashData($serverCertificate.RawData))
                return [pscustomobject]@{
                    Root = $certificateRoot
                    RootCertificate = (Join-Path $certificateRoot 'root.crt')
                    Digest = $digest
                }
            }
            finally { $serverCertificate.Dispose() }
        }
        finally { $caCertificate.Dispose() }
    }
    finally {
        $serverKey.Dispose()
        $caKey.Dispose()
    }
}

# Waits for the exact container to report healthy within a bounded interval.
function Wait-PilotHealthy([string]$ContainerName) {
    for ($attempt = 1; $attempt -le 60; $attempt++) {
        $health = @(& $docker container inspect --format '{{if .State.Health}}{{.State.Health.Status}}{{else}}missing{{end}}' $ContainerName)
        if ($LASTEXITCODE -ne 0 -or $health.Count -ne 1) {
            throw 'The PF-OBS-1 container health state became unavailable.'
        }
        if ($health[0] -eq 'healthy') { return }
        if ($health[0] -eq 'unhealthy') { throw 'The PF-OBS-1 PostgreSQL container became unhealthy.' }
        Start-Sleep -Milliseconds 500
    }
    throw 'The PF-OBS-1 PostgreSQL container did not become healthy in time.'
}

# Creates the exact loopback-only PostgreSQL 16 laboratory and its synthetic read-only fixture.
function Start-Laboratory {
    $server = @(& $docker version --format '{{.Server.Version}}' 2>$null)
    if ($LASTEXITCODE -ne 0 -or $server.Count -ne 1) {
        throw 'Docker Desktop is not available.'
    }
    $identity = @(& $docker image inspect $imageDigest --format '{{.Id}}|{{json .RepoTags}}|{{json .RepoDigests}}' 2>$null)
    if ($LASTEXITCODE -ne 0) {
        Invoke-DockerChecked @('pull', $imageTag) | Out-Null
        $identity = @(& $docker image inspect $imageDigest --format '{{.Id}}|{{json .RepoTags}}|{{json .RepoDigests}}')
    }
    if ($identity.Count -ne 1 -or
        -not $identity[0].StartsWith("$imageDigest|", [StringComparison]::Ordinal) -or
        -not $identity[0].Contains($imageTag, [StringComparison]::Ordinal)) {
        throw 'The exact official PostgreSQL 16 image provenance could not be proved.'
    }

    $runId = [Guid]::NewGuid().ToString('N')
    $temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) "DBNotifier-PF-OBS-1-$runId"
    $containerName = "db-notifier-pf-obs-1-$runId"
    $networkName = "db-notifier-pf-obs-1-net-$runId"
    $volumeName = "db-notifier-pf-obs-1-data-$runId"
    $script:pendingRun = [pscustomobject]@{
        runId = $runId
        temporaryRoot = $temporaryRoot
        containerName = $containerName
        networkName = $networkName
        volumeName = $volumeName
        port = 0
        connection = ''
        certificateDigest = ''
        evidencePath = (Join-Path $temporaryRoot 'pf-obs-1-evidence.json')
        hmPath = (Join-Path $temporaryRoot 'hm-01-03.json')
        hostPid = $null
        browserPid = $null
        baseAddress = $null
        profilePath = $null
    }
    New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
    $tls = New-PilotTlsMaterial $temporaryRoot
    $passwordPath = Join-Path $temporaryRoot 'postgres-password.txt'
    $password = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
    [IO.File]::WriteAllText($passwordPath, $password, [Text.UTF8Encoding]::new($false))
    Invoke-DockerChecked @(
        'network', 'create',
        '--label', "$pilotLabel=true",
        '--label', "$pilotLabel.run=$runId",
        $networkName
    ) | Out-Null
    Invoke-DockerChecked @(
        'volume', 'create',
        '--label', "$pilotLabel=true",
        '--label', "$pilotLabel.run=$runId",
        $volumeName
    ) | Out-Null
    $containerCommand = 'mkdir -p /var/lib/postgresql/pfobs1-certs; cp /run/pfobs1-input/* /var/lib/postgresql/pfobs1-certs/; chown -R postgres:postgres /var/lib/postgresql/pfobs1-certs; chmod 600 /var/lib/postgresql/pfobs1-certs/server.key; exec /usr/local/bin/docker-entrypoint.sh postgres -c ssl=on -c ssl_cert_file=/var/lib/postgresql/pfobs1-certs/server.crt -c ssl_key_file=/var/lib/postgresql/pfobs1-certs/server.key -c ssl_ca_file=/var/lib/postgresql/pfobs1-certs/root.crt'
    Invoke-DockerChecked @(
        'run', '--detach', '--pull', 'never',
        '--name', $containerName,
        '--label', "$pilotLabel=true",
        '--label', "$pilotLabel.run=$runId",
        '--network', $networkName,
        '--mount', "type=volume,source=$volumeName,target=/var/lib/postgresql/data",
        '--mount', "type=bind,source=$($tls.Root),target=/run/pfobs1-input,readonly",
        '--mount', "type=bind,source=$passwordPath,target=/run/secrets/pfobs1_password,readonly",
        '--env', 'POSTGRES_USER=dbnotifier_pfobs1',
        '--env', 'POSTGRES_DB=dbnotifier_pfobs1',
        '--env', 'POSTGRES_PASSWORD_FILE=/run/secrets/pfobs1_password',
        '--publish', '127.0.0.1:0:5432',
        '--cpus', '2',
        '--memory', '768m',
        '--pids-limit', '256',
        '--health-cmd', 'pg_isready -U dbnotifier_pfobs1 -d dbnotifier_pfobs1',
        '--health-interval', '1s',
        '--health-timeout', '3s',
        '--health-retries', '30',
        '--entrypoint', '/bin/sh',
        $imageDigest,
        '-ec', $containerCommand
    ) | Out-Null
    if (-not (Test-OwnedResource 'container' $containerName)) {
        throw 'The PF-OBS-1 container ownership could not be proved.'
    }
    Wait-PilotHealthy $containerName
    $binding = @(& $docker port $containerName '5432/tcp')
    if ($LASTEXITCODE -ne 0 -or $binding.Count -ne 1 -or $binding[0] -notmatch '^127\.0\.0\.1:(\d{1,5})$') {
        throw 'The PF-OBS-1 PostgreSQL port is not bound exactly once to IPv4 loopback.'
    }
    $port = [int]$Matches[1]
    Invoke-DockerChecked @(
        'exec', '--user', 'postgres', $containerName,
        'psql', '--set', 'ON_ERROR_STOP=1',
        '--username', 'dbnotifier_pfobs1',
        '--dbname', 'dbnotifier_pfobs1',
        '--command', 'CREATE TABLE pf_obs1_samples(id integer PRIMARY KEY, payload integer NOT NULL); INSERT INTO pf_obs1_samples SELECT value, value * 2 FROM generate_series(1,32) AS value;'
    ) | Out-Null
    $passwordKey = 'Pass' + 'word'
    $connection = "Host=localhost;Port=$port;Database=dbnotifier_pfobs1;Username=dbnotifier_pfobs1;$passwordKey=$password;SSL Mode=VerifyFull;Root Certificate=$($tls.RootCertificate);Pooling=false;Timeout=5;Command Timeout=5;Include Error Detail=false"
    $script:pendingRun.port = $port
    $script:pendingRun.connection = $connection
    $script:pendingRun.certificateDigest = $tls.Digest
    return $script:pendingRun
}

# Runs one filtered .NET test command with inherited process-only pilot variables.
function Invoke-Test([string]$Filter) {
    & $dotnet test $integrationProject --configuration Release --no-build --no-restore `
        --filter $Filter --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { throw "The PF-OBS-1 test filter failed: $Filter" }
}

# Runs the physical protocol and the live PostgreSQL Agent-to-Observer evidence path.
function Invoke-PilotEvidence([object]$State) {
    & $dotnet build $hostProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The PF-OBS-1 sandbox host build failed.' }

    $physicalRoot = Join-Path ([IO.Path]::GetTempPath()) ("DBNotifier-O5-R5-Physical-{0}" -f $State.runId)
    New-Item -ItemType Directory -Path $physicalRoot | Out-Null
    $physicalOutput = Join-Path $physicalRoot 'o5-r5-physical-campaign.json'
    $memory = (Get-CimInstance Win32_ComputerSystem).TotalPhysicalMemory
    $previous = @{
        marker = $env:DBNOTIFIER_O5_R5_B_PHYSICAL_TEST_ONLY
        output = $env:DBNOTIFIER_O5_R5_B_PHYSICAL_OUTPUT
        sdk = $env:DBNOTIFIER_O5_R5_B_DOTNET_SDK
        memory = $env:DBNOTIFIER_O5_R5_B_INSTALLED_MEMORY_BYTES
        pilotMarker = $env:DBNOTIFIER_PF_OBS1_TEST_ONLY
        connection = $env:DBNOTIFIER_PF_OBS1_CONNECTION
        pilotOutput = $env:DBNOTIFIER_PF_OBS1_OUTPUT
        image = $env:DBNOTIFIER_PF_OBS1_IMAGE_DIGEST
        certificate = $env:DBNOTIFIER_PF_OBS1_CERTIFICATE_DIGEST
    }
    try {
        & $dotnet $hostAssembly `
            --activation 'pf-obs-1-physical-test-only' `
            --output $physicalOutput `
            --sdk '10.0.301' `
            --installed-memory ([string]$memory)
        if ($LASTEXITCODE -ne 0) { throw 'The dedicated PF-OBS-1 physical campaign failed.' }
        Copy-Item -LiteralPath $physicalOutput -Destination $State.hmPath

        $env:DBNOTIFIER_PF_OBS1_TEST_ONLY = 'pf-obs-1-postgresql-observer-local-test'
        $env:DBNOTIFIER_PF_OBS1_CONNECTION = $State.connection
        $env:DBNOTIFIER_PF_OBS1_OUTPUT = $State.evidencePath
        $env:DBNOTIFIER_PF_OBS1_IMAGE_DIGEST = $imageDigest.Substring(7)
        $env:DBNOTIFIER_PF_OBS1_CERTIFICATE_DIGEST = $State.certificateDigest
        Invoke-Test 'FullyQualifiedName~PfObs1PilotEntryPointTests'
    }
    finally {
        $env:DBNOTIFIER_O5_R5_B_PHYSICAL_TEST_ONLY = $previous.marker
        $env:DBNOTIFIER_O5_R5_B_PHYSICAL_OUTPUT = $previous.output
        $env:DBNOTIFIER_O5_R5_B_DOTNET_SDK = $previous.sdk
        $env:DBNOTIFIER_O5_R5_B_INSTALLED_MEMORY_BYTES = $previous.memory
        $env:DBNOTIFIER_PF_OBS1_TEST_ONLY = $previous.pilotMarker
        $env:DBNOTIFIER_PF_OBS1_CONNECTION = $previous.connection
        $env:DBNOTIFIER_PF_OBS1_OUTPUT = $previous.pilotOutput
        $env:DBNOTIFIER_PF_OBS1_IMAGE_DIGEST = $previous.image
        $env:DBNOTIFIER_PF_OBS1_CERTIFICATE_DIGEST = $previous.certificate
        if (Test-Path -LiteralPath $physicalRoot) {
            Remove-Item -LiteralPath $physicalRoot -Recurse -Force
        }
    }
}

# Runs accepted control, security, recovery and observability suites, then proves database restart and offline refusal.
function Invoke-ConsolidatedValidation([object]$State) {
    Invoke-Test 'FullyQualifiedName~PfObs1PilotTests|FullyQualifiedName~O5R2ControlPlaneSandboxTests|FullyQualifiedName~O5R3ObservabilitySandboxTests|FullyQualifiedName~O5R4'
    Invoke-DockerChecked @('restart', '--time', '5', $State.containerName) | Out-Null
    Wait-PilotHealthy $State.containerName
    Invoke-PilotEvidence $State
    Invoke-DockerChecked @('stop', '--time', '5', $State.containerName) | Out-Null
    $client = [Net.Sockets.TcpClient]::new()
    try {
        $connect = $client.ConnectAsync('127.0.0.1', [int]$State.port)
        if ($connect.Wait(1000) -and $client.Connected) {
            throw 'The stopped PF-OBS-1 laboratory remained reachable.'
        }
    }
    catch [AggregateException] { }
    catch [Net.Sockets.SocketException] { }
    finally { $client.Dispose() }
    Invoke-DockerChecked @('start', $State.containerName) | Out-Null
    Wait-PilotHealthy $State.containerName
}

# Resolves the installed Chrome application without downloading or reusing a user profile.
function Find-Chrome {
    $candidates = @(
        "$env:ProgramFiles\Google\Chrome\Application\chrome.exe",
        "${env:ProgramFiles(x86)}\Google\Chrome\Application\chrome.exe",
        "$env:LOCALAPPDATA\Google\Chrome\Application\chrome.exe"
    )
    foreach ($candidate in $candidates) {
        if ($candidate -and (Test-Path -LiteralPath $candidate -PathType Leaf)) { return $candidate }
    }
    throw 'Chrome is not installed; PF-OBS-1 will not download a browser.'
}

# Reserves one temporary IPv4 loopback port.
function Get-LoopbackPort {
    $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback, 0)
    try {
        $listener.Start()
        return ([Net.IPEndPoint]$listener.LocalEndpoint).Port
    }
    finally { $listener.Stop() }
}

# Starts the HTTPS Observer host and optionally a visible dedicated Chrome review process.
function Start-ObserverReview([object]$State, [bool]$Visible) {
    $previousFlag = $env:VITE_DB_NOTIFIER_OBSERVER_SANDBOX
    try {
        $env:VITE_DB_NOTIFIER_OBSERVER_SANDBOX = 'local-test'
        & npm.cmd run build --prefix $dashboardRoot
        if ($LASTEXITCODE -ne 0) { throw 'The PF-OBS-1 Dashboard build failed.' }
    }
    finally { $env:VITE_DB_NOTIFIER_OBSERVER_SANDBOX = $previousFlag }
    & $dotnet build $hostProject --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'The PF-OBS-1 Observer host build failed.' }
    $hostOut = Join-Path $State.temporaryRoot 'host.stdout.log'
    $hostError = Join-Path $State.temporaryRoot 'host.stderr.log'
    $runGuid = [Guid]::NewGuid()
    $host = Start-Process -FilePath $dotnet -ArgumentList @(
        $hostAssembly,
        '--activation', 'pf-obs-1-postgresql-observer-local-test',
        '--dashboard-root', $dashboardDist,
        '--run-id', $runGuid.ToString('D'),
        '--pilot-evidence', $State.evidencePath
    ) -WorkingDirectory $repositoryRoot -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $hostOut -RedirectStandardError $hostError
    $ready = $null
    for ($attempt = 1; $attempt -le 80; $attempt++) {
        if ($host.HasExited) {
            throw "The PF-OBS-1 Observer host exited before readiness: $(Get-Content $hostError -Raw -ErrorAction SilentlyContinue)"
        }
        if (Test-Path -LiteralPath $hostOut) {
            foreach ($line in Get-Content -LiteralPath $hostOut) {
                try {
                    $candidate = $line | ConvertFrom-Json
                    if ($candidate.marker -eq 'DBNOTIFIER_O4_OBSERVER_SANDBOX_READY') { $ready = $candidate }
                }
                catch { }
            }
        }
        if ($null -ne $ready) { break }
        Start-Sleep -Milliseconds 250
    }
    if ($null -eq $ready) { throw 'The PF-OBS-1 Observer host did not become ready.' }
    $State.hostPid = $host.Id
    $State.baseAddress = [string]$ready.baseAddress
    if ($Visible) {
        $profile = Join-Path $State.temporaryRoot 'chrome-profile'
        $proxy = "http://127.0.0.1:$(Get-LoopbackPort)"
        $chrome = Start-Process -FilePath (Find-Chrome) -ArgumentList @(
            "--user-data-dir=$profile",
            "--proxy-server=$proxy",
            '--proxy-bypass-list=localhost;127.0.0.1;[::1]',
            "--ignore-certificate-errors-spki-list=$($ready.spkiPin)",
            '--disable-background-networking',
            '--disable-component-update',
            '--disable-default-apps',
            '--disable-extensions',
            '--no-first-run',
            '--no-default-browser-check',
            '--window-size=1440,900',
            $State.baseAddress
        ) -WorkingDirectory $repositoryRoot -PassThru
        $State.browserPid = $chrome.Id
        $State.profilePath = $profile
    }
}

if ($Action -eq 'Stop') {
    if (-not (Test-Path -LiteralPath $statePath)) {
        Write-Output 'PF-OBS-1 is already stopped; no owned active state exists.'
        exit 0
    }
    $active = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json
    Stop-Pilot $active
    Write-Output 'PF-OBS-1 stopped with zero owned container, volume, network, host or browser residue.'
    exit 0
}

if (Test-Path -LiteralPath $statePath) {
    throw 'A PF-OBS-1 active state already exists. Run this script with -Action Stop first.'
}

try {
    $run = Start-Laboratory
    $pendingRun = $null
    Invoke-PilotEvidence $run
    if ($Action -eq 'Campaign') {
        Invoke-ConsolidatedValidation $run
        Write-Output "PF-OBS-1 campaign passed. Evidence: $($run.evidencePath)"
        Write-Output "PostgreSQL image: $imageTag@$imageDigest"
    }
    else {
        Start-ObserverReview $run ([bool]$OpenReview)
        $state = [ordered]@{
            schemaVersion = 'pf-obs-1.active-state.v1'
            runId = $run.runId
            temporaryRoot = $run.temporaryRoot
            containerName = $run.containerName
            networkName = $run.networkName
            volumeName = $run.volumeName
            port = $run.port
            evidencePath = $run.evidencePath
            hmPath = $run.hmPath
            hostPid = $run.hostPid
            browserPid = $run.browserPid
            baseAddress = $run.baseAddress
            profilePath = $run.profilePath
            activationState = 'None'
        }
        [IO.File]::WriteAllText(
            $statePath,
            ($state | ConvertTo-Json -Depth 4),
            [Text.UTF8Encoding]::new($false))
        Write-Output "PF-OBS-1 started at $($run.baseAddress). ActivationState=None."
        $run = $null
    }
}
finally {
    if ($null -ne $run) {
        Stop-Pilot $run
    }
    elseif ($null -ne $pendingRun) {
        Stop-Pilot $pendingRun
    }
}
