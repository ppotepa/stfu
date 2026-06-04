using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Backend;

public interface ICpuRenderBackend : INprRenderBackend
{
    ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken);
}
