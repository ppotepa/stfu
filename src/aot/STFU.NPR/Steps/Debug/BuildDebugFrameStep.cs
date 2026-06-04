using STFU.NPR.Debug;
using STFU.NPR.Fields;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Temporal;

namespace STFU.NPR.Steps.Debug;

public sealed class BuildDebugFrameStep : INprStep
{
    public void Execute(NprContext context)
    {
        var lines = new List<NprDebugLine>(
            context.Graph.Curves.Count +
            context.Graph.VisibilitySegments.Count +
            context.Graph.Candidates.Count * 2 +
            (context.Graph.ToneField?.Samples.Count ?? 0) +
            (context.Graph.DirectionField?.Samples.Count ?? 0) +
            (context.Graph.DensityField?.Samples.Count ?? 0) +
            (context.Graph.TextureField?.Samples.Count ?? 0) +
            context.Graph.CurveMatchesByStableId.Count +
            context.Graph.StrokeMatchesByStableId.Count +
            context.Frame.Paths.Count +
            context.Graph.HatchingPlans.Count * 3 +
            context.Graph.StyleMasks.Sum(mask => mask.ScreenRegions.Count * 4) +
            context.Graph.MaterialRegions.Count * 4);

        foreach (var curve in context.Graph.Curves)
        {
            if (curve.Points.Count < 2)
            {
                continue;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.FeatureCurves,
                curve.Points[0].ScreenPosition,
                curve.Points[^1].ScreenPosition,
                $"{curve.Kind}:{curve.Confidence:0.00}",
                curve.AverageDepth,
                true,
                curve.StableId,
                curve.Confidence));
        }

        var salientSegments = 0;
        foreach (var segment in context.Graph.VisibilitySegments)
        {
            var salience = context.Graph.GetSalience(segment.StableId, segment.Importance);
            if (segment.State == VisibilityState.Visible &&
                salience.Final >= context.Style.GetMinimumSalience(segment.Kind, segment.Intent, context.Settings.MinimumSalience))
            {
                salientSegments++;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.VisibilitySegments,
                segment.Start,
                segment.End,
                segment.State.ToString(),
                segment.Depth,
                segment.State == VisibilityState.Visible,
                segment.StableId,
                salience.Final));

            lines.Add(new NprDebugLine(
                DebugOverlayKind.SalienceHeatmap,
                segment.Start,
                segment.End,
                segment.Intent.ToString(),
                segment.Depth,
                segment.State == VisibilityState.Visible,
                segment.StableId,
                salience.Final));
        }

        foreach (var candidate in context.Graph.Candidates)
        {
            if (candidate.Points.Count < 2)
            {
                continue;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.StrokeCandidates,
                candidate.Points[0],
                candidate.Points[^1],
                candidate.Intent.ToString(),
                candidate.Depth,
                true,
                candidate.StableId,
                candidate.Salience.Final));
        }

        AddToneField(lines, context.Graph.ToneField);
        AddDirectionField(lines, context.Graph.DirectionField);
        AddDensityField(lines, context.Graph.DensityField);
        AddTextureField(lines, context.Graph.TextureField);
        AddTemporalMatches(lines, context);
        AddGhostStrokes(lines, context.Frame);
        AddHatchingPlans(lines, context.Graph.HatchingPlans);
        AddStyleMasks(lines, context.Graph.StyleMasks);
        AddMaterialRegions(lines, context.Graph.MaterialRegions, context.Graph.Triangles, context.Graph.Vertices);

        var visibleSegments = context.Graph.VisibilitySegments.Count(segment => segment.State == VisibilityState.Visible);
        var hiddenSegments = context.Graph.VisibilitySegments.Count - visibleSegments;
        var ghostStrokeCount = context.Frame.Paths.Count(path => path.Metadata?.SourceKind == "GhostStroke");
        var directTemporalMatches = context.Graph.CurveMatchesByStableId.Values.Count(match => match.Kind == TemporalMatchKind.DirectStableIdMatch) +
            context.Graph.StrokeMatchesByStableId.Values.Count(match => match.Kind == TemporalMatchKind.DirectStableIdMatch);
        var fallbackTemporalMatches = context.Graph.CurveMatchesByStableId.Values.Count(match => match.Kind == TemporalMatchKind.SourceScreenOverlapMatch) +
            context.Graph.StrokeMatchesByStableId.Values.Count(match => match.Kind == TemporalMatchKind.SourceScreenOverlapMatch);

        context.DebugFrame = new NprDebugFrame(
            lines,
            new NprDebugCounters(
                context.Graph.Curves.Count,
                visibleSegments,
                hiddenSegments,
                salientSegments,
                context.Graph.Candidates.Count,
                context.Graph.StyledStrokes.Count,
                ghostStrokeCount,
                directTemporalMatches,
                fallbackTemporalMatches),
            context.StepTraces.ToArray());
    }

    private static void AddToneField(List<NprDebugLine> lines, ToneField? field)
    {
        if (field is null)
        {
            return;
        }

        foreach (var sample in field.Samples)
        {
            var length = 3f + sample.Tone * 6f;
            lines.Add(new NprDebugLine(
                DebugOverlayKind.ToneField,
                new STFU.Strokes.Point2D(sample.Position.X - length * 0.5f, sample.Position.Y),
                new STFU.Strokes.Point2D(sample.Position.X + length * 0.5f, sample.Position.Y),
                "Tone",
                sample.Tone,
                true,
                0,
                sample.Tone));
        }
    }

    private static void AddDirectionField(List<NprDebugLine> lines, DirectionField? field)
    {
        if (field is null)
        {
            return;
        }

        foreach (var sample in field.Samples)
        {
            var direction = sample.Direction;
            if (direction.LengthSquared() < 0.0001f)
            {
                continue;
            }

            direction = System.Numerics.Vector2.Normalize(direction);
            const float halfLength = 4f;
            lines.Add(new NprDebugLine(
                DebugOverlayKind.DirectionField,
                new STFU.Strokes.Point2D(sample.Position.X - direction.X * halfLength, sample.Position.Y - direction.Y * halfLength),
                new STFU.Strokes.Point2D(sample.Position.X + direction.X * halfLength, sample.Position.Y + direction.Y * halfLength),
                "Direction",
                0f,
                true,
                0,
                1f));
        }
    }

    private static void AddDensityField(List<NprDebugLine> lines, DensityField? field)
    {
        if (field is null)
        {
            return;
        }

        foreach (var sample in field.Samples)
        {
            var length = 2f + sample.Density * 8f;
            lines.Add(new NprDebugLine(
                DebugOverlayKind.DensityField,
                new STFU.Strokes.Point2D(sample.Position.X, sample.Position.Y - length * 0.5f),
                new STFU.Strokes.Point2D(sample.Position.X, sample.Position.Y + length * 0.5f),
                "Density",
                sample.Density,
                true,
                0,
                sample.Density));
        }
    }

    private static void AddTextureField(List<NprDebugLine> lines, TextureField? field)
    {
        if (field is null)
        {
            return;
        }

        foreach (var sample in field.Samples)
        {
            var half = 2f + sample.Texture * 5f;
            lines.Add(new NprDebugLine(
                DebugOverlayKind.TextureField,
                new STFU.Strokes.Point2D(sample.Position.X - half, sample.Position.Y - half),
                new STFU.Strokes.Point2D(sample.Position.X + half, sample.Position.Y + half),
                "Texture",
                sample.Texture,
                true,
                0,
                sample.Texture));
        }
    }

    private static void AddTemporalMatches(List<NprDebugLine> lines, NprContext context)
    {
        var previous = context.PreviousFrame;
        if (previous is null)
        {
            return;
        }

        foreach (var curve in context.Graph.Curves)
        {
            if (!context.Graph.CurveMatchesByStableId.TryGetValue(curve.StableId, out var match) ||
                !previous.CurvesByStableId.TryGetValue(match.PreviousStableId, out var prior) ||
                curve.Points.Count == 0 ||
                prior.Points.Count == 0)
            {
                continue;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.TemporalMatches,
                Midpoint(curve.Points[0].ScreenPosition, curve.Points[^1].ScreenPosition),
                Midpoint(prior.Points[0].ScreenPosition, prior.Points[^1].ScreenPosition),
                $"curve:{match.Kind}:{context.Graph.CurveStatesByStableId.GetValueOrDefault(curve.StableId, TemporalFeatureState.New)}",
                curve.AverageDepth,
                match.Kind == TemporalMatchKind.DirectStableIdMatch,
                curve.StableId,
                match.Confidence));
        }

        foreach (var stroke in context.Graph.StyledStrokes)
        {
            if (!context.Graph.StrokeMatchesByStableId.TryGetValue(stroke.StableId, out var match) ||
                !previous.StrokesByStableId.TryGetValue(match.PreviousStableId, out var prior) ||
                stroke.Points.Count == 0 ||
                prior.Path.Points.Count == 0)
            {
                continue;
            }

            lines.Add(new NprDebugLine(
                DebugOverlayKind.TemporalMatches,
                Midpoint(stroke.Points[0], stroke.Points[^1]),
                Midpoint(prior.Path.Points[0], prior.Path.Points[^1]),
                $"stroke:{match.Kind}:{stroke.TemporalState}",
                stroke.Depth,
                match.Kind == TemporalMatchKind.DirectStableIdMatch,
                stroke.StableId,
                match.Confidence));
        }
    }

    private static void AddGhostStrokes(List<NprDebugLine> lines, STFU.Strokes.StrokeFrame frame)
    {
        foreach (var path in frame.Paths)
        {
            if (path.Metadata?.SourceKind != "GhostStroke" || path.Points.Count < 2)
            {
                continue;
            }

            for (var index = 1; index < path.Points.Count; index++)
            {
                lines.Add(new NprDebugLine(
                    DebugOverlayKind.GhostStrokes,
                    path.Points[index - 1],
                    path.Points[index],
                    path.Metadata.Visibility ?? "FadingOut",
                    0f,
                    false,
                    path.Metadata.StableId,
                    path.Style.Opacity));
            }
        }
    }

    private static void AddHatchingPlans(List<NprDebugLine> lines, IReadOnlyList<HatchingPlan> plans)
    {
        foreach (var plan in plans)
        {
            AddLayer(lines, plan, plan.Primary, true);

            if (plan.Secondary is not null)
            {
                AddLayer(lines, plan, plan.Secondary, false);
            }

            if (plan.Tertiary is not null)
            {
                AddLayer(lines, plan, plan.Tertiary, false);
            }
        }
    }

    private static void AddLayer(List<NprDebugLine> lines, HatchingPlan plan, HatchLayer layer, bool isPrimary)
    {
        var angle = layer.DirectionAngleOffsetRadians;
        var half = layer.StrokeLengthPixels * 0.5f;
        var dx = MathF.Cos(angle) * half;
        var dy = MathF.Sin(angle) * half;

        lines.Add(new NprDebugLine(
            DebugOverlayKind.HatchingPlan,
            new STFU.Strokes.Point2D(plan.Center.X - dx, plan.Center.Y - dy),
            new STFU.Strokes.Point2D(plan.Center.X + dx, plan.Center.Y + dy),
            layer.Kind.ToString(),
            plan.ToneTarget,
            isPrimary,
            plan.StableId,
            plan.DensityTarget));
    }

    private static STFU.Strokes.Point2D Midpoint(STFU.Strokes.Point2D a, STFU.Strokes.Point2D b)
    {
        return new STFU.Strokes.Point2D((a.X + b.X) * 0.5f, (a.Y + b.Y) * 0.5f);
    }

    private static void AddStyleMasks(List<NprDebugLine> lines, IReadOnlyList<StyleMask> masks)
    {
        foreach (var mask in masks)
        {
            foreach (var polygon in mask.ScreenRegions)
            {
                for (var index = 0; index < polygon.Points.Count; index++)
                {
                    var start = polygon.Points[index];
                    var end = polygon.Points[(index + 1) % polygon.Points.Count];
                    lines.Add(new NprDebugLine(
                        DebugOverlayKind.StyleMask,
                        start,
                        end,
                        mask.Name,
                        0f,
                        true,
                        mask.StableId,
                        mask.Strength));
                }
            }
        }
    }

    private static void AddMaterialRegions(
        List<NprDebugLine> lines,
        IReadOnlyList<MaterialRegion> regions,
        IReadOnlyList<ProjectedTriangle> triangles,
        IReadOnlyList<ProjectedVertex> vertices)
    {
        foreach (var region in regions)
        {
            if (!TryGetRegionBounds(region, triangles, vertices, out var minX, out var minY, out var maxX, out var maxY))
            {
                continue;
            }

            var topLeft = new STFU.Strokes.Point2D(minX, minY);
            var topRight = new STFU.Strokes.Point2D(maxX, minY);
            var bottomRight = new STFU.Strokes.Point2D(maxX, maxY);
            var bottomLeft = new STFU.Strokes.Point2D(minX, maxY);

            lines.Add(new NprDebugLine(DebugOverlayKind.MaterialRegion, topLeft, topRight, $"region-{region.StableId}", region.BaseTone, true, region.StableId, region.BaseTone));
            lines.Add(new NprDebugLine(DebugOverlayKind.MaterialRegion, topRight, bottomRight, $"region-{region.StableId}", region.BaseTone, true, region.StableId, region.BaseTone));
            lines.Add(new NprDebugLine(DebugOverlayKind.MaterialRegion, bottomRight, bottomLeft, $"region-{region.StableId}", region.BaseTone, true, region.StableId, region.BaseTone));
            lines.Add(new NprDebugLine(DebugOverlayKind.MaterialRegion, bottomLeft, topLeft, $"region-{region.StableId}", region.BaseTone, true, region.StableId, region.BaseTone));
        }
    }

    private static bool TryGetRegionBounds(
        MaterialRegion region,
        IReadOnlyList<ProjectedTriangle> triangles,
        IReadOnlyList<ProjectedVertex> vertices,
        out float minX,
        out float minY,
        out float maxX,
        out float maxY)
    {
        minX = float.PositiveInfinity;
        minY = float.PositiveInfinity;
        maxX = float.NegativeInfinity;
        maxY = float.NegativeInfinity;
        var found = false;

        foreach (var triangleIndex in region.TriangleIndices)
        {
            if ((uint)triangleIndex >= (uint)triangles.Count)
            {
                continue;
            }

            var triangle = triangles[triangleIndex];
            found |= TryExpand(vertices, triangle.A, ref minX, ref minY, ref maxX, ref maxY);
            found |= TryExpand(vertices, triangle.B, ref minX, ref minY, ref maxX, ref maxY);
            found |= TryExpand(vertices, triangle.C, ref minX, ref minY, ref maxX, ref maxY);
        }

        return found;
    }

    private static bool TryExpand(
        IReadOnlyList<ProjectedVertex> vertices,
        int vertexIndex,
        ref float minX,
        ref float minY,
        ref float maxX,
        ref float maxY)
    {
        if ((uint)vertexIndex >= (uint)vertices.Count)
        {
            return false;
        }

        var point = vertices[vertexIndex].Position;
        minX = MathF.Min(minX, point.X);
        minY = MathF.Min(minY, point.Y);
        maxX = MathF.Max(maxX, point.X);
        maxY = MathF.Max(maxY, point.Y);
        return true;
    }
}
