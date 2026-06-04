using System.Numerics;

namespace STFU.NPR.Analysis;

public readonly record struct MeshBounds(
    Vector3 Min,
    Vector3 Max);
