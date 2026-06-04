using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Backend;

public interface IGpuRenderBackend : INprRenderBackend
{
    bool IsAvailable { get; }

    ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken);
}
