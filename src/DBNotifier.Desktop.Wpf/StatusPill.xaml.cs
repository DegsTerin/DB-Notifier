// Module purpose: Maps canonical presentation tones to the shared WPF status-pill resources without inferring health.
using System.Collections.Generic;
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

    /// <summary>Creates one text-semantic peer whose bounds represent the complete pill rather than only its inner label.</summary>
    /// <returns>A peer that exposes the localised state once and retains the control's arranged rectangle for accessibility auditing.</returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new StatusPillAutomationPeer(this);

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
        StatusIcon.Kind = IsStatusIcon(Icon) ? Icon : SemanticIconKind.Unknown;
        AutomationProperties.SetName(this, Text);
    }

    /// <summary>Accepts only icon meanings that can represent a factual status and fails closed for all other enum values.</summary>
    /// <param name="icon">Requested code-native icon supplied by the presentation model.</param>
    /// <returns>True when the icon belongs to the bounded status vocabulary; otherwise false.</returns>
    private static bool IsStatusIcon(SemanticIconKind icon) => icon is
        SemanticIconKind.Healthy or
        SemanticIconKind.Degraded or
        SemanticIconKind.Critical or
        SemanticIconKind.Disabled or
        SemanticIconKind.Unknown or
        SemanticIconKind.Stale or
        SemanticIconKind.Notification or
        SemanticIconKind.Settings;

    /// <summary>Exposes one non-interactive status and the bounds of its complete code-native pill without duplicating child text.</summary>
    private sealed class StatusPillAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initialises the peer for one arranged status-pill owner.</summary>
        /// <param name="owner">Read-only status pill represented by this peer.</param>
        public StatusPillAutomationPeer(StatusPill owner)
            : base(owner)
        {
        }

        /// <summary>Classifies the read-only pill as text for assistive technology.</summary>
        /// <returns>The standard text automation control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        /// <summary>Provides a stable code-native class name for diagnostic automation.</summary>
        /// <returns>The status-pill component name.</returns>
        protected override string GetClassNameCore() => nameof(StatusPill);

        /// <summary>Returns the complete localised factual state owned by the pill.</summary>
        /// <returns>The visible status text, or an empty string before binding completes.</returns>
        protected override string GetNameCore() => ((StatusPill)Owner).Text ?? string.Empty;

        /// <summary>Connects the raw-only label peer so geometric audits can prove that rendered text stays inside the pill.</summary>
        /// <returns>The visible label peer when WPF can create it; otherwise no diagnostic children.</returns>
        protected override List<AutomationPeer> GetChildrenCore()
        {
            AutomationPeer? textPeer = UIElementAutomationPeer.CreatePeerForElement(((StatusPill)Owner).StatusText);
            return textPeer is null ? [] : [textPeer];
        }
    }
}

/// <summary>Renders the visible status label while exposing its geometry only in the raw automation tree.</summary>
internal sealed class StatusPillTextBlock : TextBlock
{
    /// <summary>Creates the raw-only peer used to compare the visible label with its owning pill bounds.</summary>
    /// <returns>A non-control, non-content text peer that does not duplicate screen-reader output.</returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new StatusPillTextAutomationPeer(this);

    /// <summary>Keeps label geometry available to diagnostic automation without adding a second readable content node.</summary>
    private sealed class StatusPillTextAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initialises the peer for one visible label.</summary>
        /// <param name="owner">Status text whose arranged rectangle is audited.</param>
        public StatusPillTextAutomationPeer(StatusPillTextBlock owner)
            : base(owner)
        {
        }

        /// <summary>Classifies the raw diagnostic element as text.</summary>
        /// <returns>The standard text automation control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Text;

        /// <summary>Excludes the duplicate label from the control view used by assistive technology.</summary>
        /// <returns>False because the owning StatusPill is the single readable control.</returns>
        protected override bool IsControlElementCore() => false;

        /// <summary>Excludes the duplicate label from the content view used by assistive technology.</summary>
        /// <returns>False because the owning StatusPill already exposes the complete state.</returns>
        protected override bool IsContentElementCore() => false;

        /// <summary>Provides the same visible localised label to raw diagnostic automation.</summary>
        /// <returns>The rendered text, or an empty string before binding completes.</returns>
        protected override string GetNameCore() => ((StatusPillTextBlock)Owner).Text ?? string.Empty;

        /// <summary>Provides a stable identifier used only to compare raw label and container geometry.</summary>
        /// <returns>The fixed status-label diagnostic identifier.</returns>
        protected override string GetAutomationIdCore() => "StatusPillText";
    }
}
