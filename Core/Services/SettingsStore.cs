using System.Text.Json;

namespace Stamps.Core.Services;

/// <summary>
/// JSON-file-backed <see cref="ISettingsStore"/>. Per-tweak files live in
/// <c>&lt;dataDir&gt;/tweaks/&lt;id&gt;.json</c>; the central file lives at
/// <c>&lt;dataDir&gt;/settings.json</c>. Writes are atomic: a temp file is written then
/// swapped over the target via <see cref="File.Replace(string, string, string?)"/>.
/// </summary>
/// <remarks>
/// Parse failures log a warning and fall back to defaults — a corrupted settings file must
/// never prevent the app from starting. Callers are still responsible for calling
/// <see cref="SaveApp"/> or <see cref="ISettingsScope.Save{T}"/> after mutating state.
/// </remarks>
internal sealed class SettingsStore : ISettingsStore
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly ILogger _logger;
    private readonly string _appPath;
    private readonly string _tweaksDir;

    public AppSettings App { get; private set; }

    public SettingsStore(string dataDir, ILogger logger)
    {
        ArgumentException.ThrowIfNullOrEmpty(dataDir);
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;
        Directory.CreateDirectory(dataDir);
        _appPath = Path.Combine(dataDir, "settings.json");
        _tweaksDir = Path.Combine(dataDir, "tweaks");
        Directory.CreateDirectory(_tweaksDir);

        App = LoadApp();
    }

    public void SaveApp()
    {
        var json = JsonSerializer.Serialize(App, JsonOpts);
        WriteAtomic(_appPath, json);
    }

    public ISettingsScope ScopeFor(string tweakId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tweakId);
        return new Scope(this, tweakId);
    }

    private AppSettings LoadApp()
    {
        if (!File.Exists(_appPath)) return new AppSettings();
        try
        {
            var text = File.ReadAllText(_appPath);
            return JsonSerializer.Deserialize<AppSettings>(text, JsonOpts) ?? new AppSettings();
        }
        catch (Exception ex)
        {
            _logger.Warn("Failed to load app settings; using defaults.", ex);
            return new AppSettings();
        }
    }

    private string TweakPath(string id) => Path.Combine(_tweaksDir, id + ".json");

    private void WriteAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        if (File.Exists(path))
            File.Replace(tmp, path, destinationBackupFileName: null);
        else
            File.Move(tmp, path);
    }

    private sealed class Scope : ISettingsScope
    {
        private readonly SettingsStore _owner;
        public string TweakId { get; }

        public Scope(SettingsStore owner, string tweakId)
        {
            _owner = owner;
            TweakId = tweakId;
        }

        public T Load<T>() where T : class, new()
        {
            var path = _owner.TweakPath(TweakId);
            if (!File.Exists(path)) return new T();
            try
            {
                var text = File.ReadAllText(path);
                return JsonSerializer.Deserialize<T>(text, JsonOpts) ?? new T();
            }
            catch (Exception ex)
            {
                _owner._logger.Warn($"Failed to load settings for tweak '{TweakId}'; using defaults.", ex);
                return new T();
            }
        }

        public void Save<T>(T value) where T : class
        {
            ArgumentNullException.ThrowIfNull(value);
            var json = JsonSerializer.Serialize(value, JsonOpts);
            _owner.WriteAtomic(_owner.TweakPath(TweakId), json);
        }
    }
}
