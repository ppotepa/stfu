using System.Numerics;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Mesh;
using STFU.NPR.Settings;

namespace STFU.NPR.Analysis;

public sealed class MeshAnalysisCacheStore
{
    private readonly Dictionary<MeshHandle, CacheEntry> _caches = new();
    private readonly Dictionary<WireframeTopologyKey, WireframeTopologyEntry> _wireframeTopologies = new();
    private readonly Dictionary<DefaultNprTopologyCacheKey, DefaultNprTopologyEntry> _defaultNprTopologies = new();

    public int Count => _caches.Count;

    public MeshAnalysisCache GetOrCreate(MeshHandle handle, MeshData mesh)
    {
        if (_caches.TryGetValue(handle, out var entry) &&
            ReferenceEquals(entry.Mesh, mesh))
        {
            return entry.Cache;
        }

        var cache = new MeshAnalysisCache(
            BuildTopology(mesh),
            CalculateBounds(mesh),
            BuildCurvature(mesh));

        _caches[handle] = new CacheEntry(mesh, cache);
        return cache;
    }

    public TopologyCache GetOrCreateWireframeTopology(
        MeshHandle handle,
        MeshData mesh,
        MeshWireframeTopologyMode mode = MeshWireframeTopologyMode.Raw)
    {
        var signature = mode == MeshWireframeTopologyMode.Welded
            ? CalculateWeldedTriangleSignature(mesh)
            : CalculateTriangleSignature(mesh);
        var key = new WireframeTopologyKey(handle, mode);
        if (_wireframeTopologies.TryGetValue(key, out var entry) &&
            entry.VertexCount == mesh.Vertices.Count &&
            entry.TriangleCount == mesh.Triangles.Count &&
            entry.Signature == signature)
        {
            return entry.Topology;
        }

        var topology = mode == MeshWireframeTopologyMode.Welded
            ? BuildWeldedWireframeTopology(mesh)
            : BuildWireframeTopology(mesh);
        _wireframeTopologies[key] = new WireframeTopologyEntry(
            mesh.Vertices.Count,
            mesh.Triangles.Count,
            signature,
            topology);
        return topology;
    }

    public DefaultNprTopologyCache GetOrCreateDefaultNprTopology(
        MeshHandle handle,
        MeshData mesh,
        DefaultTopologyMode mode)
    {
        var signature = CalculateTriangleSignature(mesh);
        var key = new DefaultNprTopologyCacheKey(handle, mode);
        if (_defaultNprTopologies.TryGetValue(key, out var entry) &&
            entry.VertexCount == mesh.Vertices.Count &&
            entry.TriangleCount == mesh.Triangles.Count &&
            entry.Signature == signature)
        {
            return entry.Cache;
        }

        var cache = mode == DefaultTopologyMode.SharedTopology
            ? BuildDefaultSharedTopology(mesh, signature, mode)
            : BuildDefaultPerTriangleTopology(mesh, signature, mode);
        _defaultNprTopologies[key] = new DefaultNprTopologyEntry(
            mesh.Vertices.Count,
            mesh.Triangles.Count,
            signature,
            cache);
        return cache;
    }

    public bool TryGet(MeshHandle handle, out MeshAnalysisCache cache)
    {
        if (_caches.TryGetValue(handle, out var entry))
        {
            cache = entry.Cache;
            return true;
        }

        cache = default!;
        return false;
    }

    public void Invalidate(MeshHandle handle)
    {
        InvalidateGeometry(handle);
    }

    public void InvalidateGeometry(MeshHandle handle)
    {
        _caches.Remove(handle);
    }

    public void InvalidateTopology(MeshHandle handle)
    {
        _caches.Remove(handle);
        foreach (var key in _wireframeTopologies.Keys.Where(key => key.Handle == handle).ToArray())
        {
            _wireframeTopologies.Remove(key);
        }

        foreach (var key in _defaultNprTopologies.Keys.Where(key => key.Handle == handle).ToArray())
        {
            _defaultNprTopologies.Remove(key);
        }
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
            var normalAngleDegrees = 0f;
            if (edge.FirstTriangleIndex >= 0 &&
                edge.SecondTriangleIndex >= 0 &&
                edge.FirstTriangleIndex < mesh.Triangles.Count &&
                edge.SecondTriangleIndex < mesh.Triangles.Count)
            {
                var first = Geometry3D.TriangleNormal(
                    mesh.Triangles[edge.FirstTriangleIndex],
                    static item => item.A,
                    static item => item.B,
                    static item => item.C,
                    index => mesh.Vertices[index].Position,
                    Vector3.UnitY);
                var second = Geometry3D.TriangleNormal(
                    mesh.Triangles[edge.SecondTriangleIndex],
                    static item => item.A,
                    static item => item.B,
                    static item => item.C,
                    index => mesh.Vertices[index].Position,
                    Vector3.UnitY);
                normalAngleDegrees = Geometry3D.NormalAngleDegrees(first, second);
            }

            cachedEdges.Add(new TopologyCacheEdge(
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                edge.SecondTriangleIndex < 0,
                normalAngleDegrees,
                ClassifySemantic(edge.SecondTriangleIndex < 0, normalAngleDegrees),
                edge.FirstEncounterStartVertexIndex,
                edge.FirstEncounterEndVertexIndex));
        }

        return new TopologyCache(cachedEdges);
    }

    private static TopologyCache BuildWireframeTopology(MeshData mesh)
    {
        var edges = new Dictionary<long, PendingEdge>(mesh.Triangles.Count * 3);

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
            cachedEdges.Add(new TopologyCacheEdge(
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                edge.SecondTriangleIndex < 0,
                edge.SecondTriangleIndex < 0 ? 180f : 0f,
                edge.SecondTriangleIndex < 0 ? EdgeSemantic.Boundary : EdgeSemantic.Unknown,
                edge.FirstEncounterStartVertexIndex,
                edge.FirstEncounterEndVertexIndex));
        }

        return new TopologyCache(cachedEdges);
    }

    private static TopologyCache BuildWeldedWireframeTopology(MeshData mesh)
    {
        if (mesh.LogicalVertexIds is not null &&
            mesh.LogicalVertexIds.Count >= mesh.Vertices.Count)
        {
            return BuildLogicalWireframeTopology(mesh);
        }

        var vertexCount = mesh.Vertices.Count;
        var weldedIds = new int[vertexCount];
        var representativeIndices = new List<int>(vertexCount);
        var vertexMap = new Dictionary<QuantizedVector3Key, int>(vertexCount);

        for (var vertexIndex = 0; vertexIndex < vertexCount; vertexIndex++)
        {
            var key = QuantizedVector3Key.From(mesh.Vertices[vertexIndex].Position);
            if (!vertexMap.TryGetValue(key, out var weldedId))
            {
                weldedId = representativeIndices.Count;
                vertexMap.Add(key, weldedId);
                representativeIndices.Add(vertexIndex);
            }

            weldedIds[vertexIndex] = weldedId;
        }

        var edges = new Dictionary<long, PendingEdge>(mesh.Triangles.Count * 3);
        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            AddWeldedEdge(edges, weldedIds, representativeIndices, triangle.A, triangle.B, triangleIndex);
            AddWeldedEdge(edges, weldedIds, representativeIndices, triangle.B, triangle.C, triangleIndex);
            AddWeldedEdge(edges, weldedIds, representativeIndices, triangle.C, triangle.A, triangleIndex);
        }

        var cachedEdges = new List<TopologyCacheEdge>(edges.Count);
        foreach (var edge in edges.Values)
        {
            cachedEdges.Add(new TopologyCacheEdge(
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                edge.SecondTriangleIndex < 0,
                edge.SecondTriangleIndex < 0 ? 180f : 0f,
                edge.SecondTriangleIndex < 0 ? EdgeSemantic.Boundary : EdgeSemantic.Unknown,
                edge.FirstEncounterStartVertexIndex,
                edge.FirstEncounterEndVertexIndex));
        }

        return new TopologyCache(cachedEdges);
    }

    private static TopologyCache BuildLogicalWireframeTopology(MeshData mesh)
    {
        var logicalIds = mesh.LogicalVertexIds!;
        var representativeIndices = new Dictionary<int, int>(mesh.Vertices.Count);
        for (var vertexIndex = 0; vertexIndex < mesh.Vertices.Count; vertexIndex++)
        {
            var logicalId = logicalIds[vertexIndex];
            representativeIndices.TryAdd(logicalId, vertexIndex);
        }

        var edges = new Dictionary<long, PendingEdge>(mesh.Triangles.Count * 3);
        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            AddLogicalEdge(edges, logicalIds, representativeIndices, triangle.A, triangle.B, triangleIndex);
            AddLogicalEdge(edges, logicalIds, representativeIndices, triangle.B, triangle.C, triangleIndex);
            AddLogicalEdge(edges, logicalIds, representativeIndices, triangle.C, triangle.A, triangleIndex);
        }

        var cachedEdges = new List<TopologyCacheEdge>(edges.Count);
        foreach (var edge in edges.Values)
        {
            cachedEdges.Add(new TopologyCacheEdge(
                edge.StartVertexIndex,
                edge.EndVertexIndex,
                edge.FirstTriangleIndex,
                edge.SecondTriangleIndex,
                edge.SecondTriangleIndex < 0,
                edge.SecondTriangleIndex < 0 ? 180f : 0f,
                edge.SecondTriangleIndex < 0 ? EdgeSemantic.Boundary : EdgeSemantic.Unknown,
                edge.FirstEncounterStartVertexIndex,
                edge.FirstEncounterEndVertexIndex));
        }

        return new TopologyCache(cachedEdges);
    }

    private static DefaultNprTopologyCache BuildDefaultPerTriangleTopology(
        MeshData mesh,
        ulong signature,
        DefaultTopologyMode mode)
    {
        var edges = new DefaultNprTopologyEdge[checked(mesh.Triangles.Count * 3)];
        var cursor = 0;
        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            edges[cursor++] = CreateDefaultEdge(triangleIndex, 0, triangle.A, triangle.B, -1, true);
            edges[cursor++] = CreateDefaultEdge(triangleIndex, 1, triangle.B, triangle.C, -1, true);
            edges[cursor++] = CreateDefaultEdge(triangleIndex, 2, triangle.C, triangle.A, -1, true);
        }

        return new DefaultNprTopologyCache(edges, mesh.Triangles.Count, mesh.Vertices.Count, signature, mode);
    }

    private static DefaultNprTopologyCache BuildDefaultSharedTopology(
        MeshData mesh,
        ulong signature,
        DefaultTopologyMode mode)
    {
        var pending = new Dictionary<long, PendingDefaultNprEdge>(NumericMath.AtLeast(mesh.Triangles.Count * 3, 4));
        for (var triangleIndex = 0; triangleIndex < mesh.Triangles.Count; triangleIndex++)
        {
            var triangle = mesh.Triangles[triangleIndex];
            AddDefaultEdge(pending, triangle.A, triangle.B, triangleIndex);
            AddDefaultEdge(pending, triangle.B, triangle.C, triangleIndex);
            AddDefaultEdge(pending, triangle.C, triangle.A, triangleIndex);
        }

        var edges = new DefaultNprTopologyEdge[pending.Count];
        var cursor = 0;
        foreach (var entry in pending.Values)
        {
            edges[cursor++] = new DefaultNprTopologyEdge(
                entry.StableId,
                entry.StartVertexIndex,
                entry.EndVertexIndex,
                entry.FirstTriangleIndex,
                entry.SecondTriangleIndex,
                entry.SecondTriangleIndex < 0,
                entry.FirstEncounterStartVertexIndex,
                entry.FirstEncounterEndVertexIndex,
                entry.SecondEncounterStartVertexIndex,
                entry.SecondEncounterEndVertexIndex);
        }

        return new DefaultNprTopologyCache(edges, mesh.Triangles.Count, mesh.Vertices.Count, signature, mode);
    }

    private static DefaultNprTopologyEdge CreateDefaultEdge(
        int triangleIndex,
        int edgeIndex,
        int a,
        int b,
        int secondTriangleIndex,
        bool isBoundary)
    {
        var stableId = unchecked((triangleIndex * 397) ^ (edgeIndex * 131) ^ a ^ (b * 17));
        return new DefaultNprTopologyEdge(
            stableId,
            a,
            b,
            triangleIndex,
            secondTriangleIndex,
            isBoundary,
            a,
            b);
    }

    private sealed record CacheEntry(MeshData Mesh, MeshAnalysisCache Cache);

    private readonly record struct WireframeTopologyKey(
        MeshHandle Handle,
        MeshWireframeTopologyMode Mode);

    private readonly record struct WireframeTopologyEntry(
        int VertexCount,
        int TriangleCount,
        ulong Signature,
        TopologyCache Topology);

    private sealed record DefaultNprTopologyEntry(
        int VertexCount,
        int TriangleCount,
        ulong Signature,
        DefaultNprTopologyCache Cache);

    private static MeshBounds CalculateBounds(MeshData mesh)
    {
        var bounds = Geometry3D.Bounds(mesh.Vertices, static vertex => vertex.Position);
        return new MeshBounds(bounds.Min, bounds.Max);
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
                totalAngle += Geometry3D.NormalAngleDegrees(vertexNormal, neighborNormal) / 180f;
            }

            vertexCurvature[vertexIndex] = NumericMath.Clamp01(totalAngle / adjacent.Count);
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

                var weight = 0.35f + NumericMath.Abs(vertexCurvature[neighborIndex] - vertexCurvature[vertexIndex]) * 0.65f;
                flow += System.Numerics.Vector3.Normalize(tangentDelta) * weight;
            }

            smoothedVertexCurvature[vertexIndex] = NumericMath.Clamp01(totalCurvature / (adjacent.Count + 1));
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
                : NumericMath.Clamp(signed / count, -1f, 1f);
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

            smoothedVertexSignedCurvature[vertexIndex] = NumericMath.Clamp(totalSigned / (adjacent.Count + 1), -1f, 1f);
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

        var meanEdgeLength = Geometry3D.MeanTriangleEdgeLength(
            mesh.Triangles,
            static triangle => triangle.A,
            static triangle => triangle.B,
            static triangle => triangle.C,
            index => mesh.Vertices[index].Position);
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
        return NumericMath.Clamp01(curvature * 0.8f + directionFactor * 0.2f);
    }

    private static CurvatureQuality ResolveCurvatureQuality(IReadOnlyList<float> smoothedVertexCurvature, IReadOnlyList<Vector3> vertexDirections)
    {
        if (smoothedVertexCurvature.Count == 0)
        {
            return CurvatureQuality.NotComputed;
        }

        var avgCurvature = smoothedVertexCurvature.Average();
        var directionalFraction = vertexDirections.Count(direction => direction.LengthSquared() > 0.0001f) / (float)NumericMath.AtLeast(vertexDirections.Count, 1);

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
        return Geometry3D.NormalizeOrDefault(normal, System.Numerics.Vector3.UnitY);
    }

    private static void AddEdge(Dictionary<long, PendingEdge> edges, int a, int b, int triangleIndex)
    {
        var key = MeshTopologyMath.CreateUndirectedEdgeKey(a, b);

        if (edges.TryGetValue(key, out var edge))
        {
            if (edge.SecondTriangleIndex < 0)
            {
                edges[key] = edge with { SecondTriangleIndex = triangleIndex };
            }

            return;
        }

        edges[key] = new PendingEdge(
            MeshTopologyMath.UndirectedEdgeStart(a, b),
            MeshTopologyMath.UndirectedEdgeEnd(a, b),
            triangleIndex,
            -1,
            a,
            b);
    }

    private static void AddDefaultEdge(
        Dictionary<long, PendingDefaultNprEdge> edges,
        int a,
        int b,
        int triangleIndex)
    {
        var min = MeshTopologyMath.UndirectedEdgeStart(a, b);
        var max = MeshTopologyMath.UndirectedEdgeEnd(a, b);
        var key = MeshTopologyMath.CreateUndirectedEdgeKey(a, b);

        if (!edges.TryGetValue(key, out var edge))
        {
            edge = new PendingDefaultNprEdge(
                unchecked((triangleIndex * 397) ^ (a * 17) ^ b),
                min,
                max,
                triangleIndex,
                -1,
                a,
                b,
                -1,
                -1);
        }
        else if (edge.SecondTriangleIndex < 0)
        {
            edge = edge with
            {
                SecondTriangleIndex = triangleIndex,
                SecondEncounterStartVertexIndex = a,
                SecondEncounterEndVertexIndex = b
            };
        }

        edges[key] = edge;
    }

    private static void AddWeldedEdge(
        Dictionary<long, PendingEdge> edges,
        int[] weldedIds,
        List<int> representativeIndices,
        int a,
        int b,
        int triangleIndex)
    {
        if ((uint)a >= (uint)weldedIds.Length ||
            (uint)b >= (uint)weldedIds.Length)
        {
            return;
        }

        var weldedA = weldedIds[a];
        var weldedB = weldedIds[b];
        if (weldedA == weldedB)
        {
            return;
        }

        var key = MeshTopologyMath.CreateUndirectedEdgeKey(weldedA, weldedB);
        if (edges.TryGetValue(key, out var edge))
        {
            if (edge.SecondTriangleIndex < 0)
            {
                edges[key] = edge with { SecondTriangleIndex = triangleIndex };
            }

            return;
        }

        var start = representativeIndices[MeshTopologyMath.UndirectedEdgeStart(weldedA, weldedB)];
        var end = representativeIndices[MeshTopologyMath.UndirectedEdgeEnd(weldedA, weldedB)];
        edges[key] = new PendingEdge(start, end, triangleIndex, -1, a, b);
    }

    private static void AddLogicalEdge(
        Dictionary<long, PendingEdge> edges,
        IReadOnlyList<int> logicalIds,
        Dictionary<int, int> representativeIndices,
        int a,
        int b,
        int triangleIndex)
    {
        if ((uint)a >= (uint)logicalIds.Count ||
            (uint)b >= (uint)logicalIds.Count)
        {
            return;
        }

        var logicalA = logicalIds[a];
        var logicalB = logicalIds[b];
        if (logicalA == logicalB)
        {
            return;
        }

        var key = MeshTopologyMath.CreateUndirectedEdgeKey(logicalA, logicalB);
        if (edges.TryGetValue(key, out var edge))
        {
            if (edge.SecondTriangleIndex < 0)
            {
                edges[key] = edge with { SecondTriangleIndex = triangleIndex };
            }

            return;
        }

        var start = representativeIndices[MeshTopologyMath.UndirectedEdgeStart(logicalA, logicalB)];
        var end = representativeIndices[MeshTopologyMath.UndirectedEdgeEnd(logicalA, logicalB)];
        edges[key] = new PendingEdge(start, end, triangleIndex, -1, a, b);
    }

    private static ulong CalculateTriangleSignature(MeshData mesh)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;

        unchecked
        {
            var hash = offset;
            hash = (hash ^ (uint)mesh.Vertices.Count) * prime;
            hash = (hash ^ (uint)mesh.Triangles.Count) * prime;

            foreach (var triangle in mesh.Triangles)
            {
                hash = (hash ^ (uint)triangle.A) * prime;
                hash = (hash ^ (uint)triangle.B) * prime;
                hash = (hash ^ (uint)triangle.C) * prime;
            }

            return hash;
        }
    }

    private static ulong CalculateWeldedTriangleSignature(MeshData mesh)
    {
        const ulong prime = 1099511628211UL;

        unchecked
        {
            var hash = CalculateTriangleSignature(mesh);
            hash = (hash ^ (mesh.LogicalVertexIds is null ? 0u : 1u)) * prime;
            if (mesh.LogicalVertexIds is null)
            {
                return hash;
            }

            foreach (var triangle in mesh.Triangles)
            {
                hash = (hash ^ ReadLogicalId(mesh.LogicalVertexIds, triangle.A)) * prime;
                hash = (hash ^ ReadLogicalId(mesh.LogicalVertexIds, triangle.B)) * prime;
                hash = (hash ^ ReadLogicalId(mesh.LogicalVertexIds, triangle.C)) * prime;
            }

            return hash;
        }

        static uint ReadLogicalId(IReadOnlyList<int> logicalIds, int index)
        {
            return (uint)((uint)index < (uint)logicalIds.Count ? logicalIds[index] : index);
        }
    }

    private readonly record struct PendingEdge(
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex,
        int FirstEncounterStartVertexIndex,
        int FirstEncounterEndVertexIndex);

    private readonly record struct PendingDefaultNprEdge(
        int StableId,
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex,
        int FirstEncounterStartVertexIndex,
        int FirstEncounterEndVertexIndex,
        int SecondEncounterStartVertexIndex,
        int SecondEncounterEndVertexIndex);

}
