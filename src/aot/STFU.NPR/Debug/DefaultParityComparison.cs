using System.Globalization;
using System.Text;

using STFU.Common.Math;

namespace STFU.NPR.Debug;

public sealed record DefaultParityComparison(
    string LeftLabel,
    string RightLabel,
    DefaultParityComparisonCounts Counts,
    DefaultParityComparisonVertices Vertices,
    DefaultParityComparisonTriangles Triangles,
    DefaultParityComparisonTopologyEdges TopologyEdges,
    DefaultParityComparisonVisibility Visibility,
    DefaultParityComparisonFragments Fragments,
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
            $"Triangles: compared={Triangles.ComparedTriangles}, frontFacingMismatch={Triangles.FrontFacingMismatchCount}, visibleMismatch={Triangles.VisibleMismatchCount}, maxDepthDelta={Triangles.MaxDepthDelta:0.######}, maxAreaDelta={Triangles.MaxScreenAreaDelta:0.######}");
        if (Triangles.FirstMismatchSample is not null)
        {
            builder.AppendLine(
                $"Triangle sample: stableId={Triangles.FirstMismatchSample.StableId}, leftFront={Triangles.FirstMismatchSample.LeftFrontFacing}, rightFront={Triangles.FirstMismatchSample.RightFrontFacing}, leftVisible={Triangles.FirstMismatchSample.LeftVisible}, rightVisible={Triangles.FirstMismatchSample.RightVisible}");
        }
        builder.AppendLine(
            $"TopologyEdges: left={TopologyEdges.LeftCount}, right={TopologyEdges.RightCount}, sharedStableIds={TopologyEdges.SharedStableIdCount}, leftOnly={TopologyEdges.LeftOnlyStableIdCount}, rightOnly={TopologyEdges.RightOnlyStableIdCount}, endpointMismatch={TopologyEdges.EndpointMismatchCount}");
        if (TopologyEdges.RightOnlyStableIdSample.Count > 0 || TopologyEdges.LeftOnlyStableIdSample.Count > 0)
        {
            builder.AppendLine(
                $"Topology samples: leftOnly=[{string.Join(", ", TopologyEdges.LeftOnlyStableIdSample)}], rightOnly=[{string.Join(", ", TopologyEdges.RightOnlyStableIdSample)}]");
        }
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
        if (Visibility.LeftFaceIdHash is not null || Visibility.RightFaceIdHash is not null)
        {
            builder.AppendLine(
                $"Face ownership: leftHash={FormatHash(Visibility.LeftFaceIdHash)}, rightHash={FormatHash(Visibility.RightFaceIdHash)}, match={Visibility.FaceIdHashMatch}");
        }

        builder.AppendLine(
            $"Fragments: compared={Fragments.ComparedFragments}, firstStableIdMismatch={Fragments.FirstStableIdMismatchIndex}, firstGeometryMismatch={Fragments.FirstGeometryMismatchIndex}");
        if (Fragments.FirstMismatchSample is not null)
        {
            builder.AppendLine(
                $"Fragment sample: index={Fragments.FirstMismatchSample.Index}, leftStableId={Fragments.FirstMismatchSample.LeftStableId}, rightStableId={Fragments.FirstMismatchSample.RightStableId}, leftType={Fragments.FirstMismatchSample.LeftType}, rightType={Fragments.FirstMismatchSample.RightType}, leftEdge={Fragments.FirstMismatchSample.LeftEdgeStableId}, rightEdge={Fragments.FirstMismatchSample.RightEdgeStableId}, leftTri={Fragments.FirstMismatchSample.LeftFirstTriangleIndex}, rightTri={Fragments.FirstMismatchSample.RightFirstTriangleIndex}");
        }
        if (Fragments.TriangleDeltaSample.Count > 0)
        {
            builder.AppendLine(
                $"Fragment deltas by triangle: {string.Join(", ", Fragments.TriangleDeltaSample.Select(FormatFragmentDelta))}");
        }
        if (Fragments.EdgeDeltaSample.Count > 0)
        {
            builder.AppendLine(
                $"Fragment deltas by edge: {string.Join(", ", Fragments.EdgeDeltaSample.Select(FormatFragmentDelta))}");
        }

        builder.AppendLine(
            $"Paths: compared={Paths.ComparedPaths}, typeMismatch={Paths.TypeMismatchCount}, maxAvgYDelta={Paths.MaxAverageYDeltaPx:0.###} px, avgAvgYDelta={Paths.AverageAverageYDeltaPx:0.###} px, pointCountMismatch={Paths.PointCountMismatchCount}");
        return builder.ToString().TrimEnd();
    }

    private static string FormatHash(ulong? value)
    {
        return value is ulong hash ? $"0x{hash:X16}" : "n/a";
    }

    private static string FormatFragmentDelta(DefaultParityFragmentCountDeltaSample sample)
    {
        return $"{sample.Key}:{sample.LeftCount}/{sample.RightCount}";
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

public sealed record DefaultParityComparisonTriangles(
    int ComparedTriangles,
    int FrontFacingMismatchCount,
    int VisibleMismatchCount,
    float MaxDepthDelta,
    float MaxScreenAreaDelta,
    DefaultParityTriangleMismatchSample? FirstMismatchSample);

public sealed record DefaultParityTriangleMismatchSample(
    int StableId,
    bool LeftFrontFacing,
    bool RightFrontFacing,
    bool LeftVisible,
    bool RightVisible);

public sealed record DefaultParityComparisonTopologyEdges(
    int LeftCount,
    int RightCount,
    int SharedStableIdCount,
    int LeftOnlyStableIdCount,
    int RightOnlyStableIdCount,
    int EndpointMismatchCount,
    IReadOnlyList<int> LeftOnlyStableIdSample,
    IReadOnlyList<int> RightOnlyStableIdSample);

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
    int? RightOnlyLineVisibleFaceCount = null,
    ulong? LeftFaceIdHash = null,
    ulong? RightFaceIdHash = null,
    bool? FaceIdHashMatch = null);

public sealed record DefaultParityComparisonFragments(
    int ComparedFragments,
    int FirstStableIdMismatchIndex,
    int FirstGeometryMismatchIndex,
    DefaultParityFragmentMismatchSample? FirstMismatchSample,
    IReadOnlyList<DefaultParityFragmentCountDeltaSample> TriangleDeltaSample,
    IReadOnlyList<DefaultParityFragmentCountDeltaSample> EdgeDeltaSample);

public sealed record DefaultParityFragmentMismatchSample(
    int Index,
    int LeftStableId,
    int RightStableId,
    string LeftType,
    string RightType,
    int LeftEdgeStableId,
    int RightEdgeStableId,
    int LeftFirstTriangleIndex,
    int RightFirstTriangleIndex);

public sealed record DefaultParityFragmentCountDeltaSample(
    int Key,
    int LeftCount,
    int RightCount);

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
        var leftTriangles = left.Triangles ?? Array.Empty<DefaultParityTriangleSnapshot>();
        var rightTriangles = right.Triangles ?? Array.Empty<DefaultParityTriangleSnapshot>();
        var leftTopologyEdges = left.TopologyEdges ?? Array.Empty<DefaultParityTopologyEdgeSnapshot>();
        var rightTopologyEdges = right.TopologyEdges ?? Array.Empty<DefaultParityTopologyEdgeSnapshot>();

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

            var screenError = MetricMath.Distance2(
                leftVertex.Screen[0],
                leftVertex.Screen[1],
                rightVertex.Screen[0],
                rightVertex.Screen[1]);
            totalScreenError += screenError;
            maxScreenError = MetricMath.Max(maxScreenError, screenError);

            var ndcError = MetricMath.Distance3(
                leftVertex.Ndc[0],
                leftVertex.Ndc[1],
                leftVertex.Ndc[2],
                rightVertex.Ndc[0],
                rightVertex.Ndc[1],
                rightVertex.Ndc[2]);
            maxNdcError = MetricMath.Max(maxNdcError, ndcError);

            if (leftVertex.IsVisible != rightVertex.IsVisible)
            {
                visibilityMismatchCount++;
            }
        }

        var leftVisibleFaces = left.Visibility?.VisibleFaces ?? Array.Empty<int>();
        var rightVisibleFaces = right.Visibility?.VisibleFaces ?? Array.Empty<int>();
        var leftFaces = leftVisibleFaces.ToHashSet();
        var rightFaces = rightVisibleFaces.ToHashSet();
        var sharedFaces = leftFaces.Intersect(rightFaces).Count();
        var leftOnlyFaces = leftFaces.Except(rightFaces).OrderBy(value => value).ToArray();
        var rightOnlyFaces = rightFaces.Except(leftFaces).OrderBy(value => value).ToArray();
        var leftLineVisibleFaces = left.Visibility?.LineVisibleFaces ?? Array.Empty<int>();
        var rightLineVisibleFaces = right.Visibility?.LineVisibleFaces ?? Array.Empty<int>();
        var leftLineFaces = leftLineVisibleFaces.ToHashSet();
        var rightLineFaces = rightLineVisibleFaces.ToHashSet();
        var sharedLineFaces = leftLineFaces.Intersect(rightLineFaces).Count();
        var leftOnlyLineFaces = leftLineFaces.Except(rightLineFaces).Count();
        var rightOnlyLineFaces = rightLineFaces.Except(leftLineFaces).Count();
        var leftFaceIdHash = left.Visibility?.FaceIdHash;
        var rightFaceIdHash = right.Visibility?.FaceIdHash;
        var unmatchedRightVertices = rightVerticesByKey.Values.Sum(bucket => bucket.Count);

        var comparedTriangles = MetricMath.Min(leftTriangles.Count, rightTriangles.Count);
        var triangleFrontFacingMismatchCount = 0;
        var triangleVisibleMismatchCount = 0;
        var triangleMaxDepthDelta = 0f;
        var triangleMaxAreaDelta = 0f;
        DefaultParityTriangleMismatchSample? triangleSample = null;

        for (var index = 0; index < comparedTriangles; index++)
        {
            var leftTriangle = leftTriangles[index];
            var rightTriangle = rightTriangles[index];

            if (leftTriangle.IsFrontFacing != rightTriangle.IsFrontFacing)
            {
                triangleFrontFacingMismatchCount++;
                triangleSample ??= new DefaultParityTriangleMismatchSample(
                    leftTriangle.StableId,
                    leftTriangle.IsFrontFacing,
                    rightTriangle.IsFrontFacing,
                    leftTriangle.IsVisible,
                    rightTriangle.IsVisible);
            }

            if (leftTriangle.IsVisible != rightTriangle.IsVisible)
            {
                triangleVisibleMismatchCount++;
                triangleSample ??= new DefaultParityTriangleMismatchSample(
                    leftTriangle.StableId,
                    leftTriangle.IsFrontFacing,
                    rightTriangle.IsFrontFacing,
                    leftTriangle.IsVisible,
                    rightTriangle.IsVisible);
            }

            triangleMaxDepthDelta = MetricMath.Max(triangleMaxDepthDelta, MetricMath.AbsoluteDelta(leftTriangle.Depth, rightTriangle.Depth));
            triangleMaxAreaDelta = MetricMath.Max(triangleMaxAreaDelta, MetricMath.AbsoluteDelta(leftTriangle.ScreenArea, rightTriangle.ScreenArea));
        }

        var leftEdgesByStableId = BuildTopologyEdgeBuckets(leftTopologyEdges);
        var rightEdgesByStableId = BuildTopologyEdgeBuckets(rightTopologyEdges);
        var leftEdgeIds = leftEdgesByStableId.Keys.ToHashSet();
        var rightEdgeIds = rightEdgesByStableId.Keys.ToHashSet();
        var sharedEdgeIds = leftEdgeIds.Intersect(rightEdgeIds).ToArray();
        var leftOnlyEdgeIds = leftEdgeIds.Except(rightEdgeIds).OrderBy(value => value).ToArray();
        var rightOnlyEdgeIds = rightEdgeIds.Except(leftEdgeIds).OrderBy(value => value).ToArray();
        var topologyEndpointMismatchCount = 0;
        for (var index = 0; index < sharedEdgeIds.Length; index++)
        {
            var edgeId = sharedEdgeIds[index];
            var leftBucket = leftEdgesByStableId[edgeId];
            var rightBucket = rightEdgesByStableId[edgeId];
            var bucketCount = MetricMath.Min(leftBucket.Count, rightBucket.Count);
            if (leftBucket.Count != rightBucket.Count)
            {
                topologyEndpointMismatchCount++;
                continue;
            }

            for (var bucketIndex = 0; bucketIndex < bucketCount; bucketIndex++)
            {
                var leftEdge = leftBucket[bucketIndex];
                var rightEdge = rightBucket[bucketIndex];
                if (leftEdge.StartVertexIndex != rightEdge.StartVertexIndex ||
                    leftEdge.EndVertexIndex != rightEdge.EndVertexIndex ||
                    leftEdge.FirstTriangleIndex != rightEdge.FirstTriangleIndex ||
                    leftEdge.SecondTriangleIndex != rightEdge.SecondTriangleIndex ||
                    leftEdge.IsBoundary != rightEdge.IsBoundary)
                {
                    topologyEndpointMismatchCount++;
                    break;
                }
            }
        }

        var comparedPaths = MetricMath.Min(left.Paths.Count, right.Paths.Count);
        var typeMismatchCount = 0;
        var pointCountMismatchCount = 0;
        var totalAverageYDelta = 0f;
        var maxAverageYDelta = 0f;
        var comparedFragments = MetricMath.Min(left.Fragments.Count, right.Fragments.Count);
        var firstStableIdMismatchIndex = -1;
        var firstGeometryMismatchIndex = -1;
        DefaultParityFragmentMismatchSample? fragmentMismatchSample = null;

        for (var index = 0; index < comparedFragments; index++)
        {
            var leftFragment = left.Fragments[index];
            var rightFragment = right.Fragments[index];

            if (firstStableIdMismatchIndex < 0 && leftFragment.StableId != rightFragment.StableId)
            {
                firstStableIdMismatchIndex = index;
            }

            if (firstGeometryMismatchIndex < 0 &&
                (!string.Equals(leftFragment.Type, rightFragment.Type, StringComparison.OrdinalIgnoreCase) ||
                 leftFragment.EdgeStableId != rightFragment.EdgeStableId ||
                 leftFragment.FirstTriangleIndex != rightFragment.FirstTriangleIndex ||
                 leftFragment.SecondTriangleIndex != rightFragment.SecondTriangleIndex ||
                 leftFragment.P0[0] != rightFragment.P0[0] ||
                 leftFragment.P0[1] != rightFragment.P0[1] ||
                 leftFragment.P1[0] != rightFragment.P1[0] ||
                 leftFragment.P1[1] != rightFragment.P1[1] ||
                 leftFragment.StartT != rightFragment.StartT ||
                 leftFragment.EndT != rightFragment.EndT))
            {
                firstGeometryMismatchIndex = index;
                fragmentMismatchSample = new DefaultParityFragmentMismatchSample(
                    index,
                    leftFragment.StableId,
                    rightFragment.StableId,
                    leftFragment.Type,
                    rightFragment.Type,
                    leftFragment.EdgeStableId,
                    rightFragment.EdgeStableId,
                    leftFragment.FirstTriangleIndex,
                    rightFragment.FirstTriangleIndex);
            }
        }

        var triangleDeltaSample = BuildFragmentDeltaSamples(
            left.Fragments,
            right.Fragments,
            static fragment => fragment.FirstTriangleIndex);
        var edgeDeltaSample = BuildFragmentDeltaSamples(
            left.Fragments,
            right.Fragments,
            static fragment => fragment.EdgeStableId);

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

            var avgYDelta = MetricMath.AbsoluteDelta(AverageY(leftPath.Points), AverageY(rightPath.Points));
            totalAverageYDelta += avgYDelta;
            maxAverageYDelta = MetricMath.Max(maxAverageYDelta, avgYDelta);
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
            new DefaultParityComparisonTriangles(
                comparedTriangles,
                triangleFrontFacingMismatchCount,
                triangleVisibleMismatchCount,
                triangleMaxDepthDelta,
                triangleMaxAreaDelta,
                triangleSample),
            new DefaultParityComparisonTopologyEdges(
                leftTopologyEdges.Count,
                rightTopologyEdges.Count,
                sharedEdgeIds.Length,
                leftOnlyEdgeIds.Length,
                rightOnlyEdgeIds.Length,
                topologyEndpointMismatchCount,
                leftOnlyEdgeIds.Take(8).ToArray(),
                rightOnlyEdgeIds.Take(8).ToArray()),
            new DefaultParityComparisonVisibility(
                left.Visibility?.VisibleFaceCount ?? leftVisibleFaces.Count,
                right.Visibility?.VisibleFaceCount ?? rightVisibleFaces.Count,
                sharedFaces,
                leftOnlyFaces.Length,
                rightOnlyFaces.Length,
                leftOnlyFaces.Take(8).ToArray(),
                rightOnlyFaces.Take(8).ToArray(),
                left.Visibility?.LineVisibleFaceCount,
                right.Visibility?.LineVisibleFaceCount,
                left.Visibility?.LineVisibleFaceCount is not null || right.Visibility?.LineVisibleFaceCount is not null ? sharedLineFaces : null,
                left.Visibility?.LineVisibleFaceCount is not null || right.Visibility?.LineVisibleFaceCount is not null ? leftOnlyLineFaces : null,
                left.Visibility?.LineVisibleFaceCount is not null || right.Visibility?.LineVisibleFaceCount is not null ? rightOnlyLineFaces : null,
                leftFaceIdHash,
                rightFaceIdHash,
                leftFaceIdHash is not null && rightFaceIdHash is not null ? leftFaceIdHash == rightFaceIdHash : null),
            new DefaultParityComparisonFragments(
                comparedFragments,
                firstStableIdMismatchIndex,
                firstGeometryMismatchIndex,
                fragmentMismatchSample,
                triangleDeltaSample,
                edgeDeltaSample),
            new DefaultParityComparisonPaths(
                comparedPaths,
                typeMismatchCount,
                pointCountMismatchCount,
                maxAverageYDelta,
                comparedPaths > 0 ? totalAverageYDelta / comparedPaths : 0f));
    }

    private static IReadOnlyList<DefaultParityFragmentCountDeltaSample> BuildFragmentDeltaSamples(
        IReadOnlyList<DefaultParityFragmentSnapshot> leftFragments,
        IReadOnlyList<DefaultParityFragmentSnapshot> rightFragments,
        Func<DefaultParityFragmentSnapshot, int> keySelector)
    {
        var counts = new Dictionary<int, (int Left, int Right)>();

        for (var index = 0; index < leftFragments.Count; index++)
        {
            var key = keySelector(leftFragments[index]);
            counts[key] = counts.TryGetValue(key, out var value)
                ? (value.Left + 1, value.Right)
                : (1, 0);
        }

        for (var index = 0; index < rightFragments.Count; index++)
        {
            var key = keySelector(rightFragments[index]);
            counts[key] = counts.TryGetValue(key, out var value)
                ? (value.Left, value.Right + 1)
                : (0, 1);
        }

        return counts
            .Where(pair => pair.Value.Left != pair.Value.Right)
            .OrderByDescending(pair => NumericMath.Abs(pair.Value.Left - pair.Value.Right))
            .ThenBy(pair => pair.Key)
            .Take(8)
            .Select(pair => new DefaultParityFragmentCountDeltaSample(pair.Key, pair.Value.Left, pair.Value.Right))
            .ToArray();
    }

    private static Dictionary<int, List<DefaultParityTopologyEdgeSnapshot>> BuildTopologyEdgeBuckets(
        IReadOnlyList<DefaultParityTopologyEdgeSnapshot> edges)
    {
        var result = new Dictionary<int, List<DefaultParityTopologyEdgeSnapshot>>();
        for (var index = 0; index < edges.Count; index++)
        {
            var edge = edges[index];
            if (!result.TryGetValue(edge.StableId, out var bucket))
            {
                bucket = [];
                result[edge.StableId] = bucket;
            }

            bucket.Add(edge);
        }

        return result;
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
