using System.Drawing;

namespace Stamps.Core;

/// <summary>
/// The unit of functionality in Stamps. A tweak bundles one or more <see cref="IAction"/>s,
/// optional tweak-level settings, and an on-disk README, behind a stable identifier.
/// </summary>
/// <remarks>
/// <para>
/// This interface is the primary public contract for third-party tweak authors. Keeping it
/// narrow and dependency-free (no UI, host, or service references) is what will make it
/// straightforward to extract into a public SDK assembly later without breaking changes.
/// </para>
/// <para>
/// Lifecycle:
/// <list type="number">
///   <item><see cref="Initialize"/> is called once at startup with a scoped host.</item>
///   <item>Actions are registered or unregistered as the user toggles the tweak.</item>
///   <item><see cref="Shutdown"/> is called once when the app exits; tweaks must release
///   any native resources there.</item>
/// </list>
/// </para>
/// </remarks>
public interface ITweak
{
    /// <summary>
    /// Stable, snake_case identifier unique across all tweaks (e.g., <c>"snip"</c>). Used as
    /// the filename of the tweak's settings file and the folder name of its assets. Must not
    /// change across versions.
    /// </summary>
    string Id { get; }

    /// <summary>Human-readable title displayed in the home page and detail header.</summary>
    string Title { get; }

    /// <summary>One-line summary shown on the home page tweak card.</summary>
    string Description { get; }

    /// <summary>Icon shown on the home card and detail header.</summary>
    Icon Icon { get; }

    /// <summary>
    /// SDK version this tweak was built against. When plugin loading is added, the host will
    /// refuse to load a tweak whose major version exceeds its own.
    /// </summary>
    Version SdkVersion { get; }

    /// <summary>Fixed-order list of actions shipped with the tweak. Never <c>null</c>.</summary>
    IReadOnlyList<IAction> BuiltInActions { get; }

    /// <summary>
    /// Factory used by the host to let the user add new actions, or <c>null</c> if this
    /// tweak does not support user-created actions.
    /// </summary>
    IActionFactory? UserActionFactory { get; }

    /// <summary>Declarative tweak-level settings (distinct from per-action settings).</summary>
    IReadOnlyList<SettingDescriptor> Settings { get; }

    /// <summary>
    /// Mutable runtime values for <see cref="Settings"/>. Same contract as
    /// <see cref="IAction.Values"/>: the host edits this bag and then calls
    /// <see cref="PersistSettings"/>.
    /// </summary>
    SettingsValues Values { get; }

    /// <summary>Flushes the current <see cref="Values"/> to the tweak's settings file. The
    /// tweak owns the file format; this method is the host's only persistence hook.</summary>
    void PersistSettings();

    /// <summary>
    /// Optional escape hatch for tweaks whose settings cannot be expressed declaratively.
    /// Return <c>null</c> (default) to have the host render <see cref="Settings"/>; return
    /// a control to replace the auto-rendered panel entirely.
    /// </summary>
    object? CreateCustomSettingsControl() => null;

    /// <summary>
    /// Called once, on the UI thread, before any actions are invoked. Implementations
    /// typically capture the host reference for later service access.
    /// </summary>
    void Initialize(ITweakHost host);

    /// <summary>
    /// Called once, on the UI thread, during application shutdown. Tweaks must release any
    /// unmanaged resources (native handles, background threads, cached bitmaps).
    /// </summary>
    void Shutdown();
}
