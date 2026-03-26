using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace StS2ModManager.Services;

public class GamePathService
{
    private static readonly string[] _possiblePaths = new[]
    {
        @"C:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        @"C:\Program Files\Steam\steamapps\common\Slay the Spire 2",
        @"D:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        @"D:\Program Files\Steam\steamapps\common\Slay the Spire 2",
        @"F:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        @"F:\Program Files\Steam\steamapps\common\Slay the Spire 2",
        @"G:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        @"G:\Program Files\Steam\steamapps\common\Slay the Spire 2",
        @"H:\Program Files (x86)\Steam\steamapps\common\Slay the Spire 2",
        @"H:\Program Files\Steam\steamapps\common\Slay the Spire 2",
        @"C:\Steam\steamapps\common\Slay the Spire 2",
        @"D:\Steam\steamapps\common\Slay the Spire 2",
        @"E:\Steam\steamapps\common\Slay the Spire 2",
        @"F:\Steam\steamapps\common\Slay the Spire 2",
        @"G:\Steam\steamapps\common\Slay the Spire 2",
        @"H:\Steam\steamapps\common\Slay the Spire 2",
        @"C:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"D:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"E:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"F:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"G:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"H:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"C:\Epic Games\SlaytheSpire2",
        @"D:\Epic Games\SlaytheSpire2",
        @"E:\Epic Games\SlaytheSpire2",
        @"F:\Epic Games\SlaytheSpire2",
        @"G:\Epic Games\SlaytheSpire2",
        @"H:\Epic Games\SlaytheSpire2",
        @"C:\Games\Slay the Spire 2",
        @"D:\Games\Slay the Spire 2",
        @"E:\Games\Slay the Spire 2",
        @"F:\Games\Slay the Spire 2",
        @"G:\Games\Slay the Spire 2",
        @"H:\Games\Slay the Spire 2",
    };

    private static readonly string[] _commonLibraryRoots = new[]
    {
        "SteamLibrary",
        "Steam",
        "Games",
        "GameLibrary",
        "SteamGames"
    };

    private static readonly string[] _commonGameFolderNames = new[]
    {
        "Slay the Spire 2",
        "SlayTheSpire2",
        "Slay the Spire2",
        "slay the spire 2"
    };

    public static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SlayTheSpire2"
    );

    public static string ToolBaseDir => AppDomain.CurrentDomain.BaseDirectory;

    public static string ToolModsDir => Path.Combine(ToolBaseDir, "Mods");

    public static string BackupDir => Path.Combine(AppDataPath, "Backup");

    public static string ConfigDir => Path.Combine(ToolBaseDir, "Config");

    public static string AliasesFile => Path.Combine(ConfigDir, "aliases.json");

    public List<string> DetectGamePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        TryAddValidPaths(paths, _possiblePaths, IsValidGamePathForDetection);
        TryAddValidPaths(paths, DetectFromCommonLibraryPatterns(), IsValidGamePathForDetection);
        TryAddValidPaths(paths, DetectFromSteamLibraryFolders(), IsValidGamePathForDetection);
        TryAddValidPaths(paths, DetectFromToolDirectory(), IsValidGamePathForDetection);

        return paths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public bool IsValidGamePathForDetection(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var exePath = Path.Combine(path, "SlayTheSpire2.exe");
        return File.Exists(exePath);
    }

    public bool IsValidGamePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return false;

        var modsPath = Path.Combine(path, "mods");
        var exePath = Path.Combine(path, "SlayTheSpire2.exe");

        return Directory.Exists(modsPath) || File.Exists(exePath);
    }

    public string? GetGameModsDir(string gamePath)
    {
        if (string.IsNullOrWhiteSpace(gamePath))
        {
            return null;
        }

        var modsPath = Path.Combine(gamePath, "mods");
        return modsPath;
    }

    public string GetGamePendingModsDir(string gamePath)
    {
        return Path.Combine(gamePath, "mods_pending");
    }

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(ToolModsDir);
        Directory.CreateDirectory(BackupDir);
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(Path.Combine(BackupDir, "Mods"));
        Directory.CreateDirectory(Path.Combine(BackupDir, "Saves"));
    }

    private static IEnumerable<string> DetectFromCommonLibraryPatterns()
    {
        var result = new List<string>();
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (!drive.IsReady || drive.DriveType != DriveType.Fixed)
                {
                    continue;
                }
            }
            catch
            {
                continue;
            }

            foreach (var root in _commonLibraryRoots)
            {
                var steamCommon = Path.Combine(drive.RootDirectory.FullName, root, "steamapps", "common");
                foreach (var gameFolder in _commonGameFolderNames)
                {
                    result.Add(Path.Combine(steamCommon, gameFolder));
                }
            }
        }

        return result;
    }

    private static IEnumerable<string> DetectFromSteamLibraryFolders()
    {
        var result = new List<string>();
        foreach (var steamRoot in new[]
                 {
                     @"C:\Program Files (x86)\Steam",
                     @"C:\Program Files\Steam",
                     @"D:\Program Files (x86)\Steam",
                     @"D:\Program Files\Steam",
                     @"F:\Program Files (x86)\Steam",
                     @"F:\Program Files\Steam",
                     @"G:\Program Files (x86)\Steam",
                     @"G:\Program Files\Steam",
                     @"H:\Program Files (x86)\Steam",
                     @"H:\Program Files\Steam",
                     @"C:\Steam",
                     @"D:\Steam",
                     @"E:\Steam",
                     @"F:\Steam",
                     @"G:\Steam",
                     @"H:\Steam"
                 })
        {
            var libraryVdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryVdfPath))
            {
                continue;
            }

            try
            {
                var content = File.ReadAllText(libraryVdfPath, Encoding.UTF8);
                var matches = Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
                foreach (Match match in matches)
                {
                    var path = match.Groups[1].Value.Replace("\\\\", "\\");
                    foreach (var gameFolder in _commonGameFolderNames)
                    {
                        result.Add(Path.Combine(path, "steamapps", "common", gameFolder));
                    }
                }
            }
            catch
            {
                // 忽略单个Steam配置读取失败
            }
        }

        return result;
    }

    private void TryAddValidPaths(HashSet<string> paths, IEnumerable<string> candidates, Func<string, bool>? validator = null)
    {
        validator ??= IsValidGamePath;
        foreach (var path in candidates)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(path) && validator(path))
                {
                    paths.Add(path);
                }
            }
            catch
            {
                // 忽略单个目录判断失败，避免影响整体探测
            }
        }
    }

    private static IEnumerable<string> DetectFromToolDirectory()
    {
        var result = new List<string>();
        var baseDir = ToolBaseDir;
        if (!string.IsNullOrWhiteSpace(baseDir))
        {
            result.Add(baseDir);
        }

        try
        {
            var parent = Directory.GetParent(baseDir)?.FullName;
            if (!string.IsNullOrWhiteSpace(parent))
            {
                result.Add(parent);
            }
        }
        catch
        {
            // 忽略工具目录父级获取失败
        }

        return result;
    }
}
