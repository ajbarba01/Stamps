using System.Drawing;
using System.IO;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Launch;

public sealed class LaunchTweak : ITweak
{
    private readonly LaunchActionFactory _factory;
    private ISettingsScope? _scope;
    private LaunchData _data = new();

    public LaunchTweak()
    {
        _factory = new LaunchActionFactory(SaveEntries);
    }

    public string  Id          => "launch";
    public string  Title       => "Launch";
    public string  Description => "Run or switch to an exe with a keystroke";
    public Icon    Icon        => new Icon(Path.Combine(AppContext.BaseDirectory, "Tweaks", "Launch", "icon.ico"));
    public Version SdkVersion  => new(1, 0);

    public IReadOnlyList<IAction>  BuiltInActions   => _factory.Actions;
    public IActionFactory?         UserActionFactory => _factory;
    public IReadOnlyList<SettingDescriptor> Settings => Array.Empty<SettingDescriptor>();
    public SettingsValues          Values            { get; } = new();

    public void PersistSettings() { }

    public void Initialize(ITweakHost host)
    {
        _scope = host.Settings;
        _data  = host.Settings.Load<LaunchData>();
        _factory.Activate(host, _data.Launches);
    }

    public void Shutdown() => _factory.Deactivate();

    private void SaveEntries(List<LaunchEntry> entries)
    {
        _data.Launches = entries;
        _scope?.Save(_data);
    }
}
