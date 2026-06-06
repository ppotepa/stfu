using System.Numerics;

namespace STFU.Common.Math;

public readonly record struct Transform3D(
    Vector3 Position,
    Vector3 Rotation,
    Vector3 Scale)
{
    public static Transform3D Identity { get; } = new(
        Vector3.Zero,
        Vector3.Zero,
        Vector3.One);

    public Transform3D WithPosition(Vector3 position)
    {
        return this with { Position = position };
    }
}
