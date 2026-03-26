namespace StS2ModManager.Models;

public class AppSettings
{
    public string? PreferredGamePath { get; set; }
    public List<string> CustomModSourceDirs { get; set; } = new();
    public Dictionary<string, string> ModAliases { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<GithubSyncModItem> GithubSyncMods { get; set; } = new();
    public List<string> FixedModTags { get; set; } = new()
    {
        "体验优化",
        "前置依赖",
        "开发工具",
        "玩法扩展",
        "新角色",
        "新卡牌",
        "难度调整",
        "修改工具",
        "皮肤"
    };
    public List<string> PreferredGithubSyncSourcePaths { get; set; } = new();
    public string? PreferredSteamId { get; set; }
    public string PreferredSaveSlot { get; set; } = "1";
    public string LanguageMode { get; set; } = "system";
}
