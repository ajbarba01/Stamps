using Stamps.Core.Services;

namespace Stamps.Core;

/// <summary>
/// Default <see cref="ITweakHost"/> implementation: a thin, immutable record of the four
/// services a tweak is allowed to see. Created once per tweak so each tweak's settings
/// scope is pre-bound.
/// </summary>
internal sealed class TweakHost : ITweakHost
{
    public IHotkeyService Hotkeys { get; }
    public INotifier Notifier { get; }
    public ISettingsScope Settings { get; }
    public ILogger Logger { get; }

    public TweakHost(IHotkeyService hotkeys, INotifier notifier, ISettingsScope settings, ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(hotkeys);
        ArgumentNullException.ThrowIfNull(notifier);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(logger);
        Hotkeys = hotkeys;
        Notifier = notifier;
        Settings = settings;
        Logger = logger;
    }
}
