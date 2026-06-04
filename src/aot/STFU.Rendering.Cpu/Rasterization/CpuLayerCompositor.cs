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
        NprFrameBudget budget)
    {
        var layerOpacity = Math.Clamp(layer.Opacity, 0f, 1f);
        if (layerOpacity <= 0f || !layer.Visible)
        {
            return;
        }

        if (quality.RasterizeToneSurfaces)
        {
            foreach (var tone in layer.Tones)
            {
                _toneRasterizer.DrawToneSurface(target, tone, layerOpacity, budget);
            }
        }

        _strokeRasterizer.DrawPaths(target, layer.Shading, layerOpacity, quality, budget, preservePathOrder: true);
        _strokeRasterizer.DrawPaths(target, layer.Strokes, layerOpacity, quality, budget, preservePathOrder: true);
    }
}
