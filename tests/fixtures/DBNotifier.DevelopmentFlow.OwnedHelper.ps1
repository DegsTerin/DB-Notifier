# Module purpose: Provides a bounded disposable process for shutdown-preflight policy tests.
#Requires -Version 7.0

[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Start-Sleep -Seconds 30
