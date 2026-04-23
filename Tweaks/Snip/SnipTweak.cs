using System.Drawing;
using System.IO;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Snip;

public sealed class SnipTweak : ITweak
{
    private SnipSettings _settings = new();
    private readonly SnipAction _action;

    public SnipTweak()
    {
        _action = new SnipAction(_settings);
    }

    public string Id => "snip";
    public string Title => "Snip";
    public string Description => "Capture a region of the screen and copy it to the clipboard.";
    public Icon Icon => new Icon(Path.Combine(AppContext.BaseDirectory, "Tweaks", "Snip", "icon.ico"));
    public Version SdkVersion => new(1, 0);

    public IReadOnlyList<IAction> BuiltInActions => new IAction[] { _action };

    public IActionFactory? UserActionFactory => null;
    public IReadOnlyList<SettingDescriptor> Settings => Array.Empty<SettingDescriptor>();
    public SettingsValues Values { get; } = new();

    public void PersistSettings() { }

    public void Initialize(ITweakHost host)
    {
        _settings = host.Settings.Load<SnipSettings>();
        _action.Activate(host.Hotkeys, host.Notifier, host.Logger, host.Settings, _settings);
    }

    public void Shutdown() => _action.Deactivate();
}
