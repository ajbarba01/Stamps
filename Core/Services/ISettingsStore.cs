namespace Stamps.Core.Services;

/// <summary>
/// Top-level settings facade. Holds the mutable <see cref="AppSettings"/> singleton and
/// vends per-tweak scopes that are pre-bound to their tweak id.
/// </summary>
public interface ISettingsStore
{
    /// <summary>Mutable app-wide settings. Call <see cref="SaveApp"/> to persist changes.</summary>
    AppSettings App { get; }

    /// <summary>Persists the current <see cref="App"/> values to disk atomically.</summary>
    void SaveApp();

    /// <summary>
    /// Returns the persistence scope for a given tweak. Repeated calls with the same id
    /// return equivalent scopes; implementations are not required to cache, but each scope
    /// reads and writes the same underlying file.
    /// </summary>
    ISettingsScope ScopeFor(string tweakId);
}
