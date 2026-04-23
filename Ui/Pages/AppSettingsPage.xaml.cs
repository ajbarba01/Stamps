using System.Windows;
using System.Windows.Controls;
using Stamps.Core.Services;
using Stamps.Ui.Theme;

namespace Stamps.Ui.Pages;

public partial class AppSettingsPage : UserControl, IPage
{
    private readonly ISettingsStore _settings;
    private readonly IStartupManager _startup;
    private bool _suppressEvents;

    public string Title => "Settings";

    public AppSettingsPage(ISettingsStore settings, IStartupManager startup)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(startup);

        _settings = settings;
        _startup = startup;

        InitializeComponent();

        _suppressEvents = true;

        StartupToggle.IsChecked = _startup.IsEnabled;
        LaunchMinimizedToggle.IsChecked = _settings.App.LaunchMinimized;

        NotificationsCombo.Items.Add("Brief");
        NotificationsCombo.Items.Add("Silent");
        NotificationsCombo.SelectedItem = _settings.App.NotificationStyle;
        if (NotificationsCombo.SelectedIndex < 0) NotificationsCombo.SelectedIndex = 0;

        ThemeCombo.Items.Add("System");
        ThemeCombo.Items.Add("Light");
        ThemeCombo.Items.Add("Dark");
        ThemeCombo.SelectedItem = ThemeService.Current.ToString();
        if (ThemeCombo.SelectedIndex < 0) ThemeCombo.SelectedIndex = 0;

        _suppressEvents = false;

        _startup.Changed += OnStartupChangedExternally;
        Unloaded += (_, _) => _startup.Changed -= OnStartupChangedExternally;
    }

    private void OnStartupChangedExternally(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _suppressEvents = true;
            StartupToggle.IsChecked = _startup.IsEnabled;
            _suppressEvents = false;
        });
    }

    private void OnStartupToggled(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        _startup.SetEnabled(StartupToggle.IsChecked == true);
    }

    private void OnLaunchMinimizedToggled(object sender, RoutedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.App.LaunchMinimized = LaunchMinimizedToggle.IsChecked == true;
        _settings.SaveApp();
    }

    private void OnNotificationsChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        _settings.App.NotificationStyle = NotificationsCombo.SelectedItem?.ToString() ?? "Brief";
        _settings.SaveApp();
    }

    private void OnThemeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (Enum.TryParse<AppTheme>(ThemeCombo.SelectedItem?.ToString(), out var theme))
            ThemeService.Apply(theme);
    }
}
