using Stamps.Core;
using Stamps.Core.Services;

namespace Stamps.Tweaks.Snip;

internal sealed class SnipAction : IAction
{
    private readonly IHotkeyService _hotkeys;
    private readonly INotifier _notifier;
    private readonly ILogger _logger;
    private readonly ISettingsScope _scope;
    private readonly SnipSettings _settings;
    private IHotkeyBinding? _binding;
    private Hotkey? _hotkey;

    public string Id => "capture";
    public string Title => "Capture Region";
    public bool IsUserCreated => false;

    public bool Enabled
    {
        get => _settings.Enabled;
        set { _settings.Enabled = value; UpdateRegistration(); }
    }

    public Hotkey? Hotkey
    {
        get => _hotkey;
        set { _hotkey = value; _settings.Hotkey = value?.ToString() ?? ""; UpdateRegistration(); }
    }

    public IReadOnlyList<SettingDescriptor> Settings => Array.Empty<SettingDescriptor>();
    public SettingsValues Values { get; } = new();

    public SnipAction(
        IHotkeyService hotkeys,
        INotifier notifier,
        ILogger logger,
        ISettingsScope scope,
        SnipSettings settings)
    {
        _hotkeys = hotkeys;
        _notifier = notifier;
        _logger = logger;
        _scope = scope;
        _settings = settings;

        _hotkey = Stamps.Core.Hotkey.TryParse(settings.Hotkey, out var hk) ? hk : null;
        UpdateRegistration();
    }

    private void UpdateRegistration()
    {
        _binding?.Dispose();
        _binding = null;
        if (!_settings.Enabled || _hotkey is null) return;

        _binding = _hotkeys.TryRegister(_hotkey.Value, Invoke, out var result);
        if (result != HotkeyBindResult.Success)
            _logger.Warn($"Snip: failed to register hotkey {_hotkey} ({result}).");
    }

    public void PersistSettings() => _scope.Save(_settings);

    public void Invoke()
    {
        try
        {
            var window = new OverlayWindow();
            if (window.ShowDialog() == true && window.CapturedBitmap is { } bmp)
            {
                System.Windows.Clipboard.SetImage(bmp);
                _notifier.ShowBrief("Snip", "Screenshot copied to clipboard.");
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Snip: overlay failed.", ex);
        }
    }

    internal void Cleanup() => _binding?.Dispose();
}
