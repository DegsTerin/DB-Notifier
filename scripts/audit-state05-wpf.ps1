# Module purpose: Audits the STATE-05 WPF shell through Windows UI Automation without invoking database or service controls.
[CmdletBinding()]
param(
    [ValidateSet("pt-BR", "en-GB")]
    [string]$Locale = "pt-BR",
    [ValidateSet("system", "light", "dark")]
    [string]$Theme = "system",
    [ValidateRange(820, 3000)]
    [int]$Width = 1180,
    [ValidateRange(620, 2200)]
    [int]$Height = 760
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
if (-not ("AuditNativeMethods" -as [type])) {
    Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

/// <summary>Provides the target-window-only capture required by the WPF audit.</summary>
public static class AuditNativeMethods
{
    /// <summary>Renders one identified native window into the supplied device context.</summary>
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr windowHandle, IntPtr deviceContext, uint flags);
}
"@
}

$root = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $root "src/DBNotifier.Desktop.Wpf/bin/Release/net10.0-windows/DBNotifier.Desktop.Wpf.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Build the Release WPF application before running this audit."
}

$evidenceDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-State05-Audit/$Locale/$Theme"
[System.IO.Directory]::CreateDirectory($evidenceDirectory) | Out-Null
$preferencePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "DB-Notifier/ui-preferences.v1.json"
$preferenceExisted = [System.IO.File]::Exists($preferencePath)
$preferenceBytes = if ($preferenceExisted) { [System.IO.File]::ReadAllBytes($preferencePath) } else { $null }
$preferenceDirectory = [System.IO.Path]::GetDirectoryName($preferencePath)
[System.IO.Directory]::CreateDirectory($preferenceDirectory) | Out-Null
$requestedPreferences = [ordered]@{
    SchemaVersion = "dbnotifier.ui-preferences.v1"
    Language = $Locale
    Theme = $Theme
} | ConvertTo-Json -Compress
$process = $null

try {
    [System.IO.File]::WriteAllText($preferencePath, $requestedPreferences)
    $process = Start-Process -FilePath $executable -PassThru
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 200
        $process.Refresh()
    } while ($process.MainWindowHandle -eq [IntPtr]::Zero -and [DateTime]::UtcNow -lt $deadline)

    if ($process.MainWindowHandle -eq [IntPtr]::Zero) {
        throw "The WPF main window did not become available within 15 seconds."
    }

    $window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
    $condition = [System.Windows.Automation.Condition]::TrueCondition
    $transformPattern = $window.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
    if ($transformPattern.Current.CanResize) {
        $transformPattern.Resize($Width, $Height)
        Start-Sleep -Milliseconds 200
    }
    $languageCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        "LanguagePreferenceButton")
    $languageButton = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $languageCondition)
    if ($null -eq $languageButton) {
        throw "The desktop language preference button is unavailable."
    }

    $themeCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        "ThemePreferenceButton")
    $themeButton = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $themeCondition)
    if ($null -eq $themeButton) {
        throw "The desktop theme preference button is unavailable."
    }

    # Each icon button is invoked through UI Automation and returned to its requested initial preference.
    $languageInvoke = $languageButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $themeInvoke = $themeButton.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)
    $languageCycle = @($languageButton.Current.Name)
    $languageInvoke.Invoke()
    Start-Sleep -Milliseconds 150
    $languageCycle += $languageButton.Current.Name
    $languageInvoke.Invoke()
    Start-Sleep -Milliseconds 150
    $languageCycle += $languageButton.Current.Name
    $themeCycle = @($themeButton.Current.Name)
    for ($index = 0; $index -lt 3; $index += 1) {
        $themeInvoke.Invoke()
        Start-Sleep -Milliseconds 150
        $themeCycle += $themeButton.Current.Name
    }
    Start-Sleep -Milliseconds 250
    $elements = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    $focusable = @(
        foreach ($element in $elements) {
            if ($element.Current.IsKeyboardFocusable -and -not $element.Current.IsOffscreen) {
                [ordered]@{
                    name = $element.Current.Name
                    automationId = $element.Current.AutomationId
                    controlType = $element.Current.ControlType.ProgrammaticName
                }
            }
        }
    )

    # Window focus establishes a repeatable starting point before native Tab traversal.
    $window.SetFocus()
    Start-Sleep -Milliseconds 150
    $tabSequence = @()
    for ($index = 0; $index -lt 12; $index += 1) {
        [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
        Start-Sleep -Milliseconds 100
        $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
        if ($focused.Current.ProcessId -ne $process.Id) {
            throw "Keyboard focus left the audited DB-Notifier process; no external UI details were collected."
        }
        $tabSequence += [ordered]@{
            name = $focused.Current.Name
            automationId = $focused.Current.AutomationId
            controlType = $focused.Current.ControlType.ProgrammaticName
            offscreen = $focused.Current.IsOffscreen
        }
    }

    $bounds = $window.Current.BoundingRectangle
    $bitmap = [System.Drawing.Bitmap]::new([int]$bounds.Width, [int]$bounds.Height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $deviceContext = $graphics.GetHdc()
        try {
            if (-not [AuditNativeMethods]::PrintWindow($process.MainWindowHandle, $deviceContext, 2)) {
                throw "The target WPF window could not be rendered for audit evidence."
            }
        }
        finally {
            $graphics.ReleaseHdc($deviceContext)
        }
        $screenshotPath = Join-Path $evidenceDirectory "wpf-main-window-$($Width)x$($Height).png"
        $bitmap.Save($screenshotPath, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }

    $report = [ordered]@{
        generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
        locale = $Locale
        theme = $Theme
        windowTitle = $window.Current.Name
        processId = $process.Id
        windowName = $window.Current.Name
        bounds = [ordered]@{ width = $bounds.Width; height = $bounds.Height }
        focusableControlCount = $focusable.Count
        unnamedFocusable = @($focusable | Where-Object { [string]::IsNullOrWhiteSpace($_.name) })
        focusableControls = $focusable
        preferenceCycles = [ordered]@{ language = $languageCycle; theme = $themeCycle }
        tabSequence = $tabSequence
        screenshotPath = $screenshotPath
    }
    $reportPath = Join-Path $evidenceDirectory "wpf-audit-$($Width)x$($Height).json"
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
    [ordered]@{ reportPath = $reportPath; report = $report } | ConvertTo-Json -Depth 8
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) {
            Stop-Process -Id $process.Id -Force
        }
    }
    if ($preferenceExisted) {
        [System.IO.File]::WriteAllBytes($preferencePath, $preferenceBytes)
    }
    elseif ([System.IO.File]::Exists($preferencePath)) {
        Remove-Item -LiteralPath $preferencePath -Force
    }
}
