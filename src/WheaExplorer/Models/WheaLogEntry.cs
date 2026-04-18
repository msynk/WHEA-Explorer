namespace WheaExplorer.Models;

/// <summary>One row from Kernel-WHEA event channels.</summary>
public sealed class WheaLogEntry
{
    public required DateTime? TimeCreated { get; init; }
    public required string Channel { get; init; }
    public required int EventId { get; init; }
    public required string Level { get; init; }
    public required string RawHex { get; init; }
    public required string MachineName { get; init; }

    public string TimeDisplay => TimeCreated?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

    public string ChannelShort =>
        Channel.Contains('/', StringComparison.Ordinal)
            ? Channel[(Channel.LastIndexOf('/') + 1)..]
            : Channel;
}
