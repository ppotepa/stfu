using STFU.Rendering.Abstractions.Execution;

namespace STFU.Rendering.DirectX.Backend;

public sealed class DirectXRenderScheduler : ProfiledNprRenderScheduler
{
    public DirectXRenderScheduler(DirectXRenderWorker worker)
        : base(worker)
    {
    }
}
