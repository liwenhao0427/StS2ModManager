using System.IO;

namespace StS2ModManager.Models;

public class ModInfo
{
    public string FileName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedTime { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsFromGameDir { get; set; }

    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        _ => $"{Size / (1024.0 * 1024.0):F1} MB"
    };
}
