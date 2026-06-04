using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Analysis;
using STFU.NPR.Styles;
using STFU.Strokes;

namespace STFU.NPR.Steps.Analysis;

public sealed class PruneFeatureLinesStep : INprStep
{
    public void Execute(NprContext context)
    {
        if (context.Graph.VisibilitySegments.Count > 0)
        {
            var keptSegments = PruneVisibleSegments(context, context.Graph.VisibilitySegments);

            context.Graph.ReplaceVisibilitySegments(keptSegments);
            return;
        }

        var kept = new List<FeatureLine>(context.Graph.FeatureLines.Count);

        foreach (var line in context.Graph.FeatureLines)
        {
            if (ShouldKeep(context, line))
            {
                kept.Add(line);
            }
        }

        context.Graph.FeatureLines.Clear();
        context.Graph.FeatureLines.AddRange(kept);
    }

    private static IReadOnlyList<VisibilitySegment> PruneVisibleSegments(
        NprContext context,
        IReadOnlyList<VisibilitySegment> segments)
    {
        var primary = new List<VisibilitySegment>();
        var retainedHidden = new List<VisibilitySegment>();
        var candidatesByTile = new ScreenTileGrid<(VisibilitySegment Segment, float Salience, StrokeBudget Budget)>(
            context.Style.Budget.TileSizePixels);

        foreach (var segment in segments)
        {
            if (segment.State != VisibilityState.Visible)
            {
                if (ShouldKeepHidden(context, segment))
                {
                    retainedHidden.Add(segment);
                }

                continue;
            }

            var priorityRule = context.Style.BuildPriorityRule(
                segment.Kind,
                segment.Intent,
                context.Settings.MinimumStrokeLength,
                context.Style.Budget.MaxSegmentsPerTile);
            var budget = new StrokeBudget(
                context.Style.Budget.MaxSegmentsPerTile,
                priorityRule.MinScreenLength,
                priorityRule.MaxDensityPerTile);
            if (MeasureLength(segment.Start, segment.End) < budget.MinScreenLength)
            {
                continue;
            }

            var salience = context.Graph.GetSalience(segment.StableId, segment.Importance).Final;
            if (!ShouldKeep(context, segment.StableId, segment.Kind, segment.Intent, segment.Importance, salience))
            {
                continue;
            }

            if ((context.Style.Budget.AlwaysKeepPrimaryContours || priorityRule.AlwaysKeepIfOuterSilhouette) &&
                segment.Intent is NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary)
            {
                primary.Add(segment);
                continue;
            }

            var midX = (segment.Start.X + segment.End.X) * 0.5f;
            var midY = (segment.Start.Y + segment.End.Y) * 0.5f;
            candidatesByTile.Add(midX, midY, (segment, salience, budget));
        }

        var kept = new List<VisibilitySegment>(primary.Count + retainedHidden.Count + segments.Count);
        kept.AddRange(retainedHidden);
        kept.AddRange(primary);

        foreach (var tile in candidatesByTile.EnumerateTiles())
        {
            var bucket = tile.Value;
            var densityLimit = bucket
                .Select(entry => entry.Budget.MaxDensityPerTile)
                .DefaultIfEmpty(context.Style.Budget.MaxSegmentsPerTile)
                .Min();
            var tileBudget = new TileDensityBudget(
                tile.Key,
                Math.Max(1, (int)MathF.Floor(densityLimit)),
                bucket.Count);

            foreach (var item in bucket
                .OrderByDescending(entry => entry.Salience)
                .ThenBy(entry => entry.Segment.StableId)
                .Take(tileBudget.AllowedCount))
            {
                kept.Add(item.Segment);
            }
        }

        kept.Sort((left, right) => left.StableId.CompareTo(right.StableId));
        return kept;
    }

    private static bool ShouldKeepHidden(NprContext context, VisibilitySegment segment)
    {
        var hiddenPolicy = context.Style.GetHiddenLinePolicy(segment.Kind, segment.Intent);
        if (hiddenPolicy is Composition.HiddenLinePolicy.Suppress or Composition.HiddenLinePolicy.KeepForDebug)
        {
            return false;
        }

        if (!context.Style.IsEnabled(segment.Kind, segment.Intent))
        {
            return false;
        }

        var minLength = Math.Max(1f, context.Settings.MinimumStrokeLength * 0.6f);
        if (MeasureLength(segment.Start, segment.End) < minLength)
        {
            return false;
        }

        var salience = context.Graph.GetSalience(segment.StableId, segment.Importance).Final;
        return salience >= context.Style.GetMinimumSalience(segment.Kind, segment.Intent, context.Settings.MinimumSalience) * 0.45f;
    }

    private static bool ShouldKeep(NprContext context, VisibilitySegment segment)
    {
        return ShouldKeep(
            context,
            segment.StableId,
            segment.Kind,
            segment.Intent,
            segment.Importance,
            context.Graph.GetSalience(segment.StableId, segment.Importance).Final);
    }

    private static bool ShouldKeep(NprContext context, FeatureLine line)
    {
        return ShouldKeep(
            context,
            line.StableId,
            InferKind(line.Intent),
            line.Intent,
            line.Importance,
            context.Graph.GetSalience(line.StableId, line.Importance).Final);
    }

    private static bool ShouldKeep(
        NprContext context,
        int stableId,
        FeatureCurveKind kind,
        NprStrokeIntent intent,
        float importance,
        float salience)
    {
        if (!context.Style.IsEnabled(kind, intent))
        {
            return false;
        }

        if (intent is NprStrokeIntent.Silhouette or NprStrokeIntent.Boundary)
        {
            return true;
        }

        if (salience < context.Style.GetMinimumSalience(kind, intent, context.Settings.MinimumSalience))
        {
            return false;
        }

        var density = intent switch
        {
            NprStrokeIntent.Crease => MathF.Min(1f, context.Settings.FeatureLineDensity + importance * 0.2f),
            NprStrokeIntent.Hatch => context.Settings.HatchDensity,
            NprStrokeIntent.SurfaceFlow => context.Settings.SurfaceFlowDensity,
            _ => context.Settings.FeatureLineDensity
        };

        var keepChance = Math.Clamp(density * (0.45f + salience * 0.65f), 0f, 1f);
        return NprRandom.Float01(NprRandom.Hash(context.Settings.Seed, stableId)) <= keepChance;
    }

    private static float MeasureLength(Point2D start, Point2D end)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
    private static FeatureCurveKind InferKind(NprStrokeIntent intent)
    {
        return intent switch
        {
            NprStrokeIntent.Boundary => FeatureCurveKind.Boundary,
            NprStrokeIntent.Silhouette => FeatureCurveKind.Silhouette,
            NprStrokeIntent.Crease => FeatureCurveKind.Crease,
            NprStrokeIntent.SurfaceFlow => FeatureCurveKind.SurfaceFlow,
            NprStrokeIntent.Hatch => FeatureCurveKind.Hatch,
            _ => FeatureCurveKind.Accent
        };
    }
}
