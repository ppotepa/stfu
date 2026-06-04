using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Mesh;

public sealed class ExtractFeatureCurvesStep : INprStep
{
    public void Execute(NprContext context)
    {
        new ExtractFeatureLinesStep().Execute(context);
    }
}
