# Retained UI Prototypes

<!-- Purpose: Groups retained visual prototypes away from the active product source and repository root. -->

This directory preserves early DB-Notifier interface experiments for historical
and regression reference. Their source remains under the repository MIT
licence. They are not production applications, supported packages or evidence
of operational database integration.

- `pixel-ui/` is a dependency-free Tkinter visual mock-up.
- `tray-app/` is a Python tray interaction mock-up with external runtime dependencies. It consumes the canonical DB Notifier product mark from the active WPF source and retains a fail-closed vendor-asset downloader.

The active React Dashboard and .NET WPF client live under `src/`. The compatibility-sensitive PowerShell/XAML entry point remains under `desktop-wpf/` until its separately governed removal criteria are met.
