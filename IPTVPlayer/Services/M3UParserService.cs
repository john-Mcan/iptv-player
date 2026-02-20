using System.Text.RegularExpressions;
using IPTVPlayer.Models;

namespace IPTVPlayer.Services;

public static partial class M3UParserService
{
    [GeneratedRegex(@"([\w-]+)=""([^""]*)""", RegexOptions.Compiled)]
    private static partial Regex AttributeRegex();

    public static List<Channel> Parse(string content)
    {
        var channels = new List<Channel>();
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);

        Channel? pending = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("#EXTINF:", StringComparison.OrdinalIgnoreCase))
            {
                pending = ParseExtInf(line);
            }
            else if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith('#') && pending is not null)
            {
                pending.Url = line.Trim();
                if (!string.IsNullOrEmpty(pending.Url))
                    channels.Add(pending);
                pending = null;
            }
        }

        return channels;
    }

    private static Channel ParseExtInf(string line)
    {
        var channel = new Channel();
        var infoLine = line[8..];

        int commaIdx = FindTitleComma(infoLine);
        if (commaIdx >= 0)
        {
            var metaPart = infoLine[..commaIdx];
            channel.Name = infoLine[(commaIdx + 1)..].Trim();

            foreach (Match m in AttributeRegex().Matches(metaPart))
            {
                var key = m.Groups[1].Value.ToLowerInvariant();
                var val = m.Groups[2].Value;

                switch (key)
                {
                    case "tvg-id": channel.TvgId = val; break;
                    case "tvg-name": channel.TvgName = val; break;
                    case "tvg-logo": channel.LogoUrl = val; break;
                    case "group-title": channel.GroupTitle = val; break;
                }
            }
        }
        else
        {
            channel.Name = infoLine.Trim();
        }

        if (string.IsNullOrEmpty(channel.Name) && !string.IsNullOrEmpty(channel.TvgName))
            channel.Name = channel.TvgName;

        return channel;
    }

    private static int FindTitleComma(string line)
    {
        bool inQuote = false;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') inQuote = !inQuote;
            if (line[i] == ',' && !inQuote) return i;
        }
        return -1;
    }
}
