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

    /// <summary>
    /// Keywords and URL patterns used to detect content category.
    /// Order matters: first match wins.
    /// </summary>
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

        var channels = M3UParserService.Parse(content);

        // Classify each channel
        foreach (var ch in channels)
            ch.Category = DetectCategory(ch);

        return GroupByCategory(channels);
    }

    /// <summary>
    /// Detects the content category of a channel using group-title keywords
    /// and URL patterns. Falls back to LiveTV for ambiguous cases.
    /// </summary>
    private static CategoryType DetectCategory(Channel channel)
    {
        var groupLower = channel.GroupTitle.ToLowerInvariant();
        var urlLower = channel.Url.ToLowerInvariant();

        foreach (var rule in CategoryRules)
        {
            // Check group-title keywords
            foreach (var keyword in rule.GroupKeywords)
            {
                if (groupLower.Contains(keyword, StringComparison.Ordinal))
                    return rule.Type;
            }

            // Check URL patterns
            foreach (var pattern in rule.UrlPatterns)
            {
                if (urlLower.Contains(pattern, StringComparison.Ordinal))
                    return rule.Type;
            }
        }

        // Default: if group is empty or unrecognized, classify as LiveTV
        return CategoryType.LiveTV;
    }

    /// <summary>
    /// Cleans up the group title by removing category prefix if present.
    /// E.g., "VOD | Action Movies" → "Action Movies"
    /// </summary>
    private static string CleanGroupTitle(string groupTitle)
    {
        if (string.IsNullOrWhiteSpace(groupTitle))
            return "Sin Grupo";

        // Try to split on common separators and remove the category prefix
        var parts = GroupSeparatorRegex().Split(groupTitle, 2);
        if (parts.Length == 2)
        {
            var prefix = parts[0].Trim().ToLowerInvariant();
            var suffix = parts[1].Trim();

            // If the prefix looks like a category keyword, use only the suffix
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

    /// <summary>
    /// Groups channels first by category, then by cleaned group-title within each category.
    /// </summary>
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
            .OrderBy(c => c.Type) // LiveTV first, then Movies, Series, Other
            .ToList();

        return categories;
    }
}
