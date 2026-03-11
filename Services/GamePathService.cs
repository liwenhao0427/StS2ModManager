using System.IO;
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
        @"C:\Steam\steamapps\common\Slay the Spire 2",
        @"D:\Steam\steamapps\common\Slay the Spire 2",
        @"E:\Steam\steamapps\common\Slay the Spire 2",
        @"C:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"D:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"E:\SteamLibrary\steamapps\common\Slay the Spire 2",
        @"C:\Epic Games\SlaytheSpire2",
        @"D:\Epic Games\SlaytheSpire2",
        @"E:\Epic Games\SlaytheSpire2",
        @"C:\Games\Slay the Spire 2",
        @"D:\Games\Slay the Spire 2",
        @"E:\Games\Slay the Spire 2",
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

    public static string BackupDir => Path.Combine(ToolBaseDir, "Backup");

    public static string ConfigDir => Path.Combine(ToolBaseDir, "Config");

    public static string AliasesFile => Path.Combine(ConfigDir, "aliases.json");

    public List<string> DetectGamePaths()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in _possiblePaths)
        {
            if (IsValidGamePath(path))
            {
                paths.Add(path);
            }
        }

        foreach (var path in DetectFromCommonLibraryPatterns())
        {
            if (IsValidGamePath(path))
            {
                paths.Add(path);
            }
        }

        foreach (var path in DetectFromSteamLibraryFolders())
        {
            if (IsValidGamePath(path))
            {
                paths.Add(path);
            }
        }

        return paths.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
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
        var modsPath = Path.Combine(gamePath, "mods");
        return Directory.Exists(modsPath) ? modsPath : null;
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
        foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
        {
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
                     @"C:\Steam",
                     @"D:\Steam",
                     @"E:\Steam"
                 })
        {
            var libraryVdfPath = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryVdfPath))
            {
                continue;
            }

            try
            {
                var content = File.ReadAllText(libraryVdfPath);
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
}
