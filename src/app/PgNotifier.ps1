# Module purpose: Provides Pg Notifier for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
[CmdletBinding()]
param(
    [string]$ConfigPath = (Join-Path -Path $PSScriptRoot -ChildPath "..\..\config\appsettings.json")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Write-Warning "PgNotifier.ps1 is deprecated; use DBNotifier.ps1. This compatibility entry point will be removed after the documented migration window."

& (Join-Path -Path $PSScriptRoot -ChildPath "DBNotifier.ps1") -ConfigPath $ConfigPath
