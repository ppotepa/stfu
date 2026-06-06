using System.Numerics;

namespace STFU.Common.Math;

public static class CurvatureMath
{
    public const float DefaultEpsilonSquared = 0.0001f;

    public static bool HasDirection(Vector3 direction, float epsilonSquared = DefaultEpsilonSquared)
    {
        return direction.LengthSquared() > epsilonSquared;
    }

    public static Vector3 NormalizeOrZero(Vector3 value, float epsilonSquared = DefaultEpsilonSquared)
    {
        return Geometry3D.NormalizeOrDefault(value, Vector3.Zero, epsilonSquared);
    }

    public static Vector3 ProjectToTangentPlane(Vector3 value, Vector3 normal)
    {
        return value - normal * Vector3.Dot(value, normal);
    }

    public static bool TryNormalizeTangentDirection(
        Vector3 value,
        Vector3 normal,
        out Vector3 direction,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        var tangent = ProjectToTangentPlane(value, normal);
        if (tangent.LengthSquared() <= epsilonSquared)
        {
            direction = Vector3.Zero;
            return false;
        }

        direction = Vector3.Normalize(tangent);
        return true;
    }

    public static bool TryComputeFlowContribution(
        Vector3 delta,
        Vector3 normal,
        float centerCurvature,
        float neighborCurvature,
        out Vector3 contribution,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        if (!TryNormalizeTangentDirection(delta, normal, out var tangentDirection, epsilonSquared))
        {
            contribution = Vector3.Zero;
            return false;
        }

        contribution = tangentDirection * FlowWeight(centerCurvature, neighborCurvature);
        return true;
    }

    public static bool TryComputeSignedContribution(
        Vector3 delta,
        Vector3 normal,
        Vector3 neighborNormal,
        Vector3 flowDirection,
        out float contribution,
        float epsilonSquared = DefaultEpsilonSquared)
    {
        if (!HasDirection(delta, epsilonSquared) ||
            !HasDirection(flowDirection, epsilonSquared) ||
            !TryNormalizeTangentDirection(delta, normal, out var tangentDirection, epsilonSquared))
        {
            contribution = 0f;
            return false;
        }

        var directionAlignment = Vector3.Dot(tangentDirection, flowDirection);
        var normalDelta = neighborNormal - normal;
        contribution = Vector3.Dot(normalDelta, flowDirection) * directionAlignment;
        return true;
    }

    public static float FlowWeight(float centerCurvature, float neighborCurvature)
    {
        return 0.35f + NumericMath.Abs(neighborCurvature - centerCurvature) * 0.65f;
    }

    public static float Confidence(float curvature, Vector3 direction, float epsilonSquared = DefaultEpsilonSquared)
    {
        var directionFactor = HasDirection(direction, epsilonSquared) ? 1f : 0.2f;
        return NumericMath.Clamp01(curvature * 0.8f + directionFactor * 0.2f);
    }

    public static float DirectionalFraction(IReadOnlyList<Vector3> directions, float epsilonSquared = DefaultEpsilonSquared)
    {
        if (directions.Count == 0)
        {
            return 0f;
        }

        var directionalCount = 0;
        for (var index = 0; index < directions.Count; index++)
        {
            if (HasDirection(directions[index], epsilonSquared))
            {
                directionalCount++;
            }
        }

        return directionalCount / (float)directions.Count;
    }

    public static Vector3 Binormal(Vector3 normal, Vector3 direction)
    {
        return Vector3.Cross(normal, direction);
    }
}
