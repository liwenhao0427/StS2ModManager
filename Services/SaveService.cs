using System.Diagnostics;
using System.IO;
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
        var backupRoot = Path.Combine(GamePathService.BackupDir, "Saves", timestamp, steamId);
        Directory.CreateDirectory(backupRoot);

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
        foreach (var timestampDir in Directory.GetDirectories(backupsRoot, "*", SearchOption.TopDirectoryOnly))
        {
            var timestampKey = Path.GetFileName(timestampDir);
            if (string.IsNullOrWhiteSpace(timestampKey))
            {
                continue;
            }

            var backupTime = TryParseTimestamp(timestampKey) ?? Directory.GetLastWriteTime(timestampDir);
            foreach (var steamBackupDir in Directory.GetDirectories(timestampDir, "*", SearchOption.TopDirectoryOnly))
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

                list.Add(new SaveBackupInfo
                {
                    TimestampKey = timestampKey,
                    BackupTime = backupTime,
                    SteamId = id,
                    SteamBackupPath = steamBackupDir
                });
            }
        }

        return list
            .OrderByDescending(x => x.BackupTime)
            .ToList();
    }

    public SaveCopyResult RestoreFromBackup(SaveBackupInfo backup)
    {
        var result = new SaveCopyResult();
        if (!Directory.Exists(backup.SteamBackupPath))
        {
            return result;
        }

        var steamIdPath = Path.Combine(GetSteamBasePath(), backup.SteamId);
        Directory.CreateDirectory(GetSteamBasePath());

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var rescueRoot = Path.Combine(GamePathService.BackupDir, "Saves", $"restore_before_{timestamp}", backup.SteamId);

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

        CopyDirectory(backup.SteamBackupPath, steamIdPath);

        result.Success = true;
        result.CopiedCount = 1;
        result.BackupPath = Directory.Exists(rescueRoot) ? rescueRoot : string.Empty;
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
        if (DateTime.TryParseExact(text, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.None, out var value))
        {
            return value;
        }

        return null;
    }
}
