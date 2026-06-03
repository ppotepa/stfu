using System.Numerics;

namespace STFU.Mesh;

public readonly record struct MeshVertex(
    Vector3 Position,
    Vector3 Normal);
