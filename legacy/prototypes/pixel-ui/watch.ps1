# Module purpose: Provides watch for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
[CmdletBinding()]
param(
    [string]$Python = "python"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSCommandPath -Parent
$app = Join-Path -Path $root -ChildPath "app.py"

function Start-Preview {
    param([string]$PythonCommand)
    return Start-Process -FilePath $PythonCommand -ArgumentList @('"{0}"' -f $app) -PassThru
}

function Stop-Preview {
    param($Process)
    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

$script:lastRestart = Get-Date
$script:preview = Start-Preview -PythonCommand $Python

$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $root
$watcher.Filter = "*.py"
$watcher.IncludeSubdirectories = $false
$watcher.NotifyFilter = [System.IO.NotifyFilters]'LastWrite, Size, FileName'
$watcher.EnableRaisingEvents = $true

$action = {
    if (((Get-Date) - $script:lastRestart).TotalMilliseconds -lt 450) {
        return
    }

    $script:lastRestart = Get-Date
    Stop-Preview -Process $script:preview
    Start-Sleep -Milliseconds 120
    $script:preview = Start-Preview -PythonCommand $using:Python
    Write-Host ("Reloaded Pixel UI preview: {0}" -f $Event.SourceEventArgs.Name)
}

$subscriptions = @()
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Changed -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Created -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Deleted -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Renamed -Action $action

Write-Host "Pixel UI live preview is running."
Write-Host "Edit legacy/prototypes/pixel-ui/app.py and save to reload automatically."
Write-Host "Press Ctrl+C to stop."

try {
    while ($true) {
        Start-Sleep -Seconds 1
        if ($script:preview.HasExited) {
            $script:preview = Start-Preview -PythonCommand $Python
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
