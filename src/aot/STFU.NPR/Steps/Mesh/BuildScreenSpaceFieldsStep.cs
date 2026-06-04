using System.Numerics;
using STFU.NPR.Fields;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Styles;

namespace STFU.NPR.Steps.Mesh;

public sealed class BuildScreenSpaceFieldsStep : INprStep
{
    public void Execute(NprContext context)
    {
        if (context.Graph.SurfaceSamples.Count == 0)
        {
            context.Graph.ToneField = ToneField.Empty;
            context.Graph.DirectionField = DirectionField.Empty;
            context.Graph.DensityField = DensityField.Empty;
            context.Graph.TextureField = TextureField.Empty;
            return;
        }

        var toneSamples = new List<ToneSample>(context.Graph.SurfaceSamples.Count);
        var directionSamples = new List<DirectionSample>(context.Graph.SurfaceSamples.Count);
        var densitySamples = new List<DensitySample>(context.Graph.SurfaceSamples.Count);
        var textureSamples = new List<TextureSample>(context.Graph.SurfaceSamples.Count);

        foreach (var sample in context.Graph.SurfaceSamples)
        {
            var region = FindRegion(context.Graph.MaterialRegions, sample.ProjectedTriangleIndex);
            toneSamples.Add(new ToneSample(sample.Position, sample.Shade));

            var axis = new Vector2(sample.CurvatureDirection.X, -sample.CurvatureDirection.Y);
            if (axis.LengthSquared() < 0.0001f)
            {
                axis = new Vector2(sample.Normal.X, -sample.Normal.Y);
            }
            if (axis.LengthSquared() < 0.0001f)
            {
                axis = new Vector2(1f, 0f);
            }
            else
            {
                axis = Vector2.Normalize(axis);
            }

            directionSamples.Add(new DirectionSample(sample.Position, axis));
            densitySamples.Add(new DensitySample(
                sample.Position,
                Math.Clamp(sample.Shade * context.Settings.HatchDensity + sample.SmoothedCurvature * 0.12f, 0f, 1f)));
            textureSamples.Add(new TextureSample(
                sample.Position,
                ComputeTexture(sample, region)));
        }

        context.Graph.ToneField = new ToneField(toneSamples);
        context.Graph.DirectionField = new DirectionField(directionSamples);
        context.Graph.DensityField = new DensityField(densitySamples);
        context.Graph.TextureField = new TextureField(textureSamples);
    }

    private static float ComputeTexture(SurfaceSample sample, MaterialRegion? region)
    {
        var mediumBias = region?.PreferredMedium switch
        {
            StrokeMedium.Ink => 0.22f,
            StrokeMedium.Pencil => 0.58f,
            StrokeMedium.Marker => 0.18f,
            StrokeMedium.Charcoal => 0.8f,
            StrokeMedium.Wash => 0.34f,
            _ => 0.4f
        };

        var coarseNoise = NprRandom.Float01(NprRandom.Hash(sample.StableId, 401));
        var fineSeed = NprRandom.Hash(NprRandom.Hash((int)sample.Position.X, (int)sample.Position.Y), NprRandom.Hash(sample.StableId, 409));
        var fineNoise = NprRandom.Float01(fineSeed);
        var shadeBias = sample.Shade * 0.18f;
        var curvatureBias = sample.SmoothedCurvature * 0.22f;
        return Math.Clamp(mediumBias + coarseNoise * 0.16f + fineNoise * 0.12f + shadeBias + curvatureBias, 0f, 1f);
    }

    private static MaterialRegion? FindRegion(IReadOnlyList<MaterialRegion> regions, int projectedTriangleIndex)
    {
        return regions.FirstOrDefault(region => region.TriangleIndices.Contains(projectedTriangleIndex));
    }
}
