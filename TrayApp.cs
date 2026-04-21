using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Snip;

internal sealed class TrayApp : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly HotkeyWindow _hotkeyWindow;
    private readonly Icon _icon;
    private bool _overlayOpen;

    public TrayApp()
    {
        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.HotkeyPressed += OnHotkeyPressed;

        if (!_hotkeyWindow.Register(Keys.S, KeyModifiers.Control | KeyModifiers.Alt | KeyModifiers.NoRepeat))
        {
            MessageBox.Show(
                "Failed to register Ctrl+Alt+S hotkey. It may already be in use by another application.",
                "Snip",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        _icon = LoadIcon();
        _contextMenu = CreateContextMenu();
        _trayIcon = new NotifyIcon
        {
            Icon = _icon,
            Text = "Snip",
            Visible = true,
            ContextMenuStrip = _contextMenu,
        };
    }

    private static Icon LoadIcon()
    {
        using var stream = typeof(TrayApp).Assembly
            .GetManifestResourceStream("Snip.Assets.app.ico")!;
        // Copy to MemoryStream — Icon holds a reference to its source stream.
        var ms = new MemoryStream();
        stream.CopyTo(ms);
        ms.Position = 0;
        return new Icon(ms);
    }

    private ContextMenuStrip CreateContextMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Snip  (Ctrl+Alt+S)", null, (_, _) => TakeSnip());
        menu.Items.Add(new ToolStripSeparator());

        var startupItem = new ToolStripMenuItem("Start on startup")
        {
            Checked = IsStartupEnabled(),
            CheckOnClick = true,
        };
        startupItem.CheckedChanged += (_, _) => SetStartup(startupItem.Checked);
        menu.Items.Add(startupItem);

        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => ExitApplication());
        return menu;
    }

    private const string StartupRegKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string StartupRegName = "Snip";

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, writable: false);
        return key?.GetValue(StartupRegName) is string path
            && path.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetStartup(bool enable)
    {
        using var key = Registry.CurrentUser.OpenSubKey(StartupRegKey, writable: true);
        if (key is null) return;

        if (enable)
            key.SetValue(StartupRegName, Application.ExecutablePath);
        else
            key.DeleteValue(StartupRegName, throwOnMissingValue: false);
    }

    private void OnHotkeyPressed(object? sender, EventArgs e) => TakeSnip();

    private void TakeSnip()
    {
        if (_overlayOpen) return;
        _overlayOpen = true;
        using var overlay = new OverlayForm();
        overlay.ShowDialog();
        _overlayOpen = false;

        if (overlay.SelectedBitmap is not { } bitmap)
            return;

        Clipboard.SetImage(bitmap);
        bitmap.Dispose();

        Notifier.ShowBrief(_trayIcon, "Screenshot copied to clipboard");
    }

    private void ExitApplication()
    {
        _trayIcon.Visible = false;
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hotkeyWindow.Dispose();
            _contextMenu.Dispose();
            _trayIcon.Dispose();
            _icon.Dispose();
        }
        base.Dispose(disposing);
    }

    private sealed class HotkeyWindow : NativeWindow, IDisposable
    {
        private const int WmHotKey = 0x0312;
        private const int HotkeyId = 1;

        public event EventHandler? HotkeyPressed;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
        }

        public bool Register(Keys key, KeyModifiers modifiers)
        {
            return RegisterHotKey(Handle, HotkeyId, (uint)modifiers, (uint)key);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotKey && m.WParam.ToInt32() == HotkeyId)
                HotkeyPressed?.Invoke(this, EventArgs.Empty);
            base.WndProc(ref m);
        }

        public void Dispose()
        {
            UnregisterHotKey(Handle, HotkeyId);
            DestroyHandle();
        }
    }
}

[Flags]
internal enum KeyModifiers : uint
{
    None     = 0x0000,
    Alt      = 0x0001,
    Control  = 0x0002,
    Shift    = 0x0004,
    Win      = 0x0008,
    NoRepeat = 0x4000,
}
