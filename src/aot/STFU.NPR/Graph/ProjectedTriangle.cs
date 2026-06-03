using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct ProjectedTriangle(
    int StableId,
    int ProjectedMeshIndex,
    int MeshTriangleIndex,
    int A,
    int B,
    int C,
    Vector3 Normal,
    Vector3 WorldCenter,
    Point2D ScreenCenter,
    float Depth,
    float ScreenArea,
    float Shade,
    bool IsFrontFacing,
    bool IsVisible);
