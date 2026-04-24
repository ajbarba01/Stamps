using System.IO;
using System.Windows;
using System.Windows.Threading;
using Stamps.Core;
using Stamps.Core.Services;
using Stamps.Tweaks.Alias;
using Stamps.Tweaks.Snip;
using Stamps.Tweaks.Launch;
using Stamps.Ui;
using Stamps.Ui.Theme;

namespace Stamps.App;

internal sealed class StampsApplicationContext : IDisposable, ITweakManager
{
    private sealed record TweakEntry(ITweak Tweak, TweakHost Host);

    private readonly FileLogger _logger;
    private readonly SettingsStore _settings;
    private readonly HotkeyService _hotkeys;
    private readonly StartupManager _startup;
    private readonly IMainWindow _mainWindow;
    private readonly TrayIconController _tray;
    private readonly NotifyIconNotifier _notifier;
    private readonly TweakRegistry _tweakRegistry;
    private readonly Dictionary<string, TweakEntry> _tweakEntries = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _activeTweakIds = new(StringComparer.OrdinalIgnoreCase);
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

        _mainWindow = new MainWindow(_tweakRegistry, _settings, _startup, logDir, this);
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
        AddTweak(new SnipTweak());
        AddTweak(new AliasTweak());
        AddTweak(new LaunchTweak());
    }

    private void AddTweak(ITweak tweak)
    {
        _tweakRegistry.Register(tweak);
        var host = new TweakHost(_hotkeys, _notifier, _settings.ScopeFor(tweak.Id), _logger);
        _tweakEntries[tweak.Id] = new TweakEntry(tweak, host);

        if (!_settings.App.DisabledTweakIds.Contains(tweak.Id))
        {
            tweak.Initialize(host);
            _activeTweakIds.Add(tweak.Id);
        }
    }

    public void Enable(string tweakId)
    {
        if (!_tweakEntries.TryGetValue(tweakId, out var entry)) return;
        if (_activeTweakIds.Contains(tweakId)) return;

        _settings.App.DisabledTweakIds.Remove(tweakId);
        _settings.SaveApp();
        entry.Tweak.Initialize(entry.Host);
        _activeTweakIds.Add(tweakId);
    }

    public void Disable(string tweakId)
    {
        if (!_tweakEntries.TryGetValue(tweakId, out var entry)) return;
        if (!_activeTweakIds.Contains(tweakId)) return;

        _settings.App.DisabledTweakIds.Add(tweakId);
        _settings.SaveApp();
        entry.Tweak.Shutdown();
        _activeTweakIds.Remove(tweakId);
    }

    private static bool ShouldOpenWindowOnStartup(bool launchedAsAutostart)
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

        foreach (var id in _activeTweakIds)
            if (_tweakEntries.TryGetValue(id, out var entry))
                try { entry.Tweak.Shutdown(); } catch { }

        _tray.Dispose();
        _mainWindow.Dispose();
        _hotkeys.Dispose();
        _logger.Dispose();
    }
}
