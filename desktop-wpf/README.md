# DB-Notifier Desktop WPF Prototype

This folder contains a Windows desktop UI prototype retained from PgNotifier and renamed for DB-Notifier.

It is superseded and non-distributable. `desktop-wpf/build-desktop.ps1` fails closed; production WPF packaging remains owned by the signed `STATE-08` toolchain.

It uses WPF through PowerShell/XAML, so it does not require Node.js, npm, Electron, Vite, or the .NET SDK.

## Run

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -STA -File .\desktop-wpf\DBNotifier.Desktop.ps1
```

## Live Preview

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\desktop-wpf\watch.ps1
```

The watcher restarts the preview automatically when any of these files changes:

- `App.xaml`
- `DBNotifier.Desktop.ps1`
- `mock.instances.json`
- `watch.ps1`

## Files

- `App.xaml`: full WPF layout and visual styling.
- `DBNotifier.Desktop.ps1`: application runner, mock binding, button actions, dynamic status simulation.

`PgNotifier.Desktop.ps1` remains as a deprecated compatibility wrapper during the migration window.
- `mock.instances.json`: mock PostgreSQL instances used by the preview.
- `watch.ps1`: live preview mode.

## Mock Data

The preview simulates:

- Running PostgreSQL instance.
- Restarted PostgreSQL instance.
- Stopped PostgreSQL instance.

The second row alternates between `Restarted` and `Running` every few seconds to demonstrate dynamic status updates.
