using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Cpu.Backend;

public sealed class FullCpuRenderWorker
{
    private readonly INprRenderer _renderer;

    public FullCpuRenderWorker(INprRenderer renderer)
    {
        _renderer = renderer;
    }

    public ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        return _renderer.RenderAsync(request, cancellationToken);
    }
}
