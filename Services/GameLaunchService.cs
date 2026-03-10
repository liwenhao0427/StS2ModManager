using System.Diagnostics;
using System.IO;

namespace StS2ModManager.Services;

public class GameLaunchService
{
    private static readonly string[] SteamPaths = new[]
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
                return path;
        }
        return null;
    }

    public bool LaunchGame(string gamePath, bool noMods = false)
    {
        var exePath = Path.Combine(gamePath, "SlayTheSpire2.exe");
        if (!File.Exists(exePath))
            return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = gamePath
            };

            if (noMods)
            {
                startInfo.Arguments = "--nomods";
            }

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
        var steamPath = GetSteamPath();
        if (steamPath == null) return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = steamPath,
                Arguments = "-applaunch 2291400"
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LaunchGameViaSteamNoMods()
    {
        var steamPath = GetSteamPath();
        if (steamPath == null) return false;

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = steamPath,
                Arguments = "-applaunch 2291400 --nomods"
            };

            Process.Start(startInfo);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool IsGameRunning()
    {
        return Process.GetProcessesByName("SlayTheSpire2").Any();
    }
}
