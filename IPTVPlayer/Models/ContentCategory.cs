using System.Collections.ObjectModel;

namespace IPTVPlayer.Models;

public enum CategoryType
{
    LiveTV,
    Movies,
    Series,
    Other
}

public class ContentCategory
{
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Icon { get; set; } = string.Empty;
    public ObservableCollection<ChannelGroup> Groups { get; set; } = new();

    public int TotalChannels => Groups.Sum(g => g.Channels.Count);

    public static string GetDisplayName(CategoryType type) => type switch
    {
        CategoryType.LiveTV => "📺  Live TV",
        CategoryType.Movies => "🎬  Películas",
        CategoryType.Series => "📺  Series",
        CategoryType.Other => "📁  Otros",
        _ => "📁  Otros"
    };

    public static string GetIcon(CategoryType type) => type switch
    {
        CategoryType.LiveTV => "\xE7F4",    // TV icon
        CategoryType.Movies => "\xE8B2",    // Film icon
        CategoryType.Series => "\xE786",    // Library icon
        CategoryType.Other => "\xE8FD",     // Folder icon
        _ => "\xE8FD"
    };
}
