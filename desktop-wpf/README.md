# PgNotifier Desktop WPF Prototype

This folder contains a Windows desktop UI prototype that matches the supplied PgNotifier tray popup reference as closely as possible.

It uses WPF through PowerShell/XAML, so it does not require Node.js, npm, Electron, Vite, or the .NET SDK.

## Run

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File .\desktop-wpf\PgNotifier.Desktop.ps1
```

## Live Preview

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\desktop-wpf\watch.ps1
```

The watcher restarts the preview automatically when any of these files changes:

- `App.xaml`
- `PgNotifier.Desktop.ps1`
- `mock.instances.json`
- `watch.ps1`

## Files

- `App.xaml`: full WPF layout and visual styling.
- `PgNotifier.Desktop.ps1`: application runner, mock binding, button actions, dynamic status simulation.
- `mock.instances.json`: mock PostgreSQL instances used by the preview.
- `watch.ps1`: live preview mode.

## Mock Data

The preview simulates:

- Running PostgreSQL instance.
- Restarted PostgreSQL instance.
- Stopped PostgreSQL instance.

The second row alternates between `Restarted` and `Running` every few seconds to demonstrate dynamic status updates.
