using System.Runtime.CompilerServices;
using STFU.Common.Math;
using STFU.NPR.Composition;
using STFU.NPR.Debug;
using STFU.NPR.Rendering;
using STFU.Rendering.Abstractions.Surfaces;
using STFU.Strokes;

namespace STFU.Rendering.Abstractions.Diagnostics;

public static class NprRenderParityHasher
{
    public static ulong HashPixelSurface(PixelSurface surface)
    {
        ArgumentNullException.ThrowIfNull(surface);

        var hash = Start();
        Add(ref hash, surface.Width);
        Add(ref hash, surface.Height);
        Add(ref hash, surface.Stride);
        Add(ref hash, (int)surface.Format);
        AddBytes(ref hash, surface.Span);
        return hash;
    }

    public static ulong HashStrokeFrame(StrokeFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var hash = Start();
        Add(ref hash, frame.Width);
        Add(ref hash, frame.Height);
        Add(ref hash, frame.Paths.Count);
        for (var i = 0; i < frame.Paths.Count; i++)
        {
            HashStrokePath(ref hash, frame.Paths[i]);
        }

        return hash;
    }

    public static ulong HashNprFrame(NprFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var hash = Start();
        Add(ref hash, frame.Width);
        Add(ref hash, frame.Height);
        HashPaper(ref hash, frame.Paper);
        Add(ref hash, frame.Layers.Count);
        for (var i = 0; i < frame.Layers.Count; i++)
        {
            HashLayer(ref hash, frame.Layers[i]);
        }

        HashStrokeFrame(ref hash, frame.LegacyStrokes);
        return hash;
    }

    public static ulong HashDebugFrame(NprDebugFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        var hash = Start();
        Add(ref hash, frame.Lines.Count);
        for (var i = 0; i < frame.Lines.Count; i++)
        {
            var line = frame.Lines[i];
            Add(ref hash, (int)line.Kind);
            Add(ref hash, line.Start.X);
            Add(ref hash, line.Start.Y);
            Add(ref hash, line.End.X);
            Add(ref hash, line.End.Y);
            Add(ref hash, line.Label);
            Add(ref hash, line.Depth);
            Add(ref hash, line.IsPrimary);
            Add(ref hash, line.SourceId);
            Add(ref hash, line.Value);
        }

        Add(ref hash, frame.Counters.FeatureCurveCount);
        Add(ref hash, frame.Counters.VisibleSegmentCount);
        Add(ref hash, frame.Counters.HiddenSegmentCount);
        Add(ref hash, frame.Counters.SalientSegmentCount);
        Add(ref hash, frame.Counters.StrokeCandidateCount);
        Add(ref hash, frame.Counters.StrokeCount);
        Add(ref hash, frame.Counters.GhostStrokeCount);
        Add(ref hash, frame.Counters.DirectTemporalMatchCount);
        Add(ref hash, frame.Counters.FallbackTemporalMatchCount);

        return hash;
    }

    private static void HashLayer(ref ulong hash, NprLayerFrame layer)
    {
        Add(ref hash, layer.Id);
        Add(ref hash, layer.Name);
        Add(ref hash, (int)layer.Role);
        Add(ref hash, layer.Order);
        Add(ref hash, layer.Visible);
        Add(ref hash, layer.Opacity);
        Add(ref hash, (int)layer.BlendMode);
        Add(ref hash, layer.Tones.Count);
        Add(ref hash, layer.Shading.Count);
        Add(ref hash, layer.Strokes.Count);

        for (var i = 0; i < layer.Tones.Count; i++)
        {
            HashTone(ref hash, layer.Tones[i]);
        }

        for (var i = 0; i < layer.Shading.Count; i++)
        {
            HashStrokePath(ref hash, layer.Shading[i]);
        }

        for (var i = 0; i < layer.Strokes.Count; i++)
        {
            HashStrokePath(ref hash, layer.Strokes[i]);
        }
    }

    private static void HashTone(ref ulong hash, NprToneSurface2D tone)
    {
        Add(ref hash, tone.Id);
        Add(ref hash, tone.LayerId);
        Add(ref hash, (int)tone.Role);
        Add(ref hash, tone.Channel);
        Add(ref hash, tone.Width);
        Add(ref hash, tone.Height);
        Add(ref hash, tone.Opacity);
        AddBytes(ref hash, tone.Rgba);
    }

    private static void HashPaper(ref ulong hash, NprPaper paper)
    {
        Add(ref hash, paper.Color.R);
        Add(ref hash, paper.Color.G);
        Add(ref hash, paper.Color.B);
        Add(ref hash, paper.Opacity);
    }

    private static void HashStrokeFrame(ref ulong hash, StrokeFrame frame)
    {
        Add(ref hash, frame.Width);
        Add(ref hash, frame.Height);
        Add(ref hash, frame.Paths.Count);
        for (var i = 0; i < frame.Paths.Count; i++)
        {
            HashStrokePath(ref hash, frame.Paths[i]);
        }
    }

    private static void HashStrokePath(ref ulong hash, StrokePath2D path)
    {
        Add(ref hash, path.Style.Thickness);
        Add(ref hash, path.Style.Opacity);
        Add(ref hash, path.Style.Color.R);
        Add(ref hash, path.Style.Color.G);
        Add(ref hash, path.Style.Color.B);

        if (path.Metadata is StrokeMetadata metadata)
        {
            Add(ref hash, true);
            Add(ref hash, metadata.StableId);
            Add(ref hash, metadata.Layer);
            Add(ref hash, metadata.SourceKind);
            Add(ref hash, metadata.Intent);
            Add(ref hash, metadata.SourceFeatureId ?? -1);
            Add(ref hash, metadata.SourceSegmentId ?? -1);
            Add(ref hash, metadata.Visibility);
            Add(ref hash, metadata.StyleId);
            Add(ref hash, metadata.Variant);
            Add(ref hash, metadata.LayerOrder);
            Add(ref hash, metadata.EntityId ?? -1);
        }
        else
        {
            Add(ref hash, false);
        }

        if (path.TryGetSegment(out var segmentStart, out var segmentEnd))
        {
            Add(ref hash, 2);
            Add(ref hash, segmentStart.X);
            Add(ref hash, segmentStart.Y);
            Add(ref hash, segmentEnd.X);
            Add(ref hash, segmentEnd.Y);
        }
        else
        {
            Add(ref hash, path.Points.Count);
            for (var i = 0; i < path.Points.Count; i++)
            {
                Add(ref hash, path.Points[i].X);
                Add(ref hash, path.Points[i].Y);
            }
        }

        if (path.RichPoints is { } richPoints)
        {
            Add(ref hash, richPoints.Count);
            for (var i = 0; i < richPoints.Count; i++)
            {
                var point = richPoints[i];
                Add(ref hash, point.Position.X);
                Add(ref hash, point.Position.Y);
                Add(ref hash, point.Thickness);
                Add(ref hash, point.Opacity);
                Add(ref hash, point.Pressure);
            }
        }
        else
        {
            Add(ref hash, -1);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Start() => HashMath.FnvOffset64;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, bool value) => Add(ref hash, value ? 1 : 0);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, byte value)
    {
        hash = HashMath.Fnv1A(hash, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, int value) => Add(ref hash, (uint)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, uint value)
    {
        hash = HashMath.Fnv1A(hash, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, long value) => Add(ref hash, (ulong)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, ulong value) => hash = HashMath.Fnv1A(hash, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, float value) => hash = HashMath.Fnv1A(hash, value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, double value) => hash = HashMath.Fnv1A(hash, value);

    private static void Add(ref ulong hash, string? value)
    {
        if (value is null)
        {
            hash = HashMath.Fnv1A(hash, -1);
            return;
        }

        hash = HashMath.Fnv1A(hash, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, char value) => Add(ref hash, (ushort)value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Add(ref ulong hash, ushort value) => hash = HashMath.Fnv1A(hash, value);

    private static void AddBytes(ref ulong hash, ReadOnlySpan<byte> bytes)
    {
        for (var i = 0; i < bytes.Length; i++)
        {
            hash = HashMath.Fnv1A(hash, bytes[i]);
        }
    }
}
