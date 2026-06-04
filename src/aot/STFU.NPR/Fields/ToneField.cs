using STFU.Strokes;

namespace STFU.NPR.Fields;

public readonly record struct ToneSample(
    Point2D Position,
    float Tone);

public sealed record ToneField(IReadOnlyList<ToneSample> Samples)
{
    public static ToneField Empty { get; } = new([]);
}
