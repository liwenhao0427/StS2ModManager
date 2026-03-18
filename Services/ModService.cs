using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class ModService
{
    private const string MetaFileName = "modinfo.json";
    private static readonly JsonSerializerOptions JsonIndentedOptions = new()
    {
        WriteIndented = true
    };

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
                Tag = meta.Tag.Trim(),
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
            var json = JsonSerializer.Serialize(normalizedMeta, JsonIndentedOptions);
            File.WriteAllText(metaFile, json, Encoding.UTF8);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ModMetaInfo LoadModMetaByPath(string folderPath, string folderName)
    {
        var hasPck = Directory.Exists(folderPath)
            && Directory.GetFiles(folderPath, "*.pck", SearchOption.AllDirectories).Length > 0;
        var hasDll = Directory.Exists(folderPath)
            && Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories).Length > 0;
        return LoadModMeta(folderPath, folderName, hasPck, hasDll);
    }

    public bool ExtractSameNameJsonFromDllAndUpdateMeta(string folderPath, string folderName, out string sameNameJsonPath, out string errorMessage)
    {
        sameNameJsonPath = string.Empty;
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath))
        {
            errorMessage = "Mod目录不存在";
            return false;
        }

        var modBaseName = ResolveModBaseName(folderPath, folderName);
        var hasPck = Directory.GetFiles(folderPath, "*.pck", SearchOption.AllDirectories).Length > 0;
        var hasDll = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories).Length > 0;
        var dllPath = ResolvePrimaryDllPath(folderPath, modBaseName);
        if (string.IsNullOrWhiteSpace(dllPath))
        {
            errorMessage = "未找到可用的dll文件";
            return false;
        }

        ModSameNameMeta? extractedMeta = TryExtractSameNameMetaFromDll(dllPath, modBaseName);
        sameNameJsonPath = Path.Combine(folderPath, $"{modBaseName}.json");
        if (extractedMeta == null)
        {
            extractedMeta = TryReadSameNameMeta(sameNameJsonPath);
        }

        if (extractedMeta == null)
        {
            extractedMeta = TryReadManifestMeta(Path.Combine(folderPath, "mod_manifest.json"), modBaseName, folderPath);
        }

        if (extractedMeta == null)
        {
            errorMessage = "未找到可用的提取来源（dll资源/同名json/mod_manifest）";
            return false;
        }

        extractedMeta = NormalizeSameNameMeta(extractedMeta, modBaseName, folderName, hasPck, hasDll);
        TryWriteSameNameMetaFile(sameNameJsonPath, extractedMeta);

        var parsedMeta = ConvertSameNameMetaToCustomMeta(extractedMeta, modBaseName, folderName);
        var customMetaPath = Path.Combine(folderPath, MetaFileName);
        var existingCustom = TryReadCustomMeta(customMetaPath, folderName) ?? new ModMetaInfo();
        var merged = MergeCustomMeta(existingCustom, parsedMeta);
        TryWriteMetaFile(customMetaPath, merged);
        return true;
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
                Tag = meta.Tag.Trim(),
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
            var json = JsonSerializer.Serialize(normalizedMeta, JsonIndentedOptions);
            File.WriteAllText(metaFile, json, Encoding.UTF8);

            mod.DisplayName = string.IsNullOrWhiteSpace(normalizedMeta.Name) ? mod.FolderName : normalizedMeta.Name;
            mod.Tag = normalizedMeta.Tag;
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

    public ModMetaInfo LoadModMeta(string modFolderPath, string folderName, bool hasPck, bool hasDll)
    {
        var metaFile = Path.Combine(modFolderPath, MetaFileName);
        var modBaseName = ResolveModBaseName(modFolderPath, folderName);
        var modSameNameMetaFile = Path.Combine(modFolderPath, $"{modBaseName}.json");

        var customMeta = TryReadCustomMeta(metaFile, folderName);
        var sameNameMeta = TryReadSameNameMeta(modSameNameMetaFile);

        if (sameNameMeta == null)
        {
            sameNameMeta = BuildSameNameMetaFromCustom(customMeta, modBaseName, folderName, hasPck, hasDll);
        }
        else
        {
            sameNameMeta = NormalizeSameNameMeta(sameNameMeta, modBaseName, folderName, hasPck, hasDll);
        }

        TryWriteSameNameMetaFile(modSameNameMetaFile, sameNameMeta);

        if (customMeta != null)
        {
            return customMeta;
        }

        var created = ConvertSameNameMetaToCustomMeta(sameNameMeta, modBaseName, folderName);
        TryWriteMetaFile(metaFile, created);
        return created;
    }

    private static string ResolveModBaseName(string modFolderPath, string folderName)
    {
        if (!Directory.Exists(modFolderPath))
        {
            return string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim();
        }

        var dllNames = Directory
            .GetFiles(modFolderPath, "*.dll", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var pckNames = Directory
            .GetFiles(modFolderPath, "*.pck", SearchOption.AllDirectories)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (dllNames.Count > 0 && pckNames.Count > 0)
        {
            var sameName = dllNames.FirstOrDefault(name => pckNames.Contains(name, StringComparer.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(sameName))
            {
                return sameName;
            }
        }

        if (dllNames.Count > 0)
        {
            return dllNames[0];
        }

        if (pckNames.Count > 0)
        {
            return pckNames[0];
        }

        return string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim();
    }

    private static string? ResolvePrimaryDllPath(string modFolderPath, string modBaseName)
    {
        var dllFiles = Directory.GetFiles(modFolderPath, "*.dll", SearchOption.AllDirectories)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (dllFiles.Count == 0)
        {
            return null;
        }

        var exactNamePath = dllFiles.FirstOrDefault(x =>
            string.Equals(Path.GetFileNameWithoutExtension(x), modBaseName, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(exactNamePath) ? dllFiles[0] : exactNamePath;
    }

    private static ModSameNameMeta? TryExtractSameNameMetaFromDll(string dllPath, string modBaseName)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "StS2ModManager", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var tempDllPath = Path.Combine(tempDir, Path.GetFileName(dllPath));

        try
        {
            File.Copy(dllPath, tempDllPath, true);
            var loadContext = new ModAssemblyLoadContext();
            var assembly = loadContext.LoadFromAssemblyPath(tempDllPath);
            var resourceNames = assembly.GetManifestResourceNames();
            if (resourceNames.Length == 0)
            {
                loadContext.Unload();
                return null;
            }

            var preferredSuffix = $"{modBaseName}.json";
            var preferredResources = resourceNames
                .Where(x => x.EndsWith(preferredSuffix, StringComparison.OrdinalIgnoreCase))
                .Concat(resourceNames.Where(x => x.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
                .Concat(resourceNames)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var targetResource in preferredResources)
            {
                using var stream = assembly.GetManifestResourceStream(targetResource);
                if (stream == null)
                {
                    continue;
                }

                using var reader = new StreamReader(stream, Encoding.UTF8, true);
                var content = reader.ReadToEnd();
                if (TryParseSameNameMeta(content, modBaseName, out var meta))
                {
                    loadContext.Unload();
                    return meta;
                }
            }

            loadContext.Unload();
            return null;
        }
        catch
        {
            return null;
        }
        finally
        {
            try
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
            catch
            {
                // 忽略临时目录清理失败
            }
        }
    }

    private static ModSameNameMeta? TryReadManifestMeta(string manifestPath, string modBaseName, string folderPath)
    {
        if (!File.Exists(manifestPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(manifestPath, Encoding.UTF8);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            var root = doc.RootElement;
            var id = ReadStringProperty(root, "id");
            var name = ReadStringProperty(root, "name");
            var author = ReadStringProperty(root, "author");
            var description = ReadStringProperty(root, "description");
            var version = ReadStringProperty(root, "version");
            var affectsGameplay = ReadBoolProperty(root, "affects_gameplay") ?? true;
            var hasPck = ReadBoolProperty(root, "has_pck")
                         ?? Directory.GetFiles(folderPath, "*.pck", SearchOption.AllDirectories).Length > 0;
            var hasDll = ReadBoolProperty(root, "has_dll")
                         ?? Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories).Length > 0;
            var dependencies = ReadStringArrayProperty(root, "dependencies");

            var candidate = new ModSameNameMeta
            {
                Id = string.IsNullOrWhiteSpace(id) ? modBaseName : id!,
                Name = string.IsNullOrWhiteSpace(name) ? (string.IsNullOrWhiteSpace(id) ? modBaseName : id!) : name!,
                Author = author ?? string.Empty,
                Description = description ?? string.Empty,
                Version = version ?? string.Empty,
                HasPck = hasPck,
                HasDll = hasDll,
                Dependencies = dependencies,
                AffectsGameplay = affectsGameplay
            };

            return IsValidSameNameMeta(candidate) ? candidate : null;
        }
        catch
        {
            return null;
        }
    }

    private static ModMetaInfo? TryReadCustomMeta(string metaFile, string folderName)
    {
        if (!File.Exists(metaFile))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metaFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<ModMetaInfo>(json) ?? new ModMetaInfo { Name = folderName };
        }
        catch
        {
            return null;
        }
    }

    private static ModSameNameMeta? TryReadSameNameMeta(string sameNameMetaFile)
    {
        if (!File.Exists(sameNameMetaFile))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(sameNameMetaFile, Encoding.UTF8);
            return JsonSerializer.Deserialize<ModSameNameMeta>(json);
        }
        catch
        {
            return null;
        }
    }

    private static ModSameNameMeta BuildSameNameMetaFromCustom(ModMetaInfo? customMeta, string modBaseName, string folderName, bool hasPck, bool hasDll)
    {
        var id = string.IsNullOrWhiteSpace(modBaseName)
            ? (string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim())
            : modBaseName.Trim();
        var name = customMeta == null || string.IsNullOrWhiteSpace(customMeta.Name) ? id : customMeta.Name.Trim();
        var author = customMeta == null ? string.Empty : customMeta.Author.Trim();
        var description = customMeta == null ? string.Empty : customMeta.Description.Trim();
        var version = customMeta == null ? string.Empty : customMeta.Version.Trim();

        return new ModSameNameMeta
        {
            Id = id,
            Name = name,
            Author = author,
            Description = description,
            Version = version,
            HasPck = hasPck,
            HasDll = hasDll,
            Dependencies = [],
            AffectsGameplay = true
        };
    }

    private static ModSameNameMeta NormalizeSameNameMeta(ModSameNameMeta source, string modBaseName, string folderName, bool hasPck, bool hasDll)
    {
        var id = string.IsNullOrWhiteSpace(modBaseName)
            ? (string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim())
            : modBaseName.Trim();

        return new ModSameNameMeta
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(source.Name) ? id : source.Name.Trim(),
            Author = source.Author?.Trim() ?? string.Empty,
            Description = source.Description?.Trim() ?? string.Empty,
            Version = source.Version?.Trim() ?? string.Empty,
            HasPck = hasPck,
            HasDll = hasDll,
            Dependencies = source.Dependencies ?? [],
            AffectsGameplay = source.AffectsGameplay
        };
    }

    private static ModMetaInfo ConvertSameNameMetaToCustomMeta(ModSameNameMeta? sameNameMeta, string modBaseName, string folderName)
    {
        if (sameNameMeta == null)
        {
            var defaultName = string.IsNullOrWhiteSpace(modBaseName) ? folderName : modBaseName;
            return new ModMetaInfo { Name = defaultName };
        }

        var fallbackName = !string.IsNullOrWhiteSpace(sameNameMeta.Id)
            ? sameNameMeta.Id.Trim()
            : (string.IsNullOrWhiteSpace(modBaseName) ? folderName : modBaseName);
        return new ModMetaInfo
        {
            Name = string.IsNullOrWhiteSpace(sameNameMeta.Name) ? fallbackName : sameNameMeta.Name.Trim(),
            Author = sameNameMeta.Author?.Trim() ?? string.Empty,
            Version = sameNameMeta.Version?.Trim() ?? string.Empty,
            Description = sameNameMeta.Description?.Trim() ?? string.Empty,
            Detail = sameNameMeta.Description?.Trim() ?? string.Empty
        };
    }

    private static bool TryParseSameNameMeta(string jsonContent, string modBaseName, out ModSameNameMeta meta)
    {
        meta = null!;
        try
        {
            var candidate = JsonSerializer.Deserialize<ModSameNameMeta>(jsonContent);
            if (candidate == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(candidate.Id))
            {
                candidate.Id = modBaseName;
            }

            if (string.IsNullOrWhiteSpace(candidate.Name))
            {
                candidate.Name = candidate.Id;
            }

            if (candidate.Dependencies == null)
            {
                candidate.Dependencies = [];
            }

            if (!IsValidSameNameMeta(candidate))
            {
                return false;
            }

            meta = candidate;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidSameNameMeta(ModSameNameMeta meta)
    {
        var hasIdentity = !string.IsNullOrWhiteSpace(meta.Id) || !string.IsNullOrWhiteSpace(meta.Name);
        return hasIdentity;
    }

    private static string? ReadStringProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind == JsonValueKind.String ? property.GetString() : null;
    }

    private static bool? ReadBoolProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property))
        {
            return null;
        }

        return property.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static List<string> ReadStringArrayProperty(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var property) || property.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return property
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .ToList();
    }

    private static ModMetaInfo MergeCustomMeta(ModMetaInfo existing, ModMetaInfo parsed)
    {
        return new ModMetaInfo
        {
            Name = string.IsNullOrWhiteSpace(parsed.Name) ? existing.Name : parsed.Name,
            Tag = existing.Tag,
            Version = string.IsNullOrWhiteSpace(parsed.Version) ? existing.Version : parsed.Version,
            Detail = string.IsNullOrWhiteSpace(parsed.Detail) ? existing.Detail : parsed.Detail,
            Remark = existing.Remark,
            Author = string.IsNullOrWhiteSpace(parsed.Author) ? existing.Author : parsed.Author,
            DownloadUrl = existing.DownloadUrl,
            AuthorUrl = existing.AuthorUrl,
            DetailUrl = existing.DetailUrl,
            SocialUrl = existing.SocialUrl,
            Description = string.IsNullOrWhiteSpace(parsed.Description) ? existing.Description : parsed.Description
        };
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
            var meta = LoadModMeta(folder, folderName, hasPck, hasDll);
            mods.Add(new ModInfo
            {
                ModKey = modKey,
                FolderName = folderName,
                FolderPath = folder,
                MetadataFilePath = Path.Combine(folder, MetaFileName),
                SourcePath = source.Path,
                SourceName = source.Name,
                DisplayName = string.IsNullOrWhiteSpace(meta.Name) ? folderName : meta.Name,
                Tag = meta.Tag,
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
            var json = JsonSerializer.Serialize(meta, JsonIndentedOptions);
            File.WriteAllText(metaFile, json, Encoding.UTF8);
        }
        catch
        {
            // 忽略自动生成失败
        }
    }

    private static void TryWriteSameNameMetaFile(string sameNameMetaFile, ModSameNameMeta meta)
    {
        try
        {
            var json = JsonSerializer.Serialize(meta, JsonIndentedOptions);
            File.WriteAllText(sameNameMetaFile, json, Encoding.UTF8);
        }
        catch
        {
            // 忽略自动生成失败
        }
    }

    private class ModSameNameMeta
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("author")]
        public string Author { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string Version { get; set; } = string.Empty;

        [JsonPropertyName("has_pck")]
        public bool HasPck { get; set; }

        [JsonPropertyName("has_dll")]
        public bool HasDll { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = [];

        [JsonPropertyName("affects_gameplay")]
        public bool AffectsGameplay { get; set; } = true;
    }

    private sealed class ModAssemblyLoadContext : AssemblyLoadContext
    {
        public ModAssemblyLoadContext() : base(isCollectible: true)
        {
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }
    }
}
