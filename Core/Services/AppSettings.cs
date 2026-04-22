namespace Stamps.Core.Services;

/// <summary>
/// App-wide settings persisted to <c>settings.json</c>. Distinct from per-tweak settings.
/// </summary>
/// <remarks>
/// Start-on-startup is deliberately <em>not</em> stored here — the HKCU <c>Run</c> registry
/// key is the canonical source of that state, managed by <c>StartupManager</c>, to avoid a
/// second source of truth that can drift.
/// </remarks>
public sealed class AppSettings
{
    /// <summary>Schema version for forward migration.</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>If <c>true</c>, manual launches do not open the window (tray only).</summary>
    public bool LaunchMinimized { get; set; } = false;

    /// <summary>Notification style: <c>"brief"</c>, <c>"silent"</c>, or future values.</summary>
    public string NotificationStyle { get; set; } = "brief";

    /// <summary>UI theme: <c>"system"</c>, <c>"light"</c>, <c>"dark"</c>.</summary>
    public string Theme { get; set; } = "system";

    /// <summary>Last tweak opened in the UI; used to restore context on next launch.</summary>
    public string? LastOpenedTweakId { get; set; }

    /// <summary>
    /// Ids of tweaks the user has explicitly enabled. Tweaks not present here are treated as
    /// enabled by default (so adding a new tweak does not require a user opt-in); ids
    /// removed via <see cref="DisabledTweakIds"/> override that default.
    /// </summary>
    public HashSet<string> DisabledTweakIds { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
