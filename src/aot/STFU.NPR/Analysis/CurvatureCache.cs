using System.Numerics;

namespace STFU.NPR.Analysis;

public sealed record CurvatureCache(
    IReadOnlyList<float> VertexCurvature,
    IReadOnlyList<float> SmoothedVertexCurvature,
    IReadOnlyList<float> VertexSignedCurvature,
    IReadOnlyList<float> SmoothedVertexSignedCurvature,
    IReadOnlyList<float> TriangleCurvature,
    IReadOnlyList<float> SmoothedTriangleCurvature,
    IReadOnlyList<float> TriangleSignedCurvature,
    IReadOnlyList<float> SmoothedTriangleSignedCurvature,
    IReadOnlyList<Vector3> VertexDirections,
    IReadOnlyList<Vector3> TriangleDirections,
    IReadOnlyList<CurvatureSample> VertexSamples,
    IReadOnlyList<CurvatureSample> FaceSamples,
    float MeanEdgeLength,
    float SmoothingRadius,
    CurvatureQuality Quality)
{
    public static CurvatureCache Empty { get; } = new([], [], [], [], [], [], [], [], [], [], [], [], 0f, 0f, CurvatureQuality.NotComputed);

    public float GetVertexCurvature(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)VertexCurvature.Count
            ? VertexCurvature[vertexIndex]
            : 0f;
    }

    public float GetSmoothedVertexCurvature(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)SmoothedVertexCurvature.Count
            ? SmoothedVertexCurvature[vertexIndex]
            : 0f;
    }

    public float GetTriangleCurvature(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)TriangleCurvature.Count
            ? TriangleCurvature[triangleIndex]
            : 0f;
    }

    public float GetSmoothedTriangleCurvature(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)SmoothedTriangleCurvature.Count
            ? SmoothedTriangleCurvature[triangleIndex]
            : 0f;
    }

    public float GetVertexSignedCurvature(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)VertexSignedCurvature.Count
            ? VertexSignedCurvature[vertexIndex]
            : 0f;
    }

    public float GetSmoothedVertexSignedCurvature(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)SmoothedVertexSignedCurvature.Count
            ? SmoothedVertexSignedCurvature[vertexIndex]
            : 0f;
    }

    public float GetTriangleSignedCurvature(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)TriangleSignedCurvature.Count
            ? TriangleSignedCurvature[triangleIndex]
            : 0f;
    }

    public float GetSmoothedTriangleSignedCurvature(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)SmoothedTriangleSignedCurvature.Count
            ? SmoothedTriangleSignedCurvature[triangleIndex]
            : 0f;
    }

    public Vector3 GetVertexDirection(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)VertexDirections.Count
            ? VertexDirections[vertexIndex]
            : Vector3.Zero;
    }

    public Vector3 GetTriangleDirection(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)TriangleDirections.Count
            ? TriangleDirections[triangleIndex]
            : Vector3.Zero;
    }

    public CurvatureSample GetVertexSample(int vertexIndex)
    {
        return (uint)vertexIndex < (uint)VertexSamples.Count
            ? VertexSamples[vertexIndex]
            : default;
    }

    public CurvatureSample GetFaceSample(int triangleIndex)
    {
        return (uint)triangleIndex < (uint)FaceSamples.Count
            ? FaceSamples[triangleIndex]
            : default;
    }
}
