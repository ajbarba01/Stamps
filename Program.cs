using System.Windows.Forms;

namespace Snip;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetHighDpiMode(HighDpiMode.SystemAware);

        using var app = new TrayApp();
        Application.Run(app);
    }
}
