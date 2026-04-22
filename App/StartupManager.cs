using System.Windows.Forms;
using Microsoft.Win32;

namespace Stamps.App;

/// <summary>
/// Owns the "Start with Windows" toggle, backed by the per-user <c>Run</c> registry key.
/// The registry is the canonical source of truth — this class never caches the state — so
/// external changes (e.g., the user editing the registry) are reflected the next time
/// <see cref="IsEnabled"/> is read.
/// </summary>
/// <remarks>
/// The registered command line always includes <c>--autostart</c>, which <c>Program.cs</c>
/// inspects to decide whether to launch silently to tray or open the main window.
/// </remarks>
public sealed class StartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Stamps";
    private const string AutostartArg = "--autostart";

    /// <summary>Raised after <see cref="SetEnabled"/> successfully writes or clears the key.</summary>
    public event EventHandler? Changed;

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key?.GetValue(ValueName) is not string value) return false;
            return value.Contains(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return;

        if (enable)
        {
            var command = $"\"{Application.ExecutablePath}\" {AutostartArg}";
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Returns <c>true</c> if the given command-line arguments indicate an
    /// autostart launch (used by the composition root to decide launch behaviour).</summary>
    public static bool IsAutostartInvocation(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Any(a => string.Equals(a, AutostartArg, StringComparison.OrdinalIgnoreCase));
    }
}
