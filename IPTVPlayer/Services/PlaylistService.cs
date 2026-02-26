using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

public partial class PlaylistService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private static readonly (CategoryType Type, string[] GroupKeywords, string[] UrlPatterns)[] CategoryRules =
    [
        (CategoryType.Series,
         ["series", "serie", "temporada", "season", "episode", "episodio", "s0", "s1", "s2"],
         ["/series/", "/serie/"]),

        (CategoryType.Movies,
         ["vod", "movie", "movies", "pelicula", "peliculas", "película", "películas", "film", "cine", "cinema"],
         ["/movie/", "/movies/", "/vod/"]),

        (CategoryType.LiveTV,
         ["live", "en vivo", "tv", "deportes", "sports", "news", "noticias", "music", "musica",
          "kids", "infantil", "entertainment", "entretenimiento", "documentar", "religious",
          "education", "local", "regional", "nacional", "international", "ppv", "24/7"],
         ["/live/", ":8080/", ":80/"])
    ];

    [GeneratedRegex(@"[|\-–—/\\:]", RegexOptions.Compiled)]
    private static partial Regex GroupSeparatorRegex();

    public string? EpgUrl { get; private set; }

    public async Task<List<ContentCategory>> LoadPlaylistAsync(string urlOrPath)
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

        EpgUrl = M3UParserService.ParseEpgUrl(content);

        var channels = M3UParserService.Parse(content);

        foreach (var ch in channels)
            ch.Category = DetectCategory(ch);

        return GroupByCategory(channels);
    }

    // Keywords that indicate live 24/7 streams (should always be LiveTV)
    private static readonly string[] LiveStreamIndicators =
        ["24/7", "24hrs", "24 horas", "24horas", "24 hrs"];

    private static CategoryType DetectCategory(Channel channel)
    {
        var groupLower = channel.GroupTitle.ToLowerInvariant();
        var urlLower = channel.Url.ToLowerInvariant();
        var nameLower = channel.Name.ToLowerInvariant();

        // 1. Channels with 24/7 indicators in the name are always LiveTV
        foreach (var indicator in LiveStreamIndicators)
        {
            if (nameLower.Contains(indicator, StringComparison.Ordinal))
                return CategoryType.LiveTV;
        }

        // 2. Check if group title matches keywords from multiple categories
        //    e.g. "cine y series" matches both Movies ("cine") and Series ("series")
        //    → these are live TV channels broadcasting mixed content
        var seriesKeywords = CategoryRules[0].GroupKeywords;
        var movieKeywords = CategoryRules[1].GroupKeywords;

        bool matchesSeries = seriesKeywords.Any(kw => groupLower.Contains(kw, StringComparison.Ordinal));
        bool matchesMovies = movieKeywords.Any(kw => groupLower.Contains(kw, StringComparison.Ordinal));

        if (matchesSeries && matchesMovies)
            return CategoryType.LiveTV;

        // 3. Normal single-category detection
        foreach (var rule in CategoryRules)
        {
            foreach (var keyword in rule.GroupKeywords)
            {
                if (groupLower.Contains(keyword, StringComparison.Ordinal))
                    return rule.Type;
            }

            foreach (var pattern in rule.UrlPatterns)
            {
                if (urlLower.Contains(pattern, StringComparison.Ordinal))
                    return rule.Type;
            }
        }

        return CategoryType.LiveTV;
    }

    private static string CleanGroupTitle(string groupTitle)
    {
        if (string.IsNullOrWhiteSpace(groupTitle))
            return "Sin Grupo";

        var parts = GroupSeparatorRegex().Split(groupTitle, 2);
        if (parts.Length == 2)
        {
            var prefix = parts[0].Trim().ToLowerInvariant();
            var suffix = parts[1].Trim();

            var categoryKeywords = new[] { "vod", "series", "serie", "movie", "movies",
                "live", "tv", "pelicula", "peliculas", "película", "películas", "film" };

            foreach (var kw in categoryKeywords)
            {
                if (prefix.Contains(kw, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(suffix))
                    return suffix;
            }
        }

        return groupTitle.Trim();
    }

    private static List<ContentCategory> GroupByCategory(List<Channel> channels)
    {
        var categories = channels
            .GroupBy(c => c.Category)
            .Select(catGroup =>
            {
                var type = catGroup.Key;
                var groups = catGroup
                    .GroupBy(c => CleanGroupTitle(c.GroupTitle))
                    .Select(g => new ChannelGroup
                    {
                        Name = g.Key,
                        Channels = new ObservableCollection<Channel>(g.ToList())
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                return new ContentCategory
                {
                    Name = ContentCategory.GetDisplayName(type),
                    Type = type,
                    Icon = ContentCategory.GetIcon(type),
                    Groups = new ObservableCollection<ChannelGroup>(groups)
                };
            })
            .OrderBy(c => c.Type)
            .ToList();

        return categories;
    }
}
