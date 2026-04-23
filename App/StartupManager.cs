using Microsoft.Win32;
using Stamps.Core.Services;

namespace Stamps.App;

public sealed class StartupManager : IStartupManager
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Stamps";
    private const string AutostartArg = "--autostart";

    public event EventHandler? Changed;

    private static string ExePath =>
        Environment.ProcessPath
        ?? System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
        ?? "Stamps.exe";

    public bool IsEnabled
    {
        get
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
            if (key?.GetValue(ValueName) is not string value) return false;
            return value.Contains(ExePath, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void SetEnabled(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null) return;

        if (enable)
        {
            var command = $"\"{ExePath}\" {AutostartArg}";
            key.SetValue(ValueName, command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    public static bool IsAutostartInvocation(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        return args.Any(a => string.Equals(a, AutostartArg, StringComparison.OrdinalIgnoreCase));
    }
}
