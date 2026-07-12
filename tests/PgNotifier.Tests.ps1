# Module purpose: Verifies Pg Notifier Tests behaviour and protects the documented project contract.
Set-StrictMode -Version Latest

Write-Warning "PgNotifier.Tests.ps1 is deprecated; use DBNotifier.Legacy.Tests.ps1."
. (Join-Path -Path $PSScriptRoot -ChildPath "DBNotifier.Legacy.Tests.ps1")
