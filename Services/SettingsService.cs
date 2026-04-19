using System.IO;
using System.Text;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsFilePath;

    public SettingsService()
    {
        _settingsFilePath = Path.Combine(GamePathService.ConfigDir, "settings.json");
    }

    public AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings();
            }

            var json = File.ReadAllText(_settingsFilePath, Encoding.UTF8);
            var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            settings.CustomModSourceDirs = settings.CustomModSourceDirs
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            settings.ModAliases ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            settings.ModAliases = settings.ModAliases
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase);
            settings.GithubSyncMods ??= new List<GithubSyncModItem>();
            foreach (var item in settings.GithubSyncMods)
            {
                item.Tags ??= new List<string>();
                item.Tags = item.Tags
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            settings.FixedModTags ??= new List<string>();
            if (settings.FixedModTags.Count == 0)
            {
                settings.FixedModTags =
                [
                    "体验优化",
                    "前置依赖",
                    "开发工具",
                    "玩法扩展",
                    "新角色",
                    "新卡牌",
                    "难度调整",
                    "修改工具",
                    "皮肤"
                ];
            }
            else
            {
                settings.FixedModTags = settings.FixedModTags
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            settings.PreferredGithubSyncSourcePaths ??= new List<string>();
            settings.PreferredGithubSyncSourcePaths = settings.PreferredGithubSyncSourcePaths
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (string.IsNullOrWhiteSpace(settings.LanguageMode))
            {
                settings.LanguageMode = "system";
            }
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        try
        {
            Directory.CreateDirectory(GamePathService.ConfigDir);
            settings.CustomModSourceDirs = settings.CustomModSourceDirs
                .Where(Directory.Exists)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            settings.ModAliases ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            settings.ModAliases = settings.ModAliases
                .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .ToDictionary(x => x.Key.Trim(), x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase);
            settings.GithubSyncMods ??= new List<GithubSyncModItem>();
            foreach (var item in settings.GithubSyncMods)
            {
                item.Tags ??= new List<string>();
                item.Tags = item.Tags
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            settings.FixedModTags ??= new List<string>();
            if (settings.FixedModTags.Count == 0)
            {
                settings.FixedModTags =
                [
                    "体验优化",
                    "前置依赖",
                    "开发工具",
                    "玩法扩展",
                    "新角色",
                    "新卡牌",
                    "难度调整",
                    "修改工具",
                    "皮肤"
                ];
            }
            settings.PreferredGithubSyncSourcePaths ??= new List<string>();
            settings.PreferredGithubSyncSourcePaths = settings.PreferredGithubSyncSourcePaths
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
        }
        catch
        {
            // 忽略配置写入错误，避免影响主流程
        }
    }
}
