namespace StS2ModManager.Models;

public class SaveBackupInfo
{
    public string BackupFolderKey { get; set; } = string.Empty;
    public string BackupName { get; set; } = string.Empty;
    public DateTime BackupTime { get; set; }
    public string SteamId { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public bool IsFullBackup { get; set; }
    public int? SlotId { get; set; }
    public bool IsModdedBackup { get; set; }

    public string BackupKindDisplay => IsFullBackup
        ? "整档"
        : $"栏位{SlotId} {(IsModdedBackup ? "Mod" : "非Mod")}";

    public string DisplayName => $"{BackupTime:yyyy-MM-dd HH:mm:ss} | {BackupName} | {BackupKindDisplay}";
}
