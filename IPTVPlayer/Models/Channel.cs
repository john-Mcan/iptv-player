namespace IPTVPlayer.Models;

public class Channel
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string GroupTitle { get; set; } = string.Empty;
    public string TvgId { get; set; } = string.Empty;
    public string TvgName { get; set; } = string.Empty;
    public CategoryType Category { get; set; } = CategoryType.LiveTV;
}
