using System.Diagnostics;
using System.IO;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public enum SaveCopyDirection
{
    ModdedToNormal,
    NormalToModded
}

public class SaveCopyResult
{
    public bool Success { get; set; }
    public int CopiedCount { get; set; }
    public string BackupPath { get; set; } = string.Empty;
}

public class SaveBackupMeta
{
    public string Type { get; set; } = "slot";
    public string Name { get; set; } = string.Empty;
    public string SteamId { get; set; } = string.Empty;
}

public class SaveService
{
    public string GetSteamBasePath()
    {
        return Path.Combine(GamePathService.AppDataPath, "steam");
    }

    public List<string> GetAllSteamIds()
    {
        var steamPath = GetSteamBasePath();
        if (!Directory.Exists(steamPath))
        {
            return new List<string>();
        }

        return Directory.GetDirectories(steamPath)
            .Select(Path.GetFileName)
            .Where(x => !string.IsNullOrWhiteSpace(x) && long.TryParse(x, out _))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList()!;
    }

    public string? GetLatestSteamId()
    {
        var steamPath = GetSteamBasePath();
        if (!Directory.Exists(steamPath))
        {
            return null;
        }

        return Directory.GetDirectories(steamPath)
            .Where(dir => long.TryParse(Path.GetFileName(dir), out _))
            .OrderByDescending(GetSteamDirTimestamp)
            .Select(Path.GetFileName)
            .FirstOrDefault();
    }

    public SaveCopyResult CopyWithinSteamId(string steamId, SaveCopyDirection direction, string slotKey)
    {
        var result = new SaveCopyResult();
        var steamIdPath = Path.Combine(GetSteamBasePath(), steamId);
        if (!Directory.Exists(steamIdPath))
        {
            return result;
        }

        var slots = ResolveSlots(slotKey);
        if (slots.Count == 0)
        {
            return result;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupFolderKey = $"{timestamp}_{steamId}";
        var backupRoot = Path.Combine(GamePathService.BackupDir, "Saves", backupFolderKey);
        Directory.CreateDirectory(backupRoot);
        WriteBackupMeta(backupRoot, new SaveBackupMeta
        {
            Type = "slot",
            Name = "#AUTO#",
            SteamId = steamId
        });

        foreach (var slot in slots)
        {
            var sourcePath = GetSlotPath(steamIdPath, slot, direction == SaveCopyDirection.ModdedToNormal);
            var targetPath = GetSlotPath(steamIdPath, slot, direction == SaveCopyDirection.NormalToModded);

            if (!Directory.Exists(sourcePath))
            {
                continue;
            }

            if (Directory.Exists(targetPath))
            {
                var backupTargetPath = BuildBackupSlotPath(backupRoot, slot, direction == SaveCopyDirection.NormalToModded);
                var backupParent = Path.GetDirectoryName(backupTargetPath);
                if (!string.IsNullOrWhiteSpace(backupParent))
                {
                    Directory.CreateDirectory(backupParent);
                }

                if (Directory.Exists(backupTargetPath))
                {
                    Directory.Delete(backupTargetPath, true);
                }

                Directory.Move(targetPath, backupTargetPath);
            }

            CopyDirectory(sourcePath, targetPath);
            result.CopiedCount++;
        }

        result.Success = result.CopiedCount > 0;
        result.BackupPath = result.CopiedCount > 0 ? backupRoot : string.Empty;
        return result;
    }

    public List<SaveBackupInfo> GetSaveBackups(string? steamId = null)
    {
        var backupsRoot = Path.Combine(GamePathService.BackupDir, "Saves");
        if (!Directory.Exists(backupsRoot))
        {
            return new List<SaveBackupInfo>();
        }

        var list = new List<SaveBackupInfo>();
        foreach (var backupDir in Directory.GetDirectories(backupsRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var folderKey = Path.GetFileName(backupDir);
            if (string.IsNullOrWhiteSpace(folderKey))
            {
                continue;
            }

            var backupTime = TryParseTimestamp(folderKey) ?? Directory.GetLastWriteTime(backupDir);
            var backupMeta = ReadBackupMeta(backupDir);
            var backupName = string.IsNullOrWhiteSpace(backupMeta?.Name)
                ? GetDefaultBackupName(folderKey)
                : backupMeta!.Name;
            var backupSteamId = !string.IsNullOrWhiteSpace(backupMeta?.SteamId)
                ? backupMeta!.SteamId
                : ParseSteamIdFromFolderKey(folderKey);

            if (!string.IsNullOrWhiteSpace(backupSteamId))
            {
                if (!string.IsNullOrWhiteSpace(steamId) && !string.Equals(steamId, backupSteamId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AppendBackupEntries(list, backupDir, folderKey, backupName, backupTime, backupSteamId, backupMeta);
                continue;
            }

            foreach (var steamBackupDir in Directory.GetDirectories(backupDir, "*", SearchOption.TopDirectoryOnly))
            {
                var id = Path.GetFileName(steamBackupDir);
                if (string.IsNullOrWhiteSpace(id) || !long.TryParse(id, out _))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(steamId) && !string.Equals(steamId, id, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AppendBackupEntries(list, steamBackupDir, folderKey, backupName, backupTime, id, backupMeta);
            }
        }

        return list
            .OrderByDescending(x => x.BackupTime)
            .ToList();
    }

    public SaveCopyResult RestoreFromBackup(SaveBackupInfo backup)
    {
        return RestoreFromBackup(backup, "auto");
    }

    public SaveCopyResult RestoreFromBackup(SaveBackupInfo backup, string targetMode)
    {
        var result = new SaveCopyResult();
        if (!Directory.Exists(backup.BackupPath))
        {
            return result;
        }

        var steamIdPath = Path.Combine(GetSteamBasePath(), backup.SteamId);
        Directory.CreateDirectory(GetSteamBasePath());

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var rescueRoot = Path.Combine(GamePathService.BackupDir, "Saves", $"restore_before_{timestamp}_{backup.SteamId}");

        if (backup.IsFullBackup)
        {
            if (Directory.Exists(steamIdPath))
            {
                var rescueParent = Path.GetDirectoryName(rescueRoot);
                if (!string.IsNullOrWhiteSpace(rescueParent))
                {
                    Directory.CreateDirectory(rescueParent);
                }

                if (Directory.Exists(rescueRoot))
                {
                    Directory.Delete(rescueRoot, true);
                }

                Directory.Move(steamIdPath, rescueRoot);
            }

            CopyDirectory(backup.BackupPath, steamIdPath);
            result.Success = true;
            result.CopiedCount = 1;
            if (Directory.Exists(rescueRoot))
            {
                WriteBackupMeta(rescueRoot, new SaveBackupMeta
                {
                    Type = "restore",
                    Name = "#RESTORE_BEFORE#",
                    SteamId = backup.SteamId
                });
            }
            result.BackupPath = Directory.Exists(rescueRoot) ? rescueRoot : string.Empty;
            return result;
        }

        if (backup.SlotId is null)
        {
            return result;
        }

        var restoreToModded = targetMode switch
        {
            "modded" => true,
            "normal" => false,
            _ => backup.IsModdedBackup
        };

        var targetSlotPath = GetSlotPath(steamIdPath, backup.SlotId.Value, restoreToModded);
        if (Directory.Exists(targetSlotPath))
        {
            var rescueSlotPath = BuildBackupSlotPath(rescueRoot, backup.SlotId.Value, restoreToModded);
            var rescueParent = Path.GetDirectoryName(rescueSlotPath);
            if (!string.IsNullOrWhiteSpace(rescueParent))
            {
                Directory.CreateDirectory(rescueParent);
            }

            if (Directory.Exists(rescueSlotPath))
            {
                Directory.Delete(rescueSlotPath, true);
            }

            Directory.Move(targetSlotPath, rescueSlotPath);
        }

        CopyDirectory(backup.BackupPath, targetSlotPath);

        result.Success = true;
        result.CopiedCount = 1;
        if (Directory.Exists(rescueRoot))
        {
            WriteBackupMeta(rescueRoot, new SaveBackupMeta
            {
                Type = "restore",
                Name = "#RESTORE_BEFORE#",
                SteamId = backup.SteamId
            });
        }
        result.BackupPath = Directory.Exists(rescueRoot) ? rescueRoot : string.Empty;
        return result;
    }

    public SaveCopyResult CreateManualBackup(string steamId, string backupName)
    {
        var result = new SaveCopyResult();
        var steamIdPath = Path.Combine(GetSteamBasePath(), steamId);
        if (!Directory.Exists(steamIdPath))
        {
            return result;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var safeName = SanitizeBackupName(backupName);
        var backupFolderKey = string.IsNullOrWhiteSpace(safeName)
            ? $"manual_{timestamp}_{steamId}"
            : $"manual_{timestamp}_{steamId}_{safeName}";
        var backupRoot = Path.Combine(GamePathService.BackupDir, "Saves", backupFolderKey);
        var backupPath = backupRoot;

        CopyDirectory(steamIdPath, backupPath);
        WriteBackupMeta(backupRoot, new SaveBackupMeta
        {
            Type = "full",
            Name = string.IsNullOrWhiteSpace(backupName) ? "#MANUAL#" : backupName.Trim(),
            SteamId = steamId
        });

        result.Success = true;
        result.CopiedCount = 1;
        result.BackupPath = backupRoot;
        return result;
    }

    public void OpenSavesDirectory()
    {
        var path = GetSteamBasePath();
        if (Directory.Exists(path))
        {
            Process.Start("explorer.exe", path);
            return;
        }

        if (Directory.Exists(GamePathService.AppDataPath))
        {
            Process.Start("explorer.exe", GamePathService.AppDataPath);
        }
    }

    private static DateTime GetSteamDirTimestamp(string steamDir)
    {
        var profileCandidates = Directory.GetDirectories(steamDir, "profile*", SearchOption.TopDirectoryOnly)
            .ToList();

        var moddedDir = Path.Combine(steamDir, "modded");
        if (Directory.Exists(moddedDir))
        {
            profileCandidates.AddRange(Directory.GetDirectories(moddedDir, "profile*", SearchOption.TopDirectoryOnly));
        }

        return profileCandidates
            .Where(Directory.Exists)
            .Select(Directory.GetLastWriteTime)
            .DefaultIfEmpty(Directory.GetLastWriteTime(steamDir))
            .Max();
    }

    private static string GetSlotPath(string steamIdPath, int slot, bool modded)
    {
        var folder = $"profile{slot}";
        return modded
            ? Path.Combine(steamIdPath, "modded", folder)
            : Path.Combine(steamIdPath, folder);
    }

    private static string BuildBackupSlotPath(string backupRoot, int slot, bool modded)
    {
        var folder = $"profile{slot}";
        return modded
            ? Path.Combine(backupRoot, "modded", folder)
            : Path.Combine(backupRoot, folder);
    }

    private static List<int> ResolveSlots(string slotKey)
    {
        if (string.Equals(slotKey, "all", StringComparison.OrdinalIgnoreCase))
        {
            return new List<int> { 1, 2, 3 };
        }

        if (int.TryParse(slotKey, out var slot) && slot is >= 1 and <= 3)
        {
            return new List<int> { slot };
        }

        return new List<int>();
    }

    private static void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(src, file);
            var destFile = Path.Combine(dest, relativePath);
            var destDir = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrWhiteSpace(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(file, destFile, true);
        }
    }

    private static DateTime? TryParseTimestamp(string text)
    {
        if (text.Length >= 15)
        {
            var prefix = text[..15];
            if (DateTime.TryParseExact(prefix, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var prefixValue))
            {
                return prefixValue;
            }
        }

        if (text.StartsWith("manual_", StringComparison.OrdinalIgnoreCase))
        {
            var raw = text.Split('_');
            if (raw.Length >= 3)
            {
                return TryParseTimestamp($"{raw[1]}_{raw[2]}");
            }
        }

        if (text.StartsWith("restore_before_", StringComparison.OrdinalIgnoreCase))
        {
            var raw = text.Split('_');
            if (raw.Length >= 4)
            {
                return TryParseTimestamp($"{raw[2]}_{raw[3]}");
            }
        }

        if (DateTime.TryParseExact(text, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var value))
        {
            return value;
        }

        return null;
    }

    private static bool TryParseSlot(string slotDirPath, out int slotId)
    {
        slotId = 0;
        var name = Path.GetFileName(slotDirPath);
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("profile", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = name["profile".Length..];
        return int.TryParse(value, out slotId) && slotId > 0;
    }

    private static string ParseSteamIdFromFolderKey(string folderKey)
    {
        if (string.IsNullOrWhiteSpace(folderKey))
        {
            return string.Empty;
        }

        var parts = folderKey.Split('_', StringSplitOptions.RemoveEmptyEntries);
        for (var i = parts.Length - 1; i >= 0; i--)
        {
            if (long.TryParse(parts[i], out _))
            {
                return parts[i];
            }
        }

        return string.Empty;
    }

    private static void AppendBackupEntries(
        List<SaveBackupInfo> list,
        string backupPath,
        string folderKey,
        string backupName,
        DateTime backupTime,
        string steamId,
        SaveBackupMeta? backupMeta)
    {
        if (string.Equals(backupMeta?.Type, "full", StringComparison.OrdinalIgnoreCase))
        {
            list.Add(new SaveBackupInfo
            {
                BackupFolderKey = folderKey,
                BackupName = backupName,
                BackupTime = backupTime,
                SteamId = steamId,
                BackupPath = backupPath,
                IsFullBackup = true
            });
            return;
        }

        var anySlot = false;
        foreach (var slotDir in Directory.GetDirectories(backupPath, "profile*", SearchOption.TopDirectoryOnly))
        {
            if (!TryParseSlot(slotDir, out var slotId))
            {
                continue;
            }

            anySlot = true;
            list.Add(new SaveBackupInfo
            {
                BackupFolderKey = folderKey,
                BackupName = backupName,
                BackupTime = backupTime,
                SteamId = steamId,
                BackupPath = slotDir,
                IsFullBackup = false,
                SlotId = slotId,
                IsModdedBackup = false
            });
        }

        var moddedDir = Path.Combine(backupPath, "modded");
        if (Directory.Exists(moddedDir))
        {
            foreach (var slotDir in Directory.GetDirectories(moddedDir, "profile*", SearchOption.TopDirectoryOnly))
            {
                if (!TryParseSlot(slotDir, out var slotId))
                {
                    continue;
                }

                anySlot = true;
                list.Add(new SaveBackupInfo
                {
                    BackupFolderKey = folderKey,
                    BackupName = backupName,
                    BackupTime = backupTime,
                    SteamId = steamId,
                    BackupPath = slotDir,
                    IsFullBackup = false,
                    SlotId = slotId,
                    IsModdedBackup = true
                });
            }
        }

        if (!anySlot)
        {
            list.Add(new SaveBackupInfo
            {
                BackupFolderKey = folderKey,
                BackupName = backupName,
                BackupTime = backupTime,
                SteamId = steamId,
                BackupPath = backupPath,
                IsFullBackup = true
            });
        }
    }

    private static string SanitizeBackupName(string backupName)
    {
        if (string.IsNullOrWhiteSpace(backupName))
        {
            return string.Empty;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(backupName.Trim().Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
        return safe.Length > 32 ? safe[..32] : safe;
    }

    private static SaveBackupMeta? ReadBackupMeta(string backupRoot)
    {
        var metaPath = Path.Combine(backupRoot, "backup.meta.json");
        if (!File.Exists(metaPath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(metaPath);
            return JsonSerializer.Deserialize<SaveBackupMeta>(json);
        }
        catch
        {
            return null;
        }
    }

    private static void WriteBackupMeta(string backupRoot, SaveBackupMeta meta)
    {
        try
        {
            var metaPath = Path.Combine(backupRoot, "backup.meta.json");
            var json = JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(metaPath, json);
        }
        catch
        {
            // 忽略元数据写入失败
        }
    }

    private static string GetDefaultBackupName(string folderKey)
    {
        if (folderKey.StartsWith("manual_", StringComparison.OrdinalIgnoreCase))
        {
            return "#MANUAL#";
        }

        if (folderKey.StartsWith("restore_before_", StringComparison.OrdinalIgnoreCase))
        {
            return "#RESTORE_BEFORE#";
        }

        return "#AUTO#";
    }
}
