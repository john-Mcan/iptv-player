using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public sealed class PlaylistServiceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"IPTVPlayer.Tests-{Guid.NewGuid():N}.m3u");

    public void Dispose() => File.Delete(_path);

    private async Task<List<ContentCategory>> LoadAsync(string content, PlaylistService? service = null)
    {
        await File.WriteAllTextAsync(_path, content, TestContext.Current.CancellationToken);
        return await (service ?? new PlaylistService()).LoadPlaylistAsync(_path);
    }

    private static CategoryType CategoryOf(List<ContentCategory> categories, string channelName)
        => categories.Single(c => c.Groups.SelectMany(g => g.Channels).Any(ch => ch.Name == channelName)).Type;

    [Fact]
    public async Task LoadPlaylistAsync_ClasificaPorGrupoYPorUrl()
    {
        var categories = await LoadAsync("""
            #EXTM3U
            #EXTINF:-1 group-title="Noticias",Canal Noticias
            https://streams.example/live/noticias.m3u8
            #EXTINF:-1 group-title="VOD | Acción",Duro de matar
            https://streams.example/duro.mp4
            #EXTINF:-1 group-title="Estrenos",Película por URL
            https://streams.example/movie/123.mkv
            #EXTINF:-1 group-title="Series | Drama",Dark S01E01
            https://streams.example/dark-s01e01.mkv
            #EXTINF:-1 group-title="Varios",Episodio por URL
            https://streams.example/series/456.mkv
            """);

        Assert.Equal(CategoryType.LiveTV, CategoryOf(categories, "Canal Noticias"));
        Assert.Equal(CategoryType.Movies, CategoryOf(categories, "Duro de matar"));
        Assert.Equal(CategoryType.Movies, CategoryOf(categories, "Película por URL"));
        Assert.Equal(CategoryType.Series, CategoryOf(categories, "Dark S01E01"));
        Assert.Equal(CategoryType.Series, CategoryOf(categories, "Episodio por URL"));
    }

    [Fact]
    public async Task LoadPlaylistAsync_Canales247YGruposMixtosSonTvEnVivo()
    {
        var categories = await LoadAsync("""
            #EXTM3U
            #EXTINF:-1 group-title="Series",Los Simpson 24/7
            https://streams.example/simpson
            #EXTINF:-1 group-title="Cine y Series",Canal Mixto
            https://streams.example/mixto
            """);

        var category = Assert.Single(categories);
        Assert.Equal(CategoryType.LiveTV, category.Type);
        Assert.Equal(2, category.TotalChannels);
    }

    [Fact]
    public async Task LoadPlaylistAsync_LimpiaElPrefijoDeCategoriaYAgrupaSinGrupo()
    {
        var categories = await LoadAsync("""
            #EXTM3U
            #EXTINF:-1 group-title="VOD - Comedia",Película A
            https://streams.example/a.mp4
            #EXTINF:-1 group-title="Peliculas: Comedia",Película B
            https://streams.example/b.mp4
            #EXTINF:-1,Canal sin grupo
            https://streams.example/sin-grupo
            """);

        var movies = categories.Single(c => c.Type == CategoryType.Movies);
        var comedy = Assert.Single(movies.Groups);
        Assert.Equal("Comedia", comedy.Name);
        Assert.Equal(2, comedy.Channels.Count);

        var live = categories.Single(c => c.Type == CategoryType.LiveTV);
        Assert.Equal("Sin Grupo", Assert.Single(live.Groups).Name);
    }

    [Fact]
    public async Task LoadPlaylistAsync_OrdenaCategoriasYGrupos()
    {
        var categories = await LoadAsync("""
            #EXTM3U
            #EXTINF:-1 group-title="Series | Ciencia ficción",Dark S01E01
            https://streams.example/dark
            #EXTINF:-1 group-title="Música",Radio Uno
            https://streams.example/radio
            #EXTINF:-1 group-title="Deportes",Deportes Uno
            https://streams.example/deportes
            #EXTINF:-1 group-title="VOD | Terror",Película
            https://streams.example/pelicula.mp4
            """);

        Assert.Equal([CategoryType.LiveTV, CategoryType.Movies, CategoryType.Series], categories.Select(c => c.Type));
        Assert.Equal(["Deportes", "Música"], categories[0].Groups.Select(g => g.Name));
    }

    [Fact]
    public async Task LoadPlaylistAsync_GuardaLaUrlDeLaGuia()
    {
        var service = new PlaylistService();

        await LoadAsync("""
            #EXTM3U url-tvg="https://epg.example/guia.xml.gz"
            #EXTINF:-1,Uno
            https://streams.example/uno
            """, service);

        Assert.Equal("https://epg.example/guia.xml.gz", service.EpgUrl);
    }

    [Theory]
    [InlineData("no-es-una-url")]
    [InlineData("ftp://servidor.example/lista.m3u")]
    public async Task LoadPlaylistAsync_RutaInvalidaLanzaArgumentException(string source)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => new PlaylistService().LoadPlaylistAsync(source));
    }
}
