using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

public class PlaylistService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<List<ChannelGroup>> LoadPlaylistAsync(string urlOrPath)
    {
        string content;

        if (Uri.TryCreate(urlOrPath, UriKind.Absolute, out var uri)
            && (uri.Scheme == "http" || uri.Scheme == "https"))
        {
            content = await HttpClient.GetStringAsync(uri);
        }
        else if (File.Exists(urlOrPath))
        {
            content = await File.ReadAllTextAsync(urlOrPath);
        }
        else
        {
            throw new ArgumentException("URL o ruta de archivo inválida.");
        }

        var channels = M3UParserService.Parse(content);
        return GroupChannels(channels);
    }

    private static List<ChannelGroup> GroupChannels(List<Channel> channels)
    {
        return channels
            .GroupBy(c => string.IsNullOrWhiteSpace(c.GroupTitle) ? "Sin Grupo" : c.GroupTitle)
            .Select(g => new ChannelGroup
            {
                Name = g.Key,
                Channels = new ObservableCollection<Channel>(g.ToList())
            })
            .OrderBy(g => g.Name)
            .ToList();
    }
}
