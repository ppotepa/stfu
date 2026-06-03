using System.Numerics;

namespace STFU.Projection;

public readonly record struct CameraState(
    Vector3 Position,
    Vector3 Target,
    float FieldOfViewDegrees)
{
    public static CameraState Default { get; } = new(
        new Vector3(0, 0, 4),
        Vector3.Zero,
        60);
}
