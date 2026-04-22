namespace Stamps.App;

/// <summary>
/// Enforces single-instance behaviour across processes and provides a lightweight IPC
/// channel so a second launch can ask the running instance to open its main window.
/// </summary>
/// <remarks>
/// <para>
/// Backed by two named OS primitives: a <see cref="Mutex"/> for liveness detection and an
/// <see cref="EventWaitHandle"/> for activation signalling. The <c>Local\</c> prefix scopes
/// both to the current login session, which is what we want for a per-user tray app.
/// </para>
/// <para>
/// Only the primary instance starts the signal listener thread; secondary instances call
/// <see cref="SignalActivation"/> and exit immediately. <see cref="ActivationRequested"/>
/// fires on a background thread — subscribers must marshal to the UI thread.
/// </para>
/// </remarks>
public sealed class SingleInstance : IDisposable
{
    private readonly Mutex _mutex;
    private readonly EventWaitHandle _signal;
    private readonly Thread? _listener;
    private readonly CancellationTokenSource? _cts;
    private bool _disposed;

    /// <summary><c>true</c> if this process is the first (and therefore "owning") instance.</summary>
    public bool IsPrimary { get; }

    /// <summary>Fired on a background thread when another instance calls
    /// <see cref="SignalActivation"/>. Only fires on the primary instance.</summary>
    public event EventHandler? ActivationRequested;

    public SingleInstance(string baseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);

        _mutex = new Mutex(initiallyOwned: true, $"Local\\{baseName}", out bool createdNew);
        IsPrimary = createdNew;
        _signal = new EventWaitHandle(
            initialState: false,
            mode: EventResetMode.AutoReset,
            name: $"Local\\{baseName}-Activate");

        if (IsPrimary)
        {
            _cts = new CancellationTokenSource();
            _listener = new Thread(ListenLoop)
            {
                IsBackground = true,
                Name = "Stamps-SingleInstance-Listener",
            };
            _listener.Start(_cts.Token);
        }
    }

    /// <summary>Signals the primary instance (from a secondary instance) to activate its window.</summary>
    public void SignalActivation() => _signal.Set();

    private void ListenLoop(object? state)
    {
        var token = (CancellationToken)state!;
        var handles = new WaitHandle[] { _signal, token.WaitHandle };
        while (!token.IsCancellationRequested)
        {
            int which;
            try
            {
                which = WaitHandle.WaitAny(handles);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            if (which != 0) return;
            ActivationRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _cts?.Cancel();
        _listener?.Join(millisecondsTimeout: 500);
        _cts?.Dispose();
        _signal.Dispose();

        if (IsPrimary)
        {
            try { _mutex.ReleaseMutex(); }
            catch (ApplicationException) { /* not held — fine */ }
        }
        _mutex.Dispose();
    }
}
