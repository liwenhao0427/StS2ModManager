using CommunityToolkit.Mvvm.ComponentModel;

namespace StS2ModManager.Models;

public partial class SyncSourceOption : ObservableObject
{
    public string SourcePath { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;
}
