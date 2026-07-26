# Module purpose: Verifies DBNotifier Legacy Tests behaviour and protects the documented project contract.
Set-StrictMode -Version Latest
$postgresql18Executable = "C:\Program Files\PostgreSQL\18\bin\pg_isready.exe"
$postgresql18Available = Test-Path -LiteralPath $postgresql18Executable -PathType Leaf

Describe "DB-Notifier legacy compatibility" {
    It "loads the module manifest" {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psd1"
        $manifest = Test-ModuleManifest -Path $manifestPath
        $manifest.Name | Should Be "DBNotifier"
    }

    It "uses the canonical generated icon family for legacy notification-area states" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $module = Get-Module DBNotifier

        $criticalPath = & $module { Resolve-CanonicalTrayIconPath -State "Critical" }
        $unknownPath = & $module { Resolve-CanonicalTrayIconPath -State "unsupported-state" }
        $icon = & $module { New-TrayIcon -State "Critical" -Size 32 }
        $nativeSmallIcon = & $module { New-TrayIcon -State "Critical" }

        try {
            (Split-Path -Path $criticalPath -Leaf) | Should Be "DBNotifier.Critical.ico"
            (Split-Path -Path $unknownPath -Leaf) | Should Be "DBNotifier.Unknown.ico"
            $icon.Width | Should Be 32
            $icon.Height | Should Be 32
            $nativeSmallIcon.Width | Should Be ([System.Windows.Forms.SystemInformation]::SmallIconSize.Width)
            $nativeSmallIcon.Height | Should Be ([System.Windows.Forms.SystemInformation]::SmallIconSize.Height)
        }
        finally {
            $icon.Dispose()
            $nativeSmallIcon.Dispose()
        }

        $moduleSource = Get-Content -LiteralPath $modulePath -Raw
        $moduleSource | Should Not Match 'DrawString\("P"'
        $moduleSource | Should Not Match 'BadgeColour'

        $buildSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\build\build.ps1") -Raw
        $installerSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\packaging\inno\DBNotifier.iss") -Raw
        $buildSource | Should Match 'runtimeIconNames'
        $installerSource | Should Match 'Assets\\\*\.ico'
    }

    It "contains a valid sample configuration" {
        $configPath = Join-Path -Path $PSScriptRoot -ChildPath "..\examples\appsettings.sample.json"
        { Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json } | Should Not Throw
    }

    It "loads JSON configuration with array values without recursion errors" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-test-{0}.json" -f [guid]::NewGuid())
        @'
{
  "logging": {
    "logPath": "logs\\test.log"
  },
  "pgIsReady": {
    "path": "pg_isready.exe",
    "extraArguments": []
  },
  "instances": [
    {
      "name": "PostgreSQL 18",
      "serviceName": "postgresql-x64-18",
      "hostName": "localhost",
      "port": 5432,
      "postgresExe": "C:\\Program Files\\PostgreSQL\\18\\bin\\postgres.exe"
    }
  ]
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $configuration.Logging.LogPath | Should Match "logs\\test\.log$"
            $configuration.Instances.Count | Should Be 1
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "resolves pg_isready from a standard PostgreSQL installation even when it is not on PATH" -Skip:(-not $postgresql18Available) {
        $expectedPath = $postgresql18Executable
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force

        $configuration = [pscustomobject]@{
            ConfigPath = "C:\ProgramData\DB-Notifier\appsettings.json"
            PgIsReady  = [pscustomobject]@{
                Path = "pg_isready.exe"
            }
            Instances  = @(
                [pscustomobject]@{
                    PostgresExe = "C:\Program Files\PostgreSQL\18\bin\postgres.exe"
                }
            )
        }

        $module = Get-Module DBNotifier
        $resolvedPath = & $module { param($Config) Resolve-PgIsReadyPath -Configuration $Config } $configuration
        $resolvedPath | Should Be $expectedPath
    }

    It "rejects a configured pg_isready executable outside an operating-system-owned PostgreSQL root" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $temporaryDirectory = Join-Path ([System.IO.Path]::GetTempPath()) ("dbnotifier-untrusted-utility-{0}" -f [guid]::NewGuid().ToString("N"))
        $temporaryUtility = Join-Path $temporaryDirectory "pg_isready.exe"
        $temporaryConfig = Join-Path $temporaryDirectory "appsettings.json"
        New-Item -ItemType Directory -Path $temporaryDirectory | Out-Null
        Set-Content -LiteralPath $temporaryUtility -Value "untrusted test fixture" -Encoding UTF8
        @{
            pgIsReady = @{
                path = $temporaryUtility
                extraArguments = @("--host=untrusted.example")
            }
            instances = @()
        } | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $temporaryConfig -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $temporaryConfig
            $resolvedPath = & $module { param($Config) Resolve-PgIsReadyPath -Configuration $Config } $configuration

            $resolvedPath | Should Be $null
            @($configuration.PgIsReady.ExtraArguments).Count | Should Be 0
        }
        finally {
            Remove-Item -LiteralPath $temporaryDirectory -Recurse -Force -ErrorAction SilentlyContinue
        }
    }

    It "rejects a differently named relative readiness utility before discovery" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        Mock Get-PostgreSqlBinDirectoryCandidates -ModuleName DBNotifier { throw "Discovery must not run for a differently named candidate." }
        $configuration = [pscustomobject]@{
            PgIsReady = [pscustomobject]@{ Path = "pg_isready-copy.exe" }
        }

        $module = Get-Module DBNotifier
        $resolvedPath = & $module { param($Config) Resolve-PgIsReadyPath -Configuration $Config } $configuration

        $resolvedPath | Should Be $null
        Assert-MockCalled Get-PostgreSqlBinDirectoryCandidates -ModuleName DBNotifier -Times 0 -Exactly
    }

    It "expands LocalAppData variables in configured log paths" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force

        $module = Get-Module DBNotifier
        $resolvedPath = & $module { Resolve-ConfiguredPath -Candidate "%LocalAppData%\DB-Notifier\logs\dbnotifier.log" -ConfigPath "C:\ProgramData\DB-Notifier\appsettings.json" }
        $resolvedPath | Should Match "DB-Notifier\\logs\\dbnotifier\.log$"
    }

    It "ships with no hard-coded PostgreSQL instance requirement" {
        $configPath = Join-Path -Path $PSScriptRoot -ChildPath "..\config\appsettings.json"
        $configuration = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
        @($configuration.instances).Count | Should Be 0
        $configuration.application.autoDiscover | Should Be $false
        ($configuration.PSObject.Properties.Name -contains "pgIsReady") | Should Be $false
    }

    It "honours the global notification switch for startup balloons" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $configuration = [pscustomobject]@{
            Notifications = [pscustomobject]@{
                Enabled = $false
                SuppressStartupBalloon = $false
            }
            Application = [pscustomobject]@{ SilentMode = $false }
        }

        $module = Get-Module DBNotifier
        $permitted = & $module { param($Config) Test-ShouldShowStartupNotification -Configuration $Config } $configuration

        $permitted | Should Be $false
    }

    It "fails closed when legacy JSON is malformed" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-invalid-{0}.json" -f [guid]::NewGuid())
        '{ invalid json' | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            { & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath } | Should Throw
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "requires an explicit false-by-default local service control capability" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-local-{0}.json" -f [guid]::NewGuid())
        @'
{
  "application": {
    "autoDiscover": false
  },
  "instances": [
    {
      "name": "Local PostgreSQL",
      "serviceName": "postgresql-test",
      "hostName": "localhost",
      "port": 5432,
      "restartAllowed": true
    }
  ]
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $configuration.Instances.Count | Should Be 1
            $configuration.Instances[0].RestartAllowed | Should Be $false
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "keeps auto-discovered services read-only" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        Mock Get-PostgreSqlWindowsServices -ModuleName DBNotifier {
            @([pscustomobject]@{
                Name = "postgresql-test"
                DisplayName = "PostgreSQL Test"
                Port = 5432
                HostName = "localhost"
                State = "Running"
                ProcessId = 1234
                PathName = "C:\Program Files\PostgreSQL\18\bin\pg_ctl.exe"
            })
        }
        $configuration = [pscustomobject]@{
            Application = [pscustomobject]@{ AutoDiscover = $true }
            Instances = @()
        }

        $module = Get-Module DBNotifier
        $resolved = @(& $module { param($Config) Resolve-ConfiguredInstances -Configuration $Config } $configuration)
        $resolved.Count | Should Be 1
        $resolved[0].RestartAllowed | Should Be $false
        Assert-MockCalled Get-PostgreSqlWindowsServices -ModuleName DBNotifier -Times 1 -Exactly
    }

    It "does not promote TCP-only transport evidence to healthy" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $module = Get-Module DBNotifier

        $transportHealthy = & $module { Test-IsHealthyState -StateKey "UP_TCP_ONLY" }
        $transportColour = & $module { (Get-StateColour -StateKey "UP_TCP_ONLY").ToArgb() }
        $healthyColour = & $module { (Get-StateColour -StateKey "UP").ToArgb() }
        $context = @{
            Configuration = [pscustomobject]@{
                Application = [pscustomobject]@{ DisplayName = "DB-Notifier" }
            }
            InstanceStates = @{
                "transport-only" = [pscustomobject]@{ CurrentStateKey = "UP_TCP_ONLY" }
            }
        }
        $summary = & $module { param($Context) Get-Summary -Context $Context } $context

        $transportHealthy | Should Be $false
        $transportColour | Should Not Be $healthyColour
        $summary.IconState | Should Be "Unknown"
        $summary.Header | Should Match 'unproved database health'
    }

    It "does not promote a changed local PID with TCP-only evidence to restarted health" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        Mock Get-ServiceSnapshot -ModuleName DBNotifier {
            [pscustomobject]@{
                State = "Running"
                ProcessId = 5678
            }
        }
        Mock Test-PgInstanceReady -ModuleName DBNotifier {
            [pscustomobject]@{
                IsReady = $true
                TimedOut = $false
                ExitCode = -1
                Message = "TCP transport is reachable."
                Method = "tcp"
            }
        }
        $state = [pscustomobject]@{
            InstanceKey = "local-tcp-only"
            Name = "Local PostgreSQL"
            ServiceName = "postgresql-test"
            HostName = "localhost"
            Port = 5432
            RestartAllowed = $false
            IsLocalService = $true
            NotificationsEnabled = $false
            CurrentStateKey = "UP_TCP_ONLY"
            LastStateKey = "UP_TCP_ONLY"
            LastPid = 1234
            LastMessage = "TCP transport is reachable."
            YellowUntil = (Get-Date).AddSeconds(-1)
        }
        $context = @{
            Configuration = [pscustomobject]@{
                Application = [pscustomobject]@{
                    DisplayName = "DB-Notifier"
                    RestartBadgeSeconds = 30
                }
            }
            InstanceStates = @{ $state.InstanceKey = $state }
        }

        $module = Get-Module DBNotifier
        & $module { param($Context, $State) Test-AndUpdateInstanceState -Context $Context -State $State } $context $state
        $summary = & $module { param($Context) Get-Summary -Context $Context } $context

        $state.CurrentStateKey | Should Be "UP_TCP_ONLY"
        $state.LastPid | Should Be 5678
        $summary.IconState | Should Be "Unknown"
        $summary.Header | Should Match 'unproved database health'
        Assert-MockCalled Get-ServiceSnapshot -ModuleName DBNotifier -Times 1 -Exactly
        Assert-MockCalled Test-PgInstanceReady -ModuleName DBNotifier -Times 1 -Exactly
    }

    It "preserves restarted only during its bounded authenticated window" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $state = [pscustomobject]@{
            CurrentStateKey = "RESTARTED"
            YellowUntil = (Get-Date).AddMinutes(1)
        }
        $module = Get-Module DBNotifier

        & $module { param($State) Set-AuthenticatedReadyState -State $State -PidChanged $false -RestartBadgeSeconds 30 } $state
        $state.CurrentStateKey | Should Be "RESTARTED"

        $state.YellowUntil = (Get-Date).AddSeconds(-1)
        & $module { param($State) Set-AuthenticatedReadyState -State $State -PidChanged $false -RestartBadgeSeconds 30 } $state
        $state.CurrentStateKey | Should Be "UP"
    }

    It "contains no direct Windows service-control executor" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        $moduleSource = Get-Content -LiteralPath $modulePath -Raw
        $moduleSource | Should Not Match '\b(?:Start|Stop|Restart)-Service\b'
        $moduleSource | Should Not Match '\bInvoke-ServiceAction\b'
    }

    It "can normalise an empty configuration without PostgreSQL installed" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-empty-{0}.json" -f [guid]::NewGuid())
        @'
{
  "application": {
    "autoDiscover": false
  },
  "instances": []
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $resolved = & $module { param($Config) Resolve-ConfiguredInstances -Configuration $Config } $configuration
            @($resolved).Count | Should Be 0
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "supports remote PostgreSQL instances without Windows service control" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("dbnotifier-remote-{0}.json" -f [guid]::NewGuid())
        @'
{
  "application": {
    "autoDiscover": false
  },
  "instances": [
    {
      "name": "Remote Reporting DB",
      "hostName": "db.example.local",
      "port": 5432,
      "enabled": true,
      "restartAllowed": false
    }
  ]
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $resolved = @(& $module { param($Config) Resolve-ConfiguredInstances -Configuration $Config } $configuration)
            $resolved.Count | Should Be 1
            $resolved[0].InstanceKey | Should Be "db.example.local:5432"
            $resolved[0].ServiceName | Should Be ""
            $resolved[0].IsLocalService | Should Be $false
            $resolved[0].RestartAllowed | Should Be $false
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "permits only pinned loopback destinations through the legacy monitoring surface" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $module = Get-Module DBNotifier

        $localhost = & $module {
            Resolve-AuthorisedNetworkDestination -HostName "localhost" -Port 5432
        }
        $mappedLoopback = & $module {
            Resolve-AuthorisedNetworkDestination -HostName "::ffff:127.0.0.1" -Port 5432
        }
        $remote = & $module {
            Resolve-AuthorisedNetworkDestination -HostName "db.example.local" -Port 5432
        }
        $metadata = & $module {
            Resolve-AuthorisedNetworkDestination -HostName "169.254.169.254" -Port 80
        }
        $invalidPort = & $module {
            Resolve-AuthorisedNetworkDestination -HostName "localhost" -Port 70000
        }

        $localhost.IsApproved | Should Be $true
        $localhost.Address | Should Be "127.0.0.1"
        $mappedLoopback.IsApproved | Should Be $true
        $mappedLoopback.Address | Should Be "127.0.0.1"
        $remote.IsApproved | Should Be $false
        $remote.FailureCode | Should Be "network.address_denied"
        $metadata.IsApproved | Should Be $false
        $invalidPort.IsApproved | Should Be $false
        $invalidPort.FailureCode | Should Be "network.destination_invalid"
    }

    It "refuses a denied legacy destination before TCP or pg_isready execution" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        Mock Test-TcpPort -ModuleName DBNotifier {
            throw "TCP must not run for a denied destination."
        }
        Mock Invoke-ProcessWithTimeout -ModuleName DBNotifier {
            throw "pg_isready must not run for a denied destination."
        }
        $context = @{
            PgIsReadyPath = "C:\Program Files\PostgreSQL\18\bin\pg_isready.exe"
            Configuration = [pscustomobject]@{
                PgIsReady = [pscustomobject]@{
                    TimeoutSeconds = 5
                    RetryCount = 1
                    RetryDelayMs = 1
                    ExtraArguments = @()
                }
            }
        }
        $instance = [pscustomobject]@{
            HostName = "metadata.invalid"
            Port = 5432
        }

        $module = Get-Module DBNotifier
        $result = & $module {
            param($Context, $Instance)
            Test-PgInstanceReady -Context $Context -Instance $Instance
        } $context $instance

        $result.IsReady | Should Be $false
        $result.Method | Should Be "policy"
        $result.Message | Should Be "Network destination is not authorised."
        Assert-MockCalled Test-TcpPort -ModuleName DBNotifier -Times 0 -Exactly
        Assert-MockCalled Invoke-ProcessWithTimeout -ModuleName DBNotifier -Times 0 -Exactly
    }

    It "keeps the deprecated PgNotifier module entry point available" {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psd1"
        $manifest = Test-ModuleManifest -Path $manifestPath
        $manifest.Name | Should Be "PgNotifier"
        ($manifest.ExportedFunctions.Keys -contains "Start-PgNotifierApplication") | Should Be $true
    }

    It "preserves explicitly supplied PgNotifier branding and log paths" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\DBNotifier\DBNotifier.psm1"
        Import-Module $modulePath -Force
        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("pgnotifier-compat-{0}.json" -f [guid]::NewGuid())
        @'
{
  "application": {
    "displayName": "PgNotifier"
  },
  "logging": {
    "logPath": "%LocalAppData%\\PgNotifier\\logs\\pgnotifier.log"
  },
  "instances": []
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module DBNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $configuration.Application.DisplayName | Should Be "PgNotifier"
            $configuration.Logging.LogPath | Should Match "PgNotifier\\logs\\pgnotifier\.log$"
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "keeps compatibility artefact versions aligned at the security patch level" {
        $canonicalManifest = Test-ModuleManifest -Path (Join-Path $PSScriptRoot "..\src\modules\DBNotifier\DBNotifier.psd1")
        $deprecatedManifest = Test-ModuleManifest -Path (Join-Path $PSScriptRoot "..\src\modules\PgNotifier\PgNotifier.psd1")
        $installerSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\packaging\inno\DBNotifier.iss") -Raw

        $canonicalManifest.Version.ToString() | Should Be "1.1.1"
        $deprecatedManifest.Version.ToString() | Should Be "1.1.1"
        $installerSource | Should Match 'AppVersion=1\.1\.1'
    }

    It "blocks unverified compatibility and prototype packaging without downloading tooling" {
        $toolchain = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\build\compatibility-toolchain.json") -Raw | ConvertFrom-Json
        $compatibilityBuild = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\build\build.ps1") -Raw
        $prototypeBuilds = @(
            (Join-Path $PSScriptRoot "..\desktop-wpf\build-desktop.ps1"),
            (Join-Path $PSScriptRoot "..\pixel-ui\build-exe.ps1"),
            (Join-Path $PSScriptRoot "..\tray-app\build-exe.ps1")
        )
        $downloadSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\tray-app\download-postgres-icon.ps1") -Raw

        $toolchain.status | Should Be "blocked"
        $toolchain.schemaVersion | Should Be "dbnotifier.compatibility-toolchain.v2"
        $toolchain.completeExecutableClosure | Should Be $false
        @($toolchain.components).Count | Should Be 0
        $compatibilityBuild | Should Match 'completeExecutableClosure'
        $compatibilityBuild | Should Match 'dependencyClosureComplete'
        $compatibilityBuild | Should Not Match '\bInvoke-PS2EXE\b'
        foreach ($prototypeBuild in $prototypeBuilds) {
            (Get-Content -LiteralPath $prototypeBuild -Raw) | Should Match 'non-distributable historical prototype'
        }
        $downloadSource | Should Not Match '\bInvoke-WebRequest\b'
        $downloadSource | Should Match 'Automatic vendor-asset download is retired'
    }

    It "pins every official GitHub Action to a full commit SHA" {
        $workflow = Get-Content -LiteralPath (Join-Path $PSScriptRoot "..\.github\workflows\ci.yml") -Raw
        $uses = [regex]::Matches($workflow, 'uses:\s+actions/[^@\s]+@([^\s#]+)')

        $uses.Count | Should BeGreaterThan 0
        foreach ($use in $uses) {
            $use.Groups[1].Value | Should Match '^[0-9a-f]{40}$'
        }
        $workflow | Should Not Match 'uses:\s+actions/[^@\s]+@v\d+'
    }

    It "rejects an incomplete NuGet vulnerability report instead of reporting a false pass" {
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $invalidReportPath = Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.invalid.json"

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $invalidReportPath } | Should Throw
    }

    It "accepts the complete empty structure emitted by NuGet when framework findings are omitted" {
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $emptyReportPath = Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.no-frameworks.json"

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $emptyReportPath } | Should Not Throw
    }

    It "accepts complete NuGet structure and resolves relative report paths from the solution directory" {
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $completeReportPath = Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.complete-empty.json"

        Push-Location ([System.IO.Path]::GetTempPath())
        try {
            { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $completeReportPath } | Should Not Throw
        }
        finally {
            Pop-Location
        }
    }

    It "rejects NuGet reports that omit a solution project" {
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $report = Get-Content -LiteralPath (Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.no-frameworks.json") -Raw | ConvertFrom-Json
        $report.projects = @($report.projects | Select-Object -First 17)
        $reportPath = Join-Path $TestDrive "nuget-project-omitted.json"
        $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $reportPath } | Should Throw
    }

    It "rejects NuGet reports from a source set that diverges from NuGet.config" {
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $report = Get-Content -LiteralPath (Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.no-frameworks.json") -Raw | ConvertFrom-Json
        $report.sources = @("https://packages.invalid/v3/index.json")
        $reportPath = Join-Path $TestDrive "nuget-source-divergent.json"
        $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $reportPath } | Should Throw
    }

    It "rejects direct and transitive vulnerability findings" -TestCases @(
        @{ Collection = "topLevelPackages" },
        @{ Collection = "transitivePackages" }
    ) {
        param($Collection)
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $report = Get-Content -LiteralPath (Join-Path $PSScriptRoot "fixtures\nuget-vulnerability-report.complete-empty.json") -Raw | ConvertFrom-Json
        $report.projects[0].frameworks[0] | Add-Member -MemberType NoteProperty -Name $Collection -Value @(
            [pscustomobject]@{
                id = "Example.Package"
                resolvedVersion = "1.0.0"
                vulnerabilities = @([pscustomobject]@{ severity = "high"; advisoryurl = "https://advisories.invalid/example" })
            }
        )
        $reportPath = Join-Path $TestDrive ("nuget-{0}.json" -f $Collection)
        $report | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $reportPath -Encoding UTF8

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $reportPath } | Should Throw
    }

    It "rejects malformed and future-schema NuGet reports" -TestCases @(
        @{ Name = "malformed"; Content = "{" },
        @{ Name = "future"; Content = '{"version":2,"parameters":"--vulnerable --include-transitive","sources":[],"projects":[]}' }
    ) {
        param($Name, $Content)
        $gatePath = Join-Path $PSScriptRoot "..\scripts\verify-nuget-vulnerabilities.ps1"
        $reportPath = Join-Path $TestDrive ("nuget-{0}.json" -f $Name)
        Set-Content -LiteralPath $reportPath -Value $Content -Encoding UTF8

        { & $gatePath -SolutionPath (Join-Path $PSScriptRoot "..\DBNotifier.sln") -ReportPath $reportPath } | Should Throw
    }
}
