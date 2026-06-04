using STFU.NPR.Graph;
using STFU.NPR.Analysis;
using STFU.Strokes;

internal static class NprEvaluationMetric
{
    public static float VisibilityCorrectness(
        IReadOnlyList<VisibilitySegment> actual,
        params VisibilityState[] expectedStates)
    {
        if (actual.Count == 0 || expectedStates.Length == 0)
        {
            return 0f;
        }

        var count = Math.Min(actual.Count, expectedStates.Length);
        var matches = 0;
        for (var index = 0; index < count; index++)
        {
            if (actual[index].State == expectedStates[index])
            {
                matches++;
            }
        }

        return matches / (float)expectedStates.Length;
    }

    public static IReadOnlyDictionary<(int TileX, int TileY), int> StrokeDensityHistogram(
        StrokeFrame frame,
        int tileSize)
    {
        var grid = new ScreenTileGrid<StrokePath2D>(tileSize);

        foreach (var path in frame.Paths)
        {
            if (path.Points.Count == 0)
            {
                continue;
            }

            var midpoint = path.Points[path.Points.Count / 2];
            grid.Add(midpoint.X, midpoint.Y, path);
        }

        return grid.EnumerateTiles().ToDictionary(pair => (pair.Key.X, pair.Key.Y), pair => pair.Value.Count);
    }

    public static float MeanTileDensity(StrokeFrame frame, int tileSize)
    {
        var histogram = StrokeDensityHistogram(frame, tileSize);
        if (histogram.Count == 0)
        {
            return 0f;
        }

        return (float)histogram.Values.Average();
    }
}
