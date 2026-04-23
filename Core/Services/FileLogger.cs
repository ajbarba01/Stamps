using System.IO;

namespace Stamps.Core.Services;

/// <summary>
/// File-backed logger that appends to a daily-rotated log file. Thread-safe via a single
/// lock on the write path; good enough for this app's volume (human-triggered actions).
/// </summary>
/// <remarks>
/// <para>
/// Log files are named <c>stamps-YYYY-MM-DD.log</c> and live in the configured directory,
/// which is created on construction if missing. Rotation is lazy: the first write on a new
/// day closes the previous day's file and opens the new one.
/// </para>
/// <para>
/// If the log file cannot be opened (e.g., antivirus lock), the logger silently drops the
/// entry rather than crashing the app. Logging must never be the reason the app fails.
/// </para>
/// </remarks>
internal sealed class FileLogger : ILogger, IDisposable
{
    private readonly object _lock = new();
    private readonly string _directory;
    private StreamWriter? _writer;
    private DateOnly _currentDay;
    private bool _disposed;

    public FileLogger(string directory)
    {
        ArgumentException.ThrowIfNullOrEmpty(directory);
        _directory = directory;
        Directory.CreateDirectory(_directory);
        Rotate(DateOnly.FromDateTime(DateTime.Now));
    }

    public void Info(string message) => Write("INFO", message, null);
    public void Warn(string message, Exception? ex = null) => Write("WARN", message, ex);
    public void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

    private void Write(string level, string message, Exception? ex)
    {
        lock (_lock)
        {
            if (_disposed) return;

            var now = DateTime.Now;
            var today = DateOnly.FromDateTime(now);
            if (today != _currentDay) Rotate(today);
            if (_writer is null) return;

            try
            {
                _writer.Write($"{now:HH:mm:ss.fff} [{level}] {message}");
                if (ex is not null)
                    _writer.Write($"  --> {ex.GetType().Name}: {ex.Message}");
                _writer.WriteLine();
                _writer.Flush();
            }
            catch
            {
                // Never let a logging failure propagate. Drop the entry.
            }
        }
    }

    private void Rotate(DateOnly day)
    {
        try { _writer?.Dispose(); } catch { /* ignore */ }
        _writer = null;
        _currentDay = day;

        var path = Path.Combine(_directory, $"stamps-{day:yyyy-MM-dd}.log");
        try
        {
            var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            _writer = new StreamWriter(stream) { AutoFlush = false };
        }
        catch
        {
            _writer = null;
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer?.Dispose(); } catch { /* ignore */ }
            _writer = null;
        }
    }
}
