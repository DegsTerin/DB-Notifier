# Module purpose: Audits the STATE-05 WPF shell through Windows UI Automation without invoking database or service controls.
[CmdletBinding()]
param(
    [ValidateSet("pt-BR", "en-GB")]
    [string]$Locale = "pt-BR",
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

$root = Split-Path -Parent $PSScriptRoot
$executable = Join-Path $root "src/DBNotifier.Desktop.Wpf/bin/Release/net10.0-windows/DBNotifier.Desktop.Wpf.exe"
if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Build the Release WPF application before running this audit."
}

$evidenceDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "DBNotifier-State05-Audit/$Locale"
[System.IO.Directory]::CreateDirectory($evidenceDirectory) | Out-Null
$process = Start-Process -FilePath $executable -PassThru

try {
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
        "LanguageSelector")
    $languageSelector = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $languageCondition)
    if ($null -eq $languageSelector) {
        throw "The desktop language selector is unavailable."
    }
    $expandPattern = $languageSelector.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern)
    $expandPattern.Expand()
    Start-Sleep -Milliseconds 150
    $languageName = if ($Locale -eq "en-GB") { "English (UK)" } else { "Português (Brasil)" }
    $languageItemCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $languageName)
    $languageItem = $languageSelector.FindFirst([System.Windows.Automation.TreeScope]::Subtree, $languageItemCondition)
    if ($null -eq $languageItem) {
        throw "The requested desktop language item is unavailable."
    }
    $selectionPattern = $languageItem.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
    $selectionPattern.Select()
    $expandPattern.Collapse()
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
        $graphics.CopyFromScreen([int]$bounds.X, [int]$bounds.Y, 0, 0, $bitmap.Size)
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
        windowTitle = $window.Current.Name
        processId = $process.Id
        windowName = $window.Current.Name
        bounds = [ordered]@{ width = $bounds.Width; height = $bounds.Height }
        focusableControlCount = $focusable.Count
        unnamedFocusable = @($focusable | Where-Object { [string]::IsNullOrWhiteSpace($_.name) })
        focusableControls = $focusable
        tabSequence = $tabSequence
        screenshotPath = $screenshotPath
    }
    $reportPath = Join-Path $evidenceDirectory "wpf-audit-$($Width)x$($Height).json"
    $report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $reportPath -Encoding utf8
    [ordered]@{ reportPath = $reportPath; report = $report } | ConvertTo-Json -Depth 8
}
finally {
    if (-not $process.HasExited) {
        $process.CloseMainWindow() | Out-Null
        if (-not $process.WaitForExit(3000)) {
            Stop-Process -Id $process.Id -Force
        }
    }
}
