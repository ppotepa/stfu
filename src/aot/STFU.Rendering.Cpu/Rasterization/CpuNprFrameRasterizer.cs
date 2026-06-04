using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Cpu.Rasterization;

public sealed class CpuNprFrameRasterizer
{
    private readonly CpuStrokeRasterizer _strokeRasterizer = new();
    private readonly CpuGridRasterizer _gridRasterizer = new();
    private readonly CpuLayerCompositor _layerCompositor = new();

    public void Rasterize(
        PixelSurface target,
        NprRenderRequest request,
        StrokeFrame strokeFrame,
        NprFrame nprFrame)
    {
        var paper = ResolvePaperColor(request, nprFrame);
        CpuPixelBlender.Clear(target, paper, 255);

        if (request.ShowGrid && request.Quality.RasterizeGrid)
        {
            _gridRasterizer.DrawGrid(target, request.Theme, request.Quality, request.Budget);
        }

        if (nprFrame.Layers.Count > 0)
        {
            foreach (var layer in nprFrame.Layers.Where(layer => layer.Visible).OrderBy(layer => layer.Order))
            {
                _layerCompositor.CompositeLayer(target, layer, request.Quality, request.Budget);
            }

            return;
        }

        var fallback = strokeFrame.Paths.Count > 0
            ? strokeFrame
            : nprFrame.LegacyStrokes;

        _strokeRasterizer.DrawPaths(target, fallback.Paths, 1f, request.Quality, request.Budget);
    }

    public void RasterizeMeshWireframe(
        PixelSurface target,
        NprRenderRequest request,
        IReadOnlyList<CpuStrokeSegment> segments)
    {
        CpuPixelBlender.Clear(target, request.Theme.PaperColor, 255);

        if (request.ShowGrid && request.Quality.RasterizeGrid)
        {
            _gridRasterizer.DrawGrid(target, request.Theme, request.Quality, request.Budget);
        }

        _strokeRasterizer.DrawSegments(target, segments, request.Quality, request.Budget);
    }

    private static StrokeColor ResolvePaperColor(NprRenderRequest request, NprFrame frame)
    {
        if (frame.Width > 0 && frame.Height > 0)
        {
            return frame.Paper.Color;
        }

        return request.Theme.PaperColor;
    }
}
