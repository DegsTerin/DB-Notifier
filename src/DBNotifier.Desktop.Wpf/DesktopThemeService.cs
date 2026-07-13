// Module purpose: Resolves and applies System, Light, Dark and High Contrast WPF resource dictionaries without altering feature state.
using System.IO;
using System.Security;
using System.Windows;
using System.Windows.Threading;
using DBNotifier.Application.Presentation;
using Microsoft.Win32;
using WpfSystemColors = System.Windows.SystemColors;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the desktop theme preference, generated dictionary replacement and live Windows preference observation.
/// System resolution is presentation-only and never persists the derived effective theme.
/// </summary>
internal sealed class DesktopThemeService : IDisposable
{
    private const string WindowsThemeRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    private readonly System.Windows.Application application;
    private readonly DesktopUiPreferenceStore preferences;
    private readonly DispatcherTimer systemObserver;
    private ResourceDictionary? activeDictionary;
    private bool lastHighContrast;

    /// <summary>Initialises a theme owner for the application and shared preference store.</summary>
    /// <param name="application">Application whose active semantic dictionary is replaced.</param>
    /// <param name="preferences">Shared validated UI preference store.</param>
    public DesktopThemeService(System.Windows.Application application, DesktopUiPreferenceStore preferences)
    {
        this.application = application;
        this.preferences = preferences;
        systemObserver = new DispatcherTimer(DispatcherPriority.Background, application.Dispatcher)
        {
            Interval = TimeSpan.FromSeconds(2),
        };
        systemObserver.Tick += SystemObserverTick;
    }

    /// <summary>Signals a user preference or effective system-theme change.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>Gets the selected theme source rather than its derived effective value.</summary>
    public ThemePreference CurrentPreference { get; private set; } = ThemePreference.System;

    /// <summary>Gets the effective generated token set currently applied beneath any High Contrast override.</summary>
    public EffectiveTheme EffectiveTheme { get; private set; } = EffectiveTheme.Light;

    /// <summary>Applies the stored preference and starts bounded observation of Windows theme changes.</summary>
    public void Initialise()
    {
        CurrentPreference = preferences.Current.Theme;
        SystemParameters.StaticPropertyChanged += SystemParametersChanged;
        systemObserver.Start();
        Apply(force: true);
    }

    /// <summary>Applies and persists an explicit System, Light or Dark preference.</summary>
    /// <param name="preference">Validated preference selected in the topbar.</param>
    public void SetPreference(ThemePreference preference)
    {
        if (preference == CurrentPreference)
        {
            return;
        }
        CurrentPreference = preference;
        preferences.UpdateTheme(preference);
        Apply(force: true);
    }

    /// <summary>Stops system observation and releases event ownership.</summary>
    public void Dispose()
    {
        systemObserver.Stop();
        systemObserver.Tick -= SystemObserverTick;
        SystemParameters.StaticPropertyChanged -= SystemParametersChanged;
    }

    /// <summary>Re-evaluates High Contrast immediately when Windows accessibility state changes.</summary>
    private void SystemParametersChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.HighContrast))
        {
            Apply(force: true);
        }
    }

    /// <summary>Polls the lightweight current-user colour preference only while System can affect the result.</summary>
    private void SystemObserverTick(object? sender, EventArgs e)
    {
        if (CurrentPreference == ThemePreference.System || SystemParameters.HighContrast != lastHighContrast)
        {
            Apply(force: false);
        }
    }

    /// <summary>Atomically replaces only the semantic theme dictionary and preserves localisation/core resources.</summary>
    private void Apply(bool force)
    {
        bool highContrast = SystemParameters.HighContrast;
        EffectiveTheme resolved = ThemePreferenceContract.Resolve(CurrentPreference, ReadSystemUsesDark());
        if (!force && resolved == EffectiveTheme && highContrast == lastHighContrast)
        {
            return;
        }

        ResourceDictionary replacement = highContrast
            ? CreateHighContrastDictionary()
            : new ResourceDictionary
            {
                Source = new Uri($"Generated/DesignTokens.{resolved}.xaml", UriKind.Relative),
            };
        int index = FindThemeDictionary();
        if (index >= 0)
        {
            application.Resources.MergedDictionaries[index] = replacement;
        }
        else
        {
            application.Resources.MergedDictionaries.Insert(1, replacement);
        }

        activeDictionary = replacement;
        EffectiveTheme = resolved;
        lastHighContrast = highContrast;
        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Finds either the initial generated theme dictionary or the current runtime replacement.</summary>
    private int FindThemeDictionary()
    {
        for (int index = 0; index < application.Resources.MergedDictionaries.Count; index++)
        {
            ResourceDictionary dictionary = application.Resources.MergedDictionaries[index];
            string source = dictionary.Source?.OriginalString ?? string.Empty;
            if (ReferenceEquals(dictionary, activeDictionary) || source.Contains("Generated/DesignTokens.Light", StringComparison.Ordinal) || source.Contains("Generated/DesignTokens.Dark", StringComparison.Ordinal))
            {
                return index;
            }
        }
        return -1;
    }

    /// <summary>Reads the Windows application colour preference and fails safely to Light when unavailable.</summary>
    private static bool ReadSystemUsesDark()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(WindowsThemeRegistryPath, writable: false);
            return key?.GetValue("AppsUseLightTheme") is int value && value == 0;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            return false;
        }
    }

    /// <summary>Maps semantic resources to live Windows system brushes while High Contrast takes precedence.</summary>
    private static ResourceDictionary CreateHighContrastDictionary()
    {
        ResourceDictionary resources = [];
        Add(resources, WpfSystemColors.WindowBrush,
            "ColourCanvasBrush", "ColourSurfaceDefaultBrush", "ColourSurfaceRaisedBrush", "ColourSurfaceSubtleBrush", "ColourSurfaceSunkenBrush",
            "ComponentAppBackgroundBrush", "ComponentCardBackgroundBrush", "ComponentButtonSecondaryBackgroundBrush", "ComponentInputBackgroundBrush");
        Add(resources, WpfSystemColors.WindowTextBrush,
            "ColourTextPrimaryBrush", "ColourTextSecondaryBrush", "ColourTextMutedBrush", "ComponentButtonSecondaryForegroundBrush", "ComponentInputForegroundBrush");
        Add(resources, WpfSystemColors.ControlBrush, "ColourSurfaceInverseBrush");
        Add(resources, WpfSystemColors.ControlTextBrush, "ColourTextInverseBrush");
        Add(resources, WpfSystemColors.ActiveBorderBrush, "ColourBorderDefaultBrush", "ColourBorderStrongBrush", "ComponentCardBorderBrush", "ComponentButtonSecondaryBorderBrush", "ComponentInputBorderBrush");
        Add(resources, WpfSystemColors.HighlightBrush, "ColourSelectionBackgroundBrush", "ColourActionPrimaryBackgroundBrush", "ColourActionPrimaryHoverBrush", "ColourActionPrimaryActiveBrush", "ComponentButtonPrimaryBackgroundDefaultBrush", "ComponentButtonPrimaryBackgroundHoverBrush", "ComponentButtonPrimaryBackgroundActiveBrush");
        Add(resources, WpfSystemColors.HighlightTextBrush, "ColourSelectionForegroundBrush", "ColourActionPrimaryForegroundBrush", "ComponentButtonPrimaryForegroundBrush");
        Add(resources, WpfSystemColors.HotTrackBrush, "ColourFocusRingBrush", "ComponentFocusRingBrush");
        Add(resources, WpfSystemColors.WindowBrush,
            "ColourStatusCriticalBackgroundBrush", "ColourStatusDegradedBackgroundBrush", "ColourStatusHealthyBackgroundBrush", "ColourStatusInformationBackgroundBrush", "ColourStatusMaintenanceBackgroundBrush", "ColourStatusNeutralBackgroundBrush",
            "ComponentStatusCriticalBackgroundBrush", "ComponentStatusDegradedBackgroundBrush", "ComponentStatusHealthyBackgroundBrush", "ComponentStatusInformationBackgroundBrush", "ComponentStatusMaintenanceBackgroundBrush", "ComponentStatusNeutralBackgroundBrush");
        Add(resources, WpfSystemColors.WindowTextBrush,
            "ColourStatusCriticalForegroundBrush", "ColourStatusDegradedForegroundBrush", "ColourStatusHealthyForegroundBrush", "ColourStatusInformationForegroundBrush", "ColourStatusMaintenanceForegroundBrush", "ColourStatusNeutralForegroundBrush",
            "ComponentStatusCriticalForegroundBrush", "ComponentStatusDegradedForegroundBrush", "ComponentStatusHealthyForegroundBrush", "ComponentStatusInformationForegroundBrush", "ComponentStatusMaintenanceForegroundBrush", "ComponentStatusNeutralForegroundBrush");
        return resources;
    }

    /// <summary>Adds one Windows system brush under every supplied semantic resource key.</summary>
    private static void Add(ResourceDictionary resources, System.Windows.Media.Brush brush, params string[] keys)
    {
        foreach (string key in keys)
        {
            resources[key] = brush;
        }
    }
}
