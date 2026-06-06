using System.Numerics;

namespace STFU.Common.Math;

public static class Geometry3D
{
    public const float DefaultEpsilonSquared = 0.0001f;

    public static Vector3 NormalizeOrDefault(
        Vector3 value,
        Vector3 fallback,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        return value.LengthSquared() <= epsilonSquared ? fallback : Vector3.Normalize(value);
    }

    public static Vector3 TriangleNormal(
        Vector3 a,
        Vector3 b,
        Vector3 c,
        Vector3 fallback,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        return NormalizeOrDefault(Vector3.Cross(b - a, c - a), fallback, epsilonSquared);
    }

    public static Vector3 TriangleNormal<TTriangle>(
        TTriangle triangle,
        Func<TTriangle, int> getA,
        Func<TTriangle, int> getB,
        Func<TTriangle, int> getC,
        Func<int, Vector3> getPosition,
        Vector3 fallback,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        return TriangleNormal(
            getPosition(getA(triangle)),
            getPosition(getB(triangle)),
            getPosition(getC(triangle)),
            fallback,
            epsilonSquared);
    }

    public static float NormalAngleDegrees(Vector3 a, Vector3 b)
    {
        var dot = global::System.Math.Clamp(Vector3.Dot(NormalizeOrDefault(a, Vector3.UnitY), NormalizeOrDefault(b, Vector3.UnitY)), -1f, 1f);
        return MathF.Acos(dot) * NumericMath.RadiansToDegreesFactor;
    }

    public static bool IsFrontFacing(
        Vector3 normal,
        Vector3 center,
        Vector3 cameraPosition,
        float epsilonSquared = DefaultEpsilonSquared,
        bool degenerateResult = true,
        bool normalizeViewDirection = true)
    {
        var view = cameraPosition - center;
        if (view.LengthSquared() <= epsilonSquared)
        {
            return degenerateResult;
        }

        var viewDirection = normalizeViewDirection ? Vector3.Normalize(view) : view;
        return Vector3.Dot(normal, viewDirection) > 0f;
    }

    public static (Vector3 Min, Vector3 Max) Bounds(IReadOnlyList<Vector3> positions)
    {
        if (positions.Count == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        var min = positions[0];
        var max = positions[0];
        for (var index = 1; index < positions.Count; index++)
        {
            var position = positions[index];
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return (min, max);
    }

    public static (Vector3 Min, Vector3 Max) Bounds<TItem>(
        IReadOnlyList<TItem> items,
        Func<TItem, Vector3> getPosition)
    {
        if (items.Count == 0)
        {
            return (Vector3.Zero, Vector3.Zero);
        }

        var min = getPosition(items[0]);
        var max = min;
        for (var index = 1; index < items.Count; index++)
        {
            var position = getPosition(items[index]);
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return (min, max);
    }

    public static float MeanTriangleEdgeLength<TTriangle>(
        IReadOnlyList<TTriangle> triangles,
        Func<TTriangle, int> getA,
        Func<TTriangle, int> getB,
        Func<TTriangle, int> getC,
        Func<int, Vector3> getPosition)
    {
        if (triangles.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        var count = 0;
        for (var index = 0; index < triangles.Count; index++)
        {
            var triangle = triangles[index];
            var a = getPosition(getA(triangle));
            var b = getPosition(getB(triangle));
            var c = getPosition(getC(triangle));
            total += Vector3.Distance(a, b);
            total += Vector3.Distance(b, c);
            total += Vector3.Distance(c, a);
            count += 3;
        }

        return count == 0 ? 0f : total / count;
    }

    public static bool TriangleOutsideClip(Vector3 a, Vector3 b, Vector3 c)
    {
        if (a.X < -1f && b.X < -1f && c.X < -1f) return true;
        if (a.X > 1f && b.X > 1f && c.X > 1f) return true;
        if (a.Y < -1f && b.Y < -1f && c.Y < -1f) return true;
        if (a.Y > 1f && b.Y > 1f && c.Y > 1f) return true;
        if (a.Z < -1f && b.Z < -1f && c.Z < -1f) return true;
        if (a.Z > 1f && b.Z > 1f && c.Z > 1f) return true;
        return false;
    }

    public static float MaxComponent(Vector3 value)
    {
        return MathF.Max(value.X, MathF.Max(value.Y, value.Z));
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
        if (!hasRotation)
        {
            if (!hasScale && !hasTranslation)
            {
                return position;
            }

            if (!hasScale)
            {
                return position + translation;
            }

            if (!hasTranslation)
            {
                return position * scale;
            }

            return position * scale + translation;
        }

        var scaled = hasScale ? position * scale : position;
        var rotated = Vector3.Transform(scaled, rotation);
        return hasTranslation ? rotated + translation : rotated;
    }

    public static Quaternion CreateYawPitchRollRotation(Vector3 rotation)
    {
        return Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
    }

    public static bool HasVectorLength(Vector3 value, float epsilonSquared = DefaultEpsilonSquared)
    {
        return value.LengthSquared() > epsilonSquared;
    }

    public static bool HasNonIdentityScale(Vector3 scale, float epsilonSquared = DefaultEpsilonSquared)
    {
        return (scale - Vector3.One).LengthSquared() > epsilonSquared;
    }
}
