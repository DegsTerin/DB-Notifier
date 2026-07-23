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
$EvidenceRoot = Join-Path ([System.IO.Path]::GetTempPath()) ("DBNotifier-R6-UI1-Audit-" + [Guid]::NewGuid().ToString("N"))

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

# Finds one descendant by its exact localised accessible name.
function Find-AuditNamedElement {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][string]$Name
    )
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    return $Owner.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

# Finds the first positive on-screen descendant when an exact name is repeated by collapsed route surfaces.
function Find-AuditVisibleNamedElement {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][string]$Name
    )
    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    $matches = $Owner.FindAll([System.Windows.Automation.TreeScope]::Descendants, $condition)
    foreach ($match in $matches) {
        $rectangle = Get-AuditRectangle -Element $match
        if (-not $match.Current.IsOffscreen -and $rectangle.width -gt 0 -and $rectangle.height -gt 0) {
            return $match
        }
    }
    return $null
}

# Finds one descendant that exposes both a stable component identifier and its complete localised name.
function Find-AuditNamedElementByAutomationId {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][string]$AutomationId,
        [Parameter(Mandatory = $true)][string]$Name
    )
    $condition = [System.Windows.Automation.AndCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
            $AutomationId),
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::NameProperty,
            $Name))
    return $Owner.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

# Traverses a bounded raw-view subtree so diagnostic-only peers remain measurable without entering the screen-reader views.
function Find-AuditRawNamedElementByAutomationId {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][string]$AutomationId,
        [Parameter(Mandatory = $true)][string]$Name,
        [ValidateRange(1, 512)][int]$MaximumNodes = 128
    )
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $queue = [System.Collections.Generic.Queue[System.Windows.Automation.AutomationElement]]::new()
    $firstChild = $walker.GetFirstChild($Owner)
    if ($null -ne $firstChild) { $queue.Enqueue($firstChild) }
    $visited = 0
    while ($queue.Count -gt 0 -and $visited -lt $MaximumNodes) {
        $current = $queue.Dequeue()
        $visited += 1
        if ([string]::Equals($current.Current.AutomationId, $AutomationId, [StringComparison]::Ordinal) -and
            [string]::Equals($current.Current.Name, $Name, [StringComparison]::Ordinal)) {
            return $current
        }
        $child = $walker.GetFirstChild($current)
        if ($null -ne $child) { $queue.Enqueue($child) }
        $sibling = $walker.GetNextSibling($current)
        if ($null -ne $sibling) { $queue.Enqueue($sibling) }
    }
    return $null
}

# Traverses a bounded raw-view subtree for a stable identifier whose peer is intentionally absent from ControlView.
function Find-AuditRawElementByAutomationId {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][string]$AutomationId,
        [ValidateRange(1, 1024)][int]$MaximumNodes = 512
    )
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $queue = [System.Collections.Generic.Queue[System.Windows.Automation.AutomationElement]]::new()
    $firstChild = $walker.GetFirstChild($Owner)
    if ($null -ne $firstChild) { $queue.Enqueue($firstChild) }
    $visited = 0
    while ($queue.Count -gt 0 -and $visited -lt $MaximumNodes) {
        $current = $queue.Dequeue()
        $visited += 1
        if ([string]::Equals($current.Current.AutomationId, $AutomationId, [StringComparison]::Ordinal)) {
            return $current
        }
        $child = $walker.GetFirstChild($current)
        if ($null -ne $child) { $queue.Enqueue($child) }
        $sibling = $walker.GetNextSibling($current)
        if ($null -ne $sibling) { $queue.Enqueue($sibling) }
    }
    return $null
}

# Returns bounded raw-view descendants so generated DataGrid cells can prove their actual content gutters.
function Get-AuditRawDescendants {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [ValidateRange(1, 1024)][int]$MaximumNodes = 128
    )
    $walker = [System.Windows.Automation.TreeWalker]::RawViewWalker
    $queue = [System.Collections.Generic.Queue[System.Windows.Automation.AutomationElement]]::new()
    $result = [System.Collections.Generic.List[System.Windows.Automation.AutomationElement]]::new()
    $firstChild = $walker.GetFirstChild($Owner)
    if ($null -ne $firstChild) { $queue.Enqueue($firstChild) }
    while ($queue.Count -gt 0 -and $result.Count -lt $MaximumNodes) {
        $current = $queue.Dequeue()
        $result.Add($current)
        $child = $walker.GetFirstChild($current)
        if ($null -ne $child) { $queue.Enqueue($child) }
        $sibling = $walker.GetNextSibling($current)
        if ($null -ne $sibling) { $queue.Enqueue($sibling) }
    }
    return @($result)
}

# Records whether one responsive DataGrid exposes its headerless, non-horizontal compact presentation.
function Get-AuditCompactGridEvidence {
    param([Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element)
    $scrollPatternObject = $null
    $scrollPatternAvailable = $Element.TryGetCurrentPattern(
        [System.Windows.Automation.ScrollPattern]::Pattern,
        [ref]$scrollPatternObject)
    $horizontallyScrollable = $null
    if ($scrollPatternAvailable) {
        $horizontallyScrollable = ([System.Windows.Automation.ScrollPattern]$scrollPatternObject).Current.HorizontallyScrollable
    }
    $headerCondition = [System.Windows.Automation.OrCondition]::new(
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::Header),
        [System.Windows.Automation.PropertyCondition]::new(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::HeaderItem))
    $headerCount = $Element.FindAll([System.Windows.Automation.TreeScope]::Descendants, $headerCondition).Count
    return [ordered]@{
        scrollPatternAvailable = $scrollPatternAvailable
        horizontallyScrollable = $horizontallyScrollable
        headerCount = $headerCount
        compact = $scrollPatternAvailable -and -not $horizontallyScrollable -and $headerCount -eq 0
    }
}

# Measures the first desktop DataGrid record by its generated cells and their named raw content.
function Get-AuditDesktopGridGeometryEvidence {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Element,
        [Parameter(Mandatory = $true)][int]$ExpectedColumns,
        [Parameter(Mandatory = $true)][double]$Scale
    )
    $gridPatternObject = $null
    $gridPatternAvailable = $Element.TryGetCurrentPattern(
        [System.Windows.Automation.GridPattern]::Pattern,
        [ref]$gridPatternObject)
    $evidence = [ordered]@{
        gridPatternAvailable = $gridPatternAvailable
        rowCount = 0
        columnCount = 0
        expectedColumns = $ExpectedColumns
        minimumRequiredGutter = [Math]::Round(4 * $Scale, 3)
        firstContentGutter = $null
        lastContentGutter = $null
        minimumIntercolumnGap = $null
        cells = @()
        passed = $false
    }
    if (-not $gridPatternAvailable) { return $evidence }

    $gridPattern = [System.Windows.Automation.GridPattern]$gridPatternObject
    $evidence.rowCount = $gridPattern.Current.RowCount
    $evidence.columnCount = $gridPattern.Current.ColumnCount
    if ($evidence.rowCount -lt 1 -or $evidence.columnCount -lt $ExpectedColumns) { return $evidence }

    $contentRectangles = @()
    $allContentVisible = $true
    for ($column = 0; $column -lt $ExpectedColumns; $column += 1) {
        $cell = $gridPattern.GetItem(0, $column)
        $cellRectangle = Get-AuditRectangle -Element $cell
        $rawContentRectangles = @(
            Get-AuditRawDescendants -Owner $cell | ForEach-Object {
                $candidateRectangle = Get-AuditRectangle -Element $_
                if (-not $_.Current.IsOffscreen -and
                    -not [string]::IsNullOrWhiteSpace($_.Current.Name) -and
                    $candidateRectangle.width -gt 0 -and $candidateRectangle.height -gt 0 -and
                    (Test-AuditContainment -Child $candidateRectangle -Owner $cellRectangle -Tolerance 1)) {
                    $candidateRectangle
                }
            })
        $contentRectangle = if ($rawContentRectangles.Count -gt 0) {
            Get-AuditRectangleUnion -Rectangles $rawContentRectangles
        }
        else {
            $null
        }
        $contentVisible = $null -ne $contentRectangle -and
            -not $cellRectangle.offscreen -and
            (Test-AuditContainment -Child $contentRectangle -Owner $cellRectangle -Tolerance 1)
        if (-not $contentVisible) { $allContentVisible = $false }
        $contentRectangles += $contentRectangle
        $evidence.cells += [ordered]@{
            column = $column
            cellBounds = $cellRectangle
            contentBounds = $contentRectangle
            contentVisible = $contentVisible
        }
    }

    if (-not $allContentVisible) { return $evidence }
    $firstCell = $evidence.cells[0].cellBounds
    $firstContent = $evidence.cells[0].contentBounds
    $lastCell = $evidence.cells[$ExpectedColumns - 1].cellBounds
    $lastContent = $evidence.cells[$ExpectedColumns - 1].contentBounds
    $evidence.firstContentGutter = [Math]::Round([double]$firstContent.x - [double]$firstCell.x, 3)
    $evidence.lastContentGutter = [Math]::Round(
        ([double]$lastCell.x + [double]$lastCell.width) - ([double]$lastContent.x + [double]$lastContent.width),
        3)
    $minimumGap = [double]::PositiveInfinity
    for ($column = 1; $column -lt $ExpectedColumns; $column += 1) {
        $previous = $contentRectangles[$column - 1]
        $current = $contentRectangles[$column]
        $gap = [double]$current.x - ([double]$previous.x + [double]$previous.width)
        $minimumGap = [Math]::Min($minimumGap, $gap)
    }
    $evidence.minimumIntercolumnGap = [Math]::Round($minimumGap, 3)
    $minimumRequiredGutter = [double]$evidence.minimumRequiredGutter
    $evidence.passed = $evidence.firstContentGutter -ge $minimumRequiredGutter -and
        $evidence.lastContentGutter -ge $minimumRequiredGutter -and
        $evidence.minimumIntercolumnGap -ge ($minimumRequiredGutter * 2)
    return $evidence
}

# Determines whether a focused element is the requested owner or one of its control-view descendants.
function Test-AuditOwnsElement {
    param(
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Owner,
        [Parameter(Mandatory = $true)][System.Windows.Automation.AutomationElement]$Candidate
    )
    $current = $Candidate
    while ($null -ne $current) {
        if ([System.Windows.Automation.Automation]::Compare($Owner, $current)) { return $true }
        $current = [System.Windows.Automation.TreeWalker]::ControlViewWalker.GetParent($current)
    }
    return $false
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

# Unions positive finite rectangles so a textual legend can provide runtime evidence for its reserved chart area.
function Get-AuditRectangleUnion {
    param([Parameter(Mandatory = $true)][object[]]$Rectangles)
    $usable = @($Rectangles | Where-Object {
        $null -ne $_ -and $_.width -gt 0 -and $_.height -gt 0 -and
        -not [double]::IsNaN([double]$_.x) -and -not [double]::IsNaN([double]$_.y)
    })
    if ($usable.Count -eq 0) { return $null }
    $left = [double]::PositiveInfinity
    $top = [double]::PositiveInfinity
    $right = [double]::NegativeInfinity
    $bottom = [double]::NegativeInfinity
    foreach ($rectangle in $usable) {
        $left = [Math]::Min($left, [double]$rectangle.x)
        $top = [Math]::Min($top, [double]$rectangle.y)
        $right = [Math]::Max($right, [double]$rectangle.x + [double]$rectangle.width)
        $bottom = [Math]::Max($bottom, [double]$rectangle.y + [double]$rectangle.height)
    }
    return [ordered]@{ x = $left; y = $top; width = $right - $left; height = $bottom - $top; offscreen = $false }
}

# Verifies that a set of controls forms one non-overlapping visual row with comparable widths.
function Test-AuditVisualRow {
    param(
        [Parameter(Mandatory = $true)][object[]]$Rectangles,
        [Parameter(Mandatory = $true)][double]$Tolerance
    )
    if ($Rectangles.Count -lt 2 -or @($Rectangles | Where-Object { $null -eq $_ -or $_.width -le 0 -or $_.height -le 0 }).Count -gt 0) {
        return $false
    }
    $referenceY = [double]$Rectangles[0].y
    for ($index = 0; $index -lt $Rectangles.Count; $index += 1) {
        if ([Math]::Abs([double]$Rectangles[$index].y - $referenceY) -gt $Tolerance) { return $false }
        if ($index -gt 0 -and [double]$Rectangles[$index - 1].x + [double]$Rectangles[$index - 1].width -gt [double]$Rectangles[$index].x + $Tolerance) {
            return $false
        }
    }
    $minimumWidth = [double]::PositiveInfinity
    $maximumWidth = 0d
    foreach ($rectangle in $Rectangles) {
        $minimumWidth = [Math]::Min($minimumWidth, [double]$rectangle.width)
        $maximumWidth = [Math]::Max($maximumWidth, [double]$rectangle.width)
    }
    return $minimumWidth -gt 0 -and ($maximumWidth / $minimumWidth) -le 1.2
}

# Verifies that a later compact block starts below an earlier block and returns to the same left edge.
function Test-AuditVerticalStack {
    param(
        [Parameter(Mandatory = $true)]$First,
        [Parameter(Mandatory = $true)]$Second,
        [Parameter(Mandatory = $true)][double]$Tolerance
    )
    if ($null -eq $First -or $null -eq $Second -or $First.width -le 0 -or $Second.width -le 0) { return $false }
    return [Math]::Abs([double]$First.x - [double]$Second.x) -le $Tolerance -and
        [double]$Second.y -ge ([double]$First.y + [double]$First.height - $Tolerance) -and
        [Math]::Abs([double]$First.width - [double]$Second.width) -le [Math]::Max($Tolerance, [double]$First.width * 0.08)
}

# Verifies that a compact row or section follows another without requiring equal horizontal dimensions.
function Test-AuditVerticalOrder {
    param(
        [Parameter(Mandatory = $true)]$First,
        [Parameter(Mandatory = $true)]$Second,
        [Parameter(Mandatory = $true)][double]$Tolerance,
        [switch]$RequireAlignedLeft
    )
    if ($null -eq $First -or $null -eq $Second -or $First.width -le 0 -or $First.height -le 0 -or $Second.width -le 0 -or $Second.height -le 0) {
        return $false
    }
    if ($RequireAlignedLeft -and [Math]::Abs([double]$First.x - [double]$Second.x) -gt $Tolerance) { return $false }
    return [double]$Second.y -ge ([double]$First.y + [double]$First.height - $Tolerance)
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
    [ordered]@{
        id = "Overview"; titleKey = "View.Overview.Title"
        components = @("OverviewTotalCard", "OverviewHealthyCard", "OverviewWarningCard", "OverviewCriticalCard", "OverviewInventoryList", "OverviewAlertList", "OverviewPerformanceChart", "OverviewProviderDistribution")
        fieldGroups = @()
        compactRecords = @([ordered]@{
            owner = "OverviewInventoryList"
            values = @(
                [ordered]@{ label = "headline"; key = "Sample.Instance.Finance" },
                [ordered]@{ label = "provider"; value = "postgresql" },
                [ordered]@{ label = "latency"; value = "24 ms" })
        })
        focusTargets = @(); distribution = "OverviewProviderDistribution"
        pillGroup = [ordered]@{ owner = "OverviewInventoryList"; key = "Status.Disabled" }
    },
    [ordered]@{
        id = "Inventory"; titleKey = "View.Inventory.Title"
        components = @("InventoryTotalCard", "InventoryHealthyCard", "InventoryDegradedCard", "InventoryAttentionCard", "InventoryStaleCard", "InventoryDisabledCard", "InventoryGrid")
        fieldGroups = @([ordered]@{
            owner = "InventoryGrid"
            keys = @("Common.Instance", "Common.Provider", "Common.Support", "Common.Environment", "Common.Status", "Common.ObservedAtUtc", "Common.Latency")
            compactKeys = @("Common.Instance", "Common.Support", "Common.Environment", "Common.ObservedAtUtc", "Common.Latency")
        })
        compactRecords = @([ordered]@{
            owner = "InventoryGrid"
            values = @(
                [ordered]@{ label = "headline"; key = "Sample.Instance.Finance" },
                [ordered]@{ label = "summary"; key = "Sample.Support.Implemented" },
                [ordered]@{ label = "provider"; value = "postgresql" })
        })
        focusTargets = @("InventoryGrid"); distribution = $null
        pillGroup = [ordered]@{ owner = "InventoryGrid"; key = "Status.Disabled" }
    },
    [ordered]@{
        id = "Alerts"; titleKey = "View.Alerts.Title"
        components = @("AlertCriticalCard", "AlertActiveCard", "AlertTotalCard", "AlertsGrid")
        fieldGroups = @([ordered]@{
            owner = "AlertsGrid"
            keys = @("Common.Severity", "Common.State", "Common.Rule", "Common.Instance", "Common.Provider", "Common.Summary", "Common.Updated")
            compactKeys = @("Common.State", "Common.Instance", "Common.Provider", "Common.Updated")
        })
        compactRecords = @([ordered]@{
            owner = "AlertsGrid"
            values = @(
                [ordered]@{ label = "headline"; key = "Sample.Alert.TimeoutRule" },
                [ordered]@{ label = "summary"; key = "Sample.Alert.TimeoutSummary" },
                [ordered]@{ label = "provider"; value = "sql-server" })
        })
        focusTargets = @("AlertsGrid"); distribution = $null
        pillGroup = [ordered]@{ owner = "AlertsGrid"; key = "Severity.Critical" }
    },
    [ordered]@{
        id = "Performance"; titleKey = "View.Performance.Title"; components = @("PerformanceRouteChart")
        fieldGroups = @(); compactRecords = @(); focusTargets = @(); distribution = $null; pillGroup = $null
    },
    [ordered]@{
        id = "History"; titleKey = "View.History.Title"; components = @("HistoryGrid", "HistorySummaryText")
        fieldGroups = @([ordered]@{
            owner = "HistoryGrid"
            keys = @("Common.TimeUtc", "Common.Severity", "Common.Event", "Common.Instance", "Common.Provider", "Common.Summary")
            compactKeys = @("Common.TimeUtc", "Common.Instance", "Common.Provider")
        })
        compactRecords = @([ordered]@{
            owner = "HistoryGrid"
            values = @(
                [ordered]@{ label = "headline"; value = "Recovered" },
                [ordered]@{ label = "summary"; key = "Sample.Event.Recovered" },
                [ordered]@{ label = "provider"; value = "postgresql" })
        })
        focusTargets = @("HistoryGrid"); distribution = $null
        pillGroup = [ordered]@{ owner = "HistoryGrid"; key = "Severity.Critical" }
    },
    [ordered]@{
        id = "Configuration"; titleKey = "View.Configuration.Title"; components = @("ConfigurationGrid", "CapabilityGrid", "PermissionSelector")
        fieldGroups = @(
            [ordered]@{
                owner = "ConfigurationGrid"; keys = @("Common.Field", "Common.SafeValue", "Common.Description")
                compactKeys = @("Sample.Config.Interval.Label", "Sample.Config.Timeout.Label", "Sample.Config.Retry.Label", "Sample.Config.Credential.Label")
            },
            [ordered]@{
                owner = "CapabilityGrid"; keys = @("Common.Action", "Common.Capability", "Common.State", "Common.Reason")
                compactKeys = @("Common.Capability", "Common.Reason")
            })
        compactRecords = @(
            [ordered]@{
                owner = "ConfigurationGrid"
                values = @(
                    [ordered]@{ label = "headline"; key = "Sample.Config.Interval.Label" },
                    [ordered]@{ label = "value"; key = "Sample.Config.Interval.Value" },
                    [ordered]@{ label = "summary"; key = "Sample.Config.Interval.Description" })
            },
            [ordered]@{
                owner = "CapabilityGrid"
                values = @(
                    [ordered]@{ label = "headline"; key = "Action.Start" },
                    [ordered]@{ label = "state"; key = "Configuration.Unsupported" },
                    [ordered]@{ label = "summary"; value = "provider.control_unsupported" })
            },
            [ordered]@{
                owner = "__window"
                values = @([ordered]@{ label = "provider"; value = "postgresql" })
            })
        focusTargets = @("ConfigurationGrid", "PermissionSelector", "CapabilityGrid"); distribution = $null; pillGroup = $null
    },
    [ordered]@{
        id = "Providers"; titleKey = "View.Providers.Title"; components = @("ProvidersDistribution", "ProviderCatalogueList")
        fieldGroups = @(); compactRecords = @(); focusTargets = @(); distribution = "ProvidersDistribution"; pillGroup = $null
    },
    [ordered]@{
        id = "Settings"; titleKey = "View.Settings.Title"; components = @("SettingsPreferenceTitleText", "SettingsLanguageText", "SettingsThemeText", "SettingsNotificationTitleText")
        fieldGroups = @(); compactRecords = @(); focusTargets = @(); distribution = $null; pillGroup = $null
    }
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
            "--test-subject", "r6-ui1-audit"
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
            if (-not $navigation.Current.IsKeyboardFocusable -or [string]::IsNullOrWhiteSpace($navigation.Current.Name)) {
                $routeFailures.Add("The current navigation destination is not a named keyboard focus target.")
            }

            $outerScroll = [ordered]@{
                horizontallyScrollable = $false
                horizontalViewSize = 100
                horizontalScrollPercent = [System.Windows.Automation.ScrollPattern]::NoScroll
                verticallyScrollable = $false
                verticalViewSize = 100
            }
            $scroll = $null
            if ($null -eq $contentScroller) {
                $routeFailures.Add("The owning content scroller is absent, so page overflow cannot be proved bounded.")
            }
            else {
                $scrollObject = $null
                if ($contentScroller.TryGetCurrentPattern([System.Windows.Automation.ScrollPattern]::Pattern, [ref]$scrollObject)) {
                    $scroll = [System.Windows.Automation.ScrollPattern]$scrollObject
                    $outerScroll = [ordered]@{
                        horizontallyScrollable = $scroll.Current.HorizontallyScrollable
                        horizontalViewSize = $scroll.Current.HorizontalViewSize
                        horizontalScrollPercent = $scroll.Current.HorizontalScrollPercent
                        verticallyScrollable = $scroll.Current.VerticallyScrollable
                        verticalViewSize = $scroll.Current.VerticalViewSize
                    }
                    if ($scroll.Current.HorizontallyScrollable) {
                        $routeFailures.Add("The outer content surface exposed forbidden horizontal scrolling.")
                    }
                }
                elseif ($SampleWidth -eq 820) {
                    $routeFailures.Add("The 820-DIP sample exposed no ScrollPattern evidence for the owning content surface.")
                }
            }
            if ($SampleWidth -eq 820 -and
                ($outerScroll.horizontallyScrollable -or
                 [double]$outerScroll.horizontalViewSize -lt 99.9 -or
                 [double]$outerScroll.horizontalScrollPercent -ne [System.Windows.Automation.ScrollPattern]::NoScroll)) {
                $routeFailures.Add("The 820-DIP page did not retain a complete non-horizontal outer viewport.")
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
                if ([string]::IsNullOrWhiteSpace($component.Current.Name)) {
                    $routeFailures.Add("Required component '$componentId' has no accessible name.")
                }
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

            $fieldEvidence = [ordered]@{}
            $desktopTableGeometry = [ordered]@{}
            foreach ($fieldGroup in $route.fieldGroups) {
                $owner = Find-AuditElement -Window $window -AutomationId $fieldGroup.owner
                if ($null -eq $owner) {
                    $routeFailures.Add("Accessible field owner '$($fieldGroup.owner)' is absent.")
                    continue
                }
                $fields = [ordered]@{}
                $requiredFieldKeys = if ($SampleWidth -eq 820 -and $fieldGroup.Contains("compactKeys")) {
                    @($fieldGroup.compactKeys)
                }
                else {
                    @($fieldGroup.keys)
                }
                foreach ($fieldKey in $requiredFieldKeys) {
                    $fieldName = [string]$messages[$fieldKey]
                    $field = Find-AuditNamedElement -Owner $owner -Name $fieldName
                    $fields[$fieldKey] = $null -ne $field
                    if ($null -eq $field) {
                        $routeFailures.Add("Required accessible field '$fieldKey' is absent from '$($fieldGroup.owner)'.")
                    }
                }
                $fieldEvidence[$fieldGroup.owner] = $fields
                if ($SampleWidth -eq 1180) {
                    if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                        foreach ($verticalPercent in @(0, 20, 40, 60, 80, 100)) {
                            $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, $verticalPercent)
                            Start-Sleep -Milliseconds 40
                            $ownerRectangle = Get-AuditRectangle -Element $owner
                            $ownerTopVisible = -not $owner.Current.IsOffscreen -and
                                $ownerRectangle.y -ge ($contentRectangle.y - (3 * $windowDpi / 96)) -and
                                $ownerRectangle.y -lt ($contentRectangle.y + $contentRectangle.height - (56 * $windowDpi / 96))
                            if ($ownerTopVisible) { break }
                        }
                    }
                    $geometry = Get-AuditDesktopGridGeometryEvidence -Element $owner -ExpectedColumns $fieldGroup.keys.Count -Scale ($windowDpi / 96)
                    $desktopTableGeometry[$fieldGroup.owner] = $geometry
                    if (-not $geometry.passed) {
                        $routeFailures.Add("Desktop table '$($fieldGroup.owner)' has insufficient first/last content gutters, adjacent-content separation or visible cell content at 1180 DIP.")
                    }
                    if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                        $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 0)
                        Start-Sleep -Milliseconds 40
                    }
                }
            }

            $compactRecordEvidence = [ordered]@{}
            if ($SampleWidth -eq 820) {
                foreach ($recordGroup in $route.compactRecords) {
                    $recordOwner = if ($recordGroup.owner -eq "__window") {
                        $window
                    }
                    else {
                        Find-AuditElement -Window $window -AutomationId $recordGroup.owner
                    }
                    if ($null -eq $recordOwner) {
                        $routeFailures.Add("Compact-record owner '$($recordGroup.owner)' is absent.")
                        continue
                    }
                    $recordOwnerRectangle = Get-AuditRectangle -Element $recordOwner
                    $recordFields = [ordered]@{}
                    foreach ($recordValue in $recordGroup.values) {
                        $requiredName = if ($recordValue.Contains("key")) {
                            [string]$messages[$recordValue.key]
                        }
                        else {
                            [string]$recordValue.value
                        }
                        $recordElement = if ($recordGroup.owner -eq "__window") {
                            Find-AuditVisibleNamedElement -Owner $recordOwner -Name $requiredName
                        }
                        else {
                            Find-AuditNamedElement -Owner $recordOwner -Name $requiredName
                        }
                        $recordRectangle = if ($null -ne $recordElement) { Get-AuditRectangle -Element $recordElement } else { $null }
                        $present = $null -ne $recordRectangle -and $recordRectangle.width -gt 0 -and $recordRectangle.height -gt 0 -and
                            (Test-AuditHorizontalContainment -Child $recordRectangle -Owner $recordOwnerRectangle)
                        $recordFields[$recordValue.label] = [ordered]@{ name = $requiredName; present = $present; bounds = $recordRectangle }
                        if (-not $present) {
                            $routeFailures.Add("Compact-record field '$($recordValue.label)' is absent, empty or horizontally clipped in '$($recordGroup.owner)'.")
                        }
                    }
                    $compactRecordEvidence[$recordGroup.owner] = $recordFields
                }
            }

            $pillEvidence = [ordered]@{
                owner = $null
                requiredKey = $null
                automationId = "StatusPill"
                containerBounds = $null
                rawTextBounds = $null
                rawTextAvailable = $false
                aspectRatio = $null
            }
            if ($null -ne $route.pillGroup) {
                $pillOwner = Find-AuditElement -Window $window -AutomationId $route.pillGroup.owner
                $pillEvidence.owner = $route.pillGroup.owner
                $pillEvidence.requiredKey = $route.pillGroup.key
                if ($null -eq $pillOwner) {
                    $routeFailures.Add("The status-pill content owner '$($route.pillGroup.owner)' is absent.")
                }
                else {
                    $pillName = [string]$messages[$route.pillGroup.key]
                    $pillContainer = Find-AuditNamedElementByAutomationId -Owner $pillOwner -AutomationId "StatusPill" -Name $pillName
                    if ($null -eq $pillContainer) {
                        $routeFailures.Add("The complete '$($route.pillGroup.key)' StatusPill peer is absent from '$($route.pillGroup.owner)'.")
                    }
                    else {
                        $containerRectangle = Get-AuditRectangle -Element $pillContainer
                        $pillEvidence.containerBounds = $containerRectangle
                        if ($containerRectangle.height -gt 0) {
                            $pillEvidence.aspectRatio = [Math]::Round($containerRectangle.width / $containerRectangle.height, 3)
                        }
                        $pillText = Find-AuditRawNamedElementByAutomationId -Owner $pillContainer -AutomationId "StatusPillText" -Name $pillName
                        if ($null -ne $pillText) {
                            $pillEvidence.rawTextAvailable = $true
                            $pillEvidence.rawTextBounds = Get-AuditRectangle -Element $pillText
                        }
                        if ($pillContainer.Current.ControlType -ne [System.Windows.Automation.ControlType]::Text -or
                            -not [string]::Equals($pillContainer.Current.Name, $pillName, [StringComparison]::Ordinal) -or
                            $containerRectangle.width -le 0 -or $containerRectangle.height -le 0 -or
                            [double]$pillEvidence.aspectRatio -gt 14 -or
                            -not (Test-AuditHorizontalContainment -Child $containerRectangle -Owner (Get-AuditRectangle -Element $pillOwner))) {
                            $routeFailures.Add("The complete StatusPill peer has an invalid name, type, ratio or horizontal container bounds.")
                        }
                        if (-not $pillEvidence.rawTextAvailable) {
                            $routeFailures.Add("The raw status text is absent, so complete content containment cannot be proved.")
                        }
                        elseif ($pillEvidence.rawTextBounds.width -le 0 -or $pillEvidence.rawTextBounds.height -le 0 -or
                            -not (Test-AuditContainment -Child $pillEvidence.rawTextBounds -Owner $containerRectangle)) {
                            $routeFailures.Add("The raw status text is not completely contained by its StatusPill peer bounds.")
                        }
                    }
                }
            }

            $compactLayout = [ordered]@{ applicable = $SampleWidth -eq 820; checks = [ordered]@{}; grids = [ordered]@{} }
            if ($SampleWidth -eq 820) {
                $compactTolerance = [Math]::Max(4, 4 * $windowDpi / 96)
                if ($route.id -eq "Overview") {
                    $compactLayout.checks.summaryFirstRow = Test-AuditVisualRow -Rectangles @($componentBounds.OverviewTotalCard, $componentBounds.OverviewHealthyCard) -Tolerance $compactTolerance
                    $compactLayout.checks.summarySecondRow = Test-AuditVisualRow -Rectangles @($componentBounds.OverviewWarningCard, $componentBounds.OverviewCriticalCard) -Tolerance $compactTolerance
                    $compactLayout.checks.summaryRowsSeparated = Test-AuditVerticalOrder -First $componentBounds.OverviewTotalCard -Second $componentBounds.OverviewWarningCard -Tolerance $compactTolerance -RequireAlignedLeft
                    $compactLayout.checks.operationalPanelsStacked = Test-AuditVerticalStack -First $componentBounds.OverviewInventoryList -Second $componentBounds.OverviewAlertList -Tolerance $compactTolerance
                    $compactLayout.checks.insightPanelsStacked = Test-AuditVerticalStack -First $componentBounds.OverviewPerformanceChart -Second $componentBounds.OverviewProviderDistribution -Tolerance $compactTolerance
                }
                elseif ($route.id -eq "Inventory") {
                    $compactLayout.checks.summaryFirstRow = Test-AuditVisualRow -Rectangles @($componentBounds.InventoryTotalCard, $componentBounds.InventoryHealthyCard) -Tolerance $compactTolerance
                    $compactLayout.checks.summarySecondRow = Test-AuditVisualRow -Rectangles @($componentBounds.InventoryDegradedCard, $componentBounds.InventoryAttentionCard) -Tolerance $compactTolerance
                    $compactLayout.checks.summaryThirdRow = Test-AuditVisualRow -Rectangles @($componentBounds.InventoryStaleCard, $componentBounds.InventoryDisabledCard) -Tolerance $compactTolerance
                    $compactLayout.checks.summaryRowsOneAndTwoSeparated = Test-AuditVerticalOrder -First $componentBounds.InventoryTotalCard -Second $componentBounds.InventoryDegradedCard -Tolerance $compactTolerance -RequireAlignedLeft
                    $compactLayout.checks.summaryRowsTwoAndThreeSeparated = Test-AuditVerticalOrder -First $componentBounds.InventoryDegradedCard -Second $componentBounds.InventoryStaleCard -Tolerance $compactTolerance -RequireAlignedLeft
                    $inventoryGrid = Find-AuditElement -Window $window -AutomationId "InventoryGrid"
                    if ($null -ne $inventoryGrid) { $compactLayout.grids.InventoryGrid = Get-AuditCompactGridEvidence -Element $inventoryGrid }
                    $compactLayout.checks.inventoryGridCompact = $null -ne $inventoryGrid -and $compactLayout.grids.InventoryGrid.compact
                }
                elseif ($route.id -eq "Alerts") {
                    $compactLayout.checks.summaryCriticalPrecedesActive = Test-AuditVerticalStack -First $componentBounds.AlertCriticalCard -Second $componentBounds.AlertActiveCard -Tolerance $compactTolerance
                    $compactLayout.checks.summaryActivePrecedesTotal = Test-AuditVerticalStack -First $componentBounds.AlertActiveCard -Second $componentBounds.AlertTotalCard -Tolerance $compactTolerance
                    $alertsGrid = Find-AuditElement -Window $window -AutomationId "AlertsGrid"
                    if ($null -ne $alertsGrid) { $compactLayout.grids.AlertsGrid = Get-AuditCompactGridEvidence -Element $alertsGrid }
                    $compactLayout.checks.alertsGridCompact = $null -ne $alertsGrid -and $compactLayout.grids.AlertsGrid.compact
                }
                elseif ($route.id -eq "History") {
                    $historyGrid = Find-AuditElement -Window $window -AutomationId "HistoryGrid"
                    if ($null -ne $historyGrid) { $compactLayout.grids.HistoryGrid = Get-AuditCompactGridEvidence -Element $historyGrid }
                    $compactLayout.checks.historyGridCompact = $null -ne $historyGrid -and $compactLayout.grids.HistoryGrid.compact
                    $compactLayout.checks.summaryPrecedesRecords = Test-AuditVerticalOrder -First $componentBounds.HistorySummaryText -Second $componentBounds.HistoryGrid -Tolerance $compactTolerance
                }
                elseif ($route.id -eq "Configuration") {
                    $configurationGrid = Find-AuditElement -Window $window -AutomationId "ConfigurationGrid"
                    $capabilityGrid = Find-AuditElement -Window $window -AutomationId "CapabilityGrid"
                    $providerRecordRectangle = if ($compactRecordEvidence.Contains("__window") -and $compactRecordEvidence["__window"].Contains("provider")) {
                        $compactRecordEvidence["__window"]["provider"].bounds
                    }
                    else {
                        $null
                    }
                    if ($null -ne $configurationGrid) { $compactLayout.grids.ConfigurationGrid = Get-AuditCompactGridEvidence -Element $configurationGrid }
                    if ($null -ne $capabilityGrid) { $compactLayout.grids.CapabilityGrid = Get-AuditCompactGridEvidence -Element $capabilityGrid }
                    $compactLayout.checks.configurationGridCompact = $null -ne $configurationGrid -and $compactLayout.grids.ConfigurationGrid.compact
                    $compactLayout.checks.capabilityGridCompact = $null -ne $capabilityGrid -and $compactLayout.grids.CapabilityGrid.compact
                    $compactLayout.checks.providerPrecedesConfiguration = Test-AuditVerticalOrder -First $providerRecordRectangle -Second $componentBounds.ConfigurationGrid -Tolerance $compactTolerance
                    $compactLayout.checks.configurationPrecedesPermission = Test-AuditVerticalOrder -First $componentBounds.ConfigurationGrid -Second $componentBounds.PermissionSelector -Tolerance $compactTolerance
                    $compactLayout.checks.permissionPrecedesCapabilities = Test-AuditVerticalOrder -First $componentBounds.PermissionSelector -Second $componentBounds.CapabilityGrid -Tolerance $compactTolerance
                }
                elseif ($route.id -eq "Settings") {
                    $compactLayout.checks.preferencePanelsStacked = Test-AuditVerticalOrder -First $componentBounds.SettingsPreferenceTitleText -Second $componentBounds.SettingsNotificationTitleText -Tolerance ([Math]::Max($compactTolerance, 18 * $windowDpi / 96)) -RequireAlignedLeft
                }
                foreach ($check in $compactLayout.checks.GetEnumerator()) {
                    if (-not [bool]$check.Value) { $routeFailures.Add("Compact-layout geometry failed '$($check.Key)' at 820 DIP.") }
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
            $verticalReachability = [ordered]@{}
            if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 100)
                Start-Sleep -Milliseconds 100
                $bottomCapture = Save-AuditWindow -Handle $process.MainWindowHandle -Window $window -Path (Join-Path $routeDirectory "bottom-$($windowDpi)dpi.png")
                foreach ($componentId in $deferredComponentIds) {
                    $topReachable = $false
                    $bottomReachable = $false
                    $verticalTolerance = [Math]::Max(4, 4 * $windowDpi / 96)
                    foreach ($verticalPercent in @(0, 20, 40, 60, 80, 100)) {
                        $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, $verticalPercent)
                        Start-Sleep -Milliseconds 40
                        $component = Find-AuditElement -Window $window -AutomationId $componentId
                        if ($null -eq $component -or $component.Current.IsOffscreen) { continue }
                        $componentRectangle = Get-AuditRectangle -Element $component
                        if (-not (Test-AuditHorizontalContainment -Child $componentRectangle -Owner $contentRectangle)) { continue }
                        $viewportTop = [double]$contentRectangle.y
                        $viewportBottom = [double]$contentRectangle.y + [double]$contentRectangle.height
                        $componentTop = [double]$componentRectangle.y
                        $componentBottom = [double]$componentRectangle.y + [double]$componentRectangle.height
                        if ($componentTop -ge ($viewportTop - $verticalTolerance) -and $componentTop -le ($viewportBottom + $verticalTolerance)) {
                            $topReachable = $true
                        }
                        if ($componentBottom -ge ($viewportTop - $verticalTolerance) -and $componentBottom -le ($viewportBottom + $verticalTolerance)) {
                            $bottomReachable = $true
                        }
                        if ($topReachable -and $bottomReachable) { break }
                    }
                    $verticalReachability[$componentId] = [ordered]@{ top = $topReachable; bottom = $bottomReachable }
                    if (-not $topReachable -or -not $bottomReachable) {
                        $routeFailures.Add("Component '$componentId' did not expose both vertical boundaries through bounded page scrolling.")
                    }
                }
                $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 0)
                Start-Sleep -Milliseconds 75
            }

            $distributionEvidence = $null
            if (-not [string]::IsNullOrWhiteSpace([string]$route.distribution)) {
                $distribution = Find-AuditElement -Window $window -AutomationId $route.distribution
                if ($null -eq $distribution) {
                    $routeFailures.Add("Provider distribution '$($route.distribution)' is absent from the automation tree.")
                }
                else {
                    if ($distribution.Current.IsOffscreen -and $null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                        foreach ($verticalPercent in @(0, 20, 40, 60, 80, 100)) {
                            $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, $verticalPercent)
                            Start-Sleep -Milliseconds 50
                            $distribution = Find-AuditElement -Window $window -AutomationId $route.distribution
                            if ($null -ne $distribution -and -not $distribution.Current.IsOffscreen) { break }
                        }
                    }
                    $distributionRectangle = Get-AuditRectangle -Element $distribution
                    $distributionCardAutomationId = if ($route.id -eq "Overview") {
                        "OverviewProviderCard"
                    }
                    else {
                        "ProvidersRouteDistributionCard"
                    }
                    $distributionCard = Find-AuditRawElementByAutomationId -Owner $window -AutomationId $distributionCardAutomationId
                    if ($null -eq $distributionCard) {
                        throw "$distributionCardAutomationId is absent from the audited window RawView subtree."
                    }
                    $distributionCardRectangle = Get-AuditRectangle -Element $distributionCard
                    $ringViewport = Find-AuditRawElementByAutomationId -Owner $distribution -AutomationId "ProviderDistributionRingViewport"
                    if ($null -eq $ringViewport) {
                        throw "ProviderDistributionRingViewport is absent from the provider distribution RawView subtree."
                    }
                    $ringRectangle = Get-AuditRectangle -Element $ringViewport
                    $providerBounds = @()
                    $missingProviders = [System.Collections.Generic.List[string]]::new()
                    foreach ($providerName in @("postgresql", "mysql", "sql-server", "mongodb")) {
                        $providerElement = Find-AuditNamedElement -Owner $distribution -Name $providerName
                        if ($null -eq $providerElement) { $missingProviders.Add($providerName) }
                        else { $providerBounds += Get-AuditRectangle -Element $providerElement }
                    }
                    $legendRectangle = Get-AuditRectangleUnion -Rectangles $providerBounds
                    $scale = $windowDpi / 96
                    $leftReservation = if ($null -eq $legendRectangle) { 0 } else { $legendRectangle.x - $distributionRectangle.x }
                    $rightMargin = if ($null -eq $legendRectangle) { 0 } else { ($distributionRectangle.x + $distributionRectangle.width) - ($legendRectangle.x + $legendRectangle.width) }
                    $ringMargins = [ordered]@{
                        left = [Math]::Round([double]$ringRectangle.x - [double]$distributionCardRectangle.x, 3)
                        top = [Math]::Round([double]$ringRectangle.y - [double]$distributionCardRectangle.y, 3)
                        right = [Math]::Round(
                            ([double]$distributionCardRectangle.x + [double]$distributionCardRectangle.width) -
                            ([double]$ringRectangle.x + [double]$ringRectangle.width),
                            3)
                        bottom = [Math]::Round(
                            ([double]$distributionCardRectangle.y + [double]$distributionCardRectangle.height) -
                            ([double]$ringRectangle.y + [double]$ringRectangle.height),
                            3)
                    }
                    $distributionEvidence = [ordered]@{
                        bounds = $distributionRectangle
                        cardAutomationId = $distributionCardAutomationId
                        cardBounds = $distributionCardRectangle
                        ringViewportBounds = $ringRectangle
                        ringViewportMargins = $ringMargins
                        legendBounds = $legendRectangle
                        missingProviders = @($missingProviders)
                        leftRingReservation = $leftReservation
                        rightLegendMargin = $rightMargin
                    }
                    if ($distributionRectangle.width -le 0 -or $distributionRectangle.height -lt (112 * $scale) -or $distributionRectangle.offscreen) {
                        $routeFailures.Add("The provider distribution has no visible bounded area large enough for the ring.")
                    }
                    if ($ringRectangle.offscreen -or
                        -not (Test-AuditContainment -Child $ringRectangle -Owner $distributionCardRectangle -Tolerance 1) -or
                        $ringMargins.left -le 0 -or $ringMargins.top -le 0 -or
                        $ringMargins.right -le 0 -or $ringMargins.bottom -le 0) {
                        $routeFailures.Add("The provider ring viewport is not visibly contained by the card with positive margins on every edge.")
                    }
                    if ($missingProviders.Count -gt 0 -or $null -eq $legendRectangle -or
                        -not (Test-AuditContainment -Child $legendRectangle -Owner $distributionRectangle) -or
                        $leftReservation -lt (88 * $scale) -or $rightMargin -lt (2 * $scale)) {
                        $routeFailures.Add("The provider ring reservation or complete textual legend is clipped or missing its bounded margins.")
                    }
                    if ($null -ne $scroll -and $scroll.Current.VerticallyScrollable) {
                        $scroll.SetScrollPercent([System.Windows.Automation.ScrollPattern]::NoScroll, 0)
                        Start-Sleep -Milliseconds 50
                    }
                }
            }

            $focusEvidence = [ordered]@{}
            foreach ($focusTargetId in $route.focusTargets) {
                $focusTarget = Find-AuditElement -Window $window -AutomationId $focusTargetId
                if ($null -eq $focusTarget -or -not $focusTarget.Current.IsKeyboardFocusable -or [string]::IsNullOrWhiteSpace($focusTarget.Current.Name)) {
                    $focusEvidence[$focusTargetId] = $false
                    $routeFailures.Add("Required focus target '$focusTargetId' is absent, unnamed or not keyboard focusable.")
                    continue
                }
                try {
                    $focusTarget.SetFocus()
                    Start-Sleep -Milliseconds 50
                    $focusedElement = [System.Windows.Automation.AutomationElement]::FocusedElement
                    $focusOwned = $focusedElement.Current.ProcessId -eq $process.Id -and (Test-AuditOwnsElement -Owner $focusTarget -Candidate $focusedElement)
                    $focusedRectangle = Get-AuditRectangle -Element $focusedElement
                    $focusVisible = -not $focusedElement.Current.IsOffscreen
                    $focusTolerance = [Math]::Max(3, 3 * $windowDpi / 96)
                    $oversizedGridMinimumHeight = switch ($focusTargetId) {
                        "InventoryGrid" { 48 }
                        "AlertsGrid" { 52 }
                        "HistoryGrid" { 46 }
                        "ConfigurationGrid" { 46 }
                        "CapabilityGrid" { 46 }
                        default { 0 }
                    }
                    $oversizedGrid = $oversizedGridMinimumHeight -gt 0 -and
                        $null -ne $scroll -and $scroll.Current.VerticallyScrollable -and
                        $focusedRectangle.height -gt ($contentRectangle.height + $focusTolerance)
                    $visibleIntersectionHeight = [Math]::Max(
                        0,
                        [Math]::Min(
                            [double]$focusedRectangle.y + [double]$focusedRectangle.height,
                            [double]$contentRectangle.y + [double]$contentRectangle.height) -
                        [Math]::Max([double]$focusedRectangle.y, [double]$contentRectangle.y))
                    $minimumVisibleHeight = $oversizedGridMinimumHeight * $windowDpi / 96
                    $focusContained = if ($oversizedGrid) {
                        (Test-AuditHorizontalContainment -Child $focusedRectangle -Owner $contentRectangle -Tolerance $focusTolerance) -and
                            $visibleIntersectionHeight -ge $minimumVisibleHeight
                    }
                    else {
                        Test-AuditContainment -Child $focusedRectangle -Owner $contentRectangle -Tolerance $focusTolerance
                    }
                    $focusEvidence[$focusTargetId] = [ordered]@{
                        owned = $focusOwned
                        visible = $focusVisible
                        containedByViewport = $focusContained
                        containmentMode = if ($oversizedGrid) { "bounded-visible-row" } else { "complete" }
                        visibleIntersectionHeight = [Math]::Round($visibleIntersectionHeight, 3)
                        minimumVisibleHeight = [Math]::Round($minimumVisibleHeight, 3)
                        bounds = $focusedRectangle
                    }
                    if (-not $focusOwned -or -not $focusVisible -or -not $focusContained) {
                        $routeFailures.Add("Required focus target '$focusTargetId' did not retain visible keyboard focus inside its own control tree and the content viewport.")
                    }
                }
                catch {
                    $focusEvidence[$focusTargetId] = [ordered]@{ owned = $false; visible = $false; containedByViewport = $false; bounds = $null }
                    $routeFailures.Add("Required focus target '$focusTargetId' rejected bounded keyboard focus.")
                }
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
                verticalReachability = $verticalReachability
                fields = $fieldEvidence
                desktopTableGeometry = $desktopTableGeometry
                compactRecords = $compactRecordEvidence
                pillContent = $pillEvidence
                compactLayout = $compactLayout
                providerDistribution = $distributionEvidence
                focusTargets = $focusEvidence
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
        $requiredGlobalFocus = [ordered]@{}
        foreach ($requiredFocusId in @("LanguagePreferenceButton", "ThemePreferenceButton", "NotificationButton", "SettingsButton", "OverviewNavigationButton", "ScenarioSelector")) {
            $requiredFocusTarget = Find-AuditElement -Window $window -AutomationId $requiredFocusId
            $validFocusTarget = $null -ne $requiredFocusTarget -and $requiredFocusTarget.Current.IsKeyboardFocusable -and
                -not [string]::IsNullOrWhiteSpace($requiredFocusTarget.Current.Name)
            $requiredGlobalFocus[$requiredFocusId] = $validFocusTarget
            if (-not $validFocusTarget) { $sampleFailures.Add("Required global focus target '$requiredFocusId' is absent, unnamed or not keyboard focusable.") }
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
            requiredGlobalFocus = $requiredGlobalFocus
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
    schemaVersion = "dbnotifier.r6-ui1-audit.v1"
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
$reportPath = Join-Path $EvidenceRoot "r6-ui1-wpf-audit.json"
$campaign | ConvertTo-Json -Depth 14 | Set-Content -LiteralPath $reportPath -Encoding utf8
if ($null -ne $campaignFailure) { throw "WPF parity audit failed: $($campaignFailure.Exception.Message) Evidence: $reportPath" }
if ($failedSamples.Count -gt 0 -or $failedRows.Count -gt 0) { throw "WPF parity audit reported failed samples. Evidence: $reportPath" }
[ordered]@{ reportPath = $reportPath; summary = $campaign.summary } | ConvertTo-Json -Depth 4
