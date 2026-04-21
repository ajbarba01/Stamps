# Snipp

Lightweight Windows tray app for instant region screenshots. Press **Ctrl+Alt+S** to open a fullscreen overlay, drag a region, and the screenshot is copied to your clipboard.

## Requirements

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

## Usage

```bash
dotnet run
```

The app runs silently in the system tray. Right-click the tray icon for options including **Start on startup** and **Exit**.

### Hotkey

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+S` | Capture a region |
| `Esc` | Cancel capture |

## Build

```bash
# Debug
dotnet build

# Release — single self-contained .exe
dotnet publish -c Release -r win-x64 --self-contained
```
