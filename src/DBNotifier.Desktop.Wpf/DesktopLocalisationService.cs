// Module purpose: Applies the shared interface-language contract to WPF resources and persists only a non-secret locale preference.
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns desktop language selection, generated dictionary replacement and resilient local preference persistence.
/// It accepts only the locales declared by <see cref="LanguagePreferenceContract"/>.
/// </summary>
internal sealed class DesktopLocalisationService
{
    private const string SchemaVersion = "dbnotifier.ui-preferences.v1";
    private const long MaximumPreferenceBytes = 16 * 1024;
    private readonly System.Windows.Application application;
    private readonly string preferencePath;

    /// <summary>Initialises a service for the running WPF application.</summary>
    /// <param name="application">Application whose merged localisation dictionary is replaced.</param>
    public DesktopLocalisationService(System.Windows.Application application)
    {
        this.application = application;
        preferencePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DB-Notifier",
            "ui-preferences.v1.json");
    }

    /// <summary>Signals that all dynamic localisation resources have changed.</summary>
    public event EventHandler? LanguageChanged;

    /// <summary>Gets the active supported interface language.</summary>
    public InterfaceLanguage CurrentLanguage { get; private set; } = LanguagePreferenceContract.Default;

    /// <summary>Gets the culture used for presentation-only dates and numbers.</summary>
    public CultureInfo Culture => CultureInfo.GetCultureInfo(LanguagePreferenceContract.ToStorageValue(CurrentLanguage));

    /// <summary>Loads the persisted preference and applies it, failing safely to pt-BR for invalid or unavailable data.</summary>
    public void Initialise() => Apply(Load(), persist: false);

    /// <summary>Applies and persists a validated interface language.</summary>
    /// <param name="language">Supported language selected by the user.</param>
    public void SetLanguage(InterfaceLanguage language) => Apply(language, persist: true);

    /// <summary>Resolves and formats a generated message resource.</summary>
    /// <param name="key">Canonical localisation key.</param>
    /// <param name="values">Optional positional formatting values.</param>
    /// <returns>The localised and culture-formatted message.</returns>
    /// <exception cref="InvalidOperationException">Thrown when generated resources do not contain the requested key.</exception>
    public string Text(string key, params object[] values)
    {
        if (application.TryFindResource(key) is not string pattern)
        {
            throw new InvalidOperationException($"Missing generated localisation resource: {key}");
        }

        return values.Length == 0 ? pattern : string.Format(Culture, pattern, values);
    }

    /// <summary>Replaces the current generated dictionary and optionally persists the safe preference.</summary>
    private void Apply(InterfaceLanguage language, bool persist)
    {
        string locale = LanguagePreferenceContract.ToStorageValue(language);
        ResourceDictionary replacement = new()
        {
            Source = new Uri($"Generated/Localisation.{locale}.xaml", UriKind.Relative),
        };
        int index = FindLocalisationDictionary();
        if (index >= 0)
        {
            application.Resources.MergedDictionaries[index] = replacement;
        }
        else
        {
            application.Resources.MergedDictionaries.Insert(0, replacement);
        }

        CurrentLanguage = language;
        CultureInfo.CurrentUICulture = Culture;
        if (persist)
        {
            Save(locale);
        }
        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Finds the single generated localisation resource without disturbing theme dictionaries.</summary>
    private int FindLocalisationDictionary()
    {
        for (int index = 0; index < application.Resources.MergedDictionaries.Count; index++)
        {
            string source = application.Resources.MergedDictionaries[index].Source?.OriginalString ?? string.Empty;
            if (source.Contains("Generated/Localisation.", StringComparison.Ordinal))
            {
                return index;
            }
        }
        return -1;
    }

    /// <summary>Loads a bounded, non-secret JSON preference and rejects malformed schemas or locales.</summary>
    private InterfaceLanguage Load()
    {
        try
        {
            FileInfo file = new(preferencePath);
            if (!file.Exists || file.Length > MaximumPreferenceBytes)
            {
                return LanguagePreferenceContract.Default;
            }
            DesktopUiPreference? preference = JsonSerializer.Deserialize<DesktopUiPreference>(File.ReadAllText(preferencePath));
            return preference?.SchemaVersion == SchemaVersion &&
                LanguagePreferenceContract.TryParse(preference.Language, out InterfaceLanguage language)
                    ? language
                    : LanguagePreferenceContract.Default;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return LanguagePreferenceContract.Default;
        }
    }

    /// <summary>Writes the preference atomically; storage policy failures do not interrupt the interface.</summary>
    private void Save(string locale)
    {
        string? temporaryPath = null;
        try
        {
            string directory = Path.GetDirectoryName(preferencePath)!;
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, $"ui-preferences.{Guid.NewGuid():N}.tmp");
            string json = JsonSerializer.Serialize(new DesktopUiPreference(SchemaVersion, locale));
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, preferencePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // The selected language remains active for this session when persistence is unavailable.
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>Represents the versioned, non-secret preference persisted for the desktop shell.</summary>
    private sealed record DesktopUiPreference(string SchemaVersion, string Language);
}
