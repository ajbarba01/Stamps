using System.Text.Json;

namespace Stamps.Core;

/// <summary>
/// A string-keyed, loosely-typed bag of setting values. Intended as the runtime counterpart
/// to a list of <see cref="SettingDescriptor"/>: the descriptors define the shape, and this
/// bag carries the current values. Typed accessors return a caller-supplied default when a
/// key is missing or the stored value cannot be coerced.
/// </summary>
/// <remarks>
/// Designed for JSON round-tripping. When deserialized, numeric and string values arrive as
/// <see cref="JsonElement"/>; the accessors transparently unwrap those. This keeps tweaks
/// decoupled from <c>System.Text.Json</c> while still playing well with it.
/// </remarks>
public sealed class SettingsValues
{
    private readonly Dictionary<string, object?> _values;

    public SettingsValues() => _values = new Dictionary<string, object?>(StringComparer.Ordinal);

    public SettingsValues(IDictionary<string, object?> initial)
    {
        ArgumentNullException.ThrowIfNull(initial);
        _values = new Dictionary<string, object?>(initial, StringComparer.Ordinal);
    }

    public bool ContainsKey(string key) => _values.ContainsKey(key);

    public void Set(string key, object? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _values[key] = value;
    }

    public bool GetBool(string key, bool @default = false)
    {
        if (!_values.TryGetValue(key, out var raw) || raw is null) return @default;
        return raw switch
        {
            bool b                                         => b,
            JsonElement e when e.ValueKind == JsonValueKind.True  => true,
            JsonElement e when e.ValueKind == JsonValueKind.False => false,
            _                                              => @default,
        };
    }

    public string GetString(string key, string @default = "")
    {
        if (!_values.TryGetValue(key, out var raw) || raw is null) return @default;
        return raw switch
        {
            string s                                                 => s,
            JsonElement e when e.ValueKind == JsonValueKind.String   => e.GetString() ?? @default,
            _                                                        => raw.ToString() ?? @default,
        };
    }

    public double GetNumber(string key, double @default = 0)
    {
        if (!_values.TryGetValue(key, out var raw) || raw is null) return @default;
        return raw switch
        {
            double d                                                 => d,
            int i                                                    => i,
            long l                                                   => l,
            JsonElement e when e.ValueKind == JsonValueKind.Number
                && e.TryGetDouble(out var parsed)                    => parsed,
            _                                                        => @default,
        };
    }

    public Hotkey? GetHotkey(string key)
    {
        var text = GetString(key, "");
        return Hotkey.TryParse(text, out var hk) ? hk : null;
    }

    public IReadOnlyDictionary<string, object?> ToDictionary() => _values;
}
