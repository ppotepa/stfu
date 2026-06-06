using System.Numerics;

namespace STFU.Common.Math;

public static class ClipSpaceMath
{
    public static bool IsInsideCanonicalClip(Vector3 point)
    {
        return point.X is >= -1f and <= 1f &&
               point.Y is >= -1f and <= 1f &&
               point.Z is >= -1f and <= 1f;
    }

    public static bool SegmentOutsideCanonicalClipXY(Vector3 a, Vector3 b)
    {
        return (a.X < -1f && b.X < -1f) ||
               (a.X > 1f && b.X > 1f) ||
               (a.Y < -1f && b.Y < -1f) ||
               (a.Y > 1f && b.Y > 1f);
    }

    public static bool SegmentOutsideCanonicalClip(Vector3 a, Vector3 b)
    {
        return SegmentOutsideCanonicalClipXY(a, b) ||
               (a.Z < -1f && b.Z < -1f) ||
               (a.Z > 1f && b.Z > 1f);
    }

    public static bool TriangleOutsideCanonicalClip(Vector3 a, Vector3 b, Vector3 c)
    {
        return (a.X < -1f && b.X < -1f && c.X < -1f) ||
               (a.X > 1f && b.X > 1f && c.X > 1f) ||
               (a.Y < -1f && b.Y < -1f && c.Y < -1f) ||
               (a.Y > 1f && b.Y > 1f && c.Y > 1f) ||
               (a.Z < -1f && b.Z < -1f && c.Z < -1f) ||
               (a.Z > 1f && b.Z > 1f && c.Z > 1f);
    }
}
