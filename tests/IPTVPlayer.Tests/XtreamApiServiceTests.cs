using System.Net;
using System.Text;
using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class XtreamApiServiceTests
{
    private const string Server = "http://panel.example:8080";

    private static readonly PlaylistSource Account = new()
    {
        Name = "Proveedor",
        Type = PlaylistSourceType.Xtream,
        Url = Server + "/",
        Username = "usuario",
        Password = "clave con espacios"
    };

    private const string Auth = """
        { "user_info": { "username": "usuario", "auth": 1, "status": "Active", "allowed_output_formats": ["m3u8", "ts"] },
          "server_info": { "url": "panel.example", "port": "8080" } }
        """;

    private static readonly Dictionary<string, string> Catalog = new()
    {
        [""] = Auth,
        ["get_live_categories"] = """[ { "category_id": "1", "category_name": "Noticias" }, { "category_id": 2, "category_name": "Deportes" } ]""",
        ["get_live_streams"] = """
            [ { "num": 1, "name": "Canal Uno", "stream_id": 101, "stream_icon": "https://logos.example/uno.png", "epg_channel_id": "uno.cl", "category_id": "1" },
              { "num": 2, "name": "Canal Deportes", "stream_id": "102", "stream_icon": "", "epg_channel_id": null, "category_id": 2 },
              { "num": 3, "name": "Sin categoría", "stream_id": 103, "category_id": "99" },
              { "num": 4, "name": "", "stream_id": 104, "category_id": "1" } ]
            """,
        ["get_vod_categories"] = """[ { "category_id": "10", "category_name": "Acción" } ]""",
        ["get_vod_streams"] = """
            [ { "name": "Película antigua", "stream_id": 201, "added": "1600000000", "container_extension": "mkv", "category_id": "10" },
              { "name": "Película nueva", "stream_id": 202, "added": 1700000000, "container_extension": "mp4", "category_id": "10" } ]
            """,
        ["get_series_categories"] = """[ { "category_id": "20", "category_name": "Drama" } ]""",
        ["get_series"] = """[ { "name": "Dark", "series_id": 301, "cover": "https://logos.example/dark.jpg", "category_id": "20", "last_modified": "1650000000" } ]""",
        ["get_series_info"] = """
            { "info": { "name": "Dark" },
              "episodes": {
                "2": [ { "id": "4001", "episode_num": 1, "title": "Dark - S02E01 - Principios y finales", "container_extension": "mkv", "season": 2 } ],
                "1": [ { "id": "3002", "episode_num": "2", "title": "Mentiras", "container_extension": "mp4", "info": { "movie_image": "https://logos.example/s01e02.jpg" } },
                       { "id": "3001", "episode_num": "1", "title": "Dark", "container_extension": "mp4", "season": "1" } ] } }
            """
    };

    private static (XtreamApiService Service, FakeHandler Handler) Create(Dictionary<string, string> responses)
    {
        var handler = new FakeHandler(responses);
        return (new XtreamApiService(Account, new HttpClient(handler)), handler);
    }

    [Fact]
    public async Task LoadCatalogAsync_ArmaTvEnVivoPeliculasYSeries()
    {
        var (service, _) = Create(Catalog);

        var categories = await service.LoadCatalogAsync(TestContext.Current.CancellationToken);

        Assert.Equal([CategoryType.LiveTV, CategoryType.Movies, CategoryType.Series], categories.Select(c => c.Type));
        Assert.Equal(["Deportes", "Noticias", "Sin Grupo"], categories[0].Groups.Select(g => g.Name));
    }

    [Fact]
    public async Task LoadCatalogAsync_ArmaLasUrlsDeReproduccionConLasCredenciales()
    {
        var (service, _) = Create(Catalog);

        var categories = await service.LoadCatalogAsync(TestContext.Current.CancellationToken);

        var live = categories[0].Groups.SelectMany(g => g.Channels).Single(c => c.Name == "Canal Uno");
        Assert.Equal($"{Server}/live/usuario/clave%20con%20espacios/101.ts", live.Url);
        Assert.Equal("uno.cl", live.TvgId);
        Assert.Equal("https://logos.example/uno.png", live.LogoUrl);
        Assert.Equal("Noticias", live.GroupTitle);

        var movie = categories[1].Groups.Single().Channels[0];
        Assert.Equal($"{Server}/movie/usuario/clave%20con%20espacios/202.mp4", movie.Url);
    }

    [Fact]
    public async Task LoadCatalogAsync_OrdenaPeliculasDeLaMasNuevaALaMasAntiguaYOmiteSinNombre()
    {
        var (service, _) = Create(Catalog);

        var categories = await service.LoadCatalogAsync(TestContext.Current.CancellationToken);

        Assert.Equal(["Película nueva", "Película antigua"], categories[1].Groups.Single().Channels.Select(c => c.Name));
        Assert.Equal(3, categories[0].TotalChannels);
    }

    [Fact]
    public async Task LoadCatalogAsync_DejaLasSeriesPendientesDeCargarEpisodios()
    {
        var (service, _) = Create(Catalog);

        var categories = await service.LoadCatalogAsync(TestContext.Current.CancellationToken);

        var series = categories[2].Groups.Single().Channels.Single();
        Assert.Equal("Dark", series.Name);
        Assert.Equal("301", series.SeriesId);
        Assert.True(series.IsPendingSeries);
        Assert.Equal("Drama", series.GroupTitle);
    }

    [Fact]
    public async Task LoadCatalogAsync_UsaHlsSiElPanelNoPermiteTs()
    {
        var responses = new Dictionary<string, string>(Catalog)
        {
            [""] = """{ "user_info": { "auth": "1", "status": "Active", "allowed_output_formats": ["m3u8"] } }"""
        };
        var (service, _) = Create(responses);

        var categories = await service.LoadCatalogAsync(TestContext.Current.CancellationToken);

        Assert.EndsWith("/101.m3u8", categories[0].Groups.SelectMany(g => g.Channels).Single(c => c.Name == "Canal Uno").Url);
    }

    [Theory]
    [InlineData("""{ "user_info": { "auth": 0 } }""", "rechazó")]
    [InlineData("""[]""", "rechazó")]
    [InlineData("""{ "user_info": { "auth": 1, "status": "Expired" } }""", "Expired")]
    public async Task LoadCatalogAsync_CuentaInvalidaLanzaUnErrorClaro(string authResponse, string expected)
    {
        var (service, _) = Create(new Dictionary<string, string>(Catalog) { [""] = authResponse });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.LoadCatalogAsync(TestContext.Current.CancellationToken));

        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public async Task LoadSeriesEpisodesAsync_NombraLosEpisodiosParaLaNavegacionDeSeries()
    {
        var (service, handler) = Create(Catalog);
        var series = new Channel { Name = "Dark", SeriesId = "301", LogoUrl = "https://logos.example/dark.jpg", GroupTitle = "Drama", Category = CategoryType.Series };

        var episodes = await service.LoadSeriesEpisodesAsync(series, TestContext.Current.CancellationToken);

        Assert.Equal(
            ["Dark S01E01", "Dark S01E02 — Mentiras", "Dark S02E01 — Principios y finales"],
            episodes.Select(e => e.Name));
        Assert.Equal($"{Server}/series/usuario/clave%20con%20espacios/3002.mp4", episodes[1].Url);
        Assert.Equal("https://logos.example/s01e02.jpg", episodes[1].LogoUrl);
        Assert.Equal("https://logos.example/dark.jpg", episodes[0].LogoUrl);
        Assert.All(episodes, e => Assert.Equal(CategoryType.Series, e.Category));
        Assert.Contains(handler.Requests, r => r.Contains("action=get_series_info&series_id=301"));

        // Los nombres generados los reconoce la misma navegación que las listas M3U.
        Assert.Equal(new EpisodeInfo("Dark", 2, 1), EpisodeParser.Parse(episodes[2].Name));
    }

    [Fact]
    public async Task LoadSeriesEpisodesAsync_SinEpisodiosDevuelveListaVacia()
    {
        var (service, _) = Create(new Dictionary<string, string>(Catalog) { ["get_series_info"] = "[]" });

        var episodes = await service.LoadSeriesEpisodesAsync(new Channel { Name = "X", SeriesId = "1" }, TestContext.Current.CancellationToken);

        Assert.Empty(episodes);
    }

    [Fact]
    public void EpgUrl_ApuntaAlXmltvDeLaCuenta()
    {
        var service = new XtreamApiService(Account);

        Assert.Equal($"{Server}/xmltv.php?username=usuario&password=clave%20con%20espacios", service.EpgUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("panel.example:8080")]
    [InlineData("ftp://panel.example")]
    public void Constructor_RechazaServidoresInvalidos(string url)
    {
        var source = new PlaylistSource { Type = PlaylistSourceType.Xtream, Url = url };

        Assert.Throws<ArgumentException>(() => new XtreamApiService(source));
    }

    [Theory]
    [InlineData("Dark - S02E01 - Principios y finales", "Dark", "Principios y finales")]
    [InlineData("S01E02: Mentiras", "Dark", "Mentiras")]
    [InlineData("Dark", "Dark", "")]
    [InlineData("Dark: Secretos", "Dark", "Secretos")]
    [InlineData("Episodio 3", "Dark", "Episodio 3")]
    public void EpisodeTitle_QuitaLoQueRepiteSerieYCodigo(string title, string series, string expected)
    {
        Assert.Equal(expected, XtreamApiService.EpisodeTitle(title, series));
    }

    private sealed class FakeHandler(Dictionary<string, string> responses) : HttpMessageHandler
    {
        public List<string> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            lock (Requests)
                Requests.Add(url);

            var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
            var action = query["action"] ?? string.Empty;

            return Task.FromResult(responses.TryGetValue(action, out var json)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
