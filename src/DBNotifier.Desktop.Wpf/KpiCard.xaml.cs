// Module purpose: Applies shared semantic resources and accessible text to the reusable WPF KPI card.
using System.Windows;
using System.Windows.Automation;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents one factual metric using the same label, value, optional context and semantic icon hierarchy as the Dashboard.
/// The owning view supplies already-classified meaning; this control does not infer operational health.
/// </summary>
internal sealed partial class KpiCard : System.Windows.Controls.UserControl
{
    /// <summary>Identifies the localised metric-label dependency property.</summary>
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty, PresentationChanged));

    /// <summary>Identifies the formatted metric-value dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty, PresentationChanged));

    /// <summary>Identifies the optional factual supporting-text dependency property.</summary>
    public static readonly DependencyProperty DetailProperty = DependencyProperty.Register(
        nameof(Detail), typeof(string), typeof(KpiCard), new PropertyMetadata(string.Empty, PresentationChanged));

    /// <summary>Identifies the semantic presentation-tone dependency property.</summary>
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
        nameof(Tone), typeof(OperationalTone), typeof(KpiCard), new PropertyMetadata(OperationalTone.Neutral, PresentationChanged));

    /// <summary>Identifies the code-native icon dependency property.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(SemanticIconKind), typeof(KpiCard), new PropertyMetadata(SemanticIconKind.Database, PresentationChanged));

    /// <summary>Initialises a neutral empty metric card before its owning view supplies presentation values.</summary>
    public KpiCard()
    {
        InitializeComponent();
        ApplyPresentation();
    }

    /// <summary>Gets or sets the localised metric label.</summary>
    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Gets or sets the already-formatted metric value.</summary>
    public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Gets or sets optional factual context rendered beneath the value.</summary>
    public string Detail { get => (string)GetValue(DetailProperty); set => SetValue(DetailProperty, value); }

    /// <summary>Gets or sets the pre-classified semantic tone.</summary>
    public OperationalTone Tone { get => (OperationalTone)GetValue(ToneProperty); set => SetValue(ToneProperty, value); }

    /// <summary>Gets or sets the code-native semantic icon.</summary>
    public SemanticIconKind Icon { get => (SemanticIconKind)GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>Gets the visibility of optional supporting text.</summary>
    public Visibility DetailVisibility => string.IsNullOrWhiteSpace(Detail) ? Visibility.Collapsed : Visibility.Visible;

    /// <summary>Re-applies generated resources and accessible text when any card input changes.</summary>
    /// <param name="dependencyObject">Metric card whose presentation changed.</param>
    /// <param name="e">Dependency-property event metadata.</param>
    private static void PresentationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((KpiCard)dependencyObject).ApplyPresentation();

    /// <summary>Maps one pre-classified tone to existing generated resources without relying on colour alone.</summary>
    private void ApplyPresentation()
    {
        (string foreground, string background) = Tone switch
        {
            OperationalTone.Healthy => ("ComponentStatusHealthyForegroundBrush", "ComponentStatusHealthyBackgroundBrush"),
            OperationalTone.Degraded => ("ComponentStatusDegradedForegroundBrush", "ComponentStatusDegradedBackgroundBrush"),
            OperationalTone.Critical => ("ComponentStatusCriticalForegroundBrush", "ComponentStatusCriticalBackgroundBrush"),
            OperationalTone.Information => ("ComponentStatusInformationForegroundBrush", "ComponentStatusInformationBackgroundBrush"),
            OperationalTone.Maintenance => ("ComponentStatusMaintenanceForegroundBrush", "ComponentStatusMaintenanceBackgroundBrush"),
            _ => ("ComponentStatusNeutralForegroundBrush", "ComponentStatusNeutralBackgroundBrush"),
        };

        SetResourceReference(ForegroundProperty, foreground);
        IconBadge.SetResourceReference(BackgroundProperty, background);
        MetricIcon.Kind = Icon;
        DetailText.Visibility = DetailVisibility;
        AutomationProperties.SetName(this, string.Join(". ", new[] { Label, Value, Detail }.Where(value => !string.IsNullOrWhiteSpace(value))));
    }
}
