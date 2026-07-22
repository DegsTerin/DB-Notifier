// Module purpose: Hosts the shared WPF performance-chart visual without loading or interpreting telemetry.
namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents two deterministic demonstration series with complete axes and grid information.
/// Data acquisition and authoritative-source decisions remain outside this presentation-only control.
/// </summary>
internal sealed partial class PerformanceChart : System.Windows.Controls.UserControl
{
    /// <summary>Initialises the immutable code-native chart geometry.</summary>
    public PerformanceChart() => InitializeComponent();
}
