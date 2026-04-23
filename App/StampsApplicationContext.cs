using System.IO;
using System.Windows;
using System.Windows.Threading;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Tweaks.Snip;
using Stamps.Ui;
using Stamps.Ui.Theme;

namespace Stamps.App;

internal sealed class StampsApplicationContext : IDisposable
{
    private readonly FileLogger _logger;
    private readonly SettingsStore _settings;
    private readonly HotkeyService _hotkeys;
    private readonly StartupManager _startup;
    private readonly IMainWindow _mainWindow;
    private readonly TrayIconController _tray;
    private readonly NotifyIconNotifier _notifier;
    private readonly TweakRegistry _tweakRegistry;
    private readonly List<ITweak> _initializedTweaks = new();
    private readonly SingleInstance _singleInstance;
    private readonly Dispatcher _dispatcher;
    private bool _disposed;

    public StampsApplicationContext(SingleInstance singleInstance, bool launchedAsAutostart)
    {
        ArgumentNullException.ThrowIfNull(singleInstance);
        if (!singleInstance.IsPrimary)
            throw new InvalidOperationException(
                "StampsApplicationContext must be constructed on the primary instance only.");

        _singleInstance = singleInstance;
        _dispatcher = Dispatcher.CurrentDispatcher;

        var dataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Stamps");
        Directory.CreateDirectory(dataDir);

        var logDir = Path.Combine(dataDir, "logs");
        _logger = new FileLogger(logDir);
        _logger.Info($"Stamps starting (autostart={launchedAsAutostart}).");

        _settings = new SettingsStore(dataDir, _logger);
        _startup = new StartupManager();
        _hotkeys = new HotkeyService(_logger);
        _tweakRegistry = new TweakRegistry();

        _mainWindow = new MainWindow(_tweakRegistry, _settings, _startup, logDir);
        ThemeService.Apply(AppTheme.System);
        _tray = new TrayIconController(_mainWindow, _startup);
        _notifier = new NotifyIconNotifier(_tray.TaskbarIcon);

        // Tweaks registered after _notifier is ready (requires the tray icon).
        RegisterTweaks();

        _singleInstance.ActivationRequested += OnActivationRequested;

        if (ShouldOpenWindowOnStartup(launchedAsAutostart))
            _mainWindow.ShowOrFocus();
    }

    private void RegisterTweaks()
    {
        var snip = new SnipTweak();
        _tweakRegistry.Register(snip);
        var host = new TweakHost(_hotkeys, _notifier, _settings.ScopeFor(snip.Id), _logger);
        snip.Initialize(host);
        _initializedTweaks.Add(snip);
    }

    private bool ShouldOpenWindowOnStartup(bool launchedAsAutostart)
    {
        if (launchedAsAutostart) return false;
#if DEBUG
        return true;
#else
        if (_settings.App.LaunchMinimized) return false;
        return true;
#endif
    }

    private void OnActivationRequested(object? sender, EventArgs e)
    {
        _dispatcher.BeginInvoke(_mainWindow.ShowOrFocus);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try { _logger.Info("Stamps shutting down."); } catch { }

        _singleInstance.ActivationRequested -= OnActivationRequested;

        foreach (var tweak in _initializedTweaks)
            try { tweak.Shutdown(); } catch { }

        _tray.Dispose();
        _mainWindow.Dispose();
        _hotkeys.Dispose();
        _logger.Dispose();
    }
}
