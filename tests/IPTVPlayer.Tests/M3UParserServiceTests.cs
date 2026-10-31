using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class M3UParserServiceTests
{
    [Fact]
    public void Parse_LeeLosAtributosTvgYElGrupo()
    {
        const string content = """
            #EXTM3U
            #EXTINF:-1 tvg-id="canal1.cl" tvg-name="Canal Uno" tvg-logo="https://logos.example/uno.png" group-title="Noticias",Canal Uno HD
            https://streams.example/uno.m3u8
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("Canal Uno HD", channel.Name);
        Assert.Equal("https://streams.example/uno.m3u8", channel.Url);
        Assert.Equal("canal1.cl", channel.TvgId);
        Assert.Equal("Canal Uno", channel.TvgName);
        Assert.Equal("https://logos.example/uno.png", channel.LogoUrl);
        Assert.Equal("Noticias", channel.GroupTitle);
    }

    [Fact]
    public void Parse_IgnoraMayusculasEnLosNombresDeAtributo()
    {
        const string content = """
            #EXTM3U
            #EXTINF:-1 TVG-ID="uno" Group-Title="Deportes",Uno
            https://streams.example/uno
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("uno", channel.TvgId);
        Assert.Equal("Deportes", channel.GroupTitle);
    }

    [Fact]
    public void Parse_NoCortaElNombreEnUnaComaDentroDeUnAtributo()
    {
        const string content = """
            #EXTM3U
            #EXTINF:-1 group-title="Cine, series y más",Canal Mixto
            https://streams.example/mixto
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("Cine, series y más", channel.GroupTitle);
        Assert.Equal("Canal Mixto", channel.Name);
    }

    [Fact]
    public void Parse_UsaTvgNameSiElTituloVieneVacio()
    {
        const string content = """
            #EXTINF:-1 tvg-name="Canal Respaldo",
            https://streams.example/respaldo
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("Canal Respaldo", channel.Name);
    }

    [Fact]
    public void Parse_AceptaFinesDeLineaWindows()
    {
        var content = "#EXTM3U\r\n#EXTINF:-1,Uno\r\nhttps://streams.example/uno\r\n#EXTINF:-1,Dos\r\nhttps://streams.example/dos\r\n";

        var channels = M3UParserService.Parse(content);

        Assert.Equal(["Uno", "Dos"], channels.Select(c => c.Name));
        Assert.Equal("https://streams.example/uno", channels[0].Url);
    }

    [Fact]
    public void Parse_IgnoraDirectivasEntreExtinfYUrl()
    {
        const string content = """
            #EXTM3U
            #EXTINF:-1,Canal con opciones
            #EXTVLCOPT:http-user-agent=Mozilla/5.0
            #EXTGRP:Otros

            https://streams.example/opciones
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("Canal con opciones", channel.Name);
        Assert.Equal("https://streams.example/opciones", channel.Url);
    }

    [Fact]
    public void Parse_DescartaExtinfSinUrlYUrlSinExtinf()
    {
        const string content = """
            #EXTM3U
            https://streams.example/huerfana
            #EXTINF:-1,Sin URL
            #EXTINF:-1,Con URL
            https://streams.example/con-url
            """;

        var channel = Assert.Single(M3UParserService.Parse(content));

        Assert.Equal("Con URL", channel.Name);
    }

    [Fact]
    public void Parse_ExtinfSinComaNoRompeLaLectura()
    {
        const string content = """
            #EXTM3U
            #EXTINF:-1 Canal sin coma
            https://streams.example/sin-coma
            #EXTINF:-1,Siguiente
            https://streams.example/siguiente
            """;

        var channels = M3UParserService.Parse(content);

        Assert.Equal(2, channels.Count);
        Assert.Equal("https://streams.example/sin-coma", channels[0].Url);
        Assert.False(string.IsNullOrWhiteSpace(channels[0].Name));
        Assert.Equal("Siguiente", channels[1].Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("#EXTM3U")]
    [InlineData("esto no es una lista")]
    public void Parse_SinCanalesDevuelveListaVacia(string content)
    {
        Assert.Empty(M3UParserService.Parse(content));
    }

    [Theory]
    [InlineData("#EXTM3U url-tvg=\"https://epg.example/guia.xml.gz\"", "https://epg.example/guia.xml.gz")]
    [InlineData("#EXTM3U x-tvg-url=\"https://epg.example/guia.xml\"", "https://epg.example/guia.xml")]
    [InlineData("#EXTM3U URL-TVG=\"https://epg.example/mayusculas.xml\"", "https://epg.example/mayusculas.xml")]
    [InlineData("#EXTM3U tvg-shift=\"0\" url-tvg=\" https://epg.example/espacios.xml \"", "https://epg.example/espacios.xml")]
    public void ParseEpgUrl_LeeLaGuiaDeLaCabecera(string header, string expected)
    {
        var content = header + "\n#EXTINF:-1,Uno\nhttps://streams.example/uno";

        Assert.Equal(expected, M3UParserService.ParseEpgUrl(content));
    }

    [Theory]
    [InlineData("#EXTM3U\n#EXTINF:-1 url-tvg=\"https://epg.example/no.xml\",Uno\nhttps://streams.example/uno")]
    [InlineData("#EXTM3U url-tvg=\"\"\n")]
    [InlineData("#EXTINF:-1,Sin cabecera\nhttps://streams.example/uno")]
    public void ParseEpgUrl_SinGuiaEnLaCabeceraDevuelveNull(string content)
    {
        Assert.Null(M3UParserService.ParseEpgUrl(content));
    }
}
