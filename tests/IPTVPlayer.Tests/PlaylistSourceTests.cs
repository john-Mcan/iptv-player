using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public sealed class PlaylistSourceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "IPTVPlayer.Tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Theory]
    [InlineData("https://www.listas.example/tv.m3u", "listas.example")]
    [InlineData("http://servidor.example:8080/get.php?username=u&password=p", "servidor.example")]
    [InlineData(@"C:\Listas\Mi lista.m3u8", "Mi lista")]
    public void SuggestName_UsaElDominioOElNombreDelArchivo(string source, string expected)
    {
        Assert.Equal(expected, PlaylistSource.SuggestName(source));
    }

    [Fact]
    public void Load_ConvierteLasUrlsAnterioresEnFuentesM3U()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(SettingsPath, """
            {
              "LastPlaylistUrl": "https://listas.example/tv.m3u",
              "RecentPlaylistUrls": [ "https://listas.example/tv.m3u", "https://listas.example/cine.m3u", "C:\\listas\\local.m3u" ]
            }
            """);

        var settings = SettingsService.Load(SettingsPath);

        Assert.Equal(
            ["listas.example", "listas.example (2)", "local"],
            settings.Sources.Select(s => s.Name));
        Assert.All(settings.Sources, s => Assert.Equal(PlaylistSourceType.M3U, s.Type));
        Assert.Equal("https://listas.example/cine.m3u", settings.Sources[1].Url);

        var active = settings.Sources.Single(s => s.Id == settings.ActiveSourceId);
        Assert.Equal("https://listas.example/tv.m3u", active.Url);
    }

    [Fact]
    public void Load_NoMigraSiYaHayFuentes()
    {
        var saved = new AppSettings
        {
            Sources = [new PlaylistSource { Name = "Mía", Url = "https://listas.example/mia.m3u" }],
            LastPlaylistUrl = "https://listas.example/vieja.m3u"
        };
        SettingsService.Save(saved, SettingsPath);

        var loaded = SettingsService.Load(SettingsPath);

        Assert.Equal("Mía", Assert.Single(loaded.Sources).Name);
    }

    [Fact]
    public void Load_SinUrlsAnterioresNoCreaFuentes()
    {
        SettingsService.Save(new AppSettings(), SettingsPath);

        var loaded = SettingsService.Load(SettingsPath);

        Assert.Empty(loaded.Sources);
        Assert.Equal(string.Empty, loaded.ActiveSourceId);
    }

    [Fact]
    public void SaveYLoad_GuardanLaContrasenaCifrada()
    {
        var source = new PlaylistSource
        {
            Name = "Proveedor",
            Type = PlaylistSourceType.Xtream,
            Url = "http://servidor.example:8080",
            Username = "usuario",
            Password = "secreto-123"
        };

        SettingsService.Save(new AppSettings { Sources = [source], ActiveSourceId = source.Id }, SettingsPath);

        Assert.DoesNotContain("secreto-123", File.ReadAllText(SettingsPath));

        var loaded = Assert.Single(SettingsService.Load(SettingsPath).Sources);
        Assert.Equal(source.Id, loaded.Id);
        Assert.Equal(PlaylistSourceType.Xtream, loaded.Type);
        Assert.Equal("usuario", loaded.Username);
        Assert.Equal("secreto-123", loaded.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("esto-no-es-base64")]
    [InlineData("AAAA")]
    public void Unprotect_DatoInvalidoDevuelveVacio(string value)
    {
        Assert.Equal(string.Empty, SecretProtector.Unprotect(value));
    }
}
