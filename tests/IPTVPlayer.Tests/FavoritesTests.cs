using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class FavoritesTests
{
    [Fact]
    public void Set_MarcaYDesmarcaPorUrlSinDistinguirMayusculas()
    {
        var favorites = new Favorites();

        favorites.Set("https://streams.example/Uno", isFavorite: true);
        Assert.True(favorites.Contains("https://streams.example/uno"));

        favorites.Set("HTTPS://STREAMS.EXAMPLE/UNO", isFavorite: false);
        Assert.False(favorites.Contains("https://streams.example/Uno"));
        Assert.Empty(favorites.Urls);
    }

    [Fact]
    public void ToggleSeries_AlternaYDevuelveElEstadoNuevo()
    {
        var favorites = new Favorites();

        Assert.True(favorites.ToggleSeries("Dark"));
        Assert.True(favorites.ContainsSeries("DARK"));
        Assert.True(favorites.HasSeries);

        Assert.False(favorites.ToggleSeries("dark"));
        Assert.False(favorites.HasSeries);
    }

    [Fact]
    public void Constructor_ParteDeLoGuardadoYClearLoVacia()
    {
        var favorites = new Favorites(["https://streams.example/uno"], ["Dark"]);

        Assert.Equal(["https://streams.example/uno"], favorites.Urls);
        Assert.Equal(["Dark"], favorites.SeriesNames);

        favorites.Clear();

        Assert.Empty(favorites.Urls);
        Assert.Empty(favorites.SeriesNames);
    }
}
