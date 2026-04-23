using System.Drawing;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Snip;

public sealed class SnipTweak : ITweak
{
    private ITweakHost? _host;
    private SnipSettings _settings = new();
    private SnipAction? _action;

    public string Id => "snip";
    public string Title => "Snip";
    public string Description => "Capture a region of the screen and copy it to the clipboard.";
    public Icon Icon => SystemIcons.Application;
    public Version SdkVersion => new(1, 0);

    public IReadOnlyList<IAction> BuiltInActions =>
        _action is null ? Array.Empty<IAction>() : new IAction[] { _action };

    public IActionFactory? UserActionFactory => null;
    public IReadOnlyList<SettingDescriptor> Settings => Array.Empty<SettingDescriptor>();
    public SettingsValues Values { get; } = new();

    public void PersistSettings() { }

    public void Initialize(ITweakHost host)
    {
        _host = host;
        _settings = host.Settings.Load<SnipSettings>();
        _action = new SnipAction(host.Hotkeys, host.Notifier, host.Logger, host.Settings, _settings);
    }

    public void Shutdown() => _action?.Cleanup();
}
