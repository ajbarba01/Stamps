using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Snip;

internal static class ScreenCapture
{
    internal static Bitmap CaptureVirtualScreen()
    {
        var bounds = SystemInformation.VirtualScreen;
        var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bitmap);
        g.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        return bitmap;
    }

    internal static Bitmap CropRegion(Bitmap source, Rectangle region)
    {
        return source.Clone(region, source.PixelFormat);
    }
}
