using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class SeriesCatalogTests
{
    private static Channel Item(string name, string logo = "")
        => new() { Name = name, Url = $"https://streams.example/{Uri.EscapeDataString(name)}", LogoUrl = logo, Category = CategoryType.Series };

    // Desordenado a propósito, como suelen venir las listas.
    private static readonly Channel[] Channels =
    [
        Item("Dark S02E01"),
        Item("Dark S01E02", "https://logos.example/dark.png"),
        Item("Dark S01E01"),
        Item("dark S02E02"),
        Item("Friends 1x01", "https://logos.example/friends.png"),
        Item("Friends 1x02"),
        Item("Documental sin episodios")
    ];

    [Fact]
    public void ShowNames_ListaLasSeriesConEpisodiosSinDistinguirMayusculas()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.True(catalog.HasEpisodes);
        Assert.Equal(["Dark", "Friends"], catalog.ShowNames);
    }

    [Fact]
    public void GroupByShow_IncluyeLosContenidosSinPatronComoSuPropiaSerie()
    {
        var groups = new SeriesCatalog(Channels).GroupByShow().ToList();

        Assert.Equal(["Dark", "Friends", "Documental sin episodios"], groups.Select(g => g.Key));
        Assert.Equal(4, groups[0].Count());
    }

    [Fact]
    public void SeasonsOf_DevuelveLasTemporadasOrdenadas()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.Equal([1, 2], catalog.SeasonsOf("DARK"));
        Assert.Equal([1], catalog.SeasonsOf("Friends"));
        Assert.Empty(catalog.SeasonsOf("Documental sin episodios"));
        Assert.Equal([1, 2], catalog.Seasons);
    }

    [Fact]
    public void GetEpisodes_OrdenaPorTemporadaYEpisodioYFiltraPorTemporada()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.Equal(
            ["Dark S01E01", "Dark S01E02", "Dark S02E01", "dark S02E02"],
            catalog.GetEpisodes("Dark", season: 0).Select(e => e.Channel.Name));
        Assert.Equal(
            ["Dark S02E01", "dark S02E02"],
            catalog.GetEpisodes("Dark", season: 2).Select(e => e.Channel.Name));
        Assert.Equal(7, catalog.GetEpisodes(null, season: 0).Count);
    }

    [Fact]
    public void GetNextEpisode_PasaAlSiguienteEpisodioYDeTemporada()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.Equal("Dark S01E02", catalog.GetNextEpisode(Item("Dark S01E01").Url)?.Name);
        Assert.Equal("Dark S02E01", catalog.GetNextEpisode(Item("Dark S01E02").Url)?.Name);
        Assert.Equal("Friends 1x02", catalog.GetNextEpisode(Item("Friends 1x01").Url)?.Name);
    }

    [Fact]
    public void GetNextEpisode_DevuelveNullAlFinalOFueraDeUnaSerie()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.Null(catalog.GetNextEpisode(Item("dark S02E02").Url));
        Assert.Null(catalog.GetNextEpisode(Item("Documental sin episodios").Url));
        Assert.Null(catalog.GetNextEpisode("https://streams.example/no-existe"));
    }

    [Fact]
    public void FirstLogo_UsaElPrimerLogoDisponible()
    {
        var catalog = new SeriesCatalog(Channels);

        Assert.Equal("https://logos.example/dark.png", SeriesCatalog.FirstLogo(catalog.EpisodesOf("Dark")));
        Assert.Equal(string.Empty, SeriesCatalog.FirstLogo(catalog.EpisodesOf("Documental sin episodios")));
    }

    [Fact]
    public void SinPatronesNoHayEpisodios()
    {
        var catalog = new SeriesCatalog([Item("Película A"), Item("Película B")]);

        Assert.False(catalog.HasEpisodes);
        Assert.Empty(catalog.ShowNames);
    }

    [Fact]
    public void SeriesPendientesDeXtreamCuentanComoSeries()
    {
        var pending = new Channel { Name = "Dark", SeriesId = "301", Category = CategoryType.Series };
        var catalog = new SeriesCatalog([pending, Item("Friends 1x01")]);

        Assert.True(catalog.HasEpisodes);
        Assert.Equal(["Dark", "Friends"], catalog.ShowNames);
        Assert.Same(pending, catalog.PendingSeries("dark"));
        Assert.Null(catalog.PendingSeries("Friends"));
        Assert.Empty(catalog.SeasonsOf("Dark"));
    }
}
