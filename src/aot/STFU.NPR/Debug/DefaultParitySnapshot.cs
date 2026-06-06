namespace STFU.NPR.Debug;

public sealed record DefaultParitySnapshot(
    int FrameId,
    string PresetId,
    string PipelineId,
    int Width,
    int Height,
    DefaultParityCameraSnapshot Camera,
    DefaultParitySettingsSnapshot Settings,
    DefaultParityCountsSnapshot Counts,
    DefaultParityVisibilitySnapshot Visibility,
    IReadOnlyList<DefaultParityProjectedVertexSnapshot> ProjectedVertices,
    IReadOnlyList<DefaultParityTriangleSnapshot> Triangles,
    IReadOnlyList<DefaultParityTopologyEdgeSnapshot> TopologyEdges,
    IReadOnlyList<DefaultParityFragmentSnapshot> Fragments,
    IReadOnlyList<DefaultParityPathSnapshot> Paths,
    IReadOnlyList<DefaultParityPathSnapshot> DrawablePaths,
    IReadOnlyDictionary<string, double> TimingsMs);

public sealed record DefaultParityCameraSnapshot(
    float FieldOfView,
    float NearPlane,
    float FarPlane,
    float[] Position,
    float[] Target);

public sealed record DefaultParitySettingsSnapshot(
    int Seed,
    float FieldOfViewDegrees,
    float NearPlane,
    float FarPlane,
    string TopologyMode,
    bool ShowSilhouette,
    bool ShowFeature,
    bool ShowBoundary,
    float FeatureAngleDegrees,
    bool CullOutside,
    float MinSegPx,
    int MeshStride,
    bool OcclusionCulling,
    int OcclusionSamples,
    float OcclusionStrictness,
    float OcclusionBias,
    float DepthScale,
    string StrokeStyle,
    float LineWidth,
    float Jitter,
    float Pressure,
    float PathSimplify,
    bool AutoDraw,
    float DrawSpeed,
    float DrawProgress);

public sealed record DefaultParityCountsSnapshot(
    int Meshes,
    int Vertices,
    int Triangles,
    int TopologyEdges,
    int FeatureCurves,
    int VisibleSegments,
    int HiddenSegments,
    int Fragments,
    int Paths,
    int DrawablePaths,
    int FinalStrokes);

public sealed record DefaultParityVisibilitySnapshot(
    int BufferWidth,
    int BufferHeight,
    int VisibleFaceCount,
    IReadOnlyList<int> VisibleFaces,
    ulong? FaceIdHash = null,
    int? LineVisibleFaceCount = null,
    IReadOnlyList<int>? LineVisibleFaces = null);

public sealed record DefaultParityProjectedVertexSnapshot(
    int MeshVertexIndex,
    float[] WorldPosition,
    float[] Screen,
    float Depth,
    float Depth01,
    bool IsVisible,
    float[] Ndc);

public sealed record DefaultParityTriangleSnapshot(
    int StableId,
    int ProjectedMeshIndex,
    int MeshTriangleIndex,
    int A,
    int B,
    int C,
    float[] Normal,
    float Depth,
    float ScreenArea,
    bool IsFrontFacing,
    bool IsVisible);

public sealed record DefaultParityTopologyEdgeSnapshot(
    int StableId,
    int StartVertexIndex,
    int EndVertexIndex,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    float NormalAngleDegrees,
    bool IsBoundary);

public sealed record DefaultParityFragmentSnapshot(
    int StableId,
    string Type,
    float[] P0,
    float[] P1,
    int EdgeStableId,
    int FirstTriangleIndex,
    int SecondTriangleIndex,
    float StartT,
    float EndT,
    float Depth);

public sealed record DefaultParityPathSnapshot(
    int StableId,
    string Type,
    int PathIndex,
    float Length,
    IReadOnlyList<float[]> Points);
