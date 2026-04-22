# Stamps

Windows tray app for a growing library of small system tweaks. Runs quietly in the background and provides a **PowerToys-style control panel** where each tweak exposes actions (hotkey or UI triggered) and configurable settings.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Usage

```bash
dotnet run
```

The app runs in the system tray. Click the tray icon to open the control panel, or right-click for options like **Start on startup** and **Exit**.

### Interface

| Section          | Description                                                  |
| ---------------- | ------------------------------------------------------------ |
| **Tweaks**       | Browse all available tweaks, search, and enable/disable them |
| **Tweak Detail** | View overview (README), actions, and settings for a tweak    |
| **Settings**     | Configure app-wide behavior (startup, theme, notifications)  |
| **About**        | Version info and log access                                  |

### Hotkeys

Hotkeys are defined per action and managed centrally.

- Each tweak may expose one or more actions
- Hotkeys are user-configurable in the tweak detail page
- Conflicts are detected automatically

> **Default (Snip, pending port):** `Ctrl+Alt+S` for region screenshot

## Build

```bash
# Debug
dotnet build

# Run in development
dotnet run

# Release — single self-contained .exe
dotnet publish -c Release -r win-x64 --self-contained
```

## Architecture (Overview)

- **Core/** — tweak contracts, hotkeys, settings, and shared services (no UI)
- **Ui/** — WinForms control panel (sidebar, pages, controls, Markdown renderer)
- **App/** — composition root, tray icon, startup behavior, single-instance logic
- **Tweaks/** — individual tweak modules (one folder per tweak)

Each **tweak** is a self-contained module implementing a shared interface, allowing the app to scale into a larger library over time.

## Notes

- Settings are stored in `%AppData%\Stamps\`
- The app is **single-instance** — launching again focuses the existing window
- Tweaks are **enabled by default** (opt-out model)
- README files for tweaks are loaded from disk and can be edited without rebuilding

## Roadmap

- Port original **Snip** screenshot tool as the first tweak
- Improve hotkey conflict UX
- Add reset-to-defaults and testing coverage
