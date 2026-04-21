using System.Windows.Forms;

namespace Snip;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        using var mutex = new System.Threading.Mutex(true, "Snip-SingleInstance", out bool isNew);
        if (!isNew) return;

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        using var app = new TrayApp();
        Application.Run(app);
    }
}
