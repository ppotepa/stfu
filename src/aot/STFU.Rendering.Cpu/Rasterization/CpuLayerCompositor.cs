using STFU.Common.Math;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuLayerCompositor
{
    private readonly CpuStrokeRasterizer _strokeRasterizer = new();
    private readonly CpuToneRasterizer _toneRasterizer = new();

    public void CompositeLayer(
        PixelSurface target,
        NprLayerFrame layer,
        NprQualityProfile quality,
        NprFrameBudget budget,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        var layerOpacity = NumericMath.Clamp01(layer.Opacity);
        if (layerOpacity <= 0f || !layer.Visible)
        {
            return;
        }

        if (quality.RasterizeToneSurfaces)
        {
            if (layer.Tones.Count > 0)
            {
                workspace.EnsureToneScratchCapacity(target.Width * target.Height);
                workspace.Counters.LayerScratchReused++;
            }

            foreach (var tone in layer.Tones)
            {
                _toneRasterizer.DrawToneSurface(target, tone, layerOpacity, budget, workspace, cancellationToken);
            }
        }

        if (layer.ShadingSegments is { Count: > 0 } shadingSegments)
        {
            _strokeRasterizer.DrawStrokeSegments(target, shadingSegments, layerOpacity, quality, budget, workspace, cancellationToken);
        }
        else
        {
            _strokeRasterizer.DrawPaths(target, layer.Shading, layerOpacity, quality, budget, workspace, cancellationToken, preservePathOrder: true);
        }

        if (layer.StrokeSegments is { Count: > 0 } strokeSegments)
        {
            _strokeRasterizer.DrawStrokeSegments(target, strokeSegments, layerOpacity, quality, budget, workspace, cancellationToken);
        }
        else
        {
            _strokeRasterizer.DrawPaths(target, layer.Strokes, layerOpacity, quality, budget, workspace, cancellationToken, preservePathOrder: true);
        }
    }
}
