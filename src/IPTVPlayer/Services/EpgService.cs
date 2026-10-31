using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Xml;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

public class EpgService
{
    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    private readonly Dictionary<string, List<EpgProgram>> _programmes = new();
    private string? _loadedUrl;
    private bool _isLoading;

    public bool IsLoaded => _loadedUrl != null;

    public async Task LoadAsync(string epgUrl)
    {
        if (epgUrl == _loadedUrl || _isLoading) return;
        _isLoading = true;

        try
        {
            _programmes.Clear();

            using var response = await HttpClient.GetAsync(epgUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var rawStream = await response.Content.ReadAsStreamAsync();
            Stream parseStream = rawStream;

            if (epgUrl.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
                || response.Content.Headers.ContentEncoding.Any(e =>
                    e.Equals("gzip", StringComparison.OrdinalIgnoreCase)))
            {
                parseStream = new GZipStream(rawStream, CompressionMode.Decompress);
            }

            await Task.Run(() => ParseXmlTv(parseStream));
            _loadedUrl = epgUrl;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ParseXmlTv(Stream stream)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(stream, settings);

        var cutoff = DateTime.Now.AddDays(-1);
        var maxDate = DateTime.Now.AddDays(2);

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

            if (!_programmes.TryGetValue(channelId, out var list))
            {
                list = [];
                _programmes[channelId] = list;
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

        foreach (var kvp in _programmes)
            kvp.Value.Sort((a, b) => a.Start.CompareTo(b.Start));
    }

    public List<EpgProgram> GetProgramsForChannel(string? channelId)
    {
        if (string.IsNullOrEmpty(channelId) || !_programmes.TryGetValue(channelId, out var programs))
            return [];

        var now = DateTime.Now;
        return programs.Where(p => p.Stop > now).Take(24).ToList();
    }

    public EpgProgram? GetCurrentProgram(string? channelId)
    {
        if (string.IsNullOrEmpty(channelId) || !_programmes.TryGetValue(channelId, out var programs))
            return null;

        var now = DateTime.Now;
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

    private static DateTime ParseXmlTvTime(string s)
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
