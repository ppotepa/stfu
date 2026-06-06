using STFU.Common.Math;

namespace STFU.NPR.Graph;

public sealed class SurfaceVisibilityBuffer
{
    public SurfaceVisibilityBuffer(int width, int height, int triangleCount)
    {
        Width = RasterMath.AtLeastPixels(width, 1);
        Height = RasterMath.AtLeastPixels(height, 1);
        var pixelCount = Width * Height;
        Depth = new float[pixelCount];
        TriangleIndex = new int[pixelCount];
        EntityId = new int[pixelCount];
        MaterialId = new int[pixelCount];
        Tone = new float[pixelCount];
        TangentX = new float[pixelCount];
        TangentY = new float[pixelCount];
        VisibleTriangles = new bool[NumericMath.AtLeast(triangleCount, 0)];
        Clear();
    }

    public int Width { get; }

    public int Height { get; }

    public float[] Depth { get; }

    public int[] TriangleIndex { get; }

    public int[] EntityId { get; }

    public int[] MaterialId { get; }

    public float[] Tone { get; }

    public float[] TangentX { get; }

    public float[] TangentY { get; }

    public bool[] VisibleTriangles { get; }

    public void Clear()
    {
        Array.Fill(Depth, float.PositiveInfinity);
        Array.Fill(TriangleIndex, -1);
        Array.Fill(EntityId, -1);
        Array.Fill(MaterialId, -1);
        Array.Fill(Tone, 0f);
        Array.Fill(TangentX, 1f);
        Array.Fill(TangentY, 0f);
        Array.Fill(VisibleTriangles, false);
    }
}
