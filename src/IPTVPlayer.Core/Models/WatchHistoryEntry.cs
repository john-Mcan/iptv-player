namespace IPTVPlayer.Models;

public class WatchHistoryEntry
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty;
    public long PositionMs { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public CategoryType Category { get; set; } = CategoryType.LiveTV;

    public double ProgressPercent => DurationMs > 0 ? (double)PositionMs / DurationMs * 100 : 0;
    public bool HasProgress => PositionMs > 0 && DurationMs > 0 && ProgressPercent < 95;
}
