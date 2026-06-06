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
    private readonly List<NprLayerFrame> _visibleLayers = [];

    public void Rasterize(
        PixelSurface target,
        NprRenderRequest request,
        StrokeFrame strokeFrame,
        NprFrame nprFrame,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        workspace.ResetForFrame();
        var paper = ResolvePaperColor(request, nprFrame);
        CpuPixelBlender.Clear(target, paper, 255);

        if (request.ShowGrid && request.Quality.RasterizeGrid)
        {
            _gridRasterizer.DrawGrid(target, request.Theme, request.Quality, request.Budget, workspace, cancellationToken);
        }

        if (nprFrame.Layers.Count > 0)
        {
            var layers = BuildVisibleLayerOrder(nprFrame.Layers);
            for (var i = 0; i < layers.Count; i++)
            {
                _layerCompositor.CompositeLayer(target, layers[i], request.Quality, request.Budget, workspace, cancellationToken);
            }

            return;
        }

        if (strokeFrame.Segments is { Count: > 0 } strokeSegments)
        {
            _strokeRasterizer.DrawStrokeSegments(target, strokeSegments, 1f, request.Quality, request.Budget, workspace, cancellationToken);
            return;
        }

        var fallback = strokeFrame.Paths.Count > 0
            ? strokeFrame
            : nprFrame.LegacyStrokes;

        _strokeRasterizer.DrawPaths(target, fallback.Paths, 1f, request.Quality, request.Budget, workspace, cancellationToken);
    }

    public void RasterizeMeshWireframe(
        PixelSurface target,
        NprRenderRequest request,
        IReadOnlyList<CpuStrokeSegment> segments,
        CpuRasterWorkspace workspace,
        CancellationToken cancellationToken = default)
    {
        workspace.ResetForFrame();
        CpuPixelBlender.Clear(target, request.Theme.PaperColor, 255);

        if (request.ShowGrid && request.Quality.RasterizeGrid)
        {
            _gridRasterizer.DrawGrid(target, request.Theme, request.Quality, request.Budget, workspace, cancellationToken);
        }

        _strokeRasterizer.DrawSegments(target, segments, request.Quality, request.Budget, workspace, cancellationToken);
    }

    private static StrokeColor ResolvePaperColor(NprRenderRequest request, NprFrame frame)
    {
        if (frame.Width > 0 && frame.Height > 0)
        {
            return frame.Paper.Color;
        }

        return request.Theme.PaperColor;
    }

    private List<NprLayerFrame> BuildVisibleLayerOrder(IReadOnlyList<NprLayerFrame> layers)
    {
        _visibleLayers.Clear();
        for (var i = 0; i < layers.Count; i++)
        {
            if (layers[i].Visible)
            {
                _visibleLayers.Add(layers[i]);
            }
        }

        _visibleLayers.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        return _visibleLayers;
    }
}
