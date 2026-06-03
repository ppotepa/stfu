using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct ProjectedVertex(
    int MeshVertexIndex,
    Vector3 WorldPosition,
    Vector3 WorldNormal,
    Point2D Position,
    float Depth,
    bool IsVisible);
