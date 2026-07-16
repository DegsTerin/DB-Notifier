# DB-Notifier Tray App

This is a legacy system tray prototype retained as a DB-Notifier visual reference.

Historical technology:

- Python
- Tkinter borderless popup
- `pystray` real system tray icon
- Pillow and a retained PostgreSQL PNG reference asset

This prototype is non-distributable. Its Python dependencies and packaging toolchain are not owned or locked by DB-Notifier, and its vendor-specific artwork is not the canonical DB Notifier product mark.

## Historical asset boundary

`tray-app/assets/postgres.png` is retained only as historical prototype evidence. Automatic download/update is blocked, and neither the asset nor a derived executable may be distributed as DB Notifier.

## Run

```powershell
python .\tray-app\app.py
```

The app starts hidden in the system tray. Click the tray icon to show the popup.

## Live Reload

```powershell
.\tray-app\watch.ps1 -Python "python"
```

The watcher restarts the tray app when `app.py` or assets change.

## Packaging boundary

`tray-app/build-exe.ps1` fails closed. `DBNotifierTray.exe` is not an authorised or reproducible artefact.

## UI Contents

- PostgreSQL header icon.
- Header text: `All instances are healthy`.
- Local PostgreSQL 18, Analytics DB, Reporting DB mock rows.
- Running, Restarted, Stopped status colours.
- Actions: Restart service, Open log, Open config, Reload configuration, Exit.
- Popup closes when it loses focus.
- No taskbar window is shown.
