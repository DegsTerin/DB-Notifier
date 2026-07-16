# Module purpose: Verifies local API authentication boundaries and the Agent's disabled-by-default runtime without external connections.
[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$DotNetPath
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($DotNetPath)) {
    $DotNetPath = Join-Path $root '.dotnet\dotnet.exe'
}
Add-Type -AssemblyName System.Net.Http
$dotnetCommand = if (Test-Path -LiteralPath $DotNetPath -PathType Leaf) {
    (Resolve-Path -LiteralPath $DotNetPath).Path
} else {
    (Get-Command $DotNetPath -CommandType Application -ErrorAction Stop).Source
}
$temporaryRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-Runtime-Audit-{0}" -f [Guid]::NewGuid().ToString('N'))
$apiProcess = $null
$agentProcess = $null

# Reserves an ephemeral loopback port and releases the reservation before Kestrel starts.
function Get-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port }
    finally { $listener.Stop() }
}

# Stops only the process tree created by this audit, waits for redirected streams to close and releases the process handle.
function Stop-ProcessTree([System.Diagnostics.Process]$Process) {
    if ($null -eq $Process) { return }
    try {
        if (-not $Process.HasExited) {
            & taskkill.exe /PID $Process.Id /T /F 2>$null | Out-Null
            if (-not $Process.WaitForExit(5000)) {
                throw "The audit process $($Process.Id) did not exit within the cleanup timeout."
            }
        }
    }
    finally {
        $Process.Dispose()
    }
}

# Repeats a bounded local HTTP request until the exact fail-closed status is observed.
function Wait-ForStatus([System.Net.Http.HttpClient]$Client, [System.Net.Http.HttpMethod]$Method, [string]$Uri, [int]$ExpectedStatus, [int]$Attempts = 40) {
    for ($attempt = 1; $attempt -le $Attempts; $attempt++) {
        try {
            $request = [System.Net.Http.HttpRequestMessage]::new($Method, $Uri)
            try { $response = $Client.SendAsync($request).GetAwaiter().GetResult() }
            finally { $request.Dispose() }
            try {
                if ([int]$response.StatusCode -eq $ExpectedStatus) { return }
                throw "Expected HTTP $ExpectedStatus but received $([int]$response.StatusCode) for $Uri."
            }
            finally { $response.Dispose() }
        }
        catch {
            if ($attempt -ge $Attempts) { throw }
            Start-Sleep -Milliseconds 250
        }
    }
    throw "The endpoint $Uri did not become available with HTTP $ExpectedStatus."
}

New-Item -ItemType Directory -Path $temporaryRoot | Out-Null
try {
    $port = Get-AvailableLoopbackPort
    $baseAddress = "http://127.0.0.1:$port"
    $apiDll = Join-Path $root "src\DBNotifier.Server.Api\bin\$Configuration\net10.0\DBNotifier.Server.Api.dll"
    $agentDll = Join-Path $root "src\DBNotifier.Agent.Worker\bin\$Configuration\net10.0\DBNotifier.Agent.Worker.dll"
    foreach ($requiredPath in @($apiDll, $agentDll)) {
        if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) { throw "Required audit artefact is missing: $requiredPath" }
    }

    $apiProcess = Start-Process -FilePath $dotnetCommand -ArgumentList @($apiDll, '--urls', $baseAddress) -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot 'api.stdout.log') -RedirectStandardError (Join-Path $temporaryRoot 'api.stderr.log')
    $client = [System.Net.Http.HttpClient]::new()
    try {
        Wait-ForStatus $client ([System.Net.Http.HttpMethod]::Get) "$baseAddress/health/live" 200
        Wait-ForStatus $client ([System.Net.Http.HttpMethod]::Get) "$baseAddress/api/v1/catalog/instances" 426 1
        Wait-ForStatus $client ([System.Net.Http.HttpMethod]::Get) "$baseAddress/api/v1/audit" 426 1
        $agentId = [Guid]::NewGuid().ToString('D')
        Wait-ForStatus $client ([System.Net.Http.HttpMethod]::Post) "$baseAddress/api/v1/agents/$agentId/commands:poll" 426 1
    }
    finally { $client.Dispose() }

    $databasePath = Join-Path $temporaryRoot 'agent.db'
    $previousDatabasePath = $env:DBNotifier__Agent__DatabasePath
    $env:DBNotifier__Agent__DatabasePath = $databasePath
    try {
        $agentProcess = Start-Process -FilePath $dotnetCommand -ArgumentList @($agentDll) -WorkingDirectory $root -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $temporaryRoot 'agent.stdout.log') -RedirectStandardError (Join-Path $temporaryRoot 'agent.stderr.log')
        Start-Sleep -Seconds 2
        if ($agentProcess.HasExited) { throw "The fail-closed Agent exited unexpectedly with code $($agentProcess.ExitCode)." }
        $agentLogPath = Join-Path $temporaryRoot 'agent.stdout.log'
        $expectedDisabledMessages = @(
            'Agent monitoring is disabled by configuration',
            'Agent synchronization is disabled by configuration',
            'Administrative command polling is disabled',
            'Agent retention is disabled by configuration'
        )
        $agentLog = if (Test-Path -LiteralPath $agentLogPath) { Get-Content -LiteralPath $agentLogPath -Raw -ErrorAction SilentlyContinue } else { '' }
        foreach ($expectedMessage in $expectedDisabledMessages) {
            if ($agentLog -notlike "*$expectedMessage*") {
                throw "The fail-closed Agent did not log its expected disabled worker state: $expectedMessage"
            }
        }
        if (Test-Path -LiteralPath $databasePath) {
            throw "The disabled Agent initialised local persistence unexpectedly."
        }
    }
    finally { $env:DBNotifier__Agent__DatabasePath = $previousDatabasePath }

    Write-Output 'Fail-closed runtime audit passed: live=200, all protected HTTP endpoints=426, all Agent workers logged disabled and local persistence was not initialised.'
}
finally {
    Stop-ProcessTree $agentProcess
    Stop-ProcessTree $apiProcess
    $resolvedTemp = [System.IO.Path]::GetFullPath($temporaryRoot)
    $systemTemp = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath())
    if ($resolvedTemp.StartsWith($systemTemp, [StringComparison]::OrdinalIgnoreCase) -and (Test-Path -LiteralPath $resolvedTemp)) {
        Remove-Item -LiteralPath $resolvedTemp -Recurse -Force
    }
}
