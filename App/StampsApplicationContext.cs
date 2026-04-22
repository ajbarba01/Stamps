using System.Windows.Forms;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Ui;

namespace Stamps.App;

/// <summary>
/// The composition root. Wires every long-lived service and the tray in dependency order,
/// routes single-instance activation signals to the main window, and tears everything down
/// on exit. Owns the synchronisation context used to marshal background callbacks.
/// </summary>
/// <remarks>
/// <para>
/// Services are created inside the constructor rather than injected because there is
/// exactly one composition root and it is the rightful owner of lifetimes. Adding DI later
/// (for tests, for plugins) is a refactor localised to this file.
/// </para>
/// <para>
/// The <see cref="IMainWindow"/> implementation (<see cref="MainWindow"/>) is the only
/// reference from <c>App/</c> into <c>Ui/</c>; everything else flows through the interface.
/// </para>
/// </remarks>
internal sealed class StampsApplicationContext : ApplicationContext
{
    private readonly FileLogger _logger;
    private readonly SettingsStore _settings;
    private readonly HotkeyService _hotkeys;
    private readonly StartupManager _startup;
    private readonly IMainWindow _mainWindow;
    private readonly TrayIconController _tray;
    private readonly NotifyIconNotifier _notifier;
    private readonly TweakRegistry _tweakRegistry;
    private readonly SingleInstance _singleInstance;
    private readonly SynchronizationContext _uiContext;
    private bool _disposed;

    public StampsApplicationContext(SingleInstance singleInstance, bool launchedAsAutostart)
    {
        ArgumentNullException.ThrowIfNull(singleInstance);
        if (!singleInstance.IsPrimary)
            throw new InvalidOperationException(
                "StampsApplicationContext must be constructed on the primary instance only.");

        _singleInstance = singleInstance;
        _uiContext = SynchronizationContext.Current
            ?? throw new InvalidOperationException(
                "StampsApplicationContext must be constructed on a thread with a WinForms sync context.");

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
        _tray = new TrayIconController(_mainWindow, _startup);
        _notifier = new NotifyIconNotifier(_tray.NotifyIcon);

        _singleInstance.ActivationRequested += OnActivationRequested;

        // Phase 3 will call _tweakRegistry.Register(new SnipTweak(...)) here and initialize
        // each tweak with a TweakHost bound to its own settings scope.

        if (ShouldOpenWindowOnStartup(launchedAsAutostart))
            _mainWindow.ShowOrFocus();
    }

    private bool ShouldOpenWindowOnStartup(bool launchedAsAutostart)
    {
        if (launchedAsAutostart) return false;
        if (_settings.App.LaunchMinimized) return false;
        return true;
    }

    private void OnActivationRequested(object? sender, EventArgs e)
    {
        // Event is raised on the single-instance listener thread; marshal to the UI thread.
        _uiContext.Post(_ => _mainWindow.ShowOrFocus(), state: null);
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _disposed = true;
            try { _logger.Info("Stamps shutting down."); } catch { /* ignore */ }

            _singleInstance.ActivationRequested -= OnActivationRequested;

            // Reverse order of construction.
            _tray.Dispose();
            _mainWindow.Dispose();
            _hotkeys.Dispose();
            _logger.Dispose();
        }
        base.Dispose(disposing);
    }
}
