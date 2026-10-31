using IPTVPlayer.Models;
using IPTVPlayer.Services;

namespace IPTVPlayer.Tests;

public class WatchHistoryTests
{
    private readonly ManualTimeProvider _clock = new(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero));

    private static Channel Item(string name, CategoryType category = CategoryType.Movies)
        => new() { Name = name, Url = $"https://streams.example/{name}", LogoUrl = $"https://logos.example/{name}.png", Category = category };

    private WatchHistory Watch(WatchHistory history, params Channel[] channels)
    {
        foreach (var channel in channels)
        {
            history.Record(channel);
            _clock.Advance(TimeSpan.FromMinutes(1));
        }
        return history;
    }

    [Fact]
    public void Record_AgregaAlInicioConLosDatosDelContenido()
    {
        var history = new WatchHistory(timeProvider: _clock);

        var entry = history.Record(Item("Matrix"));

        Assert.Same(entry, Assert.Single(history.Entries));
        Assert.Equal("https://streams.example/Matrix", entry.Url);
        Assert.Equal("Matrix", entry.Name);
        Assert.Equal("https://logos.example/Matrix.png", entry.LogoUrl);
        Assert.Equal(CategoryType.Movies, entry.Category);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, entry.Timestamp);
    }

    [Fact]
    public void Record_ReutilizaLaEntradaDeLaMismaUrlYConservaElProgreso()
    {
        var history = new WatchHistory(timeProvider: _clock);
        var first = history.Record(Item("Matrix"));
        first.PositionMs = 60_000;
        first.DurationMs = 600_000;
        _clock.Advance(TimeSpan.FromHours(1));

        var renamed = Item("Matrix");
        renamed.Name = "Matrix (remasterizada)";
        renamed.Url = renamed.Url.ToUpperInvariant();
        var second = history.Record(renamed);

        Assert.Same(first, second);
        Assert.Single(history.Entries);
        Assert.Equal("Matrix (remasterizada)", second.Name);
        Assert.Equal(60_000, second.PositionMs);
        Assert.Equal(_clock.GetUtcNow().UtcDateTime, second.Timestamp);
    }

    [Fact]
    public void Record_GuardaComoMaximoVeinteEntradas()
    {
        var history = new WatchHistory(timeProvider: _clock);

        Watch(history, Enumerable.Range(1, 25).Select(i => Item($"Pelicula{i}")).ToArray());

        Assert.Equal(WatchHistory.MaxEntries, history.Entries.Count);
        Assert.Equal("Pelicula25", history.Entries[0].Name);
        Assert.DoesNotContain(history.Entries, e => e.Name == "Pelicula5");
    }

    [Fact]
    public void Recent_DevuelveLoUltimoVistoDeLaCategoria()
    {
        var history = Watch(new WatchHistory(timeProvider: _clock),
            Item("Canal1", CategoryType.LiveTV), Item("Pelicula"), Item("Canal2", CategoryType.LiveTV), Item("Canal3", CategoryType.LiveTV));

        Assert.Equal(["Canal3", "Canal2"], history.Recent(CategoryType.LiveTV, max: 2).Select(e => e.Name));
    }

    [Fact]
    public void ContinueWatching_SoloIncluyeLoQueQuedoAMedias()
    {
        var history = Watch(new WatchHistory(timeProvider: _clock),
            Item("SinEmpezar"), Item("AMedias"), Item("Terminada"), Item("Episodio", CategoryType.Series));
        history.Entries.Single(e => e.Name == "AMedias").PositionMs = 1_000_000;
        history.Entries.Single(e => e.Name == "AMedias").DurationMs = 5_000_000;
        history.Entries.Single(e => e.Name == "Terminada").PositionMs = 4_900_000;
        history.Entries.Single(e => e.Name == "Terminada").DurationMs = 5_000_000;
        history.Entries.Single(e => e.Name == "Episodio").PositionMs = 100_000;
        history.Entries.Single(e => e.Name == "Episodio").DurationMs = 2_000_000;

        Assert.Equal(["AMedias"], history.ContinueWatching(CategoryType.Movies, max: 10).Select(e => e.Name));
        Assert.Equal(["Episodio"], history.ContinueWatching(CategoryType.Series, max: 10).Select(e => e.Name));
    }

    [Fact]
    public void Touch_SubeLaEntradaAlPrimerLugarDeLosRecientes()
    {
        var history = Watch(new WatchHistory(timeProvider: _clock),
            Item("Canal1", CategoryType.LiveTV), Item("Canal2", CategoryType.LiveTV));

        history.Touch(history.Entries.Single(e => e.Name == "Canal1"));

        Assert.Equal("Canal1", history.Recent(CategoryType.LiveTV, max: 1).Single().Name);
    }

    [Fact]
    public void RemoveYClear_QuitanEntradas()
    {
        var history = Watch(new WatchHistory(timeProvider: _clock), Item("A"), Item("B"));

        Assert.True(history.Remove("https://streams.example/a"));
        Assert.False(history.Remove("https://streams.example/no-existe"));
        Assert.Equal(["B"], history.Entries.Select(e => e.Name));

        history.Clear();
        Assert.Empty(history.Entries);
    }

    [Fact]
    public void Constructor_ParteDelHistorialGuardado()
    {
        var saved = new List<WatchHistoryEntry> { new() { Url = "https://streams.example/guardada", Name = "Guardada" } };

        var history = new WatchHistory(saved, _clock);

        Assert.Equal("Guardada", Assert.Single(history.Entries).Name);
        history.Clear();
        Assert.Single(saved);
    }

    private sealed class ManualTimeProvider(DateTimeOffset start) : TimeProvider
    {
        private DateTimeOffset _now = start;

        public override DateTimeOffset GetUtcNow() => _now;

        public void Advance(TimeSpan delta) => _now += delta;
    }
}
