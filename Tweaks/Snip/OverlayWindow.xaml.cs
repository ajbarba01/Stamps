using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace Stamps.Tweaks.Snip;

public partial class OverlayWindow : Window
{
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const int WmNcHitTest = 0x0084;
    private const int HtClient = 1;

    private Bitmap? _screenshot;
    private HwndSource? _hwndSource;
    private double _dpiX = 1.0;
    private double _dpiY = 1.0;
    private System.Windows.Point _dragStart;
    private bool _isDragging;

    /// <summary>Set when the user completes a selection; null if cancelled.</summary>
    public BitmapSource? CapturedBitmap { get; private set; }

    public OverlayWindow()
    {
        InitializeComponent();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // Prevent the OS from treating any part of the window as a draggable caption.
        _hwndSource = (HwndSource)PresentationSource.FromVisual(this);
        _hwndSource.AddHook(WndProc);

        var source = PresentationSource.FromVisual(this);
        _dpiX = source?.CompositionTarget.TransformToDevice.M11 ?? 1.0;
        _dpiY = source?.CompositionTarget.TransformToDevice.M22 ?? 1.0;

        // Cover the full virtual screen (all monitors).
        Left   = SystemParameters.VirtualScreenLeft;
        Top    = SystemParameters.VirtualScreenTop;
        Width  = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        // Capture at physical pixel resolution before the overlay is visible.
        var physLeft   = (int)(SystemParameters.VirtualScreenLeft  * _dpiX);
        var physTop    = (int)(SystemParameters.VirtualScreenTop   * _dpiY);
        var physWidth  = (int)(SystemParameters.VirtualScreenWidth  * _dpiX);
        var physHeight = (int)(SystemParameters.VirtualScreenHeight * _dpiY);

        _screenshot = new Bitmap(physWidth, physHeight, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(_screenshot);
        g.CopyFromScreen(physLeft, physTop, 0, 0, new System.Drawing.Size(physWidth, physHeight));

        var bitmapSource = ToBitmapSource(_screenshot);
        bitmapSource.Freeze();

        // ScreenImage fills the window in DIP coordinates; RevealBrush uses the same source.
        ScreenImage.Width  = Width;
        ScreenImage.Height = Height;
        ScreenImage.Source = bitmapSource;

        TintLayer.Width  = Width;
        TintLayer.Height = Height;

        RevealBrush.ImageSource = bitmapSource;
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            DialogResult = false;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _dragStart = e.GetPosition(RootCanvas);
        _isDragging = true;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;
        UpdateSelection(e.GetPosition(RootCanvas));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging || e.ChangedButton != MouseButton.Left) return;
        _isDragging = false;
        ReleaseMouseCapture();

        var (x, y, w, h) = GetNormalizedRect(_dragStart, e.GetPosition(RootCanvas));
        if (w < 1 || h < 1)
        {
            DialogResult = false;
            return;
        }

        // Convert DIP selection to physical pixels for the crop.
        var physX = (int)(x * _dpiX);
        var physY = (int)(y * _dpiY);
        var physW = (int)(w * _dpiX);
        var physH = (int)(h * _dpiY);

        using var cropped = _screenshot!.Clone(
            new System.Drawing.Rectangle(physX, physY, physW, physH),
            _screenshot.PixelFormat);

        CapturedBitmap = ToBitmapSource(cropped);
        CapturedBitmap.Freeze();
        DialogResult = true;
    }

    private void UpdateSelection(System.Windows.Point current)
    {
        var (x, y, w, h) = GetNormalizedRect(_dragStart, current);
        var visible = w >= 1 && h >= 1;

        Canvas.SetLeft(SelectionReveal, x);
        Canvas.SetTop(SelectionReveal, y);
        SelectionReveal.Width  = w;
        SelectionReveal.Height = h;

        // Viewbox maps physical pixel region of the source image to the reveal rectangle.
        RevealBrush.Viewbox = new Rect(x * _dpiX, y * _dpiY, w * _dpiX, h * _dpiY);

        Canvas.SetLeft(SelectionBorder, x);
        Canvas.SetTop(SelectionBorder, y);
        SelectionBorder.Width  = w;
        SelectionBorder.Height = h;

        var vis = visible ? Visibility.Visible : Visibility.Hidden;
        SelectionReveal.Visibility = vis;
        SelectionBorder.Visibility = vis;
    }

    private static (double x, double y, double w, double h) GetNormalizedRect(
        System.Windows.Point a, System.Windows.Point b)
    {
        var x = Math.Min(a.X, b.X);
        var y = Math.Min(a.Y, b.Y);
        var w = Math.Abs(b.X - a.X);
        var h = Math.Abs(b.Y - a.Y);
        return (x, y, w, h);
    }

    private static BitmapSource ToBitmapSource(Bitmap bitmap)
    {
        var handle = bitmap.GetHbitmap();
        try
        {
            return Imaging.CreateBitmapSourceFromHBitmap(
                handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }
        finally
        {
            DeleteObject(handle);
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WmNcHitTest) { handled = true; return (IntPtr)HtClient; }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        _screenshot?.Dispose();
        base.OnClosed(e);
    }
}
