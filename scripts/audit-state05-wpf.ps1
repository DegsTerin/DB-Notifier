# Module purpose: Audits the bounded WPF presentation matrix through Windows UI Automation without invoking operational controls.
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
    [switch]$ReviewComboBoxOverflow,
    [switch]$ParityMatrix
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

/// <summary>Provides target-window DPI and capture operations for the WPF audit.</summary>
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

$ExpectedPreferenceHash = "ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F"
$TemporaryRoot = [System.IO.Path]::GetFullPath([System.IO.Path]::GetTempPath()).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
$Root = Split-Path -Parent $PSScriptRoot
$Executable = Join-Path $Root "src/DBNotifier.Desktop.Wpf/bin/Release/net10.0-windows10.0.22621.0/DBNotifier.Desktop.Wpf.exe"
$PreferencePath = Join-Path ([Environment]::GetFolderPath([Environment+SpecialFolder]::LocalApplicationData)) "DB-Notifier/ui-preferences.v1.json"
$EvidenceRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-R6-WPF2-Audit-" + [Guid]::NewGuid().ToString("N"))

if (-not (Test-Path -LiteralPath $Executable -PathType Leaf)) {
    throw "Build the Release WPF application before running this audit."
}
if (-not [System.IO.File]::Exists($PreferencePath)) {
    throw "The authorised preference file does not exist; no temporary preference change was made."
}
$PreferenceBytes = [System.IO.File]::ReadAllBytes($PreferencePath)
$PreferenceHashBefore = (Get-FileHash -LiteralPath $PreferencePath -Algorithm SHA256).Hash.ToUpperInvariant()
if ($PreferenceHashBefore -ne $ExpectedPreferenceHash) {
    throw "The authorised preference baseline hash is not present; no temporary preference change was made."
}

# Loads the canonical generated-message source into a key/value map.
function Get-LocalisationMap {
    param([Parameter(Mandatory = $true)][string]$Culture)
    # Windows PowerShell 5.1 otherwise decodes the BOM-less canonical XML with the active ANSI code page.
    [xml]$document = Get-Content -LiteralPath (Join-Path $Root "localisation/messages.$Culture.xml") -Raw -Encoding UTF8
    $result = @{}
    foreach ($message in $document.localisation.message) {
        $result[[string]$message.key] = [string]$message.InnerText
    }
    return $result
}

# Finds one descendant by stable WPF automation identifier.
function Find-AuditElement {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Window,
        [Parameter(Mandatory = $true)][string]$AutomationId
    )
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)
    return $Window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

# Returns a serialisable finite rectangle for geometry evidence.
function Get-AuditRectangle {
    param([Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element)
    $rectangle = $Element.Current.BoundingRectangle
    return [ordered]@{
        x = $rectangle.X
        y = $rectangle.Y
        width = $rectangle.Width
        height = $rectangle.Height
        offscreen = $Element.Current.IsOffscreen
    }
}

# Determines whether one positive finite rectangle stays inside another with a small DPI tolerance.
function Test-AuditContainment {
    param(
        [Parameter(Mandatory = $true)]$Child,
        [Parameter(Mandatory = $true)]$Owner,
        [double]$Tolerance = 3
    )
    $finite = @(@($Child.x, $Child.y, $Child.width, $Child.height, $Owner.x, $Owner.y, $Owner.width, $Owner.height) |
        Where-Object { [double]::IsNaN([double]$_) -or [double]::IsInfinity([double]$_) })
    if ($finite.Count -gt 0 -or $Child.width -le 0 -or $Child.height -le 0) { return $false }
    return $Child.x -ge ($Owner.x - $Tolerance) -and
        $Child.y -ge ($Owner.y - $Tolerance) -and
        ($Child.x + $Child.width) -le ($Owner.x + $Owner.width + $Tolerance) -and
        ($Child.y + $Child.height) -le ($Owner.y + $Owner.height + $Tolerance)
}

# Determines whether a component remains horizontally contained while its vertical position may be reached by bounded scrolling.
function Test-AuditHorizontalContainment {
    param(
        [Parameter(Mandatory = $true)]$Child,
        [Parameter(Mandatory = $true)]$Owner,
        [double]$Tolerance = 3
    )
    return $Child.width -gt 0 -and
        $Child.x -ge ($Owner.x - $Tolerance) -and
        ($Child.x + $Child.width) -le ($Owner.x + $Owner.width + $Tolerance)
}

# Captures only the identified DB Notifier window and returns its path and SHA-256 digest.
function Save-AuditWindow {
    param(
        [Parameter(Mandatory = $true)][IntPtr]$Handle,
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Window,
        [Parameter(Mandatory = $true)][string]$Path
    )
    $bounds = $Window.Current.BoundingRectangle
    if ($bounds.Width -le 0 -or $bounds.Height -le 0) { throw "The audited window has invalid capture bounds." }
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    $bitmap = [System.Drawing.Bitmap]::new([int][Math]::Ceiling($bounds.Width), [int][Math]::Ceiling($bounds.Height))
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    try {
        $deviceContext = $graphics.GetHdc()
        try {
            if (-not [AuditNativeMethods]::PrintWindow($Handle, $deviceContext, 2)) {
                throw "The target WPF window could not be rendered for audit evidence."
            }
        }
        finally { $graphics.ReleaseHdc($deviceContext) }
        $bitmap.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $bitmap.Dispose()
    }
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf) -or (Get-Item -LiteralPath $Path).Length -eq 0) {
        throw "The target-window screenshot is missing or empty."
    }
    return [ordered]@{ path = $Path; sha256 = (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToUpperInvariant() }
}

# Stops only the exact audit process after proving PID, executable path and start time.
function Stop-ExactAuditProcess {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$ExpectedPath,
        [DateTime]$ExpectedStartTime
    )
    if ($null -eq $Process) { return [ordered]@{ stopped = $true; reason = "not-started" } }
    $candidate = Get-Process -Id $Process.Id -ErrorAction SilentlyContinue
    if ($null -eq $candidate) { return [ordered]@{ stopped = $true; reason = "already-exited" } }
    $actualPath = [System.IO.Path]::GetFullPath($candidate.Path)
    $actualStartTime = $candidate.StartTime.ToUniversalTime()
    if (-not [string]::Equals($actualPath, $ExpectedPath, [StringComparison]::OrdinalIgnoreCase) -or
        [Math]::Abs(($actualStartTime - $ExpectedStartTime).TotalMilliseconds) -gt 10) {
        throw "The audit process identity changed; cleanup refused to target an unproved process."
    }
    Stop-Process -Id $candidate.Id -Force
    $deadline = [DateTime]::UtcNow.AddSeconds(5)
    do {
        Start-Sleep -Milliseconds 100
        $candidate = Get-Process -Id $Process.Id -ErrorAction SilentlyContinue
    } while ($null -ne $candidate -and [DateTime]::UtcNow -lt $deadline)
    if ($null -ne $candidate) { throw "The exact WPF audit process did not exit within five seconds." }
    return [ordered]@{ stopped = $true; reason = "exact-force-stop" }
}

# Removes one exact project-owned temporary directory after proving it remains below the operating-system temporary root.
function Remove-OwnedAuditDirectory {
    param([Parameter(Mandatory = $true)][string]$Path)
    $resolved = [System.IO.Path]::GetFullPath($Path).TrimEnd([System.IO.Path]::DirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (-not $resolved.StartsWith($TemporaryRoot, [StringComparison]::OrdinalIgnoreCase) -or $resolved.Length -le $TemporaryRoot.Length) {
        throw "Audit cleanup refused a directory outside the exact temporary root."
    }
    if ([System.IO.Directory]::Exists($Path)) { [System.IO.Directory]::Delete($Path, $true) }
    if ([System.IO.Directory]::Exists($Path)) { throw "The owned WPF audit state directory remains after cleanup." }
}

# Reserves and releases one loopback port so the quiet sandbox can fail closed without external communication.
function Get-UnboundLoopbackUri {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { $port = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port }
    finally { $listener.Stop() }
    return "https://127.0.0.1:$port/"
}

$Routes = @(
    [ordered]@{ id = "Overview"; titleKey = "View.Overview.Title"; components = @("OverviewTotalCard", "OverviewHealthyCard", "OverviewWarningCard", "OverviewCriticalCard", "OverviewInventoryList", "OverviewAlertList", "OverviewPerformanceChart", "OverviewProviderDistribution") },
    [ordered]@{ id = "Inventory"; titleKey = "View.Inventory.Title"; components = @("InventoryTotalCard", "InventoryHealthyCard", "InventoryDegradedCard", "InventoryAttentionCard", "InventoryStaleCard", "InventoryDisabledCard", "InventoryGrid") },
    [ordered]@{ id = "Alerts"; titleKey = "View.Alerts.Title"; components = @("AlertCriticalCard", "AlertActiveCard", "AlertTotalCard", "AlertsGrid") },
    [ordered]@{ id = "Performance"; titleKey = "View.Performance.Title"; components = @("PerformanceRouteChart") },
    [ordered]@{ id = "History"; titleKey = "View.History.Title"; components = @("HistoryGrid", "HistorySummaryText") },
    [ordered]@{ id = "Configuration"; titleKey = "View.Configuration.Title"; components = @("ConfigurationGrid", "CapabilityGrid", "PermissionSelector") },
    [ordered]@{ id = "Providers"; titleKey = "View.Providers.Title"; components = @("ProvidersDistribution", "ProviderCatalogueList") },
    [ordered]@{ id = "Settings"; titleKey = "View.Settings.Title"; components = @("SettingsPreferenceTitleText", "SettingsLanguageText", "SettingsThemeText", "SettingsNotificationTitleText") }
)

# Runs one locale, theme and native-window-size sample across all eight presentation routes.
function Invoke-WpfAuditSample {
    param(
        [Parameter(Mandatory = $true)][string]$SampleLocale,
        [Parameter(Mandatory = $true)][string]$SampleTheme,
        [Parameter(Mandatory = $true)][int]$SampleWidth,
        [Parameter(Mandatory = $true)][int]$SampleHeight
    )
    $messages = Get-LocalisationMap -Culture $SampleLocale
    $sampleName = "$SampleLocale-$SampleTheme-$($SampleWidth)x$SampleHeight"
    $sampleDirectory = Join-Path $EvidenceRoot $sampleName
    $stateDirectory = Join-Path $sampleDirectory "state"
    [System.IO.Directory]::CreateDirectory($stateDirectory) | Out-Null
    $requestedPreferences = [ordered]@{
        SchemaVersion = "dbnotifier.ui-preferences.v1"
        Language = $SampleLocale
        Theme = $SampleTheme
    } | ConvertTo-Json -Compress
    [System.IO.File]::WriteAllText($PreferencePath, $requestedPreferences, [System.Text.UTF8Encoding]::new($false))

    $process = $null
    $expectedExecutablePath = [System.IO.Path]::GetFullPath($Executable)
    $expectedStartTime = [DateTime]::MinValue
    $processCleanup = $null
    try {
        $apiBase = Get-UnboundLoopbackUri
        $arguments = @(
            "--show-desktop",
            "--reconciled-notification-sandbox",
            "--notifications-opt-in",
            "--notifications-quiet",
            "--api-base", $apiBase,
            "--state-directory", ('"' + $stateDirectory + '"'),
            "--test-certificate-thumbprint", ("A" * 64),
            "--test-subject", "r6-wpf2-audit"
        )
        if ($ReviewComboBoxOverflow) { $arguments += "--review-combobox-overflow" }
        $process = Start-Process -FilePath $Executable -ArgumentList $arguments -PassThru
        $expectedStartTime = $process.StartTime.ToUniversalTime()
        $deadline = [DateTime]::UtcNow.AddSeconds(15)
        do {
            Start-Sleep -Milliseconds 150
            $process.Refresh()
        } while ($process.MainWindowHandle -eq [IntPtr]::Zero -and -not $process.HasExited -and [DateTime]::UtcNow -lt $deadline)
        if ($process.HasExited -or $process.MainWindowHandle -eq [IntPtr]::Zero) {
            throw "The WPF main window did not become available within fifteen seconds."
        }
        if (-not [string]::Equals([System.IO.Path]::GetFullPath($process.Path), $expectedExecutablePath, [StringComparison]::OrdinalIgnoreCase)) {
            throw "The launched audit process path does not match the authorised WPF executable."
        }

        $window = [System.Windows.Automation.AutomationElement]::FromHandle($process.MainWindowHandle)
        $transformPattern = [System.Windows.Automation.TransformPattern]$window.GetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern)
        if ($transformPattern.Current.CanResize) {
            $transformPattern.Resize($SampleWidth, $SampleHeight)
            Start-Sleep -Milliseconds 250
        }
        $windowDpi = [AuditNativeMethods]::GetDpiForWindow($process.MainWindowHandle)
        $windowScalePercent = [Math]::Round(($windowDpi / 96) * 100)
        $windowDpiAwareness = [AuditNativeMethods]::GetAwarenessFromDpiAwarenessContext(
            [AuditNativeMethods]::GetWindowDpiAwarenessContext($process.MainWindowHandle))
        $windowRectangle = Get-AuditRectangle -Element $window
        $expectedPhysicalWidth = $SampleWidth * $windowDpi / 96
        $expectedPhysicalHeight = $SampleHeight * $windowDpi / 96
        $sampleFailures = [System.Collections.Generic.List[string]]::new()
        if ([Math]::Abs($windowRectangle.width - $expectedPhysicalWidth) -gt 4 -or [Math]::Abs($windowRectangle.height - $expectedPhysicalHeight) -gt 4) {
            $sampleFailures.Add("The audited native window did not preserve the requested DPI-scaled dimensions.")
        }
        if ($windowDpi -lt 96 -or $windowDpiAwareness -ne 2) {
            $sampleFailures.Add("The WPF window did not expose a Per-Monitor V2 context at a supported DPI.")
        }

        $contentScroller = Find-AuditElement -Window $window -AutomationId "DesktopContentScrollViewer"
        $routeRows = @()
        foreach ($route in $Routes) {
            $routeFailures = [System.Collections.Generic.List[string]]::new()
            $navigation = Find-AuditElement -Window $window -AutomationId ($route.id + "NavigationButton")
            if ($null -eq $navigation) { throw "The $($route.id) navigation control is unavailable." }
            ([System.Windows.Automation.InvokePattern]$navigation.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
            $titleDeadline = [DateTime]::UtcNow.AddSeconds(3)
            do {
                Start-Sleep -Milliseconds 75
                $titleElement = Find-AuditElement -Window $window -AutomationId "ViewTitleText"
            } while (($null -eq $titleElement -or $titleElement.Current.Name -ne $messages[$route.titleKey]) -and [DateTime]::UtcNow -lt $titleDeadline)
            if ($null -eq $titleElement -or $titleElement.Current.Name -ne $messages[$route.titleKey]) {
                $routeFailures.Add("The route title did not match the canonical localisation source.")
            }
            if ($window.Current.Name -ne ("DB Notifier " + [char]0x2014 + " " + $messages[$route.titleKey])) {
                $routeFailures.Add("The native window title did not match the active route.")
            }
            $currentNavigationCount = 0
            foreach ($candidateRoute in $Routes) {
                $candidate = Find-AuditElement -Window $window -AutomationId ($candidateRoute.id + "NavigationButton")
                if ($null -ne $candidate -and -not [string]::IsNullOrWhiteSpace($candidate.Current.ItemStatus)) { $currentNavigationCount += 1 }
            }
            if ($currentNavigationCount -ne 1 -or [string]::IsNullOrWhiteSpace($navigation.Current.ItemStatus)) {
                $routeFailures.Add("The current-route indication was not unique and textual.")
            }

            $outerScroll = [ordered]@{ horizontallyScrollable = $false; verticallyScrollable = $false; verticalViewSize = 100 }
            $scroll = $null
            if ($null -ne $contentScroller) {
                $scrollObject = $null
                if ($contentScroller.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern, [ref]$scrollObject)) {
                    $scroll = [System.Windows.Automation.ScrollPattern]$scrollObject
                    $outerScroll = [ordered]@{
                        horizontallyScrollable = $scroll.Current.HorizontallyScrollable
                        verticallyScrollable = $scroll.Current.VerticallyScrollable
                        verticalViewSize = $scroll.Current.VerticalViewSize
                    }
                    if ($scroll.Current.HorizontallyScrollable) {
                        $routeFailures.Add("The outer content surface exposed forbidden horizontal scrolling.")
                    }
                }
            }
            $contentRectangle = if ($null -ne $contentScroller) { Get-AuditRectangle -Element $contentScroller } else { $windowRectangle }
            $deferredComponentIds = [System.Collections.Generic.List[string]]::new()

            $componentBounds = [ordered]@{}
            foreach ($componentId in $route.components) {
                $component = Find-AuditElement -Window $window -AutomationId $componentId
                if ($null -eq $component) {
                    $routeFailures.Add("Required component '$componentId' is absent from the automation tree.")
                    continue
                }
                $rectangle = Get-AuditRectangle -Element $component
                $componentBounds[$componentId] = $rectangle
                if (-not (Test-AuditHorizontalContainment -Child $rectangle -Owner $contentRectangle)) {
                    $routeFailures.Add("Component '$componentId' is horizontally clipped outside the content viewport.")
                }
                elseif (-not (Test-AuditContainment -Child $rectangle -Owner $contentRectangle)) {
                    if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                        $deferredComponentIds.Add($componentId)
                    }
                    elseif (-not $rectangle.offscreen) {
                        $routeFailures.Add("Visible component '$componentId' is clipped outside the content viewport.")
                    }
                }
            }

            if ($route.id -eq "Overview" -or $route.id -eq "Performance") {
                $chartId = if ($route.id -eq "Overview") { "OverviewPerformanceChart" } else { "PerformanceRouteChart" }
                $chart = Find-AuditElement -Window $window -AutomationId $chartId
                foreach ($axisLabel in @("100%", "50%", "0%", "09:50", "09:55", "10:00", "10:05", "10:10", "10:15")) {
                    $axisCondition = [System.Windows.Automation.PropertyCondition]::new([System.Windows.Automation.AutomationElement]::NameProperty, $axisLabel)
                    $axisElement = if ($null -ne $chart) { $chart.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $axisCondition) } else { $null }
                    if ($null -eq $axisElement) { $routeFailures.Add("Chart label '$axisLabel' is missing from $chartId.") }
                }
            }

            $routeDirectory = Join-Path $sampleDirectory $route.id.ToLowerInvariant()
            $topCapture = Save-AuditWindow -Handle $process.MainWindowHandle -Window $window -Path (Join-Path $routeDirectory "top-$($windowDpi)dpi.png")
            $bottomCapture = $null
            if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 100)
                Start-Sleep -Milliseconds 100
                $bottomCapture = Save-AuditWindow -Handle $process.MainWindowHandle -Window $window -Path (Join-Path $routeDirectory "bottom-$($windowDpi)dpi.png")
                foreach ($componentId in $deferredComponentIds) {
                    $reachable = $false
                    foreach ($verticalPercent in @(0, 20, 40, 60, 80, 100)) {
                        $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, $verticalPercent)
                        Start-Sleep -Milliseconds 40
                        $component = Find-AuditElement -Window $window -AutomationId $componentId
                        if ($null -ne $component -and -not $component.Current.IsOffscreen -and
                            (Test-AuditContainment -Child (Get-AuditRectangle -Element $component) -Owner $contentRectangle)) {
                            $reachable = $true
                            break
                        }
                    }
                    if (-not $reachable) {
                        $routeFailures.Add("Component '$componentId' was not fully reachable through bounded vertical scrolling.")
                    }
                }
                $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 0)
                Start-Sleep -Milliseconds 75
            }

            $status = if ($routeFailures.Count -eq 0) { "PASS" } else { "FAIL" }
            $routeRows += [ordered]@{
                locale = $SampleLocale
                theme = $SampleTheme
                size = "$($SampleWidth)x$SampleHeight"
                route = $route.id
                status = $status
                failures = @($routeFailures)
                components = $componentBounds
                outerScroll = $outerScroll
                topCapture = $topCapture
                bottomCapture = $bottomCapture
            }
            foreach ($failure in $routeFailures) { $sampleFailures.Add("$($route.id): $failure") }
        }

        ([System.Windows.Automation.InvokePattern](Find-AuditElement -Window $window -AutomationId "OverviewNavigationButton").GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern)).Invoke()
        Start-Sleep -Milliseconds 100
        $allElements = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, [System.Windows.Automation.Condition]::TrueCondition)
        $focusable = @(
            foreach ($element in $allElements) {
                if ($element.Current.IsKeyboardFocusable -and -not $element.Current.IsOffscreen) {
                    [ordered]@{ name = $element.Current.Name; automationId = $element.Current.AutomationId; controlType = $element.Current.ControlType.ProgrammaticName }
                }
            }
        )
        $unnamedFocusable = @($focusable | Where-Object { [string]::IsNullOrWhiteSpace($_.name) })
        if ($focusable.Count -lt 12 -or $unnamedFocusable.Count -gt 0) {
            $sampleFailures.Add("The WPF shell exposed too few focusable controls or an unnamed focus target.")
        }
        $window.SetFocus()
        Start-Sleep -Milliseconds 100
        $tabSequence = @()
        for ($index = 0; $index -lt 12; $index += 1) {
            [System.Windows.Forms.SendKeys]::SendWait("{TAB}")
            Start-Sleep -Milliseconds 75
            $focused = [System.Windows.Automation.AutomationElement]::FocusedElement
            if ($focused.Current.ProcessId -ne $process.Id) { throw "Keyboard focus left the exact audited process." }
            $tabSequence += [ordered]@{ name = $focused.Current.Name; automationId = $focused.Current.AutomationId; offscreen = $focused.Current.IsOffscreen }
        }
        if (@($tabSequence | Where-Object { $_.offscreen -or [string]::IsNullOrWhiteSpace($_.name) }).Count -gt 0) {
            $sampleFailures.Add("Keyboard traversal reached an offscreen or unnamed target.")
        }

        return [ordered]@{
            locale = $SampleLocale
            theme = $SampleTheme
            width = $SampleWidth
            height = $SampleHeight
            status = if ($sampleFailures.Count -eq 0) { "PASS" } else { "FAIL" }
            failures = @($sampleFailures)
            process = [ordered]@{ id = $process.Id; path = $expectedExecutablePath; startTimeUtc = $expectedStartTime.ToString("O") }
            dpi = $windowDpi
            scalePercent = $windowScalePercent
            dpiAwareness = $windowDpiAwareness
            routes = $routeRows
            focusableCount = $focusable.Count
            unnamedFocusable = $unnamedFocusable
            tabSequence = $tabSequence
        }
    }
    finally {
        try { $processCleanup = Stop-ExactAuditProcess -Process $process -ExpectedPath $expectedExecutablePath -ExpectedStartTime $expectedStartTime }
        finally { Remove-OwnedAuditDirectory -Path $stateDirectory }
    }
}

$previousDpiAwarenessContext = [AuditNativeMethods]::SetThreadDpiAwarenessContext([IntPtr]::new(-4))
if ($previousDpiAwarenessContext -eq [IntPtr]::Zero) { throw "The audit thread could not enter the Per-Monitor V2 DPI-awareness context." }
[System.IO.Directory]::CreateDirectory($EvidenceRoot) | Out-Null
$campaign = [ordered]@{
    schemaVersion = "dbnotifier.r6-wpf2-audit.v1"
    generatedAt = [DateTimeOffset]::UtcNow.ToString("O")
    expectedPreferenceHash = $ExpectedPreferenceHash
    preferenceHashBefore = $PreferenceHashBefore
    preferenceHashAfter = $null
    workArea = [ordered]@{
        width = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Width
        height = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea.Height
    }
    samples = @()
    notTested = @()
    processResidue = @()
    ownedStateResidue = @()
}
$campaignFailure = $null
try {
    $locales = if ($ParityMatrix) { @("pt-BR", "en-GB") } else { @($Locale) }
    $themes = if ($ParityMatrix) { @("light", "dark") } else { @($Theme) }
    $sizes = if ($ParityMatrix) {
        @(
            [pscustomobject]@{ width = 820; height = 620 },
            [pscustomobject]@{ width = 1180; height = 760 },
            [pscustomobject]@{ width = 1920; height = 1080 }
        )
    }
    else { @([pscustomobject]@{ width = $Width; height = $Height }) }
    foreach ($sampleLocale in $locales) {
        foreach ($sampleTheme in $themes) {
            foreach ($size in $sizes) {
                $sampleWidth = [int]$size.width
                $sampleHeight = [int]$size.height
                if ($sampleWidth -gt $campaign.workArea.width -or $sampleHeight -gt $campaign.workArea.height) {
                    foreach ($route in $Routes) {
                        $campaign.notTested += [ordered]@{
                            locale = $sampleLocale
                            theme = $sampleTheme
                            size = "$($sampleWidth)x$sampleHeight"
                            route = $route.id
                            status = "NOT_TESTED"
                            reason = "The active Windows work area cannot contain the requested native window size without changing host configuration."
                        }
                    }
                    continue
                }
                $sample = Invoke-WpfAuditSample -SampleLocale $sampleLocale -SampleTheme $sampleTheme -SampleWidth $sampleWidth -SampleHeight $sampleHeight
                $campaign.samples += $sample
            }
        }
    }
}
catch {
    $campaignFailure = $_
}
finally {
    try {
        [System.IO.File]::WriteAllBytes($PreferencePath, $PreferenceBytes)
        $campaign.preferenceHashAfter = (Get-FileHash -LiteralPath $PreferencePath -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($campaign.preferenceHashAfter -ne $ExpectedPreferenceHash) {
            throw "The WPF audit failed to restore the authorised preference bytes."
        }
        $ownedPrefix = [System.IO.Path]::GetFullPath($EvidenceRoot)
        $campaign.ownedStateResidue = @(Get-ChildItem -LiteralPath $EvidenceRoot -Directory -Recurse -Force | Where-Object { $_.Name -eq "state" } | ForEach-Object { $_.FullName })
        $campaign.processResidue = @(Get-CimInstance Win32_Process | Where-Object {
            $_.ExecutablePath -and [string]::Equals([System.IO.Path]::GetFullPath($_.ExecutablePath), [System.IO.Path]::GetFullPath($Executable), [StringComparison]::OrdinalIgnoreCase)
        } | ForEach-Object { $_.ProcessId })
        if ($campaign.ownedStateResidue.Count -gt 0 -or $campaign.processResidue.Count -gt 0) {
            throw "The WPF audit cleanup left an owned process or state-directory residue."
        }
    }
    catch {
        if ($null -eq $campaignFailure) { $campaignFailure = $_ }
        else { $campaignFailure = [System.Management.Automation.RuntimeException]::new($campaignFailure.Exception.Message + " Cleanup: " + $_.Exception.Message) }
    }
    [AuditNativeMethods]::SetThreadDpiAwarenessContext($previousDpiAwarenessContext) | Out-Null
}

$routeRows = @($campaign.samples | ForEach-Object { $_.routes })
$failedRows = @($routeRows | Where-Object { $_.status -eq "FAIL" })
$failedSamples = @($campaign.samples | Where-Object { $_.status -eq "FAIL" })
$campaign.summary = [ordered]@{
    executedRoutes = $routeRows.Count
    passedRoutes = @($routeRows | Where-Object { $_.status -eq "PASS" }).Count
    failedRoutes = $failedRows.Count
    notTestedRoutes = $campaign.notTested.Count
    preferenceRestored = $campaign.preferenceHashAfter -eq $ExpectedPreferenceHash
    cleanupPassed = $campaign.processResidue.Count -eq 0 -and $campaign.ownedStateResidue.Count -eq 0
}
$reportPath = Join-Path $EvidenceRoot "r6-wpf2-wpf-audit.json"
$campaign | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $reportPath -Encoding utf8
if ($null -ne $campaignFailure) { throw "WPF parity audit failed: $($campaignFailure.Exception.Message) Evidence: $reportPath" }
if ($failedSamples.Count -gt 0 -or $failedRows.Count -gt 0) { throw "WPF parity audit reported failed samples. Evidence: $reportPath" }
[ordered]@{ reportPath = $reportPath; summary = $campaign.summary } | ConvertTo-Json -Depth 4
