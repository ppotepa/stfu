using STFU.NPR.Fields;
using STFU.NPR.Temporal;
using STFU.NPR.Rendering;

namespace STFU.NPR.Graph;

public sealed class NprGraph
{
    public List<ProjectedMesh> Meshes { get; } = [];

    public List<ProjectedVertex> Vertices { get; } = [];

    public List<ProjectedTriangle> Triangles { get; } = [];

    public List<TopologyEdge> TopologyEdges { get; } = [];

    public List<ProjectedEdge> Edges { get; } = [];

    public List<SurfaceSample> SurfaceSamples { get; } = [];

    public List<MaterialRegion> MaterialRegions { get; } = [];

    public List<StyleMask> StyleMasks { get; } = [];

    public List<FeatureCurve> Curves { get; } = [];

    public List<VisibilitySegment> VisibilitySegments { get; } = [];

    public List<FeatureLine> FeatureLines { get; } = [];

    public List<StrokeCandidate> Candidates { get; } = [];

    public List<StyledStroke> StyledStrokes { get; } = [];

    public List<NprToneSurface2D> ToneSurfaces { get; } = [];

    public List<StyledStroke> Strokes => StyledStrokes;

    public List<HatchingPlan> HatchingPlans { get; } = [];

    public Dictionary<int, SalienceScore> SalienceByStableId { get; } = [];

    public Dictionary<int, TemporalCurveMatch> CurveMatchesByStableId { get; } = [];

    public Dictionary<int, TemporalStrokeMatch> StrokeMatchesByStableId { get; } = [];

    public Dictionary<int, TemporalFeatureState> CurveStatesByStableId { get; } = [];

    public Dictionary<int, TemporalStrokeState> StrokeStatesByStableId { get; } = [];

    public ToneField? ToneField { get; set; }

    public DirectionField? DirectionField { get; set; }

    public DensityField? DensityField { get; set; }

    public TextureField? TextureField { get; set; }

    public SurfaceVisibilityBuffer? SurfaceVisibility { get; set; }

    public void Clear()
    {
        Meshes.Clear();
        Vertices.Clear();
        Triangles.Clear();
        TopologyEdges.Clear();
        Edges.Clear();
        SurfaceSamples.Clear();
        MaterialRegions.Clear();
        StyleMasks.Clear();
        Curves.Clear();
        VisibilitySegments.Clear();
        FeatureLines.Clear();
        Candidates.Clear();
        StyledStrokes.Clear();
        ToneSurfaces.Clear();
        HatchingPlans.Clear();
        SalienceByStableId.Clear();
        CurveMatchesByStableId.Clear();
        StrokeMatchesByStableId.Clear();
        CurveStatesByStableId.Clear();
        StrokeStatesByStableId.Clear();
        ToneField = null;
        DirectionField = null;
        DensityField = null;
        TextureField = null;
        SurfaceVisibility = null;
    }

    public void AddCurve(FeatureCurve curve)
    {
        Curves.Add(curve);
        FeatureLines.Add(curve.ToFeatureLine());
    }

    public void ReplaceCurves(IEnumerable<FeatureCurve> curves)
    {
        Curves.Clear();
        FeatureLines.Clear();

        foreach (var curve in curves)
        {
            Curves.Add(curve);
            FeatureLines.Add(curve.ToFeatureLine());
        }
    }

    public void ReplaceVisibilitySegments(IEnumerable<VisibilitySegment> segments)
    {
        VisibilitySegments.Clear();
        VisibilitySegments.AddRange(segments);

        FeatureLines.Clear();
        foreach (var segment in VisibilitySegments)
        {
            if (segment.State == VisibilityState.Visible)
            {
                FeatureLines.Add(segment.ToFeatureLine());
            }
        }
    }

    public SalienceScore GetSalience(int stableId, float fallbackImportance)
    {
        if (SalienceByStableId.TryGetValue(stableId, out var score))
        {
            return score;
        }

        var clamped = Math.Clamp(fallbackImportance, 0f, 1f);
        return new SalienceScore(clamped, 1f, clamped, 1f, clamped, 1f, 0f, clamped);
    }
}
