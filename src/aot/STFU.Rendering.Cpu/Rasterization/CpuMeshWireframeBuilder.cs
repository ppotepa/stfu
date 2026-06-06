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

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuMeshWireframeBuilder
{
    private readonly List<CpuStrokeSegment> _segments = [];
    private Point2D[] _projected = [];
    private bool[] _projectedVisible = [];

    public int LastTopologyEdgeCount { get; private set; }

    public List<CpuStrokeSegment> BuildSegments(
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
        var order = 0;

        foreach (var entity in scene.Entities)
        {
            if (!assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            var edges = analysis.GetOrCreateWireframeTopology(entity.Mesh, mesh, topologyMode).Edges;
            LastTopologyEdgeCount += edges.Count;
            _segments.EnsureCapacity(_segments.Count + edges.Count);
            EnsureScratchCapacity(mesh.Vertices.Count);
            var projected = _projected;
            var projectedVisible = _projectedVisible;
            {
                var transform = entity.Transform;
                var hasRotation = Geometry3D.HasVectorLength(transform.Rotation, 0.0000001f);
                var rotation = hasRotation ? Geometry3D.CreateYawPitchRollRotation(transform.Rotation) : Quaternion.Identity;
                for (var i = 0; i < mesh.Vertices.Count; i++)
                {
                    var worldPosition = Geometry3D.TransformPosition(
                        mesh.Vertices[i].Position,
                        transform.Scale,
                        rotation,
                        transform.Position,
                        hasRotation,
                        hasScale: true,
                        hasTranslation: true);
                    projectedVisible[i] = projection.TryProject(worldPosition, out projected[i], out _);
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
                        _segments.Add(new CpuStrokeSegment(
                            projected[edge.StartVertexIndex],
                            projected[edge.EndVertexIndex],
                            theme.MeshStrokeColor,
                            0.55f,
                            1.0f,
                            order++));
                    }
                }
            }
        }

        return _segments;
    }

    private void EnsureScratchCapacity(int vertexCount)
    {
        if (_projected.Length >= vertexCount)
        {
            return;
        }

        var capacity = 4;
        while (capacity < vertexCount)
        {
            capacity <<= 1;
        }

        _projected = new Point2D[capacity];
        _projectedVisible = new bool[capacity];
    }

}
