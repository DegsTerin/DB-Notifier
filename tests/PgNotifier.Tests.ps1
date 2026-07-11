Set-StrictMode -Version Latest

Describe "PgNotifier project" {
    It "loads the module manifest" {
        $manifestPath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psd1"
        $manifest = Test-ModuleManifest -Path $manifestPath
        $manifest.Name | Should Be "PgNotifier"
    }

    It "contains a valid sample configuration" {
        $configPath = Join-Path -Path $PSScriptRoot -ChildPath "..\examples\appsettings.sample.json"
        { Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json } | Should Not Throw
    }

    It "loads JSON configuration with array values without recursion errors" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("pgnotifier-test-{0}.json" -f [guid]::NewGuid())
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
            $module = Get-Module PgNotifier
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

        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psm1"
        Import-Module $modulePath -Force

        $configuration = [pscustomobject]@{
            ConfigPath = "C:\ProgramData\PgNotifier\appsettings.json"
            PgIsReady  = [pscustomobject]@{
                Path = "pg_isready.exe"
            }
            Instances  = @(
                [pscustomobject]@{
                    PostgresExe = "C:\Program Files\PostgreSQL\18\bin\postgres.exe"
                }
            )
        }

        $module = Get-Module PgNotifier
        $resolvedPath = & $module { param($Config) Resolve-PgIsReadyPath -Configuration $Config } $configuration
        $resolvedPath | Should Be $expectedPath
    }

    It "expands LocalAppData variables in configured log paths" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psm1"
        Import-Module $modulePath -Force

        $module = Get-Module PgNotifier
        $resolvedPath = & $module { Resolve-ConfiguredPath -Candidate "%LocalAppData%\PgNotifier\logs\pgnotifier.log" -ConfigPath "C:\ProgramData\PgNotifier\appsettings.json" }
        $resolvedPath | Should Match "PgNotifier\\logs\\pgnotifier\.log$"
    }

    It "ships with no hard-coded PostgreSQL instance requirement" {
        $configPath = Join-Path -Path $PSScriptRoot -ChildPath "..\config\appsettings.json"
        $configuration = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
        @($configuration.instances).Count | Should Be 0
        $configuration.application.autoDiscover | Should Be $true
    }

    It "can normalise an empty configuration without PostgreSQL installed" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("pgnotifier-empty-{0}.json" -f [guid]::NewGuid())
        @'
{
  "application": {
    "autoDiscover": false
  },
  "instances": []
}
'@ | Set-Content -LiteralPath $tempConfigPath -Encoding UTF8

        try {
            $module = Get-Module PgNotifier
            $configuration = & $module { param($Path) Get-JsonConfiguration -Path $Path } $tempConfigPath
            $resolved = & $module { param($Config) Resolve-ConfiguredInstances -Configuration $Config } $configuration
            @($resolved).Count | Should Be 0
        }
        finally {
            Remove-Item -LiteralPath $tempConfigPath -Force -ErrorAction SilentlyContinue
        }
    }

    It "supports remote PostgreSQL instances without Windows service control" {
        $modulePath = Join-Path -Path $PSScriptRoot -ChildPath "..\src\modules\PgNotifier\PgNotifier.psm1"
        Import-Module $modulePath -Force

        $tempConfigPath = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath ("pgnotifier-remote-{0}.json" -f [guid]::NewGuid())
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
            $module = Get-Module PgNotifier
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
}
