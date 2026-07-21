// Module purpose: Adapts the Windows client-area animation preference into one shared reduced-motion presentation signal.
using System.Windows;

namespace DBNotifier.Desktop.Wpf;

/// <summary>
/// Observes the Windows client-area animation preference and exposes its reduced-motion meaning to WPF presentation code.
/// It changes presentation only and never mutates an operating-system preference.
/// </summary>
internal sealed class DesktopMotionService : IDisposable
{
    private bool disposed;
    private bool reducedMotionActive;

    /// <summary>Initialises observation of the existing Windows presentation preference.</summary>
    internal DesktopMotionService()
    {
        reducedMotionActive = !SystemParameters.ClientAreaAnimation;
        SystemParameters.StaticPropertyChanged += SystemParametersStaticPropertyChanged;
    }

    /// <summary>Gets whether non-essential movement must be removed from the current presentation.</summary>
    internal bool ReducedMotionActive => reducedMotionActive;

    /// <summary>Signals that the effective reduced-motion preference may have changed.</summary>
    internal event EventHandler? MotionPreferenceChanged;

    /// <summary>Stops preference observation without changing Windows configuration.</summary>
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        SystemParameters.StaticPropertyChanged -= SystemParametersStaticPropertyChanged;
        MotionPreferenceChanged = null;
    }

    /// <summary>Forwards only the client-area animation preference as a presentation change.</summary>
    /// <param name="sender">Static WPF system-parameter source.</param>
    /// <param name="e">Changed system-property metadata.</param>
    private void SystemParametersStaticPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SystemParameters.ClientAreaAnimation))
        {
            bool current = !SystemParameters.ClientAreaAnimation;
            if (current != reducedMotionActive)
            {
                reducedMotionActive = current;
                MotionPreferenceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
