using System.Text;
using System.Windows.Forms;

namespace Stamps.Core;

/// <summary>
/// A platform-independent description of a global hotkey: a non-empty set of modifier keys
/// combined with a single trigger key.
/// </summary>
/// <remarks>
/// This type is a pure value — it does not register or unregister anything with the OS.
/// Registration is the responsibility of <see cref="Services.IHotkeyService"/>.
/// </remarks>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, Keys Key)
{
    /// <summary>
    /// Returns a human-readable string such as <c>Ctrl+Alt+S</c>. Stable across locales
    /// (uses invariant key names) so the output is safe to persist to settings files.
    /// </summary>
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

    /// <summary>
    /// Parses a string previously produced by <see cref="ToString"/> (or any equivalent
    /// <c>Mod+Mod+Key</c> form). Returns <c>false</c> for malformed input; never throws.
    /// </summary>
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
        if (!Enum.TryParse(parts[^1], ignoreCase: true, out Keys key)) return false;

        result = new Hotkey(mods, key);
        return true;
    }
}

/// <summary>
/// Modifier keys usable in a global hotkey. Values intentionally match the Win32
/// <c>MOD_*</c> flags so they can be passed to <c>RegisterHotKey</c> without a mapping step.
/// </summary>
[Flags]
public enum HotkeyModifiers : uint
{
    None    = 0x0000,
    Alt     = 0x0001,
    Control = 0x0002,
    Shift   = 0x0004,
    Win     = 0x0008,
}
