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

            context.Graph.SurfaceSamples.Add(new SurfaceSample(
                triangle.StableId,
                triangleIndex,
                triangle.Normal,
                triangle.ScreenCenter,
                triangle.Depth,
                triangle.Shade));
        }
    }
}
