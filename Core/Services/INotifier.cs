namespace Stamps.Core.Services;

/// <summary>
/// Thin abstraction over the host's user-notification channel. Tweaks use this to surface
/// brief status messages without depending on any specific UI toolkit.
/// </summary>
public interface INotifier
{
    /// <summary>Show a brief notification with the app's default title.</summary>
    void ShowBrief(string message);

    /// <summary>Show a brief notification with a custom title.</summary>
    void ShowBrief(string title, string message);
}
