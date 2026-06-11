namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveEvidenceScenarioMatrixBuilder
{
    public static InteractiveEvidenceScenarioMatrix BuildDefault()
    {
        return new InteractiveEvidenceScenarioMatrix(new[]
        {
            new InteractiveEvidenceScenarioMatrixRow("walking-preview", 320, 240, 6, "Preview", 16.67d),
            new InteractiveEvidenceScenarioMatrixRow("walking-balanced", 640, 480, 6, "Balanced", 24d),
            new InteractiveEvidenceScenarioMatrixRow("walking-quality", 960, 540, 4, "Quality", 33.34d),
            new InteractiveEvidenceScenarioMatrixRow("static-preview", 320, 240, 6, "Preview", 16.67d)
        });
    }
}
