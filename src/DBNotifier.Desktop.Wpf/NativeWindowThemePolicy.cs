// Module purpose: Synchronises native Windows caption chrome with the explicit WPF theme while preserving system High Contrast ownership.
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using DBNotifier.Application.Presentation;
using WpfColor = System.Windows.Media.Color;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Applies semantic DB-Notifier caption and text colours to the DWM-managed title bar without replacing native window behaviour.
/// Unsupported DWM attributes degrade safely to the Windows default caption.
/// </summary>
internal static class NativeWindowThemePolicy
{
    private const uint DwmColourDefault = 0xFFFFFFFF;

    /// <summary>
    /// Applies the current explicit theme to an initialised WPF window handle or restores system ownership in High Contrast.
    /// </summary>
    /// <param name="window">Initialised WPF window whose native caption is updated.</param>
    /// <param name="theme">Theme owner supplying the effective Light, Dark and High Contrast state.</param>
    internal static void Apply(Window window, DesktopThemeService theme)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(theme);

        nint windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == nint.Zero)
        {
            return;
        }

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        try
        {
            int immersiveDarkMode = !theme.IsHighContrastActive && theme.EffectiveTheme == EffectiveTheme.Dark ? 1 : 0;
            _ = DwmSetWindowAttribute(windowHandle, DwmWindowAttribute.UseImmersiveDarkMode, ref immersiveDarkMode, sizeof(int));

            if (theme.IsHighContrastActive)
            {
                ResetNativeColours(windowHandle);
                return;
            }

            ApplySemanticColour(windowHandle, DwmWindowAttribute.CaptionColour, window, "ComponentAppBackgroundBrush");
            ApplySemanticColour(windowHandle, DwmWindowAttribute.TextColour, window, "ColourTextPrimaryBrush");
        }
        catch (DllNotFoundException)
        {
            // DWM is unavailable only on an unsupported Windows environment; the native caption remains usable.
        }
        catch (EntryPointNotFoundException)
        {
            // Earlier Windows implementations may not expose this entry point; the native caption remains usable.
        }
    }

    /// <summary>Restores DWM defaults so Windows system colours retain authority during High Contrast.</summary>
    /// <param name="windowHandle">Initialised native window handle.</param>
    private static void ResetNativeColours(nint windowHandle)
    {
        uint colour = DwmColourDefault;
        _ = DwmSetWindowColourAttribute(windowHandle, DwmWindowAttribute.CaptionColour, ref colour, sizeof(uint));
        _ = DwmSetWindowColourAttribute(windowHandle, DwmWindowAttribute.TextColour, ref colour, sizeof(uint));
    }

    /// <summary>Resolves one semantic brush and forwards its opaque colour to the requested DWM attribute.</summary>
    /// <param name="windowHandle">Initialised native window handle.</param>
    /// <param name="attribute">Caption or text colour attribute to update.</param>
    /// <param name="window">WPF resource owner used for dynamic theme lookup.</param>
    /// <param name="resourceKey">Canonical semantic brush key.</param>
    private static void ApplySemanticColour(nint windowHandle, DwmWindowAttribute attribute, Window window, string resourceKey)
    {
        uint colour = window.TryFindResource(resourceKey) is SolidColorBrush { Opacity: 1d } brush && brush.Color.A == byte.MaxValue
            ? ToColourRef(brush.Color)
            : DwmColourDefault;
        _ = DwmSetWindowColourAttribute(windowHandle, attribute, ref colour, sizeof(uint));
    }

    /// <summary>Converts a WPF colour to the Win32 <c>COLORREF</c> byte order.</summary>
    /// <param name="colour">Opaque semantic WPF colour.</param>
    /// <returns>A Win32 red/green/blue colour reference.</returns>
    private static uint ToColourRef(WpfColor colour) =>
        colour.R | ((uint)colour.G << 8) | ((uint)colour.B << 16);

    /// <summary>Sets a signed DWM attribute such as the immersive-dark-mode flag.</summary>
    /// <param name="windowHandle">Initialised native window handle.</param>
    /// <param name="attribute">DWM attribute identifier.</param>
    /// <param name="value">Signed attribute value.</param>
    /// <param name="valueSize">Native value size in bytes.</param>
    /// <returns>An HRESULT; unsupported attributes are deliberately non-fatal.</returns>
    [DllImport("dwmapi.dll", PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int DwmSetWindowAttribute(nint windowHandle, DwmWindowAttribute attribute, ref int value, int valueSize);

    /// <summary>Sets an unsigned DWM colour attribute.</summary>
    /// <param name="windowHandle">Initialised native window handle.</param>
    /// <param name="attribute">DWM colour attribute identifier.</param>
    /// <param name="value">Win32 colour reference or <see cref="DwmColourDefault"/>.</param>
    /// <param name="valueSize">Native value size in bytes.</param>
    /// <returns>An HRESULT; unsupported attributes are deliberately non-fatal.</returns>
    [DllImport("dwmapi.dll", EntryPoint = "DwmSetWindowAttribute", PreserveSig = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern int DwmSetWindowColourAttribute(nint windowHandle, DwmWindowAttribute attribute, ref uint value, int valueSize);

    /// <summary>Lists the documented DWM attributes used by the native theme adapter.</summary>
    private enum DwmWindowAttribute
    {
        UseImmersiveDarkMode = 20,
        CaptionColour = 35,
        TextColour = 36,
    }
}
