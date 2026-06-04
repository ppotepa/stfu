using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Cpu.Backend;

public sealed class FullCpuNprRenderer : INprRenderer
{
    private readonly ICpuRenderBackend _backend;

    public FullCpuNprRenderer(ICpuRenderBackend backend)
    {
        _backend = backend;
    }

    public ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        return _backend.RenderAsync(request, cancellationToken);
    }
}
