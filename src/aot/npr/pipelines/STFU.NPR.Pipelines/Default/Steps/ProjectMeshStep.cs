using System.Numerics;
using System.Runtime.InteropServices;
using STFU.Common.Primitives;
using STFU.Common.Math;
using STFU.Mesh;
using STFU.NPR.Analysis;
using STFU.NPR.Graph;
using STFU.NPR.Projection;
using STFU.Parallelism;

namespace STFU.NPR.Pipeline.Default.Steps;

public sealed class ProjectMeshStep : STFU.NPR.Pipeline.INprStep
{
    private const float RotationEpsilonSquared = 0.0000001f;
    private const float ScaleEpsilonSquared = 0.0000001f;
    private const float TranslationEpsilonSquared = 0.0000001f;
    private readonly List<MeshProjectionJob> _jobs = [];
    private ProjectedVertex[] _projectedVertices = [];

    public void Execute(STFU.NPR.Pipeline.NprContext context)
    {
        var projection = context.View.Projection;
        _jobs.Clear();
        _jobs.EnsureCapacity(context.Scene.Entities.Count);
        var initialMeshCount = context.Graph.Meshes.Count;
        var vertexOffset = context.Graph.Vertices.Count;
        var triangleOffset = context.Graph.Triangles.Count;
        var projectedMeshIndex = initialMeshCount;
        var totalVertices = 0;
        var stagedVertexOffset = 0;

        if (context.Scene.Entities is List<STFU.Engine.Entities.Entity> entityList)
        {
            var entities = CollectionsMarshal.AsSpan(entityList);
            for (var entityIndex = 0; entityIndex < entities.Length; entityIndex++)
            {
                var entity = entities[entityIndex];
                if (!context.Assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
                {
                    continue;
                }

                var transform = entity.Transform;
                var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, RotationEpsilonSquared);
                var rotation = hasRotation ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation) : Quaternion.Identity;
                var hasScale = Geometry3D.HasNonIdentityScale(transform.Scale, ScaleEpsilonSquared);
                var hasTranslation = Geometry3D.HasVectorLength(transform.Position, TranslationEpsilonSquared);
                _jobs.Add(new MeshProjectionJob(
                    entity.Id,
                    entity.Mesh,
                    mesh,
                    vertexOffset,
                    stagedVertexOffset,
                    triangleOffset,
                    transform,
                    hasRotation,
                    hasScale,
                    hasTranslation,
                    rotation));
                projectedMeshIndex++;
                vertexOffset += mesh.Vertices.Count;
                triangleOffset += mesh.Triangles.Count;
                totalVertices += mesh.Vertices.Count;
                stagedVertexOffset += mesh.Vertices.Count;
            }
        }
        else
        {
            foreach (var entity in context.Scene.Entities)
            {
                if (!context.Assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
                {
                    continue;
                }

                var transform = entity.Transform;
                var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, RotationEpsilonSquared);
                var rotation = hasRotation ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation) : Quaternion.Identity;
                var hasScale = Geometry3D.HasNonIdentityScale(transform.Scale, ScaleEpsilonSquared);
                var hasTranslation = Geometry3D.HasVectorLength(transform.Position, TranslationEpsilonSquared);
                _jobs.Add(new MeshProjectionJob(
                    entity.Id,
                    entity.Mesh,
                    mesh,
                    vertexOffset,
                    stagedVertexOffset,
                    triangleOffset,
                    transform,
                    hasRotation,
                    hasScale,
                    hasTranslation,
                    rotation));
                projectedMeshIndex++;
                vertexOffset += mesh.Vertices.Count;
                triangleOffset += mesh.Triangles.Count;
                totalVertices += mesh.Vertices.Count;
                stagedVertexOffset += mesh.Vertices.Count;
            }
        }

        if (_jobs.Count == 0)
        {
            return;
        }

        context.Graph.Vertices.EnsureCapacity(vertexOffset);
        context.Graph.Meshes.EnsureCapacity(projectedMeshIndex);
        EnsureProjectedVertexCapacity(totalVertices);

        var jobs = CollectionsMarshal.AsSpan(_jobs);
        var parallel = context.WorkerCount > 1 && _jobs.Count > 1 && totalVertices >= 512;
        if (parallel)
        {
            DeterministicParallel.ForRanges(
                0,
                _jobs.Count,
                context.WorkerCount,
                context.CancellationToken,
                (start, end, _, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    for (var index = start; index < end; index++)
                    {
                        if ((index & 0x3FF) == 0)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                        }

                        var job = _jobs[index];
                        ProjectMeshVertices(projection, in job, _projectedVertices);
                    }
                },
                minItemsPerRange: 1);
        }
        else
        {
            for (var index = 0; index < _jobs.Count; index++)
            {
                ProjectMeshVerticesWithCache(context, projection, in jobs[index], _projectedVertices);
            }
        }

        CollectionsMarshal.SetCount(context.Graph.Vertices, vertexOffset);
        var graphVertices = CollectionsMarshal.AsSpan(context.Graph.Vertices);
        CollectionsMarshal.SetCount(context.Graph.Meshes, projectedMeshIndex);
        var graphMeshes = CollectionsMarshal.AsSpan(context.Graph.Meshes);
        for (var index = 0; index < _jobs.Count; index++)
        {
            ref readonly var job = ref jobs[index];
            _projectedVertices
                .AsSpan(job.StagedVertexOffset, job.Mesh.Vertices.Count)
                .CopyTo(graphVertices.Slice(job.VertexOffset, job.Mesh.Vertices.Count));

            graphMeshes[initialMeshCount + index] = new ProjectedMesh(
                job.EntityId,
                job.MeshHandle,
                job.Mesh,
                job.VertexOffset,
                job.Mesh.Vertices.Count,
                job.TriangleOffset,
                job.Mesh.Triangles.Count);
        }
    }

    private void EnsureProjectedVertexCapacity(int required)
    {
        if (_projectedVertices.Length < required)
        {
            _projectedVertices = new ProjectedVertex[required];
        }
    }


    private static void ProjectMeshVerticesWithCache(
        STFU.NPR.Pipeline.NprContext context,
        ProjectionInfo projection,
        in MeshProjectionJob job,
        ProjectedVertex[] output)
    {
        var meshSignature = context.Analysis.GetMeshSignature(job.MeshHandle, job.Mesh);
        var transformSignature = MeshAnalysisCacheStore.CalculateTransformSignature(job.Transform);
        var cameraSignature = MeshAnalysisCacheStore.CalculateCameraSignature(context.Camera);
        var cacheKey = new FrameProjectionCacheKey(
            job.MeshHandle,
            meshSignature,
            transformSignature,
            cameraSignature,
            context.Width,
            context.Height,
            context.Settings.DefaultDrawing.DepthScale);

        if (context.Analysis.ProjectionCache.TryGet(cacheKey, out var cached))
        {
            CopyCachedProjectedVertices(cached, in job, output);
        }
        else
        {
            MeshProjectionService.ProjectInto(
                job.Mesh,
                job.Transform,
                projection,
                job.VertexOffset,
                output,
                job.StagedVertexOffset);
            var cachedVertices = new ProjectedVertex[job.Mesh.Vertices.Count];
            Array.Copy(
                output,
                job.StagedVertexOffset,
                cachedVertices,
                0,
                job.Mesh.Vertices.Count);
            context.Analysis.ProjectionCache.Store(cacheKey, new ProjectedMeshFrame(cachedVertices, cachedVertices.Length));
        }

        context.Counters.Set("ProjectMeshStep.projectionCacheEntries", context.Analysis.ProjectionCache.Stats.Entries);
        context.Counters.Set("ProjectMeshStep.projectionCacheHits", context.Analysis.ProjectionCache.Stats.Hits);
        context.Counters.Set("ProjectMeshStep.projectionCacheMisses", context.Analysis.ProjectionCache.Stats.Misses);
        context.Counters.Set("ProjectMeshStep.projectionCacheEvictions", context.Analysis.ProjectionCache.Stats.Evictions);
        context.Counters.Set("ProjectMeshStep.projectedVertices", job.Mesh.Vertices.Count);
    }

    private static void CopyCachedProjectedVertices(
        ProjectedMeshFrame cached,
        in MeshProjectionJob job,
        ProjectedVertex[] output)
    {
        for (var vertexIndex = 0; vertexIndex < cached.VertexCount; vertexIndex++)
        {
            var vertex = cached.Vertices[vertexIndex];
            output[job.StagedVertexOffset + vertexIndex] = new ProjectedVertex(
                job.VertexOffset + vertexIndex,
                vertex.WorldPosition,
                vertex.WorldNormal,
                vertex.Position,
                vertex.Depth,
                vertex.IsVisible,
                vertex.Curvature,
                vertex.SmoothedCurvature,
                vertex.SignedCurvature,
                vertex.SmoothedSignedCurvature,
                vertex.CurvatureDirection,
                vertex.Ndc,
                vertex.Depth01);
        }
    }

    private static void ProjectMeshVertices(
        ProjectionInfo projection,
        in MeshProjectionJob job,
        ProjectedVertex[] output)
    {
        if (TryGetVertexSpan(job.Mesh.Vertices, out var vertices))
        {
            for (var vertexIndex = 0; vertexIndex < vertices.Length; vertexIndex++)
            {
                ProjectVertex(projection, job, vertices[vertexIndex], vertexIndex, output);
            }

            return;
        }

        for (var vertexIndex = 0; vertexIndex < job.Mesh.Vertices.Count; vertexIndex++)
        {
            ProjectVertex(projection, job, job.Mesh.Vertices[vertexIndex], vertexIndex, output);
        }
    }

    private static bool TryGetVertexSpan(
        IReadOnlyList<MeshVertex> source,
        out ReadOnlySpan<MeshVertex> vertices)
    {
        switch (source)
        {
            case MeshVertex[] array:
                vertices = array;
                return true;
            case List<MeshVertex> list:
                vertices = CollectionsMarshal.AsSpan(list);
                return true;
            default:
                vertices = default;
                return false;
        }
    }

    private static void ProjectVertex(
        ProjectionInfo projection,
        in MeshProjectionJob job,
        MeshVertex vertex,
        int vertexIndex,
        ProjectedVertex[] output)
    {
        var worldPosition = Geometry3D.TransformPosition(
            vertex.Position,
            job.Transform.Scale,
            job.Rotation,
            job.Transform.Position,
            job.HasRotation,
            job.HasScale,
            job.HasTranslation);
        var isVisible = projection.TryProject(worldPosition, out var position, out var depth, out var ndc, out var depth01);

        output[job.StagedVertexOffset + vertexIndex] = new ProjectedVertex(
            job.VertexOffset + vertexIndex,
            worldPosition,
            vertex.Normal,
            position,
            depth,
            isVisible,
            0f,
            0f,
            0f,
            0f,
            Vector3.Zero,
            ndc,
            depth01);
    }

    private readonly record struct MeshProjectionJob(
        EntityId EntityId,
        MeshHandle MeshHandle,
        MeshData Mesh,
        int VertexOffset,
        int StagedVertexOffset,
        int TriangleOffset,
        Transform3D Transform,
        bool HasRotation,
        bool HasScale,
        bool HasTranslation,
        Quaternion Rotation);
}
