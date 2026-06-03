using STFU.Strokes;

namespace STFU.NPR.Pipeline;

internal static class NprPipelineRunner
{
    public static StrokeFrame Execute(NprContext context, IReadOnlyList<INprStep> steps)
    {
        context.Graph.Clear();
        context.Frame = StrokeFrame.Empty;

        foreach (var step in steps)
        {
            step.Execute(context);
        }

        return context.Frame;
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
