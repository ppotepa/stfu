using System.Numerics;

namespace STFU.Common.Math;

public static class TransformMath
{
    public static Matrix4x4 IdentityMatrix => Matrix4x4.Identity;

    public static Quaternion CreateYawPitchRollRotation(Vector3 rotation)
    {
        return Geometry3D.CreateYawPitchRollRotation(rotation);
    }

    public static Vector3 TransformPosition(
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Vector3 translation,
        bool hasRotation,
        bool hasScale,
        bool hasTranslation)
    {
        return Geometry3D.TransformPosition(position, scale, rotation, translation, hasRotation, hasScale, hasTranslation);
    }
}
