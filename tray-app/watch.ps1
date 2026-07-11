[CmdletBinding()]
param(
    [string]$Python = "C:\Users\brunn\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSCommandPath -Parent
$app = Join-Path -Path $root -ChildPath "app.py"

function Start-Preview {
    return Start-Process -FilePath $Python -ArgumentList @('"{0}"' -f $app) -PassThru
}

function Stop-Preview {
    param($Process)
    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

if (-not (Test-Path -LiteralPath (Join-Path $root "assets\postgres.png"))) {
    & (Join-Path $root "download-postgres-icon.ps1")
}

$script:lastRestart = Get-Date
$script:preview = Start-Preview

$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $root
$watcher.IncludeSubdirectories = $true
$watcher.NotifyFilter = [System.IO.NotifyFilters]'LastWrite, Size, FileName'
$watcher.EnableRaisingEvents = $true

$action = {
    $name = $Event.SourceEventArgs.Name
    if ($name -notmatch '\.(py|png)$') {
        return
    }

    if (((Get-Date) - $script:lastRestart).TotalMilliseconds -lt 500) {
        return
    }

    $script:lastRestart = Get-Date
    Stop-Preview -Process $script:preview
    Start-Sleep -Milliseconds 150
    $script:preview = Start-Preview
    Write-Host ("Reloaded tray preview after change: {0}" -f $name)
}

$subscriptions = @()
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Changed -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Created -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Deleted -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Renamed -Action $action

Write-Host "PgNotifier tray live preview is running."
Write-Host "Edit tray-app/app.py or assets to restart automatically."
Write-Host "Press Ctrl+C to stop."

try {
    while ($true) {
        Start-Sleep -Seconds 1
        if ($script:preview.HasExited) {
            $script:preview = Start-Preview
        }
    }
}
finally {
    foreach ($subscription in $subscriptions) {
        Unregister-Event -SubscriptionId $subscription.Id -ErrorAction SilentlyContinue
    }
    $watcher.Dispose()
    Stop-Preview -Process $script:preview
}
