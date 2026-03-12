namespace StS2ModManager.Models;

public class SaveBackupInfo
{
    public static Func<string, string>? Localize { get; set; }

    public string BackupFolderKey { get; set; } = string.Empty;
    public string BackupName { get; set; } = string.Empty;
    public DateTime BackupTime { get; set; }
    public string SteamId { get; set; } = string.Empty;
    public string BackupPath { get; set; } = string.Empty;
    public bool IsFullBackup { get; set; }
    public int? SlotId { get; set; }
    public bool IsModdedBackup { get; set; }

    public string BackupKindDisplay => IsFullBackup
        ? T("Save.BackupKind.Full")
        : string.Format(
            T("Save.BackupKind.Slot"),
            SlotId ?? 0,
            IsModdedBackup ? T("Save.BackupKind.Modded") : T("Save.BackupKind.Normal"));

    public string DisplayName => string.Format(T("Save.Backup.Display"), BackupTime, GetLocalizedBackupName(), BackupKindDisplay);

    private static string T(string key)
    {
        return Localize?.Invoke(key) ?? key;
    }

    private string GetLocalizedBackupName()
    {
        return BackupName switch
        {
            "#AUTO#" or "自动备份" or "Auto Backup" => T("Save.BackupName.Auto"),
            "#MANUAL#" or "手动备份" or "Manual Backup" => T("Save.BackupName.Manual"),
            "#RESTORE_BEFORE#" or "恢复前备份" or "Before Restore Backup" => T("Save.BackupName.RestoreBefore"),
            _ => BackupName
        };
    }
}
