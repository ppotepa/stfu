using System.Numerics;
using STFU.Strokes;

namespace STFU.NPR.Fields;

public readonly record struct DirectionFieldSample(
    Point2D Position,
    Vector2 Direction)
{
    public static DirectionFieldSample From(DirectionSample sample)
    {
        return new DirectionFieldSample(sample.Position, sample.Direction);
    }
}
