using STFU.Strokes;

namespace STFU.NPR.Fields;

public readonly record struct TextureSample(
    Point2D Position,
    float Texture);

public sealed record TextureField(IReadOnlyList<TextureSample> Samples)
{
    public static TextureField Empty { get; } = new([]);
}
