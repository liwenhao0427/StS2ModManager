using System.Diagnostics;
using System.IO;

namespace StS2ModManager.Services;

public class SaveService
{
    private readonly GamePathService _pathService;

    public SaveService(GamePathService pathService)
    {
        _pathService = pathService;
    }

    public string GetSteamBasePath()
    {
        return Path.Combine(GamePathService.AppDataPath, "steam");
    }

    public List<string> GetAllSteamIds()
    {
        var steamPath = GetSteamBasePath();
        var ids = new List<string>();

        if (!Directory.Exists(steamPath)) return ids;

        foreach (var dir in Directory.GetDirectories(steamPath))
        {
            var dirName = Path.GetFileName(dir);
            if (long.TryParse(dirName, out _))
            {
                ids.Add(dirName);
            }
        }

        return ids;
    }

    public string BackupSaves(string sourcePath)
    {
        if (!Directory.Exists(sourcePath)) return string.Empty;

        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = Path.Combine(GamePathService.BackupDir, "Saves", timestamp);

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

    public bool CopySteamIdToAnother(string sourceId, string destId)
    {
        var steamPath = GetSteamBasePath();
        var sourcePath = Path.Combine(steamPath, sourceId);
        var destPath = Path.Combine(steamPath, destId);

        if (!Directory.Exists(sourcePath)) return false;

        // 备份目标目录
        if (Directory.Exists(destPath))
        {
            BackupSaves(destPath);
        }
        else
        {
            BackupSaves(sourcePath);
        }

        // 删除目标目录
        if (Directory.Exists(destPath))
        {
            Directory.Delete(destPath, true);
        }

        // 复制源目录到目标目录
        CopyDirectory(sourcePath, destPath);

        return true;
    }

    public void OpenSavesDirectory()
    {
        var path = GetSteamBasePath();
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
