// Module purpose: Draws a bounded provider-neutral distribution ring from already-presented WPF provider counts.
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace DBNotifier.Desktop.Wpf;

/// <summary>Represents one visible provider count without implying health, implementation or homologation.</summary>
/// <param name="ProviderIdentity">Resolved decorative identity plus the authoritative stable provider identifier.</param>
/// <param name="Count">Non-negative count represented by the categorical ring and visible legend.</param>
/// <param name="SupportLabel">Existing factual support declaration used by the provider catalogue.</param>
internal sealed record ProviderDistributionItem(ProviderVisualIdentity ProviderIdentity, int Count, string SupportLabel);

/// <summary>Hosts the decorative provider ring while exposing its arranged rectangle only to raw diagnostic automation.</summary>
internal sealed class DistributionRingViewbox : Viewbox
{
    /// <summary>Creates the raw-only peer used to prove that the complete ring stays inside its owning card.</summary>
    /// <returns>A diagnostic peer excluded from assistive-technology control and content views.</returns>
    protected override AutomationPeer OnCreateAutomationPeer() => new DistributionRingViewboxAutomationPeer(this);

    /// <summary>Keeps ring geometry measurable without duplicating the authoritative textual provider legend.</summary>
    private sealed class DistributionRingViewboxAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initialises a diagnostic peer for one arranged ring viewport.</summary>
        /// <param name="owner">Ring viewport whose bounds are audited.</param>
        public DistributionRingViewboxAutomationPeer(DistributionRingViewbox owner)
            : base(owner)
        {
        }

        /// <summary>Provides the stable identifier consumed by the bounded WPF auditor.</summary>
        /// <returns>The canonical ring-viewport automation identifier.</returns>
        protected override string GetAutomationIdCore() => "ProviderDistributionRingViewport";

        /// <summary>Classifies the diagnostic geometry as a non-interactive group.</summary>
        /// <returns>The standard group automation control type.</returns>
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        /// <summary>Excludes decorative geometry from the control view.</summary>
        /// <returns>False because the visible provider legend carries the accessible information.</returns>
        protected override bool IsControlElementCore() => false;

        /// <summary>Excludes decorative geometry from the content view.</summary>
        /// <returns>False because the visible provider legend carries the accessible information.</returns>
        protected override bool IsContentElementCore() => false;
    }
}

/// <summary>
/// Presents a code-native categorical ring and a visible provider/count legend.
/// Segment colours are ordinal product slots and never provider, support or health identities.
/// </summary>
internal sealed partial class ProviderDistributionChart : System.Windows.Controls.UserControl
{
    /// <summary>Identifies the immutable visible provider collection dependency property.</summary>
    public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
        nameof(Items),
        typeof(IReadOnlyList<ProviderDistributionItem>),
        typeof(ProviderDistributionChart),
        new PropertyMetadata(Array.Empty<ProviderDistributionItem>(), ItemsChanged));

    /// <summary>Initialises an empty distribution that remains truthful until the owning view supplies counts.</summary>
    public ProviderDistributionChart()
    {
        InitializeComponent();
        ApplyItems();
    }

    /// <summary>Gets or sets the already-validated provider counts shown by the chart and legend.</summary>
    public IReadOnlyList<ProviderDistributionItem> Items
    {
        get => (IReadOnlyList<ProviderDistributionItem>)GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>Rebuilds bounded segment geometry when the owning view atomically replaces provider counts.</summary>
    /// <param name="dependencyObject">Distribution control whose items changed.</param>
    /// <param name="e">Dependency-property event metadata.</param>
    private static void ItemsChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) =>
        ((ProviderDistributionChart)dependencyObject).ApplyItems();

    /// <summary>Reflows the ring and legend without changing their data whenever their card supplies a new width.</summary>
    /// <param name="sender">Distribution control whose arranged size changed.</param>
    /// <param name="e">Previous and new arranged dimensions.</param>
    private void DistributionSizeChanged(object sender, SizeChangedEventArgs e) => ApplyResponsiveLayout(e.NewSize.Width);

    /// <summary>Uses a smaller side-by-side ring before stacking both regions at genuinely narrow widths.</summary>
    /// <param name="availableWidth">Finite arranged width in device-independent pixels.</param>
    private void ApplyResponsiveLayout(double availableWidth)
    {
        bool hasMeasuredWidth = double.IsFinite(availableWidth) && availableWidth > 0;
        bool stacked = hasMeasuredWidth && availableWidth < 220;
        bool compact = hasMeasuredWidth && availableWidth < 300;

        RingViewport.Width = compact ? 96 : 126;
        RingViewport.Height = compact ? 96 : 126;
        RingColumn.Width = stacked ? new GridLength(1, GridUnitType.Star) : GridLength.Auto;
        LegendColumn.Width = stacked ? new GridLength(0) : new GridLength(1, GridUnitType.Star);

        Grid.SetRow(RingViewport, 0);
        Grid.SetColumn(RingViewport, 0);
        Grid.SetColumnSpan(RingViewport, stacked ? 2 : 1);
        Grid.SetRow(Legend, stacked ? 1 : 0);
        Grid.SetColumn(Legend, stacked ? 0 : 1);
        Grid.SetColumnSpan(Legend, stacked ? 2 : 1);
        RingViewport.Margin = stacked ? new Thickness(0) : new Thickness(0, 0, compact ? 8 : 12, 0);
        Legend.Margin = stacked ? new Thickness(0, 8, 0, 0) : new Thickness(0);
    }

    /// <summary>Draws at most one arc per visible provider and retains a complete textual legend.</summary>
    private void ApplyItems()
    {
        IReadOnlyList<ProviderDistributionItem> items = Items ?? Array.Empty<ProviderDistributionItem>();
        Legend.ItemsSource = items;
        foreach (Path path in SegmentCanvas.Children.OfType<Path>().ToArray())
        {
            SegmentCanvas.Children.Remove(path);
        }

        int total = items.Sum(item => Math.Max(0, item.Count));
        if (total <= 0)
        {
            AutomationProperties.SetName(this, string.Empty);
            return;
        }

        int visibleSegmentCount = items.Count(item => Math.Max(0, item.Count) > 0);
        double startAngle = -90;
        for (int index = 0; index < items.Count; index++)
        {
            int count = Math.Max(0, items[index].Count);
            if (count == 0)
            {
                continue;
            }

            double sweep = 360d * count / total;
            double gap = visibleSegmentCount > 1 ? Math.Min(4d, sweep / 3d) : 0;
            Path segment = CreateSegment(startAngle + (gap / 2d), sweep - gap);
            segment.SetResourceReference(Shape.StrokeProperty, $"ColourDataCategory{(index % 5) + 1}Brush");
            SegmentCanvas.Children.Add(segment);
            startAngle += sweep;
        }

        AutomationProperties.SetName(this, string.Join(", ", items.Select(item => $"{item.ProviderIdentity.ProviderType}: {item.Count}")));
    }

    /// <summary>Creates one ring segment on the fixed 44-unit design canvas, splitting a full circle safely.</summary>
    /// <param name="startAngle">Clockwise start angle in degrees.</param>
    /// <param name="sweepAngle">Positive clockwise sweep angle in degrees.</param>
    /// <returns>A non-interactive path that consumes a generated categorical brush.</returns>
    private static Path CreateSegment(double startAngle, double sweepAngle)
    {
        const double centre = 22;
        const double radius = 15.9;
        System.Windows.Point start = PointOnCircle(centre, radius, startAngle);
        PathFigure figure = new() { StartPoint = start, IsClosed = false, IsFilled = false };
        if (sweepAngle >= 359.999)
        {
            System.Windows.Point opposite = PointOnCircle(centre, radius, startAngle + 180);
            figure.Segments.Add(new ArcSegment(opposite, new System.Windows.Size(radius, radius), 0, false, SweepDirection.Clockwise, true));
            figure.Segments.Add(new ArcSegment(start, new System.Windows.Size(radius, radius), 0, false, SweepDirection.Clockwise, true));
        }
        else
        {
            System.Windows.Point end = PointOnCircle(centre, radius, startAngle + sweepAngle);
            figure.Segments.Add(new ArcSegment(end, new System.Windows.Size(radius, radius), 0, sweepAngle > 180, SweepDirection.Clockwise, true));
        }

        return new Path
        {
            Data = new PathGeometry([figure]),
            Fill = System.Windows.Media.Brushes.Transparent,
            StrokeThickness = 7,
            StrokeStartLineCap = PenLineCap.Flat,
            StrokeEndLineCap = PenLineCap.Flat,
            IsHitTestVisible = false,
        };
    }

    /// <summary>Converts one trusted polar coordinate into the fixed chart canvas.</summary>
    /// <param name="centre">Shared X/Y centre coordinate.</param>
    /// <param name="radius">Ring radius.</param>
    /// <param name="angle">Clockwise angle in degrees.</param>
    /// <returns>Finite point on the ring.</returns>
    private static System.Windows.Point PointOnCircle(double centre, double radius, double angle)
    {
        double radians = angle * Math.PI / 180d;
        return new System.Windows.Point(centre + radius * Math.Cos(radians), centre + radius * Math.Sin(radians));
    }
}
