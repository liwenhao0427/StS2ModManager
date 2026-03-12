namespace StS2ModManager.Models;

public class AppSettings
{
    public List<string> CustomModSourceDirs { get; set; } = new();
    public Dictionary<string, string> ModAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? PreferredSteamId { get; set; }
    public string PreferredSaveSlot { get; set; } = "1";
    public string LanguageMode { get; set; } = "system";
}
