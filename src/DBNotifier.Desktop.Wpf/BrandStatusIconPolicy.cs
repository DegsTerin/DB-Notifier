// Module purpose: Maps provider-neutral fleet state to the shared transparent product-mark assets used by WPF and Windows notification surfaces.
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Provides one fail-safe semantic icon mapping for the WPF header, window/taskbar, flyout and NotifyIcon.
/// Static executable and installer surfaces use the neutral Unknown asset because they cannot observe fleet state.
/// </summary>
internal static class BrandStatusIconPolicy
{
    /// <summary>Returns the packaged icon asset name for a canonical aggregate state.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>The matching Healthy, Warning, Critical or Unknown ICO file name.</returns>
    internal static string AssetName(TrayAggregateState state) => state switch
    {
        TrayAggregateState.Healthy => "DBNotifier.Healthy.ico",
        TrayAggregateState.Warning => "DBNotifier.Warning.ico",
        TrayAggregateState.Critical => "DBNotifier.Critical.ico",
        TrayAggregateState.Unknown => "DBNotifier.Unknown.ico",
        _ => "DBNotifier.Unknown.ico",
    };

    /// <summary>Loads a frozen WPF image source for window, taskbar and in-app product-mark presentation.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>A shareable image source decoded from the packaged semantic ICO.</returns>
    internal static ImageSource LoadImageSource(TrayAggregateState state)
    {
        BitmapImage image = new();
        image.BeginInit();
        image.UriSource = PackUri(state);
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        image.Freeze();
        return image;
    }

    /// <summary>Loads an independent disposable Windows icon for the notification-area API.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>An icon clone that remains valid after the packaged resource stream closes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the selected packaged status icon is unavailable.</exception>
    internal static Icon LoadWindowsIcon(TrayAggregateState state)
    {
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(PackUri(state))
            ?? throw new InvalidOperationException("The selected packaged DB Notifier status icon resource is unavailable.");
        using Stream stream = resource.Stream;
        using Icon source = new(stream);
        return (Icon)source.Clone();
    }

    /// <summary>Builds the absolute WPF pack URI shared by both icon decoders.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>An absolute pack URI for the selected packaged asset.</returns>
    private static Uri PackUri(TrayAggregateState state) =>
        new($"pack://application:,,,/Assets/{AssetName(state)}", UriKind.Absolute);
}
