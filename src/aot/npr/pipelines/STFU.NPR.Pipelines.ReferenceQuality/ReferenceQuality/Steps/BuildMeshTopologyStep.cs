using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Math;
using STFU.NPR.Analysis;
using STFU.NPR.Graph;
using STFU.NPR.Settings;
using STFU.Parallelism;

namespace STFU.NPR.Pipeline.ReferenceQuality.Steps;

public sealed class BuildMeshTopologyStep : STFU.NPR.Pipeline.INprStep
{
    private TopologyEdge[] _topologyEdgeScratch = [];
    private ProjectedEdge[] _projectedEdgeScratch = [];
    private byte[] _edgeFlags = [];
    private int[] _edgeOffsets = [];
    private int[] _sourceTriangleToGraphIndex = [];

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        if (context.Settings.DefaultDrawing.TopologyMode == DefaultTopologyMode.PerTriangleEdges)
        {
            BuildPerTriangleEdges(context);
            return;
        }

        BuildSharedTopologyFromCache(context);
    }

    private void BuildPerTriangleEdges(STFU.NPR.Pipeline.NprContext context)
    {
        var triangleCount = context.Graph.Triangles.Count;
        if (triangleCount == 0)
        {
            context.Counters.Set("BuildMeshTopologyStep.sourceTriangles", 0);
            context.Counters.Set("BuildMeshTopologyStep.perTriangleEdges", 0);
            context.Counters.Set("BuildMeshTopologyStep.sharedEdges", 0);
            context.Counters.Set("BuildMeshTopologyStep.boundaryEdges", 0);
            context.Counters.Set("BuildMeshTopologyStep.denseMode", 1);
            return;
        }

        BuildDensePerTriangleEdges(context, triangleCount);
    }

    private void BuildDensePerTriangleEdges(STFU.NPR.Pipeline.NprContext context, int triangleCount)
    {
        var edgeCapacity = checked(triangleCount * 3);
        var vertices = context.Graph.Vertices;
        var triangles = context.Graph.Triangles;
        var topologyEdges = context.Graph.TopologyEdges;
        var projectedEdges = context.Graph.Edges;
        var initialTopologyCount = topologyEdges.Count;
        var initialEdgeCount = projectedEdges.Count;

        topologyEdges.EnsureCapacity(initialTopologyCount + edgeCapacity);
        projectedEdges.EnsureCapacity(initialEdgeCount + edgeCapacity);
        CollectionsMarshal.SetCount(topologyEdges, initialTopologyCount + edgeCapacity);
        CollectionsMarshal.SetCount(projectedEdges, initialEdgeCount + edgeCapacity);

        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var topologySpan = CollectionsMarshal.AsSpan(topologyEdges);
                var projectedEdgeSpan = CollectionsMarshal.AsSpan(projectedEdges);
                var vertexSpan = CollectionsMarshal.AsSpan(vertices);
                var triangleSpan = CollectionsMarshal.AsSpan(triangles);

                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var triangle = triangleSpan[triangleIndex];
                    var outputBaseIndex = triangleIndex * 3;

                    WritePerTriangleEdge(
                        topologySpan,
                        projectedEdgeSpan,
                        initialTopologyCount + outputBaseIndex,
                        initialEdgeCount + outputBaseIndex,
                        vertexSpan,
                        triangle,
                        triangleIndex,
                        0,
                        triangle.A,
                        triangle.B);
                    WritePerTriangleEdge(
                        topologySpan,
                        projectedEdgeSpan,
                        initialTopologyCount + outputBaseIndex + 1,
                        initialEdgeCount + outputBaseIndex + 1,
                        vertexSpan,
                        triangle,
                        triangleIndex,
                        1,
                        triangle.B,
                        triangle.C);
                    WritePerTriangleEdge(
                        topologySpan,
                        projectedEdgeSpan,
                        initialTopologyCount + outputBaseIndex + 2,
                        initialEdgeCount + outputBaseIndex + 2,
                        vertexSpan,
                        triangle,
                        triangleIndex,
                        2,
                        triangle.C,
                        triangle.A);
                }
            },
            minItemsPerRange: 512);

        context.Counters.Set("BuildMeshTopologyStep.sourceTriangles", triangleCount);
        context.Counters.Set("BuildMeshTopologyStep.perTriangleEdges", edgeCapacity);
        context.Counters.Set("BuildMeshTopologyStep.sharedEdges", 0);
        context.Counters.Set("BuildMeshTopologyStep.boundaryEdges", edgeCapacity);
        context.Counters.Set("BuildMeshTopologyStep.topologyEdges", edgeCapacity);
        context.Counters.Set("BuildMeshTopologyStep.projectedEdges", edgeCapacity);
        context.Counters.Set("BuildMeshTopologyStep.cacheMode", 0);
        context.Counters.Set("BuildMeshTopologyStep.denseMode", 1);
    }

    private void BuildSparsePerTriangleEdges(STFU.NPR.Pipeline.NprContext context, int triangleCount)
    {
        var edgeCapacity = checked(triangleCount * 3);
        EnsureScratchCapacity(edgeCapacity);

        var vertices = context.Graph.Vertices;
        var triangles = context.Graph.Triangles;

        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var triangle = triangles[triangleIndex];
                    var baseIndex = triangleIndex * 3;
                    TryBuildPerTriangleEdge(vertices, triangle, triangleIndex, 0, triangle.A, triangle.B, baseIndex + 0);
                    TryBuildPerTriangleEdge(vertices, triangle, triangleIndex, 1, triangle.B, triangle.C, baseIndex + 1);
                    TryBuildPerTriangleEdge(vertices, triangle, triangleIndex, 2, triangle.C, triangle.A, baseIndex + 2);
                }
            },
            minItemsPerRange: 512);

        var edgeCount = PrefixSums.ExclusiveFromFlags(
            _edgeFlags.AsSpan(0, edgeCapacity),
            _edgeOffsets.AsSpan(0, edgeCapacity));

        context.Graph.TopologyEdges.EnsureCapacity(context.Graph.TopologyEdges.Count + edgeCount);
        context.Graph.Edges.EnsureCapacity(context.Graph.Edges.Count + edgeCount);

        var initialTopologyCount = context.Graph.TopologyEdges.Count;
        var initialEdgeCount = context.Graph.Edges.Count;
        CollectionsMarshal.SetCount(context.Graph.TopologyEdges, initialTopologyCount + edgeCount);
        CollectionsMarshal.SetCount(context.Graph.Edges, initialEdgeCount + edgeCount);

        DeterministicParallel.ForRanges(
            0,
            triangleCount,
            context.WorkerCount,
            context.CancellationToken,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (var triangleIndex = startInclusive; triangleIndex < endExclusive; triangleIndex++)
                {
                    if ((triangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    var baseIndex = triangleIndex * 3;
                    CopyEdgeIfPresent(context, initialTopologyCount, initialEdgeCount, baseIndex + 0);
                    CopyEdgeIfPresent(context, initialTopologyCount, initialEdgeCount, baseIndex + 1);
                    CopyEdgeIfPresent(context, initialTopologyCount, initialEdgeCount, baseIndex + 2);
                }
            },
            minItemsPerRange: 512);

        context.Counters.Set("BuildMeshTopologyStep.sourceTriangles", triangleCount);
        context.Counters.Set("BuildMeshTopologyStep.perTriangleEdges", edgeCount);
        context.Counters.Set("BuildMeshTopologyStep.sharedEdges", 0);
        context.Counters.Set("BuildMeshTopologyStep.boundaryEdges", edgeCount);
        context.Counters.Set("BuildMeshTopologyStep.topologyEdges", edgeCount);
        context.Counters.Set("BuildMeshTopologyStep.projectedEdges", edgeCount);
        context.Counters.Set("BuildMeshTopologyStep.cacheMode", 0);
        context.Counters.Set("BuildMeshTopologyStep.denseMode", 0);
    }

    private void BuildSharedTopologyFromCache(STFU.NPR.Pipeline.NprContext context)
    {
        var projectedMeshes = context.Graph.Meshes;
        var vertices = context.Graph.Vertices;
        var triangles = context.Graph.Triangles;
        var topologyEdges = context.Graph.TopologyEdges;
        var projectedEdges = context.Graph.Edges;
        var totalCachedEdges = 0;
        var maxSourceTriangleIndex = 0;

        for (var meshIndex = 0; meshIndex < projectedMeshes.Count; meshIndex++)
        {
            var projectedMesh = projectedMeshes[meshIndex];
            var cache = context.Analysis.GetOrCreateDefaultNprTopology(
                projectedMesh.MeshHandle,
                projectedMesh.Mesh,
                DefaultTopologyMode.SharedTopology);
            totalCachedEdges += cache.Edges.Count;
            maxSourceTriangleIndex = NumericMath.AtLeast(
                maxSourceTriangleIndex,
                projectedMesh.TriangleOffset + projectedMesh.TriangleCount);
        }

        EnsureSourceTriangleMapCapacity(maxSourceTriangleIndex);
        if (maxSourceTriangleIndex > 0)
        {
            Array.Fill(_sourceTriangleToGraphIndex, -1, 0, maxSourceTriangleIndex);
        }

        for (var triangleIndex = 0; triangleIndex < triangles.Count; triangleIndex++)
        {
            var triangle = triangles[triangleIndex];
            if ((uint)triangle.ProjectedMeshIndex >= (uint)projectedMeshes.Count)
            {
                continue;
            }

            var projectedMesh = projectedMeshes[triangle.ProjectedMeshIndex];
            var mapIndex = projectedMesh.TriangleOffset + triangle.MeshTriangleIndex;
            if ((uint)mapIndex < (uint)maxSourceTriangleIndex)
            {
                _sourceTriangleToGraphIndex[mapIndex] = triangleIndex;
            }
        }

        topologyEdges.EnsureCapacity(topologyEdges.Count + totalCachedEdges);
        projectedEdges.EnsureCapacity(projectedEdges.Count + totalCachedEdges);

        var addedEdges = 0;
        var boundaryEdges = 0;
        for (var meshIndex = 0; meshIndex < projectedMeshes.Count; meshIndex++)
        {
            var projectedMesh = projectedMeshes[meshIndex];
            var cache = context.Analysis.GetOrCreateDefaultNprTopology(
                projectedMesh.MeshHandle,
                projectedMesh.Mesh,
                DefaultTopologyMode.SharedTopology);

            for (var edgeIndex = 0; edgeIndex < cache.Edges.Count; edgeIndex++)
            {
                var cached = cache.Edges[edgeIndex];
                var firstTriangleIndex = ResolveGraphTriangleIndex(projectedMesh, cached.FirstTriangleIndex, maxSourceTriangleIndex);
                var secondTriangleIndex = cached.SecondTriangleIndex >= 0
                    ? ResolveGraphTriangleIndex(projectedMesh, cached.SecondTriangleIndex, maxSourceTriangleIndex)
                    : -1;

                if (firstTriangleIndex < 0 && secondTriangleIndex < 0)
                {
                    continue;
                }

                var visibleFirstTriangle = firstTriangleIndex >= 0;
                var ownerTriangleIndex = visibleFirstTriangle ? firstTriangleIndex : secondTriangleIndex;
                var adjacentTriangleIndex = visibleFirstTriangle && secondTriangleIndex >= 0
                    ? secondTriangleIndex
                    : -1;

                var startVertexIndex = projectedMesh.VertexOffset + cached.StartVertexIndex;
                var endVertexIndex = projectedMesh.VertexOffset + cached.EndVertexIndex;
                if ((uint)startVertexIndex >= (uint)vertices.Count ||
                    (uint)endVertexIndex >= (uint)vertices.Count ||
                    (uint)ownerTriangleIndex >= (uint)triangles.Count)
                {
                    continue;
                }

                var firstTriangle = triangles[ownerTriangleIndex];
                var secondTriangle = adjacentTriangleIndex >= 0 && adjacentTriangleIndex < triangles.Count
                    ? triangles[adjacentTriangleIndex]
                    : default;
                var angleDegrees = adjacentTriangleIndex >= 0
                    ? Geometry3D.NormalAngleDegrees(firstTriangle.Normal, secondTriangle.Normal)
                    : 180f;
                var a = vertices[startVertexIndex];
                var b = vertices[endVertexIndex];
                var depth = (a.Depth + b.Depth) * 0.5f;
                var stableStartVertexIndex = projectedMesh.VertexOffset + ResolveEncounterStart(cached, visibleFirstTriangle);
                var stableEndVertexIndex = projectedMesh.VertexOffset + ResolveEncounterEnd(cached, visibleFirstTriangle);
                var stableId = HashMath.StableUndirectedEdge(firstTriangle.StableId, stableStartVertexIndex, stableEndVertexIndex);
                var isBoundary = adjacentTriangleIndex < 0;

                topologyEdges.Add(new TopologyEdge(
                    stableId,
                    startVertexIndex,
                    endVertexIndex,
                    ownerTriangleIndex,
                    adjacentTriangleIndex,
                    angleDegrees,
                    isBoundary)
                {
                    EntityId = firstTriangle.EntityId
                });

                projectedEdges.Add(new ProjectedEdge(stableId, startVertexIndex, endVertexIndex, depth));
                addedEdges++;
                if (isBoundary)
                {
                    boundaryEdges++;
                }
            }
        }

        context.Counters.Set("BuildMeshTopologyStep.sourceTriangles", triangles.Count);
        context.Counters.Set("BuildMeshTopologyStep.perTriangleEdges", triangles.Count * 3);
        context.Counters.Set("BuildMeshTopologyStep.sharedEdges", addedEdges);
        context.Counters.Set("BuildMeshTopologyStep.boundaryEdges", boundaryEdges);
        context.Counters.Set("BuildMeshTopologyStep.topologyEdges", addedEdges);
        context.Counters.Set("BuildMeshTopologyStep.projectedEdges", addedEdges);
        context.Counters.Set("BuildMeshTopologyStep.cacheMode", 1);
        context.Counters.Set("BuildMeshTopologyStep.denseMode", 0);
        context.Counters.Set("BuildMeshTopologyStep.cached", 1);
        context.Counters.Set("BuildMeshTopologyStep.cachedCandidateEdges", totalCachedEdges);
    }

    private int ResolveGraphTriangleIndex(ProjectedMesh projectedMesh, int sourceTriangleIndex, int mapLength)
    {
        var mapIndex = projectedMesh.TriangleOffset + sourceTriangleIndex;
        return (uint)mapIndex < (uint)mapLength
            ? _sourceTriangleToGraphIndex[mapIndex]
            : -1;
    }

    private void EnsureSourceTriangleMapCapacity(int required)
    {
        if (_sourceTriangleToGraphIndex.Length < required)
        {
            _sourceTriangleToGraphIndex = new int[required];
        }
    }

    private static int ResolveEncounterStart(DefaultNprTopologyEdge edge, bool firstTriangleVisible)
    {
        if (firstTriangleVisible)
        {
            return edge.FirstEncounterStartVertexIndex >= 0
                ? edge.FirstEncounterStartVertexIndex
                : edge.StartVertexIndex;
        }

        return edge.SecondEncounterStartVertexIndex >= 0
            ? edge.SecondEncounterStartVertexIndex
            : edge.StartVertexIndex;
    }

    private static int ResolveEncounterEnd(DefaultNprTopologyEdge edge, bool firstTriangleVisible)
    {
        if (firstTriangleVisible)
        {
            return edge.FirstEncounterEndVertexIndex >= 0
                ? edge.FirstEncounterEndVertexIndex
                : edge.EndVertexIndex;
        }

        return edge.SecondEncounterEndVertexIndex >= 0
            ? edge.SecondEncounterEndVertexIndex
            : edge.EndVertexIndex;
    }

    private void BuildSharedTopology(STFU.NPR.Pipeline.NprContext context)
    {
        var edges = new Dictionary<long, EdgeInfo>();
        var triangleCount = context.Graph.Triangles.Count;

        for (var triangleIndex = 0; triangleIndex < triangleCount; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];
            AddEdge(edges, triangle, triangle.A, triangle.B, triangleIndex);
            AddEdge(edges, triangle, triangle.B, triangle.C, triangleIndex);
            AddEdge(edges, triangle, triangle.C, triangle.A, triangleIndex);
        }

        var boundaryEdges = 0;
        foreach (var entry in edges.Values)
        {
            var firstTriangle = context.Graph.Triangles[entry.FirstTriangleIndex];
            var secondTriangle = entry.SecondTriangleIndex >= 0
                ? context.Graph.Triangles[entry.SecondTriangleIndex]
                : default;

            var angleDegrees = entry.SecondTriangleIndex >= 0
                ? Geometry3D.NormalAngleDegrees(firstTriangle.Normal, secondTriangle.Normal)
                : 180f;

            var a = context.Graph.Vertices[entry.StartVertexIndex];
            var b = context.Graph.Vertices[entry.EndVertexIndex];
            var depth = (a.Depth + b.Depth) * 0.5f;

            context.Graph.TopologyEdges.Add(new TopologyEdge(
                entry.StableId,
                entry.StartVertexIndex,
                entry.EndVertexIndex,
                entry.FirstTriangleIndex,
                entry.SecondTriangleIndex,
                angleDegrees,
                entry.SecondTriangleIndex < 0)
            {
                EntityId = firstTriangle.EntityId
            });

            context.Graph.Edges.Add(new ProjectedEdge(entry.StableId, entry.StartVertexIndex, entry.EndVertexIndex, depth));
            if (entry.SecondTriangleIndex < 0)
            {
                boundaryEdges++;
            }
        }

        context.Counters.Set("BuildMeshTopologyStep.sourceTriangles", triangleCount);
        context.Counters.Set("BuildMeshTopologyStep.perTriangleEdges", triangleCount * 3);
        context.Counters.Set("BuildMeshTopologyStep.sharedEdges", edges.Count);
        context.Counters.Set("BuildMeshTopologyStep.boundaryEdges", boundaryEdges);
        context.Counters.Set("BuildMeshTopologyStep.topologyEdges", edges.Count);
        context.Counters.Set("BuildMeshTopologyStep.projectedEdges", edges.Count);
        context.Counters.Set("BuildMeshTopologyStep.cacheMode", 0);
        context.Counters.Set("BuildMeshTopologyStep.denseMode", 0);
    }

    private void EnsureScratchCapacity(int edgeCapacity)
    {
        var capacity = GrowCapacity(edgeCapacity);
        if (_topologyEdgeScratch.Length < edgeCapacity)
        {
            _topologyEdgeScratch = new TopologyEdge[capacity];
        }

        if (_projectedEdgeScratch.Length < edgeCapacity)
        {
            _projectedEdgeScratch = new ProjectedEdge[capacity];
        }

        if (_edgeFlags.Length < edgeCapacity)
        {
            _edgeFlags = new byte[capacity];
        }

        if (_edgeOffsets.Length < edgeCapacity)
        {
            _edgeOffsets = new int[capacity];
        }
    }

    private static int GrowCapacity(int required)
    {
        var capacity = 4;
        while (capacity < required)
        {
            capacity = checked(capacity + (capacity >> 1));
        }

        return capacity;
    }

    private void TryBuildPerTriangleEdge(
        IReadOnlyList<ProjectedVertex> vertices,
        ProjectedTriangle triangle,
        int triangleIndex,
        int edgeIndex,
        int aIndex,
        int bIndex,
        int slotIndex)
    {
        if ((uint)aIndex >= (uint)vertices.Count ||
            (uint)bIndex >= (uint)vertices.Count)
        {
            _edgeFlags[slotIndex] = 0;
            return;
        }

        var a = vertices[aIndex];
        var b = vertices[bIndex];
        var depth = (a.Depth + b.Depth) * 0.5f;
        var stableId = HashMath.StablePerTriangleEdge(triangle.StableId, edgeIndex, aIndex, bIndex);

        _topologyEdgeScratch[slotIndex] = new TopologyEdge(
            stableId,
            aIndex,
            bIndex,
            triangleIndex,
            -1,
            180f,
            true)
        {
            EntityId = triangle.EntityId
        };

        _projectedEdgeScratch[slotIndex] = new ProjectedEdge(stableId, aIndex, bIndex, depth);
        _edgeFlags[slotIndex] = 1;
    }

    private static void WritePerTriangleEdge(
        Span<TopologyEdge> topologyEdges,
        Span<ProjectedEdge> projectedEdges,
        int topologyOutputIndex,
        int projectedEdgeOutputIndex,
        ReadOnlySpan<ProjectedVertex> vertices,
        ProjectedTriangle triangle,
        int triangleIndex,
        int edgeIndex,
        int aIndex,
        int bIndex)
    {
        var a = vertices[aIndex];
        var b = vertices[bIndex];
        var depth = (a.Depth + b.Depth) * 0.5f;
        var stableId = HashMath.StablePerTriangleEdge(triangle.StableId, edgeIndex, aIndex, bIndex);

        topologyEdges[topologyOutputIndex] = new TopologyEdge(
            stableId,
            aIndex,
            bIndex,
            triangleIndex,
            -1,
            180f,
            true)
        {
            EntityId = triangle.EntityId
        };

        projectedEdges[projectedEdgeOutputIndex] = new ProjectedEdge(stableId, aIndex, bIndex, depth);
    }

    private void CopyEdgeIfPresent(
        STFU.NPR.Pipeline.NprContext context,
        int initialTopologyCount,
        int initialEdgeCount,
        int slotIndex)
    {
        if (_edgeFlags[slotIndex] == 0)
        {
            return;
        }

        var outputIndex = initialTopologyCount + _edgeOffsets[slotIndex];
        context.Graph.TopologyEdges[outputIndex] = _topologyEdgeScratch[slotIndex];
        context.Graph.Edges[initialEdgeCount + _edgeOffsets[slotIndex]] = _projectedEdgeScratch[slotIndex];
    }

    private static void AddEdge(Dictionary<long, EdgeInfo> edges, ProjectedTriangle triangle, int a, int b, int triangleIndex)
    {
        var min = MeshTopologyMath.UndirectedEdgeStart(a, b);
        var max = MeshTopologyMath.UndirectedEdgeEnd(a, b);
        var key = MeshTopologyMath.CreateUndirectedEdgeKey(a, b);

        if (!edges.TryGetValue(key, out var info))
        {
            info = new EdgeInfo(HashMath.StableUndirectedEdge(triangle.StableId, a, b), min, max, triangleIndex, -1);
        }
        else if (info.SecondTriangleIndex < 0)
        {
            info = info with { SecondTriangleIndex = triangleIndex };
        }

        edges[key] = info;
    }

    private readonly record struct EdgeInfo(
        int StableId,
        int StartVertexIndex,
        int EndVertexIndex,
        int FirstTriangleIndex,
        int SecondTriangleIndex);
}