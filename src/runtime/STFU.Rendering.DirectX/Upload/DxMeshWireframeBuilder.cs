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

namespace STFU.Rendering.DirectX.Upload;

public sealed class DxMeshWireframeBuilder
{
    private readonly Dictionary<MeshHandle, CachedMeshEdges> _edgeCache = new();

    public List<StrokePath2D> BuildPaths(
        Scene scene,
        AssetRegistry assets,
        CameraState camera,
        int width,
        int height,
        NprSettings settings,
        NprRenderTheme theme)
    {
        var paths = new List<StrokePath2D>();
        var projection = ProjectionInfo.Create(camera, width, height, settings);
        var style = new StrokeStyle2D(0.55f, 1f, theme.MeshStrokeColor);

        foreach (var entity in scene.Entities)
        {
            if (!assets.TryGetMesh(entity.Mesh, out var mesh) || mesh.Vertices.Count == 0)
            {
                continue;
            }

            var edges = GetEdges(entity.Mesh, mesh);
            paths.EnsureCapacity(paths.Count + edges.Length);
            var worldPositions = ArrayPool<Vector3>.Shared.Rent(mesh.Vertices.Count);

            try
            {
                var transform = entity.Transform;
                var hasRotation = HasRotation(transform.Rotation);
                var rotation = hasRotation ? CreateRotation(transform.Rotation) : Quaternion.Identity;
                for (var index = 0; index < mesh.Vertices.Count; index++)
                {
                    worldPositions[index] = hasRotation
                        ? TransformVertex(mesh.Vertices[index].Position, transform.Scale, rotation, transform.Position)
                        : mesh.Vertices[index].Position * transform.Scale + transform.Position;
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
                        paths.Add(StrokePath2D.Line(start, end, style));
                    }
                }
            }
            finally
            {
                ArrayPool<Vector3>.Shared.Return(worldPositions);
            }
        }

        return paths;
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

        for (var index = 0; index < mesh.Vertices.Count; index++)
        {
            var key = QuantizedVertexKey.From(mesh.Vertices[index].Position);
            if (!weldedByPosition.TryGetValue(key, out var weldedId))
            {
                weldedId = representatives.Count;
                weldedByPosition[key] = weldedId;
                representatives.Add(index);
            }

            weldedVertexIds[index] = weldedId;
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

    private static Vector3 TransformVertex(Vector3 position, Vector3 scale, Quaternion rotation, Vector3 translation)
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
