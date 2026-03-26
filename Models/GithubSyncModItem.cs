namespace StS2ModManager.Models;

public class GithubSyncModItem
{
    public string ModKey { get; set; } = string.Empty;
    public string FolderName { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string RepoUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public bool Available { get; set; } = true;
    public string CurrentVersion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string AuthorUrl { get; set; } = string.Empty;
    public string LastSyncAt { get; set; } = string.Empty;
    public string LastError { get; set; } = string.Empty;
}
