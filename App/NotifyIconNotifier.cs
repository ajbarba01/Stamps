using H.NotifyIcon;
using H.NotifyIcon.Core;
using Stamps.Core.Services;

namespace Stamps.App;

internal sealed class NotifyIconNotifier : INotifier
{
    private const string DefaultTitle = "Stamps";

    private readonly TaskbarIcon _icon;

    public NotifyIconNotifier(TaskbarIcon icon)
    {
        ArgumentNullException.ThrowIfNull(icon);
        _icon = icon;
    }

    public void ShowBrief(string message) => ShowBrief(DefaultTitle, message);

    public void ShowBrief(string title, string message) =>
        _icon.ShowNotification(title, message, NotificationIcon.None);
}
