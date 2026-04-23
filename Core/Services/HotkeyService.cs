using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Stamps.Core.Services;

internal sealed class HotkeyService : IHotkeyService, IDisposable
{
    private const int WmHotKey = 0x0312;
    private const uint ModNoRepeat = 0x4000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private readonly ILogger _logger;
    private readonly MessageWindow _window;
    private readonly Dictionary<int, Binding> _byId = new();
    private readonly Dictionary<Hotkey, int> _byHotkey = new();
    private int _nextId = 1;
    private bool _disposed;

    public HotkeyService(ILogger logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
        _window = new MessageWindow(OnHotkeyMessage);
    }

    public bool IsRegisteredByUs(Hotkey hotkey) => _byHotkey.ContainsKey(hotkey);

    public IHotkeyBinding? TryRegister(Hotkey hotkey, Action onPressed, out HotkeyBindResult result)
    {
        ArgumentNullException.ThrowIfNull(onPressed);
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_byHotkey.ContainsKey(hotkey))
        {
            result = HotkeyBindResult.AlreadyBoundByStamps;
            return null;
        }

        int id = _nextId++;
        uint mods = (uint)hotkey.Modifiers | ModNoRepeat;
        if (!RegisterHotKey(_window.Handle, id, mods, (uint)KeyInterop.VirtualKeyFromKey(hotkey.Key)))
        {
            _logger.Warn($"RegisterHotKey refused {hotkey} (Win32 error {Marshal.GetLastWin32Error()}).");
            result = HotkeyBindResult.SystemRefused;
            return null;
        }

        var binding = new Binding(id, hotkey, onPressed, this);
        _byId[id] = binding;
        _byHotkey[hotkey] = id;
        result = HotkeyBindResult.Success;
        return binding;
    }

    private void Unregister(int id)
    {
        if (_disposed) return;
        if (!_byId.Remove(id, out var binding)) return;
        _byHotkey.Remove(binding.Hotkey);
        UnregisterHotKey(_window.Handle, id);
    }

    private void OnHotkeyMessage(int id)
    {
        if (HotkeyCaptureSuppressor.IsSuppressed) return;
        if (!_byId.TryGetValue(id, out var binding)) return;
        try { binding.OnPressed(); }
        catch (Exception ex) { _logger.Error($"Hotkey callback for {binding.Hotkey} threw.", ex); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var id in _byId.Keys.ToArray())
            UnregisterHotKey(_window.Handle, id);
        _byId.Clear();
        _byHotkey.Clear();
        _window.Dispose();
    }

    private sealed class Binding : IHotkeyBinding
    {
        private readonly HotkeyService _owner;
        private bool _disposed;

        public int Id { get; }
        public Hotkey Hotkey { get; }
        public Action OnPressed { get; }

        public Binding(int id, Hotkey hotkey, Action onPressed, HotkeyService owner)
        {
            Id = id; Hotkey = hotkey; OnPressed = onPressed; _owner = owner;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner.Unregister(Id);
        }
    }

    private sealed class MessageWindow : IDisposable
    {
        private readonly HwndSource _source;
        private readonly Action<int> _onHotkey;

        public IntPtr Handle => _source.Handle;

        public MessageWindow(Action<int> onHotkey)
        {
            _onHotkey = onHotkey;
            _source = new HwndSource(new HwndSourceParameters("Stamps-HotkeyWindow")
            {
                WindowStyle = 0,
                ExtendedWindowStyle = 0,
                Width = 0,
                Height = 0,
                PositionX = -32000,
                PositionY = -32000,
            });
            _source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmHotKey)
            {
                _onHotkey((int)wParam);
                handled = true;
            }
            return IntPtr.Zero;
        }

        public void Dispose()
        {
            _source.RemoveHook(WndProc);
            _source.Dispose();
        }
    }
}
