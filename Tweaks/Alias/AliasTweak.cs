using System.Drawing;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Alias;

public sealed class AliasTweak : ITweak
{
    private readonly AliasActionFactory _factory;
    private ISettingsScope? _scope;
    private AliasData _data = new();

    public AliasTweak()
    {
        _factory = new AliasActionFactory(SaveEntries);
    }

    public string  Id          => "alias";
    public string  Title       => "Alias";
    public string  Description => "Remap one key combo to send another.";
    public Icon    Icon        => SystemIcons.Application;
    public Version SdkVersion  => new(1, 0);

    public IReadOnlyList<IAction>  BuiltInActions   => _factory.Actions;
    public IActionFactory?         UserActionFactory => _factory;
    public IReadOnlyList<SettingDescriptor> Settings => Array.Empty<SettingDescriptor>();
    public SettingsValues          Values            { get; } = new();

    public void PersistSettings() { }

    public void Initialize(ITweakHost host)
    {
        _scope = host.Settings;
        _data  = host.Settings.Load<AliasData>();
        _factory.Activate(host, _data.Aliases);
    }

    public void Shutdown() => _factory.Deactivate();

    private void SaveEntries(List<AliasEntry> entries)
    {
        _data.Aliases = entries;
        _scope?.Save(_data);
    }
}
