using System.Numerics;
using STFU.Common.Primitives;
using STFU.Mesh;

namespace STFU.NPR.Analysis;

public sealed class MeshAnalysisCacheStore
{
    private readonly Dictionary<MeshHandle, MeshAnalysisCache> _caches = new();

    public int Count => _caches.Count;

    public MeshAnalysisCache GetOrCreate(MeshHandle handle, MeshData mesh)
    {
        if (_caches.TryGetValue(handle, out var cache))
        {
            return cache;
        }

        cache = new MeshAnalysisCache(
            BuildTopology(mesh),
            CalculateBounds(mesh),
            BuildCurvature(mesh));

        _caches[handle] = cache;
        return cache;
    }

    public bool TryGet(MeshHandle handle, out MeshAnalysisCache cache)
    {
        return _caches.TryGetValue(handle, out cache!);
    }

    private static TopologyCache BuildTopology(MeshData mesh)
    {
        var edges = new Dictionary<long, PendingEdge>();

        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            AddEdge(edges, triangle.A, triangle.B, triangleIndex);
            AddEdge(edges, triangle.B, triangle.C, triangleIndex);
            AddEdge(edges, triangle.C, triangle.A, triangleIndex);
        }

        var cachedEdges = new List<TopologyCacheEdge>(edges.Count);
        foreach (var edge in edges.Values)
        {
            var normalAngleDegrees = CalculateNormalAngleDegrees(mesh, edge.FirstTriangleIndex, edge.SecondTriangleIndex);
            cachedEdges.Add(new TopologyCacheEdge(
                StableEdgeId(edge.StartVertexIndex, edge.EndVertexIndex),
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                edge.SecondTriangleIndex < 0,
                normalAngleDegrees,
                ClassifySemantic(edge.SecondTriangleIndex < 0, normalAngleDegrees)));
        }

        return new TopologyCache(cachedEdges);
    }

    private static MeshBounds CalculateBounds(MeshData mesh)
    {
        if (mesh.Vertices.Count == 0)
        {
            return new MeshBounds(Vector3.Zero, Vector3.Zero);
        }

        var min = mesh.Vertices[0].Position;
        var max = mesh.Vertices[0].Position;

        for (var index = 1; index < mesh.Vertices.Count; index++)
        {
            var position = mesh.Vertices[index].Position;
            min = Vector3.Min(min, position);
            max = Vector3.Max(max, position);
        }

        return new MeshBounds(min, max);
    }

    private static CurvatureCache BuildCurvature(MeshData mesh)
    {
        if (mesh.Vertices.Count == 0)
        {
            return CurvatureCache.Empty;
        }

        var neighbors = new List<int>[mesh.Vertices.Count];
        for (var index = 0; index < neighbors.Length; index++)
        {
            neighbors[index] = [];
        }

        foreach (var triangle in mesh.Triangles)
        {
            AddNeighbor(neighbors, triangle.A, triangle.B);
            AddNeighbor(neighbors, triangle.A, triangle.C);
            AddNeighbor(neighbors, triangle.B, triangle.A);
            AddNeighbor(neighbors, triangle.B, triangle.C);
            AddNeighbor(neighbors, triangle.C, triangle.A);
            AddNeighbor(neighbors, triangle.C, triangle.B);
        }

        var vertexCurvature = new float[mesh.Vertices.Count];
        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var vertexNormal = SafeNormal(mesh.Vertices[vertexIndex].Normal);
            var adjacent = neighbors[vertexIndex];
            if (adjacent.Count == 0)
            {
                vertexCurvature[vertexIndex] = 0f;
                continue;
            }

            var totalAngle = 0f;
            foreach (var neighborIndex in adjacent)
            {
                var neighborNormal = SafeNormal(mesh.Vertices[neighborIndex].Normal);
                var dot = Math.Clamp(System.Numerics.Vector3.Dot(vertexNormal, neighborNormal), -1f, 1f);
                totalAngle += MathF.Acos(dot) / MathF.PI;
            }

            vertexCurvature[vertexIndex] = Math.Clamp(totalAngle / adjacent.Count, 0f, 1f);
        }

        var smoothedVertexCurvature = new float[mesh.Vertices.Count];
        var vertexDirections = new System.Numerics.Vector3[mesh.Vertices.Count];
        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var adjacent = neighbors[vertexIndex];
            if (adjacent.Count == 0)
            {
                smoothedVertexCurvature[vertexIndex] = vertexCurvature[vertexIndex];
                vertexDirections[vertexIndex] = System.Numerics.Vector3.Zero;
                continue;
            }

            var totalCurvature = vertexCurvature[vertexIndex];
            var vertex = mesh.Vertices[vertexIndex];
            var vertexNormal = SafeNormal(vertex.Normal);
            var flow = System.Numerics.Vector3.Zero;

            foreach (var neighborIndex in adjacent)
            {
                totalCurvature += vertexCurvature[neighborIndex];

                var neighbor = mesh.Vertices[neighborIndex];
                var delta = neighbor.Position - vertex.Position;
                var tangentDelta = delta - vertexNormal * System.Numerics.Vector3.Dot(delta, vertexNormal);
                if (tangentDelta.LengthSquared() <= 0.0001f)
                {
                    continue;
                }

                var weight = 0.35f + MathF.Abs(vertexCurvature[neighborIndex] - vertexCurvature[vertexIndex]) * 0.65f;
                flow += System.Numerics.Vector3.Normalize(tangentDelta) * weight;
            }

            smoothedVertexCurvature[vertexIndex] = Math.Clamp(totalCurvature / (adjacent.Count + 1), 0f, 1f);
            vertexDirections[vertexIndex] = flow.LengthSquared() <= 0.0001f
                ? System.Numerics.Vector3.Zero
                : System.Numerics.Vector3.Normalize(flow);
        }

        var vertexSignedCurvature = new float[mesh.Vertices.Count];
        var smoothedVertexSignedCurvature = new float[mesh.Vertices.Count];
        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var adjacent = neighbors[vertexIndex];
            var flowDirection = vertexDirections[vertexIndex];
            if (adjacent.Count == 0 || flowDirection.LengthSquared() <= 0.0001f)
            {
                vertexSignedCurvature[vertexIndex] = 0f;
                smoothedVertexSignedCurvature[vertexIndex] = 0f;
                continue;
            }

            var vertex = mesh.Vertices[vertexIndex];
            var vertexNormal = SafeNormal(vertex.Normal);
            var signed = 0f;
            var count = 0;

            foreach (var neighborIndex in adjacent)
            {
                var neighbor = mesh.Vertices[neighborIndex];
                var delta = neighbor.Position - vertex.Position;
                if (delta.LengthSquared() <= 0.0001f)
                {
                    continue;
                }

                var tangentDelta = delta - vertexNormal * System.Numerics.Vector3.Dot(delta, vertexNormal);
                if (tangentDelta.LengthSquared() <= 0.0001f)
                {
                    continue;
                }

                var directionAlignment = System.Numerics.Vector3.Dot(System.Numerics.Vector3.Normalize(tangentDelta), flowDirection);
                var normalDelta = SafeNormal(neighbor.Normal) - vertexNormal;
                signed += System.Numerics.Vector3.Dot(normalDelta, flowDirection) * directionAlignment;
                count++;
            }

            vertexSignedCurvature[vertexIndex] = count == 0
                ? 0f
                : Math.Clamp(signed / count, -1f, 1f);
        }

        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var adjacent = neighbors[vertexIndex];
            if (adjacent.Count == 0)
            {
                smoothedVertexSignedCurvature[vertexIndex] = vertexSignedCurvature[vertexIndex];
                continue;
            }

            var totalSigned = vertexSignedCurvature[vertexIndex];
            foreach (var neighborIndex in adjacent)
            {
                totalSigned += vertexSignedCurvature[neighborIndex];
            }

            smoothedVertexSignedCurvature[vertexIndex] = Math.Clamp(totalSigned / (adjacent.Count + 1), -1f, 1f);
        }

        var triangleCurvature = new float[mesh.Triangles.Count];
        var smoothedTriangleCurvature = new float[mesh.Triangles.Count];
        var triangleSignedCurvature = new float[mesh.Triangles.Count];
        var smoothedTriangleSignedCurvature = new float[mesh.Triangles.Count];
        var triangleDirections = new System.Numerics.Vector3[mesh.Triangles.Count];
        var vertexSamples = new CurvatureSample[mesh.Vertices.Count];
        var faceSamples = new CurvatureSample[mesh.Triangles.Count];
        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            triangleCurvature[triangleIndex] = (
                vertexCurvature[triangle.A] +
                vertexCurvature[triangle.B] +
                vertexCurvature[triangle.C]) / 3f;

            smoothedTriangleCurvature[triangleIndex] = (
                smoothedVertexCurvature[triangle.A] +
                smoothedVertexCurvature[triangle.B] +
                smoothedVertexCurvature[triangle.C]) / 3f;

            triangleSignedCurvature[triangleIndex] = (
                vertexSignedCurvature[triangle.A] +
                vertexSignedCurvature[triangle.B] +
                vertexSignedCurvature[triangle.C]) / 3f;

            smoothedTriangleSignedCurvature[triangleIndex] = (
                smoothedVertexSignedCurvature[triangle.A] +
                smoothedVertexSignedCurvature[triangle.B] +
                smoothedVertexSignedCurvature[triangle.C]) / 3f;

            var direction = vertexDirections[triangle.A] + vertexDirections[triangle.B] + vertexDirections[triangle.C];
            triangleDirections[triangleIndex] = direction.LengthSquared() <= 0.0001f
                ? System.Numerics.Vector3.Zero
                : System.Numerics.Vector3.Normalize(direction);

            var faceNormal = SafeNormal(
                mesh.Vertices[triangle.A].Normal +
                mesh.Vertices[triangle.B].Normal +
                mesh.Vertices[triangle.C].Normal);
            var center = (
                mesh.Vertices[triangle.A].Position +
                mesh.Vertices[triangle.B].Position +
                mesh.Vertices[triangle.C].Position) / 3f;
            faceSamples[triangleIndex] = new CurvatureSample(
                center,
                faceNormal,
                smoothedTriangleCurvature[triangleIndex],
                smoothedTriangleSignedCurvature[triangleIndex],
                triangleDirections[triangleIndex],
                Vector3.Cross(faceNormal, triangleDirections[triangleIndex]),
                ComputeCurvatureConfidence(smoothedTriangleCurvature[triangleIndex], triangleDirections[triangleIndex]));
        }

        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var vertex = mesh.Vertices[vertexIndex];
            var direction1 = vertexDirections[vertexIndex];
            var normal = SafeNormal(vertex.Normal);
            vertexSamples[vertexIndex] = new CurvatureSample(
                vertex.Position,
                normal,
                smoothedVertexCurvature[vertexIndex],
                smoothedVertexSignedCurvature[vertexIndex],
                direction1,
                Vector3.Cross(normal, direction1),
                ComputeCurvatureConfidence(smoothedVertexCurvature[vertexIndex], direction1));
        }

        var meanEdgeLength = ComputeMeanEdgeLength(mesh);
        var smoothingRadius = meanEdgeLength * 1.5f;
        var quality = ResolveCurvatureQuality(smoothedVertexCurvature, vertexDirections);

        return new CurvatureCache(
            vertexCurvature,
            smoothedVertexCurvature,
            vertexSignedCurvature,
            smoothedVertexSignedCurvature,
            triangleCurvature,
            smoothedTriangleCurvature,
            triangleSignedCurvature,
            smoothedTriangleSignedCurvature,
            vertexDirections,
            triangleDirections,
            vertexSamples,
            faceSamples,
            meanEdgeLength,
            smoothingRadius,
            quality);
    }

    private static float ComputeCurvatureConfidence(float curvature, Vector3 direction)
    {
        var directionFactor = direction.LengthSquared() <= 0.0001f ? 0.2f : 1f;
        return Math.Clamp(curvature * 0.8f + directionFactor * 0.2f, 0f, 1f);
    }

    private static float ComputeMeanEdgeLength(MeshData mesh)
    {
        if (mesh.Triangles.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        var count = 0;
        foreach (var triangle in mesh.Triangles)
        {
            total += Vector3.Distance(mesh.Vertices[triangle.A].Position, mesh.Vertices[triangle.B].Position);
            total += Vector3.Distance(mesh.Vertices[triangle.B].Position, mesh.Vertices[triangle.C].Position);
            total += Vector3.Distance(mesh.Vertices[triangle.C].Position, mesh.Vertices[triangle.A].Position);
            count += 3;
        }

        return count == 0 ? 0f : total / count;
    }

    private static CurvatureQuality ResolveCurvatureQuality(IReadOnlyList<float> smoothedVertexCurvature, IReadOnlyList<Vector3> vertexDirections)
    {
        if (smoothedVertexCurvature.Count == 0)
        {
            return CurvatureQuality.NotComputed;
        }

        var avgCurvature = smoothedVertexCurvature.Average();
        var directionalFraction = vertexDirections.Count(direction => direction.LengthSquared() > 0.0001f) / (float)Math.Max(1, vertexDirections.Count);

        if (avgCurvature >= 0.12f && directionalFraction >= 0.45f)
        {
            return CurvatureQuality.GoodForSuggestiveContours;
        }

        if (avgCurvature >= 0.06f && directionalFraction >= 0.2f)
        {
            return CurvatureQuality.GoodForHatching;
        }

        return CurvatureQuality.LowConfidence;
    }

    private static float CalculateNormalAngleDegrees(MeshData mesh, int firstTriangleIndex, int secondTriangleIndex)
    {
        if (firstTriangleIndex < 0 || secondTriangleIndex < 0 ||
            firstTriangleIndex >= mesh.Triangles.Count || secondTriangleIndex >= mesh.Triangles.Count)
        {
            return 0f;
        }

        var first = CalculateTriangleNormal(mesh, mesh.Triangles[firstTriangleIndex]);
        var second = CalculateTriangleNormal(mesh, mesh.Triangles[secondTriangleIndex]);
        var dot = Math.Clamp(Vector3.Dot(first, second), -1f, 1f);
        return MathF.Acos(dot) * (180f / MathF.PI);
    }

    private static Vector3 CalculateTriangleNormal(MeshData mesh, MeshTriangle triangle)
    {
        var a = mesh.Vertices[triangle.A].Position;
        var b = mesh.Vertices[triangle.B].Position;
        var c = mesh.Vertices[triangle.C].Position;
        var cross = Vector3.Cross(b - a, c - a);
        return cross.LengthSquared() <= 0.0001f ? Vector3.UnitY : Vector3.Normalize(cross);
    }

    private static EdgeSemantic ClassifySemantic(bool isBoundary, float normalAngleDegrees)
    {
        if (isBoundary)
        {
            return EdgeSemantic.Boundary;
        }

        if (normalAngleDegrees >= 30f)
        {
            return EdgeSemantic.HardCrease;
        }

        return EdgeSemantic.Smooth;
    }

    private static void AddNeighbor(List<int>[] neighbors, int vertexIndex, int neighborIndex)
    {
        var adjacent = neighbors[vertexIndex];
        if (!adjacent.Contains(neighborIndex))
        {
            adjacent.Add(neighborIndex);
        }
    }

    private static System.Numerics.Vector3 SafeNormal(System.Numerics.Vector3 normal)
    {
        return normal.LengthSquared() <= 0.0001f
            ? System.Numerics.Vector3.UnitY
            : System.Numerics.Vector3.Normalize(normal);
    }

    private static void AddEdge(Dictionary<long, PendingEdge> edges, int a, int b, int triangleIndex)
    {
        var key = CreateEdgeKey(a, b);

        if (edges.TryGetValue(key, out var edge))
        {
            if (edge.SecondTriangleIndex < 0)
            {
                edges[key] = edge with { SecondTriangleIndex = triangleIndex };
            }

            return;
        }

        edges[key] = new PendingEdge(Math.Min(a, b), Math.Max(a, b), triangleIndex, -1);
    }

    private static long CreateEdgeKey(int a, int b)
    {
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        return ((long)min << 32) | (uint)max;
    }

    private static int StableEdgeId(int a, int b)
    {
        unchecked
        {
            return Math.Min(a, b) * 83492791 ^ Math.Max(a, b) * 297121507;
        }
    }

    private readonly record struct PendingEdge(
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex);
}
