using System.Windows.Forms;

namespace Stamps.Core.Services;

/// <summary>
/// <see cref="INotifier"/> implementation backed by a <see cref="NotifyIcon"/> balloon tip.
/// Does not own the tray icon — the caller retains ownership and lifetime.
/// </summary>
internal sealed class NotifyIconNotifier : INotifier
{
    private const int BalloonMs = 1500;
    private const string DefaultTitle = "Stamps";

    private readonly NotifyIcon _icon;

    public NotifyIconNotifier(NotifyIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        _icon = icon;
    }

    public void ShowBrief(string message) => ShowBrief(DefaultTitle, message);

    public void ShowBrief(string title, string message)
    {
        if (!_icon.Visible) return;
        _icon.ShowBalloonTip(BalloonMs, title, message, ToolTipIcon.None);
    }
}
