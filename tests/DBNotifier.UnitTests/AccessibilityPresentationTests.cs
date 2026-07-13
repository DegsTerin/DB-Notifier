// Module purpose: Verifies Accessibility Presentation Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Presentation;

namespace DBNotifier.UnitTests;

/// <summary>
/// Verifies presentation accessibility and cross-platform theme contracts without starting either UI runtime.
/// </summary>
public sealed class AccessibilityPresentationTests
{
    /// <summary>
    /// Confirms that representative WPF text pairs meet WCAG AA normal-text contrast.
    /// </summary>
    /// <param name="foreground">Foreground colour in hexadecimal notation.</param>
    /// <param name="background">Background colour in hexadecimal notation.</param>
    [Theory]
    [InlineData("#17202A", "#F5F7FA")]
    [InlineData("#526273", "#FFFFFF")]
    [InlineData("#0B5CAD", "#FFFFFF")]
    [InlineData("#17633A", "#FFFFFF")]
    [InlineData("#805300", "#FFFFFF")]
    [InlineData("#982B2B", "#FFFFFF")]
    public void WpfTextPaletteMeetsWcagAaContrast(string foreground, string background)
    {
        Assert.True(
            AccessibilityPresentation.ContrastRatio(foreground, background) >= 4.5,
            $"{foreground} on {background} must meet WCAG AA normal-text contrast.");
    }

    /// <summary>
    /// Confirms explicit preferences resolve to their matching semantic themes.
    /// </summary>
    /// <param name="preference">Validated stored preference.</param>
    /// <param name="expected">Expected effective semantic theme.</param>
    [Theory]
    [InlineData(ThemePreference.Light, EffectiveTheme.Light)]
    [InlineData(ThemePreference.Dark, EffectiveTheme.Dark)]
    public void ThemeResolutionPreservesExplicitPreference(
        ThemePreference preference,
        EffectiveTheme expected)
    {
        Assert.Equal(expected, ThemePreferenceContract.Resolve(preference));
    }

    /// <summary>
    /// Confirms recognised storage values parse and serialise without changing meaning.
    /// </summary>
    /// <param name="stored">Stable lower-case persisted value.</param>
    /// <param name="expected">Expected parsed preference.</param>
    [Theory]
    [InlineData("light", ThemePreference.Light)]
    [InlineData("dark", ThemePreference.Dark)]
    public void ThemeStorageValuesRoundTrip(string stored, ThemePreference expected)
    {
        Assert.True(ThemePreferenceContract.TryParse(stored, out ThemePreference parsed));
        Assert.Equal(expected, parsed);
        Assert.Equal(stored, ThemePreferenceContract.ToStorageValue(parsed));
    }

    /// <summary>
    /// Confirms invalid and retired persisted data fails safely to the Light preference.
    /// </summary>
    [Fact]
    public void InvalidThemeStorageFailsSafelyToLight()
    {
        Assert.False(ThemePreferenceContract.TryParse("unexpected", out ThemePreference parsed));
        Assert.Equal(ThemePreference.Light, parsed);
        Assert.False(ThemePreferenceContract.TryParse("system", out ThemePreference retired));
        Assert.Equal(ThemePreference.Light, retired);
    }

    /// <summary>
    /// Confirms supported interface locales round-trip through their exact BCP 47 values.
    /// </summary>
    /// <param name="stored">Supported persisted locale.</param>
    /// <param name="expected">Expected platform-neutral language.</param>
    [Theory]
    [InlineData("pt-BR", InterfaceLanguage.BrazilianPortuguese)]
    [InlineData("en-GB", InterfaceLanguage.BritishEnglish)]
    public void LanguageStorageValuesRoundTrip(string stored, InterfaceLanguage expected)
    {
        Assert.True(LanguagePreferenceContract.TryParse(stored, out InterfaceLanguage parsed));
        Assert.Equal(expected, parsed);
        Assert.Equal(stored, LanguagePreferenceContract.ToStorageValue(parsed));
    }

    /// <summary>
    /// Confirms an unsupported locale fails safely to the Brazilian Portuguese baseline.
    /// </summary>
    [Fact]
    public void InvalidLanguageStorageFailsSafelyToBrazilianPortuguese()
    {
        Assert.False(LanguagePreferenceContract.TryParse("en-US", out InterfaceLanguage parsed));
        Assert.Equal(InterfaceLanguage.BrazilianPortuguese, parsed);
    }
}
