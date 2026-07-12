// Module purpose: Defines platform-neutral theme preference and resolution contracts for React and WPF presentation adapters.
namespace DBNotifier.Application.Presentation;

/// <summary>
/// Identifies the user-selected source of the visual theme without storing an effective system result.
/// </summary>
public enum ThemePreference
{
    /// <summary>Follows the active operating-system colour preference.</summary>
    System,

    /// <summary>Always uses the Light semantic token set.</summary>
    Light,

    /// <summary>Always uses the Dark semantic token set.</summary>
    Dark
}

/// <summary>
/// Identifies the resolved semantic token set applied by a presentation adapter.
/// </summary>
public enum EffectiveTheme
{
    /// <summary>Applies the Light semantic token set.</summary>
    Light,

    /// <summary>Applies the Dark semantic token set.</summary>
    Dark
}

/// <summary>
/// Provides the stable persistence and resolution contract shared by frontend platform adapters.
/// </summary>
public static class ThemePreferenceContract
{
    /// <summary>Gets the schema identifier stored with versioned UI preferences.</summary>
    public const string SchemaVersion = "dbnotifier.ui-preferences.v1";

    /// <summary>Gets the Dashboard local-storage key for the preference value.</summary>
    public const string WebStorageKey = "dbnotifier.theme.preference.v1";

    /// <summary>Gets the WPF current-user preference file name.</summary>
    public const string WpfPreferencesFileName = "ui-preferences.v1.json";

    /// <summary>
    /// Resolves an explicit or system-derived preference to a concrete theme.
    /// </summary>
    /// <param name="preference">The validated user preference.</param>
    /// <param name="systemUsesDark">Whether the platform currently requests a dark colour scheme.</param>
    /// <returns>The effective Light or Dark theme.</returns>
    public static EffectiveTheme Resolve(ThemePreference preference, bool systemUsesDark) => preference switch
    {
        ThemePreference.Light => EffectiveTheme.Light,
        ThemePreference.Dark => EffectiveTheme.Dark,
        _ => systemUsesDark ? EffectiveTheme.Dark : EffectiveTheme.Light
    };

    /// <summary>
    /// Parses the stable lower-case storage representation without accepting arbitrary enum names.
    /// </summary>
    /// <param name="value">The untrusted persisted value.</param>
    /// <param name="preference">The parsed preference, or System when parsing fails.</param>
    /// <returns><see langword="true"/> only when the persisted value is recognised.</returns>
    public static bool TryParse(string? value, out ThemePreference preference)
    {
        preference = value switch
        {
            "system" => ThemePreference.System,
            "light" => ThemePreference.Light,
            "dark" => ThemePreference.Dark,
            _ => ThemePreference.System
        };
        return value is "system" or "light" or "dark";
    }

    /// <summary>
    /// Converts a validated preference to its stable lower-case storage value.
    /// </summary>
    /// <param name="preference">The preference to serialise.</param>
    /// <returns>The schema-compatible storage value.</returns>
    public static string ToStorageValue(ThemePreference preference) => preference switch
    {
        ThemePreference.Light => "light",
        ThemePreference.Dark => "dark",
        _ => "system"
    };
}
