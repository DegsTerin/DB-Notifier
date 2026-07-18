// Module purpose: Loads local provider-logo resources for one WPF identity view and fails safely to a neutral database glyph.
using System.IO;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media.Imaging;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Hosts decorative provider artwork without creating a WPF Automation peer.
/// The adjacent stable identifier remains the sole authoritative accessible provider label.
/// </summary>
internal sealed class DecorativeProviderImage : System.Windows.Controls.Image
{
    /// <summary>Suppresses a UI Automation element for artwork that conveys no information beyond adjacent text.</summary>
    /// <returns>Always null because the image is deliberately decorative.</returns>
    protected override AutomationPeer? OnCreateAutomationPeer() => null;
}

/// <summary>
/// Presents a decorative vendored provider image beside the stable provider identifier.
/// Missing, unreadable or unsupported resources never remove the provider text and fall back to neutral geometry.
/// </summary>
internal sealed partial class ProviderIdentityView : System.Windows.Controls.UserControl
{
    /// <summary>Identifies the provider-identity dependency property used by WPF templates.</summary>
    public static readonly DependencyProperty IdentityProperty = DependencyProperty.Register(
        nameof(Identity),
        typeof(ProviderVisualIdentity),
        typeof(ProviderIdentityView),
        new PropertyMetadata(null, IdentityChanged));

    /// <summary>Identifies the positive square icon-size dependency property.</summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize),
        typeof(double),
        typeof(ProviderIdentityView),
        new PropertyMetadata(20d),
        value => value is double size && double.IsFinite(size) && size > 0);

    /// <summary>Initialises the reusable provider identity presentation.</summary>
    public ProviderIdentityView()
    {
        InitializeComponent();
        ApplyIdentity();
    }

    /// <summary>Gets or sets the resolved provider identity supplied by the owning WPF view model.</summary>
    public ProviderVisualIdentity? Identity
    {
        get => (ProviderVisualIdentity?)GetValue(IdentityProperty);
        set => SetValue(IdentityProperty, value);
    }

    /// <summary>Gets or sets the square logo size in device-independent pixels.</summary>
    public double IconSize
    {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>Re-applies visible identity when WPF replaces the bound dependency-property value.</summary>
    /// <param name="dependencyObject">Provider identity view whose binding changed.</param>
    /// <param name="e">Old and new dependency-property values.</param>
    private static void IdentityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((ProviderIdentityView)dependencyObject).ApplyIdentity();

    /// <summary>Loads a local image eagerly so resource errors can return to the neutral fallback deterministically.</summary>
    private void ApplyIdentity()
    {
        ProviderVisualIdentity? identity = Identity;
        ProviderText.Text = identity?.ProviderType ?? "unknown";
        UseNeutralFallback();
        if (identity?.AssetUri is null)
        {
            return;
        }

        try
        {
            BitmapImage source = new();
            source.BeginInit();
            source.CacheOption = BitmapCacheOption.OnLoad;
            source.UriSource = identity.AssetUri;
            source.EndInit();
            source.Freeze();
            ProviderImage.Source = source;
            ProviderImage.Visibility = Visibility.Visible;
            FallbackGlyph.Visibility = Visibility.Collapsed;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException or ArgumentException)
        {
            UseNeutralFallback();
        }
    }

    /// <summary>Restores neutral geometry if WPF reports a deferred decoder or resource failure.</summary>
    /// <param name="sender">Decorative provider image that failed to load.</param>
    /// <param name="e">Sanitised framework image-failure event; its exception is not exposed.</param>
    private void ProviderImageFailed(object sender, ExceptionRoutedEventArgs e) => UseNeutralFallback();

    /// <summary>Clears any failed bitmap while retaining the visible provider identifier.</summary>
    private void UseNeutralFallback()
    {
        ProviderImage.Source = null;
        ProviderImage.Visibility = Visibility.Collapsed;
        FallbackGlyph.Visibility = Visibility.Visible;
    }
}
