using System.Runtime.InteropServices;
using System.Windows.Input;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Alias;

internal sealed class AliasAction : IAction
{
    private readonly AliasEntry _entry;
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
        new HotkeySetting("target", "Send keys", Description: "The key combo to send when triggered"),
    ];

    public IReadOnlyList<SettingDescriptor> Settings => _settings;
    public SettingsValues Values { get; } = new();

    internal AliasAction(AliasEntry entry, Action persist)
    {
        _entry = entry;
        _persist = persist;
        _hotkey = Core.Hotkey.TryParse(entry.Trigger, out var hk) ? hk : null;
        Values.Set("target", entry.Target);
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
            _logger!.Warn($"Alias '{_entry.Title}': failed to register hotkey {_hotkey} ({result}).");
    }

    public void PersistSettings() => _persist();

    internal AliasEntry ToEntry() => new()
    {
        Id      = _entry.Id,
        Title   = _entry.Title,
        Trigger = _hotkey?.ToString() ?? "",
        Target  = Values.GetString("target", ""),
        Enabled = _entry.Enabled,
    };

    public void Invoke()
    {
        if (!Core.Hotkey.TryParse(Values.GetString("target", ""), out var target)) return;
        SendKeys(target, _hotkey?.Modifiers ?? HotkeyModifiers.None);
    }

    // --- SendInput -----------------------------------------------------------

    private const uint   INPUT_KEYBOARD  = 1;
    private const uint   KEYEVENTF_KEYUP = 0x0002;
    private const ushort VK_SHIFT        = 0x10;
    private const ushort VK_CONTROL      = 0x11;
    private const ushort VK_MENU         = 0x12; // Alt
    private const ushort VK_LWIN         = 0x5B;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public uint Type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT Ki;
        [FieldOffset(0)] public MOUSEINPUT Mi; // ensures union is padded to MOUSEINPUT size
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx; public int dy; public uint mouseData; public uint dwFlags; public uint time; public IntPtr dwExtraInfo;
    }

    private static INPUT MakeKeyDown(ushort vk) => new() { Type = INPUT_KEYBOARD, U = new() { Ki = new() { wVk = vk } } };
    private static INPUT MakeKeyUp(ushort vk)   => new() { Type = INPUT_KEYBOARD, U = new() { Ki = new() { wVk = vk, dwFlags = KEYEVENTF_KEYUP } } };

    private static void SendKeys(Core.Hotkey target, HotkeyModifiers triggerMods)
    {
        var inputs = new List<INPUT>(16);

        // Release trigger modifiers so they don't bleed into the target combo.
        if (triggerMods.HasFlag(HotkeyModifiers.Control)) inputs.Add(MakeKeyUp(VK_CONTROL));
        if (triggerMods.HasFlag(HotkeyModifiers.Alt))     inputs.Add(MakeKeyUp(VK_MENU));
        if (triggerMods.HasFlag(HotkeyModifiers.Shift))   inputs.Add(MakeKeyUp(VK_SHIFT));
        if (triggerMods.HasFlag(HotkeyModifiers.Win))     inputs.Add(MakeKeyUp(VK_LWIN));

        // Press target modifiers.
        if (target.Modifiers.HasFlag(HotkeyModifiers.Control)) inputs.Add(MakeKeyDown(VK_CONTROL));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Alt))     inputs.Add(MakeKeyDown(VK_MENU));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Shift))   inputs.Add(MakeKeyDown(VK_SHIFT));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Win))     inputs.Add(MakeKeyDown(VK_LWIN));

        var vk = (ushort)KeyInterop.VirtualKeyFromKey(target.Key);
        if (vk == 0) return;
        inputs.Add(MakeKeyDown(vk));
        inputs.Add(MakeKeyUp(vk));

        // Release target modifiers in reverse order.
        if (target.Modifiers.HasFlag(HotkeyModifiers.Win))     inputs.Add(MakeKeyUp(VK_LWIN));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Shift))   inputs.Add(MakeKeyUp(VK_SHIFT));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Alt))     inputs.Add(MakeKeyUp(VK_MENU));
        if (target.Modifiers.HasFlag(HotkeyModifiers.Control)) inputs.Add(MakeKeyUp(VK_CONTROL));

        var arr = inputs.ToArray();
        SendInput((uint)arr.Length, arr, Marshal.SizeOf<INPUT>());
    }
}
