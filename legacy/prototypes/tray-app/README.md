# DB-Notifier Tray App

This is a legacy system tray prototype retained as a DB-Notifier visual reference.

Historical technology:

- Python
- Tkinter borderless popup
- `pystray` real system tray icon
- Pillow and the canonical DB Notifier product mark shared with the active WPF source

Its source remains available under the repository MIT licence. Executable
packaging is intentionally unavailable because its Python dependencies and
packaging toolchain are not owned or locked by DB-Notifier.

## Product-mark boundary

The prototype reads `src/DBNotifier.Desktop.Wpf/NotificationAssets/DBNotifier.Availability.png`, the canonical generated product mark already owned by the active WPF source. No vendor-specific artwork is retained here. Automatic vendor-asset download remains blocked.

## Run

```powershell
python .\legacy\prototypes\tray-app\app.py
```

The app starts hidden in the system tray. Click the tray icon to show the popup.

## Live Reload

```powershell
.\legacy\prototypes\tray-app\watch.ps1 -Python "python"
```

The watcher restarts the tray app when `app.py` changes.

## Packaging boundary

`legacy/prototypes/tray-app/build-exe.ps1` fails closed. `DBNotifierTray.exe` is not an authorised or reproducible artefact.

## UI Contents

- Canonical DB Notifier header icon.
- Header text: `All instances are healthy`.
- Local PostgreSQL 18, Analytics DB, Reporting DB mock rows.
- Running, Restarted, Stopped status colours.
- Actions: Restart service, Open log, Open config, Reload configuration, Exit.
- Popup closes when it loses focus.
- No taskbar window is shown.
