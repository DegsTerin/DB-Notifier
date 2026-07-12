[CmdletBinding()]
param(
    [switch]$NoDynamicMock
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName PresentationFramework
Add-Type -AssemblyName WindowsBase

$script:AppRoot = Split-Path -Path $PSCommandPath -Parent
$script:XamlPath = Join-Path -Path $script:AppRoot -ChildPath "App.xaml"
$script:MockPath = Join-Path -Path $script:AppRoot -ChildPath "mock.instances.json"

function Get-Brush {
    param([string]$Hex)
    return New-Object System.Windows.Media.SolidColorBrush ([System.Windows.Media.ColorConverter]::ConvertFromString($Hex))
}

function Get-StatusBrush {
    param([string]$Status)

    switch ($Status) {
        "Running" { return Get-Brush "#74FF75" }
        "Restarted" { return Get-Brush "#FFD24A" }
        "Stopped" { return Get-Brush "#FF6758" }
        default { return Get-Brush "#FFD24A" }
    }
}

function Get-NamedElement {
    param(
        [Parameter(Mandatory)]$Window,
        [Parameter(Mandatory)][string]$Name
    )

    $element = $Window.FindName($Name)
    if ($null -eq $element) {
        throw "XAML element '$Name' was not found."
    }
    return $element
}

function Get-MockInstances {
    if (-not (Test-Path -LiteralPath $script:MockPath)) {
        return @()
    }

    return @(Get-Content -LiteralPath $script:MockPath -Raw -Encoding UTF8 | ConvertFrom-Json)
}

function Set-InstanceRow {
    param(
        [Parameter(Mandatory)]$Window,
        [Parameter(Mandatory)][int]$Index,
        $Instance
    )

    $suffix = @("One", "Two", "Three")[$Index]
    $row = Get-NamedElement -Window $Window -Name "Row$suffix"

    if ($null -eq $Instance) {
        $row.Visibility = [System.Windows.Visibility]::Hidden
        return
    }

    $row.Visibility = [System.Windows.Visibility]::Visible
    $brush = Get-StatusBrush -Status ([string]$Instance.status)

    (Get-NamedElement -Window $Window -Name "Dot$suffix").Fill = $brush
    (Get-NamedElement -Window $Window -Name "Name$suffix").Text = [string]$Instance.name
    (Get-NamedElement -Window $Window -Name "Service$suffix").Text = [string]$Instance.service
    (Get-NamedElement -Window $Window -Name "Status$suffix").Text = [string]$Instance.status
    (Get-NamedElement -Window $Window -Name "Status$suffix").Foreground = $brush
    (Get-NamedElement -Window $Window -Name "Pid$suffix").Text = "PID: {0}" -f [string]$Instance.pid
}

function Update-Header {
    param(
        [Parameter(Mandatory)]$Window,
        [Parameter(Mandatory)]$Instances
    )

    $header = Get-NamedElement -Window $Window -Name "HeaderText"
    $bad = @($Instances | Where-Object { $_.status -eq "Stopped" }).Count

    if ($bad -eq 0) {
        $header.Text = "All instances are healthy"
        $header.Foreground = Get-Brush "#74FF75"
        return
    }

    $header.Text = "$bad instance(s) need attention"
    $header.Foreground = Get-Brush "#FFD24A"
}

function Render-Instances {
    param(
        [Parameter(Mandatory)]$Window,
        [Parameter(Mandatory)]$Instances
    )

    for ($i = 0; $i -lt 3; $i++) {
        $instance = if ($i -lt $Instances.Count) { $Instances[$i] } else { $null }
        Set-InstanceRow -Window $Window -Index $i -Instance $instance
    }

    Update-Header -Window $Window -Instances $Instances
}

function Import-XamlWindow {
    [xml]$xaml = Get-Content -LiteralPath $script:XamlPath -Raw -Encoding UTF8
    $reader = New-Object System.Xml.XmlNodeReader $xaml
    return [Windows.Markup.XamlReader]::Load($reader)
}

$window = Import-XamlWindow
$instances = @(Get-MockInstances)
Render-Instances -Window $window -Instances $instances

$window.Add_MouseLeftButtonDown({
    try {
        $this.DragMove()
    }
    catch {
    }
})

(Get-NamedElement -Window $window -Name "RestartButton").Add_Click({
    [System.Windows.MessageBox]::Show("Restart service action selected.", "DB-Notifier") | Out-Null
})

(Get-NamedElement -Window $window -Name "OpenLogButton").Add_Click({
    [System.Windows.MessageBox]::Show("Open log action selected.", "DB-Notifier") | Out-Null
})

(Get-NamedElement -Window $window -Name "OpenConfigButton").Add_Click({
    Start-Process -FilePath $script:MockPath | Out-Null
})

(Get-NamedElement -Window $window -Name "ReloadButton").Add_Click({
    $script:Instances = @(Get-MockInstances)
    Render-Instances -Window $window -Instances $script:Instances
})

(Get-NamedElement -Window $window -Name "ExitButton").Add_Click({
    $window.Close()
})

if (-not $NoDynamicMock) {
    $timer = New-Object System.Windows.Threading.DispatcherTimer
    $timer.Interval = [TimeSpan]::FromSeconds(4)
    $timer.Add_Tick({
        if ($instances.Count -lt 3) {
            return
        }

        if ($instances[1].status -eq "Restarted") {
            $instances[1].status = "Running"
            $instances[1].pid = "23457"
        }
        else {
            $instances[1].status = "Restarted"
            $instances[1].pid = "23456"
        }

        Render-Instances -Window $window -Instances $instances
    })
    $timer.Start()
}

[void]$window.ShowDialog()
