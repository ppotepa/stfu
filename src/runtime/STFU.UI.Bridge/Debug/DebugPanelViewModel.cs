using System.Collections.ObjectModel;
using STFU.NPR.Debug;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Debug;

public sealed class DebugPanelViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private string _graphHash = "graph 000000";
    private string _determinismLabel = "deterministic";
    private string _determinismStatus = "stable";

    public DebugPanelViewModel(UiEngineSession session)
    {
        _session = session;
        OverlayOptions = new ObservableCollection<DebugOverlayKind>(
            Enum.GetValues<DebugOverlayKind>());
        RefreshFromEngine();
    }

    public ObservableCollection<MetricItem> Counters { get; } = [];

    public ObservableCollection<MetricItem> Parity { get; } = [];

    public ObservableCollection<PipelineTraceItem> Trace { get; } = [];

    public ObservableCollection<DebugOverlayKind> OverlayOptions { get; }

    public string GraphHash
    {
        get => _graphHash;
        private set => SetProperty(ref _graphHash, value);
    }

    public string DeterminismLabel
    {
        get => _determinismLabel;
        private set => SetProperty(ref _determinismLabel, value);
    }

    public string DeterminismStatus
    {
        get => _determinismStatus;
        private set => SetProperty(ref _determinismStatus, value);
    }

    public DebugOverlayKind SelectedOverlay
    {
        get => _session.Workspace.Viewport.DebugOverlay;
        set => _session.Workspace.Viewport.DebugOverlay = value;
    }

    public void RefreshFromEngine()
    {
        var frame = _session.Debug.CurrentFrame;
        var strokeCount = _session.Strokes.CurrentFrame.Paths.Count;
        var nprFrame = _session.NprFrames.CurrentFrame;
        var meshEntries = _session.Assets.MeshEntries.ToArray();
        var vertexCount = meshEntries.Sum(entry => entry.Mesh.Vertices.Count);
        var triangleCount = meshEntries.Sum(entry => entry.Mesh.Triangles.Count);
        var toneCount = nprFrame.Layers.Sum(layer => layer.Tones.Count);
        var shadingCount = nprFrame.Layers.Sum(layer => layer.Shading.Count);
        var layerStrokeCount = nprFrame.Layers.Sum(layer => layer.Strokes.Count);
        GraphHash = $"graph {ComputeHash(frame, strokeCount, vertexCount, triangleCount, toneCount, shadingCount, layerStrokeCount)}";

        Counters.Clear();
        Counters.Add(new("meshes", meshEntries.Length.ToString()));
        Counters.Add(new("vertices", vertexCount.ToString()));
        Counters.Add(new("triangles", triangleCount.ToString()));
        Counters.Add(new("feature curves", frame.Counters.FeatureCurveCount.ToString()));
        Counters.Add(new("visible segments", frame.Counters.VisibleSegmentCount.ToString()));
        Counters.Add(new("hidden segments", frame.Counters.HiddenSegmentCount.ToString()));
        Counters.Add(new("salient segments", frame.Counters.SalientSegmentCount.ToString()));
        Counters.Add(new("stroke candidates", frame.Counters.StrokeCandidateCount.ToString()));
        Counters.Add(new("final strokes", strokeCount.ToString()));
        Counters.Add(new("ghost strokes", frame.Counters.GhostStrokeCount.ToString()));
        Counters.Add(new("npr layers", nprFrame.Layers.Count.ToString()));
        Counters.Add(new("tones", toneCount.ToString()));
        Counters.Add(new("shading paths", shadingCount.ToString()));

        Parity.Clear();
        Parity.Add(new("Pipeline", _session.ActivePreset.ActivePreset.PipelineId));
        Parity.Add(new("Seed", _session.ActivePreset.ActiveSettings.Seed.ToString()));
        Parity.Add(new("Status", DeterminismStatus));
        Parity.Add(new("Graph hash", GraphHash.Replace("graph ", string.Empty, StringComparison.Ordinal)));
        Parity.Add(new("Frame output", $"{strokeCount} StrokePath2D"));
        Parity.Add(new("Layer output", $"{nprFrame.Layers.Count} layers / {toneCount} tones / {layerStrokeCount} strokes"));
        Parity.Add(new("Trace steps", frame.StepTraces.Count.ToString()));

        Trace.Clear();
        foreach (var trace in frame.StepTraces)
        {
            Trace.Add(new PipelineTraceItem(
                trace.StepName,
                $"{trace.InputCount} -> {trace.OutputCount}",
                $"{trace.Milliseconds:0.00} ms"));
        }
    }

    public void SetDeterministic(bool stableRandom)
    {
        DeterminismLabel = stableRandom ? "deterministic" : "live random";
        DeterminismStatus = stableRandom ? "stable" : "unstable";
        RefreshFromEngine();
    }

    private static string ComputeHash(
        NprDebugFrame frame,
        int strokeCount,
        int vertexCount,
        int triangleCount,
        int toneCount,
        int shadingCount,
        int layerStrokeCount)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + vertexCount;
            hash = hash * 31 + triangleCount;
            hash = hash * 31 + frame.Counters.FeatureCurveCount;
            hash = hash * 31 + frame.Counters.VisibleSegmentCount;
            hash = hash * 31 + frame.Counters.HiddenSegmentCount;
            hash = hash * 31 + frame.Counters.StrokeCandidateCount;
            hash = hash * 31 + strokeCount;
            hash = hash * 31 + toneCount;
            hash = hash * 31 + shadingCount;
            hash = hash * 31 + layerStrokeCount;
            return (hash & 0x00ffffff).ToString("x6");
        }
    }
}
