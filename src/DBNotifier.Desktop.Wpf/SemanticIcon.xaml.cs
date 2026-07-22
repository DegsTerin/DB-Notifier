// Module purpose: Selects provider-neutral vector geometry for shared WPF status, metric and navigation meanings.
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Media;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Identifies the bounded semantic icon set shared with the Dashboard presentation contract.</summary>
internal enum SemanticIconKind
{
    Database,
    Healthy,
    Degraded,
    Critical,
    Disabled,
    Unknown,
    Stale,
    Notification,
    Restart,
    Settings,
    Providers,
    Performance,
}

/// <summary>
/// Renders decorative outlined geometry without depending on an installed symbol font or an external icon package.
/// Adjacent visible text remains authoritative, so the icon deliberately creates no automation peer.
/// </summary>
internal sealed partial class SemanticIcon : System.Windows.Controls.UserControl
{
    /// <summary>Identifies the semantic-kind dependency property used by WPF templates.</summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind),
        typeof(SemanticIconKind),
        typeof(SemanticIcon),
        new PropertyMetadata(SemanticIconKind.Database, KindChanged));

    private static readonly Dictionary<SemanticIconKind, Geometry> Geometries =
        new Dictionary<SemanticIconKind, Geometry>
        {
            [SemanticIconKind.Database] = Parse("M4.5,5 C4.5,3.3 7.9,2 12,2 C16.1,2 19.5,3.3 19.5,5 C19.5,6.7 16.1,8 12,8 C7.9,8 4.5,6.7 4.5,5 M4.5,5 V12 C4.5,13.7 7.9,15 12,15 C16.1,15 19.5,13.7 19.5,12 V5 M4.5,12 V19 C4.5,20.7 7.9,22 12,22 C16.1,22 19.5,20.7 19.5,19 V12"),
            [SemanticIconKind.Healthy] = Parse("M20,12 A8,8 0 1 1 4,12 A8,8 0 1 1 20,12 M8.5,12 L10.75,14.25 L15.75,9"),
            [SemanticIconKind.Degraded] = Parse("M12,3.5 L21,19 H3 Z M12,9 V13 M12,16.25 L12.01,16.25"),
            [SemanticIconKind.Critical] = Parse("M20,12 A8,8 0 1 1 4,12 A8,8 0 1 1 20,12 M12,7.75 V13.25 M12,16.5 L12.01,16.5"),
            [SemanticIconKind.Disabled] = Parse("M20,12 A8,8 0 1 1 4,12 A8,8 0 1 1 20,12 M6.35,6.35 L17.65,17.65"),
            [SemanticIconKind.Unknown] = Parse("M20,12 A8,8 0 1 1 4,12 A8,8 0 1 1 20,12 M9.5,9 A2.7,2.7 0 1 1 13.8,11.2 C12.6,11.8 12,12.5 12,14 M12,17 L12.01,17"),
            [SemanticIconKind.Stale] = Parse("M20,12 A8,8 0 1 1 4,12 A8,8 0 1 1 20,12 M12,7.5 V12 L15,14"),
            [SemanticIconKind.Notification] = Parse("M6,17 H18 L16.6,15 V10 A4.6,4.6 0 0 0 7.4,10 V15 Z M10,20 H14"),
            [SemanticIconKind.Restart] = Parse("M19,8 V3 L17,5 A8,8 0 1 0 19.2,13 M19,3 H14"),
            [SemanticIconKind.Settings] = Parse("M15,12 A3,3 0 1 1 9,12 A3,3 0 1 1 15,12 M12,2 V5 M12,19 V22 M2,12 H5 M19,12 H22 M5,5 L7,7 M17,17 L19,19 M19,5 L17,7 M7,17 L5,19"),
            [SemanticIconKind.Providers] = Parse("M11,8 A3,3 0 1 1 5,8 A3,3 0 1 1 11,8 M19,7 A2,2 0 1 1 15,7 A2,2 0 1 1 19,7 M19,17 A3,3 0 1 1 13,17 A3,3 0 1 1 19,17 M10.5,9.5 L14,8 M9.5,10.5 L14,15 M17,9 V14"),
            [SemanticIconKind.Performance] = Parse("M3,4 H21 V20 H3 Z M6,15 L9,12 L12,14 L17,8 L19,10"),
        };

    /// <summary>Initialises the decorative icon and its default geometry.</summary>
    public SemanticIcon()
    {
        InitializeComponent();
        ApplyKind();
    }

    /// <summary>Gets or sets the provider-neutral semantic meaning rendered by the icon.</summary>
    public SemanticIconKind Kind
    {
        get => (SemanticIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Gets the frozen vector geometry selected for the current semantic kind.</summary>
    public Geometry IconGeometry => Geometries[Kind];

    /// <summary>Suppresses an automation node because visible adjacent text carries the complete meaning.</summary>
    /// <returns>Always null for this decorative element.</returns>
    protected override AutomationPeer? OnCreateAutomationPeer() => null;

    /// <summary>Replaces the selected geometry when a template changes semantic meaning.</summary>
    /// <param name="dependencyObject">Icon whose semantic kind changed.</param>
    /// <param name="e">Dependency-property event metadata.</param>
    private static void KindChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((SemanticIcon)dependencyObject).ApplyKind();

    /// <summary>Notifies the existing binding after selecting a validated geometry.</summary>
    private void ApplyKind() => IconPath.Data = Geometries[Kind];

    /// <summary>Parses and freezes one trusted code-native path so repeated icons share immutable geometry.</summary>
    /// <param name="path">Trusted path data expressed on the shared 24-unit icon canvas.</param>
    /// <returns>Frozen WPF geometry safe for reuse across themes and DPI changes.</returns>
    private static Geometry Parse(string path)
    {
        Geometry geometry = Geometry.Parse(path);
        geometry.Freeze();
        return geometry;
    }
}
