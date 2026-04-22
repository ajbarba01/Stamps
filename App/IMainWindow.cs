namespace Stamps.App;

/// <summary>
/// The narrow contract the composition root uses to drive the main window. Keeping this
/// interface in <c>App/</c> means the rest of the app never imports anything from <c>Ui/</c>
/// — swapping WinForms for a different UI toolkit later only requires a new implementation.
/// </summary>
public interface IMainWindow : IDisposable
{
    /// <summary>
    /// Makes the window visible, restores it if minimized, and brings it to the foreground.
    /// Must be safe to call from any thread; implementations marshal to the UI thread.
    /// </summary>
    void ShowOrFocus();

    /// <summary>Hides the window without destroying it (tray remains the primary surface).</summary>
    void Hide();

    /// <summary>Whether the window is currently visible on-screen.</summary>
    bool IsVisible { get; }
}
