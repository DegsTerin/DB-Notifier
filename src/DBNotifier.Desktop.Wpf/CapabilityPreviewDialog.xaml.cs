// Module purpose: Coordinates owned capability-preview dialogue focus, native theme parity and safe dismissal without executing an action.
using System.Windows;
using System.Windows.Automation;
using System.Windows.Input;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Presents one localised fail-closed capability outcome in an owned modal window.
/// The execution control remains disabled; Close, Escape and native modal focus containment are the only interactions.
/// </summary>
internal sealed partial class CapabilityPreviewDialog : Window
{
    private readonly DesktopThemeService theme;

    /// <summary>Initialises a localised owned dialogue without retaining capability or credential data.</summary>
    /// <param name="theme">Current desktop theme owner used for native caption parity and High Contrast changes.</param>
    /// <param name="title">Localised factual outcome title.</param>
    /// <param name="message">Localised factual outcome description.</param>
    internal CapabilityPreviewDialog(DesktopThemeService theme, string title, string message)
    {
        ArgumentNullException.ThrowIfNull(theme);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        this.theme = theme;
        InitializeComponent();
        Title = title;
        DialogTitleText.Text = title;
        DialogMessageText.Text = message;
        AutomationProperties.SetName(this, title);
        AutomationProperties.SetHelpText(this, message);
        SourceInitialized += DialogSourceInitialized;
        theme.ThemeChanged += ThemeChanged;
        Closed += DialogClosed;
    }

    /// <summary>Applies the effective semantic theme to the Windows-managed dialogue caption after handle creation.</summary>
    /// <param name="sender">Initialised dialogue window.</param>
    /// <param name="e">Source-initialisation event metadata.</param>
    private void DialogSourceInitialized(object? sender, EventArgs e) => NativeWindowThemePolicy.Apply(this, theme);

    /// <summary>Reapplies native caption ownership when Light, Dark or Windows High Contrast changes.</summary>
    /// <param name="sender">Desktop theme service.</param>
    /// <param name="e">Theme-change event metadata.</param>
    private void ThemeChanged(object? sender, EventArgs e) => NativeWindowThemePolicy.Apply(this, theme);

    /// <summary>Places initial keyboard focus on the safe Close action.</summary>
    /// <param name="sender">Loaded dialogue window.</param>
    /// <param name="e">Loaded event metadata.</param>
    private void DialogLoaded(object sender, RoutedEventArgs e) => CloseButton.Focus();

    /// <summary>Dismisses the safe dialogue through the expected Escape path.</summary>
    /// <param name="sender">Dialogue receiving keyboard input.</param>
    /// <param name="e">Keyboard event whose Escape key is handled locally.</param>
    private void DialogPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Escape)
        {
            DialogResult = false;
            e.Handled = true;
        }
    }

    /// <summary>Dismisses the dialogue without enabling or executing the represented capability.</summary>
    /// <param name="sender">Safe Close button.</param>
    /// <param name="e">Button activation event metadata.</param>
    private void CloseButtonClick(object sender, RoutedEventArgs e) => DialogResult = false;

    /// <summary>Releases theme and native-source subscriptions when the owned dialogue closes.</summary>
    /// <param name="sender">Closed dialogue window.</param>
    /// <param name="e">Closed event metadata.</param>
    private void DialogClosed(object? sender, EventArgs e)
    {
        SourceInitialized -= DialogSourceInitialized;
        theme.ThemeChanged -= ThemeChanged;
        Closed -= DialogClosed;
    }
}
