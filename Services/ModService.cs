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
    private const string LegacyMetaFileName = "modinfo.json";
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
            var normalizedDetail = meta.Detail.Trim();
            var normalizedDescription = meta.Description.Trim();
            if (string.IsNullOrWhiteSpace(normalizedDescription) && !string.IsNullOrWhiteSpace(normalizedDetail))
            {
                normalizedDescription = normalizedDetail;
            }

            var normalizedMeta = new ModMetaInfo
            {
                Id = meta.Id.Trim(),
                Name = meta.Name.Trim(),
                Tag = meta.Tag.Trim(),
                Version = meta.Version.Trim(),
                Detail = normalizedDetail,
                Remark = meta.Remark.Trim(),
                Author = meta.Author.Trim(),
                DownloadUrl = meta.DownloadUrl.Trim(),
                AuthorUrl = meta.AuthorUrl.Trim(),
                DetailUrl = meta.DetailUrl.Trim(),
                SocialUrl = meta.SocialUrl.Trim(),
                Description = normalizedDescription,
                Dependencies = (meta.Dependencies ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                AffectsGameplay = meta.AffectsGameplay
            };

            var modBaseName = ResolveModBaseName(folderPath, folderName);
            var sameNameMetaFile = Path.Combine(folderPath, $"{modBaseName}.json");
            var legacyMetaFile = Path.Combine(folderPath, LegacyMetaFileName);
            var hasPck = Directory.GetFiles(folderPath, "*.pck", SearchOption.AllDirectories).Length > 0;
            var hasDll = Directory.GetFiles(folderPath, "*.dll", SearchOption.AllDirectories).Length > 0;

            var sameNameMeta = TryReadSameNameMeta(sameNameMetaFile)
                               ?? BuildSameNameMetaFromCustom(null, modBaseName, folderName, hasPck, hasDll);

            var legacyMeta = TryReadCustomMeta(legacyMetaFile, folderName);
            if (legacyMeta != null)
            {
                sameNameMeta = MergeLegacyCustomMeta(sameNameMeta, legacyMeta);
            }

            sameNameMeta = NormalizeSameNameMeta(sameNameMeta, modBaseName, folderName, hasPck, hasDll);
            sameNameMeta = ApplyCustomMetaToSameNameMeta(sameNameMeta, normalizedMeta);
            TryWriteSameNameMetaFile(sameNameMetaFile, sameNameMeta);
            TryDeleteFile(legacyMetaFile);
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
        var existingSameNameMeta = TryReadSameNameMeta(sameNameJsonPath);
        if (existingSameNameMeta != null)
        {
            extractedMeta = MergeLegacyCustomMeta(extractedMeta, ConvertSameNameMetaToCustomMeta(existingSameNameMeta, modBaseName, folderName));
        }

        var legacyMetaFile = Path.Combine(folderPath, LegacyMetaFileName);
        var legacyMeta = TryReadCustomMeta(legacyMetaFile, folderName);
        if (legacyMeta != null)
        {
            extractedMeta = MergeLegacyCustomMeta(extractedMeta, legacyMeta);
        }

        TryWriteSameNameMetaFile(sameNameJsonPath, extractedMeta);
        TryDeleteFile(legacyMetaFile);
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
            var normalizedDetail = meta.Detail.Trim();
            var normalizedDescription = meta.Description.Trim();
            if (string.IsNullOrWhiteSpace(normalizedDescription) && !string.IsNullOrWhiteSpace(normalizedDetail))
            {
                normalizedDescription = normalizedDetail;
            }

            var normalizedMeta = new ModMetaInfo
            {
                Id = meta.Id.Trim(),
                Name = meta.Name.Trim(),
                Tag = meta.Tag.Trim(),
                Version = meta.Version.Trim(),
                Detail = normalizedDetail,
                Remark = meta.Remark.Trim(),
                Author = meta.Author.Trim(),
                DownloadUrl = meta.DownloadUrl.Trim(),
                AuthorUrl = meta.AuthorUrl.Trim(),
                DetailUrl = meta.DetailUrl.Trim(),
                SocialUrl = meta.SocialUrl.Trim(),
                Description = normalizedDescription,
                Dependencies = (meta.Dependencies ?? [])
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                AffectsGameplay = meta.AffectsGameplay
            };

            if (!SaveModMetaByPath(mod.FolderPath, mod.FolderName, normalizedMeta))
            {
                return false;
            }

            var modBaseName = ResolveModBaseName(mod.FolderPath, mod.FolderName);
            var sameNameMetaFile = Path.Combine(mod.FolderPath, $"{modBaseName}.json");

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
            mod.MetadataFilePath = sameNameMetaFile;
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ModMetaInfo LoadModMeta(string modFolderPath, string folderName, bool hasPck, bool hasDll)
    {
        var legacyMetaFile = Path.Combine(modFolderPath, LegacyMetaFileName);
        var modBaseName = ResolveModBaseName(modFolderPath, folderName);
        var modSameNameMetaFile = Path.Combine(modFolderPath, $"{modBaseName}.json");

        var legacyMeta = TryReadCustomMeta(legacyMetaFile, folderName);
        var sameNameMeta = TryReadSameNameMeta(modSameNameMetaFile);

        if (sameNameMeta == null)
        {
            sameNameMeta = BuildSameNameMetaFromCustom(legacyMeta, modBaseName, folderName, hasPck, hasDll);
        }
        else
        {
            sameNameMeta = NormalizeSameNameMeta(sameNameMeta, modBaseName, folderName, hasPck, hasDll);
        }

        if (legacyMeta != null)
        {
            sameNameMeta = MergeLegacyCustomMeta(sameNameMeta, legacyMeta);
        }

        TryWriteSameNameMetaFile(modSameNameMetaFile, sameNameMeta);
        TryDeleteFile(legacyMetaFile);
        return ConvertSameNameMetaToCustomMeta(sameNameMeta, modBaseName, folderName);
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
            var affectsGameplay = ReadBoolProperty(root, "affects_gameplay") ?? false;
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
        var fallbackId = string.IsNullOrWhiteSpace(modBaseName)
            ? (string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim())
            : modBaseName.Trim();
        var id = string.IsNullOrWhiteSpace(customMeta?.Id)
            ? fallbackId
            : customMeta!.Id.Trim();
        var name = customMeta == null || string.IsNullOrWhiteSpace(customMeta.Name) ? id : customMeta.Name.Trim();
        var author = customMeta == null ? string.Empty : customMeta.Author.Trim();
        var detail = customMeta?.Detail?.Trim() ?? string.Empty;
        var description = customMeta == null ? string.Empty : customMeta.Description.Trim();
        if (string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(detail))
        {
            description = detail;
        }
        var version = customMeta == null ? string.Empty : customMeta.Version.Trim();

        return new ModSameNameMeta
        {
            Id = id,
            Name = name,
            Author = author,
            Description = description,
            Version = version,
            Tag = customMeta?.Tag?.Trim() ?? string.Empty,
            Detail = detail,
            Remark = customMeta?.Remark?.Trim() ?? string.Empty,
            DownloadUrl = customMeta?.DownloadUrl?.Trim() ?? string.Empty,
            AuthorUrl = customMeta?.AuthorUrl?.Trim() ?? string.Empty,
            DetailUrl = customMeta?.DetailUrl?.Trim() ?? string.Empty,
            SocialUrl = customMeta?.SocialUrl?.Trim() ?? string.Empty,
            HasPck = hasPck,
            HasDll = hasDll,
            Dependencies = customMeta?.Dependencies?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? [],
            AffectsGameplay = customMeta?.AffectsGameplay ?? false
        };
    }

    private static ModSameNameMeta NormalizeSameNameMeta(ModSameNameMeta source, string modBaseName, string folderName, bool hasPck, bool hasDll)
    {
        var fallbackId = string.IsNullOrWhiteSpace(modBaseName)
            ? (string.IsNullOrWhiteSpace(folderName) ? "UnknownMod" : folderName.Trim())
            : modBaseName.Trim();
        var id = string.IsNullOrWhiteSpace(source.Id) ? fallbackId : source.Id.Trim();
        var detail = source.Detail?.Trim() ?? string.Empty;
        var description = source.Description?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(description) && !string.IsNullOrWhiteSpace(detail))
        {
            description = detail;
        }

        return new ModSameNameMeta
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(source.Name) ? id : source.Name.Trim(),
            Author = source.Author?.Trim() ?? string.Empty,
            Description = description,
            Version = source.Version?.Trim() ?? string.Empty,
            Tag = source.Tag?.Trim() ?? string.Empty,
            Detail = detail,
            Remark = source.Remark?.Trim() ?? string.Empty,
            DownloadUrl = source.DownloadUrl?.Trim() ?? string.Empty,
            AuthorUrl = source.AuthorUrl?.Trim() ?? string.Empty,
            DetailUrl = source.DetailUrl?.Trim() ?? string.Empty,
            SocialUrl = source.SocialUrl?.Trim() ?? string.Empty,
            HasPck = hasPck,
            HasDll = hasDll,
            Dependencies = source.Dependencies ?? [],
            AffectsGameplay = source.AffectsGameplay,
            ExtraProperties = source.ExtraProperties
        };
    }

    private static ModMetaInfo ConvertSameNameMetaToCustomMeta(ModSameNameMeta? sameNameMeta, string modBaseName, string folderName)
    {
        if (sameNameMeta == null)
        {
            var defaultName = string.IsNullOrWhiteSpace(modBaseName) ? folderName : modBaseName;
            return new ModMetaInfo { Name = defaultName, Id = defaultName };
        }

        var fallbackName = !string.IsNullOrWhiteSpace(sameNameMeta.Id)
            ? sameNameMeta.Id.Trim()
            : (string.IsNullOrWhiteSpace(modBaseName) ? folderName : modBaseName);
        return new ModMetaInfo
        {
            Name = string.IsNullOrWhiteSpace(sameNameMeta.Name) ? fallbackName : sameNameMeta.Name.Trim(),
            Tag = sameNameMeta.Tag?.Trim() ?? string.Empty,
            Author = sameNameMeta.Author?.Trim() ?? string.Empty,
            Version = sameNameMeta.Version?.Trim() ?? string.Empty,
            Description = sameNameMeta.Description?.Trim() ?? string.Empty,
            Detail = string.IsNullOrWhiteSpace(sameNameMeta.Detail)
                ? (sameNameMeta.Description?.Trim() ?? string.Empty)
                : sameNameMeta.Detail.Trim(),
            Remark = sameNameMeta.Remark?.Trim() ?? string.Empty,
            DownloadUrl = sameNameMeta.DownloadUrl?.Trim() ?? string.Empty,
            AuthorUrl = sameNameMeta.AuthorUrl?.Trim() ?? string.Empty,
            DetailUrl = sameNameMeta.DetailUrl?.Trim() ?? string.Empty,
            SocialUrl = sameNameMeta.SocialUrl?.Trim() ?? string.Empty,
            Dependencies = sameNameMeta.Dependencies ?? [],
            AffectsGameplay = sameNameMeta.AffectsGameplay,
            Id = sameNameMeta.Id?.Trim() ?? string.Empty
        };
    }

    private static ModSameNameMeta ApplyCustomMetaToSameNameMeta(ModSameNameMeta sameNameMeta, ModMetaInfo customMeta)
    {
        if (!string.IsNullOrWhiteSpace(customMeta.Id))
        {
            sameNameMeta.Id = customMeta.Id.Trim();
        }

        sameNameMeta.Name = string.IsNullOrWhiteSpace(customMeta.Name) ? sameNameMeta.Name : customMeta.Name.Trim();
        sameNameMeta.Author = customMeta.Author?.Trim() ?? string.Empty;
        sameNameMeta.Version = customMeta.Version?.Trim() ?? string.Empty;
        sameNameMeta.Description = customMeta.Description?.Trim() ?? string.Empty;
        sameNameMeta.Tag = customMeta.Tag?.Trim() ?? string.Empty;
        sameNameMeta.Detail = customMeta.Detail?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(sameNameMeta.Description) && !string.IsNullOrWhiteSpace(sameNameMeta.Detail))
        {
            sameNameMeta.Description = sameNameMeta.Detail;
        }
        sameNameMeta.Remark = customMeta.Remark?.Trim() ?? string.Empty;
        sameNameMeta.DownloadUrl = customMeta.DownloadUrl?.Trim() ?? string.Empty;
        sameNameMeta.AuthorUrl = customMeta.AuthorUrl?.Trim() ?? string.Empty;
        sameNameMeta.DetailUrl = customMeta.DetailUrl?.Trim() ?? string.Empty;
        sameNameMeta.SocialUrl = customMeta.SocialUrl?.Trim() ?? string.Empty;
        sameNameMeta.Dependencies = (customMeta.Dependencies ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        sameNameMeta.AffectsGameplay = customMeta.AffectsGameplay;
        return sameNameMeta;
    }

    private static ModSameNameMeta MergeLegacyCustomMeta(ModSameNameMeta sameNameMeta, ModMetaInfo legacyMeta)
    {
        if (string.IsNullOrWhiteSpace(sameNameMeta.Name) && !string.IsNullOrWhiteSpace(legacyMeta.Name))
        {
            sameNameMeta.Name = legacyMeta.Name.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Author) && !string.IsNullOrWhiteSpace(legacyMeta.Author))
        {
            sameNameMeta.Author = legacyMeta.Author.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Version) && !string.IsNullOrWhiteSpace(legacyMeta.Version))
        {
            sameNameMeta.Version = legacyMeta.Version.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Description) && !string.IsNullOrWhiteSpace(legacyMeta.Description))
        {
            sameNameMeta.Description = legacyMeta.Description.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Tag) && !string.IsNullOrWhiteSpace(legacyMeta.Tag))
        {
            sameNameMeta.Tag = legacyMeta.Tag.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Detail) && !string.IsNullOrWhiteSpace(legacyMeta.Detail))
        {
            sameNameMeta.Detail = legacyMeta.Detail.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.Remark) && !string.IsNullOrWhiteSpace(legacyMeta.Remark))
        {
            sameNameMeta.Remark = legacyMeta.Remark.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.DownloadUrl) && !string.IsNullOrWhiteSpace(legacyMeta.DownloadUrl))
        {
            sameNameMeta.DownloadUrl = legacyMeta.DownloadUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.AuthorUrl) && !string.IsNullOrWhiteSpace(legacyMeta.AuthorUrl))
        {
            sameNameMeta.AuthorUrl = legacyMeta.AuthorUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.DetailUrl) && !string.IsNullOrWhiteSpace(legacyMeta.DetailUrl))
        {
            sameNameMeta.DetailUrl = legacyMeta.DetailUrl.Trim();
        }

        if (string.IsNullOrWhiteSpace(sameNameMeta.SocialUrl) && !string.IsNullOrWhiteSpace(legacyMeta.SocialUrl))
        {
            sameNameMeta.SocialUrl = legacyMeta.SocialUrl.Trim();
        }

        if ((sameNameMeta.Dependencies == null || sameNameMeta.Dependencies.Count == 0) && legacyMeta.Dependencies.Count > 0)
        {
            sameNameMeta.Dependencies = legacyMeta.Dependencies
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (!sameNameMeta.AffectsGameplay && legacyMeta.AffectsGameplay)
        {
            sameNameMeta.AffectsGameplay = true;
        }

        return sameNameMeta;
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
            if (!hasPck && !hasDll)
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
                MetadataFilePath = Path.Combine(folder, $"{ResolveModBaseName(folder, folderName)}.json"),
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

    private static void TryDeleteFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // 忽略清理失败
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

        [JsonPropertyName("tag")]
        public string Tag { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonPropertyName("remark")]
        public string Remark { get; set; } = string.Empty;

        [JsonPropertyName("download_url")]
        public string DownloadUrl { get; set; } = string.Empty;

        [JsonPropertyName("author_url")]
        public string AuthorUrl { get; set; } = string.Empty;

        [JsonPropertyName("detail_url")]
        public string DetailUrl { get; set; } = string.Empty;

        [JsonPropertyName("social_url")]
        public string SocialUrl { get; set; } = string.Empty;

        [JsonPropertyName("has_pck")]
        public bool HasPck { get; set; }

        [JsonPropertyName("has_dll")]
        public bool HasDll { get; set; }

        [JsonPropertyName("dependencies")]
        public List<string> Dependencies { get; set; } = [];

        [JsonPropertyName("affects_gameplay")]
        public bool AffectsGameplay { get; set; } = false;

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraProperties { get; set; }
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
