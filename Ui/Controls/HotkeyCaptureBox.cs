using System.Windows.Forms;
using Stamps.Core;

namespace Stamps.Ui.Controls;

/// <summary>
/// A read-only text box that captures the next modifier+key combination the user presses
/// and exposes it as a <see cref="Core.Hotkey"/>. Designed for the per-action settings UI.
/// </summary>
/// <remarks>
/// <para>
/// Capture starts when the box receives focus and ends when a non-modifier key is pressed
/// while at least one modifier is held. Pressing <c>Escape</c> with no modifiers clears the
/// current binding (sets <see cref="Hotkey"/> to <c>null</c>); the host is then expected to
/// unregister any previously bound hotkey.
/// </para>
/// <para>
/// This control performs no registration itself — it is a value editor only. The owning
/// page is responsible for asking <see cref="Core.Services.IHotkeyService"/> to bind the
/// new value and surfacing any conflict.
/// </para>
/// </remarks>
internal sealed class HotkeyCaptureBox : TextBox
{
    private Hotkey? _hotkey;

    /// <summary>Raised after the user commits a new value (including a clear to <c>null</c>).</summary>
    public event EventHandler? HotkeyChanged;

    public HotkeyCaptureBox()
    {
        ReadOnly = true;
        Cursor = Cursors.IBeam;
        Font = Theme.Body;
        TabStop = true;
        ShortcutsEnabled = false;
        UpdateDisplay();
    }

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

    protected override bool IsInputKey(Keys keyData) => true;

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        Text = "Press a key combination…";
        SelectionLength = 0;
    }

    protected override void OnLostFocus(EventArgs e)
    {
        base.OnLostFocus(e);
        UpdateDisplay();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;

        var key = e.KeyCode;
        if (IsModifierKey(key))
        {
            // Show modifiers as the user holds them, but don't commit yet.
            Text = DescribeModifiers(e.Modifiers) + "…";
            return;
        }

        if (key == Keys.Escape && e.Modifiers == Keys.None)
        {
            Hotkey = null;
            return;
        }

        var mods = ToHotkeyModifiers(e.Modifiers);
        if (mods == HotkeyModifiers.None)
        {
            // Bare keys are not allowed — they would intercept ordinary typing.
            UpdateDisplay();
            return;
        }

        Hotkey = new Hotkey(mods, key);
    }

    private void UpdateDisplay() =>
        Text = _hotkey?.ToString() ?? "(unbound — click and press a combination)";

    private static bool IsModifierKey(Keys key) =>
        key is Keys.ControlKey or Keys.LControlKey or Keys.RControlKey
            or Keys.ShiftKey   or Keys.LShiftKey   or Keys.RShiftKey
            or Keys.Menu       or Keys.LMenu       or Keys.RMenu
            or Keys.LWin       or Keys.RWin;

    private static HotkeyModifiers ToHotkeyModifiers(Keys winFormsModifiers)
    {
        var mods = HotkeyModifiers.None;
        if ((winFormsModifiers & Keys.Control) == Keys.Control) mods |= HotkeyModifiers.Control;
        if ((winFormsModifiers & Keys.Alt)     == Keys.Alt)     mods |= HotkeyModifiers.Alt;
        if ((winFormsModifiers & Keys.Shift)   == Keys.Shift)   mods |= HotkeyModifiers.Shift;
        // The Windows key is not exposed via Keys.Modifiers; users can rebind via the
        // settings file directly if they want a Win-key combination.
        return mods;
    }

    private static string DescribeModifiers(Keys mods)
    {
        var parts = new List<string>(3);
        if ((mods & Keys.Control) == Keys.Control) parts.Add("Ctrl");
        if ((mods & Keys.Alt)     == Keys.Alt)     parts.Add("Alt");
        if ((mods & Keys.Shift)   == Keys.Shift)   parts.Add("Shift");
        return parts.Count == 0 ? "" : string.Join("+", parts) + "+";
    }
}
