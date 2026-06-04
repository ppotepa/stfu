using System.Diagnostics;
using STFU.NPR.Debug;
using STFU.Rendering.Abstractions.Diagnostics;
using STFU.Rendering.Abstractions.Requests;
using STFU.Rendering.DirectX.Device;
using STFU.Rendering.DirectX.Upload;

namespace STFU.Rendering.DirectX.Passes;

public sealed class DxDebugOverlayPass
{
    private readonly DxStrokeRasterPass _strokePass;

    public DxDebugOverlayPass(DirectXDevice device, DxStrokeRasterPass strokePass)
    {
        _strokePass = strokePass;
    }

    public void Execute(
        DirectXTextureResource target,
        NprRenderRequest request,
        NprDebugFrame debugFrame,
        NprRenderDiagnostics diagnostics,
        CancellationToken cancellationToken)
    {
        if (request.DebugOverlay == DebugOverlayKind.None || debugFrame.Lines.Count == 0)
        {
            return;
        }

        var buildWatch = Stopwatch.StartNew();
        var paths = DxDebugOverlayBuilder.Build(debugFrame, request.DebugOverlay);
        buildWatch.Stop();
        diagnostics.AddTiming("GpuDebugOverlayBuild", buildWatch.Elapsed.TotalMilliseconds, $"paths={paths.Count}");

        if (paths.Count == 0)
        {
            return;
        }

        _strokePass.Execute(target, request, paths, 1f, diagnostics, cancellationToken);
        diagnostics.AddTiming("GpuDebugOverlayDraw", 0, $"paths={paths.Count}");
    }
}
