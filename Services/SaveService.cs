using System.Diagnostics;
using System.IO;

namespace StS2ModManager.Services;

public enum SaveCopyDirection
{
    ModdedToNormal,
    NormalToModded
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

    public string BackupSteamIdSaves(string steamId)
    {
        var sourcePath = Path.Combine(GetSteamBasePath(), steamId);
        if (!Directory.Exists(sourcePath))
        {
            return string.Empty;
        }

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(GamePathService.BackupDir, "Saves", timestamp, steamId);

        try
        {
            CopyDirectory(sourcePath, backupPath);
            return backupPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool CopyWithinSteamId(string steamId, SaveCopyDirection direction, string slotKey)
    {
        var steamIdPath = Path.Combine(GetSteamBasePath(), steamId);
        if (!Directory.Exists(steamIdPath))
        {
            return false;
        }

        var slots = ResolveSlots(slotKey);
        if (slots.Count == 0)
        {
            return false;
        }

        foreach (var slot in slots)
        {
            var fromPath = GetSlotPath(steamIdPath, slot, direction == SaveCopyDirection.ModdedToNormal);
            var toPath = GetSlotPath(steamIdPath, slot, direction == SaveCopyDirection.NormalToModded);

            if (!Directory.Exists(fromPath))
            {
                continue;
            }

            if (Directory.Exists(toPath))
            {
                Directory.Delete(toPath, true);
            }

            CopyDirectory(fromPath, toPath);
        }

        return true;
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

        var latestProfile = profileCandidates
            .Where(Directory.Exists)
            .Select(Directory.GetLastWriteTime)
            .DefaultIfEmpty(Directory.GetLastWriteTime(steamDir))
            .Max();

        return latestProfile;
    }

    private static string GetSlotPath(string steamIdPath, int slot, bool modded)
    {
        var folder = $"profile{slot}";
        return modded
            ? Path.Combine(steamIdPath, "modded", folder)
            : Path.Combine(steamIdPath, folder);
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
}
