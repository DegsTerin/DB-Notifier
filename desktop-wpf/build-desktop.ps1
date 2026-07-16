# Module purpose: Blocks distribution of the superseded PowerShell/WPF prototype and keeps production packaging owned by STATE-08.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
throw "DBNotifierDesktop.exe is a non-distributable historical prototype. Production WPF packaging belongs to the signed STATE-08 toolchain."
