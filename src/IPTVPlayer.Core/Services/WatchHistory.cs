using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

/// <summary>
/// Historial de reproducción. Alimenta «Vistos recientemente» (TV en vivo) y «Continuar viendo»
/// (películas y series) y guarda hasta <see cref="MaxEntries"/> entradas.
/// </summary>
public sealed class WatchHistory
{
    public const int MaxEntries = 20;

    private readonly List<WatchHistoryEntry> _entries;
    private readonly TimeProvider _timeProvider;

    public WatchHistory(IEnumerable<WatchHistoryEntry>? entries = null, TimeProvider? timeProvider = null)
    {
        _entries = entries?.ToList() ?? [];
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public IReadOnlyList<WatchHistoryEntry> Entries => _entries;

    private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

    /// <summary>
    /// Registra que se empezó a ver <paramref name="channel"/>: actualiza su entrada o crea una nueva,
    /// la deja al inicio y devuelve la entrada para ir guardando la posición.
    /// </summary>
    public WatchHistoryEntry Record(Channel channel)
    {
        var entry = Find(channel.Url);

        if (entry != null)
        {
            entry.Name = channel.Name;
            entry.LogoUrl = channel.LogoUrl;
        }
        else
        {
            entry = new WatchHistoryEntry
            {
                Url = channel.Url,
                Name = channel.Name,
                LogoUrl = channel.LogoUrl,
                Category = channel.Category
            };
        }

        Touch(entry);
        return entry;
    }

    /// <summary>
    /// Marca como vista ahora una entrada (por ejemplo, al retomarla desde el historial) y la deja al
    /// inicio, para que el recorte a <see cref="MaxEntries"/> saque siempre lo menos reciente.
    /// </summary>
    public void Touch(WatchHistoryEntry entry)
    {
        entry.Timestamp = UtcNow;

        _entries.Remove(entry);
        _entries.Insert(0, entry);

        while (_entries.Count > MaxEntries)
            _entries.RemoveAt(_entries.Count - 1);
    }

    public bool Remove(string url)
    {
        var entry = Find(url);
        return entry != null && _entries.Remove(entry);
    }

    public void Clear() => _entries.Clear();

    /// <summary>Lo último visto de una categoría, del más reciente al más antiguo.</summary>
    public IReadOnlyList<WatchHistoryEntry> Recent(CategoryType category, int max)
        => ByMostRecent().Where(h => h.Category == category).Take(max).ToList();

    /// <summary>Lo que quedó a medio ver en una categoría, del más reciente al más antiguo.</summary>
    public IReadOnlyList<WatchHistoryEntry> ContinueWatching(CategoryType category, int max)
        => ByMostRecent().Where(h => h.Category == category && h.HasProgress).Take(max).ToList();

    private IEnumerable<WatchHistoryEntry> ByMostRecent() => _entries.OrderByDescending(h => h.Timestamp);

    private WatchHistoryEntry? Find(string url)
        => _entries.FirstOrDefault(h => h.Url.Equals(url, StringComparison.OrdinalIgnoreCase));
}
