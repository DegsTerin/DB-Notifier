# Module purpose: Blocks distribution of the historical Pixel UI prototype until an owned, pinned Python toolchain exists.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
throw "DBNotifierPixelUI.exe is a non-distributable historical prototype. Packaging is blocked until its Python runtime and build toolchain are owned, pinned, hashed and approved."
