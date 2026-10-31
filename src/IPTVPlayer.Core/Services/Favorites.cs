namespace IPTVPlayer.Services;

/// <summary>
/// Favoritos del usuario: canales y películas se guardan por URL; las series, por nombre, porque
/// cada episodio tiene su propia URL. Ninguno distingue mayúsculas.
/// </summary>
public sealed class Favorites
{
    private readonly HashSet<string> _urls;
    private readonly HashSet<string> _seriesNames;

    public Favorites(IEnumerable<string>? urls = null, IEnumerable<string>? seriesNames = null)
    {
        _urls = new HashSet<string>(urls ?? [], StringComparer.OrdinalIgnoreCase);
        _seriesNames = new HashSet<string>(seriesNames ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public List<string> Urls => _urls.ToList();

    public List<string> SeriesNames => _seriesNames.ToList();

    public bool HasSeries => _seriesNames.Count > 0;

    public bool Contains(string url) => _urls.Contains(url);

    public bool ContainsSeries(string seriesName) => _seriesNames.Contains(seriesName);

    public void Set(string url, bool isFavorite)
    {
        if (isFavorite)
            _urls.Add(url);
        else
            _urls.Remove(url);
    }

    /// <summary>Marca o desmarca una serie y devuelve si quedó como favorita.</summary>
    public bool ToggleSeries(string seriesName)
    {
        if (_seriesNames.Remove(seriesName))
            return false;

        _seriesNames.Add(seriesName);
        return true;
    }

    public void Clear()
    {
        _urls.Clear();
        _seriesNames.Clear();
    }
}
