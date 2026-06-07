namespace STFU.Rendering.Abstractions.Diagnostics;

public sealed class VisibilityParityStats
{
    public const string FallbackReasonMismatch = "GpuVisibilityMismatch";
    public const string FallbackReasonReadbackFailed = "GpuVisibilityReadbackFailed";

    public int CpuVisibleFaces { get; init; }
    public int GpuVisibleFaces { get; init; }
    public int MatchingFaces { get; init; }
    public int CpuOnlyFaces { get; init; }
    public int GpuOnlyFaces { get; init; }
    public int MissingOnGpu => CpuOnlyFaces;
    public int ExtraOnGpu => GpuOnlyFaces;
    public int MismatchCount => CpuOnlyFaces + GpuOnlyFaces;
    public bool FallbackUsed { get; init; }
    public string FallbackReason { get; init; } = string.Empty;
    public float MatchRatio { get; init; }
    public bool Passed { get; init; }

    public static VisibilityParityStats Empty { get; } = new();

    public static VisibilityParityStats FromCounts(
        int cpuVisibleFaces,
        int gpuVisibleFaces,
        int matchingFaces,
        int cpuOnlyFaces,
        int gpuOnlyFaces,
        bool fallbackUsed = false,
        string fallbackReason = "",
        float requiredMatchRatio = 0.995f)
    {
        var comparedFaces = Math.Max(cpuVisibleFaces + gpuOnlyFaces, 1);
        var matchRatio = matchingFaces / (float)comparedFaces;
        return new VisibilityParityStats
        {
            CpuVisibleFaces = cpuVisibleFaces,
            GpuVisibleFaces = gpuVisibleFaces,
            MatchingFaces = matchingFaces,
            CpuOnlyFaces = cpuOnlyFaces,
            GpuOnlyFaces = gpuOnlyFaces,
            FallbackUsed = fallbackUsed,
            FallbackReason = fallbackReason,
            MatchRatio = matchRatio,
            Passed = (cpuOnlyFaces == 0 && gpuOnlyFaces == 0) || matchRatio >= requiredMatchRatio
        };
    }

    public static VisibilityParityStats Fallback(string reason, int cpuVisibleFaces = 0, int gpuVisibleFaces = 0)
    {
        return new VisibilityParityStats
        {
            CpuVisibleFaces = cpuVisibleFaces,
            GpuVisibleFaces = gpuVisibleFaces,
            FallbackUsed = true,
            FallbackReason = reason,
            Passed = false
        };
    }

    public bool ShouldFallback(float requiredMatchRatio = 0.995f)
    {
        return FallbackUsed || (MismatchCount > 0 && MatchRatio < requiredMatchRatio);
    }

    public string ToDiagnosticString()
    {
        return $"cpuFaces={CpuVisibleFaces}, gpuFaces={GpuVisibleFaces}, matching={MatchingFaces}, cpuOnly={CpuOnlyFaces}, gpuOnly={GpuOnlyFaces}, mismatches={MismatchCount}, matchRatio={MatchRatio:0.000}, passed={Passed}, fallback={FallbackUsed}, fallbackReason={FallbackReason}";
    }
}
