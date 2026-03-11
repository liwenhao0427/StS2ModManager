using CommunityToolkit.Mvvm.ComponentModel;

namespace StS2ModManager.Models;

public partial class ModInfo : ObservableObject
{
    public string ModKey { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime ModifiedTime { get; set; }
    public bool IsFromGameDir { get; set; }

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    public string LocationDisplay => SourceName;

    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        _ => $"{Size / (1024.0 * 1024.0):F1} MB"
    };
}
