namespace IPTVPlayer.Models;

public class EpgProgram
{
    public string ChannelId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public DateTime Stop { get; set; }

    public bool IsCurrentlyAiring => Start <= DateTime.Now && Stop > DateTime.Now;
    public string TimeRange => $"{Start:HH:mm} - {Stop:HH:mm}";
}
