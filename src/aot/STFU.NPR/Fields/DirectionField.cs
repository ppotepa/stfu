using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Fields;

public readonly record struct DirectionSample(
    Point2D Position,
    Vector2 Direction);

public sealed record DirectionField(IReadOnlyList<DirectionSample> Samples)
{
    public static DirectionField Empty { get; } = new([]);
}
