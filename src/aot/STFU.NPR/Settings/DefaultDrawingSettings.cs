using STFU.Strokes;

namespace STFU.NPR.Settings;

public enum DefaultStrokeStyle
{
    ComicInk,
    Pen,
    Pencil,
    Brush
}

public sealed class DefaultDrawingSettings
{
    public DefaultTopologyMode TopologyMode { get; set; } = DefaultTopologyMode.PerTriangleEdges;

    public bool ShowSilhouette { get; set; } = true;

    public bool ShowFeature { get; set; } = true;

    public bool ShowBoundary { get; set; } = true;

    public float FeatureAngleDegrees { get; set; } = 34f;

    public bool CullOutside { get; set; } = true;

    public float MinSegPx { get; set; } = 1f;

    public int MeshStride { get; set; } = 1;

    public bool OcclusionCulling { get; set; } = true;

    public int OcclusionSamples { get; set; } = 7;

    public float OcclusionStrictness { get; set; } = 1.0f;

    public float OcclusionBias { get; set; } = 0.0007f;

    public float DepthScale { get; set; } = 1.0f;

    public DefaultStrokeStyle StrokeStyle { get; set; } = DefaultStrokeStyle.ComicInk;

    public StrokeColor StrokeColor { get; set; } = new(0x23, 0x20, 0x1c);

    public StrokeColor PaperColor { get; set; } = new(0xe8, 0xe2, 0xd5);

    public float LineWidth { get; set; } = 2.2f;

    public float Jitter { get; set; } = 1.6f;

    public float Pressure { get; set; } = 0.32f;

    public float PathSimplify { get; set; } = 0.6f;

    public bool ShowPoints { get; set; }

    public bool AutoDraw { get; set; } = true;

    public float DrawSpeed { get; set; } = 0.28f;

    public float DrawProgress { get; set; } = 1f;

    public bool EnableFastNoise { get; set; } = true;

    public float FieldOfViewDegrees { get; set; } = 45f;

    public float NearPlane { get; set; } = 0.01f;

    public float FarPlane { get; set; } = 1000f;
}
