namespace StS2ModManager.Models;

public class AppSettings
{
    public string? PreferredGamePath { get; set; }
    public List<string> CustomModSourceDirs { get; set; } = new();
    public Dictionary<string, string> ModAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<GithubSyncModItem> GithubSyncMods { get; set; } = new();
    public List<string> PreferredGithubSyncSourcePaths { get; set; } = new();
    public string? PreferredSteamId { get; set; }
    public string PreferredSaveSlot { get; set; } = "1";
    public string LanguageMode { get; set; } = "system";
}
