namespace Stamps.Core.Services;

/// <summary>
/// Central registrar for global hotkeys. Owns a single hidden native window that receives
/// <c>WM_HOTKEY</c> messages and dispatches them to the registered callbacks on the UI
/// thread. Tweaks never call Win32 hotkey APIs directly.
/// </summary>
public interface IHotkeyService
{
    /// <summary>
    /// Attempts to register <paramref name="hotkey"/> and run <paramref name="onPressed"/>
    /// each time it fires. Returns a disposable binding on success, or <c>null</c> if the
    /// hotkey is already taken; <paramref name="result"/> always carries the detailed reason.
    /// Disposing the returned binding unregisters the hotkey.
    /// </summary>
    IHotkeyBinding? TryRegister(Hotkey hotkey, Action onPressed, out HotkeyBindResult result);

    /// <summary>Whether the given hotkey is currently registered by <em>this</em> service
    /// (i.e., another Stamps action). Does not detect hotkeys held by other processes.</summary>
    bool IsRegisteredByUs(Hotkey hotkey);
}

/// <summary>A live hotkey registration. Dispose to unregister.</summary>
public interface IHotkeyBinding : IDisposable
{
    /// <summary>The hotkey this binding is registered for.</summary>
    Hotkey Hotkey { get; }
}

/// <summary>Outcome of <see cref="IHotkeyService.TryRegister"/>.</summary>
public enum HotkeyBindResult
{
    /// <summary>Registered successfully.</summary>
    Success,
    /// <summary>Another Stamps action already holds this combination.</summary>
    AlreadyBoundByStamps,
    /// <summary>The OS refused the registration — typically another process owns it.</summary>
    SystemRefused,
}
