# Module purpose: Provides Pg Notifier Desktop for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
[CmdletBinding()]
param([switch]$NoDynamicMock)

Write-Warning "PgNotifier.Desktop.ps1 is deprecated; use DBNotifier.Desktop.ps1."
& (Join-Path -Path $PSScriptRoot -ChildPath "DBNotifier.Desktop.ps1") -NoDynamicMock:$NoDynamicMock
