namespace IPTVPlayer.Models;

public class AppSettings
{
    public string LastPlaylistUrl { get; set; } = string.Empty;
    public List<string> RecentPlaylistUrls { get; set; } = new();
    public int Volume { get; set; } = 80;
    public bool IsMuted { get; set; }
    public double WindowWidth { get; set; } = 1100;
    public double WindowHeight { get; set; } = 700;
    public double WindowLeft { get; set; } = double.NaN;
    public double WindowTop { get; set; } = double.NaN;
    public double SidebarWidth { get; set; } = 280;
    public bool AutoLoadPlaylist { get; set; } = true;
    public string ActiveTab { get; set; } = "LiveTV";
    public List<string> FavoriteUrls { get; set; } = new();
    public List<string> FavoriteSeriesNames { get; set; } = new();
    public List<WatchHistoryEntry> WatchHistory { get; set; } = new();
}
