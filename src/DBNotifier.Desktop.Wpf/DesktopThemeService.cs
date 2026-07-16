// Module purpose: Resolves and applies explicit Light, Dark and High Contrast WPF resource dictionaries without altering feature state.
using System.Windows;
using DBNotifier.Application.Presentation;
using WpfSystemColors = System.Windows.SystemColors;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the explicit desktop theme preference, generated dictionary replacement and High Contrast observation.
/// </summary>
internal sealed class DesktopThemeService : IDisposable
{
    private readonly System.Windows.Application application;
    private readonly DesktopUiPreferenceStore preferences;
    private ResourceDictionary? activeDictionary;
    private bool lastHighContrast;

    /// <summary>Initialises a theme owner for the application and shared preference store.</summary>
    /// <param name="application">Application whose active semantic dictionary is replaced.</param>
    /// <param name="preferences">Shared validated UI preference store.</param>
    public DesktopThemeService(System.Windows.Application application, DesktopUiPreferenceStore preferences)
    {
        this.application = application;
        this.preferences = preferences;
    }

    /// <summary>Signals an explicit user preference or effective High Contrast change.</summary>
    public event EventHandler? ThemeChanged;

    /// <summary>Gets the selected explicit theme preference.</summary>
    public ThemePreference CurrentPreference { get; private set; } = ThemePreference.Light;

    /// <summary>Gets the effective generated token set currently applied beneath any High Contrast override.</summary>
    public EffectiveTheme EffectiveTheme { get; private set; } = EffectiveTheme.Light;

    /// <summary>Gets whether Windows High Contrast currently overrides the explicit DB-Notifier theme.</summary>
    public bool IsHighContrastActive => lastHighContrast;

    /// <summary>Applies the stored preference and observes Windows High Contrast changes.</summary>
    public void Initialise()
    {
        CurrentPreference = preferences.Current.Theme;
        SystemParameters.StaticPropertyChanged += SystemParametersChanged;
        Apply(force: true);
    }

    /// <summary>Applies and persists an explicit Light or Dark preference.</summary>
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

    /// <summary>Stops High Contrast observation and releases event ownership.</summary>
    public void Dispose()
    {
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

    /// <summary>Atomically replaces only the semantic theme dictionary and preserves localisation/core resources.</summary>
    private void Apply(bool force)
    {
        bool highContrast = SystemParameters.HighContrast;
        EffectiveTheme resolved = ThemePreferenceContract.Resolve(CurrentPreference);
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

    /// <summary>Maps semantic resources to live Windows system brushes while High Contrast takes precedence.</summary>
    private static ResourceDictionary CreateHighContrastDictionary()
    {
        ResourceDictionary resources = [];
        Add(resources, WpfSystemColors.WindowBrush,
            "ColourCanvasBrush", "ColourSurfaceDefaultBrush", "ColourSurfaceRaisedBrush", "ColourSurfaceSubtleBrush", "ColourSurfaceSunkenBrush",
            "ColourChromeBackgroundBrush", "ColourChromeSurfaceBrush",
            "ComponentAppBackgroundBrush", "ComponentCardBackgroundBrush", "ComponentButtonSecondaryBackgroundBrush", "ComponentInputBackgroundBrush",
            "ComponentShellChromeBackgroundBrush", "ComponentShellChromeSurfaceBrush");
        Add(resources, WpfSystemColors.WindowTextBrush,
            "ColourTextPrimaryBrush", "ColourTextSecondaryBrush", "ColourTextMutedBrush", "ColourChromeForegroundBrush", "ColourChromeMutedBrush",
            "ColourDataCategory1Brush", "ColourDataCategory2Brush", "ColourDataCategory3Brush", "ColourDataCategory4Brush", "ColourDataCategory5Brush",
            "ComponentBrandWordmarkAccentBrush", "ComponentButtonSecondaryForegroundBrush", "ComponentInputForegroundBrush", "ComponentShellChromeForegroundBrush", "ComponentShellChromeMutedBrush");
        Add(resources, WpfSystemColors.ControlBrush, "ColourSurfaceInverseBrush");
        Add(resources, WpfSystemColors.ControlTextBrush, "ColourTextInverseBrush");
        Add(resources, WpfSystemColors.ActiveBorderBrush, "ColourBorderDefaultBrush", "ColourBorderStrongBrush", "ColourChromeBorderBrush", "ComponentCardBorderBrush", "ComponentButtonSecondaryBorderBrush", "ComponentInputBorderBrush", "ComponentShellChromeBorderBrush");
        Add(resources, WpfSystemColors.HighlightBrush, "ColourSelectionBackgroundBrush", "ColourChromeSelectedBackgroundBrush", "ColourActionPrimaryBackgroundBrush", "ColourActionPrimaryHoverBrush", "ColourActionPrimaryActiveBrush", "ComponentButtonPrimaryBackgroundDefaultBrush", "ComponentButtonPrimaryBackgroundHoverBrush", "ComponentButtonPrimaryBackgroundActiveBrush", "ComponentShellChromeSelectedBackgroundBrush");
        Add(resources, WpfSystemColors.HighlightTextBrush, "ColourSelectionForegroundBrush", "ColourChromeSelectedForegroundBrush", "ColourActionPrimaryForegroundBrush", "ComponentButtonPrimaryForegroundBrush", "ComponentShellChromeSelectedForegroundBrush");
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
