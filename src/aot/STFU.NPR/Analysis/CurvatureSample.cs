using System.Numerics;

namespace STFU.NPR.Analysis;

public readonly record struct CurvatureSample(
    Vector3 Position,
    Vector3 Normal,
    float K1,
    float K2,
    Vector3 Direction1,
    Vector3 Direction2,
    float Confidence);
