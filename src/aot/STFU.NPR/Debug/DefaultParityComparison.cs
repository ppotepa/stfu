using System.Globalization;
using System.Text;

namespace STFU.NPR.Debug;

public sealed record DefaultParityComparison(
    string LeftLabel,
    string RightLabel,
    DefaultParityComparisonCounts Counts,
    DefaultParityComparisonVertices Vertices,
    DefaultParityComparisonVisibility Visibility,
    DefaultParityComparisonPaths Paths)
{
    public string ToConsoleReport()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Compare: {LeftLabel} vs {RightLabel}");
        builder.AppendLine(
            $"Counts: vertices {Counts.LeftVertices}/{Counts.RightVertices}, triangles {Counts.LeftTriangles}/{Counts.RightTriangles}, " +
            $"fragments {Counts.LeftFragments}/{Counts.RightFragments}, paths {Counts.LeftPaths}/{Counts.RightPaths}, drawable {Counts.LeftDrawablePaths}/{Counts.RightDrawablePaths}, strokes {Counts.LeftFinalStrokes}/{Counts.RightFinalStrokes}");
        builder.AppendLine(
            $"Vertices: compared={Vertices.ComparedVertices}, unmatchedLeft={Vertices.UnmatchedLeftVertices}, unmatchedRight={Vertices.UnmatchedRightVertices}, maxScreenError={Vertices.MaxScreenErrorPx:0.###} px, avgScreenError={Vertices.AverageScreenErrorPx:0.###} px, maxNdcError={Vertices.MaxNdcError:0.######}, visibilityMismatch={Vertices.VisibilityMismatchCount}");
        builder.AppendLine(
            $"Visibility: leftFaces={Visibility.LeftVisibleFaceCount}, rightFaces={Visibility.RightVisibleFaceCount}, shared={Visibility.SharedVisibleFaceCount}, leftOnly={Visibility.LeftOnlyVisibleFaceCount}, rightOnly={Visibility.RightOnlyVisibleFaceCount}");
        if (Visibility.LeftOnlyFaceSample.Count > 0 || Visibility.RightOnlyFaceSample.Count > 0)
        {
            builder.AppendLine(
                $"Visibility samples: leftOnly=[{string.Join(", ", Visibility.LeftOnlyFaceSample)}], rightOnly=[{string.Join(", ", Visibility.RightOnlyFaceSample)}]");
        }
        if (Visibility.LeftLineVisibleFaceCount is not null || Visibility.RightLineVisibleFaceCount is not null)
        {
            builder.AppendLine(
                $"Line visibility: leftFaces={Visibility.LeftLineVisibleFaceCount ?? 0}, rightFaces={Visibility.RightLineVisibleFaceCount ?? 0}, shared={Visibility.SharedLineVisibleFaceCount ?? 0}, leftOnly={Visibility.LeftOnlyLineVisibleFaceCount ?? 0}, rightOnly={Visibility.RightOnlyLineVisibleFaceCount ?? 0}");
        }

        builder.AppendLine(
            $"Paths: compared={Paths.ComparedPaths}, typeMismatch={Paths.TypeMismatchCount}, maxAvgYDelta={Paths.MaxAverageYDeltaPx:0.###} px, avgAvgYDelta={Paths.AverageAverageYDeltaPx:0.###} px, pointCountMismatch={Paths.PointCountMismatchCount}");
        return builder.ToString().TrimEnd();
    }
}

public sealed record DefaultParityComparisonCounts(
    int LeftVertices,
    int RightVertices,
    int LeftTriangles,
    int RightTriangles,
    int LeftFragments,
    int RightFragments,
    int LeftPaths,
    int RightPaths,
    int LeftDrawablePaths,
    int RightDrawablePaths,
    int LeftFinalStrokes,
    int RightFinalStrokes);

public sealed record DefaultParityComparisonVertices(
    int ComparedVertices,
    int UnmatchedLeftVertices,
    int UnmatchedRightVertices,
    float MaxScreenErrorPx,
    float AverageScreenErrorPx,
    float MaxNdcError,
    int VisibilityMismatchCount);

public sealed record DefaultParityComparisonVisibility(
    int LeftVisibleFaceCount,
    int RightVisibleFaceCount,
    int SharedVisibleFaceCount,
    int LeftOnlyVisibleFaceCount,
    int RightOnlyVisibleFaceCount,
    IReadOnlyList<int> LeftOnlyFaceSample,
    IReadOnlyList<int> RightOnlyFaceSample,
    int? LeftLineVisibleFaceCount = null,
    int? RightLineVisibleFaceCount = null,
    int? SharedLineVisibleFaceCount = null,
    int? LeftOnlyLineVisibleFaceCount = null,
    int? RightOnlyLineVisibleFaceCount = null);

public sealed record DefaultParityComparisonPaths(
    int ComparedPaths,
    int TypeMismatchCount,
    int PointCountMismatchCount,
    float MaxAverageYDeltaPx,
    float AverageAverageYDeltaPx);

public static class DefaultParitySnapshotComparer
{
    public static DefaultParityComparison Compare(DefaultParitySnapshot left, DefaultParitySnapshot right)
    {
        var rightVerticesByKey = new Dictionary<string, Queue<DefaultParityProjectedVertexSnapshot>>(StringComparer.Ordinal);
        foreach (var vertex in right.ProjectedVertices)
        {
            var key = WorldKey(vertex.WorldPosition);
            if (!rightVerticesByKey.TryGetValue(key, out var bucket))
            {
                bucket = new Queue<DefaultParityProjectedVertexSnapshot>();
                rightVerticesByKey[key] = bucket;
            }

            bucket.Enqueue(vertex);
        }

        var comparedVertices = 0;
        var totalScreenError = 0f;
        var maxScreenError = 0f;
        var maxNdcError = 0f;
        var visibilityMismatchCount = 0;

        foreach (var leftVertex in left.ProjectedVertices)
        {
            var key = WorldKey(leftVertex.WorldPosition);
            if (!rightVerticesByKey.TryGetValue(key, out var bucket) || bucket.Count == 0)
            {
                continue;
            }

            var rightVertex = bucket.Dequeue();
            comparedVertices++;

            var dx = leftVertex.Screen[0] - rightVertex.Screen[0];
            var dy = leftVertex.Screen[1] - rightVertex.Screen[1];
            var screenError = MathF.Sqrt(dx * dx + dy * dy);
            totalScreenError += screenError;
            maxScreenError = Math.Max(maxScreenError, screenError);

            var ndcDx = leftVertex.Ndc[0] - rightVertex.Ndc[0];
            var ndcDy = leftVertex.Ndc[1] - rightVertex.Ndc[1];
            var ndcDz = leftVertex.Ndc[2] - rightVertex.Ndc[2];
            var ndcError = MathF.Sqrt(ndcDx * ndcDx + ndcDy * ndcDy + ndcDz * ndcDz);
            maxNdcError = Math.Max(maxNdcError, ndcError);

            if (leftVertex.IsVisible != rightVertex.IsVisible)
            {
                visibilityMismatchCount++;
            }
        }

        var leftFaces = left.Visibility.VisibleFaces.ToHashSet();
        var rightFaces = right.Visibility.VisibleFaces.ToHashSet();
        var sharedFaces = leftFaces.Intersect(rightFaces).Count();
        var leftOnlyFaces = leftFaces.Except(rightFaces).OrderBy(value => value).ToArray();
        var rightOnlyFaces = rightFaces.Except(leftFaces).OrderBy(value => value).ToArray();
        var leftLineFaces = (left.Visibility.LineVisibleFaces ?? []).ToHashSet();
        var rightLineFaces = (right.Visibility.LineVisibleFaces ?? []).ToHashSet();
        var sharedLineFaces = leftLineFaces.Intersect(rightLineFaces).Count();
        var leftOnlyLineFaces = leftLineFaces.Except(rightLineFaces).Count();
        var rightOnlyLineFaces = rightLineFaces.Except(leftLineFaces).Count();
        var unmatchedRightVertices = rightVerticesByKey.Values.Sum(bucket => bucket.Count);

        var comparedPaths = Math.Min(left.Paths.Count, right.Paths.Count);
        var typeMismatchCount = 0;
        var pointCountMismatchCount = 0;
        var totalAverageYDelta = 0f;
        var maxAverageYDelta = 0f;

        for (var index = 0; index < comparedPaths; index++)
        {
            var leftPath = left.Paths[index];
            var rightPath = right.Paths[index];

            if (!string.Equals(leftPath.Type, rightPath.Type, StringComparison.OrdinalIgnoreCase))
            {
                typeMismatchCount++;
            }

            if (leftPath.Points.Count != rightPath.Points.Count)
            {
                pointCountMismatchCount++;
            }

            var avgYDelta = MathF.Abs(AverageY(leftPath.Points) - AverageY(rightPath.Points));
            totalAverageYDelta += avgYDelta;
            maxAverageYDelta = Math.Max(maxAverageYDelta, avgYDelta);
        }

        return new DefaultParityComparison(
            left.PresetId,
            right.PresetId,
            new DefaultParityComparisonCounts(
                left.ProjectedVertices.Count,
                right.ProjectedVertices.Count,
                left.Counts.Triangles,
                right.Counts.Triangles,
                left.Counts.Fragments,
                right.Counts.Fragments,
                left.Counts.Paths,
                right.Counts.Paths,
                left.Counts.DrawablePaths,
                right.Counts.DrawablePaths,
                left.Counts.FinalStrokes,
                right.Counts.FinalStrokes),
            new DefaultParityComparisonVertices(
                comparedVertices,
                left.ProjectedVertices.Count - comparedVertices,
                unmatchedRightVertices,
                maxScreenError,
                comparedVertices > 0 ? totalScreenError / comparedVertices : 0f,
                maxNdcError,
                visibilityMismatchCount),
            new DefaultParityComparisonVisibility(
                left.Visibility.VisibleFaceCount,
                right.Visibility.VisibleFaceCount,
                sharedFaces,
                leftOnlyFaces.Length,
                rightOnlyFaces.Length,
                leftOnlyFaces.Take(8).ToArray(),
                rightOnlyFaces.Take(8).ToArray(),
                left.Visibility.LineVisibleFaceCount,
                right.Visibility.LineVisibleFaceCount,
                left.Visibility.LineVisibleFaceCount is not null || right.Visibility.LineVisibleFaceCount is not null ? sharedLineFaces : null,
                left.Visibility.LineVisibleFaceCount is not null || right.Visibility.LineVisibleFaceCount is not null ? leftOnlyLineFaces : null,
                left.Visibility.LineVisibleFaceCount is not null || right.Visibility.LineVisibleFaceCount is not null ? rightOnlyLineFaces : null),
            new DefaultParityComparisonPaths(
                comparedPaths,
                typeMismatchCount,
                pointCountMismatchCount,
                maxAverageYDelta,
                comparedPaths > 0 ? totalAverageYDelta / comparedPaths : 0f));
    }

    private static float AverageY(IReadOnlyList<float[]> points)
    {
        if (points.Count == 0)
        {
            return 0f;
        }

        var sum = 0f;
        for (var index = 0; index < points.Count; index++)
        {
            sum += points[index][1];
        }

        return sum / points.Count;
    }

    private static string WorldKey(IReadOnlyList<float> worldPosition)
    {
        if (worldPosition is null || worldPosition.Count < 3)
        {
            return "missing";
        }

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{worldPosition[0]:0.000000}:{worldPosition[1]:0.000000}:{worldPosition[2]:0.000000}");
    }
}
