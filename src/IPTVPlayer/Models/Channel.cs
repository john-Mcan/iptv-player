using CommunityToolkit.Mvvm.ComponentModel;

namespace IPTVPlayer.Models;

public partial class Channel : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public string GroupTitle { get; set; } = string.Empty;
    public string TvgId { get; set; } = string.Empty;
    public string TvgName { get; set; } = string.Empty;
    public CategoryType Category { get; set; } = CategoryType.LiveTV;

    [ObservableProperty]
    private bool _isFavorite;
}
