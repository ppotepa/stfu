namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractivePerformanceRunSample
{
    public long FrameId { get; init; }
    public string Strategy { get; init; } = string.Empty;
    public string Scenario { get; init; } = string.Empty;
    public double TotalMs { get; init; }
    public double ProjectionMs { get; init; }
    public double VisibilityMs { get; init; }
    public double CandidateMs { get; init; }
    public double StrokeMs { get; init; }
    public double ToneMs { get; init; }
    public int CandidateEdges { get; init; }
    public int StrokeCommands { get; init; }
    public int VisibleSegments { get; init; }
    public int ToneRegions { get; init; }
    public int HealthScore { get; init; }
    public bool ReturnedInteractiveFrame { get; init; }
    public bool ReturnedReferenceFallback { get; init; }
    public bool ProjectionBuiltSelfContained { get; init; }
    public bool CandidateEdgesBuiltFromProjectedTriangles { get; init; }
}
