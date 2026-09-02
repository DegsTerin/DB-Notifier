# DB-Notifier Pixel UI

This is a legacy desktop UI prototype retained as a DB-Notifier visual reference.

Its source remains available under the repository MIT licence. Executable
packaging is intentionally unavailable: `legacy/prototypes/pixel-ui/build-exe.ps1`
fails closed because no owned, pinned and hashed PyInstaller toolchain exists.

Technology: Python + Tkinter Canvas. No external Python packages are required.

## Structure

```text
legacy/prototypes/pixel-ui/
  app.py
  watch.ps1
  README.md
```

## Run

```powershell
python .\legacy\prototypes\pixel-ui\app.py
```

If Python is not on `PATH`, use the full path to your Python executable:

```powershell
& "C:\Path\To\python.exe" .\legacy\prototypes\pixel-ui\app.py
```

## Live Preview

```powershell
.\legacy\prototypes\pixel-ui\watch.ps1 -Python "python"
```

If Python is not on `PATH`:

```powershell
.\legacy\prototypes\pixel-ui\watch.ps1 -Python "C:\Path\To\python.exe"
```

The watcher restarts the preview automatically whenever `app.py` is saved.

## Visual Targets

- 386 x 453 window matching the supplied image proportions.
- Main rounded dark glass panel.
- Canonical DB Notifier product mark shared with the active WPF source.
- Header: `All instances are healthy`.
- Three fixed mock instances with Running, Restarted, and Stopped states.
- Action rows: Restart service, Open log, Open config, Reload configuration, Exit.
- Simulated Windows tray footer with time and date.
- Hover highlight on action rows.
