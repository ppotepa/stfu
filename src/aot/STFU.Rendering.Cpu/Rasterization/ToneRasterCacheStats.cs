namespace STFU.Rendering.Cpu.Rasterization;

public readonly record struct ToneRasterCacheStats(
    int ToneSurfaceCount,
    int TonePixels,
    int SameSizeFastPathCount,
    int SourceMapReuseCount,
    int CoverageScratchCapacity,
    int AlphaScratchCapacity)
{
    public static ToneRasterCacheStats Empty { get; } = new(0, 0, 0, 0, 0, 0);

    public string ToDiagnosticString()
    {
        return $"tones={ToneSurfaceCount}, pixels={TonePixels}, sameSize={SameSizeFastPathCount}, sourceMapReuse={SourceMapReuseCount}, coverageCapacity={CoverageScratchCapacity}, alphaCapacity={AlphaScratchCapacity}";
    }
}
