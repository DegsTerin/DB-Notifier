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
    /// Confirms explicit preferences override the platform while System follows it.
    /// </summary>
    /// <param name="preference">Validated stored preference.</param>
    /// <param name="systemUsesDark">Current platform colour request.</param>
    /// <param name="expected">Expected effective semantic theme.</param>
    [Theory]
    [InlineData(ThemePreference.System, false, EffectiveTheme.Light)]
    [InlineData(ThemePreference.System, true, EffectiveTheme.Dark)]
    [InlineData(ThemePreference.Light, true, EffectiveTheme.Light)]
    [InlineData(ThemePreference.Dark, false, EffectiveTheme.Dark)]
    public void ThemeResolutionPreservesExplicitPreference(
        ThemePreference preference,
        bool systemUsesDark,
        EffectiveTheme expected)
    {
        Assert.Equal(expected, ThemePreferenceContract.Resolve(preference, systemUsesDark));
    }

    /// <summary>
    /// Confirms recognised storage values parse and serialise without changing meaning.
    /// </summary>
    /// <param name="stored">Stable lower-case persisted value.</param>
    /// <param name="expected">Expected parsed preference.</param>
    [Theory]
    [InlineData("system", ThemePreference.System)]
    [InlineData("light", ThemePreference.Light)]
    [InlineData("dark", ThemePreference.Dark)]
    public void ThemeStorageValuesRoundTrip(string stored, ThemePreference expected)
    {
        Assert.True(ThemePreferenceContract.TryParse(stored, out ThemePreference parsed));
        Assert.Equal(expected, parsed);
        Assert.Equal(stored, ThemePreferenceContract.ToStorageValue(parsed));
    }

    /// <summary>
    /// Confirms invalid persisted data fails safely to the System preference.
    /// </summary>
    [Fact]
    public void InvalidThemeStorageFailsSafelyToSystem()
    {
        Assert.False(ThemePreferenceContract.TryParse("unexpected", out ThemePreference parsed));
        Assert.Equal(ThemePreference.System, parsed);
    }
}
