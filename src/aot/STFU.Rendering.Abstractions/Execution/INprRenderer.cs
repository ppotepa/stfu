using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Execution;

public interface INprRenderer
{
    ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken);
}
