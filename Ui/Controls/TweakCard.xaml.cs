using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Stamps.Core;

namespace Stamps.Ui.Controls;

public partial class TweakCard : UserControl
{
    public ITweak Tweak { get; }
    public event EventHandler? OpenRequested;
    public event EventHandler<bool>? EnableToggled;

    public TweakCard(ITweak tweak, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        Tweak = tweak;

        InitializeComponent();

        TitleText.Text = tweak.Title;
        DescText.Text = tweak.Description;
        IconImage.Source = IconToImageSource(tweak.Icon);

        EnableToggle.IsChecked = enabled;
        EnableToggle.Checked   += (_, _) => EnableToggled?.Invoke(this, true);
        EnableToggle.Unchecked += (_, _) => EnableToggled?.Invoke(this, false);

        MouseLeftButtonDown += OnCardMouseDown;
        MouseLeftButtonUp += OnCardMouseUp;
        CardBorder.MouseEnter += (_, _) =>
            CardBorder.Background = (Brush)FindResource("Stamps.Background.Elevated");
        CardBorder.MouseLeave += (_, _) =>
            CardBorder.Background = (Brush)FindResource("Stamps.Background.Card");
    }

    private void OnCardMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && IsDescendantOf(fe, EnableToggle))
            return;
        Mouse.Capture(this);
        e.Handled = true;
    }

    private void OnCardMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!IsMouseCaptured) return;
        ReleaseMouseCapture();

        if (!IsMouseOver) return;

        if (e.OriginalSource is FrameworkElement fe && IsDescendantOf(fe, EnableToggle))
            return;

        OpenRequested?.Invoke(this, EventArgs.Empty);
    }

    private static bool IsDescendantOf(DependencyObject child, DependencyObject parent)
    {
        var current = child;
        while (current != null)
        {
            if (ReferenceEquals(current, parent)) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private static ImageSource? IconToImageSource(System.Drawing.Icon? icon)
    {
        if (icon is null) return null;
        using var bmp = icon.ToBitmap();
        using var ms = new MemoryStream();
        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        ms.Position = 0;
        var img = new BitmapImage();
        img.BeginInit();
        img.StreamSource = ms;
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }
}
