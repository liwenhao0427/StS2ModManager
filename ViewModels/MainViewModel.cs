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
    private ObservableCollection<ModInfo> _toolMods = new();

    [ObservableProperty]
    private ModInfo? _selectedToolMod;

    [ObservableProperty]
    private ModInfo? _selectedGameMod;

    [ObservableProperty]
    private ModInfo? _selectedModForDetail;

    [ObservableProperty]
    private string _selectedModFolderPath = string.Empty;

    [ObservableProperty]
    private string _selectedModFolderName = string.Empty;

    [ObservableProperty]
    private string _selectedModUpdatedDisplay = string.Empty;

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
    private ObservableCollection<SaveBackupInfo> _saveBackups = new();

    [ObservableProperty]
    private SaveBackupInfo? _selectedSaveBackup;

    [ObservableProperty]
    private ObservableCollection<SaveOptionItem> _restoreTargets = new();

    [ObservableProperty]
    private SaveOptionItem? _selectedRestoreTarget;

    [ObservableProperty]
    private string _manualBackupName = string.Empty;

    [ObservableProperty]
    private string _modNameInput = string.Empty;

    [ObservableProperty]
    private string _modVersionInput = string.Empty;

    [ObservableProperty]
    private string _modAuthorInput = string.Empty;

    [ObservableProperty]
    private string _modDownloadUrlInput = string.Empty;

    [ObservableProperty]
    private string _modDetailInput = string.Empty;

    [ObservableProperty]
    private string _modDetailUrlInput = string.Empty;

    [ObservableProperty]
    private string _modRemarkInput = string.Empty;

    [ObservableProperty]
    private string _modAuthorUrlInput = string.Empty;

    [ObservableProperty]
    private string _modSocialUrlInput = string.Empty;

    [ObservableProperty]
    private string _modDescriptionInput = string.Empty;

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
        RefreshGameMods();

        StatusMessage = DetectedPaths.Count > 0 ? $"已找到 {DetectedPaths.Count} 个游戏路径" : "未找到游戏，请手动选择";
    }

    private void InitializeSaveOptions()
    {
        SaveDirections = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "normal_to_mod", Label = "非Mod存档 -> Mod存档" },
            new() { Key = "mod_to_normal", Label = "Mod存档 -> 非Mod存档" }
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
        SelectedSaveSlot = SaveSlots.FirstOrDefault(x => string.Equals(x.Key, savedSlot, StringComparison.OrdinalIgnoreCase)) ?? SaveSlots[0];

        RestoreTargets = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "auto", Label = "原路径（默认）" },
            new() { Key = "normal", Label = "恢复到非Mod存档" },
            new() { Key = "modded", Label = "恢复到Mod存档" }
        };
        SelectedRestoreTarget = RestoreTargets[0];
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
        if (value != null)
        {
            SetSelectedModContext(value);
            return;
        }

        ClearSelectedModContext();
    }

    partial void OnSelectedGameModChanged(ModInfo? value)
    {
        if (value != null)
        {
            SetSelectedModContext(value);
        }
    }

    private void SetSelectedModContext(ModInfo? mod)
    {
        if (mod == null)
        {
            ClearSelectedModContext();
            return;
        }

        SelectedModFolderName = mod.FolderName;
        SelectedModFolderPath = mod.FolderPath;
        SelectedModForDetail = mod;
    }

    private void ClearSelectedModContext()
    {
        SelectedModForDetail = null;
        SelectedModFolderName = string.Empty;
        SelectedModFolderPath = string.Empty;
    }

    partial void OnSelectedModFolderPathChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModNameInput = string.Empty;
            ModVersionInput = string.Empty;
            ModDetailInput = string.Empty;
            ModAuthorInput = string.Empty;
            ModDownloadUrlInput = string.Empty;
            ModRemarkInput = string.Empty;
            ModAuthorUrlInput = string.Empty;
            ModDetailUrlInput = string.Empty;
            ModSocialUrlInput = string.Empty;
            ModDescriptionInput = string.Empty;
            SelectedModUpdatedDisplay = string.Empty;
            return;
        }

        var meta = _modService.LoadModMetaByPath(value, SelectedModFolderName);
        ModNameInput = string.IsNullOrWhiteSpace(meta.Name) ? SelectedModFolderName : meta.Name;
        ModVersionInput = meta.Version ?? string.Empty;
        ModDetailInput = meta.Detail ?? string.Empty;
        ModAuthorInput = meta.Author ?? string.Empty;
        ModDownloadUrlInput = meta.DownloadUrl ?? string.Empty;
        ModRemarkInput = meta.Remark ?? string.Empty;
        ModAuthorUrlInput = meta.AuthorUrl ?? string.Empty;
        ModDetailUrlInput = meta.DetailUrl ?? string.Empty;
        ModSocialUrlInput = meta.SocialUrl ?? string.Empty;
        ModDescriptionInput = meta.Description ?? string.Empty;
        SelectedModUpdatedDisplay = Directory.Exists(value)
            ? Directory.GetLastWriteTime(value).ToString("yyyy-MM-dd HH:mm")
            : string.Empty;
    }

    partial void OnSelectedSteamIdChanged(string? value)
    {
        _modService.Settings.PreferredSteamId = value;
        _modService.SaveSettings();
        RefreshSaveBackups();
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
        if (SelectedToolMod == null)
        {
            MessageBox.Show("请先选中某个Mod后再移除其来源目录", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sourcePath = SelectedToolMod.SourcePath;
        var canRemove = _modService.Settings.CustomModSourceDirs.Any(x => string.Equals(x, sourcePath, StringComparison.OrdinalIgnoreCase));
        if (!canRemove)
        {
            MessageBox.Show("当前来源为系统目录，不能移除", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _modService.RemoveCustomModSource(sourcePath);
        RefreshModSources();
        RefreshToolMods();
        StatusMessage = "已移除来源目录";
    }

    [RelayCommand]
    private void OpenSelectedModSource()
    {
        if (SelectedToolMod == null)
        {
            MessageBox.Show("请先选中某个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (Directory.Exists(SelectedToolMod.SourcePath))
        {
            System.Diagnostics.Process.Start("explorer.exe", SelectedToolMod.SourcePath);
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
    }

    [RelayCommand]
    private void RefreshToolMods()
    {
        ToolMods.Clear();
        var sourceMods = _modService.ScanModsFromSources(ModSources);
        var gameMods = string.IsNullOrWhiteSpace(SelectedPath)
            ? new List<ModInfo>()
            : _modService.ScanGameMods(SelectedPath);
        var mods = _modService.BuildUnifiedMods(sourceMods, gameMods);
        foreach (var mod in mods)
        {
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
    private void SaveModMeta()
    {
        if (string.IsNullOrWhiteSpace(SelectedModFolderPath))
        {
            MessageBox.Show("请先选中一个Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var success = _modService.SaveModMetaByPath(SelectedModFolderPath, SelectedModFolderName, new ModMetaInfo
        {
            Name = ModNameInput,
            Version = ModVersionInput,
            Detail = ModDetailInput,
            Author = ModAuthorInput,
            DownloadUrl = ModDownloadUrlInput,
            Remark = ModRemarkInput,
            AuthorUrl = ModAuthorUrlInput,
            DetailUrl = ModDetailUrlInput,
            SocialUrl = ModSocialUrlInput,
            Description = ModDescriptionInput
        });

        if (!success)
        {
            MessageBox.Show("写入Mod信息失败，请确认该Mod目录可写", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        RefreshToolMods();
        RefreshGameMods();
        StatusMessage = "Mod信息已保存到该Mod目录";
    }

    [RelayCommand]
    private void ClearModMeta()
    {
        if (string.IsNullOrWhiteSpace(SelectedModFolderPath))
        {
            return;
        }

        var success = _modService.SaveModMetaByPath(SelectedModFolderPath, SelectedModFolderName, new ModMetaInfo());
        if (!success)
        {
            MessageBox.Show("清空Mod信息失败，请确认该Mod目录可写", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ModNameInput = SelectedModFolderName;
        ModVersionInput = string.Empty;
        ModDetailInput = string.Empty;
        ModAuthorInput = string.Empty;
        ModDownloadUrlInput = string.Empty;
        ModRemarkInput = string.Empty;
        ModAuthorUrlInput = string.Empty;
        ModDetailUrlInput = string.Empty;
        ModSocialUrlInput = string.Empty;
        ModDescriptionInput = string.Empty;
        RefreshToolMods();
        RefreshGameMods();
        StatusMessage = "Mod信息已重置";
    }

    [RelayCommand]
    private void OpenUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            var fixedUrl = url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                           url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? url
                : $"https://{url}";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = fixedUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            MessageBox.Show("无法打开链接，请检查地址格式", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void ToggleToolMod(ModInfo? mod)
    {
        if (mod == null || string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        SetSelectedModContext(mod);

        try
        {
            if (mod.IsEnabled)
            {
                if (!_modService.ApplySingleMod(SelectedPath, mod))
                {
                    mod.IsEnabled = false;
                    MessageBox.Show("生效失败，请检查Mod目录", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusMessage = $"已生效 {mod.DisplayName}";
            }
            else
            {
                if (!_modService.MoveGameModToPendingByFolderName(SelectedPath, mod.FolderName))
                {
                    mod.IsEnabled = true;
                    MessageBox.Show("取消生效失败，未找到游戏内对应Mod", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = $"已取消生效 {mod.DisplayName}";
            }

            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();
        }
        catch (Exception ex)
        {
            mod.IsEnabled = !mod.IsEnabled;
            MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void ToggleGameMod(ModInfo? mod)
    {
        if (mod == null || mod.IsEnabled || string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        SetSelectedModContext(mod);

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
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        var toEnable = ToolMods
            .Where(x => !x.IsEnabled)
            .GroupBy(x => x.FolderName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        foreach (var mod in toEnable)
        {
            _modService.ApplySingleMod(SelectedPath, mod);
        }

        RefreshModSources();
        RefreshToolMods();
        RefreshGameMods();
        StatusMessage = "已批量生效所有可用Mod";
    }

    [RelayCommand]
    private void DeselectAllMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            return;
        }

        var toDisable = ToolMods
            .Where(x => x.IsEnabled)
            .GroupBy(x => x.FolderName, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
        foreach (var mod in toDisable)
        {
            _modService.MoveGameModToPendingByFolderName(SelectedPath, mod.FolderName);
        }

        RefreshModSources();
        RefreshToolMods();
        RefreshGameMods();
        StatusMessage = "已批量取消所有生效Mod";
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

        RefreshSaveBackups();
    }

    [RelayCommand]
    private void RefreshSaveBackups()
    {
        SaveBackups.Clear();
        var backups = _saveService.GetSaveBackups(SelectedSteamId);
        foreach (var backup in backups)
        {
            SaveBackups.Add(backup);
        }

        SelectedSaveBackup = SaveBackups.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CopySave()
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

        var direction = SelectedSaveDirection.Key == "mod_to_normal"
            ? SaveCopyDirection.ModdedToNormal
            : SaveCopyDirection.NormalToModded;

        var directionText = direction == SaveCopyDirection.NormalToModded
            ? "非Mod -> Mod"
            : "Mod -> 非Mod";

        var confirmResult = MessageBox.Show(
            $"⚠️ 确认复制存档？\n\n" +
            $"Steam ID: {SelectedSteamId}\n" +
            $"方向: {directionText}\n" +
            $"栏位: {SelectedSaveSlot.Label}\n\n" +
            "会先把目标栏位备份到存档目录同级的 Backup/Saves，再执行复制。\n" +
            "是否继续？",
            "确认复制",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes)
        {
            return;
        }

        StatusMessage = "正在复制存档...";
        SaveCopyResult result;
        try
        {
            result = await Task.Run(() => _saveService.CopyWithinSteamId(SelectedSteamId, direction, SelectedSaveSlot.Key));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"存档复制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "存档复制失败";
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show("存档复制失败，请确认源栏位存档是否存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "存档复制失败";
            return;
        }

        StatusMessage = $"已复制 {result.CopiedCount} 个栏位（{directionText}）";
        MessageBox.Show($"存档复制成功！\n备份位置: {result.BackupPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshSaveBackups();
    }

    [RelayCommand]
    private async Task RestoreSaveBackup()
    {
        if (SelectedSaveBackup == null)
        {
            MessageBox.Show("请先选择要恢复的备份存档", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var backup = SelectedSaveBackup;
        var confirm = MessageBox.Show(
            $"确认恢复该备份？\n\n" +
            $"时间: {backup.BackupTime:yyyy-MM-dd HH:mm:ss}\n" +
            $"Steam ID: {backup.SteamId}\n" +
            $"备份路径: {backup.BackupPath}\n" +
            $"备份类型: {backup.BackupKindDisplay}\n" +
            $"恢复目标: {SelectedRestoreTarget?.Label ?? "原路径（默认）"}\n\n" +
            "当前存档会先自动备份后再恢复。",
            "恢复存档确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        StatusMessage = "正在恢复存档...";
        SaveCopyResult result;
        try
        {
            var targetMode = SelectedRestoreTarget?.Key ?? "auto";
            result = await Task.Run(() => _saveService.RestoreFromBackup(backup, targetMode));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"恢复存档失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "恢复存档失败";
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show("恢复存档失败，备份路径可能已失效", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "恢复存档失败";
            return;
        }

        if (!string.Equals(SelectedSteamId, backup.SteamId, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSteamId = backup.SteamId;
        }

        StatusMessage = "存档恢复成功";
        var rescueText = string.IsNullOrWhiteSpace(result.BackupPath) ? "未生成恢复前备份" : result.BackupPath;
        MessageBox.Show($"恢复完成。\n恢复前备份: {rescueText}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshSaveBackups();
    }

    [RelayCommand]
    private async Task CreateManualBackup()
    {
        if (string.IsNullOrWhiteSpace(SelectedSteamId))
        {
            MessageBox.Show("请先选择Steam ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StatusMessage = "正在执行手动备份...";
        SaveCopyResult result;
        try
        {
            result = await Task.Run(() => _saveService.CreateManualBackup(SelectedSteamId, ManualBackupName));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"手动备份失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "手动备份失败";
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show("手动备份失败，请确认Steam ID目录存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = "手动备份失败";
            return;
        }

        StatusMessage = "手动备份完成";
        ManualBackupName = string.Empty;
        RefreshSaveBackups();
        MessageBox.Show($"手动备份成功！\n备份位置: {result.BackupPath}", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
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
