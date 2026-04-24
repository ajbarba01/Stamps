using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Launch;

internal sealed class LaunchAction : IAction
{
    private readonly LaunchEntry _entry;
    private readonly Action _persist;
    private IHotkeyService? _hotkeys;
    private ILogger? _logger;
    private IHotkeyBinding? _binding;
    private Hotkey? _hotkey;

    public string Id           => _entry.Id;
    public string Title        => _entry.Title;
    public bool   IsUserCreated => true;

    public bool Enabled
    {
        get => _entry.Enabled;
        set { _entry.Enabled = value; UpdateRegistration(); }
    }

    public Hotkey? Hotkey
    {
        get => _hotkey;
        set { _hotkey = value; _entry.Trigger = value?.ToString() ?? ""; UpdateRegistration(); }
    }

    private static readonly IReadOnlyList<SettingDescriptor> _settings =
    [
        new TextSetting("path", "Exe path", Placeholder: "e.g. C:\\...\\app.exe"),
    ];

    public IReadOnlyList<SettingDescriptor> Settings => _settings;
    public SettingsValues Values { get; } = new();

    internal LaunchAction(LaunchEntry entry, Action persist)
    {
        _entry = entry;
        _persist = persist;
        _hotkey = Core.Hotkey.TryParse(entry.Trigger, out var hk) ? hk : null;
        Values.Set("path", entry.ExePath);
    }

    internal void Activate(ITweakHost host)
    {
        _hotkeys = host.Hotkeys;
        _logger = host.Logger;
        UpdateRegistration();
    }

    internal void Deactivate()
    {
        _binding?.Dispose();
        _binding = null;
        _hotkeys = null;
        _logger = null;
    }

    private void UpdateRegistration()
    {
        _binding?.Dispose();
        _binding = null;
        if (_hotkeys is null || !_entry.Enabled || _hotkey is null) return;

        _binding = _hotkeys.TryRegister(_hotkey.Value, Invoke, out var result);
        if (result != HotkeyBindResult.Success)
            _logger!.Warn($"Launch '{_entry.Title}': failed to register hotkey {_hotkey} ({result}).");
    }

    public void PersistSettings() => _persist();

    internal LaunchEntry ToEntry() => new()
    {
        Id      = _entry.Id,
        Title   = _entry.Title,
        Trigger = _hotkey?.ToString() ?? "",
        ExePath  = Values.GetString("path", ""),
        Enabled = _entry.Enabled,
    };

    public void Invoke()
    {
        var path = Values.GetString("path", "");
        if (string.IsNullOrWhiteSpace(path)) return;

        var name = Path.GetFileNameWithoutExtension(path);
        var running = Process.GetProcessesByName(name)
                             .FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);

        if (running is not null)
        {
            if (IsIconic(running.MainWindowHandle))
                ShowWindow(running.MainWindowHandle, 9); // SW_RESTORE only when minimized
            SetForegroundWindow(running.MainWindowHandle);
        }
        else
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
}
