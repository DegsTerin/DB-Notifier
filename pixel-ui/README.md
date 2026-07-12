# DB-Notifier Pixel UI

This is a legacy desktop UI prototype retained as a DB-Notifier visual reference.

Technology: Python + Tkinter Canvas. No external Python packages are required.

## Structure

```text
pixel-ui/
  app.py
  watch.ps1
  README.md
```

## Run

```powershell
python .\pixel-ui\app.py
```

If Python is not on `PATH`, use the full path to your Python executable:

```powershell
& "C:\Path\To\python.exe" .\pixel-ui\app.py
```

## Live Preview

```powershell
.\pixel-ui\watch.ps1 -Python "python"
```

If Python is not on `PATH`:

```powershell
.\pixel-ui\watch.ps1 -Python "C:\Path\To\python.exe"
```

The watcher restarts the preview automatically whenever `app.py` is saved.

## Visual Targets

- 386 x 453 window matching the supplied image proportions.
- Main rounded dark glass panel.
- PostgreSQL-style green icon.
- Header: `All instances are healthy`.
- Three fixed mock instances with Running, Restarted, and Stopped states.
- Action rows: Restart service, Open log, Open config, Reload configuration, Exit.
- Simulated Windows tray footer with time and date.
- Hover highlight on action rows.
