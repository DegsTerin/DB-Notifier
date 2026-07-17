// Module purpose: Guards WPF markup contracts that compile successfully but can fail when templates are materialised at runtime.
using System.Text.RegularExpressions;

namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Verifies desktop markup boundaries that cannot be proved by the WPF compiler alone, including typed spacing values
/// and parity of the shared eight-destination information architecture.
/// </summary>
public sealed class WpfPresentationContractTests
{
    private static readonly Regex NumericSpacingResourceOnThicknessProperty = new(
        "(?:Padding|Margin|BorderThickness)=\\\"\\{(?:Dynamic|Static)Resource Space[0-9]+\\}\\\"|CornerRadius=\\\"\\{(?:Dynamic|Static)Resource Radius[A-Za-z]+\\}\\\"",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NumericSpacingResourceOnGridLengthProperty = new(
        "<(?:RowDefinition|ColumnDefinition)\\b[^>]*(?:Height|Width)=\\\"\\{(?:Dynamic|Static)Resource Space[0-9]+\\}\\\"",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Ensures numeric spacing tokens are not assigned to parser-owned WPF value types through runtime resource lookup.</summary>
    [Fact]
    public void WpfParserOwnedPropertiesDoNotUseNumericSpacingResources()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string[] violations = Directory.GetFiles(desktopDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(path =>
            {
                string markup = File.ReadAllText(path);
                return NumericSpacingResourceOnThicknessProperty.IsMatch(markup) || NumericSpacingResourceOnGridLengthProperty.IsMatch(markup);
            })
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>Ensures WPF keeps the same eight primary destinations and ordering as the Dashboard Web shell.</summary>
    [Fact]
    public void WpfNavigationPreservesSharedDestinationOrder()
    {
        string markup = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf", "MainWindow.xaml"));
        string[] destinations = ["Overview", "Inventory", "Alerts", "Performance", "History", "Configuration", "Providers", "Settings"];
        int previousIndex = -1;

        foreach (string destination in destinations)
        {
            int index = markup.IndexOf($"Tag=\"{destination}\"", StringComparison.Ordinal);
            Assert.True(index > previousIndex, $"The WPF navigation destination '{destination}' is missing or out of order.");
            previousIndex = index;
        }
    }

    /// <summary>Ensures read-only WPF tables remain one task-order stop without placing virtualised cells in the Tab sequence.</summary>
    [Fact]
    public void WpfReadOnlyDataGridsPreserveSingleTabStopContract()
    {
        string markup = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf", "MainWindow.xaml"));
        MatchCollection dataGrids = Regex.Matches(markup, "<DataGrid(?=\\s)(?<attributes>[^>]*)>", RegexOptions.CultureInvariant);
        Match dataGridStyle = Regex.Match(markup, "<Style TargetType=\"DataGrid\">(?<body>.*?)</Style>", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        Match dataGridCellStyle = Regex.Match(markup, "<Style TargetType=\"DataGridCell\">(?<body>.*?)</Style>", RegexOptions.CultureInvariant | RegexOptions.Singleline);

        Assert.NotEmpty(dataGrids);
        Assert.All(dataGrids.Cast<Match>(), dataGrid =>
            Assert.Contains("IsReadOnly=\"True\"", dataGrid.Groups["attributes"].Value, StringComparison.Ordinal));
        Assert.True(dataGridStyle.Success, "The shared WPF DataGrid style is missing.");
        Assert.Contains("<Setter Property=\"KeyboardNavigation.IsTabStop\" Value=\"True\" />", dataGridStyle.Groups["body"].Value, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"KeyboardNavigation.TabNavigation\" Value=\"None\" />", dataGridStyle.Groups["body"].Value, StringComparison.Ordinal);
        Assert.True(dataGridCellStyle.Success, "The shared WPF DataGridCell style is missing.");
        Assert.Contains("<Setter Property=\"KeyboardNavigation.IsTabStop\" Value=\"False\" />", dataGridCellStyle.Groups["body"].Value, StringComparison.Ordinal);
    }

    /// <summary>Ensures Windows shell and notification attribution use the canonical visual product name.</summary>
    [Fact]
    public void WpfAssemblyMetadataUsesCanonicalDisplayName()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "DBNotifier.Desktop.Wpf",
            "DBNotifier.Desktop.Wpf.csproj"));

        Assert.Contains("<AssemblyTitle>DB Notifier</AssemblyTitle>", project, StringComparison.Ordinal);
        Assert.Contains("<Product>DB Notifier</Product>", project, StringComparison.Ordinal);
        Assert.DoesNotContain("<AssemblyName>DB Notifier</AssemblyName>", project, StringComparison.Ordinal);
    }

    /// <summary>Ensures invalid future or reversed inventory evidence is not rendered as a factual desktop timestamp.</summary>
    [Fact]
    public void WpfInventorySuppressesTimestampWhenFreshnessIsUnknown()
    {
        string mainWindow = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "DBNotifier.Desktop.Wpf",
            "MainWindow.xaml.cs"));

        Assert.Contains("string observedAt = freshness == EvidenceFreshness.Unknown", mainWindow, StringComparison.Ordinal);
        Assert.Contains("? localisation.Text(\"Status.Unknown\")", mainWindow, StringComparison.Ordinal);
        Assert.Contains("observedAt, latency", mainWindow, StringComparison.Ordinal);
    }

    /// <summary>Ensures native caption and scrolling chrome follow generated theme resources without replacing Windows accessibility ownership.</summary>
    [Fact]
    public void WpfNativeChromeUsesThemeAwarePlatformAdapters()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string applicationMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "App.xaml"));
        string controlStyles = File.ReadAllText(Path.Combine(desktopDirectory, "Resources", "ControlStyles.xaml"));
        string coreTokens = File.ReadAllText(Path.Combine(desktopDirectory, "Generated", "DesignTokens.Core.xaml"));
        string lightTheme = File.ReadAllText(Path.Combine(desktopDirectory, "Generated", "DesignTokens.Light.xaml"));
        string darkTheme = File.ReadAllText(Path.Combine(desktopDirectory, "Generated", "DesignTokens.Dark.xaml"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string nativeThemePolicy = File.ReadAllText(Path.Combine(desktopDirectory, "NativeWindowThemePolicy.cs"));
        string themeService = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopThemeService.cs"));

        Assert.Contains("Resources/ControlStyles.xaml", applicationMarkup, StringComparison.Ordinal);
        Assert.Contains("<Style TargetType=\"{x:Type ScrollBar}\">", controlStyles, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Count(controlStyles, "x:Name=\"PART_Track\"", RegexOptions.CultureInvariant));
        foreach (string command in new[] { "LineUp", "LineDown", "LineLeft", "LineRight", "PageUp", "PageDown", "PageLeft", "PageRight" })
        {
            Assert.Contains($"ScrollBar.{command}Command", controlStyles, StringComparison.Ordinal);
        }
        string[] themeResources = Regex.Matches(controlStyles, "\\{DynamicResource (?<key>[A-Za-z0-9]+Brush)\\}", RegexOptions.CultureInvariant)
            .Cast<Match>()
            .Select(match => match.Groups["key"].Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(themeResources);
        foreach (string resource in themeResources)
        {
            Assert.Contains($"x:Key=\"{resource}\"", lightTheme, StringComparison.Ordinal);
            Assert.Contains($"x:Key=\"{resource}\"", darkTheme, StringComparison.Ordinal);
            Assert.Contains($"\"{resource}\"", themeService, StringComparison.Ordinal);
        }
        Assert.Contains("ColourSelectionForegroundBrush", controlStyles, StringComparison.Ordinal);
        Assert.Contains("ColourActionPrimaryForegroundBrush", controlStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("Property=\"Opacity\"", controlStyles, StringComparison.Ordinal);
        Assert.DoesNotMatch("#[0-9A-Fa-f]{6,8}", controlStyles);
        Assert.Contains("<sys:Double x:Key=\"Space4\">", coreTokens, StringComparison.Ordinal);
        Assert.Contains("<sys:Double x:Key=\"Space6\">", coreTokens, StringComparison.Ordinal);

        Assert.Equal(2, Regex.Count(mainWindow, Regex.Escape("NativeWindowThemePolicy.Apply(this, theme)"), RegexOptions.CultureInvariant));
        Assert.Contains("OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000)", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("UseImmersiveDarkMode = 20", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("CaptionColour = 35", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("TextColour = 36", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("DwmColourDefault = 0xFFFFFFFF", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("ComponentAppBackgroundBrush", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("ColourTextPrimaryBrush", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Count(nativeThemePolicy, Regex.Escape("[DefaultDllImportSearchPaths(DllImportSearchPath.System32)]"), RegexOptions.CultureInvariant));
        Assert.DoesNotContain("BorderColour", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("IsHighContrastActive", nativeThemePolicy, StringComparison.Ordinal);
        Assert.Contains("public bool IsHighContrastActive => lastHighContrast;", themeService, StringComparison.Ordinal);
    }

    /// <summary>Ensures WPF selects DPI-aware native ICO frames and keeps notification sources at the Windows small-icon metric.</summary>
    [Fact]
    public void WpfBrandIconsUseNativeResolutionFrames()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string policy = File.ReadAllText(Path.Combine(desktopDirectory, "BrandStatusIconPolicy.cs"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string flyout = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml.cs"));
        string flyoutMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml"));
        string trayController = File.ReadAllText(Path.Combine(desktopDirectory, "TrayApplicationController.cs"));
        string notificationPublisher = File.ReadAllText(Path.Combine(desktopDirectory, "WindowsAppNotificationPublisher.cs"));
        string project = File.ReadAllText(Path.Combine(desktopDirectory, "DBNotifier.Desktop.Wpf.csproj"));

        Assert.Contains("IconBitmapDecoder", policy, StringComparison.Ordinal);
        Assert.Contains("Math.Ceiling(targetDipSize * Math.Max(dpi.DpiScaleX, dpi.DpiScaleY))", policy, StringComparison.Ordinal);
        Assert.Contains("candidate.PixelWidth >= targetPixelSize", policy, StringComparison.Ordinal);
        Assert.Contains("OrderByDescending(candidate => candidate.PixelWidth)", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("BitmapImage image = new()", policy, StringComparison.Ordinal);
        Assert.Contains("ApplyNativeWindowIcons(this, aggregateState)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DpiScale dpi = VisualTreeHelper.GetDpi(this)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("LoadImageSource(aggregateState, 40, dpi)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DpiScale dpi = VisualTreeHelper.GetDpi(this)", flyout, StringComparison.Ordinal);
        Assert.Contains("LoadImageSource(fleetSummary.State, 32, dpi)", flyout, StringComparison.Ordinal);
        Assert.Contains("Stretch=\"None\" SnapsToDevicePixels=\"True\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Stretch=\"None\" SnapsToDevicePixels=\"True\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Equal(
            3,
            Regex.Count(
                trayController,
                Regex.Escape("Forms.SystemInformation.SmallIconSize.Width"),
                RegexOptions.CultureInvariant));
        Assert.DoesNotContain("Forms.SystemInformation.IconSize.Width", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationPresentationPolicy.ResolveIconState", trayController, StringComparison.Ordinal);
        Assert.Contains("notifyIcon.Icon = notificationMeaningIcon", trayController, StringComparison.Ordinal);
        Assert.Contains("notifyIcon.Icon = applicationIcon", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("notifyIcon.BalloonTipShown +=", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("notifyIcon.BalloonTipClosed +=", trayController, StringComparison.Ordinal);
        Assert.Contains("notifyIcon.BalloonTipClicked += NotifyIconBalloonTipClicked", trayController, StringComparison.Ordinal);
        Assert.Contains("Interval = TrayNotificationIconLeasePolicy.FallbackDelay", trayController, StringComparison.Ordinal);
        Assert.Contains("LegacyNotificationDisplayInterval = TimeSpan.FromSeconds(4)", trayController, StringComparison.Ordinal);
        Assert.Contains("notificationIconRestoreTimer.Start()", trayController, StringComparison.Ordinal);
        Assert.Contains("legacyNotificationAdvanceTimer.Start()", trayController, StringComparison.Ordinal);
        Assert.Contains("Stopwatch.GetElapsedTime(notificationIconLeaseStartedTimestamp)", trayController, StringComparison.Ordinal);
        Assert.Contains("Stopwatch.GetElapsedTime(legacyNotificationStartedTimestamp)", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationIconLeasePolicy.Resolve", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationIconLeaseSignal.FallbackElapsed", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationIconLeaseSignal.DeliveryFailed", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationIconLeaseSignal.Disposed", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayPresentationPolicy.ShouldRequestAvailabilityConfirmation(intent)", trayController, StringComparison.Ordinal);
        Assert.Contains("ShowCloseToTrayNotification()", trayController, StringComparison.Ordinal);
        Assert.Contains("WindowsAppNotificationPublisher.TryCreate", trayController, StringComparison.Ordinal);
        Assert.Contains("appNotificationPublisher?.TryPublishAvailability(title, message)", trayController, StringComparison.Ordinal);
        Assert.Contains("instanceStates = evidence.CaptureInstanceStates(evidence.GeneratedAt)", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayInstanceStateChangePolicy.DetectChanges", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationTransitionValidationMatrix.Cases", trayController, StringComparison.Ordinal);
        Assert.Contains("PublishNextTransitionValidationCase()", trayController, StringComparison.Ordinal);
        Assert.Contains("Tray.TransitionValidationTitle", trayController, StringComparison.Ordinal);
        Assert.Contains("Tray.TransitionValidationCase", trayController, StringComparison.Ordinal);
        Assert.True(
            trayController.IndexOf("instanceStates = nextInstanceStates", StringComparison.Ordinal) <
            trayController.IndexOf("PublishDemonstrationStatusChanges(changes)", StringComparison.Ordinal),
            "The local baseline must advance before status-change delivery is attempted.");
        Assert.Contains("TryPublishDemonstrationStatusChange", trayController, StringComparison.Ordinal);
        Assert.Contains("QueueLegacyNotification(title, message, meaning)", trayController, StringComparison.Ordinal);
        Assert.Contains("MaximumLegacyNotificationQueueLength = 16", trayController, StringComparison.Ordinal);
        Assert.Contains("legacyNotificationQueue.Enqueue", trayController, StringComparison.Ordinal);
        Assert.Contains("legacyNotificationInFlight", trayController, StringComparison.Ordinal);
        Assert.Contains("notificationIconRestoreTimer.Stop();", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("firstHide", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("ShowFirstHideNotification", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("notificationIconRestorePending", trayController, StringComparison.Ordinal);
        Assert.Contains("Forms.ToolTipIcon.None", trayController, StringComparison.Ordinal);
        Assert.True(
            Regex.Count(trayController, "\\bShowBalloonTip\\s*\\(", RegexOptions.CultureInvariant) == 1,
            "STATE-05 must keep exactly one local legacy fallback callsite.");
        Assert.Contains("AppNotificationManager.IsSupported()", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("Bootstrap.TryInitialize", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("Bootstrap.InitializeOptions.None", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("runtime-initialisation-failed", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("manager.Register(DisplayName, new Uri(iconPath))", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("DBNotifier.Availability.png", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("TryPublishDemonstrationStatusChange", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("SetAppLogoOverride(new Uri(iconPath), AppNotificationImageCrop.Default, title)", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("DBNotifier.Healthy.png", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("DBNotifier.Warning.png", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("DBNotifier.Critical.png", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("DBNotifier.Unknown.png", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("private const string AvailabilityGroup = \"local-avail\"", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("private const string DemonstrationStatusGroup = \"local-demo\"", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("WindowsNotificationIdentifierMaximumLength = 16", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("Guid.NewGuid().ToString(\"N\")[..4]", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("instanceId.ToString(\"N\")[^3..]", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("$\"d{demonstrationStatusSession}{instanceSuffix}{sequence:x8}\"", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("CreateDemonstrationStatusTag(instanceId)", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("Interlocked.Increment(ref demonstrationStatusSequence)", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("tag.Length <= WindowsNotificationIdentifierMaximumLength", notificationPublisher, StringComparison.Ordinal);
        Assert.Equal(16, 1 + 4 + 3 + 8);
        Assert.Contains("DemonstrationStatusGroup", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains(".MuteAudio()", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("notification.Tag = AvailabilityTag", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("notification.Group = AvailabilityGroup", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("notification.Expiration = DateTimeOffset.UtcNow.Add(AvailabilityLifetime)", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("notification.ExpiresOnReboot = true", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("notification.SuppressDisplay = false", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("manager.Show(notification)", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("manager.NotificationInvoked -= NotificationInvoked", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("args.Arguments.TryGetValue(\"action\"", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("Bootstrap.Shutdown()", notificationPublisher, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthStatus", notificationPublisher, StringComparison.Ordinal);
        Assert.DoesNotContain("service.start", notificationPublisher, StringComparison.Ordinal);
        Assert.Contains("<TargetFramework>net10.0-windows10.0.22621.0</TargetFramework>", project, StringComparison.Ordinal);
        Assert.Contains("<WindowsPackageType>None</WindowsPackageType>", project, StringComparison.Ordinal);
        Assert.Contains("<WindowsAppSdkBootstrapInitialize>false</WindowsAppSdkBootstrapInitialize>", project, StringComparison.Ordinal);
        Assert.Contains("<PackageReference Include=\"Microsoft.WindowsAppSDK\" />", project, StringComparison.Ordinal);
        foreach (string asset in new[] { "Availability", "Healthy", "Warning", "Critical", "Unknown" })
        {
            Assert.Contains($"NotificationAssets\\DBNotifier.{asset}.png", project, StringComparison.Ordinal);
        }
        Assert.Contains("WmSetIcon", policy, StringComparison.Ordinal);
        Assert.Contains("IconSmall2", policy, StringComparison.Ordinal);
        Assert.Contains("IconBig", policy, StringComparison.Ordinal);
    }

    /// <summary>Ensures WPF accessibility, dialogue and High Contrast contracts remain explicit in source and token-driven markup.</summary>
    [Fact]
    public void WpfAccessibilityContractsCoverStateChangesGraphsDialogsAndTargets()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string themeService = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopThemeService.cs"));
        string controlStyles = File.ReadAllText(Path.Combine(desktopDirectory, "Resources", "ControlStyles.xaml"));
        string dialogMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "CapabilityPreviewDialog.xaml"));
        string dialog = File.ReadAllText(Path.Combine(desktopDirectory, "CapabilityPreviewDialog.xaml.cs"));

        Assert.Contains("<Setter Property=\"Width\" Value=\"{DynamicResource ControlHeightCompact}\" />", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Height\" Value=\"{DynamicResource ControlHeightCompact}\" />", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Count(mainWindowMarkup, "AutomationProperties.Name=\"\\{DynamicResource Overview\\.TrendAccessibleLabel\\}\"", RegexOptions.CultureInvariant));
        Assert.Contains("AutomationProperties.SetItemStatus(button, active ? Text(\"Navigation.Current\") : string.Empty)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("AutomationEvents.LiveRegionChanged", mainWindow, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StateMessageText\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.SetName(StateMessageText", mainWindow, StringComparison.Ordinal);
        Assert.Contains("UIElementAutomationPeer.CreatePeerForElement(StateMessageText)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("KeyboardNavigation.TabNavigation=\"Cycle\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ExecuteButton\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"False\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("IsCancel=\"True\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", dialogMarkup, StringComparison.Ordinal);
        Assert.Contains("NativeWindowThemePolicy.Apply(this, theme)", dialog, StringComparison.Ordinal);
        Assert.Contains("e.Key == System.Windows.Input.Key.Escape", dialog, StringComparison.Ordinal);
        Assert.Contains("dialog.ShowDialog()", mainWindow, StringComparison.Ordinal);
        Assert.Contains("opener?.Focus()", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("MessageBox.Show", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Trigger Property=\"IsEnabled\" Value=\"False\">", controlStyles, StringComparison.Ordinal);
        Assert.Contains("ComponentBrandWordmarkAccentBrush", themeService, StringComparison.Ordinal);
        for (int category = 1; category <= 5; category++)
        {
            Assert.Contains($"ColourDataCategory{category}Brush", themeService, StringComparison.Ordinal);
        }
    }

    /// <summary>Ensures language, navigation and bounded refreshes age one immutable desktop evidence snapshot across shell and Tray.</summary>
    [Fact]
    public void WpfDesktopSurfacesShareImmutableFreshnessEvidence()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string application = File.ReadAllText(Path.Combine(desktopDirectory, "App.xaml.cs"));
        string evidence = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopDemonstrationEvidence.cs"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string flyout = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml.cs"));
        string trayController = File.ReadAllText(Path.Combine(desktopDirectory, "TrayApplicationController.cs"));

        Assert.Equal(1, Regex.Count(application, "DesktopDemonstrationEvidence\\.Create", RegexOptions.CultureInvariant));
        Assert.Contains("TrayNotificationValidationPolicy.Resolve(e.Args)", application, StringComparison.Ordinal);
        Assert.Contains("notificationValidationMode", application, StringComparison.Ordinal);
        Assert.Contains("evidence.CreateInventorySnapshot(localisation)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CreateTimelineSnapshot(evidence.GeneratedAt)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("FormatUtc(evidence.GeneratedAt)", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("fixtureGeneratedAt", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TrayFleetPresentationPolicy.Summarise(CreateInventorySnapshot", evidence, StringComparison.Ordinal);
        Assert.Contains("TrayInstanceStateChangePolicy.Capture(CreateInventorySnapshot", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("TrayNotificationTransitionValidationMatrix", evidence, StringComparison.Ordinal);
        Assert.Contains("Interval = TimeSpan.FromSeconds(30)", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayInstanceStateChangePolicy.DetectChanges", trayController, StringComparison.Ordinal);
        Assert.Contains("PublishDemonstrationStatusChanges(changes)", trayController, StringComparison.Ordinal);
        Assert.Contains("advanceTransitionValidation: false", trayController, StringComparison.Ordinal);
        Assert.Contains("advanceTransitionValidation: true", trayController, StringComparison.Ordinal);
        Assert.Contains("window.RefreshOperationalEvidence(evaluatedAt, next.State)", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("notifyIcon.DoubleClick", trayController, StringComparison.Ordinal);
        Assert.Contains("FormatUtc(evidence.GeneratedAt)", flyout, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeProvider.System.GetUtcNow", flyout, StringComparison.Ordinal);
    }

    /// <summary>Finds the repository root from the compiled test output without depending on the caller's working directory.</summary>
    /// <returns>The absolute directory containing the solution file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the test assembly is not running below the repository.</exception>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
