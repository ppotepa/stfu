using System.Runtime.InteropServices;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildMaterialRegionsStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.MaterialRegions.Clear();

        for (var meshIndex = 0; meshIndex < context.Graph.Meshes.Count; meshIndex++)
        {
            var mesh = context.Graph.Meshes[meshIndex];
            var buckets = new Dictionary<int, List<int>>();
            var toneSums = new Dictionary<int, float>();
            var toneCounts = new Dictionary<int, int>();

            for (var triangleIndex = mesh.TriangleOffset; triangleIndex < mesh.TriangleOffset + mesh.TriangleCount; triangleIndex++)
            {
                if ((uint)triangleIndex >= (uint)context.Graph.Triangles.Count)
                {
                    continue;
                }

                var triangle = context.Graph.Triangles[triangleIndex];
                var materialId = ResolveMaterialId(triangle.Shade);
                ref var triangleBucket = ref CollectionsMarshal.GetValueRefOrAddDefault(buckets, materialId, out _);
                triangleBucket ??= [];
                triangleBucket.Add(triangleIndex);

                if (!triangle.IsVisible)
                {
                    continue;
                }

                toneSums[materialId] = toneSums.GetValueOrDefault(materialId) + triangle.Shade;
                toneCounts[materialId] = toneCounts.GetValueOrDefault(materialId) + 1;
            }

            if (buckets.Count == 0)
            {
                continue;
            }

            foreach (var (materialId, triangleIndices) in buckets.OrderBy(entry => entry.Key))
            {
                var toneCount = toneCounts.GetValueOrDefault(materialId);
                var baseTone = toneCount == 0
                    ? EstimateToneFallback(context, triangleIndices)
                    : toneSums[materialId] / toneCount;

                context.Graph.MaterialRegions.Add(new MaterialRegion(
                    StableId: Hash(mesh.EntityId.Value, meshIndex, materialId),
                    EntityId: mesh.EntityId,
                    MaterialId: materialId,
                    TriangleIndices: triangleIndices,
                    BaseTone: baseTone,
                    PreferredMedium: ResolveMedium(baseTone),
                    HatchingPolicy: ResolveHatchingPolicy(baseTone)));
            }
        }
    }

    private static int ResolveMaterialId(float shade)
    {
        return shade switch
        {
            >= 0.8f => 3,
            >= 0.6f => 2,
            >= 0.4f => 1,
            _ => 0
        };
    }

    private static float EstimateToneFallback(NprContext context, IReadOnlyList<int> triangleIndices)
    {
        if (triangleIndices.Count == 0)
        {
            return 0f;
        }

        var toneSum = 0f;
        for (var index = 0; index < triangleIndices.Count; index++)
        {
            toneSum += context.Graph.Triangles[triangleIndices[index]].Shade;
        }

        return toneSum / triangleIndices.Count;
    }

    private static StrokeMedium ResolveMedium(float baseTone)
    {
        return baseTone switch
        {
            >= 0.8f => StrokeMedium.Ink,
            >= 0.55f => StrokeMedium.Pencil,
            >= 0.35f => StrokeMedium.Marker,
            _ => StrokeMedium.Wash
        };
    }

    private static RegionHatchingPolicy ResolveHatchingPolicy(float baseTone)
    {
        return baseTone switch
        {
            >= 0.85f => RegionHatchingPolicy.Dense,
            >= 0.65f => RegionHatchingPolicy.CrossHatch,
            >= 0.4f => RegionHatchingPolicy.Default,
            _ => RegionHatchingPolicy.Sparse
        };
    }

    private static int Hash(int entityId, int meshIndex, int materialId)
    {
        unchecked
        {
            return ((entityId * 397) ^ (meshIndex + 17)) * 31 ^ (materialId + 11);
        }
    }
}
