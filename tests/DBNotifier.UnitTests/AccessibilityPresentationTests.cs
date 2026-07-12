// Module purpose: Verifies Accessibility Presentation Tests behaviour and protects the documented project contract.
using DBNotifier.Application.Presentation;

namespace DBNotifier.UnitTests;

public sealed class AccessibilityPresentationTests
{
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
}
