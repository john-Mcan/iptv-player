using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "IPTVPlayer.Tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void SaveYLoad_ConservanLosValores()
    {
        var settings = new AppSettings
        {
            LastPlaylistUrl = "https://listas.example/tv.m3u",
            RecentPlaylistUrls = ["https://listas.example/tv.m3u", "C:\\listas\\local.m3u"],
            Volume = 35,
            IsMuted = true,
            WindowWidth = 1600,
            WindowHeight = 900,
            WindowLeft = -1920,
            WindowTop = 40,
            IsWindowMaximized = true,
            IsFullscreen = true,
            SidebarWidth = 320,
            AutoLoadPlaylist = false,
            ActiveTab = "Series",
            FavoriteUrls = ["https://streams.example/uno"],
            FavoriteSeriesNames = ["Dark"],
            WatchHistory =
            [
                new WatchHistoryEntry
                {
                    Url = "https://streams.example/pelicula",
                    Name = "Película",
                    LogoUrl = "https://logos.example/pelicula.png",
                    PositionMs = 600_000,
                    DurationMs = 5_400_000,
                    Timestamp = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
                    Category = CategoryType.Movies
                }
            ],
            MaxReconnectAttempts = 4,
            HideAdultContent = true
        };

        SettingsService.Save(settings, SettingsPath);
        var loaded = SettingsService.Load(SettingsPath);

        Assert.Equal(settings.LastPlaylistUrl, loaded.LastPlaylistUrl);
        Assert.Equal(settings.RecentPlaylistUrls, loaded.RecentPlaylistUrls);
        Assert.Equal(35, loaded.Volume);
        Assert.True(loaded.IsMuted);
        Assert.Equal((1600d, 900d, -1920d, 40d), (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowLeft, loaded.WindowTop));
        Assert.True(loaded.IsWindowMaximized);
        Assert.True(loaded.IsFullscreen);
        Assert.Equal(320, loaded.SidebarWidth);
        Assert.False(loaded.AutoLoadPlaylist);
        Assert.Equal("Series", loaded.ActiveTab);
        Assert.Equal(settings.FavoriteUrls, loaded.FavoriteUrls);
        Assert.Equal(settings.FavoriteSeriesNames, loaded.FavoriteSeriesNames);
        Assert.Equal(4, loaded.MaxReconnectAttempts);
        Assert.True(loaded.HideAdultContent);

        var entry = Assert.Single(loaded.WatchHistory);
        Assert.Equal("https://streams.example/pelicula", entry.Url);
        Assert.Equal("Película", entry.Name);
        Assert.Equal(600_000, entry.PositionMs);
        Assert.Equal(5_400_000, entry.DurationMs);
        Assert.Equal(settings.WatchHistory[0].Timestamp, entry.Timestamp);
        Assert.Equal(CategoryType.Movies, entry.Category);
    }

    [Fact]
    public void SaveYLoad_ConservanLaConfiguracionPorDefecto()
    {
        // Una instalación nueva no tiene posición de ventana: WindowLeft y WindowTop valen NaN.
        SettingsService.Save(new AppSettings(), SettingsPath);

        Assert.True(File.Exists(SettingsPath));
        var loaded = SettingsService.Load(SettingsPath);
        Assert.True(double.IsNaN(loaded.WindowLeft));
        Assert.True(double.IsNaN(loaded.WindowTop));
        Assert.Equal(80, loaded.Volume);
    }

    [Fact]
    public void Save_CreaLaCarpetaSiNoExiste()
    {
        var nested = Path.Combine(_dir, "sub", "carpeta", "settings.json");

        SettingsService.Save(new AppSettings { Volume = 10 }, nested);

        Assert.Equal(10, SettingsService.Load(nested).Volume);
    }

    [Fact]
    public void Load_SinArchivoDevuelveValoresPorDefecto()
    {
        var loaded = SettingsService.Load(SettingsPath);

        Assert.Equal(80, loaded.Volume);
        Assert.True(loaded.AutoLoadPlaylist);
        Assert.Equal(10, loaded.MaxReconnectAttempts);
        Assert.Empty(loaded.WatchHistory);
    }

    [Theory]
    [InlineData("{ esto no es json")]
    [InlineData("")]
    [InlineData("null")]
    public void Load_ArchivoDanadoDevuelveValoresPorDefecto(string content)
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, content);

        var loaded = SettingsService.Load(SettingsPath);

        Assert.Equal(80, loaded.Volume);
    }

    [Fact]
    public void Load_ArchivoDeUnaVersionAnteriorCompletaLoQueFalta()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """{ "LastPlaylistUrl": "https://listas.example/vieja.m3u", "Volume": 55 }""");

        var loaded = SettingsService.Load(SettingsPath);

        Assert.Equal("https://listas.example/vieja.m3u", loaded.LastPlaylistUrl);
        Assert.Equal(55, loaded.Volume);
        Assert.Equal(10, loaded.MaxReconnectAttempts);
        Assert.Empty(loaded.FavoriteSeriesNames);
    }
}
