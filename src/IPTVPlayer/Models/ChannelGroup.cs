using System.Collections.ObjectModel;

namespace IPTVPlayer.Models;

public class ChannelGroup
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<Channel> Channels { get; set; } = new();
}
