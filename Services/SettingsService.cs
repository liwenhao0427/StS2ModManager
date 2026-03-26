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
            settings.GithubSyncMods ??= new List<GithubSyncModItem>();
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
            settings.GithubSyncMods ??= new List<GithubSyncModItem>();

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(_settingsFilePath, json, Encoding.UTF8);
        }
        catch
        {
            // 忽略配置写入错误，避免影响主流程
        }
    }
}
