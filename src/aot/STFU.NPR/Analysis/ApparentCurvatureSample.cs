using System.Numerics;

namespace STFU.NPR.Analysis;

public readonly record struct ApparentCurvatureSample(
    int TriangleIndex,
    Vector3 ViewDirection,
    float ApparentCurvature,
    float Confidence);
