namespace WheaExplorer.HardwareEventAnalysis.ParseSupport;

/// <summary>Host-level switches for parsing hardware event blobs in library context (e.g. GUI).</summary>
public static class AnalysisHost
{
    /// <summary>When true, recoverable errors throw instead of terminating the process.</summary>
    public static bool UseThrowingErrorPath { get; set; }
}
