using STFU.NPR.Fields;
using STFU.NPR.Graph;
using STFU.NPR.Pipeline;
using STFU.NPR.Composition;
using STFU.NPR.Temporal;
using STFU.Strokes;

namespace STFU.NPR.Steps.Strokes;

public sealed class StyleStrokesStep : INprStep
{
    public void Execute(NprContext context)
    {
        context.Graph.StyledStrokes.Clear();

        foreach (var candidate in context.Graph.Candidates)
        {
            var stroke = new StyledStroke(
                candidate.StableId,
                candidate.FeatureCurveId,
                candidate.Kind,
                candidate.Intent,
                candidate.Points,
                candidate.Depth,
                candidate.Shade,
                candidate.Importance,
                candidate.Visibility,
                candidate.Tone,
                candidate.Density,
                candidate.HatchLayerKind);

            var layerName = context.Style.ResolveOutputLayer(candidate.Kind, candidate.Intent, candidate.Visibility, candidate.HatchLayerKind);
            var depthFactor = 1f / (1f + MathF.Max(0f, stroke.Depth) * 0.16f);
            var lengthFactor = Math.Clamp(candidate.ScreenLength / 180f, 0f, 1f);
            var shadeBoost = Math.Clamp(candidate.Shade, 0f, 1f);
            var densityBoost = 0.85f + candidate.Density * 0.35f;
            var textureBoost = 1f;
            var textureOpacity = 1f;
            if (context.Graph.TextureField is { Samples.Count: > 0 })
            {
                var texture = SampleTexture(context.Graph.TextureField, candidate.Points[0], 0.4f);
                textureBoost = 0.94f + texture * 0.18f;
                textureOpacity = 0.92f + texture * 0.16f;
            }
            var toneBias = 1f;
            if (context.Style.Tone.Enabled)
            {
                toneBias = 0.7f +
                    candidate.Tone * context.Style.Tone.ToneInfluence +
                    candidate.Shade * context.Style.Tone.ShadeInfluence;
            }
            var salienceBias = 0.7f + candidate.Salience.Final * 0.45f;
            var profile = context.Style.Stroke.FindProfile(candidate.Kind, candidate.Intent, layerName);
            var baseThickness = profile?.BaseThickness ?? 1.2f;
            var baseOpacity = profile?.BaseOpacity ??
                (candidate.Intent == NprStrokeIntent.SurfaceFlow ? 0.22f + shadeBoost * 0.26f : 0.65f);
            var color = profile?.Color ?? StrokeColor.Black;
            var hasTemporalMatch = context.Graph.StrokeMatchesByStableId.TryGetValue(candidate.StableId, out var temporalMatch);

            var computedThickness = MathF.Max(0.35f, baseThickness * context.Style.Stroke.ThicknessScale * (0.72f + depthFactor * 0.34f + lengthFactor * 0.12f) * densityBoost * salienceBias * textureBoost);
            var computedOpacity = Math.Clamp(
                baseOpacity * context.Style.Stroke.OpacityScale * (0.55f + depthFactor * 0.45f) * (0.8f + stroke.Importance * 0.15f + candidate.Salience.Final * 0.2f) * toneBias * textureOpacity,
                context.Style.Tone.MinimumOpacity,
                context.Style.Tone.MaximumOpacity);
            if (hasTemporalMatch && context.PreviousFrame is not null &&
                context.PreviousFrame.StrokesByStableId.TryGetValue(temporalMatch!.PreviousStableId, out var previousStroke))
            {
                computedThickness = Lerp(previousStroke.Path.Style.Thickness, computedThickness, 0.65f);
                computedOpacity = Math.Clamp(
                    Lerp(previousStroke.Path.Style.Opacity, computedOpacity, 0.7f),
                    context.Style.Tone.MinimumOpacity,
                    context.Style.Tone.MaximumOpacity);
            }
            else if (context.PreviousFrame is not null)
            {
                computedOpacity *= 0.88f;
            }

            stroke.Thickness = computedThickness;
            stroke.Opacity = computedOpacity;
            stroke.Color = color;
            stroke.TemporalState = context.Graph.StrokeStatesByStableId.GetValueOrDefault(candidate.StableId, TemporalStrokeState.FadingIn);
            if (candidate.Visibility == VisibilityState.Hidden)
            {
                ApplyHiddenPolicy(stroke, context.Style.GetHiddenLinePolicy(candidate.Kind, candidate.Intent), context);
            }
            if (stroke.TemporalState == TemporalStrokeState.Replaced)
            {
                stroke.Opacity = Math.Clamp(stroke.Opacity * 0.94f, context.Style.Tone.MinimumOpacity, context.Style.Tone.MaximumOpacity);
                stroke.Thickness = MathF.Max(0.3f, stroke.Thickness * 0.97f);
            }
            context.Graph.StyledStrokes.Add(stroke);
        }
    }

    private static float Lerp(float start, float end, float t)
    {
        return start + (end - start) * t;
    }

    private static void ApplyHiddenPolicy(StyledStroke stroke, HiddenLinePolicy policy, NprContext context)
    {
        switch (policy)
        {
            case HiddenLinePolicy.Ghost:
                stroke.Opacity = Math.Clamp(stroke.Opacity * 0.35f, context.Style.Tone.MinimumOpacity, context.Style.Tone.MaximumOpacity * 0.45f);
                stroke.Thickness = MathF.Max(0.25f, stroke.Thickness * 0.88f);
                break;
            case HiddenLinePolicy.Dashed:
                stroke.Opacity = Math.Clamp(stroke.Opacity * 0.55f, context.Style.Tone.MinimumOpacity, context.Style.Tone.MaximumOpacity * 0.65f);
                stroke.Thickness = MathF.Max(0.25f, stroke.Thickness * 0.92f);
                break;
        }
    }

    private static float SampleTexture(TextureField field, Point2D point, float fallback)
    {
        var bestDistance = float.MaxValue;
        var bestTexture = fallback;

        foreach (var sample in field.Samples)
        {
            var dx = sample.Position.X - point.X;
            var dy = sample.Position.Y - point.Y;
            var distance = dx * dx + dy * dy;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            bestTexture = sample.Texture;
        }

        return bestTexture;
    }
}
