// Module purpose: Resolves vendored provider-logo resources for WPF presentation without changing provider contracts or support truth.
using System.Collections.ObjectModel;
using DBNotifier.Application.Presentation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Describes one provider identity for a decorative logo beside its authoritative stable identifier.
/// A null asset URI requires the neutral database glyph and never implies missing provider support.
/// </summary>
/// <param name="ProviderType">Stable provider identifier retained as visible text.</param>
/// <param name="AssetUri">Theme-specific vendored WPF resource, or null when the neutral fallback is required.</param>
internal sealed record ProviderVisualIdentity(string ProviderType, Uri? AssetUri);

/// <summary>
/// Maps stable provider identifiers to local Light and Dark PNG resources at the WPF presentation boundary.
/// Unknown identifiers, High Contrast and asset-loading failures remain provider-neutral and never alter health or homologation state.
/// </summary>
internal sealed class ProviderVisualIdentityPolicy : IDisposable
{
    private const string PackRoot = "pack://application:,,,/DBNotifier.Desktop.Wpf;component/";
    private static readonly ReadOnlyDictionary<string, ProviderVisualAsset> Assets =
        new ReadOnlyDictionary<string, ProviderVisualAsset>(
            new Dictionary<string, ProviderVisualAsset>(StringComparer.Ordinal)
            {
                ["cassandra"] = ProviderVisualAsset.Create("cassandra"),
                ["dynamodb"] = ProviderVisualAsset.Create("dynamodb"),
                ["elasticsearch"] = ProviderVisualAsset.Create("elasticsearch"),
                ["firebase"] = ProviderVisualAsset.Create("firebase"),
                ["mongodb"] = ProviderVisualAsset.Create("mongodb"),
                ["mysql"] = ProviderVisualAsset.Create("mysql"),
                ["planetscale"] = ProviderVisualAsset.Create("planetscale"),
                ["postgresql"] = ProviderVisualAsset.Create("postgresql"),
                ["redis"] = ProviderVisualAsset.Create("redis"),
                ["sqlite"] = ProviderVisualAsset.Create("sqlite"),
                ["supabase"] = ProviderVisualAsset.Create("supabase"),
            });

    private readonly DesktopThemeService theme;
    private bool disposed;

    /// <summary>Initialises an identity resolver that follows the existing effective WPF theme and High Contrast owner.</summary>
    /// <param name="theme">Application theme service; no independent theme state is created.</param>
    internal ProviderVisualIdentityPolicy(DesktopThemeService theme)
    {
        this.theme = theme ?? throw new ArgumentNullException(nameof(theme));
        theme.ThemeChanged += ThemeChanged;
    }

    /// <summary>Signals that visible provider identities must be re-resolved after theme or High Contrast changes.</summary>
    internal event EventHandler? VisualIdentityChanged;

    /// <summary>Resolves one stable provider identifier to a vendored theme asset or the neutral fallback.</summary>
    /// <param name="providerType">Stable provider identifier supplied by presentation evidence.</param>
    /// <returns>An identity retaining visible text and, when safe and known, a local pack URI.</returns>
    internal ProviderVisualIdentity Resolve(string? providerType)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        string visibleProviderType = providerType ?? "unknown";
        if (providerType is null ||
            theme.IsHighContrastActive ||
            !Assets.TryGetValue(providerType, out ProviderVisualAsset? asset))
        {
            return new ProviderVisualIdentity(visibleProviderType, null);
        }

        string resourcePath = theme.EffectiveTheme == EffectiveTheme.Dark
            ? asset.DarkResourcePath
            : asset.LightResourcePath;
        return new ProviderVisualIdentity(visibleProviderType, new Uri(PackRoot + resourcePath, UriKind.Absolute));
    }

    /// <summary>Stops theme observation; no image, provider or external resource is disposed by this policy.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        theme.ThemeChanged -= ThemeChanged;
        VisualIdentityChanged = null;
    }

    /// <summary>Forwards effective presentation changes without interpreting provider health or capability.</summary>
    /// <param name="sender">Existing desktop theme service.</param>
    /// <param name="e">Theme-change event data.</param>
    private void ThemeChanged(object? sender, EventArgs e) => VisualIdentityChanged?.Invoke(this, EventArgs.Empty);

    /// <summary>Contains the local Light and Dark resource paths for one declared provider identifier.</summary>
    /// <param name="LightResourcePath">Light-theme PNG resource path.</param>
    /// <param name="DarkResourcePath">Dark-theme PNG resource path.</param>
    private sealed record ProviderVisualAsset(string LightResourcePath, string DarkResourcePath)
    {
        /// <summary>Creates the two required resource paths without adding engine-specific presentation logic.</summary>
        /// <param name="providerType">Stable identifier used as the vendored file stem.</param>
        /// <returns>A pair of application-relative PNG resource paths.</returns>
        internal static ProviderVisualAsset Create(string providerType) => new(
            $"Assets/ProviderIcons/{providerType}-light.png",
            $"Assets/ProviderIcons/{providerType}-dark.png");
    }
}
