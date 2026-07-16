# Module purpose: Blocks distribution of the historical Tray prototype until an owned, pinned Python toolchain exists.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
throw "DBNotifierTray.exe is a non-distributable historical prototype. Packaging is blocked until its external Python dependencies and build toolchain are owned, pinned, hashed and approved."
