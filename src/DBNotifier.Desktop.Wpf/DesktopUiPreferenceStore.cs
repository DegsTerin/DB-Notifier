// Module purpose: Persists only validated, non-secret WPF language and theme preferences through one atomic versioned file.
using System.IO;
using System.Text.Json;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Owns the shared current-user UI preference document so language and theme updates cannot erase each other.
/// Invalid, oversized or inaccessible data fails safely to the platform-neutral defaults.
/// </summary>
internal sealed class DesktopUiPreferenceStore
{
    private const long MaximumPreferenceBytes = 16 * 1024;
    private readonly string preferencePath;

    /// <summary>Initialises the store and loads its bounded preference document.</summary>
    public DesktopUiPreferenceStore()
    {
        preferencePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DB-Notifier",
            ThemePreferenceContract.WpfPreferencesFileName);
        Current = Load();
    }

    /// <summary>Gets the last validated in-memory preference pair.</summary>
    public DesktopUiPreferences Current { get; private set; }

    /// <summary>Updates the language while preserving the selected theme.</summary>
    /// <param name="language">Validated supported interface language.</param>
    public void UpdateLanguage(InterfaceLanguage language) => Save(Current with { Language = language });

    /// <summary>Updates the theme while preserving the selected language.</summary>
    /// <param name="theme">Validated theme preference rather than its effective system result.</param>
    public void UpdateTheme(ThemePreference theme) => Save(Current with { Theme = theme });

    /// <summary>Loads a bounded document and accepts missing fields only as their safe defaults.</summary>
    private DesktopUiPreferences Load()
    {
        try
        {
            FileInfo file = new(preferencePath);
            if (!file.Exists || file.Length > MaximumPreferenceBytes)
            {
                return DesktopUiPreferences.Default;
            }

            PersistedUiPreferences? persisted = JsonSerializer.Deserialize<PersistedUiPreferences>(File.ReadAllText(preferencePath));
            if (persisted?.SchemaVersion != ThemePreferenceContract.SchemaVersion)
            {
                return DesktopUiPreferences.Default;
            }

            InterfaceLanguage language = LanguagePreferenceContract.TryParse(persisted.Language, out InterfaceLanguage parsedLanguage)
                ? parsedLanguage
                : LanguagePreferenceContract.Default;
            ThemePreference theme = ThemePreferenceContract.TryParse(persisted.Theme, out ThemePreference parsedTheme)
                ? parsedTheme
                : ThemePreference.System;
            return new DesktopUiPreferences(language, theme);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            return DesktopUiPreferences.Default;
        }
    }

    /// <summary>Atomically writes both preferences; a storage failure leaves the session value active in memory.</summary>
    private void Save(DesktopUiPreferences preferences)
    {
        Current = preferences;
        string? temporaryPath = null;
        try
        {
            string directory = Path.GetDirectoryName(preferencePath)!;
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, $"ui-preferences.{Guid.NewGuid():N}.tmp");
            string json = JsonSerializer.Serialize(new PersistedUiPreferences(
                ThemePreferenceContract.SchemaVersion,
                LanguagePreferenceContract.ToStorageValue(preferences.Language),
                ThemePreferenceContract.ToStorageValue(preferences.Theme)));
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, preferencePath, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Presentation remains active for this session when local preference persistence is unavailable.
        }
        finally
        {
            if (temporaryPath is not null)
            {
                try { File.Delete(temporaryPath); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    /// <summary>Represents the strict JSON envelope while allowing safe migration from a previous locale-only file.</summary>
    private sealed record PersistedUiPreferences(string SchemaVersion, string? Language, string? Theme);
}

/// <summary>Represents validated language and theme choices used by desktop presentation services.</summary>
internal sealed record DesktopUiPreferences(InterfaceLanguage Language, ThemePreference Theme)
{
    /// <summary>Gets the safe defaults for a new or invalid preference document.</summary>
    public static DesktopUiPreferences Default { get; } = new(LanguagePreferenceContract.Default, ThemePreference.System);
}
