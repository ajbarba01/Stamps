using System.Windows.Forms;

namespace Stamps.App;

/// <summary>
/// Entry point. Enforces single-instance behaviour, configures WinForms globals, then hands
/// off to <see cref="StampsApplicationContext"/> for the lifetime of the app.
/// </summary>
internal static class Program
{
    private const string InstanceName = "Stamps-SingleInstance";

    [STAThread]
    private static void Main(string[] args)
    {
        bool autostart = StartupManager.IsAutostartInvocation(args);

        using var instance = new SingleInstance(InstanceName);
        if (!instance.IsPrimary)
        {
            // Secondary launch: foreground the running instance unless this was an autostart
            // race (in which case we silently exit — the primary is already present).
            if (!autostart) instance.SignalActivation();
            return;
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(defaultValue: false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        // Install the WinForms sync context now so the composition root can capture it.
        // Application.Run installs one too, but only after Run is called — and the context
        // is constructed before then.
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        using var context = new StampsApplicationContext(instance, launchedAsAutostart: autostart);
        Application.Run(context);
    }
}
