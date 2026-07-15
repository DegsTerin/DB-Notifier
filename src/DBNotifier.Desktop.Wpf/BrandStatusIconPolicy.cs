// Module purpose: Maps provider-neutral fleet state to the shared transparent product-mark assets used by WPF and Windows notification surfaces.
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DBNotifier.Application.Presentation;
using Forms = System.Windows.Forms;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Provides one fail-safe semantic icon mapping for the WPF header, window/taskbar, flyout and NotifyIcon.
/// Static executable and installer surfaces use the neutral Unknown asset because they cannot observe fleet state.
/// </summary>
internal static class BrandStatusIconPolicy
{
    private const int WmSetIcon = 0x0080;
    private static readonly IntPtr IconSmall = IntPtr.Zero;
    private static readonly IntPtr IconBig = new(1);
    private static readonly IntPtr IconSmall2 = new(2);

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

    /// <summary>Loads the exact or next-larger native ICO frame for a logical WPF size at the current DPI.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <param name="targetDipSize">Requested square display size in device-independent pixels.</param>
    /// <param name="dpi">Current visual DPI scale used to derive the physical target size.</param>
    /// <returns>A frozen image source decoded from the smallest packaged frame that does not require upscaling, or the largest fallback.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested size or DPI scale is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the selected icon resource or its frames are unavailable.</exception>
    internal static ImageSource LoadImageSource(TrayAggregateState state, double targetDipSize, DpiScale dpi)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetDipSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpi.DpiScaleX);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dpi.DpiScaleY);
        int targetPixelSize = (int)Math.Ceiling(targetDipSize * Math.Max(dpi.DpiScaleX, dpi.DpiScaleY));
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(PackUri(state))
            ?? throw new InvalidOperationException("The selected packaged DB Notifier status icon resource is unavailable.");
        using Stream stream = resource.Stream;
        IconBitmapDecoder decoder = new(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        BitmapFrame frame = decoder.Frames
            .Where(candidate => candidate.PixelWidth >= targetPixelSize && candidate.PixelHeight >= targetPixelSize)
            .OrderBy(candidate => candidate.PixelWidth)
            .FirstOrDefault()
            ?? decoder.Frames.OrderByDescending(candidate => candidate.PixelWidth).FirstOrDefault()
            ?? throw new InvalidOperationException("The selected packaged DB Notifier status icon contains no decodable frame.");
        frame.Freeze();
        return frame;
    }

    /// <summary>Loads an independent disposable Windows icon at the exact platform-requested shell size.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <param name="targetPixelSize">Requested square icon size in physical pixels.</param>
    /// <returns>An icon clone that remains valid after the packaged resource stream closes.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the requested size is not positive.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the selected packaged status icon is unavailable.</exception>
    internal static Icon LoadWindowsIcon(TrayAggregateState state, int targetPixelSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetPixelSize);
        System.Windows.Resources.StreamResourceInfo resource = System.Windows.Application.GetResourceStream(PackUri(state))
            ?? throw new InvalidOperationException("The selected packaged DB Notifier status icon resource is unavailable.");
        using Stream stream = resource.Stream;
        using Icon source = new(stream, targetPixelSize, targetPixelSize);
        return (Icon)source.Clone();
    }

    /// <summary>Assigns separate native small and large icons so Windows does not resample one WPF bitmap for both roles.</summary>
    /// <param name="window">Initialised WPF window whose native handle owns the icons.</param>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>A lease that keeps both native icon handles alive for the lifetime of the window.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the WPF window handle is unavailable.</exception>
    internal static IDisposable ApplyNativeWindowIcons(Window window, TrayAggregateState state)
    {
        ArgumentNullException.ThrowIfNull(window);
        IntPtr windowHandle = new WindowInteropHelper(window).Handle;
        if (windowHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException("The WPF window handle is unavailable for native icon assignment.");
        }

        System.Drawing.Size smallSize = Forms.SystemInformation.SmallIconSize;
        System.Drawing.Size largeSize = Forms.SystemInformation.IconSize;
        Icon smallIcon = LoadWindowsIcon(state, smallSize.Width);
        Icon largeIcon = LoadWindowsIcon(state, largeSize.Width);
        SendMessage(windowHandle, WmSetIcon, IconSmall, smallIcon.Handle);
        SendMessage(windowHandle, WmSetIcon, IconSmall2, smallIcon.Handle);
        SendMessage(windowHandle, WmSetIcon, IconBig, largeIcon.Handle);
        return new WindowIconLease(smallIcon, largeIcon);
    }

    /// <summary>Builds the absolute WPF pack URI shared by both icon decoders.</summary>
    /// <param name="state">Provider-neutral aggregate state.</param>
    /// <returns>An absolute pack URI for the selected packaged asset.</returns>
    private static Uri PackUri(TrayAggregateState state) =>
        new($"pack://application:,,,/Assets/{AssetName(state)}", UriKind.Absolute);

    /// <summary>Sends a synchronous native window message to assign a size-specific icon handle.</summary>
    /// <param name="windowHandle">Owning native WPF window handle.</param>
    /// <param name="message">Windows message identifier.</param>
    /// <param name="parameter">Icon role requested by <c>WM_SETICON</c>.</param>
    /// <param name="iconHandle">Native icon handle that remains owned by the returned lease.</param>
    /// <returns>The previously associated icon handle, which remains owned by its original source.</returns>
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr windowHandle, int message, IntPtr parameter, IntPtr iconHandle);

    /// <summary>Keeps native window icons alive and releases their handles after the owning window closes.</summary>
    private sealed class WindowIconLease(Icon smallIcon, Icon largeIcon) : IDisposable
    {
        /// <summary>Releases only the two icons created for the owning DB Notifier window.</summary>
        public void Dispose()
        {
            smallIcon.Dispose();
            largeIcon.Dispose();
        }
    }
}
