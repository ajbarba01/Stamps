namespace Stamps.Core;

/// <summary>
/// Optional capability exposed by a tweak that supports user-created actions — e.g., an
/// "App Launcher" tweak lets users add one action per launchable app. Tweaks that only
/// ship built-in actions return <c>null</c> from <see cref="ITweak.UserActionFactory"/>.
/// </summary>
public interface IActionFactory
{
    /// <summary>Label shown on the UI's "add action" button (e.g., <c>"Add app launcher"</c>).</summary>
    string CreateButtonLabel { get; }

    /// <summary>
    /// Settings the user must fill in when creating a new action. The host renders an editor
    /// based on these descriptors and passes the collected values to <see cref="Create"/>.
    /// </summary>
    IReadOnlyList<SettingDescriptor> TemplateSettings { get; }

    /// <summary>
    /// Constructs a new user-created action from the collected template values.
    /// The returned action must have <see cref="IAction.IsUserCreated"/> set to <c>true</c>
    /// and a unique <see cref="IAction.Id"/>.
    /// </summary>
    IAction Create(SettingsValues initial);

    /// <summary>
    /// Removes a user-created action, unregistering its hotkey and deleting its persisted state.
    /// No-op if <paramref name="action"/> is not owned by this factory.
    /// </summary>
    void Delete(IAction action);
}
