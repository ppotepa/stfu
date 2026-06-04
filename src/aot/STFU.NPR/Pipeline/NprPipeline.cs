using STFU.Strokes;
using System.Diagnostics;

namespace STFU.NPR.Pipeline;

internal static class NprPipelineRunner
{
    public static StrokeFrame Execute(NprContext context, IReadOnlyList<INprStep> steps)
    {
        context.Graph.Clear();
        context.Frame = StrokeFrame.Empty;
        context.NprFrame = STFU.NPR.Rendering.NprFrame.Empty;
        context.StepTraces.Clear();

        foreach (var step in steps)
        {
            var inputCount = CountItems(context);
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var stopwatch = Stopwatch.StartNew();
            step.Execute(context);
            stopwatch.Stop();
            var allocatedBytes = Math.Max(0, GC.GetAllocatedBytesForCurrentThread() - allocatedBefore);

            var outputCount = CountItems(context);
            context.StepTraces.Add(new STFU.NPR.Debug.NprStepTrace(
                step.GetType().Name,
                stopwatch.Elapsed.TotalMilliseconds,
                inputCount,
                outputCount,
                Math.Max(0, inputCount - outputCount),
                BuildNotes(context, step),
                allocatedBytes));
        }

        if (!ReferenceEquals(context.DebugFrame, STFU.NPR.Debug.NprDebugFrame.Empty))
        {
            context.DebugFrame = context.DebugFrame with { StepTraces = context.StepTraces.ToArray() };
        }

        return context.Frame;
    }

    private static int CountItems(NprContext context)
    {
        return context.Graph.Meshes.Count +
            context.Graph.Vertices.Count +
            context.Graph.Triangles.Count +
            context.Graph.TopologyEdges.Count +
            context.Graph.Edges.Count +
            context.Graph.SurfaceSamples.Count +
            context.Graph.Curves.Count +
            context.Graph.VisibilitySegments.Count +
            context.Graph.FeatureLines.Count +
            context.Graph.Candidates.Count +
            context.Graph.StyledStrokes.Count +
            context.Graph.ToneSurfaces.Count +
            context.Frame.Paths.Count;
    }

    private static string BuildNotes(NprContext context, INprStep step)
    {
        return step switch
        {
            _ when step.GetType().Name.Contains("Project", StringComparison.Ordinal) =>
                $"vertices={context.Graph.Vertices.Count}, triangles={context.Graph.Triangles.Count}",
            _ when step.GetType().Name.Contains("Feature", StringComparison.Ordinal) =>
                $"curves={context.Graph.Curves.Count}, lines={context.Graph.FeatureLines.Count}",
            _ when step.GetType().Name.Contains("Visibility", StringComparison.Ordinal) ||
                step.GetType().Name.Contains("Occlusion", StringComparison.Ordinal) =>
                $"visible={context.Graph.VisibilitySegments.Count(segment => segment.State == STFU.NPR.Graph.VisibilityState.Visible)}, hidden={context.Graph.VisibilitySegments.Count(segment => segment.State == STFU.NPR.Graph.VisibilityState.Hidden)}",
            _ when step.GetType().Name.Contains("Stroke", StringComparison.Ordinal) =>
                $"candidates={context.Graph.Candidates.Count}, strokes={context.Graph.StyledStrokes.Count}",
            _ => $"graph={CountItems(context)}"
        };
    }
}

public sealed class NprPipeline<T1> : INprPipeline
    where T1 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
    where T17 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
    where T17 : INprStep, new()
    where T18 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17(), new T18()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
    where T17 : INprStep, new()
    where T18 : INprStep, new()
    where T19 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17(), new T18(), new T19()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
    where T17 : INprStep, new()
    where T18 : INprStep, new()
    where T19 : INprStep, new()
    where T20 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17(), new T18(), new T19(), new T20()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19, T20, T21> : INprPipeline
    where T1 : INprStep, new()
    where T2 : INprStep, new()
    where T3 : INprStep, new()
    where T4 : INprStep, new()
    where T5 : INprStep, new()
    where T6 : INprStep, new()
    where T7 : INprStep, new()
    where T8 : INprStep, new()
    where T9 : INprStep, new()
    where T10 : INprStep, new()
    where T11 : INprStep, new()
    where T12 : INprStep, new()
    where T13 : INprStep, new()
    where T14 : INprStep, new()
    where T15 : INprStep, new()
    where T16 : INprStep, new()
    where T17 : INprStep, new()
    where T18 : INprStep, new()
    where T19 : INprStep, new()
    where T20 : INprStep, new()
    where T21 : INprStep, new()
{
    private readonly INprStep[] _steps = [new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9(), new T10(), new T11(), new T12(), new T13(), new T14(), new T15(), new T16(), new T17(), new T18(), new T19(), new T20(), new T21()];

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}

public sealed class NprPipeline : INprPipeline
{
    private readonly IReadOnlyList<INprStep> _steps;

    public NprPipeline(IReadOnlyList<INprStep> steps)
    {
        _steps = steps;
    }

    public StrokeFrame Execute(NprContext context) => NprPipelineRunner.Execute(context, _steps);
}
