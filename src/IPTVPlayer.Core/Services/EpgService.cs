using System.Globalization;
using System.IO.Compression;
using System.Xml;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

/// <summary>
/// Descarga y consulta una guía de programación en formato XMLTV, plana o comprimida con GZip.
/// Guarda en memoria los programas desde ayer hasta dos días adelante, agrupados por canal.
/// </summary>
public class EpgService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private readonly TimeProvider _timeProvider;
    private Dictionary<string, List<EpgProgram>> _programmes = new();
    private string? _loadedUrl;
    private bool _isLoading;

    public EpgService() : this(TimeProvider.System)
    {
    }

    public EpgService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public bool IsLoaded => _loadedUrl != null;

    private DateTime Now => _timeProvider.GetLocalNow().DateTime;

    public async Task LoadAsync(string epgUrl)
    {
        if (epgUrl == _loadedUrl || _isLoading) return;
        _isLoading = true;

        try
        {
            _programmes = new();

            using var response = await HttpClient.GetAsync(epgUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var rawStream = await response.Content.ReadAsStreamAsync();

            var isGzip = epgUrl.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                || response.Content.Headers.ContentEncoding.Any(e =>
                    e.Equals("gzip", StringComparison.OrdinalIgnoreCase));

            await Task.Run(() => Load(rawStream, isGzip));
            _loadedUrl = epgUrl;
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>Reemplaza la guía en memoria por la que se lee de <paramref name="stream"/>.</summary>
    public void Load(Stream stream, bool isGzip)
    {
        using var parseStream = isGzip
            ? new GZipStream(stream, CompressionMode.Decompress, leaveOpen: true)
            : null;

        // Se arma un diccionario nuevo y se reemplaza al final, para que la UI no lea uno a medio llenar.
        _programmes = ParseXmlTv(parseStream ?? stream, Now);
    }

    internal static Dictionary<string, List<EpgProgram>> ParseXmlTv(Stream stream, DateTime now)
    {
        var programmes = new Dictionary<string, List<EpgProgram>>();
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(stream, settings);

        var cutoff = now.AddDays(-1);
        var maxDate = now.AddDays(2);

        while (reader.Read())
        {
            if (reader is not { NodeType: XmlNodeType.Element, Name: "programme" })
                continue;

            var startStr = reader.GetAttribute("start");
            var stopStr = reader.GetAttribute("stop");
            var channelId = reader.GetAttribute("channel");

            if (string.IsNullOrEmpty(startStr) || string.IsNullOrEmpty(channelId))
                continue;

            var start = ParseXmlTvTime(startStr);
            var stop = !string.IsNullOrEmpty(stopStr) ? ParseXmlTvTime(stopStr) : start.AddHours(1);

            if (stop < cutoff || start > maxDate)
            {
                reader.Skip();
                continue;
            }

            string title = "", desc = "";
            var subtree = reader.ReadSubtree();
            while (subtree.Read())
            {
                if (subtree.NodeType != XmlNodeType.Element) continue;
                switch (subtree.Name)
                {
                    case "title":
                        title = subtree.ReadElementContentAsString();
                        break;
                    case "desc":
                        desc = subtree.ReadElementContentAsString();
                        break;
                }
            }

            if (!programmes.TryGetValue(channelId, out var list))
            {
                list = [];
                programmes[channelId] = list;
            }

            list.Add(new EpgProgram
            {
                ChannelId = channelId,
                Title = title,
                Description = desc,
                Start = start,
                Stop = stop
            });
        }

        foreach (var kvp in programmes)
            kvp.Value.Sort((a, b) => a.Start.CompareTo(b.Start));

        return programmes;
    }

    public List<EpgProgram> GetProgramsForChannel(string? channelId)
    {
        if (string.IsNullOrEmpty(channelId) || !_programmes.TryGetValue(channelId, out var programs))
            return [];

        var now = Now;
        return programs.Where(p => p.Stop > now).Take(24).ToList();
    }

    public EpgProgram? GetCurrentProgram(string? channelId)
    {
        if (string.IsNullOrEmpty(channelId) || !_programmes.TryGetValue(channelId, out var programs))
            return null;

        var now = Now;
        return programs.FirstOrDefault(p => p.Start <= now && p.Stop > now);
    }

    public EpgProgram? GetNextProgram(string? channelId)
    {
        var current = GetCurrentProgram(channelId);
        if (current == null || string.IsNullOrEmpty(channelId)
            || !_programmes.TryGetValue(channelId, out var programs))
            return null;

        return programs.FirstOrDefault(p => p.Start >= current.Stop);
    }

    /// <summary>
    /// Convierte una fecha XMLTV («20260131203000 +0100») a hora local.
    /// Sin zona horaria, la fecha se toma tal cual; si no se puede leer, devuelve <see cref="DateTime.MinValue"/>.
    /// </summary>
    internal static DateTime ParseXmlTvTime(string s)
    {
        s = s.Trim();

        if (s.Length >= 19 && (s[15] == '+' || s[15] == '-'))
        {
            var dtPart = s[..14];
            var tzPart = s[15..].Trim();

            if (DateTime.TryParseExact(dtPart, "yyyyMMddHHmmss",
                    CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
                && tzPart.Length >= 4)
            {
                var sign = s[15] == '-' ? -1 : 1;
                var tzDigits = tzPart.TrimStart('+', '-');
                if (int.TryParse(tzDigits[..2], out var h) && int.TryParse(tzDigits[2..4], out var m))
                {
                    var offset = new TimeSpan(sign * h, sign * m, 0);
                    var dto = new DateTimeOffset(dt, offset);
                    return dto.LocalDateTime;
                }

                return dt;
            }
        }

        if (s.Length >= 14 && DateTime.TryParseExact(s[..14], "yyyyMMddHHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var fallback))
            return fallback;

        return DateTime.MinValue;
    }
}
