using System.Text.Json;
using System.Globalization;
using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Debug;

public static class DefaultParitySnapshotBuilder
{
    public static DefaultParitySnapshot Create(NprContext context)
    {
        var timings = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var trace in context.StepTraces)
        {
            timings[trace.StepName] = trace.Milliseconds;
        }

        var drawing = context.Settings.DefaultDrawing;
        var visibility = context.Graph.DefaultFaceIdVisibility;
        var visibleFaces = visibility is null
            ? []
            : visibility.FaceVisible
                .Select((isVisible, index) => (isVisible, index))
                .Where(pair => pair.isVisible)
                .Select(pair => pair.index)
                .ToArray();
        var lineVisibleFaces = visibility is null
            ? []
            : context.Graph.Triangles
                .Select((triangle, index) => (triangle, index))
                .Where(pair => pair.triangle.IsFrontFacing &&
                    (uint)pair.index < (uint)visibility.FaceVisible.Length &&
                    visibility.FaceVisible[pair.index])
                .Select(pair => pair.index)
                .ToArray();
        var projectedVertices = new List<DefaultParityProjectedVertexSnapshot>(context.Graph.Vertices.Count);
        var projectedVertexKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var vertex in context.Graph.Vertices)
        {
            var key = string.Create(
                CultureInfo.InvariantCulture,
                $"{vertex.WorldPosition.X:0.000000}:{vertex.WorldPosition.Y:0.000000}:{vertex.WorldPosition.Z:0.000000}");

            if (!projectedVertexKeys.Add(key))
            {
                continue;
            }

            projectedVertices.Add(new DefaultParityProjectedVertexSnapshot(
                projectedVertices.Count,
                [vertex.WorldPosition.X, vertex.WorldPosition.Y, vertex.WorldPosition.Z],
                [vertex.Position.X, vertex.Position.Y],
                vertex.Depth,
                vertex.Depth01,
                vertex.IsVisible,
                [vertex.Ndc.X, vertex.Ndc.Y, vertex.Ndc.Z]));
        }

        return new DefaultParitySnapshot(
            context.FrameId,
            context.Style.StyleId,
            NprPipelineIds.Default,
            context.Width,
            context.Height,
            new DefaultParityCameraSnapshot(
                drawing.FieldOfViewDegrees,
                drawing.NearPlane,
                drawing.FarPlane,
                ToArray(context.Camera.Position),
                ToArray(context.Camera.Target)),
            new DefaultParitySettingsSnapshot(
                context.Settings.Seed,
                drawing.FieldOfViewDegrees,
                drawing.NearPlane,
                drawing.FarPlane,
                drawing.TopologyMode.ToString(),
                drawing.ShowSilhouette,
                drawing.ShowFeature,
                drawing.ShowBoundary,
                drawing.FeatureAngleDegrees,
                drawing.CullOutside,
                drawing.MinSegPx,
                drawing.MeshStride,
                drawing.OcclusionCulling,
                drawing.OcclusionSamples,
                drawing.OcclusionStrictness,
                drawing.OcclusionBias,
                drawing.DepthScale,
                drawing.StrokeStyle.ToString(),
                drawing.LineWidth,
                drawing.Jitter,
                drawing.Pressure,
                drawing.PathSimplify,
                drawing.AutoDraw,
                drawing.DrawSpeed,
                drawing.DrawProgress),
            new DefaultParityCountsSnapshot(
                context.Graph.Meshes.Count,
                context.Graph.Vertices.Count,
                context.Graph.Triangles.Count,
                context.Graph.TopologyEdges.Count,
                context.Graph.Curves.Count,
                context.Graph.VisibilitySegments.Count(segment => segment.State == VisibilityState.Visible),
                context.Graph.VisibilitySegments.Count(segment => segment.State == VisibilityState.Hidden),
                context.Graph.DefaultFragments.Count,
                context.Graph.DefaultPaths.Count,
                context.Graph.DefaultDrawablePaths.Count,
                context.Frame.Paths.Count),
            new DefaultParityVisibilitySnapshot(
                visibility?.Width ?? 0,
                visibility?.Height ?? 0,
                visibleFaces.Length,
                visibleFaces,
                visibility is null ? null : HashIntArray(visibility.FaceId),
                lineVisibleFaces.Length,
                lineVisibleFaces),
            projectedVertices,
            context.Graph.Triangles
                .Select(triangle => new DefaultParityTriangleSnapshot(
                    triangle.StableId,
                    triangle.ProjectedMeshIndex,
                    triangle.MeshTriangleIndex,
                    triangle.A,
                    triangle.B,
                    triangle.C,
                    [triangle.Normal.X, triangle.Normal.Y, triangle.Normal.Z],
                    triangle.Depth,
                    triangle.ScreenArea,
                    triangle.IsFrontFacing,
                    triangle.IsVisible))
                .ToArray(),
            context.Graph.TopologyEdges
                .Select(edge => new DefaultParityTopologyEdgeSnapshot(
                    edge.StableId,
                    edge.StartVertexIndex,
                    edge.EndVertexIndex,
                    edge.FirstTriangleIndex,
                    edge.SecondTriangleIndex,
                    edge.NormalAngleDegrees,
                    edge.IsBoundary))
                .ToArray(),
            context.Graph.DefaultFragments
                .Select(fragment => new DefaultParityFragmentSnapshot(
                    fragment.StableId,
                    fragment.Type.ToString(),
                    [fragment.P0.X, fragment.P0.Y],
                    [fragment.P1.X, fragment.P1.Y],
                    fragment.EdgeStableId,
                    fragment.FirstTriangleIndex,
                    fragment.SecondTriangleIndex,
                    fragment.StartT,
                    fragment.EndT,
                    fragment.Depth))
                .ToArray(),
            context.Graph.DefaultPaths
                .Select(CreatePathSnapshot)
                .ToArray(),
            context.Graph.DefaultDrawablePaths
                .Select(CreatePathSnapshot)
                .ToArray(),
            timings);
    }

    public static string ToJson(NprContext context, bool indented = true)
    {
        var snapshot = Create(context);
        var options = new JsonSerializerOptions(DefaultParitySnapshotJsonContext.Default.Options)
        {
            WriteIndented = indented
        };
        return JsonSerializer.Serialize(snapshot, new DefaultParitySnapshotJsonContext(options).DefaultParitySnapshot);
    }

    public static DefaultParitySnapshot? FromJson(string json)
    {
        return JsonSerializer.Deserialize(json, DefaultParitySnapshotJsonContext.Default.DefaultParitySnapshot);
    }

    private static DefaultParityPathSnapshot CreatePathSnapshot(DefaultProjectedPath path)
    {
        return new DefaultParityPathSnapshot(
            path.StableId,
            path.Type.ToString(),
            path.PathIndex,
            path.Length,
            path.Points.Select(point => new[] { point.X, point.Y }).ToArray());
    }

    private static float[] ToArray(System.Numerics.Vector3 vector)
    {
        return [vector.X, vector.Y, vector.Z];
    }

    private static ulong HashIntArray(IReadOnlyList<int> values)
    {
        var hash = HashMath.FnvOffset64;

        for (var index = 0; index < values.Count; index++)
        {
            hash = HashMath.Fnv1A(hash, values[index]);
        }

        return hash;
    }
}
