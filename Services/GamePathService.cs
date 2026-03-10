using System.IO;

namespace StS2ModManager.Services;

public class GamePathService
{
    private static readonly string[] _possiblePaths = new[]
    {
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
        var paths = new List<string>();
        foreach (var path in _possiblePaths)
        {
            if (IsValidGamePath(path))
            {
                paths.Add(path);
            }
        }
        return paths;
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

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(ToolModsDir);
        Directory.CreateDirectory(BackupDir);
        Directory.CreateDirectory(ConfigDir);
        Directory.CreateDirectory(Path.Combine(BackupDir, "Mods"));
        Directory.CreateDirectory(Path.Combine(BackupDir, "Saves"));
    }
}
