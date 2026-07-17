# Module purpose: Audits the STATE-05 WPF shell through Windows UI Automation without invoking database or service controls.
[CmdletBinding()]
param(
    [ValidateSet("pt-BR", "en-GB")]
    [string]$Locale = "pt-BR",
    [ValidateSet("light", "dark")]
    [string]$Theme = "light",
    [ValidateRange(820, 3000)]
    [int]$Width = 1180,
    [ValidateRange(620, 2200)]
    [int]$Height = 760,
    [switch]$ReviewComboBoxOverflow
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
    /// <summary>Returns the DPI-awareness context currently associated with the audited window.</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr GetWindowDpiAwarenessContext(IntPtr windowHandle);

    /// <summary>Maps a DPI-awareness context to the documented awareness classification.</summary>
    [DllImport("user32.dll")]
    public static extern int GetAwarenessFromDpiAwarenessContext(IntPtr awarenessContext);

    /// <summary>Returns the effective DPI applied by Windows to the audited WPF window.</summary>
    [DllImport("user32.dll")]
    public static extern uint GetDpiForWindow(IntPtr windowHandle);

    /// <summary>Changes only the audit thread's DPI context so UI Automation bounds and captures use physical pixels.</summary>
    [DllImport("user32.dll")]
    public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr awarenessContext);

    /// <summary>Renders one identified native window into the supplied device context.</summary>
    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr windowHandle, IntPtr deviceContext, uint flags);
}
"@
}

$root = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $root "src/DBNotifier.Desktop.Wpf/bin/Release/net10.0-windows10.0.22621.0/DBNotifier.Desktop.Wpf.exe"
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
$previousDpiAwarenessContext = [AuditNativeMethods]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
if ($previousDpiAwarenessContext -eq [IntPtr]::Zero) {
    throw "The WPF audit thread could not enter the Per-Monitor V2 DPI-awareness context."
}

try {
    [System.IO.File]::WriteAllText($preferencePath, $requestedPreferences)
    # The product is notification-area-first; the explicit review switch exposes the secondary desktop shell for this bounded audit.
    $startupArguments = [System.Collections.Generic.List[string]]::new()
    $startupArguments.Add('--show-desktop')
    if ($ReviewComboBoxOverflow) { $startupArguments.Add('--review-combobox-overflow') }
    $process = Start-Process -FilePath $executable -ArgumentList $startupArguments -PassThru
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
    for ($index = 0; $index -lt 2; $index += 1) {
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
    $inventoryGridCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        "InventoryGrid")
    $inventoryGrid = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $inventoryGridCondition)
    $inventoryScroll = $null
    if ($null -ne $inventoryGrid) {
        $scrollPatternObject = $null
        if ($inventoryGrid.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern, [ref]$scrollPatternObject)) {
            $scrollPattern = [System.Windows.Automation.ScrollPattern]$scrollPatternObject
            $inventoryScroll = [ordered]@{
                horizontallyScrollable = $scrollPattern.Current.HorizontallyScrollable
                horizontalViewSize = $scrollPattern.Current.HorizontalViewSize
                horizontalScrollPercent = $scrollPattern.Current.HorizontalScrollPercent
                verticallyScrollable = $scrollPattern.Current.VerticallyScrollable
                verticalViewSize = $scrollPattern.Current.VerticalViewSize
                verticalScrollPercent = $scrollPattern.Current.VerticalScrollPercent
            }
        }
    }
    $comboBoxOverflow = $null
    if ($ReviewComboBoxOverflow) {
        $scenarioCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            'ScenarioSelector')
        $scenarioSelector = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $scenarioCondition)
        if ($null -eq $scenarioSelector) { throw 'The scenario selector is unavailable for the isolated overflow review.' }
        $expandPattern = [System.Windows.Automation.ExpandCollapsePattern]$scenarioSelector.GetCurrentPattern(
            [System.Windows.Automation.ExpandCollapsePattern]::Pattern)
        $expandPattern.Expand()
        Start-Sleep -Milliseconds 300
        $processCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
            $process.Id)
        $scrollBarCondition = [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ScrollBar)
        $popupScrollBars = [System.Windows.Automation.AutomationElement]::RootElement.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            [System.Windows.Automation.AndCondition]::new($processCondition, $scrollBarCondition))
        $visiblePopupScrollBars = @($popupScrollBars | Where-Object { -not $_.Current.IsOffscreen })
        $comboBoxOverflow = [ordered]@{
            enabled = $true
            fixtureItemCount = $scenarioSelector.FindAll(
                [System.Windows.Automation.TreeScope]::Descendants,
                [System.Windows.Automation.Condition]::TrueCondition).Count
            visibleScrollBarCount = $visiblePopupScrollBars.Count
            scrollBars = @($visiblePopupScrollBars | ForEach-Object {
                $rangePatternObject = $null
                $range = if ($_.TryGetCurrentPattern([System.Windows.Automation.RangeValuePattern]::Pattern, [ref]$rangePatternObject)) {
                    [System.Windows.Automation.RangeValuePattern]$rangePatternObject
                }
                else { $null }
                [ordered]@{
                    name = $_.Current.Name
                    automationId = $_.Current.AutomationId
                    minimum = if ($null -ne $range) { $range.Current.Minimum } else { $null }
                    maximum = if ($null -ne $range) { $range.Current.Maximum } else { $null }
                    value = if ($null -ne $range) { $range.Current.Value } else { $null }
                }
            })
        }
        $expandPattern.Collapse()
        Start-Sleep -Milliseconds 150
    }

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
    $windowDpi = [AuditNativeMethods]::GetDpiForWindow($process.MainWindowHandle)
    $windowScalePercent = [Math]::Round(($windowDpi / 96) * 100)
    $windowDpiAwareness = [AuditNativeMethods]::GetAwarenessFromDpiAwarenessContext(
        [AuditNativeMethods]::GetWindowDpiAwarenessContext($process.MainWindowHandle))
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
        $screenshotPath = Join-Path $evidenceDirectory "wpf-main-window-$($Width)x$($Height)-$($windowDpi)dpi.png"
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
        windowDpi = $windowDpi
        windowScalePercent = $windowScalePercent
        windowDpiAwareness = $windowDpiAwareness
        focusableControlCount = $focusable.Count
        unnamedFocusable = @($focusable | Where-Object { [string]::IsNullOrWhiteSpace($_.name) })
        focusableControls = $focusable
        inventoryScroll = $inventoryScroll
        comboBoxOverflowReview = $comboBoxOverflow
        preferenceCycles = [ordered]@{ language = $languageCycle; theme = $themeCycle }
        tabSequence = $tabSequence
        screenshotPath = $screenshotPath
    }
    $reportPath = Join-Path $evidenceDirectory "wpf-audit-$($Width)x$($Height)-$($windowDpi)dpi.json"
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
    $failures = [System.Collections.Generic.List[string]]::new()
    if ([Math]::Abs($bounds.Width - $Width) -gt 2 -or [Math]::Abs($bounds.Height - $Height) -gt 2) {
        $failures.Add("The audited window did not preserve the requested dimensions.")
    }
    if ($focusable.Count -lt 12 -or @($focusable | Where-Object { [string]::IsNullOrWhiteSpace($_.name) }).Count -gt 0) {
        $failures.Add("The WPF shell exposed too few focusable controls or an unnamed focus target.")
    }
    if ($tabSequence.Count -ne 12 -or @($tabSequence | Where-Object offscreen).Count -gt 0) {
        $failures.Add("Keyboard traversal did not preserve twelve visible in-process focus targets.")
    }
    if ($languageCycle.Count -ne 3 -or $languageCycle[0] -ne $languageCycle[2] -or $languageCycle[0] -eq $languageCycle[1] -or @($languageCycle | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) {
        $failures.Add("The language preference did not change and return to its accessible initial state.")
    }
    if ($themeCycle.Count -ne 3 -or $themeCycle[0] -ne $themeCycle[2] -or $themeCycle[0] -eq $themeCycle[1] -or @($themeCycle | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -gt 0) {
        $failures.Add("The theme preference did not change and return to its accessible initial state.")
    }
    if ($windowDpi -lt 96 -or $windowDpiAwareness -ne 2) {
        $failures.Add("The WPF window did not expose a Per-Monitor DPI-aware context at a supported DPI.")
    }
    if ($ReviewComboBoxOverflow -and
        ($null -eq $comboBoxOverflow -or $comboBoxOverflow.visibleScrollBarCount -lt 1 -or
         @($comboBoxOverflow.scrollBars | Where-Object { $null -ne $_.maximum -and $_.maximum -gt $_.minimum }).Count -lt 1)) {
        $failures.Add("The isolated ComboBox overflow review did not expose a usable vertical scrollbar.")
    }
    if (-not (Test-Path -LiteralPath $screenshotPath -PathType Leaf) -or (Get-Item -LiteralPath $screenshotPath).Length -eq 0) {
        $failures.Add("The WPF audit screenshot is missing or empty.")
    }
    if ($failures.Count -gt 0) {
        throw "WPF audit failed: $($failures -join ' ') Evidence: $reportPath"
    }
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
    [AuditNativeMethods]::SetThreadDpiAwarenessContext($previousDpiAwarenessContext) | Out-Null
}
