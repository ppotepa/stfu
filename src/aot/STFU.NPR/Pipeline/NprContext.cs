using STFU.Assets;
using STFU.Camera;
using STFU.Engine.Scenes;
using STFU.NPR.Debug;
using STFU.NPR.Analysis;
using STFU.NPR.Graph;
using STFU.NPR.Composition;
using STFU.NPR.Settings;
using STFU.NPR.Temporal;
using STFU.NPR.Rendering;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline;

public sealed class NprContext
{
    public int WorkerCount { get; init; } = 1;

    public WorkerBudgetRequest WorkerBudgetRequest { get; init; } = new();

    public WorkerBudgetMode WorkerBudgetMode { get; init; } = WorkerBudgetMode.Performance;

    public CancellationToken CancellationToken { get; init; }

    public bool IsParallelEnabled => WorkerCount > 1;

    public required Scene Scene { get; init; }

    public required AssetRegistry Assets { get; init; }

    public required CameraState Camera { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required NprSettings Settings { get; init; }

    public required StyleGrammar Style { get; init; }

    public required NprStyleSet StyleSet { get; init; }

    public required NprEntityStyleRegistry EntityStyles { get; init; }

    public required MeshAnalysisCacheStore Analysis { get; init; }

    public required FrameHistoryState FrameHistoryState { get; init; }

    public required int FrameId { get; init; }

    public required float TimeSeconds { get; init; }

    public FrameHistory? PreviousFrame { get; init; }

    public bool IncludeDebugFrame { get; init; } = true;

    public bool EnablePassTimings { get; init; } = true;

    public bool EnableStepAllocationTracking { get; init; }

    public bool EnableDetailedStepNotes { get; init; }

    public bool EnableRangeTimings { get; init; }

    public NprGraph Graph { get; init; } = new();

    public StrokeFrame Frame { get; set; } = StrokeFrame.Empty;

    public NprFrame NprFrame { get; set; } = NprFrame.Empty;

    public NprDebugFrame DebugFrame { get; set; } = NprDebugFrame.Empty;

    public List<NprStepTrace> StepTraces { get; } = [];

    public NprPipelineCounters Counters { get; } = new();

    public List<NprRangeTrace> RangeTraces { get; } = [];

    public NprViewContext View => new(
        Camera,
        ProjectionInfo.Create(Camera, Width, Height, Settings),
        LightContext.Default,
        Settings,
        Style,
        Style.StyleId,
        FrameId,
        TimeSeconds,
        PreviousFrame);
}
