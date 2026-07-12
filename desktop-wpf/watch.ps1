# Module purpose: Provides watch for the legacy-compatible DB-Notifier tooling without changing database services implicitly.
[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path -Path $PSCommandPath -Parent
$app = Join-Path -Path $root -ChildPath "DBNotifier.Desktop.ps1"
$script:watchFiles = @("*.xaml", "*.ps1", "*.json")

function Start-Preview {
    $arguments = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-STA",
        "-File", ('"{0}"' -f $app)
    )

    return Start-Process -FilePath "powershell.exe" -ArgumentList $arguments -PassThru
}

function Stop-Preview {
    param($Process)

    if ($null -ne $Process -and -not $Process.HasExited) {
        Stop-Process -Id $Process.Id -Force -ErrorAction SilentlyContinue
    }
}

$preview = Start-Preview
$watcher = New-Object System.IO.FileSystemWatcher
$watcher.Path = $root
$watcher.IncludeSubdirectories = $false
$watcher.EnableRaisingEvents = $true
$watcher.NotifyFilter = [System.IO.NotifyFilters]'FileName, LastWrite, Size'

$lastRestart = Get-Date
$action = {
    $path = $Event.SourceEventArgs.FullPath
    $name = [System.IO.Path]::GetFileName($path)
    $matches = $false

    foreach ($pattern in $script:watchFiles) {
        if ($name -like $pattern) {
            $matches = $true
            break
        }
    }

    if (-not $matches) {
        return
    }

    if (((Get-Date) - $script:lastRestart).TotalMilliseconds -lt 500) {
        return
    }

    $script:lastRestart = Get-Date
    Stop-Preview -Process $script:preview
    Start-Sleep -Milliseconds 150
    $script:preview = Start-Preview
    Write-Host ("Reloaded preview after change: {0}" -f $name)
}

$script:preview = $preview
$script:lastRestart = $lastRestart

$subscriptions = @()
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Changed -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Created -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Deleted -Action $action
$subscriptions += Register-ObjectEvent -InputObject $watcher -EventName Renamed -Action $action

Write-Host "DB-Notifier WPF live preview is running."
Write-Host "Edit App.xaml, DBNotifier.Desktop.ps1, or mock.instances.json to reload automatically."
Write-Host "Press Ctrl+C to stop."

try {
    while ($true) {
        Start-Sleep -Seconds 1
        if ($preview.HasExited) {
            $preview = Start-Preview
            $script:preview = $preview
        }
    }
}
finally {
    foreach ($subscription in $subscriptions) {
        Unregister-Event -SubscriptionId $subscription.Id -ErrorAction SilentlyContinue
    }

    $watcher.Dispose()
    Stop-Preview -Process $preview
}
