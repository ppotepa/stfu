namespace STFU.UI.Bridge.Scene;

public sealed record SceneMeshBoundsInfo(
    bool HasBounds,
    float MinX,
    float MinY,
    float MinZ,
    float MaxX,
    float MaxY,
    float MaxZ)
{
    public static SceneMeshBoundsInfo Empty { get; } = new(false, 0f, 0f, 0f, 0f, 0f, 0f);

    public float SizeX => HasBounds ? MaxX - MinX : 0f;

    public float SizeY => HasBounds ? MaxY - MinY : 0f;

    public float SizeZ => HasBounds ? MaxZ - MinZ : 0f;

    public float CenterX => HasBounds ? (MinX + MaxX) * 0.5f : 0f;

    public float CenterY => HasBounds ? (MinY + MaxY) * 0.5f : 0f;

    public float CenterZ => HasBounds ? (MinZ + MaxZ) * 0.5f : 0f;

    public float LargestDimension => MathF.Max(SizeX, MathF.Max(SizeY, SizeZ));

    public string SizeLabel => HasBounds
        ? $"{SizeX:0.###} x {SizeY:0.###} x {SizeZ:0.###}"
        : "no bounds";

    public string CenterLabel => HasBounds
        ? $"{CenterX:0.###}, {CenterY:0.###}, {CenterZ:0.###}"
        : "no center";

    public string Summary => HasBounds
        ? $"size {SizeLabel}, center {CenterLabel}"
        : "no mesh bounds";
}
