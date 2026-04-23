using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using Stamps.App;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Ui.Pages;
using Stamps.Ui.Theme;

namespace Stamps.Ui;

public partial class MainWindow : Window, IMainWindow
{
    private readonly HomePage _home;
    private readonly AppSettingsPage _appSettings;
    private readonly AboutPage _about;
    private TweakDetailPage? _activeDetail;
    private IPage? _currentPage;
    private bool _isDisposing;

    public MainWindow(
        TweakRegistry registry,
        ISettingsStore settings,
        IStartupManager startup,
        string logDirectory)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(startup);

        InitializeComponent();

        _home = new HomePage(registry, settings, OpenTweakDetail);
        _appSettings = new AppSettingsPage(settings, startup);
        _about = new AboutPage(logDirectory);

        Navigate(_home);
    }

    public void ShowOrFocus()
    {
        Dispatcher.BeginInvoke(() =>
        {
            if (!IsVisible) Show();
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Activate();
        });
    }

    bool IMainWindow.IsVisible => IsVisible;

    public void Dispose()
    {
        _isDisposing = true;
        Dispatcher.Invoke(Close);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ThemeService.ApplyToHwnd(new WindowInteropHelper(this).Handle);
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        ThemeService.ApplyToHwnd(new WindowInteropHelper(this).Handle);
    }

    protected override void OnClosed(EventArgs e)
    {
        ThemeService.ThemeChanged -= OnThemeChanged;
        base.OnClosed(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isDisposing)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    private void OnNavChecked(object sender, RoutedEventArgs e)
    {
        if (sender == NavTweaks)   Navigate(_home);
        if (sender == NavSettings) Navigate(_appSettings);
        if (sender == NavAbout)    Navigate(_about);
    }

    private void OpenTweakDetail(ITweak tweak)
    {
        _activeDetail?.Dispose();
        _activeDetail = new TweakDetailPage(tweak, onBack: () => Navigate(_home));
        Navigate(_activeDetail);
    }

    private void Navigate(IPage page)
    {
        if (ReferenceEquals(_currentPage, page)) return;

        ContentArea.Content = page;
        HeaderTitle.Text = page.Title;
        UpdateNavSelection(page);

        if (page is not TweakDetailPage && _activeDetail is { } stale)
        {
            _activeDetail = null;
            stale.Dispose();
        }

        _currentPage = page;
    }

    private void UpdateNavSelection(IPage current)
    {
        NavTweaks.IsChecked   = current == _home || current is TweakDetailPage;
        NavSettings.IsChecked = current == _appSettings;
        NavAbout.IsChecked    = current == _about;
    }
}
