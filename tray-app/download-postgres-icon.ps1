# Module purpose: Prevents network retrieval of the retired vendor-specific Tray prototype asset.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
throw "Automatic vendor-asset download is retired. The historical Tray prototype is non-distributable and must not acquire or refresh external artwork."
