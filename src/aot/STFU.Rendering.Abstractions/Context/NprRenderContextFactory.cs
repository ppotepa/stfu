using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.Parallelism;
using STFU.Rendering.Abstractions.Requests;

namespace STFU.Rendering.Abstractions.Context;

public static class NprRenderContextFactory
{
    public static NprContext Create(
        NprRenderRequest request,
        NprGraph? graph = null,
        CancellationToken cancellationToken = default)
    {
        return CreateCore(
            request,
            graph ?? new NprGraph(),
            NprSettingsCloner.Clone(request.Settings),
            cancellationToken);
    }

    public static NprContext CreateWithScratch(
        NprRenderRequest request,
        NprRenderContextScratch scratch,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scratch);
        return CreateCore(
            request,
            scratch.Graph,
            NprSettingsCloner.CopyInto(request.Settings, scratch.Settings),
            cancellationToken);
    }

    private static NprContext CreateCore(
        NprRenderRequest request,
        NprGraph graph,
        STFU.NPR.Settings.NprSettings settings,
        CancellationToken cancellationToken)
    {
        var workerBudgetRequest = request.Budget.ToWorkerBudgetRequest();
        var workerCount = WorkerBudget.Resolve(workerBudgetRequest);

        return new NprContext
        {
            WorkerCount = workerCount,
            WorkerBudgetRequest = workerBudgetRequest,
            WorkerBudgetMode = request.Budget.WorkerBudgetMode,
            CancellationToken = cancellationToken,
            FrameId = request.FrameId,
            TimeSeconds = request.TimeSeconds,
            PreviousFrame = request.PreviousFrame,
            IncludeDebugFrame = request.IncludeDebugFrame,
            EnablePassTimings = request.DiagnosticsOptions?.EnablePassTimings ?? true,
            EnableStepAllocationTracking = request.DiagnosticsOptions?.EnableStepAllocationTracking ?? false,
            EnableDetailedStepNotes = request.DiagnosticsOptions?.EnableDetailedStepNotes ?? false,
            Scene = request.Scene,
            Assets = request.Assets,
            Camera = request.Camera,
            Width = request.Width,
            Height = request.Height,
            Settings = settings,
            Style = request.Style,
            StyleSet = request.StyleSet,
            EntityStyles = request.EntityStyles,
            Analysis = request.Analysis,
            FrameHistoryState = request.FrameHistoryState,
            Graph = graph
        };
    }
}
