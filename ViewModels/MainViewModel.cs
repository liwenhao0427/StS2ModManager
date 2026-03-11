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
    private ObservableCollection<ModSourceInfo> _modSources = new();

    [ObservableProperty]
    private ModSourceInfo? _selectedModSource;

    [ObservableProperty]
    private ObservableCollection<ModInfo> _toolMods = new();

    [ObservableProperty]
    private ModInfo? _selectedToolMod;

    [ObservableProperty]
    private ObservableCollection<ModInfo> _gameMods = new();

    [ObservableProperty]
    private ObservableCollection<string> _steamIds = new();

    [ObservableProperty]
    private string? _selectedSteamId;

    [ObservableProperty]
    private ObservableCollection<SaveOptionItem> _saveDirections = new();

    [ObservableProperty]
    private SaveOptionItem? _selectedSaveDirection;

    [ObservableProperty]
    private ObservableCollection<SaveOptionItem> _saveSlots = new();

    [ObservableProperty]
    private SaveOptionItem? _selectedSaveSlot;

    [ObservableProperty]
    private string _aliasInput = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    public MainViewModel()
    {
        _pathService = new GamePathService();
        var settingsService = new SettingsService();
        _modService = new ModService(_pathService, settingsService);
        _saveService = new SaveService();
        _launchService = new GameLaunchService();

        GamePathService.EnsureDirectoriesExist();
        InitializeSaveOptions();
        Initialize();
    }

    private void Initialize()
    {
        RefreshPaths();
        RefreshSteamIds();
        RefreshModSources();
        RefreshToolMods();

        StatusMessage = DetectedPaths.Count > 0 ? $"已找到 {DetectedPaths.Count} 个游戏路径" : "未找到游戏，请手动选择";
    }

    private void InitializeSaveOptions()
    {
        SaveDirections = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "mod_to_normal", Label = "Mod存档 -> 非Mod存档" },
            new() { Key = "normal_to_mod", Label = "非Mod存档 -> Mod存档" }
        };

        SaveSlots = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "1", Label = "栏位1" },
            new() { Key = "2", Label = "栏位2" },
            new() { Key = "3", Label = "栏位3" },
            new() { Key = "all", Label = "全部栏位" }
        };

        SelectedSaveDirection = SaveDirections[0];
        var savedSlot = _modService.Settings.PreferredSaveSlot;
        SelectedSaveSlot = SaveSlots.FirstOrDefault(x => string.Equals(x.Key, savedSlot, StringComparison.OrdinalIgnoreCase))
            ?? SaveSlots[0];
    }

    partial void OnSelectedPathChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            GamePathStatus = "未选择";
            return;
        }

        if (_pathService.IsValidGamePath(value))
        {
            GamePathStatus = "✓ 已找到";
            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();
            return;
        }

        GamePathStatus = "✗ 无效路径";
    }

    partial void OnSelectedToolModChanged(ModInfo? value)
    {
        AliasInput = value?.DisplayName ?? string.Empty;
    }

    partial void OnSelectedSteamIdChanged(string? value)
    {
        _modService.Settings.PreferredSteamId = value;
        _modService.SaveSettings();
    }

    partial void OnSelectedSaveSlotChanged(SaveOptionItem? value)
    {
        if (value == null)
        {
            return;
        }

        _modService.Settings.PreferredSaveSlot = value.Key;
        _modService.SaveSettings();
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

        if (DetectedPaths.Count > 0 && (SelectedPath == null || !DetectedPaths.Contains(SelectedPath)))
        {
            SelectedPath = DetectedPaths[0];
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

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (!_pathService.IsValidGamePath(dialog.FolderName))
        {
            MessageBox.Show("选择的目录不是有效的游戏目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!DetectedPaths.Contains(dialog.FolderName))
        {
            DetectedPaths.Add(dialog.FolderName);
        }

        SelectedPath = dialog.FolderName;
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
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
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var modsPath = _pathService.GetGameModsDir(SelectedPath) ?? Path.Combine(SelectedPath, "mods");
        Directory.CreateDirectory(modsPath);
        System.Diagnostics.Process.Start("explorer.exe", modsPath);
    }

    [RelayCommand]
    private void OpenPendingModsDirectory()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pendingPath = _pathService.GetGamePendingModsDir(SelectedPath);
        Directory.CreateDirectory(pendingPath);
        System.Diagnostics.Process.Start("explorer.exe", pendingPath);
    }

    [RelayCommand]
    private void OpenToolModsDirectory()
    {
        Directory.CreateDirectory(GamePathService.ToolModsDir);
        System.Diagnostics.Process.Start("explorer.exe", GamePathService.ToolModsDir);
    }

    [RelayCommand]
    private void AddModSource()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "选择Mod来源目录"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (_modService.AddCustomModSource(dialog.FolderName))
        {
            RefreshModSources();
            RefreshToolMods();
            StatusMessage = "已添加自定义Mod目录";
            return;
        }

        MessageBox.Show("目录无效或已存在", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void RemoveModSource()
    {
        if (SelectedModSource == null)
        {
            MessageBox.Show("请先选中要移除的来源目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedModSource.IsSystem)
        {
            MessageBox.Show("系统目录不能移除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _modService.RemoveCustomModSource(SelectedModSource.Path);
        RefreshModSources();
        RefreshToolMods();
        StatusMessage = "已移除自定义Mod目录";
    }

    [RelayCommand]
    private void OpenSelectedModSource()
    {
        if (SelectedModSource == null)
        {
            return;
        }

        if (Directory.Exists(SelectedModSource.Path))
        {
            System.Diagnostics.Process.Start("explorer.exe", SelectedModSource.Path);
        }
    }

    [RelayCommand]
    private void RefreshModSources()
    {
        ModSources.Clear();
        var sources = _modService.BuildModSources(SelectedPath);
        foreach (var source in sources)
        {
            ModSources.Add(source);
        }

        SelectedModSource = ModSources.FirstOrDefault();
    }

    [RelayCommand]
    private void RefreshToolMods()
    {
        ToolMods.Clear();
        var mods = _modService.ScanModsFromSources(ModSources);
        foreach (var mod in mods)
        {
            mod.IsEnabled = false;
            ToolMods.Add(mod);
        }
    }

    [RelayCommand]
    private void RefreshGameMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        GameMods.Clear();
        var mods = _modService.ScanGameMods(SelectedPath);
        foreach (var mod in mods)
        {
            mod.IsEnabled = true;
            GameMods.Add(mod);
        }
    }

    [RelayCommand]
    private void OpenModFolder(ModInfo? mod)
    {
        if (mod == null || !Directory.Exists(mod.FolderPath))
        {
            return;
        }

        System.Diagnostics.Process.Start("explorer.exe", mod.FolderPath);
    }

    [RelayCommand]
    private void SaveAlias()
    {
        if (SelectedToolMod == null)
        {
            MessageBox.Show("请先在左侧列表选中一个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _modService.SetAlias(SelectedToolMod.ModKey, AliasInput);
        RefreshToolMods();
        StatusMessage = "备注名已保存";
    }

    [RelayCommand]
    private void ClearAlias()
    {
        if (SelectedToolMod == null)
        {
            return;
        }

        _modService.SetAlias(SelectedToolMod.ModKey, string.Empty);
        AliasInput = SelectedToolMod.FolderName;
        RefreshToolMods();
        StatusMessage = "备注名已清除";
    }

    [RelayCommand]
    private void ApplyMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var enabledMods = ToolMods.Where(x => x.IsEnabled).ToList();
        if (enabledMods.Count == 0)
        {
            MessageBox.Show("请先在左侧勾选至少一个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var count = _modService.ApplyMods(SelectedPath, enabledMods);
            RefreshGameMods();
            StatusMessage = $"已应用 {count} 个Mod";
            MessageBox.Show($"应用完成，已生效 {count} 个Mod。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"应用Mod失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void RemoveMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (GameMods.Count == 0)
        {
            MessageBox.Show("当前没有可取出的生效Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        try
        {
            var count = _modService.RemoveModsToTool(SelectedPath);
            RefreshToolMods();
            StatusMessage = $"已取出 {count} 个Mod到工具目录";
            MessageBox.Show($"取出完成，共复制 {count} 个Mod。", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"取出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ToggleGameMod(ModInfo? mod)
    {
        if (mod == null || mod.IsEnabled || string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        try
        {
            if (_modService.MoveGameModToPending(SelectedPath, mod))
            {
                RefreshModSources();
                RefreshToolMods();
                RefreshGameMods();
                StatusMessage = $"已移出 {mod.DisplayName} 到待生效目录";
                return;
            }

            mod.IsEnabled = true;
            MessageBox.Show("移出失败，未找到对应Mod目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            mod.IsEnabled = true;
            MessageBox.Show($"移出失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
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
    private void RefreshSteamIds()
    {
        SteamIds.Clear();
        var ids = _saveService.GetAllSteamIds();
        foreach (var id in ids)
        {
            SteamIds.Add(id);
        }

        var preferredId = _modService.Settings.PreferredSteamId;
        var latestId = _saveService.GetLatestSteamId();
        if (!string.IsNullOrWhiteSpace(preferredId) && SteamIds.Contains(preferredId))
        {
            SelectedSteamId = preferredId;
        }
        else
        {
            SelectedSteamId = latestId;
        }
    }

    [RelayCommand]
    private void CopySave()
    {
        if (string.IsNullOrWhiteSpace(SelectedSteamId))
        {
            MessageBox.Show("未找到可用Steam ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedSaveDirection == null || SelectedSaveSlot == null)
        {
            MessageBox.Show("请选择复制方向和栏位", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var direction = SelectedSaveDirection.Key == "normal_to_mod"
            ? SaveCopyDirection.NormalToModded
            : SaveCopyDirection.ModdedToNormal;

        var directionText = direction == SaveCopyDirection.NormalToModded
            ? "非Mod -> Mod"
            : "Mod -> 非Mod";
        var slotText = SelectedSaveSlot.Label;

        var confirmResult = MessageBox.Show(
            $"⚠️ 确认复制存档？\n\n" +
            $"Steam ID: {SelectedSteamId}\n" +
            $"方向: {directionText}\n" +
            $"栏位: {slotText}\n\n" +
            "操作将覆盖目标栏位，复制前会自动备份。\n" +
            "备份目录: Backup/Saves/时间戳\n\n" +
            "是否继续？",
            "确认复制",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes)
        {
            return;
        }

        var backupPath = _saveService.BackupSteamIdSaves(SelectedSteamId);
        var success = _saveService.CopyWithinSteamId(SelectedSteamId, direction, SelectedSaveSlot.Key);
        if (!success)
        {
            MessageBox.Show("存档复制失败，请确认源栏位存档是否存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        StatusMessage = $"已完成存档复制（{directionText}，{slotText}）";
        var backupText = string.IsNullOrWhiteSpace(backupPath) ? "备份失败或无可备份内容" : backupPath;
        MessageBox.Show($"存档复制成功！\n备份位置: {backupText}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void OpenSavesDirectory()
    {
        _saveService.OpenSavesDirectory();
    }

    [RelayCommand]
    private void OpenBackupDirectory()
    {
        var backupDir = GamePathService.BackupDir;
        Directory.CreateDirectory(backupDir);
        System.Diagnostics.Process.Start("explorer.exe", backupDir);
    }

    [RelayCommand]
    private void LaunchGame()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, false))
        {
            StatusMessage = "游戏已启动";
            return;
        }

        MessageBox.Show("启动游戏失败，请检查游戏路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameNoMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show("请先选择游戏路径", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, true))
        {
            StatusMessage = "游戏已启动（无Mod）";
            return;
        }

        MessageBox.Show("启动游戏失败，请检查游戏路径", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameViaSteam()
    {
        if (_launchService.LaunchGameViaSteam())
        {
            StatusMessage = "已通过Steam启动游戏";
            return;
        }

        MessageBox.Show("通过Steam启动失败，请确认Steam已安装并已登录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameViaSteamNoMods()
    {
        if (_launchService.LaunchGameViaSteamNoMods())
        {
            StatusMessage = "已通过Steam启动游戏（无Mod）";
            return;
        }

        MessageBox.Show("通过Steam启动失败，请确认Steam已安装并已登录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
