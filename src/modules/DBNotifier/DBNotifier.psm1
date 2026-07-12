Set-StrictMode -Version Latest

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Set-RoundedRegion {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][System.Windows.Forms.Control]$Control,
        [int]$Radius = 12
    )

    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $width = $Control.Width - 1
    $height = $Control.Height - 1
    $diameter = $Radius * 2

    $path.AddArc(0, 0, $diameter, $diameter, 180, 90)
    $path.AddArc($width - $diameter, 0, $diameter, $diameter, 270, 90)
    $path.AddArc($width - $diameter, $height - $diameter, $diameter, $diameter, 0, 90)
    $path.AddArc(0, $height - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()

    if ($Control.Region) {
        $Control.Region.Dispose()
    }
    $Control.Region = New-Object System.Drawing.Region($path)
    $path.Dispose()
}

function ConvertTo-Hashtable {
    [CmdletBinding()]
    param($InputObject)

    if ($null -eq $InputObject) {
        return $null
    }

    if ($InputObject -is [string] -or
        $InputObject -is [char] -or
        $InputObject -is [bool] -or
        $InputObject -is [byte] -or
        $InputObject -is [int16] -or
        $InputObject -is [int32] -or
        $InputObject -is [int64] -or
        $InputObject -is [decimal] -or
        $InputObject -is [double] -or
        $InputObject -is [single] -or
        $InputObject -is [datetime]) {
        return $InputObject
    }

    if ($InputObject -is [System.Collections.IDictionary]) {
        $hash = @{}
        foreach ($key in $InputObject.Keys) {
            $hash[$key] = ConvertTo-Hashtable -InputObject $InputObject[$key]
        }
        return $hash
    }

    if ($InputObject -is [System.Collections.IEnumerable] -and $InputObject -isnot [string]) {
        $items = @()
        foreach ($item in $InputObject) {
            $items += ,(ConvertTo-Hashtable -InputObject $item)
        }
        return $items
    }

    $properties = @($InputObject.PSObject.Properties | Where-Object { $_.MemberType -eq "NoteProperty" -or $_.MemberType -eq "Property" })
    if ($properties.Count -gt 0) {
        $hash = @{}
        foreach ($property in $properties) {
            $hash[$property.Name] = ConvertTo-Hashtable -InputObject $property.Value
        }
        return $hash
    }

    return $InputObject
}

function Merge-Hashtable {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Base,
        [Parameter(Mandatory)][hashtable]$Override
    )

    foreach ($key in $Override.Keys) {
        if ($Base.ContainsKey($key) -and $Base[$key] -is [hashtable] -and $Override[$key] -is [hashtable]) {
            $null = Merge-Hashtable -Base $Base[$key] -Override $Override[$key]
            continue
        }

        $Base[$key] = $Override[$key]
    }

    return $Base
}

function Get-Value {
    param($Object, [string]$Name, $DefaultValue = $null)

    if ($null -eq $Object) {
        return $DefaultValue
    }

    if ($Object -is [System.Collections.IDictionary]) {
        if ($Object.ContainsKey($Name)) {
            return $Object[$Name]
        }
        return $DefaultValue
    }

    $property = $Object.PSObject.Properties[$Name]
    if ($null -eq $property) {
        return $DefaultValue
    }

    return $property.Value
}

function New-DefaultConfiguration {
    [CmdletBinding()]
    param()

    return @{
        application = @{
            displayName         = "DB-Notifier"
            intervalSeconds     = 5
            restartBadgeSeconds = 10
            startMinimized      = $true
            autoDiscover        = $true
            silentMode          = $false
        }
        logging = @{
            logPath = "%LocalAppData%\DB-Notifier\logs\dbnotifier.log"
            level   = "INFO"
        }
        notifications = @{
            enabled                 = $true
            suppressStartupBalloon  = $false
            defaultBalloonTimeoutMs = 3000
        }
        pgIsReady = @{
            path           = "pg_isready.exe"
            timeoutSeconds = 5
            retryCount     = 1
            retryDelayMs   = 500
            extraArguments = @()
        }
        instances = @()
    }
}

function Resolve-ConfiguredPath {
    [CmdletBinding()]
    param(
        [string]$Candidate,
        [string]$ConfigPath
    )

    if ([string]::IsNullOrWhiteSpace($Candidate)) {
        return $Candidate
    }

    $expanded = [System.Environment]::ExpandEnvironmentVariables($Candidate)
    if ([System.IO.Path]::IsPathRooted($expanded)) {
        return [System.IO.Path]::GetFullPath($expanded)
    }

    $basePath = Split-Path -Path $ConfigPath -Parent
    if ([string]::IsNullOrWhiteSpace($basePath)) {
        $basePath = (Get-Location).Path
    }

    return [System.IO.Path]::GetFullPath((Join-Path -Path $basePath -ChildPath $expanded))
}

function Resolve-WritableLogPath {
    [CmdletBinding()]
    param([string]$CandidatePath)

    $fallback = Join-Path -Path $env:LocalAppData -ChildPath "DB-Notifier\logs\dbnotifier.log"
    $path = if ([string]::IsNullOrWhiteSpace($CandidatePath)) { $fallback } else { [System.Environment]::ExpandEnvironmentVariables($CandidatePath) }

    try {
        $directory = Split-Path -Path $path -Parent
        if (-not [string]::IsNullOrWhiteSpace($directory)) {
            New-Item -Path $directory -ItemType Directory -Force | Out-Null
        }

        Add-Content -LiteralPath $path -Value "" -Encoding UTF8 -ErrorAction Stop
        return [pscustomobject]@{ Path = [System.IO.Path]::GetFullPath($path); UsedFallback = $false }
    }
    catch {
        $directory = Split-Path -Path $fallback -Parent
        New-Item -Path $directory -ItemType Directory -Force | Out-Null
        Add-Content -LiteralPath $fallback -Value "" -Encoding UTF8 -ErrorAction SilentlyContinue
        return [pscustomobject]@{ Path = [System.IO.Path]::GetFullPath($fallback); UsedFallback = $true }
    }
}

function Write-AppLog {
    [CmdletBinding()]
    param(
        [hashtable]$Context,
        [string]$Message,
        [ValidateSet("DEBUG", "INFO", "WARN", "ERROR")]
        [string]$Level = "INFO"
    )

    try {
        $path = $Context.Configuration.Logging.LogPath
        $line = "{0} [{1}] {2}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Level, $Message
        Add-Content -LiteralPath $path -Value $line -Encoding UTF8 -ErrorAction Stop
    }
    catch {
    }
}

function ConvertTo-NormalizedConfiguration {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Configuration,
        [Parameter(Mandatory)][string]$ConfigPath
    )

    $instances = @()
    foreach ($instance in @((Get-Value -Object $Configuration -Name "instances" -DefaultValue @()))) {
        $name = [string](Get-Value -Object $instance -Name "name" -DefaultValue "")
        $serviceName = [string](Get-Value -Object $instance -Name "serviceName" -DefaultValue "")
        $hostName = [string](Get-Value -Object $instance -Name "hostName" -DefaultValue "localhost")
        $port = [int](Get-Value -Object $instance -Name "port" -DefaultValue 5432)
        if ([string]::IsNullOrWhiteSpace($serviceName) -and [string]::IsNullOrWhiteSpace($hostName)) {
            continue
        }

        $postgresExe = [string](Get-Value -Object $instance -Name "postgresExe" -DefaultValue "")
        $instanceKey = if (-not [string]::IsNullOrWhiteSpace($serviceName)) { $serviceName } else { "{0}:{1}" -f $hostName, $port }
        $instances += ,([pscustomobject]@{
            InstanceKey          = $instanceKey
            Name                 = if ([string]::IsNullOrWhiteSpace($name)) { $instanceKey } else { $name }
            ServiceName          = $serviceName
            HostName             = if ([string]::IsNullOrWhiteSpace($hostName)) { "localhost" } else { $hostName }
            Port                 = $port
            PostgresExe          = if ([string]::IsNullOrWhiteSpace($postgresExe)) { $null } else { Resolve-ConfiguredPath -Candidate $postgresExe -ConfigPath $ConfigPath }
            Enabled              = [bool](Get-Value -Object $instance -Name "enabled" -DefaultValue $true)
            NotificationsEnabled = [bool](Get-Value -Object $instance -Name "notificationsEnabled" -DefaultValue $true)
            RestartAllowed       = [bool](Get-Value -Object $instance -Name "restartAllowed" -DefaultValue (-not [string]::IsNullOrWhiteSpace($serviceName)))
            IsLocalService       = -not [string]::IsNullOrWhiteSpace($serviceName)
        })
    }

    $application = Get-Value -Object $Configuration -Name "application" -DefaultValue @{}
    $logging = Get-Value -Object $Configuration -Name "logging" -DefaultValue @{}
    $notifications = Get-Value -Object $Configuration -Name "notifications" -DefaultValue @{}
    $pgIsReady = Get-Value -Object $Configuration -Name "pgIsReady" -DefaultValue @{}
    $logResolution = Resolve-WritableLogPath -CandidatePath (Resolve-ConfiguredPath -Candidate ([string](Get-Value -Object $logging -Name "logPath" -DefaultValue "%LocalAppData%\DB-Notifier\logs\dbnotifier.log")) -ConfigPath $ConfigPath)

    return [pscustomobject]@{
        ConfigPath    = [System.IO.Path]::GetFullPath($ConfigPath)
        Application   = [pscustomobject]@{
            DisplayName         = [string](Get-Value -Object $application -Name "displayName" -DefaultValue "DB-Notifier")
            IntervalSeconds     = [Math]::Max(1, [int](Get-Value -Object $application -Name "intervalSeconds" -DefaultValue 5))
            RestartBadgeSeconds = [Math]::Max(1, [int](Get-Value -Object $application -Name "restartBadgeSeconds" -DefaultValue 10))
            StartMinimized      = [bool](Get-Value -Object $application -Name "startMinimized" -DefaultValue $true)
            AutoDiscover        = [bool](Get-Value -Object $application -Name "autoDiscover" -DefaultValue $true)
            SilentMode          = [bool](Get-Value -Object $application -Name "silentMode" -DefaultValue $false)
        }
        Logging       = [pscustomobject]@{
            LogPath      = $logResolution.Path
            Level        = [string](Get-Value -Object $logging -Name "level" -DefaultValue "INFO")
            UsedFallback = [bool]$logResolution.UsedFallback
        }
        Notifications = [pscustomobject]@{
            Enabled                 = [bool](Get-Value -Object $notifications -Name "enabled" -DefaultValue $true)
            SuppressStartupBalloon  = [bool](Get-Value -Object $notifications -Name "suppressStartupBalloon" -DefaultValue $false)
            DefaultBalloonTimeoutMs = [int](Get-Value -Object $notifications -Name "defaultBalloonTimeoutMs" -DefaultValue 3000)
        }
        PgIsReady     = [pscustomobject]@{
            Path           = [string](Get-Value -Object $pgIsReady -Name "path" -DefaultValue "pg_isready.exe")
            TimeoutSeconds = [Math]::Max(1, [int](Get-Value -Object $pgIsReady -Name "timeoutSeconds" -DefaultValue 5))
            RetryCount     = [Math]::Max(0, [int](Get-Value -Object $pgIsReady -Name "retryCount" -DefaultValue 1))
            RetryDelayMs   = [Math]::Max(0, [int](Get-Value -Object $pgIsReady -Name "retryDelayMs" -DefaultValue 500))
            ExtraArguments = @((Get-Value -Object $pgIsReady -Name "extraArguments" -DefaultValue @()))
        }
        Instances     = @($instances)
    }
}

function Get-JsonConfiguration {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$Path)

    $resolvedPath = [System.IO.Path]::GetFullPath($Path)
    $configuration = New-DefaultConfiguration

    if (Test-Path -LiteralPath $resolvedPath -PathType Leaf) {
        try {
            $content = Get-Content -LiteralPath $resolvedPath -Raw -Encoding UTF8
            if (-not [string]::IsNullOrWhiteSpace($content)) {
                $parsed = ConvertTo-Hashtable -InputObject (ConvertFrom-Json -InputObject $content)
                $configuration = Merge-Hashtable -Base $configuration -Override $parsed
            }
        }
        catch {
            $configuration = New-DefaultConfiguration
        }
    }

    return ConvertTo-NormalizedConfiguration -Configuration $configuration -ConfigPath $resolvedPath
}

function Resolve-ExecutablePath {
    [CmdletBinding()]
    param(
        [string]$Candidate,
        [string[]]$BasePaths = @(),
        [string[]]$AdditionalCandidates = @()
    )

    $candidates = @()
    if (-not [string]::IsNullOrWhiteSpace($Candidate)) {
        $expanded = [System.Environment]::ExpandEnvironmentVariables($Candidate)
        $candidates += $expanded

        if (-not [System.IO.Path]::IsPathRooted($expanded)) {
            foreach ($basePath in @($BasePaths)) {
                if (-not [string]::IsNullOrWhiteSpace($basePath)) {
                    $candidates += (Join-Path -Path $basePath -ChildPath $expanded)
                }
            }
        }
    }

    $candidates += @($AdditionalCandidates)

    foreach ($candidatePath in $candidates) {
        if ([string]::IsNullOrWhiteSpace($candidatePath)) {
            continue
        }

        try {
            if (Test-Path -LiteralPath $candidatePath -PathType Leaf) {
                return [System.IO.Path]::GetFullPath($candidatePath)
            }
        }
        catch {
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($Candidate) -and -not [System.IO.Path]::IsPathRooted($Candidate)) {
        $command = Get-Command -Name $Candidate -CommandType Application -ErrorAction SilentlyContinue
        if ($command) {
            return $command.Source
        }
    }

    return $null
}

function Get-PostgreSqlBinDirectoryCandidates {
    [CmdletBinding()]
    param([Parameter(Mandatory)][pscustomobject]$Configuration)

    $directories = New-Object System.Collections.Generic.List[string]
    $add = {
        param([string]$Path)
        if (-not [string]::IsNullOrWhiteSpace($Path) -and -not $directories.Contains($Path) -and (Test-Path -LiteralPath $Path -PathType Container)) {
            [void]$directories.Add($Path)
        }
    }

    foreach ($instance in @($Configuration.Instances)) {
        if (-not [string]::IsNullOrWhiteSpace($instance.PostgresExe) -and (Test-Path -LiteralPath $instance.PostgresExe -PathType Leaf)) {
            & $add (Split-Path -Path $instance.PostgresExe -Parent)
        }
    }

    $roots = @()
    if (-not [string]::IsNullOrWhiteSpace($env:ProgramFiles)) {
        $roots += (Join-Path -Path $env:ProgramFiles -ChildPath "PostgreSQL")
    }
    $programFilesX86 = [System.Environment]::GetEnvironmentVariable("ProgramFiles(x86)")
    if (-not [string]::IsNullOrWhiteSpace($programFilesX86)) {
        $roots += (Join-Path -Path $programFilesX86 -ChildPath "PostgreSQL")
    }

    foreach ($root in $roots) {
        if (-not (Test-Path -LiteralPath $root -PathType Container)) {
            continue
        }

        foreach ($versionDirectory in @(Get-ChildItem -LiteralPath $root -Directory -ErrorAction SilentlyContinue)) {
            & $add (Join-Path -Path $versionDirectory.FullName -ChildPath "bin")
        }
    }

    return @($directories)
}

function Resolve-PgIsReadyPath {
    [CmdletBinding()]
    param([Parameter(Mandatory)][pscustomobject]$Configuration)

    $candidate = $Configuration.PgIsReady.Path
    if ([string]::IsNullOrWhiteSpace($candidate)) {
        $candidate = "pg_isready.exe"
    }

    $configDirectory = Split-Path -Path $Configuration.ConfigPath -Parent
    $additional = @()
    foreach ($binDirectory in (Get-PostgreSqlBinDirectoryCandidates -Configuration $Configuration)) {
        $additional += (Join-Path -Path $binDirectory -ChildPath "pg_isready.exe")
        if (-not [System.IO.Path]::IsPathRooted($candidate)) {
            $additional += (Join-Path -Path $binDirectory -ChildPath $candidate)
        }
    }

    return Resolve-ExecutablePath -Candidate $candidate -BasePaths @($configDirectory) -AdditionalCandidates $additional
}

function Get-PostgreSqlServicePort {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ServiceName)

    foreach ($path in @("HKLM:\SOFTWARE\PostgreSQL\Services\$ServiceName", "HKLM:\SOFTWARE\WOW6432Node\PostgreSQL\Services\$ServiceName")) {
        try {
            if (Test-Path -LiteralPath $path) {
                $item = Get-ItemProperty -LiteralPath $path -ErrorAction Stop
                if ($item.PSObject.Properties.Name -contains "Port") {
                    return [int]$item.Port
                }
            }
        }
        catch {
        }
    }

    return 5432
}

function Get-PostgreSqlWindowsServices {
    [CmdletBinding()]
    param()

    try {
        $services = Get-CimInstance -ClassName Win32_Service -ErrorAction Stop |
            Where-Object { $_.Name -match "^postgresql" -or $_.DisplayName -match "PostgreSQL" }
    }
    catch {
        return @()
    }

    $result = @()
    foreach ($service in @($services)) {
        $result += ,([pscustomobject]@{
            Name        = [string]$service.Name
            DisplayName = [string]$service.DisplayName
            Port        = Get-PostgreSqlServicePort -ServiceName ([string]$service.Name)
            HostName    = "localhost"
            State       = [string]$service.State
            ProcessId   = [int]$service.ProcessId
            PathName    = [string]$service.PathName
        })
    }

    return $result
}

function Resolve-ConfiguredInstances {
    [CmdletBinding()]
    param([Parameter(Mandatory)][pscustomobject]$Configuration)

    $resolved = @()
    $seen = @{}

    foreach ($instance in @($Configuration.Instances)) {
        if (-not $instance.Enabled -or [string]::IsNullOrWhiteSpace($instance.InstanceKey)) {
            continue
        }

        if (-not $seen.ContainsKey($instance.InstanceKey)) {
            $resolved += ,$instance
            $seen[$instance.InstanceKey] = $true
        }
    }

    if ($Configuration.Application.AutoDiscover) {
        foreach ($service in (Get-PostgreSqlWindowsServices)) {
            if ($seen.ContainsKey($service.Name)) {
                continue
            }

            $resolved += ,([pscustomobject]@{
                InstanceKey          = $service.Name
                Name                 = $service.DisplayName
                ServiceName          = $service.Name
                HostName             = $service.HostName
                Port                 = $service.Port
                PostgresExe          = $null
                Enabled              = $true
                NotificationsEnabled = $true
                RestartAllowed       = $true
                IsLocalService       = $true
            })
            $seen[$service.Name] = $true
        }
    }

    return @($resolved)
}

function Get-ServiceSnapshot {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ServiceName)

    try {
        $safeName = $ServiceName.Replace("'", "''")
        $service = Get-CimInstance -ClassName Win32_Service -Filter "Name='$safeName'" -ErrorAction Stop
        if ($null -eq $service) {
            return $null
        }

        return [pscustomobject]@{
            Name        = [string]$service.Name
            DisplayName = [string]$service.DisplayName
            State       = [string]$service.State
            Status      = [string]$service.Status
            ProcessId   = [int]$service.ProcessId
        }
    }
    catch {
        return $null
    }
}

function Invoke-ProcessWithTimeout {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$FilePath,
        [string[]]$Arguments = @(),
        [int]$TimeoutSeconds = 5
    )

    $process = New-Object System.Diagnostics.Process
    $process.StartInfo.FileName = $FilePath
    $process.StartInfo.Arguments = ($Arguments | ForEach-Object { if ($_ -match "\s") { '"' + ($_ -replace '"', '\"') + '"' } else { $_ } }) -join " "
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.RedirectStandardError = $true
    $process.StartInfo.CreateNoWindow = $true

    try {
        [void]$process.Start()
        if (-not $process.WaitForExit([Math]::Max(1, $TimeoutSeconds) * 1000)) {
            try { $process.Kill() } catch {}
            return [pscustomobject]@{ ExitCode = -1; TimedOut = $true; StdOut = ""; StdErr = "Timed out" }
        }

        return [pscustomobject]@{
            ExitCode = [int]$process.ExitCode
            TimedOut = $false
            StdOut   = $process.StandardOutput.ReadToEnd()
            StdErr   = $process.StandardError.ReadToEnd()
        }
    }
    catch {
        return [pscustomobject]@{ ExitCode = -1; TimedOut = $false; StdOut = ""; StdErr = $_.Exception.Message }
    }
    finally {
        $process.Dispose()
    }
}

function Test-TcpPort {
    [CmdletBinding()]
    param(
        [string]$HostName,
        [int]$Port,
        [int]$TimeoutSeconds = 3
    )

    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $async = $client.BeginConnect($HostName, $Port, $null, $null)
        if (-not $async.AsyncWaitHandle.WaitOne([Math]::Max(1, $TimeoutSeconds) * 1000, $false)) {
            return [pscustomobject]@{ IsReady = $false; TimedOut = $true; Message = "TCP timeout" }
        }

        $client.EndConnect($async)
        return [pscustomobject]@{ IsReady = $true; TimedOut = $false; Message = "TCP connection accepted" }
    }
    catch {
        return [pscustomobject]@{ IsReady = $false; TimedOut = $false; Message = $_.Exception.Message }
    }
    finally {
        $client.Dispose()
    }
}

function Test-PgInstanceReady {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Context,
        [Parameter(Mandatory)][pscustomobject]$Instance
    )

    if ([string]::IsNullOrWhiteSpace($Context.PgIsReadyPath)) {
        $tcp = Test-TcpPort -HostName $Instance.HostName -Port $Instance.Port -TimeoutSeconds $Context.Configuration.PgIsReady.TimeoutSeconds
        return [pscustomobject]@{
            IsReady  = [bool]$tcp.IsReady
            TimedOut = [bool]$tcp.TimedOut
            ExitCode = -1
            Message  = if ($tcp.IsReady) { "TCP fallback: $($tcp.Message)" } else { "TCP fallback: $($tcp.Message)" }
            Method   = "tcp"
        }
    }

    $arguments = @("-h", $Instance.HostName, "-p", [string]$Instance.Port, "-t", [string]$Context.Configuration.PgIsReady.TimeoutSeconds) + @($Context.Configuration.PgIsReady.ExtraArguments)
    $attempt = 0
    $result = $null

    do {
        $attempt++
        $result = Invoke-ProcessWithTimeout -FilePath $Context.PgIsReadyPath -Arguments $arguments -TimeoutSeconds $Context.Configuration.PgIsReady.TimeoutSeconds
        if (-not $result.TimedOut -and $result.ExitCode -eq 0) {
            return [pscustomobject]@{ IsReady = $true; TimedOut = $false; ExitCode = 0; Message = $result.StdOut.Trim(); Method = "pg_isready" }
        }

        if ($attempt -le $Context.Configuration.PgIsReady.RetryCount) {
            Start-Sleep -Milliseconds $Context.Configuration.PgIsReady.RetryDelayMs
        }
    } while ($attempt -le $Context.Configuration.PgIsReady.RetryCount)

    return [pscustomobject]@{
        IsReady  = $false
        TimedOut = [bool]$result.TimedOut
        ExitCode = [int]$result.ExitCode
        Message  = (($result.StdErr, $result.StdOut) -join " ").Trim()
        Method   = "pg_isready"
    }
}

function New-InstanceState {
    [CmdletBinding()]
    param([Parameter(Mandatory)][pscustomobject]$Instance)

    return [pscustomobject]@{
        InstanceKey          = $Instance.InstanceKey
        Name                 = $Instance.Name
        ServiceName          = $Instance.ServiceName
        HostName             = $Instance.HostName
        Port                 = $Instance.Port
        RestartAllowed       = [bool]$Instance.RestartAllowed
        IsLocalService       = [bool]$Instance.IsLocalService
        NotificationsEnabled = [bool]$Instance.NotificationsEnabled
        CurrentStateKey      = "UNKNOWN"
        LastStateKey         = $null
        LastPid              = $null
        LastMessage          = "Waiting for first check."
        YellowUntil          = (Get-Date).AddSeconds(-1)
    }
}

function Get-StateDisplayName {
    param([string]$StateKey)

    switch ($StateKey) {
        "UP" { "Running" }
        "UP_TCP_ONLY" { "Running" }
        "RESTARTED" { "Restarted" }
        "STOPPED" { "Stopped" }
        "RUNNING_NO_CONN" { "Connection issue" }
        "TIMEOUT" { "Timeout" }
        "MISSING" { "Missing" }
        "ERROR" { "Error" }
        default { "Checking" }
    }
}

function Test-IsHealthyState {
    param([string]$StateKey)
    return $StateKey -eq "UP" -or $StateKey -eq "UP_TCP_ONLY" -or $StateKey -eq "RESTARTED"
}

function Get-StateColour {
    param([string]$StateKey)

    switch ($StateKey) {
        "UP" { [System.Drawing.Color]::FromArgb(99, 245, 112) }
        "UP_TCP_ONLY" { [System.Drawing.Color]::FromArgb(99, 245, 112) }
        "RESTARTED" { [System.Drawing.Color]::FromArgb(255, 205, 70) }
        "STOPPED" { [System.Drawing.Color]::FromArgb(255, 102, 83) }
        "MISSING" { [System.Drawing.Color]::FromArgb(255, 102, 83) }
        "ERROR" { [System.Drawing.Color]::FromArgb(255, 102, 83) }
        default { [System.Drawing.Color]::FromArgb(255, 205, 70) }
    }
}

function New-TrayIcon {
    [CmdletBinding()]
    param([System.Drawing.Color]$BadgeColour = [System.Drawing.Color]::FromArgb(99, 245, 112))

    $bitmap = New-Object System.Drawing.Bitmap 32, 32
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $background = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(30, 48, 66))
        $green = New-Object System.Drawing.SolidBrush($BadgeColour)
        $whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 235), 2)
        $font = New-Object System.Drawing.Font("Segoe UI", 9, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
        $text = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(14, 28, 38))

        $graphics.FillRectangle($background, 1, 1, 30, 30)
        $graphics.FillEllipse($green, 6, 5, 20, 20)
        $graphics.DrawArc($whitePen, 8, 7, 8, 12, 90, 230)
        $graphics.DrawArc($whitePen, 16, 7, 8, 12, 220, 230)
        $graphics.DrawLine($whitePen, 15, 18, 15, 27)
        $graphics.DrawLine($whitePen, 15, 27, 20, 25)
        $graphics.DrawString("P", $font, $text, 10, 11)

        $background.Dispose()
        $green.Dispose()
        $whitePen.Dispose()
        $font.Dispose()
        $text.Dispose()
        $handle = $bitmap.GetHicon()
        return [System.Drawing.Icon]::FromHandle($handle).Clone()
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
}

function New-LogoPictureBox {
    [CmdletBinding()]
    param([int]$X, [int]$Y, [int]$Size = 28)

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)

    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $graphics.Clear([System.Drawing.Color]::Transparent)
        $green = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(99, 245, 112))
        $dark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(11, 24, 34))
        $whitePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(230, 255, 235), 2)
        $font = New-Object System.Drawing.Font("Segoe UI", 8, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)

        $graphics.FillEllipse($green, 3, 4, $Size - 7, $Size - 8)
        $graphics.DrawArc($whitePen, 6, 7, 8, 12, 90, 230)
        $graphics.DrawArc($whitePen, 14, 7, 8, 12, 220, 230)
        $graphics.DrawLine($whitePen, 13, 17, 13, 25)
        $graphics.DrawLine($whitePen, 13, 25, 19, 23)
        $graphics.DrawString("P", $font, $dark, 9, 10)

        $green.Dispose()
        $dark.Dispose()
        $whitePen.Dispose()
        $font.Dispose()
    }
    finally {
        $graphics.Dispose()
    }

    $picture = New-Object System.Windows.Forms.PictureBox
    $picture.Location = New-Object System.Drawing.Point($X, $Y)
    $picture.Size = New-Object System.Drawing.Size($Size, $Size)
    $picture.BackColor = [System.Drawing.Color]::Transparent
    $picture.Image = $bitmap
    return $picture
}

function Add-Control {
    param(
        [Parameter(Mandatory)][System.Windows.Forms.Control]$Parent,
        [Parameter(Mandatory)][System.Windows.Forms.Control]$Child
    )

    [void]$Parent.Controls.Add($Child)
}

function New-Label {
    param(
        [string]$Text,
        [int]$X,
        [int]$Y,
        [int]$Width,
        [int]$Height,
        [System.Drawing.Color]$Colour,
        [float]$Size = 9,
        [System.Drawing.FontStyle]$Style = [System.Drawing.FontStyle]::Regular
    )

    $label = New-Object System.Windows.Forms.Label
    $label.Text = $Text
    $label.Location = New-Object System.Drawing.Point($X, $Y)
    $label.Size = New-Object System.Drawing.Size($Width, $Height)
    $label.ForeColor = $Colour
    $label.BackColor = [System.Drawing.Color]::Transparent
    $label.Font = New-Object System.Drawing.Font("Segoe UI", $Size, $Style)
    $label.AutoEllipsis = $true
    return $label
}

function New-ActionButton {
    param([string]$Text, [int]$Y, [string]$IconText)

    $label = New-Object System.Windows.Forms.Label
    $label.Text = ("{0}  {1}" -f $IconText, $Text)
    $label.Location = New-Object System.Drawing.Point(14, $Y)
    $label.Size = New-Object System.Drawing.Size(246, 24)
    $label.TextAlign = [System.Drawing.ContentAlignment]::MiddleLeft
    $label.BackColor = [System.Drawing.Color]::Transparent
    $label.ForeColor = [System.Drawing.Color]::White
    $label.Font = New-Object System.Drawing.Font("Segoe UI", 9)
    $label.Cursor = [System.Windows.Forms.Cursors]::Hand
    $label.Add_MouseEnter({ $this.BackColor = [System.Drawing.Color]::FromArgb(25, 45, 62) })
    $label.Add_MouseLeave({ $this.BackColor = [System.Drawing.Color]::Transparent })
    return $label
}

function New-StatusDot {
    [CmdletBinding()]
    param(
        [System.Drawing.Color]$Colour,
        [int]$X,
        [int]$Y,
        [int]$Size = 12
    )

    $bitmap = New-Object System.Drawing.Bitmap $Size, $Size
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
        $brush = New-Object System.Drawing.SolidBrush($Colour)
        $graphics.FillEllipse($brush, 0, 0, $Size - 1, $Size - 1)
        $brush.Dispose()
    }
    finally {
        $graphics.Dispose()
    }

    $picture = New-Object System.Windows.Forms.PictureBox
    $picture.Location = New-Object System.Drawing.Point($X, $Y)
    $picture.Size = New-Object System.Drawing.Size($Size, $Size)
    $picture.BackColor = [System.Drawing.Color]::Transparent
    $picture.Image = $bitmap
    return $picture
}

function New-PopupForm {
    [CmdletBinding()]
    param([hashtable]$Context)

    $form = New-Object System.Windows.Forms.Form
    $form.Text = ""
    $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
    $form.ControlBox = $false
    $form.MinimizeBox = $false
    $form.MaximizeBox = $false
    $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
    $form.ShowInTaskbar = $false
    $form.TopMost = $true
    $form.Size = New-Object System.Drawing.Size(276, 304)
    $form.BackColor = [System.Drawing.Color]::FromArgb(71, 84, 98)
    $form.Padding = New-Object System.Windows.Forms.Padding(1)

    $panel = New-Object System.Windows.Forms.Panel
    $panel.Dock = [System.Windows.Forms.DockStyle]::Fill
    $panel.BackColor = [System.Drawing.Color]::FromArgb(10, 23, 34)
    Add-Control -Parent $form -Child $panel
    $form.Add_Shown({ Set-RoundedRegion -Control $this -Radius 10 })
    $form.Add_Resize({ Set-RoundedRegion -Control $this -Radius 10 })

    $icon = New-LogoPictureBox -X 13 -Y 10 -Size 28
    $header = New-Label -Text "Checking PostgreSQL instances" -X 52 -Y 17 -Width 206 -Height 18 -Colour ([System.Drawing.Color]::FromArgb(120, 255, 120)) -Size 8.25
    Add-Control -Parent $panel -Child $icon
    Add-Control -Parent $panel -Child $header

    $line1 = New-Object System.Windows.Forms.Panel
    $line1.Location = New-Object System.Drawing.Point(13, 52)
    $line1.Size = New-Object System.Drawing.Size(250, 1)
    $line1.BackColor = [System.Drawing.Color]::FromArgb(64, 78, 92)
    Add-Control -Parent $panel -Child $line1

    $list = New-Object System.Windows.Forms.Panel
    $list.Location = New-Object System.Drawing.Point(0, 60)
    $list.Size = New-Object System.Drawing.Size(276, 126)
    $list.BackColor = [System.Drawing.Color]::Transparent
    Add-Control -Parent $panel -Child $list

    $line2 = New-Object System.Windows.Forms.Panel
    $line2.Location = New-Object System.Drawing.Point(13, 194)
    $line2.Size = New-Object System.Drawing.Size(250, 1)
    $line2.BackColor = [System.Drawing.Color]::FromArgb(64, 78, 92)
    Add-Control -Parent $panel -Child $line2

    $serviceButton = New-ActionButton -Text "Restart service" -Y 199 -IconText ([string][char]0x21BB)
    $logButton = New-ActionButton -Text "Open log" -Y 224 -IconText ([string][char]0x25A4)
    $configButton = New-ActionButton -Text "Open config" -Y 249 -IconText ([string][char]0x2699)
    $reloadButton = New-ActionButton -Text "Reload configuration" -Y 266 -IconText ([string][char]0x21BB)

    foreach ($button in @($serviceButton, $logButton, $configButton, $reloadButton)) {
        Add-Control -Parent $panel -Child $button
    }

    $serviceMenu = New-Object System.Windows.Forms.ContextMenuStrip
    $serviceMenu.BackColor = [System.Drawing.Color]::FromArgb(13, 26, 38)
    $serviceMenu.ForeColor = [System.Drawing.Color]::White
    $startMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem("Start")
    $stopMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem("Stop")
    $restartMenuItem = New-Object System.Windows.Forms.ToolStripMenuItem("Restart")
    [void]$serviceMenu.Items.Add($startMenuItem)
    [void]$serviceMenu.Items.Add($stopMenuItem)
    [void]$serviceMenu.Items.Add($restartMenuItem)

    $serviceButton.Add_Click({ $serviceMenu.Show($serviceButton, 0, $serviceButton.Height) })
    $startMenuItem.Add_Click({ Invoke-ServiceAction -Context $Context -Action "Start" })
    $stopMenuItem.Add_Click({ Invoke-ServiceAction -Context $Context -Action "Stop" })
    $restartMenuItem.Add_Click({ Invoke-ServiceAction -Context $Context -Action "Restart" })
    $logButton.Add_Click({ Start-FileSafe -Path $Context.Configuration.Logging.LogPath })
    $configButton.Add_Click({ Start-FileSafe -Path $Context.Configuration.ConfigPath })
    $reloadButton.Add_Click({ Reload-Configuration -Context $Context })

    $line3 = New-Object System.Windows.Forms.Panel
    $line3.Location = New-Object System.Drawing.Point(13, 278)
    $line3.Size = New-Object System.Drawing.Size(250, 1)
    $line3.BackColor = [System.Drawing.Color]::FromArgb(44, 58, 72)
    Add-Control -Parent $panel -Child $line3

    $exitButton = New-ActionButton -Text "Exit" -Y 280 -IconText ([string][char]0x21B5)
    Add-Control -Parent $panel -Child $exitButton
    $exitButton.Add_Click({ Stop-DBNotifierApplication -Context $Context })
    $form.Add_Deactivate({ $Context.Popup.Hide() })

    $Context.PopupControls = @{
        Header          = $header
        List            = $list
        ServiceButton   = $serviceButton
        StartMenuItem   = $startMenuItem
        StopMenuItem    = $stopMenuItem
        RestartMenuItem = $restartMenuItem
    }

    return $form
}

function Start-FileSafe {
    param([string]$Path)

    try {
        if (Test-Path -LiteralPath $Path) {
            Start-Process -FilePath $Path | Out-Null
        }
    }
    catch {
    }
}

function Set-SelectedInstance {
    param([hashtable]$Context, [string]$InstanceKey)

    $Context.SelectedInstanceKey = $InstanceKey
    Update-Popup -Context $Context
}

function Get-Summary {
    param([hashtable]$Context)

    $states = @($Context.InstanceStates.Values)
    if ($states.Count -eq 0) {
        return [pscustomobject]@{
            Header = "No PostgreSQL instances detected"
            Tooltip = "$($Context.Configuration.Application.DisplayName): no PostgreSQL instances"
            Colour = [System.Drawing.Color]::FromArgb(255, 205, 70)
        }
    }

    $attention = @($states | Where-Object { -not (Test-IsHealthyState -StateKey $_.CurrentStateKey) }).Count
    if ($attention -eq 0) {
        return [pscustomobject]@{
            Header = "All instances are healthy"
            Tooltip = "$($Context.Configuration.Application.DisplayName): all instances healthy"
            Colour = [System.Drawing.Color]::FromArgb(99, 245, 112)
        }
    }

    return [pscustomobject]@{
        Header = "$attention instance(s) need attention"
        Tooltip = "$($Context.Configuration.Application.DisplayName): $attention instance(s) need attention"
        Colour = [System.Drawing.Color]::FromArgb(255, 102, 83)
    }
}

function Update-Popup {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    if (-not $Context.ContainsKey("PopupControls") -or $null -eq $Context.PopupControls) {
        return
    }

    $summary = Get-Summary -Context $Context
    $Context.PopupControls.Header.Text = $summary.Header
    $list = $Context.PopupControls.List
    $list.Controls.Clear()

    $states = @($Context.InstanceStates.Values | Sort-Object Name)
    if ($states.Count -eq 0) {
        $empty = New-Label -Text "No PostgreSQL instances were found." -X 24 -Y 30 -Width 238 -Height 22 -Colour ([System.Drawing.Color]::White) -Size 8.75
        $hint = New-Label -Text "Install PostgreSQL or add local/remote instances in appsettings.json." -X 24 -Y 58 -Width 238 -Height 36 -Colour ([System.Drawing.Color]::FromArgb(205, 211, 218)) -Size 8
        Add-Control -Parent $list -Child $empty
        Add-Control -Parent $list -Child $hint
        $Context.PopupControls.ServiceButton.Enabled = $true
        $Context.PopupControls.StartMenuItem.Enabled = $false
        $Context.PopupControls.StopMenuItem.Enabled = $false
        $Context.PopupControls.RestartMenuItem.Enabled = $false
        return
    }

    $y = 0
    foreach ($state in @($states | Select-Object -First 3)) {
        $row = New-Object System.Windows.Forms.Panel
        $row.Location = New-Object System.Drawing.Point(0, $y)
        $row.Size = New-Object System.Drawing.Size(276, 42)
        $row.BackColor = [System.Drawing.Color]::Transparent
        $row.Cursor = [System.Windows.Forms.Cursors]::Hand

        $dot = New-StatusDot -Colour (Get-StateColour -StateKey $state.CurrentStateKey) -X 24 -Y 13 -Size 12
        Add-Control -Parent $row -Child $dot

        $name = New-Label -Text $state.Name -X 50 -Y 1 -Width 138 -Height 20 -Colour ([System.Drawing.Color]::White) -Size 9
        $instanceSubtitle = if ([string]::IsNullOrWhiteSpace($state.ServiceName)) { "{0}:{1}" -f $state.HostName, $state.Port } else { $state.ServiceName }
        $service = New-Label -Text $instanceSubtitle -X 50 -Y 21 -Width 138 -Height 18 -Colour ([System.Drawing.Color]::FromArgb(190, 195, 205)) -Size 8
        $status = New-Label -Text (Get-StateDisplayName -StateKey $state.CurrentStateKey) -X 204 -Y 1 -Width 64 -Height 20 -Colour (Get-StateColour -StateKey $state.CurrentStateKey) -Size 9
        $pid = New-Label -Text ("PID: {0}" -f $(if ($state.LastPid) { $state.LastPid } else { "-" })) -X 204 -Y 21 -Width 64 -Height 18 -Colour ([System.Drawing.Color]::FromArgb(190, 195, 205)) -Size 8

        Add-Control -Parent $row -Child $name
        Add-Control -Parent $row -Child $service
        Add-Control -Parent $row -Child $status
        Add-Control -Parent $row -Child $pid

        $instanceKey = $state.InstanceKey
        $selectHandler = { Set-SelectedInstance -Context $Context -InstanceKey $instanceKey }.GetNewClosure()
        $row.Add_Click($selectHandler)
        foreach ($child in @($row.Controls)) {
            $child.Add_Click($selectHandler)
        }

        Add-Control -Parent $list -Child $row
        $y += 42
    }

    if ([string]::IsNullOrWhiteSpace($Context.SelectedInstanceKey) -or -not $Context.InstanceStates.ContainsKey($Context.SelectedInstanceKey)) {
        $Context.SelectedInstanceKey = $states[0].InstanceKey
    }

    $selected = $Context.InstanceStates[$Context.SelectedInstanceKey]
    $canControl = $selected.IsLocalService -and $selected.RestartAllowed -and $selected.CurrentStateKey -ne "MISSING"
    $Context.PopupControls.ServiceButton.Enabled = $true
    $Context.PopupControls.StartMenuItem.Enabled = $canControl -and ($selected.CurrentStateKey -eq "STOPPED")
    $Context.PopupControls.StopMenuItem.Enabled = $canControl -and ($selected.CurrentStateKey -ne "STOPPED")
    $Context.PopupControls.RestartMenuItem.Enabled = $canControl -and ($selected.CurrentStateKey -ne "STOPPED")
}

function Update-NotifyIcon {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    $summary = Get-Summary -Context $Context
    $Context.NotifyIcon.Text = if ($summary.Tooltip.Length -gt 63) { $summary.Tooltip.Substring(0, 63) } else { $summary.Tooltip }
    $oldIcon = $Context.NotifyIcon.Icon
    $Context.NotifyIcon.Icon = New-TrayIcon -BadgeColour $summary.Colour
    if ($oldIcon) {
        $oldIcon.Dispose()
    }
}

function Show-Popup {
    [CmdletBinding()]
    param([hashtable]$Context)

    Update-Popup -Context $Context
    $position = [System.Windows.Forms.Cursor]::Position
    $screen = [System.Windows.Forms.Screen]::FromPoint($position).WorkingArea
    $x = [Math]::Min([Math]::Max($screen.Left, $position.X - $Context.Popup.Width + 12), $screen.Right - $Context.Popup.Width)
    $y = [Math]::Min([Math]::Max($screen.Top, $position.Y - $Context.Popup.Height - 12), $screen.Bottom - $Context.Popup.Height)
    $Context.Popup.Location = New-Object System.Drawing.Point($x, $y)
    $Context.Popup.Show()
    $Context.Popup.Activate()
}

function Test-AndUpdateInstanceState {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Context,
        [Parameter(Mandatory)][pscustomobject]$State
    )

    $previous = $State.CurrentStateKey
    $State.LastStateKey = $previous

    if (-not $State.IsLocalService -or [string]::IsNullOrWhiteSpace($State.ServiceName)) {
        $ready = Test-PgInstanceReady -Context $Context -Instance $State
        $State.LastPid = $null
        if ($ready.IsReady) {
            $State.CurrentStateKey = if ($ready.Method -eq "tcp") { "UP_TCP_ONLY" } else { "UP" }
            $State.LastMessage = if ([string]::IsNullOrWhiteSpace($ready.Message)) { "Remote instance is reachable." } else { $ready.Message }
            return
        }

        $State.CurrentStateKey = if ($ready.TimedOut) { "TIMEOUT" } else { "RUNNING_NO_CONN" }
        $State.LastMessage = if ([string]::IsNullOrWhiteSpace($ready.Message)) { "Remote connectivity check failed." } else { $ready.Message }
        return
    }

    $snapshot = Get-ServiceSnapshot -ServiceName $State.ServiceName

    if ($null -eq $snapshot) {
        $State.CurrentStateKey = "MISSING"
        $State.LastPid = $null
        $State.LastMessage = "Windows service was not found."
        return
    }

    if ($snapshot.State -ne "Running") {
        $State.CurrentStateKey = "STOPPED"
        $State.LastPid = $null
        $State.LastMessage = "Windows service state is $($snapshot.State)."
        return
    }

    $pidChanged = $State.LastPid -and $snapshot.ProcessId -and $State.LastPid -ne $snapshot.ProcessId
    $State.LastPid = $snapshot.ProcessId
    $ready = Test-PgInstanceReady -Context $Context -Instance $State

    if ($ready.IsReady) {
        $State.CurrentStateKey = if ($ready.Method -eq "tcp") { "UP_TCP_ONLY" } else { "UP" }
        if ($pidChanged) {
            $State.CurrentStateKey = "RESTARTED"
            $State.YellowUntil = (Get-Date).AddSeconds($Context.Configuration.Application.RestartBadgeSeconds)
        }
        elseif ($State.CurrentStateKey -eq "RESTARTED" -and (Get-Date) -gt $State.YellowUntil) {
            $State.CurrentStateKey = "UP"
        }
        $State.LastMessage = if ([string]::IsNullOrWhiteSpace($ready.Message)) { "Ready." } else { $ready.Message }
        return
    }

    $State.CurrentStateKey = if ($ready.TimedOut) { "TIMEOUT" } else { "RUNNING_NO_CONN" }
    $State.LastMessage = if ([string]::IsNullOrWhiteSpace($ready.Message)) { "Service is running but connectivity check failed." } else { $ready.Message }
}

function Invoke-HealthCheckCycle {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    foreach ($state in @($Context.InstanceStates.Values)) {
        try {
            Test-AndUpdateInstanceState -Context $Context -State $state
        }
        catch {
            $state.CurrentStateKey = "ERROR"
            $state.LastMessage = $_.Exception.Message
            Write-AppLog -Context $Context -Level "ERROR" -Message ("Health check failed for {0}: {1}" -f $state.ServiceName, $_.Exception.Message)
        }
    }

    Update-NotifyIcon -Context $Context
    Update-Popup -Context $Context
}

function Initialize-InstanceStates {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    $Context.InstanceStates.Clear()
    $instances = @(Resolve-ConfiguredInstances -Configuration $Context.Configuration)

    foreach ($instance in $instances) {
        if (-not [string]::IsNullOrWhiteSpace($instance.InstanceKey)) {
            $Context.InstanceStates[$instance.InstanceKey] = New-InstanceState -Instance $instance
        }
    }

    if ($Context.InstanceStates.Count -gt 0 -and ([string]::IsNullOrWhiteSpace($Context.SelectedInstanceKey) -or -not $Context.InstanceStates.ContainsKey($Context.SelectedInstanceKey))) {
        $Context.SelectedInstanceKey = @($Context.InstanceStates.Keys)[0]
    }

    Update-NotifyIcon -Context $Context
    Update-Popup -Context $Context
}

function Reload-Configuration {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    $Context.Configuration = Get-JsonConfiguration -Path $Context.Configuration.ConfigPath
    $Context.PgIsReadyPath = Resolve-PgIsReadyPath -Configuration $Context.Configuration
    $Context.Timer.Interval = $Context.Configuration.Application.IntervalSeconds * 1000
    Initialize-InstanceStates -Context $Context
    Write-AppLog -Context $Context -Message "Configuration reloaded."
}

function Invoke-ServiceAction {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][hashtable]$Context,
        [Parameter(Mandatory)][ValidateSet("Start", "Stop", "Restart")][string]$Action
    )

    if ([string]::IsNullOrWhiteSpace($Context.SelectedInstanceKey) -or -not $Context.InstanceStates.ContainsKey($Context.SelectedInstanceKey)) {
        return
    }

    $state = $Context.InstanceStates[$Context.SelectedInstanceKey]
    if (-not $state.IsLocalService -or -not $state.RestartAllowed -or [string]::IsNullOrWhiteSpace($state.ServiceName)) {
        return
    }

    try {
        switch ($Action) {
            "Start" { Start-Service -Name $state.ServiceName -ErrorAction Stop }
            "Stop" { Stop-Service -Name $state.ServiceName -ErrorAction Stop }
            "Restart" {
                Restart-Service -Name $state.ServiceName -ErrorAction Stop
            }
        }
        Write-AppLog -Context $Context -Message ("{0} requested for {1}." -f $Action, $state.ServiceName)
        Start-Sleep -Milliseconds 500
        Invoke-HealthCheckCycle -Context $Context
    }
    catch {
        $state.CurrentStateKey = "ERROR"
        $state.LastMessage = $_.Exception.Message
        Write-AppLog -Context $Context -Level "ERROR" -Message ("{0} failed for {1}: {2}" -f $Action, $state.ServiceName, $_.Exception.Message)
        Update-Popup -Context $Context
        Update-NotifyIcon -Context $Context
    }
}

function New-AppContext {
    [CmdletBinding()]
    param([Parameter(Mandatory)][pscustomobject]$Configuration)

    $notifyIcon = New-Object System.Windows.Forms.NotifyIcon
    $notifyIcon.Visible = $true
    $notifyIcon.Text = $Configuration.Application.DisplayName
    $notifyIcon.Icon = New-TrayIcon

    $timer = New-Object System.Windows.Forms.Timer
    $timer.Interval = $Configuration.Application.IntervalSeconds * 1000

    $applicationContext = New-Object System.Windows.Forms.ApplicationContext
    $context = @{
        Configuration       = $Configuration
        PgIsReadyPath       = $null
        InstanceStates      = @{}
        SelectedInstanceKey = $null
        NotifyIcon          = $notifyIcon
        Timer               = $timer
        ApplicationContext  = $applicationContext
        Popup               = $null
        PopupControls       = $null
    }

    $context.Popup = New-PopupForm -Context $context
    $notifyIcon.Add_MouseUp({
        if ($_.Button -eq [System.Windows.Forms.MouseButtons]::Left -or $_.Button -eq [System.Windows.Forms.MouseButtons]::Right) {
            Show-Popup -Context $context
        }
    })
    $timer.Add_Tick({ Invoke-HealthCheckCycle -Context $context })

    return $context
}

function Stop-DBNotifierApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][hashtable]$Context)

    try { $Context.Timer.Stop() } catch {}
    try { $Context.NotifyIcon.Visible = $false; $Context.NotifyIcon.Dispose() } catch {}
    try { if ($Context.Popup) { $Context.Popup.Close(); $Context.Popup.Dispose() } } catch {}
    try { $Context.ApplicationContext.ExitThread() } catch {}
}

function Start-DBNotifierApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    [System.Windows.Forms.Application]::EnableVisualStyles()
    [System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)
    [System.Windows.Forms.Application]::SetUnhandledExceptionMode([System.Windows.Forms.UnhandledExceptionMode]::CatchException)

    $startupLog = Join-Path -Path $env:LocalAppData -ChildPath "DB-Notifier\logs\startup-errors.log"
    try {
        New-Item -Path (Split-Path -Path $startupLog -Parent) -ItemType Directory -Force | Out-Null
    }
    catch {
    }

    [System.Windows.Forms.Application]::add_ThreadException({
        param($Sender, $EventArgs)

        try {
            Add-Content -LiteralPath $startupLog -Value ("{0} [ERROR] UI exception: {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $EventArgs.Exception.Message) -Encoding UTF8
        }
        catch {
        }
    })

    try {
        $configuration = Get-JsonConfiguration -Path $ConfigPath
        $context = New-AppContext -Configuration $configuration
        $context.PgIsReadyPath = Resolve-PgIsReadyPath -Configuration $configuration

        if ($context.PgIsReadyPath) {
            Write-AppLog -Context $context -Message ("pg_isready resolved to '{0}'." -f $context.PgIsReadyPath)
        }
        else {
            Write-AppLog -Context $context -Level "WARN" -Message "pg_isready was not found. DB-Notifier will use TCP fallback when services exist."
        }

        Initialize-InstanceStates -Context $context
        Invoke-HealthCheckCycle -Context $context
        $context.Timer.Start()

        if (-not $configuration.Notifications.SuppressStartupBalloon -and -not $configuration.Application.SilentMode) {
            $summary = Get-Summary -Context $context
            $context.NotifyIcon.BalloonTipTitle = $configuration.Application.DisplayName
            $context.NotifyIcon.BalloonTipText = $summary.Header
            $context.NotifyIcon.ShowBalloonTip($configuration.Notifications.DefaultBalloonTimeoutMs)
        }

        [System.Windows.Forms.Application]::Run($context.ApplicationContext)
    }
    catch {
        $message = "Startup failed: {0}" -f $_.Exception.Message
        try {
            Add-Content -LiteralPath $startupLog -Value ("{0} [ERROR] {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $message) -Encoding UTF8
        }
        catch {
        }
        [System.Windows.Forms.MessageBox]::Show($message, "DB-Notifier - Fatal error", [System.Windows.Forms.MessageBoxButtons]::OK, [System.Windows.Forms.MessageBoxIcon]::Error) | Out-Null
    }
}

function Start-PgNotifierApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    Write-Warning "Start-PgNotifierApplication is deprecated; use Start-DBNotifierApplication."
    Start-DBNotifierApplication -ConfigPath $ConfigPath
}

if ($ExecutionContext.SessionState.Module) {
    Export-ModuleMember -Function @('Start-DBNotifierApplication', 'Start-PgNotifierApplication')
}
