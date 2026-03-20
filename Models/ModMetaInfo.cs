namespace StS2ModManager.Models;

public class ModMetaInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Remark { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string AuthorUrl { get; set; } = string.Empty;
    public string DetailUrl { get; set; } = string.Empty;
    public string SocialUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Dependencies { get; set; } = [];
    public bool AffectsGameplay { get; set; } = false;
}
