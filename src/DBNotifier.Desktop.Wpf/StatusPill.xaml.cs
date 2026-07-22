// Module purpose: Maps canonical presentation tones to the shared WPF status-pill resources without inferring health.
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Identifies provider-neutral presentation tones used by status pills and semantic icon badges.</summary>
internal enum OperationalTone
{
    Neutral,
    Healthy,
    Degraded,
    Critical,
    Information,
    Maintenance,
}

/// <summary>
/// Presents text, code-native icon geometry and a complete boundary for one already-classified operational state.
/// It never derives health, support or permission from colour.
/// </summary>
internal sealed partial class StatusPill : System.Windows.Controls.UserControl
{
    /// <summary>Identifies the visible localised text dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(StatusPill),
        new PropertyMetadata(string.Empty, PresentationChanged));

    /// <summary>Identifies the semantic presentation-tone dependency property.</summary>
    public static readonly DependencyProperty ToneProperty = DependencyProperty.Register(
        nameof(Tone),
        typeof(OperationalTone),
        typeof(StatusPill),
        new PropertyMetadata(OperationalTone.Neutral, PresentationChanged));

    /// <summary>Identifies the explicit semantic icon dependency property kept separate from colour tone.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(SemanticIconKind),
        typeof(StatusPill),
        new PropertyMetadata(SemanticIconKind.Unknown, PresentationChanged));

    /// <summary>Initialises a neutral pill before its owning row supplies factual text and tone.</summary>
    public StatusPill()
    {
        InitializeComponent();
        ApplyPresentation();
    }

    /// <summary>Gets or sets the visible localised state text.</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets the semantic tone selected by the presentation model.</summary>
    public OperationalTone Tone
    {
        get => (OperationalTone)GetValue(ToneProperty);
        set => SetValue(ToneProperty, value);
    }

    /// <summary>Gets or sets the explicit non-colour semantic icon for the factual state.</summary>
    public SemanticIconKind Icon
    {
        get => (SemanticIconKind)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Suppresses a redundant control peer while retaining the visible text in the containing row.</summary>
    /// <returns>Always null because this read-only pill is represented by adjacent row semantics.</returns>
    protected override AutomationPeer? OnCreateAutomationPeer() => null;

    /// <summary>Re-applies semantic resources when either visible text or tone changes.</summary>
    /// <param name="dependencyObject">Status pill whose presentation changed.</param>
    /// <param name="e">Dependency-property event metadata.</param>
    private static void PresentationChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((StatusPill)dependencyObject).ApplyPresentation();

    /// <summary>Maps one pre-classified tone to existing generated resources and code-native icon geometry.</summary>
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
        PillBorder.SetResourceReference(BackgroundProperty, background);
        PillBorder.SetResourceReference(Border.BorderBrushProperty, foreground);
        StatusIcon.Kind = Icon;
        AutomationProperties.SetName(this, Text);
    }
}
