using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

/// <summary>
/// Cliente de la API de paneles Xtream Codes (player_api.php). Arma el mismo catálogo que una lista M3U:
/// TV en vivo, películas y series. Los episodios de cada serie se piden recién cuando se abre la serie.
/// </summary>
public partial class XtreamApiService
{
    private static readonly HttpClient SharedHttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _username;
    private readonly string _password;
    private string _liveExtension = "ts";

    public XtreamApiService(PlaylistSource source, HttpClient? httpClient = null)
    {
        if (!Uri.TryCreate(source.Url.Trim(), UriKind.Absolute, out var server)
            || (server.Scheme != Uri.UriSchemeHttp && server.Scheme != Uri.UriSchemeHttps))
            throw new ArgumentException("La URL del servidor Xtream no es válida. Usa el formato http://servidor:puerto.");

        _httpClient = httpClient ?? SharedHttpClient;
        _baseUrl = server.GetLeftPart(UriPartial.Path).TrimEnd('/');
        _username = Uri.EscapeDataString(source.Username.Trim());
        _password = Uri.EscapeDataString(source.Password);
    }

    /// <summary>La guía XMLTV que publica el panel para la cuenta.</summary>
    public string EpgUrl => $"{_baseUrl}/xmltv.php?username={_username}&password={_password}";

    /// <summary>Comprueba la cuenta y descarga categorías y contenidos (las series, sin episodios).</summary>
    public async Task<List<ContentCategory>> LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        await AuthenticateAsync(cancellationToken);

        var liveCategories = GetAsync("get_live_categories", cancellationToken);
        var liveStreams = GetAsync("get_live_streams", cancellationToken);
        var vodCategories = GetAsync("get_vod_categories", cancellationToken);
        var vodStreams = GetAsync("get_vod_streams", cancellationToken);
        var seriesCategories = GetAsync("get_series_categories", cancellationToken);
        var series = GetAsync("get_series", cancellationToken);

        using var liveCategoriesJson = await liveCategories;
        using var liveStreamsJson = await liveStreams;
        using var vodCategoriesJson = await vodCategories;
        using var vodStreamsJson = await vodStreams;
        using var seriesCategoriesJson = await seriesCategories;
        using var seriesJson = await series;

        var liveGroups = CategoryNames(liveCategoriesJson.RootElement);
        var vodGroups = CategoryNames(vodCategoriesJson.RootElement);
        var seriesGroups = CategoryNames(seriesCategoriesJson.RootElement);

        var live = Items(liveStreamsJson.RootElement)
            .Select(s => new Channel
            {
                Name = Text(s, "name"),
                Url = $"{_baseUrl}/live/{_username}/{_password}/{Text(s, "stream_id")}.{_liveExtension}",
                LogoUrl = Text(s, "stream_icon"),
                TvgId = Text(s, "epg_channel_id"),
                TvgName = Text(s, "name"),
                GroupTitle = GroupName(liveGroups, s),
                Category = CategoryType.LiveTV
            });

        var movies = Items(vodStreamsJson.RootElement)
            .OrderByDescending(s => Number(s, "added"))
            .Select(s => new Channel
            {
                Name = Text(s, "name"),
                Url = $"{_baseUrl}/movie/{_username}/{_password}/{Text(s, "stream_id")}.{Extension(s)}",
                LogoUrl = Text(s, "stream_icon"),
                GroupTitle = GroupName(vodGroups, s),
                Category = CategoryType.Movies
            });

        var shows = Items(seriesJson.RootElement)
            .OrderByDescending(s => Number(s, "last_modified"))
            .Select(s => new Channel
            {
                Name = Text(s, "name"),
                LogoUrl = Text(s, "cover"),
                GroupTitle = GroupName(seriesGroups, s),
                SeriesId = Text(s, "series_id"),
                Category = CategoryType.Series
            });

        return new[]
            {
                BuildCategory(CategoryType.LiveTV, live),
                BuildCategory(CategoryType.Movies, movies),
                BuildCategory(CategoryType.Series, shows)
            }
            .Where(c => c.Groups.Count > 0)
            .ToList();
    }

    /// <summary>
    /// Los episodios de <paramref name="series"/>, en orden y nombrados «Serie S01E02 — título»
    /// para que la navegación de series los agrupe igual que los de una lista M3U.
    /// </summary>
    public async Task<List<Channel>> LoadSeriesEpisodesAsync(Channel series, CancellationToken cancellationToken = default)
    {
        using var json = await GetAsync($"get_series_info&series_id={Uri.EscapeDataString(series.SeriesId)}", cancellationToken);
        if (json.RootElement.ValueKind != JsonValueKind.Object
            || !json.RootElement.TryGetProperty("episodes", out var episodesBySeason))
            return [];

        // Según el panel, «episodes» es un objeto { "1": [...], "2": [...] } o un arreglo de arreglos.
        var seasons = episodesBySeason.ValueKind switch
        {
            JsonValueKind.Object => episodesBySeason.EnumerateObject()
                .Select(p => (Key: int.TryParse(p.Name, out var n) ? n : 0, Items: p.Value)),
            JsonValueKind.Array => episodesBySeason.EnumerateArray().Select((items, index) => (Key: index + 1, Items: items)),
            _ => []
        };

        return seasons
            .SelectMany(s => Items(s.Items).Select(e => (SeasonKey: s.Key, Episode: e)))
            .Select(item =>
            {
                var e = item.Episode;
                var season = (int)Number(e, "season") is > 0 and var s ? s : item.SeasonKey;
                var number = (int)Number(e, "episode_num");
                var title = EpisodeTitle(Text(e, "title"), series.Name);
                var info = e.TryGetProperty("info", out var i) ? i : default;
                var image = Text(info, "movie_image");

                return (Season: season, Number: number, Channel: new Channel
                {
                    Name = $"{series.Name} S{season:00}E{number:00}" + (title.Length > 0 ? $" \u2014 {title}" : string.Empty),
                    Url = $"{_baseUrl}/series/{_username}/{_password}/{Text(e, "id")}.{Extension(e)}",
                    LogoUrl = string.IsNullOrEmpty(image) ? series.LogoUrl : image,
                    GroupTitle = series.GroupTitle,
                    Category = CategoryType.Series
                });
            })
            .Where(e => e.Season > 0)
            .OrderBy(e => e.Season)
            .ThenBy(e => e.Number)
            .Select(e => e.Channel)
            .ToList();
    }

    private async Task AuthenticateAsync(CancellationToken cancellationToken)
    {
        using var json = await GetAsync(action: null, cancellationToken);
        var root = json.RootElement;

        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("user_info", out var user)
            || Text(user, "auth") != "1")
            throw new InvalidOperationException("El servidor Xtream rechazó el usuario o la contraseña.");

        var status = Text(user, "status");
        if (status.Length > 0 && !status.Equals("Active", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"La cuenta Xtream no está activa (estado: {status}).");

        // Se prefiere MPEG-TS; si el panel no lo permite, HLS.
        if (user.TryGetProperty("allowed_output_formats", out var formats) && formats.ValueKind == JsonValueKind.Array)
        {
            var allowed = formats.EnumerateArray().Select(f => f.ToString()).ToList();
            if (!allowed.Contains("ts") && allowed.Contains("m3u8"))
                _liveExtension = "m3u8";
        }
    }

    private async Task<JsonDocument> GetAsync(string? action, CancellationToken cancellationToken)
    {
        var url = $"{_baseUrl}/player_api.php?username={_username}&password={_password}";
        if (action != null)
            url += $"&action={action}";

        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
    }

    private static ContentCategory BuildCategory(CategoryType type, IEnumerable<Channel> channels)
    {
        var groups = channels
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .GroupBy(c => c.GroupTitle)
            .Select(g => new ChannelGroup { Name = g.Key, Channels = new ObservableCollection<Channel>(g) })
            .OrderBy(g => g.Name)
            .ToList();

        return new ContentCategory
        {
            Name = ContentCategory.GetDisplayName(type),
            Type = type,
            Icon = ContentCategory.GetIcon(type),
            Groups = new ObservableCollection<ChannelGroup>(groups)
        };
    }

    private static Dictionary<string, string> CategoryNames(JsonElement categories)
        => Items(categories)
            .Select(c => (Id: Text(c, "category_id"), Name: Text(c, "category_name")))
            .Where(c => c.Id.Length > 0)
            .DistinctBy(c => c.Id)
            .ToDictionary(c => c.Id, c => string.IsNullOrWhiteSpace(c.Name) ? "Sin Grupo" : c.Name.Trim());

    private static string GroupName(Dictionary<string, string> categoryNames, JsonElement item)
        => categoryNames.TryGetValue(Text(item, "category_id"), out var name) ? name : "Sin Grupo";

    // Mismos formatos que reconoce EpisodeParser: S01E02, T01E02 y 1x02.
    [GeneratedRegex(@"[Ss]\d{1,2}\s*[Ee]\d{1,3}|[Tt]\d{1,2}\s*[Ee]\d{1,3}|(?<!\d)\d{1,2}[xX]\d{1,3}(?!\d)")]
    private static partial Regex EpisodeCode();

    /// <summary>
    /// Deja solo el título propio del episodio. Muchos paneles lo mandan como «Serie - S01E02 - Título»;
    /// se quita lo que va hasta el código y el nombre de la serie si viene al principio.
    /// </summary>
    internal static string EpisodeTitle(string title, string seriesName)
    {
        var clean = title.Trim();

        var code = EpisodeCode().Match(clean);
        if (code.Success)
            clean = clean[(code.Index + code.Length)..];
        else if (seriesName.Length > 0 && clean.StartsWith(seriesName, StringComparison.OrdinalIgnoreCase))
            clean = clean[seriesName.Length..];

        return clean.Trim(' ', '-', '\u2013', '\u2014', ':', '.', '|');
    }

    private static IEnumerable<JsonElement> Items(JsonElement array)
        => array.ValueKind == JsonValueKind.Array
            ? array.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object)
            : [];

    /// <summary>Lee una propiedad como texto; los paneles mandan los números a veces como número y a veces como texto.</summary>
    private static string Text(JsonElement element, string property)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty(property, out var value))
            return string.Empty;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            JsonValueKind.True => "1",
            JsonValueKind.False => "0",
            _ => string.Empty
        };
    }

    private static long Number(JsonElement element, string property)
        => decimal.TryParse(Text(element, property), NumberStyles.Number, CultureInfo.InvariantCulture, out var n) ? (long)n : 0;

    private static string Extension(JsonElement element)
    {
        var extension = Text(element, "container_extension").TrimStart('.');
        return extension.Length > 0 ? extension : "mp4";
    }
}
