using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Graph;

public readonly record struct SurfaceSample(
    int StableId,
    int ProjectedTriangleIndex,
    Vector3 Normal,
    Vector3 CurvatureDirection,
    Point2D Position,
    float Depth,
    float Shade,
    float Curvature,
    float SmoothedCurvature);
