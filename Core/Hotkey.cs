using System.Text;
using System.Windows.Input;

namespace Stamps.Core;

public readonly record struct Hotkey(HotkeyModifiers Modifiers, Key Key)
{
    public override string ToString()
    {
        var sb = new StringBuilder();
        if (Modifiers.HasFlag(HotkeyModifiers.Control)) sb.Append("Ctrl+");
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))     sb.Append("Alt+");
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))   sb.Append("Shift+");
        if (Modifiers.HasFlag(HotkeyModifiers.Win))     sb.Append("Win+");
        sb.Append(Key);
        return sb.ToString();
    }

    public static bool TryParse(string? text, out Hotkey result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        var mods = HotkeyModifiers.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            switch (parts[i].ToLowerInvariant())
            {
                case "ctrl": case "control": mods |= HotkeyModifiers.Control; break;
                case "alt":                  mods |= HotkeyModifiers.Alt;     break;
                case "shift":                mods |= HotkeyModifiers.Shift;   break;
                case "win": case "windows":  mods |= HotkeyModifiers.Win;     break;
                default: return false;
            }
        }

        if (mods == HotkeyModifiers.None) return false;
        if (!Enum.TryParse(parts[^1], ignoreCase: true, out Key key)) return false;

        result = new Hotkey(mods, key);
        return true;
    }
}

[Flags]
public enum HotkeyModifiers : uint
{
    None    = 0x0000,
    Alt     = 0x0001,
    Control = 0x0002,
    Shift   = 0x0004,
    Win     = 0x0008,
}
