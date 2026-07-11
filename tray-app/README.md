# PgNotifier Tray App

This is a fresh system tray implementation focused on the supplied reference image.

Technology:

- Python
- Tkinter borderless popup
- `pystray` real system tray icon
- Pillow for the official PostgreSQL PNG asset

## Official PostgreSQL Icon

The icon is downloaded from the PostgreSQL Wiki logo page:

`https://wiki.postgresql.org/images/9/9a/PostgreSQL_logo.3colors.540x557.png`

Download/update it with:

```powershell
.\tray-app\download-postgres-icon.ps1
```

The file is saved to:

```text
tray-app/assets/postgres.png
```

The app uses this official asset for:

- System tray icon
- Popup header icon

The displayed icon is tinted green at runtime to match the provided design while preserving the official elephant shape.

## Run

```powershell
& "C:\Users\brunn\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" .\tray-app\app.py
```

The app starts hidden in the system tray. Click the tray icon to show the popup.

## Live Reload

```powershell
.\tray-app\watch.ps1 -Python "C:\Users\brunn\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe"
```

The watcher restarts the tray app when `app.py` or assets change.

## Build EXE

```powershell
.\tray-app\build-exe.ps1
```

Output:

```text
dist/tray-app/PgNotifierTray.exe
```

## UI Contents

- PostgreSQL header icon.
- Header text: `All instances are healthy`.
- Local PostgreSQL 18, Analytics DB, Reporting DB mock rows.
- Running, Restarted, Stopped status colours.
- Actions: Restart service, Open log, Open config, Reload configuration, Exit.
- Popup closes when it loses focus.
- No taskbar window is shown.
