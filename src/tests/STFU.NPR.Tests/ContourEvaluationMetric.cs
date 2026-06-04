using STFU.NPR.Graph;
using STFU.Strokes;

internal static class ContourEvaluationMetric
{
    public static (float Precision, float Recall) PrecisionRecall(
        IReadOnlyList<FeatureCurve> actual,
        IReadOnlyList<ExpectedContour> expected,
        float endpointTolerance = 4f)
    {
        if (actual.Count == 0 || expected.Count == 0)
        {
            return (0f, 0f);
        }

        var matchedActual = new HashSet<int>();
        var matchedExpected = new HashSet<int>();

        for (var expectedIndex = 0; expectedIndex < expected.Count; expectedIndex++)
        {
            var target = expected[expectedIndex];
            for (var actualIndex = 0; actualIndex < actual.Count; actualIndex++)
            {
                if (matchedActual.Contains(actualIndex))
                {
                    continue;
                }

                if (Matches(actual[actualIndex], target, endpointTolerance))
                {
                    matchedActual.Add(actualIndex);
                    matchedExpected.Add(expectedIndex);
                    break;
                }
            }
        }

        var precision = matchedActual.Count / (float)Math.Max(1, actual.Count);
        var recall = matchedExpected.Count / (float)Math.Max(1, expected.Count);
        return (precision, recall);
    }

    private static bool Matches(FeatureCurve actual, ExpectedContour expected, float endpointTolerance)
    {
        if (actual.Kind != expected.Kind || actual.Points.Count < 2)
        {
            return false;
        }

        var actualStart = actual.Points[0].ScreenPosition;
        var actualEnd = actual.Points[^1].ScreenPosition;
        return
            (Distance(actualStart, expected.Start) <= endpointTolerance &&
             Distance(actualEnd, expected.End) <= endpointTolerance) ||
            (Distance(actualStart, expected.End) <= endpointTolerance &&
             Distance(actualEnd, expected.Start) <= endpointTolerance);
    }

    private static float Distance(Point2D a, Point2D b)
    {
        var dx = a.X - b.X;
        var dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}

internal sealed record ExpectedContour(
    FeatureCurveKind Kind,
    Point2D Start,
    Point2D End);
