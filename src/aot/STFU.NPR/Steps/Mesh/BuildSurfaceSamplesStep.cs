using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildSurfaceSamplesStep : INprStep
{
    public void Execute(NprContext context)
    {
        for (var triangleIndex = 0; triangleIndex < context.Graph.Triangles.Count; triangleIndex++)
        {
            var triangle = context.Graph.Triangles[triangleIndex];

            if (!triangle.IsVisible ||
                !triangle.IsFrontFacing ||
                triangle.ScreenArea < context.Settings.MinimumProjectedTriangleArea)
            {
                continue;
            }

            var projectedMesh = context.Graph.Meshes[triangle.ProjectedMeshIndex];
            var cache = context.Analysis.GetOrCreate(projectedMesh.MeshHandle, projectedMesh.Mesh);
            var curvature = cache.Curvature?.GetTriangleCurvature(triangle.MeshTriangleIndex) ?? 0f;
            var smoothedCurvature = cache.Curvature?.GetSmoothedTriangleCurvature(triangle.MeshTriangleIndex) ?? curvature;
            var curvatureDirection = cache.Curvature?.GetTriangleDirection(triangle.MeshTriangleIndex) ?? triangle.Normal;

            context.Graph.SurfaceSamples.Add(new SurfaceSample(
                triangle.StableId,
                triangleIndex,
                triangle.Normal,
                curvatureDirection,
                triangle.ScreenCenter,
                triangle.Depth,
                triangle.Shade,
                curvature,
                smoothedCurvature));
        }
    }
}
