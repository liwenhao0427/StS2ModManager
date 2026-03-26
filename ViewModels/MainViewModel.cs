using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StS2ModManager.Models;
using StS2ModManager.Services;

namespace StS2ModManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private static readonly Regex TagSplitRegex = new("[,;|/\\n\\r，、]+", RegexOptions.Compiled);
    private static readonly Regex DependencySplitRegex = new("[,;|\\n\\r，、]+", RegexOptions.Compiled);

    private readonly GamePathService _pathService;
    private readonly ModService _modService;
    private readonly SaveService _saveService;
    private readonly GameLaunchService _launchService;
    private readonly LocalizationService _localizationService;
    private readonly GithubModSyncService _githubSyncService;
    private bool _isUpdatingGithubSyncState;

    [ObservableProperty]
    private ObservableCollection<string> _detectedPaths = new();

    [ObservableProperty]
    private string? _selectedPath;

    [ObservableProperty]
    private string _gamePathStatus = string.Empty;

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
    private string _modIdInput = string.Empty;

    [ObservableProperty]
    private string _modTagInput = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _modTags = new();

    [ObservableProperty]
    private string _newTagInput = string.Empty;

    [ObservableProperty]
    private string? _selectedTagSuggestion;

    [ObservableProperty]
    private ObservableCollection<string> _availableTags = new();

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
    private string _modDependenciesInput = string.Empty;

    [ObservableProperty]
    private bool _modAffectsGameplayInput = false;

    [ObservableProperty]
    private string _modSearchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _tagFilters = new();

    [ObservableProperty]
    private string _selectedTagFilter = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _authorFilters = new();

    [ObservableProperty]
    private string _selectedAuthorFilter = string.Empty;

    [ObservableProperty]
    private ObservableCollection<LanguageOption> _languageOptions = new();

    [ObservableProperty]
    private LanguageOption? _selectedLanguageOption;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _githubSyncProgressText = string.Empty;

    [ObservableProperty]
    private bool _isGithubSyncRunning;

    [ObservableProperty]
    private bool _modGithubSyncEnabledInput;

    [ObservableProperty]
    private bool _modGithubSyncAvailableInput = true;

    [ObservableProperty]
    private string _modGithubRepoUrlInput = string.Empty;

    public LocalizationService Loc => _localizationService;

    public ICollectionView FilteredToolModsView { get; }

    public MainViewModel()
    {
        _pathService = new GamePathService();
        var settingsService = new SettingsService();
        _modService = new ModService(_pathService, settingsService);
        _githubSyncService = new GithubModSyncService(_modService);
        _saveService = new SaveService();
        _launchService = new GameLaunchService();
        _localizationService = new LocalizationService(_modService.Settings.LanguageMode);
        _localizationService.LanguageChanged += HandleLanguageChanged;
        SaveBackupInfo.Localize = L;

        LanguageOptions = new ObservableCollection<LanguageOption>(_localizationService.GetLanguageOptions());
        SelectedLanguageOption = LanguageOptions.FirstOrDefault(x => string.Equals(x.Key, _localizationService.LanguageMode, StringComparison.OrdinalIgnoreCase))
                                 ?? LanguageOptions.FirstOrDefault();

        FilteredToolModsView = CollectionViewSource.GetDefaultView(ToolMods);
        FilteredToolModsView.Filter = FilterToolMod;

        GamePathService.EnsureDirectoriesExist();
        InitializeSaveOptions();
        Initialize();

        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            GamePathStatus = L("Path.Status.None");
        }
    }

    private void Initialize()
    {
        RefreshPaths();
        RefreshSteamIds();
        RefreshModSources();
        RefreshToolMods();
        RefreshGameMods();

        StatusMessage = DetectedPaths.Count > 0
            ? string.Format(L("Status.PathFound"), DetectedPaths.Count)
            : L("Status.PathNotFound");
    }

    private void InitializeSaveOptions()
    {
        SaveDirections = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "normal_to_mod", Label = L("Save.Direction.NormalToMod") },
            new() { Key = "mod_to_normal", Label = L("Save.Direction.ModToNormal") }
        };

        SaveSlots = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "1", Label = L("Save.Slot1") },
            new() { Key = "2", Label = L("Save.Slot2") },
            new() { Key = "3", Label = L("Save.Slot3") },
            new() { Key = "all", Label = L("Save.SlotAll") }
        };

        SelectedSaveDirection = SaveDirections[0];
        var savedSlot = _modService.Settings.PreferredSaveSlot;
        SelectedSaveSlot = SaveSlots.FirstOrDefault(x => string.Equals(x.Key, savedSlot, StringComparison.OrdinalIgnoreCase)) ?? SaveSlots[0];

        RestoreTargets = new ObservableCollection<SaveOptionItem>
        {
            new() { Key = "auto", Label = L("Save.Restore.Auto") },
            new() { Key = "normal", Label = L("Save.Restore.Normal") },
            new() { Key = "modded", Label = L("Save.Restore.Modded") }
        };
        SelectedRestoreTarget = RestoreTargets[0];
    }

    partial void OnSelectedPathChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            _modService.Settings.PreferredGamePath = null;
            _modService.SaveSettings();
            GamePathStatus = L("Path.Status.None");
            return;
        }

        var normalizedPath = value.Trim();
        if (!string.Equals(_modService.Settings.PreferredGamePath, normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            _modService.Settings.PreferredGamePath = normalizedPath;
            _modService.SaveSettings();
        }

        if (_pathService.IsValidGamePath(normalizedPath))
        {
            GamePathStatus = L("Path.Status.Valid");
            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();
            return;
        }

        GamePathStatus = L("Path.Status.Invalid");
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
            ModIdInput = string.Empty;
            ModTagInput = string.Empty;
            ModTags.Clear();
            NewTagInput = string.Empty;
            ModVersionInput = string.Empty;
            ModDetailInput = string.Empty;
            ModAuthorInput = string.Empty;
            ModDownloadUrlInput = string.Empty;
            ModRemarkInput = string.Empty;
            ModAuthorUrlInput = string.Empty;
            ModDetailUrlInput = string.Empty;
            ModSocialUrlInput = string.Empty;
            ModDescriptionInput = string.Empty;
            ModDependenciesInput = string.Empty;
            ModAffectsGameplayInput = false;
            SelectedModUpdatedDisplay = string.Empty;
            _isUpdatingGithubSyncState = true;
            ModGithubSyncEnabledInput = false;
            ModGithubSyncAvailableInput = true;
            ModGithubRepoUrlInput = string.Empty;
            _isUpdatingGithubSyncState = false;
            return;
        }

        var meta = _modService.LoadModMetaByPath(value, SelectedModFolderName);
        ApplyMetaToUiInputs(meta, SelectedModFolderName);
        SelectedModUpdatedDisplay = Directory.Exists(value)
            ? Directory.GetLastWriteTime(value).ToString("yyyy-MM-dd HH:mm")
            : string.Empty;
        UpdateSelectedModGithubSyncState();
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

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (value == null)
        {
            return;
        }

        _localizationService.SetLanguageMode(value.Key);
        _modService.Settings.LanguageMode = value.Key;
        _modService.SaveSettings();
    }

    partial void OnModSearchTextChanged(string value)
    {
        RefreshToolModsFilter();
    }

    partial void OnSelectedTagFilterChanged(string value)
    {
        RefreshToolModsFilter();
    }

    partial void OnSelectedAuthorFilterChanged(string value)
    {
        RefreshToolModsFilter();
    }

    partial void OnModGithubSyncEnabledInputChanged(bool value)
    {
        if (_isUpdatingGithubSyncState || SelectedModForDetail == null)
        {
            return;
        }

        var key = $"{SelectedModForDetail.SourcePath}|{SelectedModForDetail.FolderName}";
        var record = _modService.Settings.GithubSyncMods
            .FirstOrDefault(x => string.Equals(x.ModKey, key, StringComparison.OrdinalIgnoreCase));
        if (record == null)
        {
            _githubSyncService.EnsureGithubSyncList(_modService.Settings, new[] { SelectedModForDetail });
            record = _modService.Settings.GithubSyncMods
                .FirstOrDefault(x => string.Equals(x.ModKey, key, StringComparison.OrdinalIgnoreCase));
        }

        if (record == null)
        {
            return;
        }

        record.Enabled = value;
        _modService.SaveSettings();
    }

    [RelayCommand]
    private void AddNewTag()
    {
        if (!TryAppendTag(NewTagInput))
        {
            return;
        }

        NewTagInput = string.Empty;
        SaveModMetaCore(showNoSelectionWarning: false, showErrorDialog: true);
    }

    partial void OnSelectedTagSuggestionChanged(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (TryAppendTag(value))
        {
            NewTagInput = string.Empty;
            SaveModMetaCore(showNoSelectionWarning: false, showErrorDialog: true);
        }

        SelectedTagSuggestion = null;
    }

    [RelayCommand]
    private void RemoveTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        var target = tag.Trim();
        var hit = ModTags.FirstOrDefault(x => string.Equals(x, target, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(hit))
        {
            return;
        }

        ModTags.Remove(hit);
        ModTagInput = JoinTags(ModTags);
        SaveModMetaCore(showNoSelectionWarning: false, showErrorDialog: true);
    }

    private void HandleLanguageChanged()
    {
        var selectedLanguageKey = SelectedLanguageOption?.Key ?? _localizationService.LanguageMode;
        var selectedSlotKey = SelectedSaveSlot?.Key;
        var selectedDirectionKey = SelectedSaveDirection?.Key;
        var selectedRestoreKey = SelectedRestoreTarget?.Key;

        LanguageOptions = new ObservableCollection<LanguageOption>(_localizationService.GetLanguageOptions());
        SelectedLanguageOption = LanguageOptions.FirstOrDefault(x => string.Equals(x.Key, selectedLanguageKey, StringComparison.OrdinalIgnoreCase))
                                 ?? LanguageOptions.FirstOrDefault();

        InitializeSaveOptions();

        if (!string.IsNullOrWhiteSpace(selectedSlotKey))
        {
            SelectedSaveSlot = SaveSlots.FirstOrDefault(x => string.Equals(x.Key, selectedSlotKey, StringComparison.OrdinalIgnoreCase)) ?? SelectedSaveSlot;
        }

        if (!string.IsNullOrWhiteSpace(selectedDirectionKey))
        {
            SelectedSaveDirection = SaveDirections.FirstOrDefault(x => string.Equals(x.Key, selectedDirectionKey, StringComparison.OrdinalIgnoreCase)) ?? SelectedSaveDirection;
        }

        if (!string.IsNullOrWhiteSpace(selectedRestoreKey))
        {
            SelectedRestoreTarget = RestoreTargets.FirstOrDefault(x => string.Equals(x.Key, selectedRestoreKey, StringComparison.OrdinalIgnoreCase)) ?? SelectedRestoreTarget;
        }

        UpdateTagFilters();
        RefreshToolModsFilter();
        RefreshSaveBackups();

        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            GamePathStatus = L("Path.Status.None");
        }
        else
        {
            GamePathStatus = _pathService.IsValidGamePath(SelectedPath)
                ? L("Path.Status.Valid")
                : L("Path.Status.Invalid");
        }
    }

    [RelayCommand]
    private void RefreshPaths()
    {
        var previousSelectedPath = SelectedPath;
        List<string> paths;
        try
        {
            paths = _pathService.DetectGamePaths();
        }
        catch (Exception ex)
        {
            StatusMessage = F("Msg.OperationFailed", ex.Message);
            return;
        }

        DetectedPaths.Clear();
        foreach (var path in paths)
        {
            DetectedPaths.Add(path);
        }

        TryAppendDetectedPath(previousSelectedPath);
        TryAppendDetectedPath(_modService.Settings.PreferredGamePath);

        if (!string.IsNullOrWhiteSpace(previousSelectedPath) && ContainsDetectedPath(previousSelectedPath))
        {
            SelectedPath = previousSelectedPath;
        }
        else if (!string.IsNullOrWhiteSpace(_modService.Settings.PreferredGamePath)
                 && ContainsDetectedPath(_modService.Settings.PreferredGamePath))
        {
            SelectedPath = _modService.Settings.PreferredGamePath;
        }
        else
        {
            SelectedPath = DetectedPaths.FirstOrDefault();
        }

        StatusMessage = string.Format(L("Status.PathRefresh"), paths.Count);
    }

    [RelayCommand]
    private void BrowseGamePath()
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = L("Dialog.SelectGameDirectory")
            };

            if (dialog.ShowDialog() != true)
            {
                return;
            }

            var selectedFolder = dialog.FolderName?.Trim();
            if (string.IsNullOrWhiteSpace(selectedFolder))
            {
                return;
            }

            if (!_pathService.IsValidGamePath(selectedFolder))
            {
                MessageBox.Show(L("Msg.InvalidGameDirectory"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!ContainsDetectedPath(selectedFolder))
            {
                DetectedPaths.Add(selectedFolder);
            }

            SelectedPath = selectedFolder;
        }
        catch (Exception ex)
        {
            MessageBox.Show(F("Msg.OperationFailed", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void OpenGameDirectory()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show(L("Msg.SelectGamePathFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show(L("Msg.SelectGamePathFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
            MessageBox.Show(L("Msg.SelectGamePathFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
            Title = L("Dialog.SelectModSourceDirectory")
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        if (_modService.AddCustomModSource(dialog.FolderName))
        {
            RefreshModSources();
            RefreshToolMods();
            StatusMessage = L("Status.ModSourceAdded");
            return;
        }

        MessageBox.Show(L("Msg.InvalidOrDuplicateSource"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void RemoveModSource()
    {
        if (SelectedToolMod == null)
        {
            MessageBox.Show(L("Msg.SelectModToRemoveSource"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var sourcePath = SelectedToolMod.SourcePath;
        var canRemove = _modService.Settings.CustomModSourceDirs.Any(x => string.Equals(x, sourcePath, StringComparison.OrdinalIgnoreCase));
        if (!canRemove)
        {
            MessageBox.Show(L("Msg.SystemSourceCannotRemove"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        _modService.RemoveCustomModSource(sourcePath);
        RefreshModSources();
        RefreshToolMods();
        StatusMessage = L("Status.ModSourceRemoved");
    }

    [RelayCommand]
    private void OpenSelectedModSource()
    {
        if (SelectedToolMod == null)
        {
            MessageBox.Show(L("Msg.SelectModAny"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
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

        _githubSyncService.EnsureGithubSyncList(_modService.Settings, ToolMods);
        _modService.SaveSettings();
        UpdateSelectedModGithubSyncState();

        UpdateTagFilters();
        RefreshToolModsFilter();
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
        SaveModMetaCore(showNoSelectionWarning: true, showErrorDialog: true);
    }

    [RelayCommand]
    private void SaveModMetaOnBlur()
    {
        SaveModMetaCore(showNoSelectionWarning: false, showErrorDialog: true);
    }

    private bool SaveModMetaCore(bool showNoSelectionWarning, bool showErrorDialog)
    {
        if (string.IsNullOrWhiteSpace(SelectedModFolderPath))
        {
            if (showNoSelectionWarning)
            {
                MessageBox.Show(L("Msg.SelectModFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            return false;
        }

        ModTagInput = JoinTags(ModTags);
        var detailValue = ModDetailInput?.Trim() ?? string.Empty;
        var descriptionValue = ModDescriptionInput?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(descriptionValue) && !string.IsNullOrWhiteSpace(detailValue))
        {
            descriptionValue = detailValue;
        }

        var success = _modService.SaveModMetaByPath(SelectedModFolderPath, SelectedModFolderName, new ModMetaInfo
        {
            Id = ModIdInput,
            Name = ModNameInput,
            Tag = ModTagInput,
            Version = ModVersionInput,
            Detail = detailValue,
            Author = ModAuthorInput,
            DownloadUrl = ModDownloadUrlInput,
            Remark = ModRemarkInput,
            AuthorUrl = ModAuthorUrlInput,
            DetailUrl = ModDetailUrlInput,
            SocialUrl = ModSocialUrlInput,
            Description = descriptionValue,
            Dependencies = ParseDependencies(ModDependenciesInput),
            AffectsGameplay = ModAffectsGameplayInput
        });

        if (!success)
        {
            if (showErrorDialog)
            {
                MessageBox.Show(L("Msg.ModMetaSaveFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return false;
        }

        ReloadModMetaAndViews();
        StatusMessage = L("Status.ModMetaSaved");
        return true;
    }

    private void ReloadModMetaAndViews()
    {
        var path = SelectedModFolderPath;
        var folderName = SelectedModFolderName;
        var meta = _modService.LoadModMetaByPath(path, folderName);

        ApplyMetaToUiInputs(meta, folderName);
        SelectedModUpdatedDisplay = Directory.Exists(path)
            ? Directory.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm")
            : string.Empty;

        var displayName = string.IsNullOrWhiteSpace(meta.Name) ? folderName : meta.Name;
        var relatedMods = ToolMods
            .Where(x => string.Equals(x.FolderPath, path, StringComparison.OrdinalIgnoreCase))
            .Concat(GameMods.Where(x => string.Equals(x.FolderPath, path, StringComparison.OrdinalIgnoreCase)))
            .Distinct()
            .ToList();

        foreach (var item in relatedMods)
        {
            item.DisplayName = displayName;
            item.Tag = ModTagInput;
            item.Version = meta.Version ?? string.Empty;
            item.Detail = meta.Detail ?? string.Empty;
            item.Author = meta.Author ?? string.Empty;
            item.DownloadUrl = meta.DownloadUrl ?? string.Empty;
            item.Remark = meta.Remark ?? string.Empty;
            item.AuthorUrl = meta.AuthorUrl ?? string.Empty;
            item.DetailUrl = meta.DetailUrl ?? string.Empty;
            item.SocialUrl = meta.SocialUrl ?? string.Empty;
            item.Description = meta.Description ?? string.Empty;
        }

        _githubSyncService.EnsureGithubSyncList(_modService.Settings, relatedMods);
        _modService.SaveSettings();
        UpdateSelectedModGithubSyncState();

        UpdateTagFilters();
        RefreshToolModsFilter();
        RefreshGameMods();
    }

    private void ApplyMetaToUiInputs(ModMetaInfo meta, string folderName)
    {
        ModIdInput = string.IsNullOrWhiteSpace(meta.Id) ? folderName : meta.Id;
        ModNameInput = string.IsNullOrWhiteSpace(meta.Name) ? folderName : meta.Name;
        SetModTags(ParseTags(meta.Tag));
        NewTagInput = string.Empty;
        ModVersionInput = meta.Version ?? string.Empty;
        ModDetailInput = meta.Detail ?? string.Empty;
        ModAuthorInput = meta.Author ?? string.Empty;
        ModDownloadUrlInput = meta.DownloadUrl ?? string.Empty;
        ModRemarkInput = meta.Remark ?? string.Empty;
        ModAuthorUrlInput = meta.AuthorUrl ?? string.Empty;
        ModDetailUrlInput = meta.DetailUrl ?? string.Empty;
        ModSocialUrlInput = meta.SocialUrl ?? string.Empty;
        ModDescriptionInput = string.IsNullOrWhiteSpace(meta.Description)
            ? (meta.Detail ?? string.Empty)
            : (meta.Description ?? string.Empty);
        ModDependenciesInput = JoinDependencies(meta.Dependencies);
        ModAffectsGameplayInput = meta.AffectsGameplay;
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
            MessageBox.Show(L("Msg.ModMetaResetFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ReloadModMetaAndViews();
        StatusMessage = L("Status.ModMetaReset");
    }

    [RelayCommand]
    private void ExtractJsonFromDll()
    {
        if (string.IsNullOrWhiteSpace(SelectedModFolderPath))
        {
            MessageBox.Show(L("Msg.SelectModFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var success = _modService.ExtractSameNameJsonFromDllAndUpdateMeta(
            SelectedModFolderPath,
            SelectedModFolderName,
            out var jsonPath,
            out var errorMessage);

        if (!success)
        {
            var detail = string.IsNullOrWhiteSpace(errorMessage) ? string.Empty : $"\n{errorMessage}";
            MessageBox.Show($"{L("Msg.ModJsonExtractFailed")}{detail}", L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        ReloadModMetaAndViews();
        StatusMessage = F("Status.ModJsonExtracted", jsonPath);
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
            MessageBox.Show(L("Msg.OpenUrlFailed"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show(L("Msg.ModEnableFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                StatusMessage = F("Status.ModEnabled", mod.DisplayName);
            }
            else
            {
                if (!_modService.MoveGameModToPendingByFolderName(SelectedPath, mod.FolderName))
                {
                    mod.IsEnabled = true;
                    MessageBox.Show(L("Msg.ModDisableFailed"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                StatusMessage = F("Status.ModDisabled", mod.DisplayName);
            }

            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();
        }
        catch (Exception ex)
        {
            mod.IsEnabled = !mod.IsEnabled;
            MessageBox.Show(F("Msg.OperationFailed", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
                StatusMessage = F("Status.ModMovedPending", mod.DisplayName);
                return;
            }

            mod.IsEnabled = true;
            MessageBox.Show(L("Msg.ModMoveOutFailed"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            mod.IsEnabled = true;
            MessageBox.Show(F("Msg.ModMoveOutException", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
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
        StatusMessage = L("Status.EnableAll");
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
        StatusMessage = L("Status.DisableAll");
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
            MessageBox.Show(L("Msg.NoSteamId"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedSaveDirection == null || SelectedSaveSlot == null)
        {
            MessageBox.Show(L("Msg.SelectDirectionAndSlot"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var direction = SelectedSaveDirection.Key == "mod_to_normal"
            ? SaveCopyDirection.ModdedToNormal
            : SaveCopyDirection.NormalToModded;

        var directionText = direction == SaveCopyDirection.NormalToModded
            ? L("Save.Direction.Short.NormalToMod")
            : L("Save.Direction.Short.ModToNormal");

        var confirmResult = MessageBox.Show(
            F("Msg.CopySaveConfirm", SelectedSteamId, directionText, SelectedSaveSlot.Label),
            L("Dialog.Confirm.Copy"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirmResult != MessageBoxResult.Yes)
        {
            return;
        }

        StatusMessage = L("Status.CopySaveRunning");
        SaveCopyResult result;
        try
        {
            result = await Task.Run(() => _saveService.CopyWithinSteamId(SelectedSteamId, direction, SelectedSaveSlot.Key));
        }
        catch (Exception ex)
        {
            MessageBox.Show(F("Msg.CopySaveException", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.CopySaveFailed");
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show(L("Msg.CopySaveSourceMissing"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.CopySaveFailed");
            return;
        }

        StatusMessage = F("Status.CopySaveSuccess", result.CopiedCount, directionText);
        MessageBox.Show(F("Msg.CopySaveDone", result.BackupPath), L("Dialog.Title.Success"), MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshSaveBackups();
    }

    [RelayCommand]
    private async Task RestoreSaveBackup()
    {
        if (SelectedSaveBackup == null)
        {
            MessageBox.Show(L("Msg.SelectRestoreBackup"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var backup = SelectedSaveBackup;
        var confirm = MessageBox.Show(
            F("Msg.RestoreConfirm", backup.BackupTime, backup.SteamId, backup.BackupPath, backup.BackupKindDisplay, SelectedRestoreTarget?.Label ?? L("Save.Restore.Auto")),
            L("Dialog.Confirm.Restore"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes)
        {
            return;
        }

        StatusMessage = L("Status.RestoreSaveRunning");
        SaveCopyResult result;
        try
        {
            var targetMode = SelectedRestoreTarget?.Key ?? "auto";
            result = await Task.Run(() => _saveService.RestoreFromBackup(backup, targetMode));
        }
        catch (Exception ex)
        {
            MessageBox.Show(F("Msg.RestoreException", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.RestoreSaveFailed");
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show(L("Msg.RestoreInvalidBackup"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.RestoreSaveFailed");
            return;
        }

        if (!string.Equals(SelectedSteamId, backup.SteamId, StringComparison.OrdinalIgnoreCase))
        {
            SelectedSteamId = backup.SteamId;
        }

        StatusMessage = L("Status.RestoreSaveSuccess");
        var rescueText = string.IsNullOrWhiteSpace(result.BackupPath) ? L("Msg.RestoreRescueMissing") : result.BackupPath;
        MessageBox.Show(F("Msg.RestoreDone", rescueText), L("Dialog.Title.Success"), MessageBoxButton.OK, MessageBoxImage.Information);
        RefreshSaveBackups();
    }

    [RelayCommand]
    private async Task CreateManualBackup()
    {
        if (string.IsNullOrWhiteSpace(SelectedSteamId))
        {
            MessageBox.Show(L("Msg.SelectSteamIdFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        StatusMessage = L("Status.ManualBackupRunning");
        SaveCopyResult result;
        try
        {
            result = await Task.Run(() => _saveService.CreateManualBackup(SelectedSteamId, ManualBackupName));
        }
        catch (Exception ex)
        {
            MessageBox.Show(F("Msg.ManualBackupException", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.ManualBackupFailed");
            return;
        }

        if (!result.Success)
        {
            MessageBox.Show(L("Msg.ManualBackupIdMissing"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.ManualBackupFailed");
            return;
        }

        StatusMessage = L("Status.ManualBackupDone");
        ManualBackupName = string.Empty;
        RefreshSaveBackups();
        MessageBox.Show(F("Msg.ManualBackupDone", result.BackupPath), L("Dialog.Title.Success"), MessageBoxButton.OK, MessageBoxImage.Information);
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
            MessageBox.Show(L("Msg.SelectGamePathFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, false))
        {
            StatusMessage = L("Status.GameLaunched");
            return;
        }

        MessageBox.Show(L("Msg.GameLaunchFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameNoMods()
    {
        if (string.IsNullOrWhiteSpace(SelectedPath))
        {
            MessageBox.Show(L("Msg.SelectGamePathFirst"), L("Dialog.Title.Tip"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_launchService.LaunchGame(SelectedPath, true))
        {
            StatusMessage = L("Status.GameLaunchedNoMods");
            return;
        }

        MessageBox.Show(L("Msg.GameLaunchFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameViaSteam()
    {
        if (_launchService.LaunchGameViaSteam())
        {
            StatusMessage = L("Status.GameLaunchedSteam");
            return;
        }

        MessageBox.Show(L("Msg.SteamLaunchFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private void LaunchGameViaSteamNoMods()
    {
        if (_launchService.LaunchGameViaSteamNoMods())
        {
            StatusMessage = L("Status.GameLaunchedSteamNoMods");
            return;
        }

        MessageBox.Show(L("Msg.SteamLaunchFailed"), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    [RelayCommand]
    private async Task SyncGithubMods()
    {
        if (IsGithubSyncRunning)
        {
            return;
        }

        IsGithubSyncRunning = true;
        GithubSyncProgressText = "0/0";
        try
        {
            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();

            var allMods = ToolMods.ToList();
            _githubSyncService.EnsureGithubSyncList(_modService.Settings, allMods);
            _modService.SaveSettings();

            var duplicateEnabledCount = _modService.Settings.GithubSyncMods
                .Where(x => x.Enabled && x.Available)
                .Select(x => NormalizeGithubRepo(x.RepoUrl))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .GroupBy(x => x, StringComparer.OrdinalIgnoreCase)
                .Count(g => g.Count() > 1);
            if (duplicateEnabledCount > 0)
            {
                var confirm = MessageBox.Show(
                    F("Msg.GithubDuplicateConfirm", duplicateEnabledCount),
                    L("Dialog.Title.Confirm"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes)
                {
                    StatusMessage = L("Status.GithubSyncCancelled");
                    return;
                }
            }

            var progress = new Progress<GithubSyncProgress>(p =>
            {
                GithubSyncProgressText = $"{p.Current}/{p.Total}";
                StatusMessage = p.Message;
            });

            var summary = await _githubSyncService.SyncAsync(_modService.Settings, allMods, progress);
            _modService.SaveSettings();

            RefreshModSources();
            RefreshToolMods();
            RefreshGameMods();
            UpdateSelectedModGithubSyncState();

            GithubSyncProgressText = $"{summary.Total}/{summary.Total}";
            var msg = F("Msg.GithubSyncSummary", summary.Updated, summary.Invalid, summary.Latest, summary.DuplicateRepoHints, summary.LogFilePath);
            MessageBox.Show(msg, L("Dialog.Title.Success"), MessageBoxButton.OK, MessageBoxImage.Information);
            StatusMessage = L("Status.GithubSyncDone");
        }
        catch (Exception ex)
        {
            MessageBox.Show(F("Msg.OperationFailed", ex.Message), L("Dialog.Title.Error"), MessageBoxButton.OK, MessageBoxImage.Error);
            StatusMessage = L("Status.GithubSyncFailed");
        }
        finally
        {
            IsGithubSyncRunning = false;
        }
    }

    private bool FilterToolMod(object obj)
    {
        if (obj is not ModInfo mod)
        {
            return false;
        }

        var keyword = ModSearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var hit = ContainsText(mod.DisplayName, keyword)
                      || ContainsText(mod.FolderName, keyword)
                      || ContainsText(mod.Author, keyword)
                      || ContainsText(mod.Detail, keyword)
                      || ContainsText(mod.Remark, keyword)
                      || ContainsText(mod.Tag, keyword);
            if (!hit)
            {
                return false;
            }
        }

        if (!string.IsNullOrWhiteSpace(SelectedTagFilter)
            && !string.Equals(SelectedTagFilter, L("Tag.All"), StringComparison.OrdinalIgnoreCase)
            && !ParseTags(mod.Tag).Contains(SelectedTagFilter, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(SelectedAuthorFilter)
            && !string.Equals(SelectedAuthorFilter, L("Author.All"), StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mod.AuthorDisplay, SelectedAuthorFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static bool ContainsText(string? source, string keyword)
    {
        return !string.IsNullOrWhiteSpace(source)
               && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshToolModsFilter()
    {
        FilteredToolModsView.Refresh();
    }

    private void UpdateTagFilters()
    {
        var tags = ToolMods
            .SelectMany(x => ParseTags(x.Tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        AvailableTags.Clear();
        foreach (var tag in tags)
        {
            AvailableTags.Add(tag);
        }

        var allTagLabel = L("Tag.All");
        var nextFilters = new List<string> { allTagLabel };
        nextFilters.AddRange(tags);

        TagFilters.Clear();
        foreach (var tag in nextFilters)
        {
            TagFilters.Add(tag);
        }

        if (!TagFilters.Contains(SelectedTagFilter))
        {
            SelectedTagFilter = allTagLabel;
        }

        var authors = ToolMods
            .Select(x => x.AuthorDisplay)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var allAuthorLabel = L("Author.All");
        var nextAuthorFilters = new List<string> { allAuthorLabel };
        nextAuthorFilters.AddRange(authors);

        AuthorFilters.Clear();
        foreach (var author in nextAuthorFilters)
        {
            AuthorFilters.Add(author);
        }

        if (!AuthorFilters.Contains(SelectedAuthorFilter))
        {
            SelectedAuthorFilter = allAuthorLabel;
        }
    }

    private static List<string> ParseTags(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return TagSplitRegex
            .Split(raw)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string JoinTags(IEnumerable<string> tags)
    {
        return string.Join(", ", tags
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static List<string> ParseDependencies(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return DependencySplitRegex
            .Split(raw)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string JoinDependencies(IEnumerable<string>? dependencies)
    {
        if (dependencies == null)
        {
            return string.Empty;
        }

        return string.Join(", ", dependencies
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private void SetModTags(IEnumerable<string> tags)
    {
        ModTags.Clear();
        foreach (var tag in tags)
        {
            ModTags.Add(tag);
        }

        ModTagInput = JoinTags(ModTags);
    }

    private bool TryAppendTag(string? tagText)
    {
        var normalized = tagText?.Trim() ?? string.Empty;
        if (normalized.Length == 0)
        {
            return false;
        }

        if (ModTags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        ModTags.Add(normalized);
        if (!AvailableTags.Contains(normalized, StringComparer.OrdinalIgnoreCase))
        {
            AvailableTags.Add(normalized);
        }

        ModTagInput = JoinTags(ModTags);
        return true;
    }

    private void UpdateSelectedModGithubSyncState()
    {
        _isUpdatingGithubSyncState = true;
        try
        {
            if (SelectedModForDetail == null)
            {
                ModGithubSyncEnabledInput = false;
                ModGithubSyncAvailableInput = true;
                ModGithubRepoUrlInput = string.Empty;
                return;
            }

            var key = $"{SelectedModForDetail.SourcePath}|{SelectedModForDetail.FolderName}";
            var record = _modService.Settings.GithubSyncMods
                .FirstOrDefault(x => string.Equals(x.ModKey, key, StringComparison.OrdinalIgnoreCase));
            if (record == null)
            {
                _githubSyncService.EnsureGithubSyncList(_modService.Settings, new[] { SelectedModForDetail });
                _modService.SaveSettings();
                record = _modService.Settings.GithubSyncMods
                    .FirstOrDefault(x => string.Equals(x.ModKey, key, StringComparison.OrdinalIgnoreCase));
            }

            if (record == null)
            {
                ModGithubSyncEnabledInput = false;
                ModGithubSyncAvailableInput = true;
                ModGithubRepoUrlInput = string.Empty;
                return;
            }

            ModGithubSyncEnabledInput = record.Enabled;
            ModGithubSyncAvailableInput = record.Available;
            ModGithubRepoUrlInput = record.RepoUrl;
        }
        finally
        {
            _isUpdatingGithubSyncState = false;
        }
    }

    private static string NormalizeGithubRepo(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return string.Empty;
        }

        return $"https://github.com/{segments[0]}/{segments[1]}";
    }

    private string L(string key)
    {
        return _localizationService[key];
    }

    private string F(string key, params object[] args)
    {
        return string.Format(L(key), args);
    }

    private void TryAppendDetectedPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!ContainsDetectedPath(path))
        {
            DetectedPaths.Add(path);
        }
    }

    private bool ContainsDetectedPath(string path)
    {
        return DetectedPaths.Any(x => string.Equals(x, path, StringComparison.OrdinalIgnoreCase));
    }
}
