namespace Stamps.Core.Services;

/// <summary>
/// Reference-counted flag that tells <see cref="HotkeyService"/> to silently ignore
/// WM_HOTKEY messages while a hotkey capture control has keyboard focus. Uses a counter
/// so nested Suppress/Resume pairs (e.g., two focused boxes) stay balanced.
/// </summary>
public static class HotkeyCaptureSuppressor
{
    private static int _depth;

    public static bool IsSuppressed => _depth > 0;

    public static void Suppress() => Interlocked.Increment(ref _depth);

    public static void Resume() => Interlocked.Decrement(ref _depth);
}
