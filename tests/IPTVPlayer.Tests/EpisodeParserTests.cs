using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class EpisodeParserTests
{
    [Theory]
    [InlineData("Breaking Bad S01E02", "Breaking Bad", 1, 2)]
    [InlineData("Breaking Bad s1e2", "Breaking Bad", 1, 2)]
    [InlineData("The Office S02 E10", "The Office", 2, 10)]
    [InlineData("La Casa de Papel T02E05", "La Casa de Papel", 2, 5)]
    [InlineData("31 Minutos t1e3", "31 Minutos", 1, 3)]
    [InlineData("Friends 3x07", "Friends", 3, 7)]
    [InlineData("Los Simpson 12X021", "Los Simpson", 12, 21)]
    public void Parse_ReconoceLosFormatosDeEpisodio(string name, string series, int season, int episode)
    {
        var info = EpisodeParser.Parse(name);

        Assert.Equal(new EpisodeInfo(series, season, episode), info);
        Assert.True(info.IsEpisode);
    }

    [Theory]
    [InlineData("Dark - S03E08", "Dark")]
    [InlineData("Dark – S03E08", "Dark")]
    [InlineData("Serie: S01E01", "Serie")]
    [InlineData("  Serie con espacios.  S01E01 — Piloto", "Serie con espacios")]
    public void Parse_LimpiaSeparadoresAlFinalDelNombreDeLaSerie(string name, string series)
    {
        Assert.Equal(series, EpisodeParser.Parse(name).SeriesName);
    }

    [Fact]
    public void Parse_SinNombreAntesDelPatronUsaElNombreCompleto()
    {
        var info = EpisodeParser.Parse(" S01E01 ");

        Assert.Equal(new EpisodeInfo("S01E01", 1, 1), info);
    }

    [Theory]
    [InlineData("Matrix (1999)")]
    [InlineData("Canal 24 Horas")]
    [InlineData("Película 4K HDR")]
    [InlineData("Seinfeld")]
    [InlineData("Temporada completa")]
    public void Parse_NoMarcaComoEpisodioLoQueNoLoEs(string name)
    {
        var info = EpisodeParser.Parse(name);

        Assert.False(info.IsEpisode);
        Assert.Equal(new EpisodeInfo(name.Trim(), 0, 0), info);
    }
}
