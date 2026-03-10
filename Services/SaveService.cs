using System.Diagnostics;
using System.IO;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class SaveService
{
    private readonly GamePathService _pathService;
    private const int MaxSlots = 10;

    public SaveService(GamePathService pathService)
    {
        _pathService = pathService;
    }

    public string GetSteamSavesPath()
    {
        var path = Path.Combine(GamePathService.AppDataPath, "steam");
        if (!Directory.Exists(path))
        {
            // 尝试查找其他可能的用户ID目录
            var steamDir = Path.Combine(GamePathService.AppDataPath, "steam");
            if (Directory.Exists(steamDir))
            {
                var dirs = Directory.GetDirectories(steamDir);
                foreach (var dir in dirs)
                {
                    if (long.TryParse(Path.GetFileName(dir), out _))
                    {
                        return dir;
                    }
                }
            }
        }
        return path;
    }

    public List<SaveSlotInfo> ScanSaveSlots(string? steamPath = null)
    {
        var slots = new List<SaveSlotInfo>();
        steamPath ??= GetSteamSavesPath();

        if (!Directory.Exists(steamPath)) return slots;

        for (int i = 0; i < MaxSlots; i++)
        {
            var slot = new SaveSlotInfo { SlotId = i };

            var normalPath = Path.Combine(steamPath, $"profile{i}", "saves");
            var moddedPath = Path.Combine(steamPath, "modded", $"profile{i}", "saves");

            if (Directory.Exists(normalPath) && Directory.GetFiles(normalPath, "*.save").Any())
            {
                slot.HasNormalSave = true;
                var files = Directory.GetFiles(normalPath, "*.save");
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    slot.NormalSaveTime = File.GetLastWriteTime(latestFile);
                }
            }

            if (Directory.Exists(moddedPath) && Directory.GetFiles(moddedPath, "*.save").Any())
            {
                slot.HasModdedSave = true;
                var files = Directory.GetFiles(moddedPath, "*.save");
                if (files.Length > 0)
                {
                    var latestFile = files.OrderByDescending(f => File.GetLastWriteTime(f)).First();
                    slot.ModdedSaveTime = File.GetLastWriteTime(latestFile);
                }
            }

            if (slot.HasNormalSave || slot.HasModdedSave)
                slots.Add(slot);
        }

        return slots;
    }

    public string BackupSaves()
    {
        var steamPath = GetSteamSavesPath();
        if (!Directory.Exists(steamPath)) return string.Empty;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(GamePathService.BackupDir, "Saves", timestamp);

        try
        {
            CopyDirectory(steamPath, backupPath);
            return backupPath;
        }
        catch
        {
            return string.Empty;
        }
    }

    public bool CopyNormalToModded()
    {
        var steamPath = GetSteamSavesPath();
        if (!Directory.Exists(steamPath)) return false;

        var normalPath = steamPath;
        var moddedPath = Path.Combine(steamPath, "modded");

        if (!Directory.Exists(normalPath)) return false;

        BackupSaves();

        if (!Directory.Exists(moddedPath))
        {
            Directory.CreateDirectory(moddedPath);
        }

        // 复制profile目录
        for (int i = 0; i < MaxSlots; i++)
        {
            var srcProfile = Path.Combine(normalPath, $"profile{i}");
            var destProfile = Path.Combine(moddedPath, $"profile{i}");

            if (!Directory.Exists(srcProfile)) continue;

            Directory.CreateDirectory(destProfile);

            var srcSaves = Path.Combine(srcProfile, "saves");
            var destSaves = Path.Combine(destProfile, "saves");

            if (Directory.Exists(srcSaves))
            {
                Directory.CreateDirectory(destSaves);

                foreach (var file in Directory.GetFiles(srcSaves, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(srcSaves, file);
                    var destFile = Path.Combine(destSaves, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    File.Copy(file, destFile, true);
                }
            }
        }

        return true;
    }

    public bool CopyModdedToNormal()
    {
        var steamPath = GetSteamSavesPath();
        if (!Directory.Exists(steamPath)) return false;

        var normalPath = steamPath;
        var moddedPath = Path.Combine(steamPath, "modded");

        if (!Directory.Exists(moddedPath)) return false;

        BackupSaves();

        // 复制modded下的profile目录到顶层
        for (int i = 0; i < MaxSlots; i++)
        {
            var srcProfile = Path.Combine(moddedPath, $"profile{i}");
            var destProfile = Path.Combine(normalPath, $"profile{i}");

            if (!Directory.Exists(srcProfile)) continue;

            Directory.CreateDirectory(destProfile);

            var srcSaves = Path.Combine(srcProfile, "saves");
            var destSaves = Path.Combine(destProfile, "saves");

            if (Directory.Exists(srcSaves))
            {
                Directory.CreateDirectory(destSaves);

                foreach (var file in Directory.GetFiles(srcSaves, "*", SearchOption.AllDirectories))
                {
                    var relativePath = Path.GetRelativePath(srcSaves, file);
                    var destFile = Path.Combine(destSaves, relativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
                    File.Copy(file, destFile, true);
                }
            }
        }

        return true;
    }

    public void OpenSavesDirectory()
    {
        var path = GetSteamSavesPath();
        if (Directory.Exists(path))
            Process.Start("explorer.exe", path);
        else if (Directory.Exists(GamePathService.AppDataPath))
            Process.Start("explorer.exe", GamePathService.AppDataPath);
    }

    private void CopyDirectory(string src, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(src, file);
            var destFile = Path.Combine(dest, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, true);
        }
    }
}
