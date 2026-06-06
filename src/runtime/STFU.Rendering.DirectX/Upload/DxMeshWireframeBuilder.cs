using System.Buffers;
using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Engine.Scenes;
using STFU.NPR.Analysis;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.Rendering.Abstractions.Requests;
using STFU.Strokes;

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxMeshWireframeBuilder
{
    private readonly List<StrokeSegment2D> _segments = [];
    private readonly DxMeshWireframeBatch _batch = new();

    public int LastTopologyEdgeCount { get; private set; }
    public int LastDrawEdgeCount { get; private set; }

    public List<StrokeSegment2D> BuildSegments(
        Scene scene,
        AssetRegistry assets,
        MeshAnalysisCacheStore analysis,
        CameraState camera,
        int width,
        int height,
        NprSettings settings,
        NprRenderTheme theme,
        MeshWireframeTopologyMode topologyMode)
    {
        _segments.Clear();
        LastTopologyEdgeCount = 0;
        var projection = ProjectionInfo.Create(camera, width, height, settings);
        var style = new StrokeStyle2D(0.55f, 1f, theme.MeshStrokeColor);

        foreach (var entity in scene.Entities)
        {
            if (!assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            var edges = analysis.GetOrCreateWireframeTopology(entity.Mesh, mesh, topologyMode).Edges;
            LastTopologyEdgeCount += edges.Count;
            _segments.EnsureCapacity(_segments.Count + edges.Count);
            var projected = ArrayPool<Point2D>.Shared.Rent(mesh.Vertices.Count);
            var projectedVisible = ArrayPool<bool>.Shared.Rent(mesh.Vertices.Count);

            try
            {
                var transform = entity.Transform;
                var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, 0.0000001f);
                var rotation = hasRotation ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation) : Quaternion.Identity;
                for (var index = 0; index < mesh.Vertices.Count; index++)
                {
                    var worldPosition = Geometry3D.TransformPosition(
                        mesh.Vertices[index].Position,
                        transform.Scale,
                        rotation,
                        transform.Position,
                        hasRotation,
                        hasScale: true,
                        hasTranslation: true);
                    projectedVisible[index] = projection.TryProject(worldPosition, out projected[index], out _);
                }

                foreach (var edge in edges)
                {
                    if ((uint)edge.StartVertexIndex >= (uint)mesh.Vertices.Count ||
                        (uint)edge.EndVertexIndex >= (uint)mesh.Vertices.Count)
                    {
                        continue;
                    }

                    if (projectedVisible[edge.StartVertexIndex] &&
                        projectedVisible[edge.EndVertexIndex])
                    {
                        _segments.Add(new StrokeSegment2D(
                            projected[edge.StartVertexIndex],
                            projected[edge.EndVertexIndex],
                            style));
                    }
                }
            }
            finally
            {
                ArrayPool<Point2D>.Shared.Return(projected);
                ArrayPool<bool>.Shared.Return(projectedVisible);
            }
        }

        return _segments;
    }

    public DxMeshWireframeBatch BuildGpuBatch(
        Scene scene,
        AssetRegistry assets,
        MeshAnalysisCacheStore analysis,
        CameraState camera,
        int width,
        int height,
        NprSettings settings,
        MeshWireframeTopologyMode topologyMode)
    {
        _batch.Clear();
        LastTopologyEdgeCount = 0;
        LastDrawEdgeCount = 0;
        var edgeSignature = HashMath.FnvOffset64;
        var projection = ProjectionInfo.Create(camera, width, height, settings);

        foreach (var entity in scene.Entities)
        {
            if (!assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            edgeSignature = HashMath.Fnv1A(edgeSignature, entity.Mesh.Value);
            edgeSignature = HashMath.Fnv1A(edgeSignature, mesh.Vertices.Count);
            edgeSignature = HashMath.Fnv1A(edgeSignature, mesh.Triangles.Count);
            edgeSignature = HashMath.Fnv1A(edgeSignature, (int)topologyMode);
            edgeSignature = HashMath.Fnv1A(edgeSignature, width);
            edgeSignature = HashMath.Fnv1A(edgeSignature, height);

            var edges = analysis.GetOrCreateWireframeTopology(entity.Mesh, mesh, topologyMode).Edges;
            LastTopologyEdgeCount += edges.Count;
            _batch.Edges.EnsureCapacity(_batch.Edges.Count + edges.Count);
            _batch.Vertices.EnsureCapacity(_batch.Vertices.Count + edges.Count * 2);
            var projected = ArrayPool<Point2D>.Shared.Rent(mesh.Vertices.Count);
            var projectedVisible = ArrayPool<bool>.Shared.Rent(mesh.Vertices.Count);
            try
            {
                var transform = entity.Transform;
                var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, 0.0000001f);
                var rotation = hasRotation ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation) : Quaternion.Identity;
                for (var index = 0; index < mesh.Vertices.Count; index++)
                {
                    var worldPosition = Geometry3D.TransformPosition(
                        mesh.Vertices[index].Position,
                        transform.Scale,
                        rotation,
                        transform.Position,
                        hasRotation,
                        hasScale: true,
                        hasTranslation: true);
                    projectedVisible[index] = projection.TryProject(worldPosition, out projected[index], out _);
                }

                foreach (var edge in edges)
                {
                    if ((uint)edge.StartVertexIndex >= (uint)mesh.Vertices.Count ||
                        (uint)edge.EndVertexIndex >= (uint)mesh.Vertices.Count)
                    {
                        continue;
                    }

                    if (!projectedVisible[edge.StartVertexIndex] ||
                        !projectedVisible[edge.EndVertexIndex])
                    {
                        continue;
                    }

                    var start = _batch.Vertices.Count;
                    var end = start + 1;
                    _batch.Vertices.Add(new DxMeshVertex(projected[edge.StartVertexIndex]));
                    _batch.Vertices.Add(new DxMeshVertex(projected[edge.EndVertexIndex]));
                    _batch.Edges.Add(new DxMeshEdge(start, end));
                    edgeSignature = HashMath.Fnv1A(edgeSignature, edge.StartVertexIndex);
                    edgeSignature = HashMath.Fnv1A(edgeSignature, edge.EndVertexIndex);
                }
            }
            finally
            {
                ArrayPool<Point2D>.Shared.Return(projected);
                ArrayPool<bool>.Shared.Return(projectedVisible);
            }
        }

        LastDrawEdgeCount = _batch.Edges.Count;
        _batch.EdgeSignature = edgeSignature;
        return _batch;
    }

}
