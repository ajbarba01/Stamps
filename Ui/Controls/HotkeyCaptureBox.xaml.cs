using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Ui.Controls;

public partial class HotkeyCaptureBox : UserControl
{
    private Hotkey? _hotkey;

    public event EventHandler? HotkeyChanged;

    public HotkeyCaptureBox() => InitializeComponent();

    public Hotkey? Hotkey
    {
        get => _hotkey;
        set
        {
            if (Nullable.Equals(_hotkey, value)) return;
            _hotkey = value;
            UpdateDisplay();
            HotkeyChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnGotFocus(object sender, RoutedEventArgs e)
    {
        HotkeyCaptureSuppressor.Suppress();
        Display.Text = "Press a key combination…";
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        HotkeyCaptureSuppressor.Resume();
        UpdateDisplay();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (IsModifierKey(key))
        {
            Display.Text = DescribeModifiers(e.KeyboardDevice.Modifiers) + "…";
            return;
        }

        if (key == Key.Escape && e.KeyboardDevice.Modifiers == ModifierKeys.None)
        {
            Hotkey = null;
            return;
        }

        var mods = ToHotkeyModifiers(e.KeyboardDevice.Modifiers);
        if (mods == HotkeyModifiers.None) { UpdateDisplay(); return; }

        Hotkey = new Hotkey(mods, key);
    }

    private void UpdateDisplay() =>
        Display.Text = _hotkey?.ToString() ?? "(unbound — click and press a combination)";

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin;

    private static HotkeyModifiers ToHotkeyModifiers(ModifierKeys mods)
    {
        var r = HotkeyModifiers.None;
        if ((mods & ModifierKeys.Control) != 0) r |= HotkeyModifiers.Control;
        if ((mods & ModifierKeys.Alt)     != 0) r |= HotkeyModifiers.Alt;
        if ((mods & ModifierKeys.Shift)   != 0) r |= HotkeyModifiers.Shift;
        return r;
    }

    private static string DescribeModifiers(ModifierKeys mods)
    {
        var parts = new List<string>(3);
        if ((mods & ModifierKeys.Control) != 0) parts.Add("Ctrl");
        if ((mods & ModifierKeys.Alt)     != 0) parts.Add("Alt");
        if ((mods & ModifierKeys.Shift)   != 0) parts.Add("Shift");
        return parts.Count == 0 ? "" : string.Join("+", parts) + "+";
    }
}
