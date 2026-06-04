using STFU.Strokes;

namespace STFU.NPR.Fields;

public readonly record struct DensitySample(
    Point2D Position,
    float Density);

public sealed record DensityField(IReadOnlyList<DensitySample> Samples)
{
    public static DensityField Empty { get; } = new([]);
}
