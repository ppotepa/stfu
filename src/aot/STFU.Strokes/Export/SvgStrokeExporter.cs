using System.Globalization;
using System.IO;
using System.Text;
using STFU.Common.Math;

namespace STFU.Strokes.Export;

public sealed class SvgStrokeExporter : IStrokeExporter<SvgExportOptions>
{
    public ExportResult Export(StrokeFrame frame, SvgExportOptions options, Stream output)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        var warnings = new List<ExportWarning>();

        if (!output.CanWrite)
        {
            return new ExportResult(false, "Output stream is not writable.", 0, warnings);
        }

        var pathsByLayer = GroupPaths(frame, options, warnings);
        using var writer = new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true);

        var width = NumericMath.AtLeast(frame.Width * options.Scale, 0f);
        var height = NumericMath.AtLeast(frame.Height * options.Scale, 0f);
        writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        writer.WriteLine(
            $"<svg xmlns=\"http://www.w3.org/2000/svg\" version=\"1.1\" width=\"{Format(width)}{Escape(options.Units)}\" height=\"{Format(height)}{Escape(options.Units)}\" viewBox=\"0 0 {Format(width)} {Format(height)}\">");

        var pathCount = 0;
        foreach (var layer in OrderLayers(pathsByLayer, options))
        {
            writer.WriteLine($"  <g id=\"{Escape(ToLayerId(layer.Key))}\" data-stfu-layer=\"{Escape(layer.Key)}\">");

            foreach (var path in layer.Value)
            {
                if (ShouldWriteRichSegments(path))
                {
                    pathCount += WriteRichSegments(writer, path, options);
                    continue;
                }

                if (!WritePath(writer, path, options, warnings))
                {
                    continue;
                }

                pathCount++;
            }

            writer.WriteLine("  </g>");
        }

        writer.WriteLine("</svg>");
        writer.Flush();
        return new ExportResult(true, null, pathCount, warnings);
    }

    public string ExportToString(StrokeFrame frame, SvgExportOptions? options = null)
    {
        using var stream = new MemoryStream();
        var result = Export(frame, options ?? SvgExportOptions.Default, stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error ?? "SVG export failed.");
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return reader.ReadToEnd();
    }

    private static bool WritePath(
        StreamWriter writer,
        StrokePath2D path,
        SvgExportOptions options,
        List<ExportWarning> warnings)
    {
        var d = BuildPathData(path, options.Scale);
        if (string.IsNullOrWhiteSpace(d))
        {
            warnings.Add(new ExportWarning("empty-path", "Skipped a path with fewer than two points."));
            return false;
        }

        writer.Write("    <path");
        writer.Write($" d=\"{d}\"");
        writer.Write($" fill=\"none\" stroke=\"{ToRgb(path.Style.Color)}\"");
        writer.Write($" stroke-width=\"{Format(path.Style.Thickness * options.Scale)}\"");
        writer.Write($" stroke-opacity=\"{Format(path.Style.Opacity)}\"");
        writer.Write(" stroke-linecap=\"round\" stroke-linejoin=\"round\"");
        if (path.Metadata?.SourceKind == "DashedHiddenStroke")
        {
            writer.Write(" stroke-dasharray=\"6 4\"");
        }

        WriteMetadata(writer, path, options);
        writer.WriteLine(" />");
        return true;
    }

    private static int WriteRichSegments(StreamWriter writer, StrokePath2D path, SvgExportOptions options)
    {
        var richPoints = path.RichPoints!;
        var count = 0;
        for (var index = 1; index < richPoints.Count; index++)
        {
            var start = richPoints[index - 1];
            var end = richPoints[index];
            var style = new StrokeStyle2D(
                NumericMath.AtLeast((start.Thickness + end.Thickness) * 0.5f, 0.2f),
                NumericMath.Clamp01((start.Opacity + end.Opacity) * 0.5f),
                path.Style.Color);
            var segment = new StrokePath2D(
                start.Position,
                end.Position,
                style,
                null,
                path.Metadata);
            writer.Write("    <path");
            writer.Write($" d=\"{BuildPathData(segment, options.Scale)}\"");
            writer.Write($" fill=\"none\" stroke=\"{ToRgb(segment.Style.Color)}\"");
            writer.Write($" stroke-width=\"{Format(segment.Style.Thickness * options.Scale)}\"");
            writer.Write($" stroke-opacity=\"{Format(segment.Style.Opacity)}\"");
            writer.Write(" stroke-linecap=\"round\" stroke-linejoin=\"round\"");
            WriteMetadata(writer, segment, options);
            writer.WriteLine(" />");
            count++;
        }

        return count;
    }

    private static bool ShouldWriteRichSegments(StrokePath2D path)
    {
        return path.Metadata?.SourceKind != "DashedHiddenStroke" &&
            path.RichPoints is { Count: > 2 } richPoints &&
            richPoints.Count == path.Points.Count;
    }

    private static void WriteMetadata(StreamWriter writer, StrokePath2D path, SvgExportOptions options)
    {
        if (!options.IncludeMetadata || path.Metadata is null)
        {
            return;
        }

        var metadata = path.Metadata.Value;
        writer.Write($" data-stfu-stable-id=\"{metadata.StableId}\"");

        if (!string.IsNullOrWhiteSpace(metadata.Layer))
        {
            writer.Write($" data-stfu-layer=\"{Escape(metadata.Layer!)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.SourceKind))
        {
            writer.Write($" data-stfu-source-kind=\"{Escape(metadata.SourceKind!)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Intent))
        {
            writer.Write($" data-stfu-intent=\"{Escape(metadata.Intent!)}\"");
        }

        if (metadata.SourceFeatureId is int sourceFeatureId)
        {
            writer.Write($" data-stfu-feature-id=\"{sourceFeatureId}\"");
        }

        if (metadata.SourceSegmentId is int sourceSegmentId)
        {
            writer.Write($" data-stfu-segment-id=\"{sourceSegmentId}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Visibility))
        {
            writer.Write($" data-stfu-visibility=\"{Escape(metadata.Visibility!)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.StyleId))
        {
            writer.Write($" data-stfu-style=\"{Escape(metadata.StyleId!)}\"");
        }

        if (!string.IsNullOrWhiteSpace(metadata.Variant))
        {
            writer.Write($" data-stfu-variant=\"{Escape(metadata.Variant!)}\"");
        }

        writer.Write($" data-stfu-layer-order=\"{metadata.LayerOrder}\"");
    }

    private static Dictionary<string, List<StrokePath2D>> GroupPaths(
        StrokeFrame frame,
        SvgExportOptions options,
        List<ExportWarning> warnings)
    {
        var enabledLayers = options.EnabledLayers.Count == 0
            ? null
            : new HashSet<string>(options.EnabledLayers, StringComparer.OrdinalIgnoreCase);
        var pathsByLayer = new Dictionary<string, List<StrokePath2D>>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in frame.Paths)
        {
            var layer = path.Metadata?.Layer;
            if (string.IsNullOrWhiteSpace(layer))
            {
                layer = "strokes";
            }

            var isDebugLayer = layer.StartsWith("Debug", StringComparison.OrdinalIgnoreCase) ||
                layer.StartsWith("debug", StringComparison.OrdinalIgnoreCase);
            if (isDebugLayer && !options.IncludeDebugLayers)
            {
                continue;
            }

            if (enabledLayers is not null && !enabledLayers.Contains(layer))
            {
                continue;
            }

            if (path.Points.Count < 2)
            {
                warnings.Add(new ExportWarning("short-path", $"Layer '{layer}' contains a path with fewer than two points."));
                continue;
            }

            if (!pathsByLayer.TryGetValue(layer, out var list))
            {
                list = [];
                pathsByLayer.Add(layer, list);
            }

            list.Add(path);
        }

        return pathsByLayer;
    }

    private static IEnumerable<KeyValuePair<string, List<StrokePath2D>>> OrderLayers(
        Dictionary<string, List<StrokePath2D>> pathsByLayer,
        SvgExportOptions options)
    {
        var preferred = options.EnabledLayers
            .Select((layer, index) => (Layer: layer, Index: index))
            .ToDictionary(entry => entry.Layer, entry => entry.Index, StringComparer.OrdinalIgnoreCase);

        return pathsByLayer
            .OrderByDescending(layer => layer.Value
                .Select(path => path.Metadata?.LayerOrder ?? 100)
                .DefaultIfEmpty(100)
                .Max())
            .ThenBy(layer => preferred.GetValueOrDefault(layer.Key, int.MaxValue))
            .ThenBy(layer => layer.Key, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildPathData(StrokePath2D path, float scale)
    {
        if (path.TryGetSegment(out var segmentStart, out var segmentEnd))
        {
            var segmentBuilder = new StringBuilder();
            segmentBuilder.Append("M ");
            segmentBuilder.Append(Format(segmentStart.X * scale));
            segmentBuilder.Append(' ');
            segmentBuilder.Append(Format(segmentStart.Y * scale));
            segmentBuilder.Append(" L ");
            segmentBuilder.Append(Format(segmentEnd.X * scale));
            segmentBuilder.Append(' ');
            segmentBuilder.Append(Format(segmentEnd.Y * scale));
            return segmentBuilder.ToString();
        }

        if (path.Points.Count < 2)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        for (var index = 0; index < path.Points.Count; index++)
        {
            var point = path.Points[index];
            var command = index == 0 ? 'M' : 'L';
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(command);
            builder.Append(' ');
            builder.Append(Format(point.X * scale));
            builder.Append(' ');
            builder.Append(Format(point.Y * scale));
        }

        return builder.ToString();
    }

    private static string ToRgb(StrokeColor color)
    {
        return $"rgb({color.R},{color.G},{color.B})";
    }

    private static string ToLayerId(string layer)
    {
        var builder = new StringBuilder(layer.Length);
        foreach (var character in layer)
        {
            builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-');
        }

        return builder.ToString().Trim('-');
    }

    private static string Format(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        return value
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal);
    }
}
