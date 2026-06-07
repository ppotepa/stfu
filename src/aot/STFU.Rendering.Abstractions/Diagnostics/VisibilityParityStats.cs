namespace STFU.Rendering.Abstractions.Diagnostics;

public sealed class VisibilityParityStats
{
    public int CpuVisibleFaces { get; init; }
    public int GpuVisibleFaces { get; init; }
    public int MatchingFaces { get; init; }
    public int CpuOnlyFaces { get; init; }
    public int GpuOnlyFaces { get; init; }
    public float MatchRatio { get; init; }
    public bool Passed { get; init; }

    public static VisibilityParityStats Empty { get; } = new();

    public string ToDiagnosticString()
    {
        return $"cpuFaces={CpuVisibleFaces}, gpuFaces={GpuVisibleFaces}, matching={MatchingFaces}, cpuOnly={CpuOnlyFaces}, gpuOnly={GpuOnlyFaces}, matchRatio={MatchRatio:0.000}, passed={Passed}";
    }
}
