using System.Windows;

namespace Stamps.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
    }
}
