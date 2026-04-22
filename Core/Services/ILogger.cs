namespace Stamps.Core.Services;

/// <summary>
/// Minimal structured log sink. Implementations must be thread-safe and non-blocking for
/// the caller's common case — logging is called from UI and background threads alike.
/// </summary>
public interface ILogger
{
    void Info(string message);
    void Warn(string message, Exception? ex = null);
    void Error(string message, Exception? ex = null);
}
