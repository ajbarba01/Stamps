using System.Reflection;
using System.Windows.Controls;

namespace Stamps.Ui.Pages;

public partial class AboutPage : UserControl, IPage
{
    public string Title => "About";

    public AboutPage()
    {
        InitializeComponent();

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        VersionText.Text = $"Version {version}";
    }

}
