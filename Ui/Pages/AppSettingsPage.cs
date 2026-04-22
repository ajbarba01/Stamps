using System.Windows.Forms;
using Stamps.Core.Services;

namespace Stamps.Ui.Pages;

/// <summary>
/// App-wide settings page: launch behaviour, start-on-startup, notification style, and theme.
/// Writes through <see cref="ISettingsStore"/> for the JSON-backed values and through
/// <see cref="IStartupManager"/> for the registry-backed startup flag.
/// </summary>
internal sealed class AppSettingsPage : UserControl, IPage
{
    private readonly ISettingsStore _settings;
    private readonly IStartupManager _startup;
    private readonly CheckBox _startupBox;

    public string Title => "Settings";

    public AppSettingsPage(ISettingsStore settings, IStartupManager startup)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(startup);
        _settings = settings;
        _startup = startup;

        Dock = DockStyle.Fill;
        BackColor = Theme.ContentBackground;
        Padding = new Padding(24);

        var stack = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = false,
            BackColor = Color.Transparent,
        };

        _startupBox = MakeToggle(
            "Start with Windows",
            _startup.IsEnabled,
            checkedNow => _startup.SetEnabled(checkedNow));

        var launchMin = MakeToggle(
            "Launch minimized to tray",
            _settings.App.LaunchMinimized,
            checkedNow =>
            {
                _settings.App.LaunchMinimized = checkedNow;
                _settings.SaveApp();
            });

        var notif = MakeDropdown(
            "Notifications",
            new[] { "brief", "silent" },
            _settings.App.NotificationStyle,
            value =>
            {
                _settings.App.NotificationStyle = value;
                _settings.SaveApp();
            });

        var theme = MakeDropdown(
            "Theme",
            new[] { "light" },
            "light",
            _ => { /* dark/system not yet implemented in WinForms */ });
        theme.Enabled = false;

        stack.Controls.Add(_startupBox);
        stack.Controls.Add(launchMin);
        stack.Controls.Add(notif);
        stack.Controls.Add(theme);

        Controls.Add(stack);

        _startup.Changed += OnStartupChangedExternally;
        Disposed += (_, _) => _startup.Changed -= OnStartupChangedExternally;
    }

    private void OnStartupChangedExternally(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(() => OnStartupChangedExternally(sender, e))); return; }
        if (_startupBox.Checked != _startup.IsEnabled) _startupBox.Checked = _startup.IsEnabled;
    }

    private static CheckBox MakeToggle(string label, bool initial, Action<bool> onChanged)
    {
        var cb = new CheckBox
        {
            Text = label,
            Font = Theme.Body,
            Checked = initial,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 6),
        };
        cb.CheckedChanged += (_, _) => onChanged(cb.Checked);
        return cb;
    }

    private static Control MakeDropdown(string label, string[] options, string selected, Action<string> onChanged)
    {
        var row = new TableLayoutPanel
        {
            ColumnCount = 2,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6),
        };
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        row.Controls.Add(new Label
        {
            Text = label,
            Font = Theme.Body,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 0),
        }, 0, 0);
        var combo = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 240,
            Font = Theme.Body,
        };
        foreach (var o in options) combo.Items.Add(o);
        int idx = combo.Items.IndexOf(selected);
        combo.SelectedIndex = idx >= 0 ? idx : 0;
        combo.SelectedIndexChanged += (_, _) =>
            onChanged(combo.SelectedItem?.ToString() ?? "");
        row.Controls.Add(combo, 1, 0);
        return row;
    }
}
