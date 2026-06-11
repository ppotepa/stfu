namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed record InteractiveEvidenceScenarioMatrixRow(
    string Scenario,
    int Width,
    int Height,
    int Frames,
    string QualityMode,
    double TargetFrameMs);
