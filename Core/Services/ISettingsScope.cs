namespace Stamps.Core.Services;

/// <summary>
/// Per-tweak persistence scope. Each scope owns a single JSON file and is blind to every
/// other tweak's settings — this isolation is intentional and will matter more once third-
/// party tweaks land.
/// </summary>
public interface ISettingsScope
{
    /// <summary>The tweak this scope belongs to.</summary>
    string TweakId { get; }

    /// <summary>
    /// Loads the tweak's persisted state, or returns a freshly-constructed default if the
    /// file does not exist or cannot be parsed. Never throws for the "missing file" case.
    /// </summary>
    T Load<T>() where T : class, new();

    /// <summary>Persists the given state atomically. Throws only on unrecoverable I/O errors.</summary>
    void Save<T>(T value) where T : class;
}
