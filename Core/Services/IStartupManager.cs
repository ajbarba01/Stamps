namespace Stamps.Core.Services;

/// <summary>
/// Abstraction over the "Start with Windows" toggle. Lives in <c>Core/</c> so the UI layer
/// can bind a settings control to it without depending on the platform-specific implementation
/// in <c>App/</c>.
/// </summary>
public interface IStartupManager
{
    /// <summary>Raised whenever the underlying state changes (in either direction).</summary>
    event EventHandler? Changed;

    /// <summary>Whether the app is currently registered to start with Windows.</summary>
    bool IsEnabled { get; }

    /// <summary>Enables or disables the registration. Implementations are expected to be
    /// idempotent and to raise <see cref="Changed"/> after a successful write.</summary>
    void SetEnabled(bool enable);
}
