using Stamps.Core;

namespace Stamps.Tweaks.Alias;

internal sealed class AliasActionFactory : IActionFactory
{
    private readonly List<AliasAction> _actions = new();
    private readonly Action<List<AliasEntry>> _persist;
    private ITweakHost? _host;

    public string CreateButtonLabel => "Add alias";

    public IReadOnlyList<SettingDescriptor> TemplateSettings =>
    [
        new TextSetting("title", "Name", Placeholder: "e.g. Close window"),
        new HotkeySetting("target", "Send keys"),
    ];

    internal AliasActionFactory(Action<List<AliasEntry>> persist)
    {
        _persist = persist;
    }

    internal IReadOnlyList<AliasAction> Actions => _actions;

    internal void Activate(ITweakHost host, IReadOnlyList<AliasEntry> entries)
    {
        _host = host;
        foreach (var entry in entries)
        {
            var action = new AliasAction(entry, SaveAll);
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
        var entry = new AliasEntry
        {
            Id      = "user_" + Guid.NewGuid().ToString("N")[..8],
            Title   = initial.GetString("title", "New alias"),
            Target  = initial.GetString("target", ""),
            Trigger = "",
            Enabled = true,
        };
        var action = new AliasAction(entry, SaveAll);
        if (_host is not null) action.Activate(_host);
        _actions.Add(action);
        SaveAll();
        return action;
    }

    public void Delete(IAction action)
    {
        if (action is not AliasAction alias || !_actions.Contains(alias)) return;
        alias.Deactivate();
        _actions.Remove(alias);
        SaveAll();
    }

    private void SaveAll()
    {
        _persist(_actions.Select(a => a.ToEntry()).ToList());
    }
}
