using System.Windows.Forms;

namespace Snip;

internal static class Notifier
{
    internal static void ShowBrief(NotifyIcon trayIcon, string message)
    {
        trayIcon.ShowBalloonTip(1500, "Snip", message, ToolTipIcon.None);
    }
}
