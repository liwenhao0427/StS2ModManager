using System.Diagnostics;
using System.IO;

namespace StS2ModManager.Services;

public class GameLaunchService
{
    private const string GameAppId = "2868840";

    private static readonly string[] SteamPaths =
    {
        @"C:\Program Files (x86)\Steam\steam.exe",
        @"C:\Program Files\Steam\steam.exe",
        @"D:\Program Files (x86)\Steam\steam.exe",
        @"D:\Program Files\Steam\steam.exe"
    };

    public static string? GetSteamPath()
    {
        foreach (var path in SteamPaths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    public bool LaunchGame(string gamePath, bool noMods = false)
    {
        var exePath = Path.Combine(gamePath, "SlayTheSpire2.exe");
        if (!File.Exists(exePath))
        {
            return false;
        }

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = gamePath,
                Arguments = noMods ? "--nomods" : string.Empty,
                UseShellExecute = true
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LaunchGameViaSteam()
    {
        return LaunchByUri($"steam://rungameid/{GameAppId}") || LaunchByAppLaunch(false);
    }

    public bool LaunchGameViaSteamNoMods()
    {
        return LaunchByUri($"steam://run/{GameAppId}//--nomods") || LaunchByAppLaunch(true);
    }

    public bool IsGameRunning()
    {
        return Process.GetProcessesByName("SlayTheSpire2").Any();
    }

    private static bool LaunchByUri(string uri)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = uri,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool LaunchByAppLaunch(bool noMods)
    {
        var steamPath = GetSteamPath();
        if (string.IsNullOrWhiteSpace(steamPath))
        {
            return false;
        }

        try
        {
            var arguments = noMods
                ? $"-applaunch {GameAppId} --nomods"
                : $"-applaunch {GameAppId}";

            Process.Start(new ProcessStartInfo
            {
                FileName = steamPath,
                Arguments = arguments,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
