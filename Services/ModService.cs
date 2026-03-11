using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class ModService
{
    private const string MetaFileName = "modinfo.json";

    private readonly GamePathService _pathService;
    private readonly SettingsService _settingsService;
    private AppSettings _settings;

    public ModService(GamePathService pathService, SettingsService settingsService)
    {
        _pathService = pathService;
        _settingsService = settingsService;
        _settings = _settingsService.Load();
    }

    public AppSettings Settings => _settings;

    public void SaveSettings()
    {
        _settingsService.Save(_settings);
    }

    public List<ModSourceInfo> BuildModSources(string? gamePath)
    {
        var sources = new List<ModSourceInfo>
        {
            new()
            {
                Name = "工具Mods",
                Path = GamePathService.ToolModsDir,
                IsSystem = true
            }
        };

        foreach (var dir in _settings.CustomModSourceDirs)
        {
            if (Directory.Exists(dir))
            {
                sources.Add(new ModSourceInfo
                {
                    Name = Path.GetFileName(dir),
                    Path = dir,
                    IsSystem = false
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(gamePath))
        {
            var pendingPath = _pathService.GetGamePendingModsDir(gamePath);
            Directory.CreateDirectory(pendingPath);
            sources.Add(new ModSourceInfo
            {
                Name = "游戏待生效",
                Path = pendingPath,
                IsSystem = true
            });
        }

        return sources
            .GroupBy(s => s.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    public bool AddCustomModSource(string path)
    {
        if (!Directory.Exists(path))
        {
            return false;
        }

        if (_settings.CustomModSourceDirs.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        _settings.CustomModSourceDirs.Add(path);
        SaveSettings();
        return true;
    }

    public void RemoveCustomModSource(string path)
    {
        _settings.CustomModSourceDirs = _settings.CustomModSourceDirs
            .Where(x => !string.Equals(x, path, StringComparison.OrdinalIgnoreCase))
            .ToList();
        SaveSettings();
    }

    public bool SaveModMetaByPath(string folderPath, string folderName, ModMetaInfo meta)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            return false;
        }

        try
        {
            var normalizedMeta = new ModMetaInfo
            {
                Name = meta.Name.Trim(),
                Version = meta.Version.Trim(),
                Detail = meta.Detail.Trim(),
                Remark = meta.Remark.Trim(),
                Author = meta.Author.Trim(),
                DownloadUrl = meta.DownloadUrl.Trim(),
                AuthorUrl = meta.AuthorUrl.Trim(),
                DetailUrl = meta.DetailUrl.Trim(),
                SocialUrl = meta.SocialUrl.Trim(),
                Description = meta.Description.Trim()
            };

            var metaFile = Path.Combine(folderPath, MetaFileName);
            var json = JsonSerializer.Serialize(normalizedMeta, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(metaFile, json);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ModMetaInfo LoadModMetaByPath(string folderPath, string folderName)
    {
        return LoadModMeta(folderPath, folderName);
    }

    public bool SaveModMeta(ModInfo mod, ModMetaInfo meta)
    {
        if (mod == null || string.IsNullOrWhiteSpace(mod.FolderPath) || !Directory.Exists(mod.FolderPath))
        {
            return false;
        }

        try
        {
            var normalizedMeta = new ModMetaInfo
            {
                Name = meta.Name.Trim(),
                Version = meta.Version.Trim(),
                Detail = meta.Detail.Trim(),
                Remark = meta.Remark.Trim(),
                Author = meta.Author.Trim(),
                DownloadUrl = meta.DownloadUrl.Trim(),
                AuthorUrl = meta.AuthorUrl.Trim(),
                DetailUrl = meta.DetailUrl.Trim(),
                SocialUrl = meta.SocialUrl.Trim(),
                Description = meta.Description.Trim()
            };

            var metaFile = Path.Combine(mod.FolderPath, MetaFileName);
            var json = JsonSerializer.Serialize(normalizedMeta, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(metaFile, json);

            mod.DisplayName = string.IsNullOrWhiteSpace(normalizedMeta.Name) ? mod.FolderName : normalizedMeta.Name;
            mod.Author = normalizedMeta.Author;
            mod.Version = normalizedMeta.Version;
            mod.Detail = normalizedMeta.Detail;
            mod.Remark = normalizedMeta.Remark;
            mod.DownloadUrl = normalizedMeta.DownloadUrl;
            mod.AuthorUrl = normalizedMeta.AuthorUrl;
            mod.DetailUrl = normalizedMeta.DetailUrl;
            mod.SocialUrl = normalizedMeta.SocialUrl;
            mod.Description = normalizedMeta.Description;
            mod.MetadataFilePath = metaFile;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ModMetaInfo LoadModMeta(string modFolderPath, string folderName)
    {
        var metaFile = Path.Combine(modFolderPath, MetaFileName);
        if (!File.Exists(metaFile))
        {
            var created = new ModMetaInfo
            {
                Name = folderName
            };
            TryWriteMetaFile(metaFile, created);
            return created;
        }

        try
        {
            var json = File.ReadAllText(metaFile);
            return JsonSerializer.Deserialize<ModMetaInfo>(json) ?? new ModMetaInfo { Name = folderName };
        }
        catch
        {
            return new ModMetaInfo { Name = folderName };
        }
    }

    public List<ModInfo> ScanModsFromSources(IEnumerable<ModSourceInfo> sources)
    {
        var mods = new List<ModInfo>();
        foreach (var source in sources)
        {
            mods.AddRange(ScanSingleSource(source, false));
        }

        return mods
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<ModInfo> ScanGameMods(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath) || !Directory.Exists(gameModsPath))
        {
            return new List<ModInfo>();
        }

        var source = new ModSourceInfo
        {
            Name = "游戏生效目录",
            Path = gameModsPath,
            IsSystem = true
        };

        return ScanSingleSource(source, true)
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public List<ModInfo> BuildUnifiedMods(List<ModInfo> sourceMods, List<ModInfo> gameMods)
    {
        var result = new List<ModInfo>();
        var gameMap = gameMods
            .GroupBy(x => x.FolderName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var sourceGroups = sourceMods
            .GroupBy(x => x.FolderName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var folderNames = gameMap.Keys
            .Union(sourceGroups.Keys, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var folderName in folderNames)
        {
            if (gameMap.TryGetValue(folderName, out var gameMod))
            {
                gameMod.IsEnabled = true;
                result.Add(gameMod);
                continue;
            }

            if (!sourceGroups.TryGetValue(folderName, out var candidates) || candidates.Count == 0)
            {
                continue;
            }

            foreach (var mod in DeduplicateByContent(candidates))
            {
                mod.IsEnabled = false;
                result.Add(mod);
            }
        }

        return result
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<ModInfo> ScanSingleSource(ModSourceInfo source, bool isFromGameDir)
    {
        var mods = new List<ModInfo>();
        if (!Directory.Exists(source.Path))
        {
            return mods;
        }

        foreach (var folder in Directory.GetDirectories(source.Path, "*", SearchOption.TopDirectoryOnly))
        {
            var folderName = Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(folderName))
            {
                continue;
            }

            var hasPck = Directory.GetFiles(folder, "*.pck", SearchOption.AllDirectories).Length > 0;
            var hasDll = Directory.GetFiles(folder, "*.dll", SearchOption.AllDirectories).Length > 0;
            if (!hasPck || !hasDll)
            {
                continue;
            }

            var lastWrite = Directory.GetLastWriteTime(folder);
            var size = CalculateDirectorySize(folder);
            var modKey = $"{source.Path}|{folderName}";
            var meta = LoadModMeta(folder, folderName);
            mods.Add(new ModInfo
            {
                ModKey = modKey,
                FolderName = folderName,
                FolderPath = folder,
                MetadataFilePath = Path.Combine(folder, MetaFileName),
                SourcePath = source.Path,
                SourceName = source.Name,
                DisplayName = string.IsNullOrWhiteSpace(meta.Name) ? folderName : meta.Name,
                Author = meta.Author,
                Version = meta.Version,
                Detail = meta.Detail,
                Remark = meta.Remark,
                DownloadUrl = meta.DownloadUrl,
                AuthorUrl = meta.AuthorUrl,
                DetailUrl = string.IsNullOrWhiteSpace(meta.DetailUrl) ? meta.SocialUrl : meta.DetailUrl,
                SocialUrl = meta.SocialUrl,
                Description = meta.Description,
                Size = size,
                ModifiedTime = lastWrite,
                IsFromGameDir = isFromGameDir,
                IsEnabled = true
            });
        }

        return mods;
    }

    public string BackupGameMods(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath) || !Directory.Exists(gameModsPath))
        {
            return string.Empty;
        }

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

    public int ApplyMods(string gamePath, IReadOnlyCollection<ModInfo> enabledMods)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath))
        {
            return 0;
        }

        Directory.CreateDirectory(gameModsPath);
        BackupGameMods(gamePath);

        foreach (var dir in Directory.GetDirectories(gameModsPath))
        {
            Directory.Delete(dir, true);
        }

        foreach (var file in Directory.GetFiles(gameModsPath))
        {
            File.Delete(file);
        }

        var copiedCount = 0;
        foreach (var mod in enabledMods)
        {
            if (!Directory.Exists(mod.FolderPath))
            {
                continue;
            }

            var targetPath = Path.Combine(gameModsPath, mod.FolderName);
            if (Directory.Exists(targetPath))
            {
                Directory.Delete(targetPath, true);
            }

            CopyDirectory(mod.FolderPath, targetPath);
            copiedCount++;
        }

        return copiedCount;
    }

    public bool ApplySingleMod(string gamePath, ModInfo mod)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath) || !Directory.Exists(mod.FolderPath))
        {
            return false;
        }

        Directory.CreateDirectory(gameModsPath);
        var targetPath = Path.Combine(gameModsPath, mod.FolderName);
        if (Directory.Exists(targetPath))
        {
            Directory.Delete(targetPath, true);
        }

        CopyDirectory(mod.FolderPath, targetPath);
        return true;
    }

    public int RemoveModsToTool(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath) || !Directory.Exists(gameModsPath))
        {
            return 0;
        }

        Directory.CreateDirectory(GamePathService.ToolModsDir);

        var count = 0;
        foreach (var folder in Directory.GetDirectories(gameModsPath, "*", SearchOption.TopDirectoryOnly))
        {
            var folderName = Path.GetFileName(folder);
            var target = Path.Combine(GamePathService.ToolModsDir, folderName);
            if (Directory.Exists(target))
            {
                Directory.Delete(target, true);
            }

            CopyDirectory(folder, target);
            count++;
        }

        return count;
    }

    public bool MoveGameModToPending(string gamePath, ModInfo mod)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath))
        {
            return false;
        }

        var sourceFolder = Path.Combine(gameModsPath, mod.FolderName);
        if (!Directory.Exists(sourceFolder))
        {
            return false;
        }

        var pendingPath = _pathService.GetGamePendingModsDir(gamePath);
        Directory.CreateDirectory(pendingPath);
        var targetFolder = Path.Combine(pendingPath, mod.FolderName);
        if (Directory.Exists(targetFolder))
        {
            Directory.Delete(targetFolder, true);
        }

        Directory.Move(sourceFolder, targetFolder);
        return true;
    }

    public bool MoveGameModToPendingByFolderName(string gamePath, string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
        {
            return false;
        }

        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath))
        {
            return false;
        }

        var sourceFolder = Path.Combine(gameModsPath, folderName);
        if (!Directory.Exists(sourceFolder))
        {
            return false;
        }

        var pendingPath = _pathService.GetGamePendingModsDir(gamePath);
        Directory.CreateDirectory(pendingPath);
        var targetFolder = Path.Combine(pendingPath, folderName);
        if (Directory.Exists(targetFolder))
        {
            Directory.Delete(targetFolder, true);
        }

        Directory.Move(sourceFolder, targetFolder);
        return true;
    }

    public HashSet<string> GetActiveModFolderNames(string gamePath)
    {
        var gameModsPath = _pathService.GetGameModsDir(gamePath);
        if (string.IsNullOrWhiteSpace(gameModsPath) || !Directory.Exists(gameModsPath))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return Directory.GetDirectories(gameModsPath, "*", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)!;
    }

    private static long CalculateDirectorySize(string folder)
    {
        return Directory
            .GetFiles(folder, "*", SearchOption.AllDirectories)
            .Sum(file => new FileInfo(file).Length);
    }

    private static List<ModInfo> DeduplicateByContent(List<ModInfo> mods)
    {
        var hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<ModInfo>();
        foreach (var mod in mods.OrderBy(x => x.SourceName, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase))
        {
            var hash = ComputeDirectoryContentHash(mod.FolderPath);
            if (hashSet.Add(hash))
            {
                result.Add(mod);
            }
        }

        return result;
    }

    private static string ComputeDirectoryContentHash(string folderPath)
    {
        if (!Directory.Exists(folderPath))
        {
            return string.Empty;
        }

        try
        {
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var files = Directory
                .GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in files)
            {
                var relativePath = Path.GetRelativePath(folderPath, file).Replace('\\', '/');
                var pathBytes = Encoding.UTF8.GetBytes(relativePath.ToLowerInvariant());
                hasher.AppendData(pathBytes);

                var fileInfo = new FileInfo(file);
                var lengthBytes = BitConverter.GetBytes(fileInfo.Length);
                hasher.AppendData(lengthBytes);

                var fileHash = SHA256.HashData(File.ReadAllBytes(file));
                hasher.AppendData(fileHash);
            }

            return Convert.ToHexString(hasher.GetHashAndReset());
        }
        catch
        {
            return $"fallback:{folderPath}";
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source))
        {
            var targetFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            var targetDir = Path.Combine(destination, Path.GetFileName(directory));
            CopyDirectory(directory, targetDir);
        }
    }

    private static void TryWriteMetaFile(string metaFile, ModMetaInfo meta)
    {
        try
        {
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(metaFile, json);
        }
        catch
        {
            // 忽略自动生成失败
        }
    }
}
