using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Xml.Linq;
using WheaExplorer.Models;

namespace WheaExplorer.Services;

public sealed class WheaEventLogService
{
    private static readonly string[] Channels =
    [
        "Microsoft-Windows-Kernel-WHEA/Operational",
        "Microsoft-Windows-Kernel-WHEA/Errors"
    ];

    private static readonly XNamespace EventNs = "http://schemas.microsoft.com/win/2004/08/events/event";

    /// <summary>Reads all events from both Kernel-WHEA channels (newest first after sort).</summary>
    public IReadOnlyList<WheaLogEntry> LoadEntries(CancellationToken cancellationToken = default)
    {
        var list = new List<WheaLogEntry>();

        foreach (var channel in Channels)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var query = new EventLogQuery(channel, PathType.LogName);
                using var reader = new EventLogReader(query);
                EventRecord? record;
                while ((record = reader.ReadEvent()) != null)
                {
                    using (record)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var raw = TryExtractRawHex(record);
                        if (raw is null)
                            continue;

                        list.Add(new WheaLogEntry
                        {
                            TimeCreated = record.TimeCreated,
                            Channel = channel,
                            EventId = record.Id,
                            Level = record.LevelDisplayName ?? "Unknown",
                            RawHex = raw,
                            MachineName = record.MachineName ?? ""
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Channel missing or no permission — skip and continue with other channel
            }
        }

        list.Sort((a, b) =>
        {
            var ta = a.TimeCreated?.Ticks ?? 0;
            var tb = b.TimeCreated?.Ticks ?? 0;
            return tb.CompareTo(ta);
        });

        return list;
    }

    private static string? TryExtractRawHex(EventRecord record)
    {
        try
        {
            var xml = record.ToXml();
            var doc = XDocument.Parse(xml);
            var rawData = doc.Descendants(EventNs + "Data")
                .FirstOrDefault(e => string.Equals((string?)e.Attribute("Name"), "RawData", StringComparison.OrdinalIgnoreCase));
            var preferred = rawData?.Value?.Trim();
            if (!string.IsNullOrEmpty(preferred))
            {
                var n = NormalizeHexSeparators(preferred);
                if (n.Length >= 8 && n.Length % 2 == 0 && IsAllHex(n))
                    return n;
            }

            foreach (var data in doc.Descendants(EventNs + "Data"))
            {
                var v = data.Value?.Trim();
                if (string.IsNullOrEmpty(v))
                    continue;
                var n = NormalizeHexSeparators(v);
                if (n.Length >= 8 && n.Length % 2 == 0 && IsAllHex(n))
                    return n;
            }
        }
        catch
        {
            // Malformed XML
        }

        return null;
    }

    private static string NormalizeHexSeparators(string s)
    {
        var t = s.Trim();
        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            t = t[2..];

        var sb = new StringBuilder(t.Length);
        foreach (var c in t)
        {
            if (c is ' ' or '\t' or '\r' or '\n' or '-' or ':' or ',')
                continue;
            sb.Append(c);
        }

        return sb.ToString();
    }

    private static bool IsAllHex(string s)
    {
        foreach (var c in s)
        {
            if (c is (>= '0' and <= '9') or (>= 'A' and <= 'F') or (>= 'a' and <= 'f'))
                continue;
            return false;
        }

        return true;
    }
}
