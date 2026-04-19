using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace StS2ModManager.Models;

public partial class ModTreeNode : ObservableObject
{
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string NodeKey { get; set; } = string.Empty;
    public ModInfo? Mod { get; set; }
    public ObservableCollection<ModTreeNode> Children { get; } = new();

    public bool IsModNode => Mod != null;

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    public bool ShowSubtitleBadge { get; set; }

    public bool ShowInlineSubtitle { get; set; }

    public bool ShowSecondaryText { get; set; }

    [ObservableProperty]
    private bool _isExpanded = true;
}
