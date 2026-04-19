using CommunityToolkit.Mvvm.ComponentModel;
using System.IO;

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
    public string MetadataFilePath { get; set; } = string.Empty;

    [ObservableProperty]
    private string _author = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string _tag = string.Empty;

    [ObservableProperty]
    private string _detail = string.Empty;

    [ObservableProperty]
    private string _remark = string.Empty;

    [ObservableProperty]
    private string _authorUrl = string.Empty;

    [ObservableProperty]
    private string _socialUrl = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _detailUrl = string.Empty;

    [ObservableProperty]
    private string _downloadUrl = string.Empty;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _originalName = string.Empty;

    [ObservableProperty]
    private string _aliasName = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;

    public string LocationDisplay => SourceName;

    public string AuthorDisplay => string.IsNullOrWhiteSpace(Author) ? "未知作者" : Author;

    public string UpdatedDisplay => ModifiedTime.ToString("yyyy-MM-dd HH:mm");

    public string VersionDisplay => string.IsNullOrWhiteSpace(Version) ? "-" : Version;

    public string RelativeFolderPath => string.IsNullOrWhiteSpace(SourcePath) || string.IsNullOrWhiteSpace(FolderPath)
        ? FolderName
        : Path.GetRelativePath(SourcePath, FolderPath);

    public string SecondaryDisplayName => string.IsNullOrWhiteSpace(AliasName) || string.Equals(DisplayName, AliasName, StringComparison.OrdinalIgnoreCase)
        ? string.Empty
        : AliasName;

    public bool HasSecondaryDisplayName => !string.IsNullOrWhiteSpace(SecondaryDisplayName);

    public string SizeDisplay => Size switch
    {
        < 1024 => $"{Size} B",
        < 1024 * 1024 => $"{Size / 1024.0:F1} KB",
        _ => $"{Size / (1024.0 * 1024.0):F1} MB"
    };
}
