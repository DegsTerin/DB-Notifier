// Module purpose: Loads local provider-logo resources for one WPF identity view and fails safely to a neutral database glyph.
using System.Collections;
using System.IO;
using System.Resources;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media.Imaging;
using System.Windows.Resources;

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

    /// <summary>Identifies whether this composite also renders provider text beside its decorative artwork.</summary>
    public static readonly DependencyProperty ShowTextProperty = DependencyProperty.Register(
        nameof(ShowText),
        typeof(bool),
        typeof(ProviderIdentityView),
        new PropertyMetadata(true, ShowTextChanged));

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

    /// <summary>
    /// Gets or sets whether this composite renders the provider identifier itself.
    /// An owner may hide it only when the same authoritative identifier remains visibly adjacent.
    /// </summary>
    public bool ShowText
    {
        get => (bool)GetValue(ShowTextProperty);
        set => SetValue(ShowTextProperty, value);
    }

    /// <summary>Gets the visibility used by the provider text binding.</summary>
    public Visibility TextVisibility => ShowText ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>Re-applies visible identity when WPF replaces the bound dependency-property value.</summary>
    /// <param name="dependencyObject">Provider identity view whose binding changed.</param>
    /// <param name="e">Old and new dependency-property values.</param>
    private static void IdentityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((ProviderIdentityView)dependencyObject).ApplyIdentity();

    /// <summary>Updates the text host when an owning row switches between composite and icon-only layout.</summary>
    /// <param name="dependencyObject">Provider identity view whose text mode changed.</param>
    /// <param name="e">Dependency-property event metadata.</param>
    private static void ShowTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((ProviderIdentityView)dependencyObject).ProviderText.Visibility =
            ((ProviderIdentityView)dependencyObject).TextVisibility;

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
            Stream? stream = OpenApplicationResource(identity.AssetUri);
            if (stream is null)
            {
                return;
            }

            using (stream)
            {
                BitmapImage source = new();
                source.BeginInit();
                source.CacheOption = BitmapCacheOption.OnLoad;
                source.StreamSource = stream;
                source.EndInit();
                source.Freeze();
                ProviderImage.Source = source;
                ProviderImage.Visibility = Visibility.Visible;
                FallbackGlyph.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException or ArgumentException or FormatException)
        {
            UseNeutralFallback();
        }
    }

    /// <summary>
    /// Resolves one generated, application-local resource path through the URI forms accepted by supported WPF hosts.
    /// No candidate can address the file system or a network origin.
    /// </summary>
    /// <param name="assetUri">Relative generated resource path owned by the desktop assembly.</param>
    /// <returns>The first matching compiled resource stream, or null when every local form is unavailable.</returns>
    private static Stream? OpenApplicationResource(Uri assetUri)
    {
        string relativePath = assetUri.OriginalString.TrimStart('/');
        Uri[] candidates =
        [
            assetUri,
            new Uri($"pack://application:,,,/{relativePath}", UriKind.Absolute),
            new Uri($"pack://application:,,,/DBNotifier.Desktop.Wpf;component/{relativePath}", UriKind.Absolute),
        ];

        foreach (Uri candidate in candidates)
        {
            try
            {
                StreamResourceInfo? resource = System.Windows.Application.GetResourceStream(candidate);
                if (resource is not null)
                {
                    return resource.Stream;
                }
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or NotSupportedException or ArgumentException)
            {
                // A host-specific URI form may fail; the remaining application-local forms are still safe to try.
            }
        }

        string? manifestName = Array.Find(
            typeof(ProviderIdentityView).Assembly.GetManifestResourceNames(),
            name => name.EndsWith(".g.resources", StringComparison.Ordinal));
        if (manifestName is null)
        {
            return null;
        }

        using Stream? manifest = typeof(ProviderIdentityView).Assembly.GetManifestResourceStream(manifestName);
        if (manifest is null)
        {
            return null;
        }

        using ResourceReader reader = new(manifest);
        IDictionaryEnumerator entries = reader.GetEnumerator();
        while (entries.MoveNext())
        {
            if (!string.Equals(entries.Key as string, relativePath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            object? value = entries.Value;
            if (value is byte[] bytes)
            {
                return new MemoryStream(bytes, writable: false);
            }

            if (value is Stream embeddedStream)
            {
                MemoryStream copy = new();
                embeddedStream.CopyTo(copy);
                copy.Position = 0;
                return copy;
            }

            return null;
        }

        return null;
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
