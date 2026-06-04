using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.DirectX.Backend;

public sealed class DirectXRenderWorker
{
    private readonly INprRenderer _renderer;

    public DirectXRenderWorker(INprRenderer renderer)
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
