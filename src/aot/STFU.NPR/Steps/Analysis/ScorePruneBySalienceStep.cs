using STFU.NPR.Pipeline;

namespace STFU.NPR.Steps.Analysis;

public sealed class ScorePruneBySalienceStep : INprStep
{
    private readonly ScoreFeatureSalienceStep _score = new();
    private readonly PruneFeatureLinesStep _prune = new();

    public void Execute(NprContext context)
    {
        _score.Execute(context);
        _prune.Execute(context);
    }
}
