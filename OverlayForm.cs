using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Snip;

internal sealed class OverlayForm : Form
{
    private readonly Bitmap _screenshot;
    private readonly Bitmap _tintedScreenshot;
    private readonly Pen _selectionPen = new(Color.White, 1);

    private Point _dragStart;
    private Point _dragCurrent;
    private bool _isDragging;
    private Rectangle _lastSelection;

    // Ownership transfers to caller on DialogResult.OK; not disposed here.
    public Bitmap? SelectedBitmap { get; private set; }

    public OverlayForm()
    {
        _screenshot = ScreenCapture.CaptureVirtualScreen();
        _tintedScreenshot = BakeTintedScreenshot(_screenshot);

        FormBorderStyle = FormBorderStyle.None;
        Bounds = SystemInformation.VirtualScreen;
        TopMost = true;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        BackColor = Color.Black;
        KeyPreview = true;

        KeyDown += OnKeyDown;
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Activate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.CompositingMode = CompositingMode.SourceCopy;
        g.CompositingQuality = CompositingQuality.HighSpeed;

        g.DrawImage(_tintedScreenshot, Point.Empty);

        if (!_isDragging)
            return;

        var selection = GetNormalizedSelection();
        if (selection.Width < 1 || selection.Height < 1)
            return;

        g.CompositingMode = CompositingMode.SourceCopy;
        g.DrawImage(_screenshot, selection, selection, GraphicsUnit.Pixel);
        g.CompositingMode = CompositingMode.SourceOver;
        g.DrawRectangle(_selectionPen, selection);
    }

    private static Bitmap BakeTintedScreenshot(Bitmap source)
    {
        var bmp = new Bitmap(source.Width, source.Height, source.PixelFormat);
        using var g = Graphics.FromImage(bmp);
        g.CompositingMode = CompositingMode.SourceOver;
        g.DrawImage(source, Point.Empty);
        using var tintBrush = new SolidBrush(Color.FromArgb(100, 0, 0, 0));
        g.FillRectangle(tintBrush, 0, 0, bmp.Width, bmp.Height);
        return bmp;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Escape)
            return;
        DialogResult = DialogResult.Cancel;
        Close();
    }

    private void OnMouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left)
            return;
        _dragStart = e.Location;
        _dragCurrent = e.Location;
        _isDragging = true;
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_isDragging)
            return;
        _dragCurrent = e.Location;
        var newSelection = GetNormalizedSelection();
        // Repaint only the union of old and new selection rectangles (plus 1px border).
        var dirty = Rectangle.Union(_lastSelection, newSelection);
        dirty.Inflate(2, 2);
        _lastSelection = newSelection;
        Invalidate(dirty);
    }

    private void OnMouseUp(object? sender, MouseEventArgs e)
    {
        if (!_isDragging || e.Button != MouseButtons.Left)
            return;

        _isDragging = false;
        var selection = GetNormalizedSelection();

        if (selection.Width < 1 || selection.Height < 1)
        {
            DialogResult = DialogResult.Cancel;
            Close();
            return;
        }

        SelectedBitmap = ScreenCapture.CropRegion(_screenshot, selection);
        DialogResult = DialogResult.OK;
        Close();
    }

    private Rectangle GetNormalizedSelection() => new(
        Math.Min(_dragStart.X, _dragCurrent.X),
        Math.Min(_dragStart.Y, _dragCurrent.Y),
        Math.Abs(_dragCurrent.X - _dragStart.X),
        Math.Abs(_dragCurrent.Y - _dragStart.Y));

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _screenshot.Dispose();
            _tintedScreenshot.Dispose();
            _selectionPen.Dispose();
        }
        base.Dispose(disposing);
    }
}
