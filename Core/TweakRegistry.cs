namespace Stamps.Core;

/// <summary>
/// The authoritative in-memory list of registered tweaks. In Phase 1 this is populated
/// explicitly by the composition root; in a later phase a plugin loader may register
/// additional tweaks discovered on disk.
/// </summary>
/// <remarks>
/// Not thread-safe by design — all registration happens on the UI thread during startup.
/// </remarks>
public sealed class TweakRegistry
{
    private readonly List<ITweak> _tweaks = new();
    private readonly Dictionary<string, ITweak> _byId =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registered tweaks in insertion order.</summary>
    public IReadOnlyList<ITweak> Tweaks => _tweaks;

    /// <summary>
    /// Adds a tweak to the registry. Throws if the tweak's id is missing, blank, or already
    /// registered. Registration alone does not initialize the tweak.
    /// </summary>
    public void Register(ITweak tweak)
    {
        ArgumentNullException.ThrowIfNull(tweak);
        if (string.IsNullOrWhiteSpace(tweak.Id))
            throw new ArgumentException("Tweak Id must be non-empty.", nameof(tweak));
        if (_byId.ContainsKey(tweak.Id))
            throw new InvalidOperationException($"A tweak with id '{tweak.Id}' is already registered.");

        _byId[tweak.Id] = tweak;
        _tweaks.Add(tweak);
    }

    /// <summary>Looks up a tweak by id; returns <c>null</c> if not found.</summary>
    public ITweak? Find(string id) =>
        string.IsNullOrEmpty(id) ? null : _byId.GetValueOrDefault(id);
}
