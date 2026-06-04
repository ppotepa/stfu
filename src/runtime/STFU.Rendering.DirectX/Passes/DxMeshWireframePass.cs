using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxMeshWireframePass
{
    private readonly DxMeshWireframeBuilder _builder;
    private readonly DxStrokeRasterPass _strokePass;

    public DxMeshWireframePass(DxMeshWireframeBuilder builder, DxStrokeRasterPass strokePass)
    {
        _builder = builder;
        _strokePass = strokePass;
    }

    public void Execute(
        DirectXTextureResource target,
        NprRenderRequest request,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        var buildWatch = System.Diagnostics.Stopwatch.StartNew();
        var meshPaths = _builder.BuildPaths(
            request.Scene,
            request.Assets,
            request.Camera,
            request.Width,
            request.Height,
            request.Settings,
            request.Theme);
        buildWatch.Stop();

        diagnostics.PathCount = meshPaths.Count;
        diagnostics.AddTiming("GpuMeshBuild", buildWatch.Elapsed.TotalMilliseconds, $"paths={meshPaths.Count}");
        _strokePass.Execute(target, request, meshPaths, 1f, diagnostics, cancellationToken);
    }
}
