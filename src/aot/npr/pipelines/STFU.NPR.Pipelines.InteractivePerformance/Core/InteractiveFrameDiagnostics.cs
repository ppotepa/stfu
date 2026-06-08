using STFU.NPR.Pipelines.Abstractions;
using STFU.NPR.Pipeline.InteractivePerformance.Scheduling;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveFrameDiagnostics
{
    private readonly Dictionary<string, double> _stageTimingsMs = [];

    public FramePipelineStrategy Strategy { get; set; }
    public InteractiveWorkClass WorkClass { get; set; }

    public double ProjectionMs { get; set; }
    public double VisibilityMs { get; set; }
    public double CandidateMs { get; set; }
    public double StrokePlanMs { get; set; }
    public double TonePlanMs { get; set; }
    public double GpuUploadMs { get; set; }
    public double GpuDrawMs { get; set; }

    public int CacheHits { get; set; }
    public int CacheMisses { get; set; }
    public int ProjectedVertices { get; set; }
    public int ProjectedTriangles { get; set; }
    public int VisibleFaces { get; set; }
    public int CandidateEdges { get; set; }
    public int VisibleSegments { get; set; }
    public int StrokeCommands { get; set; }

    public bool UsedReferenceFallback { get; set; }
    public string FallbackReason { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, double> StageTimingsMs => _stageTimingsMs;

    public void AddStageTiming(string stageName, TimeSpan elapsed)
    {
        if (string.IsNullOrWhiteSpace(stageName))
        {
            return;
        }

        var ms = elapsed.TotalMilliseconds;
        _stageTimingsMs[stageName] = ms;

        switch (stageName)
        {
            case "InteractiveProjection":
                ProjectionMs = ms;
                break;
            case "InteractiveVisibility":
                VisibilityMs = ms;
                break;
            case "InteractiveCandidateEdges":
                CandidateMs = ms;
                break;
            case "InteractiveStrokePlanning":
                StrokePlanMs = ms;
                break;
            case "InteractiveTonePlanning":
                TonePlanMs = ms;
                break;
            case "InteractiveGpuUpload":
                GpuUploadMs = ms;
                break;
            case "InteractiveGpuDraw":
                GpuDrawMs = ms;
                break;
        }
    }
}
