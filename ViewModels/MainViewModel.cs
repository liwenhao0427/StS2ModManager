using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StS2ModManager.Models;
using StS2ModManager.Services;

namespace StS2ModManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly GamePathService _pathService;
    private readonly ModService _modService;
    private readonly SaveService _saveService;
    private readonly GameLaunchService _launchService;

    [ObservableProperty]
    private ObservableCollection<string> _detectedPaths = new();

    [ObservableProperty]
    private string? _selectedPath;

    [ObservableProperty]
    private string _gamePathStatus = "未选择";

    [ObservableProperty]
    private ObservableCollection<ModInfo> _toolMods = new();

    [ObservableProperty]
    private ObservableCollection<ModInfo> _gameMods = new();

    [ObservableProperty]
    private ObservableCollection<string> _steamIds = new();

    [ObservableProperty]
    private string? _selectedSourceId;

    [ObservableProperty]
    private string? _selectedDestId;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private string _modAliasInput = string.Empty;

    [ObservableProperty]
    private ModInfo? _selectedModForAlias;

    public MainViewModel()
    {
        _pathService = new GamePathService();
        _modService = new ModService(_pathService);
        _saveService = new SaveService(_pathService);
        _launchService = new GameLaunchService();

        GamePathService.EnsureDirectoriesExist();
        Initialize();
    }

    private void Initialize()
    {
        var paths = _pathService.DetectGamePaths();
        foreach (var path in paths)
        {
            DetectedPaths.Add(path);
        }

        if (DetectedPaths.Count > 0)
        {
            SelectedPath = DetectedPaths[0];
        }

        RefreshToolMods();
        RefreshSteamIds();

        StatusMessage = DetectedPaths.Count > 0 ? $"已找到 {DetectedPaths.Count} 个游戏路径" : "未找到游戏，请手动选择";
    }

    partial void OnSelectedPathChanged(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            GamePathStatus = "未选择";
            return;
        }

        if (_pathService.IsValidGamePath(value))
        {
            GamePathStatus = "✓ 已找到";
            RefreshGameMods();
        }
        else
        {
            GamePathStatus = "✗ 无效路径";
        }
    }

    [RelayCommand]
    private void RefreshPaths()
    {
        DetectedPaths.Clear();
        var paths = _pathService.DetectGamePaths();
        foreach (var path in paths)
        {
            DetectedPaths.Add(path);
        }
        StatusMessage = $"已刷新，找到 {paths.Count} 个路径";
    }

    [RelayCommand]
    private void BrowseGamePath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择游戏目录"
        };

        if (dialog.ShowDialog() == true)
        {
            if (_pathService.IsValidGamePath(dialog.FolderName))
            {
                if (!DetectedPaths.Contains(dialog.FolderName))
                {
                    DetectedPaths.Add(dialog.FolderName);
                }
                SelectedPath = dialog.FolderName;
            }
            else
            {
                MessageBox.Show("选择的目录不是有效的游戏目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Directory.Exists(SelectedPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", SelectedPath);
        }
    }

    [RelayCommand]
    private void OpenGameModsDirectory()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var modsPath = _pathService.GetGameModsDir(SelectedPath);
        if (modsPath != null && Directory.Exists(modsPath))
        {
            System.Diagnostics.Process.Start("explorer.exe", modsPath);
        }
        else
        {
            MessageBox.Show("游戏Mods目录不存在", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void RefreshToolMods()
    {
        ToolMods.Clear();
        var mods = _modService.ScanToolMods();
        foreach (var mod in mods)
        {
            mod.DisplayName = _modService.GetDisplayName(mod.FileName);
            ToolMods.Add(mod);
        }
    }

    [RelayCommand]
    private void RefreshGameMods()
    {
        if (string.IsNullOrEmpty(SelectedPath)) return;

        GameMods.Clear();
        var mods = _modService.ScanGameMods(SelectedPath);
        foreach (var mod in mods)
        {
            mod.IsEnabled = true;
            GameMods.Add(mod);
        }
    }

    [RelayCommand]
    private void RefreshSteamIds()
    {
        SteamIds.Clear();
        var ids = _saveService.GetAllSteamIds();
        foreach (var id in ids)
        {
            SteamIds.Add(id);
        }
    }

    [RelayCommand]
    private void ApplyMods()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var enabledMods = ToolMods.Where(m => m.IsEnabled).ToList();
        if (enabledMods.Count == 0)
        {
            MessageBox.Show("请至少选择一个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            _modService.ApplyMods(SelectedPath, enabledMods);
            RefreshGameMods();
            StatusMessage = $"已应用 {enabledMods.Count} 个Mod";
            MessageBox.Show($"Mod应用成功！\n已启用 {enabledMods.Count} 个Mod", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用Mod失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RemoveMods()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (GameMods.Count == 0)
        {
            MessageBox.Show("游戏目录下没有Mod可取出", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"确认将 {GameMods.Count} 个Mod从游戏目录取出到工具目录？\n\n" +
            "操作: 将游戏Mods目录下的所有Mod复制到工具Mods目录\n" +
            "游戏目录下的Mod不会被删除",
            "确认取出",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question
        );

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                _modService.RemoveModsToTool(SelectedPath);
                RefreshToolMods();
                RefreshGameMods();
                StatusMessage = $"已取出 {GameMods.Count} 个Mod到工具目录";
                MessageBox.Show($"Mod取出成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"取出Mod失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void SelectAllMods()
    {
        foreach (var mod in ToolMods)
        {
            mod.IsEnabled = true;
        }
    }

    [RelayCommand]
    private void DeselectAllMods()
    {
        foreach (var mod in ToolMods)
        {
            mod.IsEnabled = false;
        }
    }

    [RelayCommand]
    private void CopySaveToAnother()
    {
        if (string.IsNullOrEmpty(SelectedSourceId) || string.IsNullOrEmpty(SelectedDestId))
        {
            MessageBox.Show("请选择源和目标Steam ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedSourceId == SelectedDestId)
        {
            MessageBox.Show("源和目标不能相同", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"⚠️ 确认复制存档？\n\n" +
            $"操作: 将 {SelectedSourceId} 目录下的所有内容\n复制到 {SelectedDestId} 目录（先删除目标目录）\n\n" +
            "⚠️ 风险提示:\n" +
            $"目标目录 {SelectedDestId} 的所有存档将被覆盖！\n" +
            "操作前已自动备份到: Backup/Saves/\n如需恢复，请手动从备份目录恢复。\n\n" +
            "是否确认操作？",
            "确认复制",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );

        if (result == MessageBoxResult.Yes)
        {
            if (_saveService.CopySteamIdToAnother(SelectedSourceId, SelectedDestId))
            {
                MessageBox.Show("存档复制成功！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("存档复制失败，请检查Steam ID是否正确", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void OpenSavesDirectory()
    {
        _saveService.OpenSavesDirectory();
    }

    [RelayCommand]
    private void LaunchGame()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, false))
        {
            StatusMessage = "游戏已启动";
        }
        else
        {
            MessageBox.Show("启动游戏失败，请检查游戏路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void LaunchGameNoMods()
    {
        if (string.IsNullOrEmpty(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, true))
        {
            StatusMessage = "游戏已启动(无Mod)";
        }
        else
        {
            MessageBox.Show("启动游戏失败，请检查游戏路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void LaunchGameViaSteam()
    {
        if (_launchService.LaunchGameViaSteam())
        {
            StatusMessage = "已通过Steam启动游戏";
        }
        else
        {
            MessageBox.Show("通过Steam启动失败，请确保Steam已安装", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void LaunchGameViaSteamNoMods()
    {
        if (_launchService.LaunchGameViaSteamNoMods())
        {
            StatusMessage = "已通过Steam启动游戏(无Mod)";
        }
        else
        {
            MessageBox.Show("通过Steam启动失败，请确保Steam已安装", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenBackupDirectory()
    {
        var backupDir = GamePathService.BackupDir;
        if (Directory.Exists(backupDir))
        {
            System.Diagnostics.Process.Start("explorer.exe", backupDir);
        }
    }

    [RelayCommand]
    private void OpenToolModsDirectory()
    {
        var modsDir = GamePathService.ToolModsDir;
        if (Directory.Exists(modsDir))
        {
            System.Diagnostics.Process.Start("explorer.exe", modsDir);
        }
        else
        {
            Directory.CreateDirectory(modsDir);
            System.Diagnostics.Process.Start("explorer.exe", modsDir);
        }
    }
}
