namespace Stamps.Tweaks.Alias;

internal sealed class AliasData
{
    public List<AliasEntry> Aliases { get; set; } = new();
}

internal sealed class AliasEntry
{
    public string Id      { get; set; } = "";
    public string Title   { get; set; } = "";
    public string Trigger { get; set; } = "";
    public string Target  { get; set; } = "";
    public bool   Enabled { get; set; } = true;
}
