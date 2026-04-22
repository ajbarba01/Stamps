using System.Windows.Forms;

namespace Stamps.App;

/// <summary>
/// Owns the <see cref="NotifyIcon"/> and its context menu. Delegates window interactions
/// to <see cref="IMainWindow"/> so the tray has no compile-time dependency on any specific
/// UI implementation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="NotifyIcon"/> instance is exposed via <see cref="NotifyIcon"/> so that
/// the app's notifier can reuse it for balloon tips — sharing one icon keeps the tray
/// experience consistent and avoids duplicate tray entries.
/// </para>
/// <para>
/// The "Start on startup" menu item is a live view of the <see cref="StartupManager"/>;
/// the controller listens for <see cref="StartupManager.Changed"/> so that a toggle made
/// from the settings page is reflected back in the tray menu.
/// </para>
/// </remarks>
internal sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _startupItem;
    private readonly IMainWindow _mainWindow;
    private readonly StartupManager _startup;
    private readonly Icon _trayIcon;
    private bool _suppressStartupToggle;

    public NotifyIcon NotifyIcon => _icon;

    public TrayIconController(IMainWindow mainWindow, StartupManager startup)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        ArgumentNullException.ThrowIfNull(startup);

        _mainWindow = mainWindow;
        _startup = startup;

        _trayIcon = LoadEmbeddedIcon();
        _menu = BuildContextMenu(out _startupItem);
        _icon = new NotifyIcon
        {
            Icon = _trayIcon,
            Text = "Stamps",
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _icon.MouseClick += OnTrayMouseClick;
        _startup.Changed += OnStartupChanged;
    }

    private void OnTrayMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
            _mainWindow.ShowOrFocus();
    }

    private ContextMenuStrip BuildContextMenu(out ToolStripMenuItem startupItem)
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open Stamps", image: null, (_, _) => _mainWindow.ShowOrFocus());
        menu.Items.Add(new ToolStripSeparator());

        startupItem = new ToolStripMenuItem("Start on startup")
        {
            Checked = _startup.IsEnabled,
            CheckOnClick = true,
        };
        startupItem.CheckedChanged += OnStartupItemToggled;
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", image: null, (_, _) => Application.Exit());
        return menu;
    }

    private void OnStartupItemToggled(object? sender, EventArgs e)
    {
        if (_suppressStartupToggle) return;
        _startup.SetEnabled(_startupItem.Checked);
    }

    private void OnStartupChanged(object? sender, EventArgs e)
    {
        var desired = _startup.IsEnabled;
        if (_startupItem.Checked == desired) return;

        // Avoid re-entering SetEnabled while we mirror the external change.
        _suppressStartupToggle = true;
        try { _startupItem.Checked = desired; }
        finally { _suppressStartupToggle = false; }
    }

    private static Icon LoadEmbeddedIcon()
    {
        using var stream = typeof(TrayIconController).Assembly
            .GetManifestResourceStream("Stamps.Assets.app.ico")
            ?? throw new InvalidOperationException(
                "Embedded resource 'Stamps.Assets.app.ico' not found.");

        // Copy to a MemoryStream because Icon holds a reference to its source stream.
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return new Icon(ms);
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _startup.Changed -= OnStartupChanged;
        _startupItem.CheckedChanged -= OnStartupItemToggled;
        _icon.MouseClick -= OnTrayMouseClick;
        _icon.Dispose();
        _menu.Dispose();
        _trayIcon.Dispose();
    }
}
