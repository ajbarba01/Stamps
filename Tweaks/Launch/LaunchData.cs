namespace Stamps.Tweaks.Launch;

internal sealed class LaunchData
{
    public List<LaunchEntry> Launches { get; set; } = new();
}

internal sealed class LaunchEntry
{
    public string Id      { get; set; } = "";
    public string Title   { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string ExePath  { get; set; } = "";
    public bool   Enabled { get; set; } = true;
}
