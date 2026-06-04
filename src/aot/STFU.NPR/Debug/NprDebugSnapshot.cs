namespace STFU.NPR.Debug;

public sealed record NprDebugSnapshot(
    int FrameId,
    string PresetId,
    NprDebugSnapshotCamera Camera,
    NprDebugSnapshotCounts Counts,
    IReadOnlyDictionary<string, double> TimingsMs);

public sealed record NprDebugSnapshotCamera(
    float FieldOfView,
    float[] Position,
    float[] Target);

public sealed record NprDebugSnapshotCounts(
    int Triangles,
    int FeatureCurves,
    int VisibleSegments,
    int HiddenSegments,
    int StrokeCandidates,
    int FinalStrokes);
