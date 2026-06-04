using STFU.Logging;
using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.DirectX.Backend;

public sealed class DirectXNprRenderer : INprRenderer
{
    private readonly ProfiledNprRenderer _inner;

    public DirectXNprRenderer(ICpuRenderBackend cpu, IGpuRenderBackend gpu)
    {
        _inner = new ProfiledNprRenderer(cpu, gpu);
    }

    public ValueTask<NprRenderResult> RenderAsync(NprRenderRequest request, CancellationToken cancellationToken)
    {
        if (request.ExecutionProfile == NprExecutionProfile.CpuDrivenGpuAccelerated &&
            !_inner.IsGpuAvailable)
        {
            StfuLog.Write(
                StfuLogDomain.RenderGpu,
                "fallback.cpu",
                "GPU profile requested but GPU backend is unavailable.",
                StfuLogLevel.Warning,
                new Dictionary<string, object?>
                {
                    ["revision"] = request.Revision,
                    ["width"] = request.Width,
                    ["height"] = request.Height
                });
        }

        return _inner.RenderAsync(request, cancellationToken);
    }
}
