using Stamps.Core.Services;

namespace Stamps.Core;

/// <summary>
/// The narrow service surface exposed to a tweak. Tweaks receive an instance scoped to
/// themselves (the <see cref="Settings"/> scope is already bound to the tweak's id) and must
/// not reach beyond this interface to interact with the host — this boundary is what makes
/// future runtime plugin loading safe.
/// </summary>
public interface ITweakHost
{
    /// <summary>Global hotkey registration and conflict detection.</summary>
    IHotkeyService Hotkeys { get; }

    /// <summary>User-visible notifications (balloon tips / toasts).</summary>
    INotifier Notifier { get; }

    /// <summary>
    /// Persistent settings scope for this tweak. The tweak's id is pre-bound; calling code
    /// never needs to supply it.
    /// </summary>
    ISettingsScope Settings { get; }

    /// <summary>Structured logging routed to the app's log file.</summary>
    ILogger Logger { get; }
}
