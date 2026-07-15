# Module purpose: Verifies DBNotifier Legacy Tests behaviour and protects the documented project contract.
Set-StrictMode -Version Latest

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
        $buildSource | Should Match 'packageIconDirectory'
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

    It "resolves pg_isready from a standard PostgreSQL installation even when it is not on PATH" {
        $expectedPath = "C:\Program Files\PostgreSQL\18\bin\pg_isready.exe"
        if (-not (Test-Path -LiteralPath $expectedPath)) {
            return
        }

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
        $configuration.application.autoDiscover | Should Be $true
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
}
