using System.Windows.Forms;
using Stamps.App;

namespace Stamps.Ui;

/// <summary>
/// Phase 1 stand-in for the real main window. Implements <see cref="IMainWindow"/> so the
/// composition root is already wired to the correct seam; Phase 2 replaces this with the
/// sidebar-driven control panel without touching <c>App/</c>.
/// </summary>
/// <remarks>
/// Closes to tray on the user clicking the X (consistent with Phase 2 behaviour). Uses
/// <c>BeginInvoke</c> to marshal <see cref="ShowOrFocus"/> onto the UI thread so the method
/// is safe to call from any thread.
/// </remarks>
internal sealed class PlaceholderMainWindow : Form, IMainWindow
{
    public PlaceholderMainWindow()
    {
        Text = "Stamps";
        Size = new Size(640, 400);
        MinimumSize = new Size(480, 320);
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;

        var label = new Label
        {
            Text =
                "Stamps — Phase 1 scaffolding.\r\n\r\n" +
                "Tray, single-instance, settings, hotkey, and logging services are wired up.\r\n" +
                "The real control-panel UI lands in Phase 2; the first tweak (Snip) in Phase 3.",
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(16),
        };
        Controls.Add(label);
    }

    public void ShowOrFocus()
    {
        if (IsDisposed) return;
        if (InvokeRequired) { BeginInvoke(new Action(ShowOrFocus)); return; }

        if (!Visible) Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
        BringToFront();
    }

    bool IMainWindow.IsVisible => Visible;

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        // Closing the X hides to tray; only real shutdown reasons close the form.
        if (e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnFormClosing(e);
    }
}
