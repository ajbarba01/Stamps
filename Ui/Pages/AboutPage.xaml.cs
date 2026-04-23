using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Stamps.Ui.Pages;

public partial class AboutPage : UserControl, IPage
{
    private readonly string _logDirectory;

    public string Title => "About";

    public AboutPage(string logDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logDirectory);
        _logDirectory = logDirectory;

        InitializeComponent();

        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
        VersionText.Text = $"Version {version}";
    }

    private void OnOpenLogsClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Directory.CreateDirectory(_logDirectory);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_logDirectory}\"")
            {
                UseShellExecute = true,
            });
        }
        catch { }
    }
}
