using System.Diagnostics;
using System.IO;

namespace StS2ModManager.Services;

public class GameLaunchService
{
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

    public bool IsGameRunning()
    {
        return Process.GetProcessesByName("SlayTheSpire2").Any();
    }
}
