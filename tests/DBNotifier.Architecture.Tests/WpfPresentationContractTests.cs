// Module purpose: Guards WPF markup contracts that compile successfully but can fail when templates are materialised at runtime.
using System.Text.Json;
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

    /// <summary>Ensures shared labels are unambiguous while the WPF rail uses neutral surfaces and an explicit current-route accent.</summary>
    [Fact]
    public void WpfAndWebNavigationShareCanonicalLabelsAndSemanticRail()
    {
        string root = RepositoryRoot();
        string portuguese = File.ReadAllText(Path.Combine(root, "localisation", "messages.pt-BR.xml"));
        string english = File.ReadAllText(Path.Combine(root, "localisation", "messages.en-GB.xml"));
        string markup = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Desktop.Wpf", "MainWindow.xaml"));
        string behaviour = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Desktop.Wpf", "MainWindow.xaml.cs"));
        string portugueseAdapter = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Desktop.Wpf", "Generated", "Localisation.pt-BR.xaml"));
        string englishAdapter = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Desktop.Wpf", "Generated", "Localisation.en-GB.xaml"));
        string designSystem = File.ReadAllText(Path.Combine(root, "docs", "design", "DB-Notifier-Design-System.md"));

        Assert.Contains("<message key=\"Navigation.Configuration\">Configuração operacional</message>", portuguese, StringComparison.Ordinal);
        Assert.Contains("<message key=\"Navigation.Settings\">Preferências</message>", portuguese, StringComparison.Ordinal);
        Assert.Contains("<message key=\"Navigation.Configuration\">Operational configuration</message>", english, StringComparison.Ordinal);
        Assert.Contains("<message key=\"Navigation.Settings\">Preferences</message>", english, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Navigation.Configuration\">Configuração operacional</sys:String>", portugueseAdapter, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Navigation.Settings\">Preferências</sys:String>", portugueseAdapter, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Navigation.Configuration\">Operational configuration</sys:String>", englishAdapter, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"Navigation.Settings\">Preferences</sys:String>", englishAdapter, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"{DynamicResource Navigation.Configuration}\"", markup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"{DynamicResource Navigation.Settings}\"", markup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DesktopNavigationRail\"", markup, StringComparison.Ordinal);
        Assert.Contains("Background=\"{DynamicResource ColourSurfaceSubtleBrush}\"", markup, StringComparison.Ordinal);
        Assert.Contains("BorderBrush=\"{DynamicResource ColourBorderDefaultBrush}\"", markup, StringComparison.Ordinal);
        Assert.DoesNotMatch("x:Name=\"DesktopNavigationRail\"[^>]*ComponentShellChrome", markup);
        Assert.Contains("<ColumnDefinition Width=\"230\" />", markup, StringComparison.Ordinal);
        Assert.Contains("Width=\"1180\"", markup, StringComparison.Ordinal);
        Assert.Contains("Height=\"760\"", markup, StringComparison.Ordinal);
        Assert.Contains("MinWidth=\"820\"", markup, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"620\"", markup, StringComparison.Ordinal);
        Assert.Contains("active ? \"ColourSelectionBackgroundBrush\" : \"ColourSurfaceSubtleBrush\"", behaviour, StringComparison.Ordinal);
        Assert.Contains("active ? \"ColourActionPrimaryBackgroundBrush\" : \"ColourSurfaceSubtleBrush\"", behaviour, StringComparison.Ordinal);
        Assert.Contains("bool compact = availableWidth < 1100", behaviour, StringComparison.Ordinal);
        Assert.Contains("bool compactInventorySummary = availableWidth < 1240", behaviour, StringComparison.Ordinal);
        Assert.Contains("OverviewSummaryGrid.Columns = compact ? 2 : 4", behaviour, StringComparison.Ordinal);
        Assert.Contains("ArrangeAdaptivePair(SettingsPreferencePanel, SettingsNotificationsPanel, compact)", behaviour, StringComparison.Ordinal);
        Assert.Contains("### 14.1 Shared destination consistency matrix", designSystem, StringComparison.Ordinal);
        Assert.Contains("Configuração operacional / Operational configuration", designSystem, StringComparison.Ordinal);
        Assert.Contains("Preferências / Preferences", designSystem, StringComparison.Ordinal);
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

    /// <summary>Ensures WPF provider identities use only declared local theme assets and retain neutral accessible fallbacks.</summary>
    [Fact]
    public void WpfProviderIdentitiesUseLocalThemeAssetsAndNeutralFallbacks()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string policy = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderVisualIdentityPolicy.cs"));
        string generatedRegistry = File.ReadAllText(Path.Combine(desktopDirectory, "Generated", "ProviderIconRegistry.g.cs"));
        string identityMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderIdentityView.xaml"));
        string identityView = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderIdentityView.xaml.cs"));
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string flyoutMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml"));
        string project = File.ReadAllText(Path.Combine(desktopDirectory, "DBNotifier.Desktop.Wpf.csproj"));
        string iconDirectory = Path.Combine(desktopDirectory, "Assets", "ProviderIcons");
        string[] providerTypes =
        [
            "cassandra",
            "dynamodb",
            "elasticsearch",
            "firebase",
            "mongodb",
            "mysql",
            "planetscale",
            "postgresql",
            "redis",
            "sqlite",
            "supabase",
        ];
        using JsonDocument manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "design-system",
            "provider-icons",
            "manifest.json")));
        string[] manifestProviderTypes = manifest.RootElement.GetProperty("icons")
            .EnumerateArray()
            .Select(icon => icon.GetProperty("providerType").GetString()!)
            .ToArray();

        Assert.Equal(providerTypes, manifestProviderTypes);
        Assert.Contains("new Uri(resourcePath, UriKind.Relative)", policy, StringComparison.Ordinal);
        Assert.Contains("UriKind.Relative", policy, StringComparison.Ordinal);
        Assert.Contains("theme.EffectiveTheme == EffectiveTheme.Dark", policy, StringComparison.Ordinal);
        Assert.Contains("theme.IsHighContrastActive", policy, StringComparison.Ordinal);
        Assert.Contains("string visibleProviderType = providerType ?? \"unknown\"", policy, StringComparison.Ordinal);
        Assert.Contains("GeneratedProviderIconRegistry.Assets.TryGetValue(providerType", policy, StringComparison.Ordinal);
        Assert.DoesNotContain(".Trim()", policy, StringComparison.Ordinal);
        Assert.Contains("new ProviderVisualIdentity(visibleProviderType, null)", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("http://", policy, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("https://", policy, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<Resource Include=\"Assets\\ProviderIcons\\*.png\" />", project, StringComparison.Ordinal);
        Assert.Contains("SkillIcons.LICENSE.txt", project, StringComparison.Ordinal);
        Assert.Contains("THIRD-PARTY-NOTICES.md", project, StringComparison.Ordinal);

        foreach (string providerType in providerTypes)
        {
            Assert.Contains($"[\"{providerType}\"] = new(\"Assets/ProviderIcons/{providerType}-light.png\", \"Assets/ProviderIcons/{providerType}-dark.png\")", generatedRegistry, StringComparison.Ordinal);
            Assert.True(File.Exists(Path.Combine(iconDirectory, $"{providerType}-light.png")), $"Missing Light provider icon for '{providerType}'.");
            Assert.True(File.Exists(Path.Combine(iconDirectory, $"{providerType}-dark.png")), $"Missing Dark provider icon for '{providerType}'.");
        }

        Assert.Contains("<local:DecorativeProviderImage", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("RenderOptions.BitmapScalingMode=\"HighQuality\"", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProviderText\"", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("Grid.Column=\"1\"", identityMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<StackPanel Orientation=\"Horizontal\"", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FallbackGlyph\"", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("ImageFailed=\"ProviderImageFailed\"", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("UseNeutralFallback()", identityView, StringComparison.Ordinal);
        Assert.Contains("or FormatException", identityView, StringComparison.Ordinal);
        Assert.Contains("Application.GetResourceStream(candidate)", identityView, StringComparison.Ordinal);
        Assert.Contains("GetManifestResourceStream(manifestName)", identityView, StringComparison.Ordinal);
        Assert.Contains("using ResourceReader reader", identityView, StringComparison.Ordinal);
        Assert.Contains("source.StreamSource = stream", identityView, StringComparison.Ordinal);
        Assert.Contains("nameof(ShowText)", identityView, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => null", identityView, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationProperties.AccessibilityView", identityMarkup, StringComparison.Ordinal);
        Assert.Contains("ProviderIdentityView", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Equal(3, Regex.Count(mainWindowMarkup, "SortMemberPath=\\\"ProviderType\\\""));
        Assert.Contains("ProviderIdentityView", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BrandStatusImage\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"BrandStatusImage\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FinanceStatusGlyph\"", flyoutMarkup, StringComparison.Ordinal);
    }

    /// <summary>Ensures navigation, motion and flyout geometry use code-native and bounded presentation contracts.</summary>
    [Fact]
    public void WpfAutomaticAccessibilityContractsRemainNonVisualAndBounded()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string application = File.ReadAllText(Path.Combine(desktopDirectory, "App.xaml.cs"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string motion = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopMotionService.cs"));
        string flyout = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml.cs"));
        string flyoutMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml"));

        Assert.Contains("new DesktopMotionService()", application, StringComparison.Ordinal);
        Assert.Contains("SystemParameters.ClientAreaAnimation", motion, StringComparison.Ordinal);
        Assert.Contains("PopupAnimation.None", mainWindow, StringComparison.Ordinal);
        Assert.Contains("PopupAnimation.Fade", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"⌂\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"⚙\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Equal(8, Regex.Count(mainWindowMarkup, "Style=\"\\{StaticResource SidebarNavigationIcon\\}\"", RegexOptions.CultureInvariant));
        Assert.Contains("DpiChanged=\"WindowDpiChanged\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FlyoutBodyScrollViewer\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("Forms.Screen.FromPoint(Forms.Cursor.Position).WorkingArea", flyout, StringComparison.Ordinal);
        Assert.Contains("ApplyResponsiveLayout(Width < 480)", flyout, StringComparison.Ordinal);
        Assert.Contains("DesktopDateTimePresentation.FormatUtc", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DesktopDateTimePresentation.FormatUtc", flyout, StringComparison.Ordinal);
        Assert.Contains("Common.ObservedAtUtc", mainWindow, StringComparison.Ordinal);
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
        Assert.Contains("DesktopDateTimePresentation.FormatUtc(item.ObservedAt, localisation.Culture)", mainWindow, StringComparison.Ordinal);
        Assert.Matches("CreateSparkline\\(index, item.Enabled\\),\\s*observedAt,\\s*latency\\);", mainWindow);
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
        string manifest = File.ReadAllText(Path.Combine(desktopDirectory, "app.manifest"));
        string entryPoint = File.ReadAllText(Path.Combine(desktopDirectory, "Program.cs"));

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
        Assert.Contains("TrayInstanceStateChangePolicy.Capture(", trayController, StringComparison.Ordinal);
        Assert.Contains("initialFrame.EvaluatedAt", trayController, StringComparison.Ordinal);
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
        Assert.Contains("<ApplicationManifest>app.manifest</ApplicationManifest>", project, StringComparison.Ordinal);
        Assert.Contains("<ApplicationHighDpiMode>PerMonitorV2</ApplicationHighDpiMode>", project, StringComparison.Ordinal);
        Assert.Contains("<StartupObject>DBNotifier.Desktop.Wpf.Program</StartupObject>", project, StringComparison.Ordinal);
        Assert.Contains("<requestedExecutionLevel level=\"asInvoker\" uiAccess=\"false\" />", manifest, StringComparison.Ordinal);
        Assert.DoesNotContain("dpiAware", manifest, StringComparison.Ordinal);
        Assert.Contains("Forms.Application.SetHighDpiMode(Forms.HighDpiMode.PerMonitorV2)", entryPoint, StringComparison.Ordinal);
        Assert.True(
            entryPoint.IndexOf("SetHighDpiMode", StringComparison.Ordinal) <
            entryPoint.IndexOf("App application = new()", StringComparison.Ordinal),
            "Per-Monitor V2 awareness must be selected before the WPF application creates any presentation resources.");
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
        string application = File.ReadAllText(Path.Combine(desktopDirectory, "App.xaml.cs"));
        string presentation = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "DBNotifier.Application", "Presentation", "TrayPresentation.cs"));

        Assert.Contains("<Setter Property=\"Width\" Value=\"{DynamicResource ControlHeightCompact}\" />", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Height\" Value=\"{DynamicResource ControlHeightCompact}\" />", mainWindowMarkup, StringComparison.Ordinal);
        string performanceChartMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "PerformanceChart.xaml"));
        Assert.Equal(2, Regex.Count(mainWindowMarkup, "<local:PerformanceChart", RegexOptions.CultureInvariant));
        Assert.Contains("AutomationProperties.Name=\"{DynamicResource Overview.TrendAccessibleLabel}\"", performanceChartMarkup, StringComparison.Ordinal);
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
        Assert.Contains("<Setter Property=\"MaxDropDownHeight\" Value=\"320\" />", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("MaxHeight=\"{TemplateBinding MaxDropDownHeight}\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("VerticalScrollBarVisibility=\"Auto\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("--review-combobox-overflow", presentation, StringComparison.Ordinal);
        Assert.Contains("DesktopAccessibilityReviewPolicy.Resolve(e.Args)", application, StringComparison.Ordinal);
        Assert.Contains("DesktopAccessibilityReviewMode.ComboBoxOverflow => 128", mainWindow, StringComparison.Ordinal);
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

    /// <summary>Ensures the WPF shell uses reusable parity components and retains the complete Web chart information hierarchy.</summary>
    [Fact]
    public void WpfStructuralParityUsesSharedComponentsAndCompleteCharts()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string chartMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "PerformanceChart.xaml"));
        string distributionMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderDistributionChart.xaml"));
        string distribution = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderDistributionChart.xaml.cs"));
        string metricMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "KpiCard.xaml"));
        string statusMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "StatusPill.xaml"));
        string semanticIcons = File.ReadAllText(Path.Combine(desktopDirectory, "SemanticIcon.xaml.cs"));

        Assert.Equal(13, Regex.Count(mainWindowMarkup, "<local:KpiCard", RegexOptions.CultureInvariant));
        Assert.Equal(13, Regex.Count(mainWindowMarkup, "<local:KpiCard[^>]*Margin=\"8\"", RegexOptions.CultureInvariant));
        Assert.Equal(2, Regex.Count(mainWindowMarkup, "<local:PerformanceChart", RegexOptions.CultureInvariant));
        Assert.Equal(2, Regex.Count(mainWindowMarkup, "<local:ProviderDistributionChart", RegexOptions.CultureInvariant));
        Assert.True(Regex.Count(mainWindowMarkup, "<local:StatusPill", RegexOptions.CultureInvariant) >= 3);
        Assert.Contains("Icon=\"Database\" Tone=\"Neutral\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Icon=\"Healthy\" Tone=\"Healthy\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Icon=\"Degraded\" Tone=\"Degraded\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Icon=\"Critical\" Tone=\"Critical\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("StatusTone", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SparklineTone", mainWindow, StringComparison.Ordinal);
        Assert.Contains("UpdatedAtLabel", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ProviderDistributionItem", mainWindow, StringComparison.Ordinal);
        Assert.Contains("RefreshProviderCharts()", mainWindow, StringComparison.Ordinal);

        Assert.Contains("BorderThickness=\"1\"", metricMarkup, StringComparison.Ordinal);
        Assert.Contains("<local:SemanticIcon", metricMarkup, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Count(metricMarkup, "Foreground=\"\\{Binding Foreground, ElementName=Root\\}\"", RegexOptions.CultureInvariant));
        Assert.DoesNotContain("SemanticForeground", metricMarkup, StringComparison.Ordinal);
        Assert.Contains("BorderThickness=\"1\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("SemanticIconKind.Database", semanticIcons, StringComparison.Ordinal);
        Assert.Contains("SemanticIconKind.Disabled", semanticIcons, StringComparison.Ordinal);
        Assert.Contains("SemanticIconKind.Unknown", semanticIcons, StringComparison.Ordinal);
        Assert.Contains("SemanticIconKind.Performance", semanticIcons, StringComparison.Ordinal);
        Assert.True(Regex.Count(mainWindowMarkup, "Icon=\"\\{Binding StatusIcon\\}\"", RegexOptions.CultureInvariant) >= 2);
        Assert.Equal(2, Regex.Count(mainWindowMarkup, "Icon=\"\\{Binding IconKind\\}\"", RegexOptions.CultureInvariant));
        Assert.Contains("x:Name=\"AlertTotalCard\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Icon=\"Healthy\" Tone=\"Neutral\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Property=\"BorderThickness\" Value=\"3,2,2,2\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<Ellipse", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("CreateSegment", distribution, StringComparison.Ordinal);
        Assert.Contains("ColourDataCategory", distribution, StringComparison.Ordinal);

        Assert.Equal(2, Regex.Count(chartMarkup, "<Polyline", RegexOptions.CultureInvariant));
        foreach (string label in new[] { "100%", "50%", "0%", "09:50", "09:55", "10:00", "10:05", "10:10", "10:15" })
        {
            Assert.Contains($"Text=\"{label}\"", chartMarkup, StringComparison.Ordinal);
        }
        Assert.Contains("M0,25 H640 M0,75 H640 M0,125 H640", chartMarkup, StringComparison.Ordinal);
        Assert.Contains("ClipToBounds=\"True\"", chartMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.HelpText=\"{DynamicResource Overview.DemonstrationChart}\"", chartMarkup, StringComparison.Ordinal);
    }

    /// <summary>Ensures R6-UI1 keeps compact WPF geometry bounded, informative and accessible without changing the Web reference.</summary>
    [Fact]
    public void WpfUi1GeometryAndCompactAccessibilityContractsRemainBounded()
    {
        string repositoryRoot = RepositoryRoot();
        string desktopDirectory = Path.Combine(repositoryRoot, "src", "DBNotifier.Desktop.Wpf");
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string statusMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "StatusPill.xaml"));
        string status = File.ReadAllText(Path.Combine(desktopDirectory, "StatusPill.xaml.cs"));
        string distributionMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderDistributionChart.xaml"));
        string distribution = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderDistributionChart.xaml.cs"));
        string providerIdentityMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "ProviderIdentityView.xaml"));
        string auditor = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "audit-state05-wpf.ps1"));

        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", statusMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("MaxWidth=", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"PillBorder\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("CornerRadius=\"6\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("<Grid MinWidth=\"0\">", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StatusIcon\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StatusText\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusPill\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("<local:StatusPillTextBlock", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"StatusPillText\"", statusMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("AutomationProperties.AccessibilityView", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("ToolTip=\"{Binding Text, ElementName=Root}\"", statusMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("<StackPanel Orientation=\"Horizontal\"", statusMarkup, StringComparison.Ordinal);
        Assert.Contains("OnCreateAutomationPeer() => new StatusPillAutomationPeer(this)", status, StringComparison.Ordinal);
        Assert.Contains("sealed class StatusPillAutomationPeer : FrameworkElementAutomationPeer", status, StringComparison.Ordinal);
        Assert.Contains("GetAutomationControlTypeCore() => AutomationControlType.Text", status, StringComparison.Ordinal);
        Assert.Contains("GetClassNameCore() => nameof(StatusPill)", status, StringComparison.Ordinal);
        Assert.Contains("GetNameCore() => ((StatusPill)Owner).Text", status, StringComparison.Ordinal);
        Assert.Contains("override List<AutomationPeer> GetChildrenCore()", status, StringComparison.Ordinal);
        Assert.Contains("UIElementAutomationPeer.CreatePeerForElement(((StatusPill)Owner).StatusText)", status, StringComparison.Ordinal);
        Assert.Contains("sealed class StatusPillTextBlock : TextBlock", status, StringComparison.Ordinal);
        Assert.Contains("new StatusPillTextAutomationPeer(this)", status, StringComparison.Ordinal);
        Assert.Contains("IsControlElementCore() => false", status, StringComparison.Ordinal);
        Assert.Contains("IsContentElementCore() => false", status, StringComparison.Ordinal);
        Assert.Contains("GetAutomationIdCore() => \"StatusPillText\"", status, StringComparison.Ordinal);
        Assert.Contains("IsStatusIcon(Icon) ? Icon : SemanticIconKind.Unknown", status, StringComparison.Ordinal);
        Assert.Contains("private static bool IsStatusIcon", status, StringComparison.Ordinal);

        foreach (string elementName in new[] { "DistributionLayout", "RingViewport", "SegmentCanvas", "Legend" })
        {
            Assert.Contains($"x:Name=\"{elementName}\"", distributionMarkup, StringComparison.Ordinal);
        }
        Assert.Contains("SizeChanged=\"DistributionSizeChanged\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("Width=\"126\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("Height=\"126\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,12,0\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.AutomationId=\"ProviderDistributionRingViewport\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("<local:DistributionRingViewbox", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("Width=\"44\" Height=\"44\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("Canvas.Left=\"6.1\" Canvas.Top=\"6.1\" Width=\"31.8\" Height=\"31.8\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("StrokeThickness=\"7\"", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", distributionMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("CharacterEllipsis", distributionMarkup, StringComparison.Ordinal);
        Assert.Contains("availableWidth < 220", distribution, StringComparison.Ordinal);
        Assert.Contains("availableWidth < 300", distribution, StringComparison.Ordinal);
        Assert.Contains("RingViewport.Width = compact ? 96 : 126", distribution, StringComparison.Ordinal);
        Assert.Contains("Grid.SetRow(Legend, stacked ? 1 : 0)", distribution, StringComparison.Ordinal);
        Assert.Contains("Grid.SetColumnSpan(Legend, stacked ? 2 : 1)", distribution, StringComparison.Ordinal);
        Assert.Contains("new Thickness(0, 8, 0, 0)", distribution, StringComparison.Ordinal);
        Assert.Contains("int visibleSegmentCount", distribution, StringComparison.Ordinal);
        Assert.Contains("Math.Min(4d, sweep / 3d)", distribution, StringComparison.Ordinal);
        Assert.Contains("sealed class DistributionRingViewbox : Viewbox", distribution, StringComparison.Ordinal);
        Assert.Contains("new DistributionRingViewboxAutomationPeer(this)", distribution, StringComparison.Ordinal);
        Assert.Contains("GetAutomationIdCore() => \"ProviderDistributionRingViewport\"", distribution, StringComparison.Ordinal);
        Assert.Contains("IsControlElementCore() => false", distribution, StringComparison.Ordinal);
        Assert.Contains("IsContentElementCore() => false", distribution, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"ProviderText\"", providerIdentityMarkup, StringComparison.Ordinal);
        Assert.Contains("TextWrapping=\"Wrap\"", providerIdentityMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("TextTrimming=\"CharacterEllipsis\"", providerIdentityMarkup, StringComparison.Ordinal);

        Assert.Contains("x:Name=\"DesktopContentScrollViewer\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility=\"Disabled\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DesktopContentRoot\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("RowHeight=\"Auto\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.DoesNotContain("ColumnHeaderHeight=\"Auto\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"DesktopDataGridContent\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Margin\" Value=\"12,0\" />", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<local:AuditableCardBorder x:Name=\"OverviewProvidersPanel\" AutomationProperties.AutomationId=\"OverviewProviderCard\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<local:AuditableCardBorder AutomationProperties.AutomationId=\"ProvidersRouteDistributionCard\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("sealed class AuditableCardBorder : Border", mainWindow, StringComparison.Ordinal);
        Assert.Contains("new AuditableCardBorderAutomationPeer(this)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsControlElementCore() => false", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsContentElementCore() => false", mainWindow, StringComparison.Ordinal);
        foreach (string template in new[]
                 {
                     "OverviewInventoryDesktopRowTemplate",
                     "OverviewInventoryCompactRowTemplate",
                     "InventoryCompactRowTemplate",
                     "TimelineCompactRowTemplate",
                     "AlertCompactRowTemplate",
                     "ConfigurationCompactRowTemplate",
                     "CapabilityCompactRowTemplate",
                     "ProviderCatalogueCompactPanel",
                 })
        {
            Assert.Contains($"x:Key=\"{template}\"", mainWindowMarkup, StringComparison.Ordinal);
        }
        foreach (string key in new[]
                 {
                     "Common.Instance",
                     "Common.Support",
                     "Common.Environment",
                     "Common.ObservedAtUtc",
                     "Common.Latency",
                     "Common.State",
                     "Common.Provider",
                     "Common.Updated",
                     "Common.TimeUtc",
                     "Common.Capability",
                     "Common.Reason",
                 })
        {
            Assert.Contains($"DynamicResource {key}", mainWindowMarkup, StringComparison.Ordinal);
        }
        Assert.Contains("bool compact = availableWidth < 1100", mainWindow, StringComparison.Ordinal);
        Assert.Contains("OverviewSummaryGrid.Columns = compact ? 2 : 4", mainWindow, StringComparison.Ordinal);
        Assert.Contains("InventorySummaryGrid.Columns = compact ? 2", mainWindow, StringComparison.Ordinal);
        Assert.Contains("AlertSummaryGrid.Columns = compact ? 1 : 3", mainWindow, StringComparison.Ordinal);
        Assert.Contains("DesktopContentRoot.Margin = compact", mainWindow, StringComparison.Ordinal);
        Assert.Contains("OverviewInventoryList.ItemTemplate = (DataTemplate)FindResource(compact", mainWindow, StringComparison.Ordinal);
        Assert.Contains("? \"OverviewInventoryCompactRowTemplate\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains(": \"OverviewInventoryDesktopRowTemplate\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("<Border MinHeight=\"76\" Padding=\"14,10\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("<TextBlock Grid.Row=\"1\" Grid.Column=\"1\" Text=\"{Binding LatencyLabel}\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Equal(5, Regex.Count(mainWindow, "ConfigureResponsiveGrid\\(", RegexOptions.CultureInvariant) - 1);
        Assert.Contains("ScrollViewer.SetHorizontalScrollBarVisibility(grid, compact", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ScrollBarVisibility.Disabled", mainWindow, StringComparison.Ordinal);
        Assert.Contains("ProviderCatalogueCompactPanel", mainWindow, StringComparison.Ordinal);

        foreach (string auditorContract in new[]
                 {
                     "Find-AuditNamedElement",
                     "Find-AuditVisibleNamedElement",
                     "Find-AuditNamedElementByAutomationId",
                     "Find-AuditRawNamedElementByAutomationId",
                     "Find-AuditRawElementByAutomationId",
                     "Get-AuditRawDescendants",
                     "Get-AuditDesktopGridGeometryEvidence",
                     "Test-AuditOwnsElement",
                     "Test-AuditVisualRow",
                     "Test-AuditVerticalStack",
                     "Test-AuditVerticalOrder",
                     "compactKeys",
                     "horizontalViewSize",
                     "horizontalScrollPercent",
                     "pillContent",
                     "compactLayout",
                     "compactRecords",
                     "verticalReachability",
                     "providerDistribution",
                     "desktopTableGeometry",
                     "focusTargets",
                     "requiredGlobalFocus",
                     "dbnotifier.r6-ui1-audit.v1",
                 })
        {
            Assert.Contains(auditorContract, auditor, StringComparison.Ordinal);
        }
        Assert.Contains("$SampleWidth -eq 820", auditor, StringComparison.Ordinal);
        Assert.Contains("[System.Windows.Automation.ScrollPattern]::NoScroll", auditor, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Count(auditor, "key = \"Status.Disabled\"", RegexOptions.CultureInvariant));
        Assert.Contains("AutomationId \"StatusPill\" -Name $pillName", auditor, StringComparison.Ordinal);
        Assert.Contains("AutomationId \"StatusPillText\" -Name $pillName", auditor, StringComparison.Ordinal);
        Assert.Contains("[System.Windows.Automation.TreeWalker]::RawViewWalker", auditor, StringComparison.Ordinal);
        Assert.Contains("ProviderDistributionRingViewport", auditor, StringComparison.Ordinal);
        Assert.Contains("ringViewportBounds", auditor, StringComparison.Ordinal);
        Assert.Contains("ringViewportMargins", auditor, StringComparison.Ordinal);
        Assert.Contains("OverviewProviderCard", auditor, StringComparison.Ordinal);
        Assert.Contains("ProvidersRouteDistributionCard", auditor, StringComparison.Ordinal);
        Assert.Contains("cardBounds", auditor, StringComparison.Ordinal);
        Assert.Contains("minimumIntercolumnGap", auditor, StringComparison.Ordinal);
        Assert.Contains("firstContentGutter", auditor, StringComparison.Ordinal);
        Assert.Contains("lastContentGutter", auditor, StringComparison.Ordinal);
        Assert.Contains("containedByViewport", auditor, StringComparison.Ordinal);
        Assert.Contains("bounded-visible-row", auditor, StringComparison.Ordinal);
        Assert.Contains("visibleIntersectionHeight", auditor, StringComparison.Ordinal);
        Assert.Contains("minimumVisibleHeight", auditor, StringComparison.Ordinal);
        Assert.Contains("Test-AuditHorizontalContainment -Child $focusedRectangle", auditor, StringComparison.Ordinal);
        Assert.Contains("[ValidateRange(1, 512)][int]$MaximumNodes = 128", auditor, StringComparison.Ordinal);
        Assert.Contains("complete content containment cannot be proved", auditor, StringComparison.Ordinal);
        Assert.Contains("Get-AuditCompactGridEvidence", auditor, StringComparison.Ordinal);
        Assert.Contains("horizontallyScrollable", auditor, StringComparison.Ordinal);
        Assert.Contains("headerCount", auditor, StringComparison.Ordinal);
        Assert.Contains("inventoryGridCompact", auditor, StringComparison.Ordinal);
        Assert.Contains("alertsGridCompact", auditor, StringComparison.Ordinal);
        Assert.Contains("historyGridCompact", auditor, StringComparison.Ordinal);
        Assert.Contains("configurationGridCompact", auditor, StringComparison.Ordinal);
        Assert.Contains("capabilityGridCompact", auditor, StringComparison.Ordinal);
        Assert.DoesNotContain("Get-AuditGridColumnCount", auditor, StringComparison.Ordinal);
        Assert.Contains("summaryPrecedesRecords", auditor, StringComparison.Ordinal);
        Assert.Contains("configurationPrecedesPermission", auditor, StringComparison.Ordinal);
        Assert.Contains("permissionPrecedesCapabilities", auditor, StringComparison.Ordinal);
        Assert.Contains("DBNotifier-R6-UI1-Audit-", auditor, StringComparison.Ordinal);
        Assert.Contains("\"--test-subject\", \"r6-ui1-audit\"", auditor, StringComparison.Ordinal);
        Assert.Contains("r6-ui1-wpf-audit.json", auditor, StringComparison.Ordinal);
    }

    /// <summary>Ensures the existing WPF runner covers the authorised matrix, restores preferences and never enters the notification-producing close path.</summary>
    [Fact]
    public void WpfParityAuditorOwnsRoutesPreferencesAndExactCleanup()
    {
        string script = File.ReadAllText(Path.Combine(RepositoryRoot(), "scripts", "audit-state05-wpf.ps1"));
        string[] routes = ["Overview", "Inventory", "Alerts", "Performance", "History", "Configuration", "Providers", "Settings"];

        Assert.Contains("[switch]$ParityMatrix", script, StringComparison.Ordinal);
        foreach (string route in routes)
        {
            Assert.Contains($"id = \"{route}\"", script, StringComparison.Ordinal);
        }
        Assert.Contains("($route.id + \"NavigationButton\")", script, StringComparison.Ordinal);
        Assert.Contains("@(\"pt-BR\", \"en-GB\")", script, StringComparison.Ordinal);
        Assert.Contains("@(\"light\", \"dark\")", script, StringComparison.Ordinal);
        Assert.Contains("width = 820; height = 620", script, StringComparison.Ordinal);
        Assert.Contains("width = 1180; height = 760", script, StringComparison.Ordinal);
        Assert.Contains("width = 1920; height = 1080", script, StringComparison.Ordinal);
        Assert.Contains("ABC049CBB37CC998FF86E018E6853D811E58ED166B2B6B4A5CF0FBA4B171868F", script, StringComparison.Ordinal);
        Assert.Contains("[System.IO.File]::WriteAllBytes($PreferencePath, $PreferenceBytes)", script, StringComparison.Ordinal);
        Assert.Contains("preferenceHashAfter", script, StringComparison.Ordinal);
        Assert.Contains("--reconciled-notification-sandbox", script, StringComparison.Ordinal);
        Assert.Contains("--notifications-quiet", script, StringComparison.Ordinal);
        Assert.Contains("Stop-ExactAuditProcess", script, StringComparison.Ordinal);
        Assert.Contains("Stop-Process -Id $candidate.Id -Force", script, StringComparison.Ordinal);
        Assert.DoesNotContain("CloseMainWindow", script, StringComparison.Ordinal);
        Assert.Contains("Remove-OwnedAuditDirectory", script, StringComparison.Ordinal);
        Assert.Contains("NOT_TESTED", script, StringComparison.Ordinal);
        Assert.Contains("top-$($windowDpi)dpi.png", script, StringComparison.Ordinal);
        Assert.Contains("bottom-$($windowDpi)dpi.png", script, StringComparison.Ordinal);
    }

    /// <summary>Ensures desktop surfaces share validated frames from one serial read-only reconciliation boundary.</summary>
    [Fact]
    public void WpfDesktopSurfacesShareSerialReadOnlyReconciliationFrames()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string applicationDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Application", "Presentation");
        string application = File.ReadAllText(Path.Combine(desktopDirectory, "App.xaml.cs"));
        string evidence = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopDemonstrationEvidence.cs"));
        string source = File.ReadAllText(Path.Combine(desktopDirectory, "DesktopDemonstrationInventorySnapshotSource.cs"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string flyout = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml.cs"));
        string trayController = File.ReadAllText(Path.Combine(desktopDirectory, "TrayApplicationController.cs"));
        string reconciliation = File.ReadAllText(Path.Combine(applicationDirectory, "DesktopFleetReconciliation.cs"));

        Assert.Equal(1, Regex.Count(application, "DesktopDemonstrationEvidence\\.Create", RegexOptions.CultureInvariant));
        Assert.Contains("DesktopFleetReconciliationCoordinator reconciliation", application, StringComparison.Ordinal);
        Assert.Contains("configuredInventorySource ??", application, StringComparison.Ordinal);
        Assert.Contains("internal App(IDesktopFleetSnapshotSource inventorySource)", application, StringComparison.Ordinal);
        Assert.Contains("ReconcileAsync(DesktopFleetRefreshTrigger.Initial)", application, StringComparison.Ordinal);
        Assert.Contains("TrayNotificationValidationPolicy.Resolve(e.Args)", application, StringComparison.Ordinal);
        Assert.Contains("notificationValidationMode", application, StringComparison.Ordinal);
        Assert.Contains("evidence.CreateInventorySnapshot(localisation)", source, StringComparison.Ordinal);
        Assert.DoesNotContain("evidence.CreateInventorySnapshot(localisation)", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("evidence.CreateInventorySnapshot(localisation)", flyout, StringComparison.Ordinal);
        Assert.Contains("ApplyReconciledInventory", mainWindow, StringComparison.Ordinal);
        Assert.Contains("CreateTimelineSnapshot(evidence.GeneratedAt)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("FormatUtc(evidence.GeneratedAt)", mainWindow, StringComparison.Ordinal);
        Assert.DoesNotContain("fixtureGeneratedAt", mainWindow, StringComparison.Ordinal);
        Assert.Contains("TrayFleetPresentationPolicy.Summarise(CreateInventorySnapshot", evidence, StringComparison.Ordinal);
        Assert.Contains("TrayInstanceStateChangePolicy.Capture(CreateInventorySnapshot", evidence, StringComparison.Ordinal);
        Assert.DoesNotContain("TrayNotificationTransitionValidationMatrix", evidence, StringComparison.Ordinal);
        Assert.Contains("Interlocked.CompareExchange(ref acquisitionInProgress", reconciliation, StringComparison.Ordinal);
        Assert.Contains("lastAcceptedSnapshot", reconciliation, StringComparison.Ordinal);
        Assert.Contains("MaximumSnapshotItems", reconciliation, StringComparison.Ordinal);
        Assert.Contains("Interval = TimeSpan.FromSeconds(30)", trayController, StringComparison.Ordinal);
        Assert.Contains("fleetRefreshTimer.Stop()", trayController, StringComparison.Ordinal);
        Assert.Contains("fleetRefreshTimer.Start()", trayController, StringComparison.Ordinal);
        Assert.Contains("reconciliation.ReconcileAsync", trayController, StringComparison.Ordinal);
        Assert.Contains("DesktopFleetRefreshTrigger.Periodic", trayController, StringComparison.Ordinal);
        Assert.Contains("DesktopFleetRefreshTrigger.Manual", trayController, StringComparison.Ordinal);
        Assert.Contains("DesktopFleetRefreshTrigger.Localisation", trayController, StringComparison.Ordinal);
        Assert.Contains("TrayInstanceStateChangePolicy.DetectChanges", trayController, StringComparison.Ordinal);
        Assert.Contains("PublishDemonstrationStatusChanges(changes)", trayController, StringComparison.Ordinal);
        Assert.Contains("advanceTransitionValidation: false", trayController, StringComparison.Ordinal);
        Assert.Contains("advanceTransitionValidation: true", trayController, StringComparison.Ordinal);
        Assert.Contains("window.ApplyReconciledInventory", trayController, StringComparison.Ordinal);
        Assert.DoesNotContain("notifyIcon.DoubleClick", trayController, StringComparison.Ordinal);
        Assert.Contains("RefreshStatusClick", flyout, StringComparison.Ordinal);
        Assert.Contains("FormatUtc(snapshot.GeneratedAt)", flyout, StringComparison.Ordinal);
        Assert.DoesNotContain("TimeProvider.System.GetUtcNow", flyout, StringComparison.Ordinal);
    }

    /// <summary>Ensures the operational source uses the human API boundary and cannot silently adopt Agent identity or administrative routes.</summary>
    [Fact]
    public void DesktopFleetHttpSourceRemainsHumanAuthorisedAndReadOnly()
    {
        string root = RepositoryRoot();
        string source = File.ReadAllText(Path.Combine(
            root,
            "src",
            "DBNotifier.Infrastructure",
            "Presentation",
            "HttpDesktopFleetSnapshotSource.cs"));
        string endpoint = File.ReadAllText(Path.Combine(
            root,
            "src",
            "DBNotifier.Server.Api",
            "DesktopFleetEndpoints.cs"));
        string program = File.ReadAllText(Path.Combine(root, "src", "DBNotifier.Server.Api", "Program.cs"));

        Assert.Contains(": IDesktopFleetSnapshotSource", source, StringComparison.Ordinal);
        Assert.Contains("HttpMethod.Get", source, StringComparison.Ordinal);
        Assert.Contains("BoundedHttpJsonReader.ReadAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AuthorizationHeaderValue", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentApiPolicy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("HttpMethod.Post", source, StringComparison.Ordinal);
        Assert.DoesNotContain("commands", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("service.restart", source, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("RequireAuthorization(ApiSecurityDefaults.HumanApiPolicy)", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("AgentApiPolicy", endpoint, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", endpoint, StringComparison.Ordinal);
        Assert.Contains("app.MapDesktopFleetEndpoint();", program, StringComparison.Ordinal);
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
