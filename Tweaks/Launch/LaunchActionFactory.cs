using Stamps.Core;

namespace Stamps.Tweaks.Launch;

internal sealed class LaunchActionFactory : IActionFactory
{
    private readonly List<LaunchAction> _actions = new();
    private readonly Action<List<LaunchEntry>> _persist;
    private ITweakHost? _host;

    public string CreateButtonLabel => "Add launch";

    public IReadOnlyList<SettingDescriptor> TemplateSettings =>
    [
        new TextSetting("title", "Name", Placeholder: "e.g. Close window"),
        new TextSetting("path", "Exe Path", Placeholder: "e.g. C:\\...\\app.exe"),
    ];

    internal LaunchActionFactory(Action<List<LaunchEntry>> persist)
    {
        _persist = persist;
    }

    internal IReadOnlyList<LaunchAction> Actions => _actions;

    internal void Activate(ITweakHost host, IReadOnlyList<LaunchEntry> entries)
    {
        _host = host;
        foreach (var entry in entries)
        {
            var action = new LaunchAction(entry, SaveAll);
            action.Activate(host);
            _actions.Add(action);
        }
    }

    internal void Deactivate()
    {
        foreach (var action in _actions)
            action.Deactivate();
        _actions.Clear();
        _host = null;
    }

    public IAction Create(SettingsValues initial)
    {
        var entry = new LaunchEntry
        {
            Id      = "user_" + Guid.NewGuid().ToString("N")[..8],
            Title   = initial.GetString("title", "New alias"),
            ExePath  = initial.GetString("path", ""),
            Trigger = "",
            Enabled = true,
        };
        var action = new LaunchAction(entry, SaveAll);
        if (_host is not null) action.Activate(_host);
        _actions.Add(action);
        SaveAll();
        return action;
    }

    public void Delete(IAction action)
    {
        if (action is not LaunchAction alias || !_actions.Contains(alias)) return;
        alias.Deactivate();
        _actions.Remove(alias);
        SaveAll();
    }

    private void SaveAll()
    {
        _persist(_actions.Select(a => a.ToEntry()).ToList());
    }
}
