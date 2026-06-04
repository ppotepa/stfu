using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct ProjectedVertex(
    int MeshVertexIndex,
    Vector3 WorldPosition,
    Vector3 WorldNormal,
    Point2D Position,
    float Depth,
    bool IsVisible,
    float Curvature = 0f,
    float SmoothedCurvature = 0f,
    float SignedCurvature = 0f,
    float SmoothedSignedCurvature = 0f,
    Vector3 CurvatureDirection = default,
    Vector3 Ndc = default,
    float Depth01 = 1f);
