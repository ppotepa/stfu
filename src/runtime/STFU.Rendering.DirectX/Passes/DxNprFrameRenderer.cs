using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;
using STFU.Strokes;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxNprFrameRenderer
{
    private readonly DxClearPass _clearPass;
    private readonly DxToneSurfacePass _tonePass;
    private readonly DxStrokeRasterPass _strokePass;
    private readonly DxMeshWireframePass _meshPass;
    private readonly DxDebugOverlayPass _debugPass;
    private readonly List<NprLayerFrame> _visibleLayers = [];

    public DxNprFrameRenderer(DirectXDevice device)
    {
        _clearPass = new DxClearPass(device);
        _strokePass = new DxStrokeRasterPass(device);
        _tonePass = new DxToneSurfacePass(device);
        _meshPass = new DxMeshWireframePass(device, new DxMeshWireframeBuilder(), _strokePass);
        _debugPass = new DxDebugOverlayPass(device, _strokePass);
    }

    public DxStrokeRasterPass StrokePass => _strokePass;

    public DxToneSurfacePass TonePass => _tonePass;

    public void Render(
        DirectXTextureResource target,
        NprRenderRequest request,
        StrokeFrame strokeFrame,
        NprFrame nprFrame,
        NprDebugFrame debugFrame,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var clearWatch = System.Diagnostics.Stopwatch.StartNew();
        _clearPass.Execute(target, request.Theme);
        clearWatch.Stop();
        diagnostics.AddTiming("GpuClear", clearWatch.Elapsed.TotalMilliseconds);

        if (request.ContentKind == STFU.Rendering.Abstractions.Execution.NprRenderContentKind.MeshWireframe)
        {
            _meshPass.Execute(target, request, diagnostics, cancellationToken);
        }
        else if (nprFrame.Layers.Count > 0)
        {
            var layers = BuildVisibleLayerOrder(nprFrame.Layers);
            for (var i = 0; i < layers.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var layer = layers[i];

                if (request.Quality.RasterizeToneSurfaces && request.Quality.UseGpuToneRaster)
                {
                    _tonePass.Execute(target, request, layer, diagnostics, cancellationToken);
                }

                if (layer.ShadingSegments is { Count: > 0 } shadingSegments)
                {
                    _strokePass.Execute(target, request, shadingSegments, layer.Opacity, diagnostics, cancellationToken);
                }
                else if (layer.Shading.Count > 0)
                {
                    _strokePass.Execute(target, request, layer.Shading, layer.Opacity, diagnostics, cancellationToken);
                }

                if (layer.StrokeSegments is { Count: > 0 } strokeSegments)
                {
                    _strokePass.Execute(target, request, strokeSegments, layer.Opacity, diagnostics, cancellationToken);
                }
                else if (layer.Strokes.Count > 0)
                {
                    _strokePass.Execute(target, request, layer.Strokes, layer.Opacity, diagnostics, cancellationToken);
                }
            }
        }
        else if (strokeFrame.Segments is { Count: > 0 } frameSegments)
        {
            _strokePass.Execute(target, request, frameSegments, 1f, diagnostics, cancellationToken);
        }
        else if (strokeFrame.Paths.Count > 0)
        {
            _strokePass.Execute(target, request, strokeFrame.Paths, 1f, diagnostics, cancellationToken);
        }

        if (request.IncludeDebugFrame && request.DebugOverlay != DebugOverlayKind.None)
        {
            _debugPass.Execute(target, request, debugFrame, diagnostics, cancellationToken);
        }
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
