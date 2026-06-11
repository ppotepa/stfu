using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Math;
using STFU.Mesh;
using STFU.NPR.Graph;
using STFU.Parallelism;
using STFU.Strokes;

namespace STFU.NPR.Pipeline.ReferenceQuality.Steps;

public sealed class BuildProjectedTrianglesStep : STFU.NPR.Pipeline.INprStep
{
    private ProjectedTriangle[] _stagedTriangles = [];
    private byte[] _triangleFlags = [];
    private int[] _triangleOffsets = [];
    private TriangleBuildMeshJob[] _meshJobs = [];
    private int[] _rangeRejectedInvalid = [];
    private int[] _rangeRejectedArea = [];
    private int[] _rangeRejectedInvisible = [];

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var initialTriangleCount = context.Graph.Triangles.Count;
        var cameraPosition = context.Camera.Position;
        var minimumProjectedTriangleArea = context.Settings.MinimumProjectedTriangleArea;
        var totalTriangleCount = 0;
        var meshCount = context.Graph.Meshes.Count;

        EnsureMeshJobCapacity(meshCount);
        for (var meshIndex = 0; meshIndex < meshCount; meshIndex++)
        {
            var projectedMesh = context.Graph.Meshes[meshIndex];
            _meshJobs[meshIndex] = new TriangleBuildMeshJob(
                meshIndex,
                projectedMesh.TriangleOffset,
                projectedMesh.TriangleCount,
                projectedMesh,
                ResolveTriangles(projectedMesh.Mesh));
            totalTriangleCount += projectedMesh.TriangleCount;
        }

        context.Counters.Set("BuildProjectedTrianglesStep.sourceMeshes", meshCount);
        context.Counters.Set("BuildProjectedTrianglesStep.sourceTriangles", totalTriangleCount);
        context.Counters.Set("BuildProjectedTrianglesStep.rangeCount", totalTriangleCount > 0
            ? DeterministicParallel.GetRangeCount(totalTriangleCount, context.WorkerCount, 512)
            : 0);

        if (totalTriangleCount <= 0)
        {
            context.Counters.Set("BuildProjectedTrianglesStep.outputTriangles", 0);
            context.Counters.Set("BuildProjectedTrianglesStep.culledTriangles", 0);
            context.Counters.Set("BuildProjectedTrianglesStep.rejectedTriangles", 0);
            context.Counters.Set("BuildProjectedTrianglesStep.triangleScratchCapacity", _stagedTriangles.Length);
            context.Counters.Set("BuildProjectedTrianglesStep.rejectedInvalidIndex", 0);
            context.Counters.Set("BuildProjectedTrianglesStep.rejectedArea", 0);
            context.Counters.Set("BuildProjectedTrianglesStep.rejectedInvisible", 0);
            return;
        }

        EnsureScratchCapacity(totalTriangleCount);
        var rangeCount = DeterministicParallel.GetRangeCount(totalTriangleCount, context.WorkerCount, 512);
        EnsureRejectScratchCapacity(rangeCount);

        BuildTriangles(
            context,
            meshCount,
            totalTriangleCount,
            cameraPosition,
            minimumProjectedTriangleArea,
            context.Graph.Vertices,
            rangeCount);

        var writeCount = PrefixSums.ExclusiveFromFlags(
            _triangleFlags.AsSpan(0, totalTriangleCount),
            _triangleOffsets.AsSpan(0, totalTriangleCount));

        context.Graph.Triangles.EnsureCapacity(initialTriangleCount + writeCount);
        CollectionsMarshal.SetCount(context.Graph.Triangles, initialTriangleCount + writeCount);
        var graphTriangles = context.Graph.Triangles;

        NprParallelTrace.ForRanges(
            context,
            "BuildProjectedTrianglesStep.Copy",
            0,
            totalTriangleCount,
            (startInclusive, endExclusive, _, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var graphTriangleSpan = CollectionsMarshal.AsSpan(graphTriangles);
                var staged = _stagedTriangles.AsSpan();
                var flags = _triangleFlags.AsSpan();
                var offsets = _triangleOffsets.AsSpan();
                for (var globalTriangleIndex = startInclusive; globalTriangleIndex < endExclusive; globalTriangleIndex++)
                {
                    if ((globalTriangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (flags[globalTriangleIndex] == 0)
                    {
                        continue;
                    }

                    graphTriangleSpan[initialTriangleCount + offsets[globalTriangleIndex]] =
                        staged[globalTriangleIndex];
                }
            },
            minItemsPerRange: 512);

        context.Counters.Set("BuildProjectedTrianglesStep.outputTriangles", writeCount);
        context.Counters.Set("BuildProjectedTrianglesStep.culledTriangles", totalTriangleCount - writeCount);
        context.Counters.Set("BuildProjectedTrianglesStep.rejectedTriangles", totalTriangleCount - writeCount);
        context.Counters.Set("BuildProjectedTrianglesStep.triangleScratchCapacity", _stagedTriangles.Length);
        context.Counters.Set("BuildProjectedTrianglesStep.rejectedInvalidIndex", SumRangeCounters(_rangeRejectedInvalid, rangeCount));
        context.Counters.Set("BuildProjectedTrianglesStep.rejectedArea", SumRangeCounters(_rangeRejectedArea, rangeCount));
        context.Counters.Set("BuildProjectedTrianglesStep.rejectedInvisible", SumRangeCounters(_rangeRejectedInvisible, rangeCount));
    }

    private void BuildTriangles(
        STFU.NPR.Pipeline.NprContext context,
        int meshJobCount,
        int totalTriangleCount,
        Vector3 cameraPosition,
        float minimumProjectedTriangleArea,
        List<ProjectedVertex> graphVertices,
        int rangeCount)
    {
        Array.Clear(_rangeRejectedInvalid, 0, rangeCount);
        Array.Clear(_rangeRejectedArea, 0, rangeCount);
        Array.Clear(_rangeRejectedInvisible, 0, rangeCount);
        NprParallelTrace.ForRanges(
            context,
            "BuildProjectedTrianglesStep.Build",
            0,
            totalTriangleCount,
            (startInclusive, endExclusive, rangeIndex, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var meshJobIndex = FindMeshJobIndex(startInclusive, meshJobCount);
                var meshJob = _meshJobs[meshJobIndex];
                var graphVertexSpan = CollectionsMarshal.AsSpan(graphVertices);
                var rejectedInvalidIndex = 0;
                var rejectedArea = 0;
                var rejectedInvisible = 0;

                for (var globalTriangleIndex = startInclusive; globalTriangleIndex < endExclusive; globalTriangleIndex++)
                {
                    if ((globalTriangleIndex & 0x3FF) == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    while (globalTriangleIndex >= meshJob.SourceTriangleStart + meshJob.SourceTriangleCount)
                    {
                        meshJobIndex++;
                        if (meshJobIndex >= meshJobCount)
                        {
                            throw new InvalidOperationException("Projected triangle job scan overflowed the active mesh job range.");
                        }

                        meshJob = _meshJobs[meshJobIndex];
                    }

                    var triangleIndex = globalTriangleIndex - meshJob.SourceTriangleStart;
                    var triangle = meshJob.GetTriangle(triangleIndex);
                    if (TryBuildTriangle(
                            cameraPosition,
                            minimumProjectedTriangleArea,
                            meshJob.ProjectedMesh,
                            meshJob.ProjectedMeshIndex,
                            triangleIndex,
                            triangle,
                            graphVertexSpan,
                            out var rejectReason,
                            out var staged))
                    {
                        _stagedTriangles[globalTriangleIndex] = staged;
                        _triangleFlags[globalTriangleIndex] = 1;
                    }
                    else
                    {
                        _triangleFlags[globalTriangleIndex] = 0;
                        switch (rejectReason)
                        {
                            case TriangleRejectReason.InvalidIndex:
                                rejectedInvalidIndex++;
                                break;
                            case TriangleRejectReason.Area:
                                rejectedArea++;
                                break;
                            case TriangleRejectReason.Invisible:
                                rejectedInvisible++;
                                break;
                        }
                    }
                }

                _rangeRejectedInvalid[rangeIndex] = rejectedInvalidIndex;
                _rangeRejectedArea[rangeIndex] = rejectedArea;
                _rangeRejectedInvisible[rangeIndex] = rejectedInvisible;
            },
            minItemsPerRange: 512);
    }

    private int FindMeshJobIndex(int globalTriangleIndex, int meshJobCount)
    {
        var low = 0;
        var high = meshJobCount - 1;

        while (low <= high)
        {
            var mid = low + ((high - low) >> 1);
            var job = _meshJobs[mid];

            if (globalTriangleIndex < job.SourceTriangleStart)
            {
                high = mid - 1;
                continue;
            }

            if (globalTriangleIndex >= job.SourceTriangleStart + job.SourceTriangleCount)
            {
                low = mid + 1;
                continue;
            }

            return mid;
        }

        throw new InvalidOperationException($"Could not resolve projected mesh job for triangle index {globalTriangleIndex}.");
    }

    private void EnsureScratchCapacity(int required)
    {
        if (_stagedTriangles.Length < required)
        {
            _stagedTriangles = new ProjectedTriangle[required];
        }

        if (_triangleFlags.Length < required)
        {
            _triangleFlags = new byte[required];
        }

        if (_triangleOffsets.Length < required)
        {
            _triangleOffsets = new int[required];
        }
    }

    private void EnsureMeshJobCapacity(int required)
    {
        if (_meshJobs.Length < required)
        {
            _meshJobs = new TriangleBuildMeshJob[required];
        }
    }

    private void EnsureRejectScratchCapacity(int rangeCount)
    {
        if (_rangeRejectedInvalid.Length < rangeCount)
        {
            _rangeRejectedInvalid = new int[rangeCount];
            _rangeRejectedArea = new int[rangeCount];
            _rangeRejectedInvisible = new int[rangeCount];
        }
    }

    private static int SumRangeCounters(int[] counters, int count)
    {
        var total = 0;
        for (var i = 0; i < count; i++)
        {
            total += counters[i];
        }

        return total;
    }

    private static IReadOnlyList<MeshTriangle> ResolveTriangles(MeshData mesh)
    {
        return mesh.Triangles;
    }

    private static bool TryBuildTriangle(
        Vector3 cameraPosition,
        float minimumProjectedTriangleArea,
        ProjectedMesh projectedMesh,
        int meshIndex,
        int triangleIndex,
        MeshTriangle triangle,
        ReadOnlySpan<ProjectedVertex> graphVertices,
        out TriangleRejectReason rejectReason,
        out ProjectedTriangle projectedTriangle)
    {
        var aIndex = projectedMesh.VertexOffset + triangle.A;
        var bIndex = projectedMesh.VertexOffset + triangle.B;
        var cIndex = projectedMesh.VertexOffset + triangle.C;

        if ((uint)aIndex >= (uint)graphVertices.Length ||
            (uint)bIndex >= (uint)graphVertices.Length ||
            (uint)cIndex >= (uint)graphVertices.Length)
        {
            rejectReason = TriangleRejectReason.InvalidIndex;
            projectedTriangle = default;
            return false;
        }

        var a = graphVertices[aIndex];
        var b = graphVertices[bIndex];
        var c = graphVertices[cIndex];

        var normal = Geometry3D.TriangleNormal(a.WorldPosition, b.WorldPosition, c.WorldPosition, Vector3.UnitZ, 1e-12f);
        var worldCenter = (a.WorldPosition + b.WorldPosition + c.WorldPosition) / 3f;
        var depth = (a.Depth + b.Depth + c.Depth) / 3f;
        var screenArea = Geometry2D.SignedTriangleArea(
            a.Position.X,
            a.Position.Y,
            b.Position.X,
            b.Position.Y,
            c.Position.X,
            c.Position.Y);
        var absScreenArea = NumericMath.Abs(screenArea);
        var frontFacing = Geometry3D.IsFrontFacing(
            normal,
            worldCenter,
            cameraPosition,
            epsilonSquared: 1e-6f,
            degenerateResult: false,
            normalizeViewDirection: false);
        var visible = absScreenArea >= minimumProjectedTriangleArea &&
            (a.IsVisible || b.IsVisible || c.IsVisible);
        if (absScreenArea < minimumProjectedTriangleArea)
        {
            rejectReason = TriangleRejectReason.Area;
            projectedTriangle = default;
            return false;
        }

        if (!(a.IsVisible || b.IsVisible || c.IsVisible))
        {
            rejectReason = TriangleRejectReason.Invisible;
            projectedTriangle = default;
            return false;
        }

        projectedTriangle = new ProjectedTriangle(
            StableId: projectedMesh.TriangleOffset + triangleIndex,
            ProjectedMeshIndex: meshIndex,
            MeshTriangleIndex: triangleIndex,
            A: aIndex,
            B: bIndex,
            C: cIndex,
            Normal: normal,
            WorldCenter: default,
            ScreenCenter: default,
            Depth: depth,
            ScreenArea: absScreenArea,
            Shade: 0f,
            IsFrontFacing: frontFacing,
            IsVisible: visible)
        {
            EntityId = projectedMesh.EntityId
        };
        rejectReason = TriangleRejectReason.None;
        return true;
    }

    private enum TriangleRejectReason : byte
    {
        None = 0,
        InvalidIndex = 1,
        Area = 2,
        Invisible = 3
    }

    private readonly record struct TriangleBuildMeshJob(
        int ProjectedMeshIndex,
        int SourceTriangleStart,
        int SourceTriangleCount,
        ProjectedMesh ProjectedMesh,
        IReadOnlyList<MeshTriangle> Triangles)
    {
        public MeshTriangle GetTriangle(int triangleIndex)
        {
            return Triangles[triangleIndex];
        }
    }
}
