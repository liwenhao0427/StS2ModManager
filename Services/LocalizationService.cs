using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public partial class LocalizationService : ObservableObject
{
    public const string LanguageSystem = "system";
    public const string LanguageZhCn = "zh-CN";
    public const string LanguageEnUs = "en-US";

    private readonly Dictionary<string, Dictionary<string, string>> _texts = new(StringComparer.OrdinalIgnoreCase)
    {
        [LanguageZhCn] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["App.Title"] = "杀戮尖塔2 Mod管理器",
            ["App.Subtitle"] = "支持多来源Mod管理、存档互拷与快捷恢复",
            ["Language.Label"] = "语言:",
            ["Language.Option.System"] = "跟随系统",
            ["Language.Option.ZhCn"] = "简体中文",
            ["Language.Option.EnUs"] = "英语",
            ["Common.Refresh"] = "刷新",
            ["Common.Browse"] = "浏览...",
            ["Common.Search"] = "搜索:",
            ["Common.Tag"] = "标签:",
            ["Common.Author"] = "作者:",
            ["Common.Open"] = "打开",
            ["Common.Go"] = "跳转",
            ["Common.Save"] = "保存",
            ["Common.Reset"] = "重置",
            ["Common.Confirm"] = "确定",
            ["Common.Cancel"] = "取消",
            ["Common.SelectAll"] = "全选",
            ["Common.UnselectAll"] = "全不选",
            ["Common.DownloadPage"] = "下载页",
            ["Common.AuthorPage"] = "作者页",
            ["Path.Label"] = "游戏路径:",
            ["Path.OpenGameDir"] = "📂 游戏目录",
            ["Path.OpenGameMods"] = "📂 游戏Mods",
            ["Path.OpenPendingMods"] = "📂 待生效Mods",
            ["Path.OpenToolMods"] = "📂 工具Mods",
            ["Path.Status.None"] = "未选择",
            ["Path.Status.Valid"] = "✓ 已找到",
            ["Path.Status.Invalid"] = "✗ 无效路径",
            ["Launch.Title"] = "启动游戏",
            ["Launch.Steam"] = "Steam启动",
            ["Launch.SteamNoMods"] = "Steam启动(无Mod)",
            ["Launch.Direct"] = "直接启动",
            ["Launch.DirectNoMods"] = "直接启动(无Mod)",
            ["ModList.Title"] = "Mod列表（勾选后复制到游戏Mods）",
            ["Source.Add"] = "添加来源",
            ["Source.Remove"] = "移除来源",
            ["Source.Open"] = "打开来源",
            ["Mods.EnableAll"] = "全选生效",
            ["Mods.DisableAll"] = "全部取消",
            ["Mods.SyncGithub"] = "同步GitHub Mod",
            ["ModDetail.Title"] = "Mod详情（统一编辑）",
            ["ModDetail.ExtractJsonFromDll"] = "从Dll提取Json覆盖",
            ["Field.Name"] = "名称:",
            ["Field.Tag"] = "标签:",
            ["Field.TagHint"] = "输入标签后按回车新增，可点击 x 删除",
            ["Field.Detail"] = "详情:",
            ["Field.Author"] = "作者:",
            ["Field.DownloadUrl"] = "下载链接:",
            ["Field.Remark"] = "备注:",
            ["Field.DetailUrl"] = "详情链接:",
            ["Field.AuthorUrl"] = "作者链接:",
            ["Field.Version"] = "版本:",
            ["Field.ModId"] = "标识ID:",
            ["Field.Dependencies"] = "依赖Mod:",
            ["Field.DependenciesHint"] = "多个依赖可用逗号或换行分隔，例如: BaseLib, SharedApi",
            ["Field.AffectsGameplay"] = "影响游戏:",
            ["Field.AffectsGameplay.Checkbox"] = "此 Mod 会影响游戏玩法平衡",
            ["Field.AffectsGameplayHint"] = "多人游戏时，勾选表示会校验此 Mod；不勾选表示可跳过检测，允许其他玩家不安装此 Mod 也能联机。",
            ["Field.Updated"] = "更新时间:",
            ["Field.Description"] = "扩展详情:",
            ["Field.GithubRepo"] = "GitHub仓库:",
            ["Field.GithubSyncEnabled"] = "启用GitHub同步更新",
            ["Field.GithubAvailable"] = "链接可用",
            ["Save.Title"] = "存档管理",
            ["Save.OpenSavesDir"] = "打开存档目录",
            ["Save.OpenBackupDir"] = "打开备份目录",
            ["Save.RefreshSteamId"] = "刷新SteamID",
            ["Save.RefreshBackups"] = "刷新备份",
            ["Save.SteamId"] = "Steam ID:",
            ["Save.CopyDirection"] = "复制方向:",
            ["Save.Slot"] = "栏位:",
            ["Save.Copy"] = "复制存档",
            ["Save.QuickRestore"] = "快捷恢复:",
            ["Save.RestoreTo"] = "恢复到:",
            ["Save.RestoreSelected"] = "恢复选中备份",
            ["Save.ManualName"] = "手动备份名:",
            ["Save.ManualBackupNow"] = "立即手动备份",
            ["Save.ManualHint"] = "不填则默认命名为“手动备份”",
            ["Dialog.Title.Tip"] = "提示",
            ["Dialog.Title.Error"] = "错误",
            ["Dialog.Title.Success"] = "成功",
            ["Dialog.Title.Confirm"] = "确认",
            ["Tag.All"] = "全部标签",
            ["Author.All"] = "全部作者",
            ["Save.Direction.NormalToMod"] = "非Mod存档 -> Mod存档",
            ["Save.Direction.ModToNormal"] = "Mod存档 -> 非Mod存档",
            ["Save.Slot1"] = "栏位1",
            ["Save.Slot2"] = "栏位2",
            ["Save.Slot3"] = "栏位3",
            ["Save.SlotAll"] = "全部栏位",
            ["Save.Restore.Auto"] = "原路径（默认）",
            ["Save.Restore.Normal"] = "恢复到非Mod存档",
            ["Save.Restore.Modded"] = "恢复到Mod存档",
            ["Save.Direction.Short.NormalToMod"] = "非Mod -> Mod",
            ["Save.Direction.Short.ModToNormal"] = "Mod -> 非Mod",
            ["Save.BackupName.Auto"] = "自动备份",
            ["Save.BackupName.Manual"] = "手动备份",
            ["Save.BackupName.RestoreBefore"] = "恢复前备份",
            ["Save.BackupKind.Full"] = "整档",
            ["Save.BackupKind.Slot"] = "栏位{0} {1}",
            ["Save.BackupKind.Modded"] = "Mod",
            ["Save.BackupKind.Normal"] = "非Mod",
            ["Save.Backup.Display"] = "{0:yyyy-MM-dd HH:mm:ss} | {1} | {2}",
            ["Status.PathFound"] = "已找到 {0} 个游戏路径",
            ["Status.PathNotFound"] = "未找到游戏，请手动选择",
            ["Status.PathRefresh"] = "已刷新，找到 {0} 个路径",
            ["Status.ModSourceAdded"] = "已添加自定义Mod目录",
            ["Status.ModSourceRemoved"] = "已移除来源目录",
            ["Status.ModMetaSaved"] = "Mod信息已保存并刷新",
            ["Status.ModMetaReset"] = "Mod信息已重置",
            ["Status.ModJsonExtracted"] = "已提取并覆盖同名json，且同步更新自定义配置: {0}",
            ["Status.ModEnabled"] = "已生效 {0}",
            ["Status.ModDisabled"] = "已取消生效 {0}",
            ["Status.ModMovedPending"] = "已移出 {0} 到待生效目录",
            ["Status.EnableAll"] = "已批量生效所有可用Mod",
            ["Status.DisableAll"] = "已批量取消所有生效Mod",
            ["Status.CopySaveRunning"] = "正在复制存档...",
            ["Status.CopySaveFailed"] = "存档复制失败",
            ["Status.CopySaveSuccess"] = "已复制 {0} 个栏位（{1}）",
            ["Status.RestoreSaveRunning"] = "正在恢复存档...",
            ["Status.RestoreSaveFailed"] = "恢复存档失败",
            ["Status.RestoreSaveSuccess"] = "存档恢复成功",
            ["Status.ManualBackupRunning"] = "正在执行手动备份...",
            ["Status.ManualBackupFailed"] = "手动备份失败",
            ["Status.ManualBackupDone"] = "手动备份完成",
            ["Status.GameLaunched"] = "游戏已启动",
            ["Status.GameLaunchedNoMods"] = "游戏已启动（无Mod）",
            ["Status.GameLaunchedSteam"] = "已通过Steam启动游戏",
            ["Status.GameLaunchedSteamNoMods"] = "已通过Steam启动游戏（无Mod）",
            ["Status.GithubSyncDone"] = "GitHub同步完成",
            ["Status.GithubSyncFailed"] = "GitHub同步失败",
            ["Status.GithubSyncCancelled"] = "已取消GitHub同步",
            ["Status.GithubSyncPreparing"] = "正在准备同步任务...",
            ["Status.GithubSyncRunning"] = "正在后台同步，请稍候...",
            ["Msg.SelectModFirst"] = "请先选中一个Mod",
            ["Msg.SelectGamePathFirst"] = "请先选择游戏路径",
            ["Msg.SelectSteamIdFirst"] = "请先选择Steam ID",
            ["Msg.InvalidGameDirectory"] = "选择的目录不是有效的游戏目录",
            ["Msg.InvalidOrDuplicateSource"] = "目录无效或已存在",
            ["Msg.SelectModToRemoveSource"] = "请先选中某个Mod后再移除其来源目录",
            ["Msg.SystemSourceCannotRemove"] = "当前来源为系统目录，不能移除",
            ["Msg.SelectModAny"] = "请先选中某个Mod",
            ["Msg.ModMetaSaveFailed"] = "写入Mod信息失败，请确认该Mod目录可写",
            ["Msg.ModMetaResetFailed"] = "清空Mod信息失败，请确认该Mod目录可写",
            ["Msg.ModJsonExtractFailed"] = "从dll提取json失败",
            ["Msg.OpenUrlFailed"] = "无法打开链接，请检查地址格式",
            ["Msg.ModEnableFailed"] = "生效失败，请检查Mod目录",
            ["Msg.ModDisableFailed"] = "取消生效失败，未找到游戏内对应Mod",
            ["Msg.OperationFailed"] = "操作失败: {0}",
            ["Msg.ModMoveOutFailed"] = "移出失败，未找到对应Mod目录",
            ["Msg.ModMoveOutException"] = "移出失败: {0}",
            ["Msg.NoSteamId"] = "未找到可用Steam ID",
            ["Msg.SelectDirectionAndSlot"] = "请选择复制方向和栏位",
            ["Msg.CopySaveConfirm"] = "⚠️ 确认复制存档？\n\nSteam ID: {0}\n方向: {1}\n栏位: {2}\n\n会先把目标栏位备份到存档目录同级的 Backup/Saves，再执行复制。\n是否继续？",
            ["Msg.CopySaveException"] = "存档复制失败: {0}",
            ["Msg.CopySaveSourceMissing"] = "存档复制失败，请确认源栏位存档是否存在",
            ["Msg.CopySaveDone"] = "存档复制成功！\n备份位置: {0}",
            ["Msg.SelectRestoreBackup"] = "请先选择要恢复的备份存档",
            ["Msg.RestoreConfirm"] = "确认恢复该备份？\n\n时间: {0:yyyy-MM-dd HH:mm:ss}\nSteam ID: {1}\n备份路径: {2}\n备份类型: {3}\n恢复目标: {4}\n\n当前存档会先自动备份后再恢复。",
            ["Msg.RestoreException"] = "恢复存档失败: {0}",
            ["Msg.RestoreInvalidBackup"] = "恢复存档失败，备份路径可能已失效",
            ["Msg.RestoreDone"] = "恢复完成。\n恢复前备份: {0}",
            ["Msg.RestoreRescueMissing"] = "未生成恢复前备份",
            ["Msg.ManualBackupException"] = "手动备份失败: {0}",
            ["Msg.ManualBackupIdMissing"] = "手动备份失败，请确认Steam ID目录存在",
            ["Msg.ManualBackupDone"] = "手动备份成功！\n备份位置: {0}",
            ["Msg.GameLaunchFailed"] = "启动游戏失败，请检查游戏路径",
            ["Msg.SteamLaunchFailed"] = "通过Steam启动失败，请确认Steam已安装并已登录",
            ["Msg.GithubSyncSummary"] = "同步完成\n有更新: {0}\n地址无效: {1}\n已是最新: {2}\n重复仓库提示: {3}\n日志: {4}",
            ["Msg.GithubDuplicateConfirm"] = "检测到 {0} 组启用中的重复仓库映射。\n同步将仅更新其中一个，是否继续？",
            ["Msg.GithubNoCandidate"] = "没有可更新的目录可供选择。",
            ["Msg.GithubSourceRequired"] = "请至少勾选一个要同步的目录。",
            ["Dialog.GithubSync.Title"] = "GitHub同步进度",
            ["Dialog.GithubSync.Header"] = "正在同步 GitHub Mod",
            ["Dialog.GithubSync.SourceSelectTitle"] = "选择要同步的目录",
            ["Dialog.GithubSync.SourceSelectHeader"] = "请选择要同步的目录",
            ["Dialog.GithubSync.SourceSelectHint"] = "仅会更新你勾选目录下可同步的 Mod。",
            ["Dialog.Confirm.Copy"] = "确认复制",
            ["Dialog.Confirm.Restore"] = "恢复存档确认",
            ["Dialog.SelectGameDirectory"] = "选择游戏目录",
            ["Dialog.SelectModSourceDirectory"] = "选择Mod来源目录"
        },
        [LanguageEnUs] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["App.Title"] = "StS2 Mod Manager",
            ["App.Subtitle"] = "Manage mods, copy saves, and restore backups quickly",
            ["Language.Label"] = "Language:",
            ["Language.Option.System"] = "Follow System",
            ["Language.Option.ZhCn"] = "Simplified Chinese",
            ["Language.Option.EnUs"] = "English",
            ["Common.Refresh"] = "Refresh",
            ["Common.Browse"] = "Browse...",
            ["Common.Search"] = "Search:",
            ["Common.Tag"] = "Tag:",
            ["Common.Author"] = "Author:",
            ["Common.Open"] = "Open",
            ["Common.Go"] = "Go",
            ["Common.Save"] = "Save",
            ["Common.Reset"] = "Reset",
            ["Common.Confirm"] = "Confirm",
            ["Common.Cancel"] = "Cancel",
            ["Common.SelectAll"] = "Select All",
            ["Common.UnselectAll"] = "Unselect All",
            ["Common.DownloadPage"] = "Download Page",
            ["Common.AuthorPage"] = "Author Page",
            ["Path.Label"] = "Game Path:",
            ["Path.OpenGameDir"] = "📂 Game Folder",
            ["Path.OpenGameMods"] = "📂 Game Mods",
            ["Path.OpenPendingMods"] = "📂 Pending Mods",
            ["Path.OpenToolMods"] = "📂 Tool Mods",
            ["Path.Status.None"] = "Not selected",
            ["Path.Status.Valid"] = "✓ Found",
            ["Path.Status.Invalid"] = "✗ Invalid path",
            ["Launch.Title"] = "Launch Game",
            ["Launch.Steam"] = "Launch via Steam",
            ["Launch.SteamNoMods"] = "Steam Launch (No Mods)",
            ["Launch.Direct"] = "Direct Launch",
            ["Launch.DirectNoMods"] = "Direct Launch (No Mods)",
            ["ModList.Title"] = "Mod List (checked mods copy to game mods)",
            ["Source.Add"] = "Add Source",
            ["Source.Remove"] = "Remove Source",
            ["Source.Open"] = "Open Source",
            ["Mods.EnableAll"] = "Enable All",
            ["Mods.DisableAll"] = "Disable All",
            ["Mods.SyncGithub"] = "Sync GitHub Mods",
            ["ModDetail.Title"] = "Mod Details (Unified Edit)",
            ["ModDetail.ExtractJsonFromDll"] = "Extract Json From Dll",
            ["Field.Name"] = "Name:",
            ["Field.Tag"] = "Tag:",
            ["Field.TagHint"] = "Type a tag and press Enter to add, click x to remove",
            ["Field.Detail"] = "Detail:",
            ["Field.Author"] = "Author:",
            ["Field.DownloadUrl"] = "Download URL:",
            ["Field.Remark"] = "Remark:",
            ["Field.DetailUrl"] = "Detail URL:",
            ["Field.AuthorUrl"] = "Author URL:",
            ["Field.Version"] = "Version:",
            ["Field.ModId"] = "Mod ID:",
            ["Field.Dependencies"] = "Dependencies:",
            ["Field.DependenciesHint"] = "Separate multiple dependencies with commas or new lines, e.g. BaseLib, SharedApi",
            ["Field.AffectsGameplay"] = "Affects Gameplay:",
            ["Field.AffectsGameplay.Checkbox"] = "This mod changes gameplay balance",
            ["Field.AffectsGameplayHint"] = "In multiplayer, checked means this mod is required for validation; unchecked allows players to join without installing this mod.",
            ["Field.Updated"] = "Updated:",
            ["Field.Description"] = "Description:",
            ["Field.GithubRepo"] = "GitHub Repo:",
            ["Field.GithubSyncEnabled"] = "Enable GitHub sync update",
            ["Field.GithubAvailable"] = "Link Available",
            ["Save.Title"] = "Save Management",
            ["Save.OpenSavesDir"] = "Open Saves Folder",
            ["Save.OpenBackupDir"] = "Open Backup Folder",
            ["Save.RefreshSteamId"] = "Refresh Steam IDs",
            ["Save.RefreshBackups"] = "Refresh Backups",
            ["Save.SteamId"] = "Steam ID:",
            ["Save.CopyDirection"] = "Copy Direction:",
            ["Save.Slot"] = "Slot:",
            ["Save.Copy"] = "Copy Saves",
            ["Save.QuickRestore"] = "Quick Restore:",
            ["Save.RestoreTo"] = "Restore To:",
            ["Save.RestoreSelected"] = "Restore Selected Backup",
            ["Save.ManualName"] = "Manual Backup Name:",
            ["Save.ManualBackupNow"] = "Create Manual Backup",
            ["Save.ManualHint"] = "Leave empty to use default name \"Manual Backup\"",
            ["Dialog.Title.Tip"] = "Tip",
            ["Dialog.Title.Error"] = "Error",
            ["Dialog.Title.Success"] = "Success",
            ["Dialog.Title.Confirm"] = "Confirm",
            ["Tag.All"] = "All Tags",
            ["Author.All"] = "All Authors",
            ["Save.Direction.NormalToMod"] = "Normal save -> Modded save",
            ["Save.Direction.ModToNormal"] = "Modded save -> Normal save",
            ["Save.Slot1"] = "Slot 1",
            ["Save.Slot2"] = "Slot 2",
            ["Save.Slot3"] = "Slot 3",
            ["Save.SlotAll"] = "All slots",
            ["Save.Restore.Auto"] = "Original path (default)",
            ["Save.Restore.Normal"] = "Restore to normal saves",
            ["Save.Restore.Modded"] = "Restore to modded saves",
            ["Save.Direction.Short.NormalToMod"] = "Normal -> Modded",
            ["Save.Direction.Short.ModToNormal"] = "Modded -> Normal",
            ["Save.BackupName.Auto"] = "Auto Backup",
            ["Save.BackupName.Manual"] = "Manual Backup",
            ["Save.BackupName.RestoreBefore"] = "Before Restore Backup",
            ["Save.BackupKind.Full"] = "Full",
            ["Save.BackupKind.Slot"] = "Slot {0} {1}",
            ["Save.BackupKind.Modded"] = "Modded",
            ["Save.BackupKind.Normal"] = "Normal",
            ["Save.Backup.Display"] = "{0:yyyy-MM-dd HH:mm:ss} | {1} | {2}",
            ["Status.PathFound"] = "Found {0} game paths",
            ["Status.PathNotFound"] = "Game not found, please select manually",
            ["Status.PathRefresh"] = "Refreshed, found {0} paths",
            ["Status.ModSourceAdded"] = "Custom mod source added",
            ["Status.ModSourceRemoved"] = "Mod source removed",
            ["Status.ModMetaSaved"] = "Mod metadata saved and reloaded",
            ["Status.ModMetaReset"] = "Mod metadata reset",
            ["Status.ModJsonExtracted"] = "Extracted and overwritten same-name json, and synced custom metadata: {0}",
            ["Status.ModEnabled"] = "Enabled {0}",
            ["Status.ModDisabled"] = "Disabled {0}",
            ["Status.ModMovedPending"] = "Moved {0} to pending mods",
            ["Status.EnableAll"] = "Enabled all available mods",
            ["Status.DisableAll"] = "Disabled all active mods",
            ["Status.CopySaveRunning"] = "Copying saves...",
            ["Status.CopySaveFailed"] = "Save copy failed",
            ["Status.CopySaveSuccess"] = "Copied {0} slot(s) ({1})",
            ["Status.RestoreSaveRunning"] = "Restoring saves...",
            ["Status.RestoreSaveFailed"] = "Save restore failed",
            ["Status.RestoreSaveSuccess"] = "Save restore completed",
            ["Status.ManualBackupRunning"] = "Creating manual backup...",
            ["Status.ManualBackupFailed"] = "Manual backup failed",
            ["Status.ManualBackupDone"] = "Manual backup completed",
            ["Status.GameLaunched"] = "Game started",
            ["Status.GameLaunchedNoMods"] = "Game started (no mods)",
            ["Status.GameLaunchedSteam"] = "Game launched via Steam",
            ["Status.GameLaunchedSteamNoMods"] = "Game launched via Steam (no mods)",
            ["Status.GithubSyncDone"] = "GitHub sync completed",
            ["Status.GithubSyncFailed"] = "GitHub sync failed",
            ["Status.GithubSyncCancelled"] = "GitHub sync cancelled",
            ["Status.GithubSyncPreparing"] = "Preparing sync task...",
            ["Status.GithubSyncRunning"] = "Syncing in background, please wait...",
            ["Msg.SelectModFirst"] = "Please select a mod first",
            ["Msg.SelectGamePathFirst"] = "Please select a game path first",
            ["Msg.SelectSteamIdFirst"] = "Please select a Steam ID first",
            ["Msg.InvalidGameDirectory"] = "The selected folder is not a valid game directory",
            ["Msg.InvalidOrDuplicateSource"] = "Directory is invalid or already exists",
            ["Msg.SelectModToRemoveSource"] = "Select a mod before removing its source",
            ["Msg.SystemSourceCannotRemove"] = "Current source is system-managed and cannot be removed",
            ["Msg.SelectModAny"] = "Please select a mod first",
            ["Msg.ModMetaSaveFailed"] = "Failed to save mod metadata. Please check write permissions",
            ["Msg.ModMetaResetFailed"] = "Failed to reset mod metadata. Please check write permissions",
            ["Msg.ModJsonExtractFailed"] = "Failed to extract json from dll",
            ["Msg.OpenUrlFailed"] = "Unable to open URL. Please check the format",
            ["Msg.ModEnableFailed"] = "Enable failed. Please check mod directory",
            ["Msg.ModDisableFailed"] = "Disable failed. Matching game mod not found",
            ["Msg.OperationFailed"] = "Operation failed: {0}",
            ["Msg.ModMoveOutFailed"] = "Move out failed. Matching mod directory not found",
            ["Msg.ModMoveOutException"] = "Move out failed: {0}",
            ["Msg.NoSteamId"] = "No available Steam ID found",
            ["Msg.SelectDirectionAndSlot"] = "Please select copy direction and slot",
            ["Msg.CopySaveConfirm"] = "⚠️ Confirm save copy?\n\nSteam ID: {0}\nDirection: {1}\nSlot: {2}\n\nThe target slot will be backed up to Backup/Saves first, then copied.\nContinue?",
            ["Msg.CopySaveException"] = "Save copy failed: {0}",
            ["Msg.CopySaveSourceMissing"] = "Save copy failed. Source slot save may not exist",
            ["Msg.CopySaveDone"] = "Save copy succeeded!\nBackup location: {0}",
            ["Msg.SelectRestoreBackup"] = "Please select a backup to restore",
            ["Msg.RestoreConfirm"] = "Confirm restoring this backup?\n\nTime: {0:yyyy-MM-dd HH:mm:ss}\nSteam ID: {1}\nBackup Path: {2}\nBackup Type: {3}\nRestore Target: {4}\n\nCurrent saves will be backed up automatically before restore.",
            ["Msg.RestoreException"] = "Restore failed: {0}",
            ["Msg.RestoreInvalidBackup"] = "Restore failed. Backup path may be invalid",
            ["Msg.RestoreDone"] = "Restore completed.\nPre-restore backup: {0}",
            ["Msg.RestoreRescueMissing"] = "No pre-restore backup created",
            ["Msg.ManualBackupException"] = "Manual backup failed: {0}",
            ["Msg.ManualBackupIdMissing"] = "Manual backup failed. Steam ID directory does not exist",
            ["Msg.ManualBackupDone"] = "Manual backup succeeded!\nBackup location: {0}",
            ["Msg.GameLaunchFailed"] = "Failed to launch game. Please check game path",
            ["Msg.SteamLaunchFailed"] = "Steam launch failed. Please confirm Steam is installed and logged in",
            ["Msg.GithubSyncSummary"] = "Sync done\nUpdated: {0}\nInvalid links: {1}\nAlready latest: {2}\nDuplicate repo hints: {3}\nLog: {4}",
            ["Msg.GithubDuplicateConfirm"] = "Detected {0} duplicate enabled repository mappings.\nOnly one in each group will be updated. Continue?",
            ["Msg.GithubNoCandidate"] = "No updatable directory available.",
            ["Msg.GithubSourceRequired"] = "Please select at least one source directory.",
            ["Dialog.GithubSync.Title"] = "GitHub Sync Progress",
            ["Dialog.GithubSync.Header"] = "Syncing GitHub Mods",
            ["Dialog.GithubSync.SourceSelectTitle"] = "Select Source Directories",
            ["Dialog.GithubSync.SourceSelectHeader"] = "Choose directories to sync",
            ["Dialog.GithubSync.SourceSelectHint"] = "Only mods under selected directories will be updated.",
            ["Dialog.Confirm.Copy"] = "Confirm Copy",
            ["Dialog.Confirm.Restore"] = "Restore Confirmation",
            ["Dialog.SelectGameDirectory"] = "Select game folder",
            ["Dialog.SelectModSourceDirectory"] = "Select mod source folder"
        }
    };

    [ObservableProperty]
    private string _languageMode = LanguageSystem;

    public event Action? LanguageChanged;

    public LocalizationService(string? savedLanguageMode)
    {
        SetLanguageMode(savedLanguageMode);
    }

    public string this[string key] => GetText(key);

    public List<LanguageOption> GetLanguageOptions()
    {
        return
        [
            new LanguageOption { Key = LanguageSystem, Label = GetText("Language.Option.System") },
            new LanguageOption { Key = LanguageZhCn, Label = GetText("Language.Option.ZhCn") },
            new LanguageOption { Key = LanguageEnUs, Label = GetText("Language.Option.EnUs") }
        ];
    }

    public string CurrentLanguage => ResolveLanguage(LanguageMode);

    public void SetLanguageMode(string? mode)
    {
        var normalized = NormalizeMode(mode);
        if (string.Equals(LanguageMode, normalized, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        LanguageMode = normalized;
    }

    public string GetText(string key)
    {
        var language = CurrentLanguage;
        if (_texts.TryGetValue(language, out var languageDict)
            && languageDict.TryGetValue(key, out var localized))
        {
            return localized;
        }

        if (_texts.TryGetValue(LanguageZhCn, out var defaultDict)
            && defaultDict.TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        return key;
    }

    partial void OnLanguageModeChanged(string value)
    {
        RaiseLanguageChanged();
    }

    private void RaiseLanguageChanged()
    {
        OnPropertyChanged("Item[]");
        OnPropertyChanged(nameof(CurrentLanguage));
        LanguageChanged?.Invoke();
    }

    private static string NormalizeMode(string? mode)
    {
        if (string.Equals(mode, LanguageZhCn, StringComparison.OrdinalIgnoreCase))
        {
            return LanguageZhCn;
        }

        if (string.Equals(mode, LanguageEnUs, StringComparison.OrdinalIgnoreCase))
        {
            return LanguageEnUs;
        }

        return LanguageSystem;
    }

    private static string ResolveLanguage(string? mode)
    {
        if (string.Equals(mode, LanguageZhCn, StringComparison.OrdinalIgnoreCase))
        {
            return LanguageZhCn;
        }

        if (string.Equals(mode, LanguageEnUs, StringComparison.OrdinalIgnoreCase))
        {
            return LanguageEnUs;
        }

        var uiName = CultureInfo.CurrentUICulture.Name;
        return uiName.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
            ? LanguageZhCn
            : LanguageEnUs;
    }
}
