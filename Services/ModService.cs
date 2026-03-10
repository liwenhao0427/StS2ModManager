using System.IO;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class ModService
{
    private readonly GamePathService _pathService;
    private Dictionary<string, string> _aliases = new();

    public ModService(GamePathService pathService)
    {
        _pathService = pathService;
        LoadAliases();
    }

    private void LoadAliases()
    {
        try
        {
            if (File.Exists(GamePathService.AliasesFile))
            {
                var json = File.ReadAllText(GamePathService.AliasesFile);
                _aliases = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
            }
        }
        catch
        {
            _aliases = new();
        }
    }

    public void SaveAliases()
    {
        try
        {
            var json = JsonSerializer.Serialize(_aliases, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(GamePathService.AliasesFile, json);
        }
        catch { }
    }

    public void SetAlias(string fileName, string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
            _aliases.Remove(fileName);
        else
            _aliases[fileName] = alias;
        SaveAliases();
    }

    public string GetDisplayName(string fileName)
    {
        return _aliases.TryGetValue(fileName, out var alias) ? alias : fileName;
    }

    public List<ModInfo> ScanToolMods()
    {
        return ScanModsDirectory(GamePathService.ToolModsDir, false);
    }

    public List<ModInfo> ScanGameMods(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (gameModsPath == null) return new List<ModInfo>();
        return ScanModsDirectory(gameModsPath, true);
    }

    private List<ModInfo> ScanModsDirectory(string modsPath, bool isFromGameDir)
    {
        var mods = new List<ModInfo>();
        if (!Directory.Exists(modsPath)) return mods;

        foreach (var file in Directory.GetFiles(modsPath, "*.pck", SearchOption.AllDirectories))
        {
            var fileInfo = new FileInfo(file);
            var fileName = Path.GetFileName(file);
            mods.Add(new ModInfo
            {
                FileName = fileName,
                DisplayName = GetDisplayName(fileName),
                FullPath = file,
                Size = fileInfo.Length,
                ModifiedTime = fileInfo.LastWriteTime,
                IsFromGameDir = isFromGameDir
            });
        }

        return mods;
    }

    public string BackupGameMods(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (gameModsPath == null || !Directory.Exists(gameModsPath))
            return string.Empty;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(GamePathService.BackupDir, "Mods", timestamp);

        try
        {
            CopyDirectory(gameModsPath, backupPath);
            return backupPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    public void ApplyMods(string gamePath, List<ModInfo> enabledMods)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (gameModsPath == null) return;

        // 备份现有Mods
        BackupGameMods(gamePath);

        // 清空游戏Mods目录
        foreach (var file in Directory.GetFiles(gameModsPath, "*.pck", SearchOption.AllDirectories))
        {
            File.Delete(file);
        }

        // 复制选中的Mods
        foreach (var mod in enabledMods)
        {
            var destPath = Path.Combine(gameModsPath, mod.FileName);
            if (File.Exists(mod.FullPath) && mod.FullPath != destPath)
            {
                File.Copy(mod.FullPath, destPath, true);
            }
        }
    }

    public void RemoveModsToTool(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (gameModsPath == null || !Directory.Exists(gameModsPath)) return;

        // 复制游戏Mods到工具目录
        foreach (var file in Directory.GetFiles(gameModsPath, "*.pck", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            var destPath = Path.Combine(GamePathService.ToolModsDir, fileName);
            File.Copy(file, destPath, true);
        }
    }

    private void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src))
        {
            var destFile = Path.Combine(dest, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }
        foreach (var dir in Directory.GetDirectories(src))
        {
            var destDir = Path.Combine(dest, Path.GetFileName(dir));
            CopyDirectory(dir, destDir);
        }
    }
}
