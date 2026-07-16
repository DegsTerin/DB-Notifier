# Module purpose: Delegates deprecated PgNotifier entry points to read-only DBNotifier compatibility monitoring.
Set-StrictMode -Version Latest

$canonicalModule = Join-Path -Path $PSScriptRoot -ChildPath "..\DBNotifier\DBNotifier.psm1"
Import-Module $canonicalModule -Force

function Start-PgNotifierApplication {
    [CmdletBinding()]
    param([Parameter(Mandatory)][string]$ConfigPath)

    Write-Warning "The PgNotifier module is deprecated; use DBNotifier and Start-DBNotifierApplication."
    Start-DBNotifierApplication -ConfigPath $ConfigPath
}

Export-ModuleMember -Function Start-PgNotifierApplication
