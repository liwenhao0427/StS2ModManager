namespace StS2ModManager.Models;

public class SaveBackupInfo
{
    public string TimestampKey { get; set; } = string.Empty;
    public DateTime BackupTime { get; set; }
    public string SteamId { get; set; } = string.Empty;
    public string SteamBackupPath { get; set; } = string.Empty;

    public string DisplayName => $"{BackupTime:yyyy-MM-dd HH:mm:ss} | SteamID {SteamId}";
}
