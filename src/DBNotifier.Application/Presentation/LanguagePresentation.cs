// Module purpose: Defines the platform-neutral interface-language preference contract shared by React and WPF adapters.
namespace DBNotifier.Application.Presentation;

/// <summary>
/// Identifies an interface locale homologated for DB-Notifier presentation adapters.
/// </summary>
public enum InterfaceLanguage
{
    /// <summary>Uses Brazilian Portuguese interface resources.</summary>
    BrazilianPortuguese,

    /// <summary>Uses British English interface resources.</summary>
    BritishEnglish
}

/// <summary>
/// Provides stable culture and persistence values for interface-language adapters.
/// </summary>
public static class LanguagePreferenceContract
{
    /// <summary>Gets the Dashboard local-storage key for the interface language.</summary>
    public const string WebStorageKey = "dbnotifier.language.preference.v1";

    /// <summary>Gets the default interface language used after invalid or unavailable persistence.</summary>
    public const InterfaceLanguage Default = InterfaceLanguage.BrazilianPortuguese;

    /// <summary>
    /// Parses a supported BCP 47 value without accepting arbitrary runtime cultures.
    /// </summary>
    /// <param name="value">The untrusted persisted value.</param>
    /// <param name="language">The parsed language, or Brazilian Portuguese when parsing fails.</param>
    /// <returns><see langword="true"/> only for an explicitly supported locale.</returns>
    public static bool TryParse(string? value, out InterfaceLanguage language)
    {
        language = value switch
        {
            "en-GB" => InterfaceLanguage.BritishEnglish,
            _ => InterfaceLanguage.BrazilianPortuguese
        };
        return value is "pt-BR" or "en-GB";
    }

    /// <summary>
    /// Converts a supported language to its stable BCP 47 storage value.
    /// </summary>
    /// <param name="language">The validated interface language.</param>
    /// <returns>The exact supported culture name.</returns>
    public static string ToStorageValue(InterfaceLanguage language) => language switch
    {
        InterfaceLanguage.BritishEnglish => "en-GB",
        _ => "pt-BR"
    };
}
