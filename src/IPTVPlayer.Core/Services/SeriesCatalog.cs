using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

/// <summary>Contenido de la pestaña Series con la serie, temporada y episodio que se leyeron de su nombre.</summary>
public readonly record struct SeriesEpisode(string SeriesName, int Season, int Episode, Channel Channel);

/// <summary>
/// Organiza los contenidos de series por serie, temporada y episodio a partir de sus nombres
/// («Serie S01E02», «Serie 1x02»…). Los contenidos sin patrón quedan con temporada 0.
/// Las comparaciones de nombres de serie no distinguen mayúsculas.
/// </summary>
public sealed class SeriesCatalog
{
    private static readonly StringComparer NameComparer = StringComparer.OrdinalIgnoreCase;

    private readonly List<SeriesEpisode> _entries;

    public SeriesCatalog(IEnumerable<Channel> channels)
    {
        _entries = channels
            .Select(c =>
            {
                var (seriesName, season, episode) = EpisodeParser.Parse(c.Name);
                return new SeriesEpisode(seriesName, season, episode, c);
            })
            .ToList();
    }

    public IReadOnlyList<SeriesEpisode> Entries => _entries;

    /// <summary>True si al menos un nombre sigue un patrón de episodio.</summary>
    public bool HasEpisodes => _entries.Any(e => e.Season > 0);

    /// <summary>Series con al menos un episodio reconocido, en el orden de la lista.</summary>
    public IReadOnlyList<string> ShowNames => _entries
        .Where(e => e.Season > 0)
        .Select(e => e.SeriesName)
        .Distinct(NameComparer)
        .ToList();

    /// <summary>Temporadas presentes en todo el catálogo, de menor a mayor.</summary>
    public IReadOnlyList<int> Seasons => DistinctSeasons(_entries);

    /// <summary>Todo el catálogo agrupado por nombre de serie, en el orden de la lista.</summary>
    public IEnumerable<IGrouping<string, SeriesEpisode>> GroupByShow()
        => _entries.GroupBy(e => e.SeriesName, NameComparer);

    public IReadOnlyList<SeriesEpisode> EpisodesOf(string showName)
        => _entries.Where(e => NameComparer.Equals(e.SeriesName, showName)).ToList();

    public IReadOnlyList<int> SeasonsOf(string showName) => DistinctSeasons(EpisodesOf(showName));

    /// <summary>
    /// Episodios de una serie (o de todo el catálogo si <paramref name="showName"/> es null),
    /// filtrados por temporada si <paramref name="season"/> es mayor que 0, en orden de temporada y episodio.
    /// </summary>
    public IReadOnlyList<SeriesEpisode> GetEpisodes(string? showName, int season)
    {
        IEnumerable<SeriesEpisode> source = _entries;
        if (showName != null)
            source = source.Where(e => NameComparer.Equals(e.SeriesName, showName));
        if (season > 0)
            source = source.Where(e => e.Season == season);
        return source.OrderBy(e => e.Season).ThenBy(e => e.Episode).ToList();
    }

    /// <summary>El episodio que sigue al de <paramref name="currentUrl"/> en la misma serie, o null si es el último.</summary>
    public Channel? GetNextEpisode(string currentUrl)
    {
        var current = _entries.FirstOrDefault(e => e.Channel.Url.Equals(currentUrl, StringComparison.OrdinalIgnoreCase));
        if (current.Channel == null || current.Season <= 0)
            return null;

        var episodes = GetEpisodes(current.SeriesName, season: 0);
        var index = episodes.ToList().FindIndex(e => e.Channel.Url.Equals(currentUrl, StringComparison.OrdinalIgnoreCase));

        return index >= 0 && index < episodes.Count - 1 ? episodes[index + 1].Channel : null;
    }

    /// <summary>El primer logo no vacío de los episodios, o cadena vacía si ninguno tiene.</summary>
    public static string FirstLogo(IEnumerable<SeriesEpisode> episodes)
        => episodes.Select(e => e.Channel.LogoUrl).FirstOrDefault(logo => !string.IsNullOrEmpty(logo)) ?? string.Empty;

    private static List<int> DistinctSeasons(IEnumerable<SeriesEpisode> episodes)
        => episodes.Select(e => e.Season).Where(s => s > 0).Distinct().Order().ToList();
}
