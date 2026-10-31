using System.Text.RegularExpressions;

namespace IPTVPlayer.Services;

/// <summary>
/// Temporada y episodio detectados en el nombre de un contenido.
/// <see cref="Season"/> es 0 cuando el nombre no sigue ningún patrón de episodio.
/// </summary>
public readonly record struct EpisodeInfo(string SeriesName, int Season, int Episode)
{
    public bool IsEpisode => Season > 0;
}

/// <summary>
/// Reconoce episodios en nombres como «Serie S01E02», «Serie T01E02» o «Serie 1x02»
/// y separa el nombre de la serie (lo que va antes del patrón).
/// </summary>
public static partial class EpisodeParser
{
    // En «1x02» no se aceptan dígitos pegados antes ni después, para no leer «1920x1080» como episodio.
    [GeneratedRegex(@"[Ss](\d{1,2})\s*[Ee](\d{1,3})|[Tt](\d{1,2})\s*[Ee](\d{1,3})|(?<!\d)(\d{1,2})[xX](\d{1,3})(?!\d)")]
    private static partial Regex EpisodePattern();

    public static EpisodeInfo Parse(string name)
    {
        var match = EpisodePattern().Match(name);
        if (!match.Success)
            return new EpisodeInfo(name.Trim(), 0, 0);

        var seriesName = name[..match.Index].Trim(' ', '-', '\u2013', '\u2014', ':', '.');
        if (string.IsNullOrWhiteSpace(seriesName))
            seriesName = name.Trim();

        int season = 0, episode = 0;
        for (var i = 1; i <= 5; i += 2)
        {
            if (match.Groups[i].Success)
            {
                season = int.Parse(match.Groups[i].Value);
                episode = int.Parse(match.Groups[i + 1].Value);
                break;
            }
        }
        return new EpisodeInfo(seriesName, season, episode);
    }
}
