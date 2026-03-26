using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using StS2ModManager.Models;

namespace StS2ModManager.Services;

public class GithubSyncSummary
{
    public int Total { get; set; }
    public int Updated { get; set; }
    public int Invalid { get; set; }
    public int Latest { get; set; }
    public int DuplicateRepoHints { get; set; }
    public string LogFilePath { get; set; } = string.Empty;
}

public class GithubSyncProgress
{
    public int Current { get; set; }
    public int Total { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class GithubModSyncService
{
    private static readonly HttpClient HttpClient = new();
    private static readonly object LogFileLock = new();
    private readonly ModService _modService;
    private string _logFilePath = string.Empty;

    public GithubModSyncService(ModService modService)
    {
        _modService = modService;
    }

    public void EnsureGithubSyncList(AppSettings settings, IEnumerable<ModInfo> mods)
    {
        settings.GithubSyncMods ??= new List<GithubSyncModItem>();
        var existingKeys = settings.GithubSyncMods
            .Select(x => x.ModKey)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var mod in mods)
        {
            var repoUrl = ExtractGithubRepoUrl(mod.DetailUrl);
            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                repoUrl = ExtractGithubRepoUrl(mod.DownloadUrl);
            }

            if (string.IsNullOrWhiteSpace(repoUrl))
            {
                continue;
            }

            if (existingKeys.Contains(mod.ModKey))
            {
                continue;
            }

            settings.GithubSyncMods.Add(new GithubSyncModItem
            {
                ModKey = mod.ModKey,
                FolderName = mod.FolderName,
                SourcePath = mod.SourcePath,
                RepoUrl = repoUrl,
                Enabled = true,
                Available = true,
                CurrentVersion = mod.Version ?? string.Empty,
                Description = mod.Description ?? string.Empty,
                DetailUrl = mod.DetailUrl ?? string.Empty,
                DownloadUrl = mod.DownloadUrl ?? string.Empty,
                AuthorUrl = mod.AuthorUrl ?? string.Empty
            });
            existingKeys.Add(mod.ModKey);
        }
    }

    public async Task<GithubSyncSummary> SyncAsync(
        AppSettings settings,
        IReadOnlyCollection<ModInfo> mods,
        ISet<string>? selectedSourcePaths = null,
        IProgress<GithubSyncProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        settings.GithubSyncMods ??= new List<GithubSyncModItem>();
        InitializeRunLog();
        LogInfo($"同步开始，可见Mod总数: {mods.Count}, 启用记录总数: {settings.GithubSyncMods.Count}");
        if (selectedSourcePaths != null)
        {
            LogInfo($"目录筛选已启用，勾选目录数: {selectedSourcePaths.Count}");
        }

        var modMap = mods
            .GroupBy(x => $"{x.SourcePath}|{x.FolderName}", StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var enabledRecords = settings.GithubSyncMods
            .Where(x => x.Enabled && x.Available && !string.IsNullOrWhiteSpace(x.RepoUrl))
            .Where(x => selectedSourcePaths == null || selectedSourcePaths.Contains(x.SourcePath))
            .ToList();
        LogInfo($"参与同步的启用记录数: {enabledRecords.Count}");

        var summary = new GithubSyncSummary();
        var targetRecords = new List<GithubSyncModItem>();
        foreach (var group in enabledRecords.GroupBy(x => NormalizeRepoUrl(x.RepoUrl), StringComparer.OrdinalIgnoreCase))
        {
            var records = group.ToList();
            if (records.Count > 1)
            {
                summary.DuplicateRepoHints++;
                LogInfo($"检测到重复仓库映射: {group.Key}, 条目数: {records.Count}，将只处理其中一个。");
            }

            var chosen = records.FirstOrDefault(x => modMap.ContainsKey($"{x.SourcePath}|{x.FolderName}"))
                         ?? records.First();
            targetRecords.Add(chosen);
        }

        summary.Total = targetRecords.Count;
        var current = 0;
        foreach (var record in targetRecords)
        {
            cancellationToken.ThrowIfCancellationRequested();
            current++;
            progress?.Report(new GithubSyncProgress
            {
                Current = current,
                Total = targetRecords.Count,
                Message = $"正在同步 {record.FolderName}"
            });

            var modLookupKey = $"{record.SourcePath}|{record.FolderName}";
            if (!modMap.TryGetValue(modLookupKey, out var mod))
            {
                record.Available = false;
                record.Enabled = false;
                record.LastError = "未找到本地Mod目录";
                record.LastSyncAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                summary.Invalid++;
                LogInfo($"[{record.FolderName}] 失败：未找到本地目录，Key={modLookupKey}");
                continue;
            }

            LogInfo($"[{record.FolderName}] 开始同步，仓库={record.RepoUrl}, 当前版本={record.CurrentVersion}");
            var result = await SyncSingleRecord(record, mod, cancellationToken);
            record.LastSyncAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            record.LastError = result.ErrorMessage;
            record.Available = result.Success || result.IsLatest;
            if (!result.Success && !result.IsLatest)
            {
                record.Enabled = false;
            }

            if (result.IsLatest)
            {
                summary.Latest++;
                LogInfo($"[{record.FolderName}] 已是最新版本");
            }
            else if (result.Success)
            {
                summary.Updated++;
                record.CurrentVersion = result.Version;
                record.DetailUrl = result.DetailUrl;
                record.DownloadUrl = result.DownloadUrl;
                record.AuthorUrl = result.AuthorUrl;
                record.Description = result.Description;
                if (!string.IsNullOrWhiteSpace(result.NewFolderName))
                {
                    record.FolderName = result.NewFolderName;
                    record.ModKey = $"{record.SourcePath}|{result.NewFolderName}";
                }
                LogInfo($"[{record.FolderName}] 更新成功 -> 版本 {result.Version}");
            }
            else
            {
                summary.Invalid++;
                LogInfo($"[{record.FolderName}] 更新失败：{result.ErrorMessage}");
            }
        }

        LogInfo($"同步结束：更新={summary.Updated}, 无效={summary.Invalid}, 最新={summary.Latest}, 重复提示={summary.DuplicateRepoHints}");
        summary.LogFilePath = _logFilePath;
        return summary;
    }

    private async Task<SingleSyncResult> SyncSingleRecord(GithubSyncModItem record, ModInfo mod, CancellationToken cancellationToken)
    {
        try
        {
            var repo = TryParseRepo(record.RepoUrl);
            if (repo == null)
            {
                return SingleSyncResult.Fail("仓库地址无效");
            }

            var release = GetLatestRelease(repo.Value.owner, repo.Value.name);
            if (!release.Success)
            {
                return SingleSyncResult.Fail(release.ErrorMessage);
            }
            LogInfo($"[{record.FolderName}] 获取Release成功，tag={release.Tag}, 资产数={release.AssetUrls.Count}");

            if (string.Equals(record.CurrentVersion, release.Tag, StringComparison.OrdinalIgnoreCase))
            {
                return SingleSyncResult.Latest();
            }

            var modMeta = _modService.LoadModMetaByPath(mod.FolderPath, mod.FolderName);
            using var workspace = new TempWorkspace();
            var downloadsDir = Path.Combine(workspace.Root, "downloads");
            var extractDir = Path.Combine(workspace.Root, "extracted");
            var flatDir = Path.Combine(workspace.Root, "flat");
            Directory.CreateDirectory(downloadsDir);
            Directory.CreateDirectory(extractDir);
            Directory.CreateDirectory(flatDir);

            var downloadUrls = release.AssetUrls
                .Where(IsSupportedAssetUrl)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (downloadUrls.Count == 0)
            {
                return SingleSyncResult.Fail("Release中没有可识别资产");
            }
            LogInfo($"[{record.FolderName}] 可识别资产数量: {downloadUrls.Count}");

            foreach (var url in downloadUrls)
            {
                var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    fileName = Guid.NewGuid().ToString("N");
                }

                var localPath = Path.Combine(downloadsDir, fileName);
                await DownloadFileAsync(url, localPath, cancellationToken);
                LogInfo($"[{record.FolderName}] 已下载资产: {fileName}");
                if (fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    var zipExtract = Path.Combine(extractDir, Path.GetFileNameWithoutExtension(fileName));
                    Directory.CreateDirectory(zipExtract);
                    ZipFile.ExtractToDirectory(localPath, zipExtract, true);
                    LogInfo($"[{record.FolderName}] 已解压: {fileName}");
                }
            }

            var copiedHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            FlattenSupportedFiles(downloadsDir, flatDir, copiedHashes);
            FlattenSupportedFiles(extractDir, flatDir, copiedHashes);
            DeleteAllSubDirectories(flatDir);

            var collectedFiles = Directory.GetFiles(flatDir, "*", SearchOption.TopDirectoryOnly);
            var hasUsefulFile = collectedFiles.Any(IsSupportedAssetFile);
            if (!hasUsefulFile)
            {
                return SingleSyncResult.Fail("下载内容中不存在dll/pck/json");
            }
            LogInfo($"[{record.FolderName}] 扁平化后文件数: {collectedFiles.Length}");

            var versionToken = SanitizeFileNameToken(release.Tag);
            var newFolderName = BuildVersionedFolderName(mod.FolderName, versionToken);
            var targetFolder = Path.Combine(mod.SourcePath, newFolderName);
            var backupRoot = Path.Combine(GamePathService.BackupDir, "Mods", "Updates", DateTime.Now.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupRoot);

            if (Directory.Exists(mod.FolderPath))
            {
                var backupPath = Path.Combine(backupRoot, mod.FolderName);
                if (Directory.Exists(backupPath))
                {
                    Directory.Delete(backupPath, true);
                }

                MoveOrCopyDirectory(mod.FolderPath, backupPath);
                LogInfo($"[{record.FolderName}] 已备份旧目录 -> {backupPath}");
            }

            if (Directory.Exists(targetFolder))
            {
                Directory.Delete(targetFolder, true);
            }

            MoveOrCopyDirectory(flatDir, targetFolder);
            LogInfo($"[{record.FolderName}] 已部署新目录 -> {targetFolder}");

            var mergedMeta = _modService.LoadModMetaByPath(targetFolder, newFolderName);
            MergeMissingFields(mergedMeta, modMeta);
            mergedMeta.Version = release.Tag;
            mergedMeta.DownloadUrl = release.AssetUrls.FirstOrDefault() ?? mergedMeta.DownloadUrl;
            mergedMeta.DetailUrl = release.ReleaseUrl;
            mergedMeta.AuthorUrl = $"https://github.com/{repo.Value.owner}";
            if (!string.IsNullOrWhiteSpace(release.Body))
            {
                mergedMeta.Description = release.Body;
            }

            _modService.SaveModMetaByPath(targetFolder, newFolderName, mergedMeta);
            LogInfo($"[{record.FolderName}] 已回写元数据JSON");

            return SingleSyncResult.Ok(
                release.Tag,
                release.Body,
                release.ReleaseUrl,
                mergedMeta.DownloadUrl,
                mergedMeta.AuthorUrl,
                newFolderName);
        }
        catch (Exception ex)
        {
            LogError($"[{record.FolderName}] 异常: {ex}");
            return SingleSyncResult.Fail(ex.Message);
        }
    }

    private static void CopyDirectoryRecursive(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            var targetDir = Path.GetDirectoryName(target);
            if (!string.IsNullOrWhiteSpace(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            File.Copy(file, target, true);
        }
    }

    private static void MoveOrCopyDirectory(string source, string destination)
    {
        try
        {
            Directory.Move(source, destination);
            return;
        }
        catch (IOException)
        {
            // 跨卷移动失败时回退复制+删除
        }
        catch (UnauthorizedAccessException)
        {
            // 目标盘或权限限制时回退复制+删除
        }

        CopyDirectoryRecursive(source, destination);
        Directory.Delete(source, true);
    }

    private static void MergeMissingFields(ModMetaInfo target, ModMetaInfo source)
    {
        target.Id = FillIfEmpty(target.Id, source.Id);
        target.Name = FillIfEmpty(target.Name, source.Name);
        target.Tag = FillIfEmpty(target.Tag, source.Tag);
        target.Detail = FillIfEmpty(target.Detail, source.Detail);
        target.Remark = FillIfEmpty(target.Remark, source.Remark);
        target.Author = FillIfEmpty(target.Author, source.Author);
        target.Description = FillIfEmpty(target.Description, source.Description);
        target.DownloadUrl = FillIfEmpty(target.DownloadUrl, source.DownloadUrl);
        target.AuthorUrl = FillIfEmpty(target.AuthorUrl, source.AuthorUrl);
        target.DetailUrl = FillIfEmpty(target.DetailUrl, source.DetailUrl);
        target.SocialUrl = FillIfEmpty(target.SocialUrl, source.SocialUrl);
        if ((target.Dependencies == null || target.Dependencies.Count == 0) && source.Dependencies.Count > 0)
        {
            target.Dependencies = source.Dependencies.ToList();
        }

        if (!target.AffectsGameplay && source.AffectsGameplay)
        {
            target.AffectsGameplay = true;
        }
    }

    private static string FillIfEmpty(string target, string fallback)
    {
        return string.IsNullOrWhiteSpace(target) ? fallback : target;
    }

    private static void FlattenSupportedFiles(string sourceRoot, string flatDir, HashSet<string> copiedHashes)
    {
        if (!Directory.Exists(sourceRoot))
        {
            return;
        }

        foreach (var sourceFile in Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            if (!IsSupportedAssetFile(sourceFile))
            {
                continue;
            }

            var fileHash = ComputeFileHash(sourceFile);
            if (!string.IsNullOrWhiteSpace(fileHash) && copiedHashes.Contains(fileHash))
            {
                continue;
            }

            var fileName = Path.GetFileName(sourceFile);
            var targetPath = Path.Combine(flatDir, fileName);
            var index = 2;
            while (File.Exists(targetPath))
            {
                var withoutExt = Path.GetFileNameWithoutExtension(fileName);
                var ext = Path.GetExtension(fileName);
                targetPath = Path.Combine(flatDir, $"{withoutExt} ({index}){ext}");
                index++;
            }

            File.Copy(sourceFile, targetPath, true);
            if (!string.IsNullOrWhiteSpace(fileHash))
            {
                copiedHashes.Add(fileHash);
            }
        }
    }

    private static string ComputeFileHash(string filePath)
    {
        try
        {
            using var stream = File.OpenRead(filePath);
            var hash = SHA256.HashData(stream);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void DeleteAllSubDirectories(string root)
    {
        if (!Directory.Exists(root))
        {
            return;
        }

        foreach (var subDir in Directory.GetDirectories(root, "*", SearchOption.TopDirectoryOnly))
        {
            Directory.Delete(subDir, true);
        }
    }

    private static bool IsSupportedAssetUrl(string url)
    {
        return url.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
               || url.EndsWith(".pck", StringComparison.OrdinalIgnoreCase)
               || url.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
               || url.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSupportedAssetFile(string filePath)
    {
        return filePath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
               || filePath.EndsWith(".pck", StringComparison.OrdinalIgnoreCase)
               || filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private static string SanitizeFileNameToken(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "latest";
        }

        var invalid = Path.GetInvalidFileNameChars();
        var buffer = input.Trim().Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray();
        return new string(buffer);
    }

    private static string BuildVersionedFolderName(string folderName, string versionToken)
    {
        var baseName = folderName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "Mod";
        }

        var suffix = "_" + versionToken;
        if (baseName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            return baseName;
        }

        var idx = baseName.LastIndexOf('_');
        if (idx > 0)
        {
            var tail = baseName[(idx + 1)..];
            if (tail.StartsWith("v", StringComparison.OrdinalIgnoreCase)
                || tail.All(ch => char.IsDigit(ch) || ch == '.'))
            {
                baseName = baseName[..idx];
            }
        }

        return $"{baseName}_{versionToken}";
    }

    private static string? ExtractGithubRepoUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return null;
        }

        return $"https://github.com/{segments[0]}/{segments[1]}";
    }

    private static string NormalizeRepoUrl(string? repoUrl)
    {
        var extracted = ExtractGithubRepoUrl(repoUrl);
        return extracted ?? string.Empty;
    }

    private static (string owner, string name)? TryParseRepo(string repoUrl)
    {
        var normalized = NormalizeRepoUrl(repoUrl);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var uri = new Uri(normalized);
        var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 2)
        {
            return null;
        }

        return (segments[0], segments[1]);
    }

    private static async Task DownloadFileAsync(string url, string targetPath, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(targetPath);
        await stream.CopyToAsync(file, cancellationToken);
    }

    private static ReleaseInfoResult GetLatestRelease(string owner, string repo)
    {
        try
        {
            var json = RunProcess("gh", $"api repos/{owner}/{repo}/releases/latest");
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var tag = root.TryGetProperty("tag_name", out var tagNode) ? tagNode.GetString() ?? string.Empty : string.Empty;
            var body = root.TryGetProperty("body", out var bodyNode) ? bodyNode.GetString() ?? string.Empty : string.Empty;
            var htmlUrl = root.TryGetProperty("html_url", out var urlNode) ? urlNode.GetString() ?? $"https://github.com/{owner}/{repo}/releases" : $"https://github.com/{owner}/{repo}/releases";
            var assets = new List<string>();
            if (root.TryGetProperty("assets", out var assetsNode) && assetsNode.ValueKind == JsonValueKind.Array)
            {
                foreach (var asset in assetsNode.EnumerateArray())
                {
                    if (!asset.TryGetProperty("browser_download_url", out var downloadNode))
                    {
                        continue;
                    }

                    var assetUrl = downloadNode.GetString();
                    if (!string.IsNullOrWhiteSpace(assetUrl))
                    {
                        assets.Add(assetUrl);
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(tag))
            {
                return ReleaseInfoResult.Fail("未获取到release版本号");
            }

            return ReleaseInfoResult.Ok(tag, body, htmlUrl, assets);
        }
        catch (Exception ex)
        {
            return ReleaseInfoResult.Fail($"读取GitHub Release失败: {ex.Message}");
        }
    }

    private static string RunProcess(string fileName, string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };
        using var process = Process.Start(psi) ?? throw new InvalidOperationException("无法启动外部进程");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? $"命令失败: {fileName} {arguments}" : error.Trim());
        }

        return output;
    }

    private void InitializeRunLog()
    {
        var logDir = Path.Combine(GamePathService.ConfigDir, "Logs");
        Directory.CreateDirectory(logDir);
        _logFilePath = Path.Combine(logDir, $"github_sync_{DateTime.Now:yyyyMMdd_HHmmss}.log");
        LogInfo("日志初始化完成");
    }

    private void LogInfo(string message)
    {
        WriteLog("INFO", message);
    }

    private void LogError(string message)
    {
        WriteLog("ERROR", message);
    }

    private void WriteLog(string level, string message)
    {
        if (string.IsNullOrWhiteSpace(_logFilePath))
        {
            return;
        }

        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
        lock (LogFileLock)
        {
            File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
        }
    }

    private sealed class TempWorkspace : IDisposable
    {
        public string Root { get; }

        public TempWorkspace()
        {
            Root = Path.Combine(Path.GetTempPath(), "StS2ModManager", "sync", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, true);
                }
            }
            catch
            {
                // ignore
            }
        }
    }

    private sealed class ReleaseInfoResult
    {
        public bool Success { get; private set; }
        public string ErrorMessage { get; private set; } = string.Empty;
        public string Tag { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;
        public string ReleaseUrl { get; private set; } = string.Empty;
        public List<string> AssetUrls { get; private set; } = new();

        public static ReleaseInfoResult Ok(string tag, string body, string releaseUrl, List<string> assetUrls)
        {
            return new ReleaseInfoResult
            {
                Success = true,
                Tag = tag,
                Body = body ?? string.Empty,
                ReleaseUrl = releaseUrl ?? string.Empty,
                AssetUrls = assetUrls
            };
        }

        public static ReleaseInfoResult Fail(string error)
        {
            return new ReleaseInfoResult
            {
                Success = false,
                ErrorMessage = error
            };
        }
    }

    private sealed class SingleSyncResult
    {
        public bool Success { get; private set; }
        public bool IsLatest { get; private set; }
        public string Version { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string DetailUrl { get; private set; } = string.Empty;
        public string DownloadUrl { get; private set; } = string.Empty;
        public string AuthorUrl { get; private set; } = string.Empty;
        public string NewFolderName { get; private set; } = string.Empty;
        public string ErrorMessage { get; private set; } = string.Empty;

        public static SingleSyncResult Ok(string version, string description, string detailUrl, string downloadUrl, string authorUrl, string newFolderName)
        {
            return new SingleSyncResult
            {
                Success = true,
                Version = version,
                Description = description ?? string.Empty,
                DetailUrl = detailUrl ?? string.Empty,
                DownloadUrl = downloadUrl ?? string.Empty,
                AuthorUrl = authorUrl ?? string.Empty,
                NewFolderName = newFolderName
            };
        }

        public static SingleSyncResult Latest()
        {
            return new SingleSyncResult
            {
                IsLatest = true,
                Success = false
            };
        }

        public static SingleSyncResult Fail(string error)
        {
            return new SingleSyncResult
            {
                Success = false,
                ErrorMessage = error
            };
        }
    }
}
