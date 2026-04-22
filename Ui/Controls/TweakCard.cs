using System.Windows.Forms;
using Stamps.Core;

namespace Stamps.Ui.Controls;

/// <summary>
/// A clickable home-page card for a single tweak: icon, title, description, and an enable
/// toggle. Raises <see cref="OpenRequested"/> when the user clicks the card body, and
/// <see cref="EnabledChanged"/> when the toggle is flipped.
/// </summary>
/// <remarks>
/// The toggle's <c>Click</c> is intercepted so flipping it never bubbles up as a card-open
/// event — users frequently want to enable/disable without navigating into the detail page.
/// </remarks>
internal sealed class TweakCard : Panel
{
    private readonly CheckBox _toggle;

    public ITweak Tweak { get; }
    public event EventHandler? OpenRequested;
    public event EventHandler<bool>? EnableToggled;

    public TweakCard(ITweak tweak, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        Tweak = tweak;

        Height = 88;
        Margin = new Padding(0, 0, 0, 8);
        BackColor = Theme.CardBackground;
        Padding = new Padding(16, 12, 16, 12);
        Cursor = Cursors.Hand;

        var iconBox = new PictureBox
        {
            Image = tweak.Icon.ToBitmap(),
            SizeMode = PictureBoxSizeMode.Zoom,
            Size = new Size(48, 48),
            Location = new Point(16, 20),
            BackColor = Color.Transparent,
        };

        var title = new Label
        {
            Text = tweak.Title,
            Font = Theme.CardTitle,
            AutoSize = false,
            Height = 22,
            BackColor = Color.Transparent,
        };

        var desc = new Label
        {
            Text = tweak.Description,
            Font = Theme.Body,
            ForeColor = Theme.SecondaryText,
            AutoSize = false,
            Height = 36,
            BackColor = Color.Transparent,
            AutoEllipsis = true,
        };

        _toggle = new CheckBox
        {
            Appearance = Appearance.Normal,
            Checked = enabled,
            Text = "",
            AutoSize = true,
            BackColor = Color.Transparent,
        };
        _toggle.CheckedChanged += (_, _) => EnableToggled?.Invoke(this, _toggle.Checked);

        Controls.Add(iconBox);
        Controls.Add(title);
        Controls.Add(desc);
        Controls.Add(_toggle);

        Layout += (_, _) =>
        {
            const int textLeft = 80;
            int textWidth = ClientSize.Width - textLeft - 80;
            title.Bounds = new Rectangle(textLeft, 14, textWidth, 22);
            desc.Bounds = new Rectangle(textLeft, 38, textWidth, 36);
            _toggle.Location = new Point(ClientSize.Width - _toggle.Width - 16, (ClientSize.Height - _toggle.Height) / 2);
        };

        // Card-body click → open. We attach to the panel and to its non-interactive children.
        Click += RaiseOpen;
        iconBox.Click += RaiseOpen;
        title.Click += RaiseOpen;
        desc.Click += RaiseOpen;
    }

    public void SetEnabled(bool value)
    {
        if (_toggle.Checked != value) _toggle.Checked = value;
    }

    private void RaiseOpen(object? sender, EventArgs e) => OpenRequested?.Invoke(this, EventArgs.Empty);

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); BackColor = Theme.HoverSubtle; }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); BackColor = Theme.CardBackground; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var pen = new Pen(Theme.CardBorder);
        var r = ClientRectangle;
        e.Graphics.DrawRectangle(pen, 0, 0, r.Width - 1, r.Height - 1);
    }
}
