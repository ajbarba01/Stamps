using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using H.NotifyIcon;
using Stamps.Ui.Theme;

namespace Stamps.App;

internal sealed class TrayIconController : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT pt);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private readonly TaskbarIcon _icon;
    private readonly ContextMenu _contextMenu;
    private readonly MenuItem _startupItem;
    private readonly IMainWindow _mainWindow;
    private readonly StartupManager _startup;
    private bool _suppressStartupToggle;

    public TaskbarIcon TaskbarIcon => _icon;

    public TrayIconController(IMainWindow mainWindow, StartupManager startup)
    {
        ArgumentNullException.ThrowIfNull(mainWindow);
        ArgumentNullException.ThrowIfNull(startup);

        _mainWindow = mainWindow;
        _startup = startup;

        _startupItem = new MenuItem
        {
            Header = "Start on startup",
            IsCheckable = true,
            IsChecked = _startup.IsEnabled,
        };
        _startupItem.Click += OnStartupItemClick;

        var openItem = new MenuItem { Header = "Open Stamps" };
        openItem.Click += (_, _) => _mainWindow.ShowOrFocus();

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += (_, _) => Application.Current.Shutdown();

        _contextMenu = new ContextMenu();
        _contextMenu.Opened += OnContextMenuOpened;
        _contextMenu.Items.Add(openItem);
        _contextMenu.Items.Add(new Separator());
        _contextMenu.Items.Add(_startupItem);
        _contextMenu.Items.Add(new Separator());
        _contextMenu.Items.Add(exitItem);

        _icon = new TaskbarIcon
        {
            Icon = LoadEmbeddedIcon(),
            ToolTipText = "Stamps",
        };
        _icon.TrayLeftMouseUp  += (_, _) => _mainWindow.ShowOrFocus();
        _icon.TrayRightMouseUp += (_, _) => OpenContextMenuAtCursor();
        _icon.ForceCreate(enablesEfficiencyMode: false);

        _startup.Changed += OnStartupChanged;
    }

    private void OpenContextMenuAtCursor()
    {
        GetCursorPos(out var pt);
        _contextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.AbsolutePoint;
        _contextMenu.HorizontalOffset = pt.X;
        _contextMenu.VerticalOffset = pt.Y;
        _contextMenu.IsOpen = true;
    }

    private static void OnContextMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is ContextMenu m && PresentationSource.FromVisual(m) is HwndSource src)
            ThemeService.ApplyToHwnd(src.Handle);
    }

    private void OnStartupItemClick(object sender, RoutedEventArgs e)
    {
        if (_suppressStartupToggle) return;
        _startup.SetEnabled(_startupItem.IsChecked);
    }

    private void OnStartupChanged(object? sender, EventArgs e)
    {
        var desired = _startup.IsEnabled;
        if (_startupItem.IsChecked == desired) return;

        _suppressStartupToggle = true;
        try { _startupItem.IsChecked = desired; }
        finally { _suppressStartupToggle = false; }
    }

    private static Icon LoadEmbeddedIcon()
    {
        using var stream = typeof(TrayIconController).Assembly
            .GetManifestResourceStream("Stamps.Assets.app.ico")
            ?? throw new InvalidOperationException(
                "Embedded resource 'Stamps.Assets.app.ico' not found.");

        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return new Icon(ms);
    }

    public void Dispose()
    {
        _startup.Changed -= OnStartupChanged;
        _startupItem.Click -= OnStartupItemClick;
        _icon.Dispose();
    }
}
