// Module purpose: Guards WPF markup contracts that compile successfully but can fail when templates are materialised at runtime.
using System.Text.RegularExpressions;

namespace DBNotifier.Architecture.Tests;

/// <summary>
/// Verifies desktop markup boundaries that cannot be proved by the WPF compiler alone, including typed spacing values
/// and parity of the shared eight-destination information architecture.
/// </summary>
public sealed class WpfPresentationContractTests
{
    private static readonly Regex NumericSpacingResourceOnThicknessProperty = new(
        "(?:Padding|Margin|BorderThickness)=\\\"\\{(?:Dynamic|Static)Resource Space[0-9]+\\}\\\"|CornerRadius=\\\"\\{(?:Dynamic|Static)Resource Radius[A-Za-z]+\\}\\\"",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>Ensures numeric spacing tokens are not assigned to Thickness properties through runtime resource lookup.</summary>
    [Fact]
    public void WpfThicknessPropertiesDoNotUseNumericSpacingResources()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string[] violations = Directory.GetFiles(desktopDirectory, "*.xaml", SearchOption.AllDirectories)
            .Where(path => NumericSpacingResourceOnThicknessProperty.IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepositoryRoot(), path))
            .ToArray();

        Assert.Empty(violations);
    }

    /// <summary>Ensures WPF keeps the same eight primary destinations and ordering as the Dashboard Web shell.</summary>
    [Fact]
    public void WpfNavigationPreservesSharedDestinationOrder()
    {
        string markup = File.ReadAllText(Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf", "MainWindow.xaml"));
        string[] destinations = ["Overview", "Inventory", "Alerts", "Performance", "History", "Configuration", "Providers", "Settings"];
        int previousIndex = -1;

        foreach (string destination in destinations)
        {
            int index = markup.IndexOf($"Tag=\"{destination}\"", StringComparison.Ordinal);
            Assert.True(index > previousIndex, $"The WPF navigation destination '{destination}' is missing or out of order.");
            previousIndex = index;
        }
    }

    /// <summary>Ensures Windows shell and notification attribution use the canonical visual product name.</summary>
    [Fact]
    public void WpfAssemblyMetadataUsesCanonicalDisplayName()
    {
        string project = File.ReadAllText(Path.Combine(
            RepositoryRoot(),
            "src",
            "DBNotifier.Desktop.Wpf",
            "DBNotifier.Desktop.Wpf.csproj"));

        Assert.Contains("<AssemblyTitle>DB Notifier</AssemblyTitle>", project, StringComparison.Ordinal);
        Assert.Contains("<Product>DB Notifier</Product>", project, StringComparison.Ordinal);
        Assert.DoesNotContain("<AssemblyName>DB Notifier</AssemblyName>", project, StringComparison.Ordinal);
    }

    /// <summary>Ensures WPF selects resolution-matched ICO frames instead of resampling one large bitmap across shell roles.</summary>
    [Fact]
    public void WpfBrandIconsUseNativeResolutionFrames()
    {
        string desktopDirectory = Path.Combine(RepositoryRoot(), "src", "DBNotifier.Desktop.Wpf");
        string policy = File.ReadAllText(Path.Combine(desktopDirectory, "BrandStatusIconPolicy.cs"));
        string mainWindow = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml.cs"));
        string mainWindowMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "MainWindow.xaml"));
        string flyout = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml.cs"));
        string flyoutMarkup = File.ReadAllText(Path.Combine(desktopDirectory, "TrayFlyoutWindow.xaml"));
        string trayController = File.ReadAllText(Path.Combine(desktopDirectory, "TrayApplicationController.cs"));

        Assert.Contains("IconBitmapDecoder", policy, StringComparison.Ordinal);
        Assert.Contains("Math.Abs(candidate.PixelWidth - targetPixelSize)", policy, StringComparison.Ordinal);
        Assert.DoesNotContain("BitmapImage image = new()", policy, StringComparison.Ordinal);
        Assert.Contains("ApplyNativeWindowIcons(this, aggregateState)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("LoadImageSource(aggregateState, 40)", mainWindow, StringComparison.Ordinal);
        Assert.Contains("LoadImageSource(fleetSummary.State, 32)", flyout, StringComparison.Ordinal);
        Assert.Contains("Stretch=\"None\" SnapsToDevicePixels=\"True\"", mainWindowMarkup, StringComparison.Ordinal);
        Assert.Contains("Stretch=\"None\" SnapsToDevicePixels=\"True\"", flyoutMarkup, StringComparison.Ordinal);
        Assert.Contains("Forms.SystemInformation.SmallIconSize.Width", trayController, StringComparison.Ordinal);
        Assert.Contains("WmSetIcon", policy, StringComparison.Ordinal);
        Assert.Contains("IconSmall2", policy, StringComparison.Ordinal);
        Assert.Contains("IconBig", policy, StringComparison.Ordinal);
    }

    /// <summary>Finds the repository root from the compiled test output without depending on the caller's working directory.</summary>
    /// <returns>The absolute directory containing the solution file.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the test assembly is not running below the repository.</exception>
    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DBNotifier.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("The DB-Notifier repository root could not be resolved.");
    }
}
