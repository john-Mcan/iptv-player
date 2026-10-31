using System.Text;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class EpgServiceTests
{
    // «Ahora» para todos los tests; coincide con el comentario de TestData/guia.xml.
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 11, 15, 0, TimeSpan.Zero);

    private static DateTime Utc(int day, int hour, int minute = 0)
        => new DateTimeOffset(2026, 1, day, hour, minute, 0, TimeSpan.Zero).LocalDateTime;

    private static Stream OpenTestData(string fileName)
        => File.OpenRead(Path.Combine(AppContext.BaseDirectory, "TestData", fileName));

    private static EpgService LoadGuide(string fileName, bool isGzip)
    {
        var service = new EpgService(new FixedTimeProvider(Now));
        using var stream = OpenTestData(fileName);
        service.Load(stream, isGzip);
        return service;
    }

    [Theory]
    [InlineData("guia.xml", false)]
    [InlineData("guia.xml.gz", true)]
    public void Load_LeeLaGuiaPlanaYComprimida(string fileName, bool isGzip)
    {
        var epg = LoadGuide(fileName, isGzip);

        var programs = epg.GetProgramsForChannel("canal1.cl");

        Assert.Equal(["Película de la mañana", "Deportes", "Sin hora de término"], programs.Select(p => p.Title));
    }

    [Fact]
    public void ParseXmlTv_OrdenaPorInicioYDescartaLoQueQuedaFueraDeLaVentana()
    {
        using var stream = OpenTestData("guia.xml");

        var guide = EpgService.ParseXmlTv(stream, Now.LocalDateTime);

        Assert.Equal(["canal1.cl", "canal2.cl"], guide.Keys.Order());
        Assert.Equal(
            ["Noticias", "Película de la mañana", "Deportes", "Sin hora de término"],
            guide["canal1.cl"].Select(p => p.Title));
    }

    [Fact]
    public void ParseXmlTv_LeeHorasDescripcionYZonaHoraria()
    {
        using var stream = OpenTestData("guia.xml");

        var guide = EpgService.ParseXmlTv(stream, Now.LocalDateTime);

        var movie = guide["canal1.cl"].Single(p => p.Title == "Película de la mañana");
        Assert.Equal(Utc(15, 11), movie.Start);
        Assert.Equal(Utc(15, 12, 30), movie.Stop);
        Assert.Equal("Una película para empezar el día.", movie.Description);
        Assert.Equal("canal1.cl", movie.ChannelId);

        var noStop = guide["canal1.cl"].Single(p => p.Title == "Sin hora de término");
        Assert.Equal(Utc(15, 14), noStop.Stop);

        var morning = Assert.Single(guide["canal2.cl"]);
        Assert.Equal(Utc(15, 11), morning.Start);
        Assert.Equal("Conversación & actualidad.", morning.Description);
    }

    [Fact]
    public void GetCurrentAndNextProgram_DevuelvenLoQueEstaAlAireYLoQueSigue()
    {
        var epg = LoadGuide("guia.xml", isGzip: false);

        Assert.Equal("Película de la mañana", epg.GetCurrentProgram("canal1.cl")?.Title);
        Assert.Equal("Deportes", epg.GetNextProgram("canal1.cl")?.Title);

        Assert.Equal("Matinal", epg.GetCurrentProgram("canal2.cl")?.Title);
        Assert.Null(epg.GetNextProgram("canal2.cl"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("canal-inexistente")]
    public void Consultas_SinDatosDelCanalNoFallan(string? channelId)
    {
        var epg = LoadGuide("guia.xml", isGzip: false);

        Assert.Empty(epg.GetProgramsForChannel(channelId));
        Assert.Null(epg.GetCurrentProgram(channelId));
        Assert.Null(epg.GetNextProgram(channelId));
    }

    [Fact]
    public void Load_ReemplazaLaGuiaAnterior()
    {
        var epg = LoadGuide("guia.xml", isGzip: false);

        using var empty = new MemoryStream(Encoding.UTF8.GetBytes("<tv></tv>"));
        epg.Load(empty, isGzip: false);

        Assert.Empty(epg.GetProgramsForChannel("canal1.cl"));
    }

    [Fact]
    public void ParseXmlTvTime_AplicaLaZonaHoraria()
    {
        var expected = new DateTimeOffset(2026, 1, 15, 10, 30, 0, TimeSpan.FromHours(1)).LocalDateTime;

        Assert.Equal(expected, EpgService.ParseXmlTvTime("20260115103000 +0100"));
        Assert.Equal(expected, EpgService.ParseXmlTvTime("  20260115103000 +0100  "));
    }

    [Fact]
    public void ParseXmlTvTime_AplicaZonasNegativasConMinutos()
    {
        var expected = new DateTimeOffset(2026, 1, 15, 10, 30, 0, new TimeSpan(-3, -30, 0)).LocalDateTime;

        Assert.Equal(expected, EpgService.ParseXmlTvTime("20260115103000 -0330"));
    }

    [Fact]
    public void ParseXmlTvTime_SinZonaHorariaDejaLaHoraTalCual()
    {
        Assert.Equal(new DateTime(2026, 1, 15, 10, 30, 0), EpgService.ParseXmlTvTime("20260115103000"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("mañana a las diez")]
    [InlineData("2026-01-15")]
    public void ParseXmlTvTime_FechaIlegibleDevuelveMinValue(string value)
    {
        Assert.Equal(DateTime.MinValue, EpgService.ParseXmlTvTime(value));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Local;
    }
}
