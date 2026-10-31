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

    /// <summary>
    /// Solo en fuentes Xtream: id de una serie cuyos episodios todavía no se cargan.
    /// Mientras tanto la serie aparece como un único elemento sin URL.
    /// </summary>
    public string SeriesId { get; set; } = string.Empty;

    public bool IsPendingSeries => !string.IsNullOrEmpty(SeriesId) && string.IsNullOrEmpty(Url);

    [ObservableProperty]
    private bool _isFavorite;
}
