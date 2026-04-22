namespace Stamps.Core;

/// <summary>
/// A single invokable unit exposed by a tweak. An action can be triggered by a global hotkey,
/// by the host UI, or programmatically. Actions are the only surface through which a tweak
/// "does" something on the user's behalf.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Id"/> must be unique within the owning tweak and stable across app versions —
/// it is the primary key for persisted per-action state. User-created actions should generate
/// ids that cannot collide with built-in ones (e.g., prefix with <c>user_</c>).
/// </para>
/// <para>
/// The contract is deliberately UI-free: the host decides how actions are listed, enabled, and
/// triggered. <see cref="Invoke"/> is always called on the application's UI thread.
/// </para>
/// </remarks>
public interface IAction
{
    /// <summary>Unique, stable identifier within the owning tweak.</summary>
    string Id { get; }

    /// <summary>User-facing title displayed in the detail page's actions list.</summary>
    string Title { get; }

    /// <summary><c>true</c> if this action was created from the tweak's
    /// <see cref="IActionFactory"/>; <c>false</c> for built-in actions. Only user-created
    /// actions are deletable from the UI.</summary>
    bool IsUserCreated { get; }

    /// <summary>Whether the hotkey is registered and UI invocation is allowed. Setting this
    /// toggles registration with the hotkey service immediately.</summary>
    bool Enabled { get; set; }

    /// <summary>
    /// The global hotkey bound to this action, or <c>null</c> if the action is UI-only.
    /// Setting this property re-registers with the hotkey service; callers must check the
    /// result via the owning tweak's <c>ITweakHost</c> when a specific outcome is required.
    /// </summary>
    Hotkey? Hotkey { get; set; }

    /// <summary>Declarative per-action settings. Empty for actions that need no configuration.</summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Mutable runtime values for <see cref="Settings"/>. The host edits this bag in response
    /// to user input and then calls <see cref="PersistSettings"/>; the action is free to
    /// observe changes here for behavioural updates (e.g., re-validate).
    /// </summary>
    SettingsValues Values { get; }

    /// <summary>Flushes the current <see cref="Values"/> to the tweak's settings file. The
    /// tweak owns the file format; this method is the host's only persistence hook.</summary>
    void PersistSettings();

    /// <summary>
    /// Executes the action. Called on the UI thread. Implementations should guard against
    /// re-entry and should not block — long work must be dispatched off the UI thread.
    /// Exceptions are caught and logged by the host; they do not propagate.
    /// </summary>
    void Invoke();
}
