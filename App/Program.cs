namespace Stamps.App;

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
            if (!autostart) instance.SignalActivation();
            return;
        }

        var app = new App();
        app.InitializeComponent();

        using var host = new StampsApplicationContext(instance, launchedAsAutostart: autostart);
        app.Run();
    }
}
