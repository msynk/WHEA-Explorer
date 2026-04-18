using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using WheaExplorer.HardwareEventAnalysis.FaultRecords;
using WheaExplorer.HardwareEventAnalysis.KernelTraceEntries;
using WheaExplorer.HardwareEventAnalysis.ParseSupport;
using static WheaExplorer.HardwareEventAnalysis.ParseSupport.ParseHelpers;

namespace WheaExplorer.Services;

/// <summary>Parses raw Kernel hardware event blobs into JSON (in-process).</summary>
public static class WheaRecordDecodeService
{
    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore
    };

    public static string Decode(string rawInput)
    {
        ArgumentNullException.ThrowIfNull(rawInput);
        var bytes = ParseToBytes(rawInput);
        if (bytes.Length < 4)
            throw new InvalidOperationException("Record is too short (need at least 4 bytes for signature).");

        var prev = AnalysisHost.UseThrowingErrorPath;
        AnalysisHost.UseThrowingErrorPath = true;
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        try
        {
            var addr = handle.AddrOfPinnedObject();
            var sigBytes = new[] { bytes[0], bytes[1], bytes[2], bytes[3] };
            var signature = Encoding.ASCII.GetString(sigBytes);

            return signature switch
            {
                WHEA_EVENT_LOG_ENTRY_HEADER.WHEA_ERROR_LOG_ENTRY_SIGNATURE => JsonConvert.SerializeObject(
                    new WHEA_EVENT_LOG_ENTRY(addr, (uint)bytes.Length), JsonSettings),
                WHEA_ERROR_RECORD_HEADER.WHEA_ERROR_RECORD_SIGNATURE => JsonConvert.SerializeObject(
                    new WHEA_ERROR_RECORD(addr, (uint)bytes.Length), JsonSettings),
                _ => throw new InvalidOperationException(
                    $"Unknown hardware event signature: \"{signature}\". Expected a kernel event envelope or a CPER fault record.")
            };
        }
        finally
        {
            handle.Free();
            AnalysisHost.UseThrowingErrorPath = prev;
        }
    }

    private static byte[] ParseToBytes(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input is empty.", nameof(input));

        var hexCandidate = NormalizeHexSeparators(input);
        if (hexCandidate.Length >= 8 && hexCandidate.Length % 2 == 0 && IsAllHex(hexCandidate))
            return ConvertHexToBytes(hexCandidate);

        try
        {
            var compact = input.Replace("\r", "", StringComparison.Ordinal)
                .Replace("\n", "", StringComparison.Ordinal)
                .Replace(" ", "", StringComparison.Ordinal)
                .Replace("\t", "", StringComparison.Ordinal);
            var fromB64 = Convert.FromBase64String(compact);
            if (fromB64.Length >= 4)
                return fromB64;
        }
        catch (FormatException)
        {
            // Fall through
        }

        throw new ArgumentException(
            "Input is not valid raw hardware event data. Provide an even-length hexadecimal string (as in Event Viewer RawData) or Base64.");
    }

    private static byte[] ConvertHexToBytes(string hex)
    {
        if (hex.Length % 2 != 0)
            throw new ArgumentException("Hexadecimal string has an odd number of characters.", nameof(hex));

        var bytes = new byte[hex.Length / 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            var hexByte = hex.AsSpan(i * 2, 2);
            if (!byte.TryParse(hexByte, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out bytes[i]))
                throw new ArgumentException("Hexadecimal string contains invalid characters.", nameof(hex));
        }

        return bytes;
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
