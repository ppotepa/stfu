using System.Buffers;
using System.Numerics;
using STFU.Assets;
using STFU.Camera;
using STFU.Common.Math;
using STFU.Common.Primitives;
using STFU.Engine.Scenes;
using STFU.NPR.Pipeline;
using STFU.NPR.Settings;
using STFU.Rendering.Abstractions.Requests;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuMeshWireframeBuilder
{
    private readonly Dictionary<MeshHandle, CachedMeshEdges> _edgeCache = new();

    public List<CpuStrokeSegment> BuildSegments(
        Scene scene,
        AssetRegistry assets,
        CameraState camera,
        int width,
        int height,
        NprSettings settings,
        NprRenderTheme theme)
    {
        var segments = new List<CpuStrokeSegment>();
        var projection = ProjectionInfo.Create(camera, width, height, settings);
        var order = 0;

        foreach (var entity in scene.Entities)
        {
            if (!assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            var edges = GetEdges(entity.Mesh, mesh);
            segments.EnsureCapacity(segments.Count + edges.Length);
            var worldPositions = ArrayPool<Vector3>.Shared.Rent(mesh.Vertices.Count);
            try
            {
                var transform = entity.Transform;
                var hasRotation = HasRotation(transform.Rotation);
                var rotation = hasRotation ? CreateRotation(transform.Rotation) : Quaternion.Identity;
                for (var i = 0; i < mesh.Vertices.Count; i++)
                {
                    worldPositions[i] = hasRotation
                        ? TransformVertex(mesh.Vertices[i].Position, transform.Scale, rotation, transform.Position)
                        : mesh.Vertices[i].Position * transform.Scale + transform.Position;
                }

                foreach (var edge in edges)
                {
                    if ((uint)edge.Start >= (uint)mesh.Vertices.Count ||
                        (uint)edge.End >= (uint)mesh.Vertices.Count)
                    {
                        continue;
                    }

                    if (projection.TryProject(worldPositions[edge.Start], out var start, out _) &&
                        projection.TryProject(worldPositions[edge.End], out var end, out _))
                    {
                        segments.Add(new CpuStrokeSegment(
                            start,
                            end,
                            theme.MeshStrokeColor,
                            0.55f,
                            1.0f,
                            order++));
                    }
                }
            }
            finally
            {
                ArrayPool<Vector3>.Shared.Return(worldPositions);
            }
        }

        return segments;
    }

    private CpuMeshEdge[] GetEdges(MeshHandle handle, STFU.Mesh.MeshData mesh)
    {
        if (_edgeCache.TryGetValue(handle, out var cached) &&
            ReferenceEquals(cached.Triangles, mesh.Triangles))
        {
            return cached.Edges;
        }

        var weldedVertexIds = BuildWeldedVertexIds(mesh, out var representativeVertexIndices);
        var emittedEdges = new HashSet<long>(mesh.Triangles.Count * 3);
        var edges = new List<CpuMeshEdge>(mesh.Triangles.Count * 3);

        foreach (var triangle in mesh.Triangles)
        {
            if ((uint)triangle.A >= (uint)weldedVertexIds.Length ||
                (uint)triangle.B >= (uint)weldedVertexIds.Length ||
                (uint)triangle.C >= (uint)weldedVertexIds.Length)
            {
                continue;
            }

            AddEdge(weldedVertexIds[triangle.A], weldedVertexIds[triangle.B]);
            AddEdge(weldedVertexIds[triangle.B], weldedVertexIds[triangle.C]);
            AddEdge(weldedVertexIds[triangle.C], weldedVertexIds[triangle.A]);
        }

        var edgeArray = edges.ToArray();
        _edgeCache[handle] = new CachedMeshEdges(mesh.Triangles, edgeArray);
        return edgeArray;

        void AddEdge(int weldedA, int weldedB)
        {
            if (weldedA < 0 ||
                weldedB < 0 ||
                weldedA == weldedB ||
                !emittedEdges.Add(CreateEdgeKey(weldedA, weldedB)))
            {
                return;
            }

            edges.Add(new CpuMeshEdge(
                representativeVertexIndices[weldedA],
                representativeVertexIndices[weldedB]));
        }
    }

    private static int[] BuildWeldedVertexIds(
        STFU.Mesh.MeshData mesh,
        out int[] representativeVertexIndices)
    {
        var weldedByPosition = new Dictionary<QuantizedVertexKey, int>(mesh.Vertices.Count);
        var representatives = new List<int>(mesh.Vertices.Count);
        var weldedVertexIds = new int[mesh.Vertices.Count];

        for (var i = 0; i < mesh.Vertices.Count; i++)
        {
            var key = QuantizedVertexKey.From(mesh.Vertices[i].Position);
            if (!weldedByPosition.TryGetValue(key, out var weldedId))
            {
                weldedId = representatives.Count;
                weldedByPosition[key] = weldedId;
                representatives.Add(i);
            }

            weldedVertexIds[i] = weldedId;
        }

        representativeVertexIndices = representatives.ToArray();
        return weldedVertexIds;
    }

    private static long CreateEdgeKey(int a, int b)
    {
        var min = Math.Min(a, b);
        var max = Math.Max(a, b);
        return ((long)min << 32) | (uint)max;
    }

    private static Vector3 TransformVertex(
        Vector3 position,
        Vector3 scale,
        Quaternion rotation,
        Vector3 translation)
    {
        return Vector3.Transform(position * scale, rotation) + translation;
    }

    private static Quaternion CreateRotation(Vector3 rotation)
    {
        return Quaternion.CreateFromYawPitchRoll(rotation.Y, rotation.X, rotation.Z);
    }

    private static bool HasRotation(Vector3 rotation)
    {
        return rotation.LengthSquared() > 0.0000001f;
    }

    private sealed record CachedMeshEdges(
        IReadOnlyList<STFU.Mesh.MeshTriangle> Triangles,
        CpuMeshEdge[] Edges);

    private readonly record struct CpuMeshEdge(int Start, int End);

    private readonly record struct QuantizedVertexKey(long X, long Y, long Z)
    {
        private const float Scale = 100000f;

        public static QuantizedVertexKey From(Vector3 position)
        {
            return new QuantizedVertexKey(
                (long)MathF.Round(position.X * Scale),
                (long)MathF.Round(position.Y * Scale),
                (long)MathF.Round(position.Z * Scale));
        }
    }
}
