using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Math;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxMeshWireframeBatch
{
    public readonly List<DxMeshVertex> Vertices = [];
    public readonly List<DxMeshEdge> Edges = [];
    public readonly List<bool> Visible = [];

    public ulong EdgeSignature { get; set; }

    public void Clear()
    {
        Vertices.Clear();
        Edges.Clear();
        Visible.Clear();
        EdgeSignature = 0;
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DxMeshVertex
{
    public readonly Vector4 Position;

    public DxMeshVertex(STFU.Strokes.Point2D position)
    {
        Position = new Vector4(position.X, position.Y, 0f, 1f);
    }

    public const int SizeInBytes = 16;
}

[StructLayout(LayoutKind.Sequential)]
public readonly struct DxMeshEdge
{
    public readonly uint Start;
    public readonly uint End;

    public DxMeshEdge(int start, int end)
    {
        Start = (uint)NumericMath.AtLeast(start, 0);
        End = (uint)NumericMath.AtLeast(end, 0);
    }

    public const int SizeInBytes = 8;
}
