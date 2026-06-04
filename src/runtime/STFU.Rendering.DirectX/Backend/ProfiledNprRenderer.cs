using STFU.Rendering.Abstractions.Backend;
using STFU.Rendering.Abstractions.Execution;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.DirectX.Backend;

public sealed class ProfiledNprRenderer : INprRenderer
{
    private readonly ICpuRenderBackend _cpu;
    private readonly IGpuRenderBackend _gpu;

    public ProfiledNprRenderer(ICpuRenderBackend cpu, IGpuRenderBackend gpu)
    {
        _cpu = cpu;
        _gpu = gpu;
    }

    public bool IsGpuAvailable => _gpu.IsAvailable;

    public ValueTask<NprRenderResult> RenderAsync(
        NprRenderRequest request,
        CancellationToken cancellationToken)
    {
        return request.ExecutionProfile switch
        {
            NprExecutionProfile.FullCpuReference => _cpu.RenderAsync(request, cancellationToken),
            NprExecutionProfile.CpuDrivenGpuAccelerated when _gpu.IsAvailable => _gpu.RenderAsync(request, cancellationToken),
            NprExecutionProfile.CpuDrivenGpuAccelerated => _cpu.RenderAsync(
                request with { ExecutionProfile = NprExecutionProfile.FullCpuReference },
                cancellationToken),
            _ => _cpu.RenderAsync(
                request with { ExecutionProfile = NprExecutionProfile.FullCpuReference },
                cancellationToken)
        };
    }
}
