using System.IO;
using System.Text;
using STFU.Common.Math;

namespace STFU.Strokes.Export;

public sealed class RasterStrokeExporter : IStrokeExporter<RasterExportOptions>
{
    public ExportResult Export(StrokeFrame frame, RasterExportOptions options, Stream output)
    {
        ArgumentNullException.ThrowIfNull(frame);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(output);

        if (!output.CanWrite)
        {
            return new ExportResult(false, "Output stream is not writable.", 0, []);
        }

        var width = NumericMath.AtLeast(options.Width, 1);
        var height = NumericMath.AtLeast(options.Height, 1);
        var pixels = new StrokeColor[width * height];
        Array.Fill(pixels, options.BackgroundColor);

        var pathCount = 0;
        foreach (var path in frame.Paths)
        {
            if (path.Points.Count < 2)
            {
                continue;
            }

            for (var index = 0; index < path.Points.Count - 1; index++)
            {
                DrawLine(
                    pixels,
                    width,
                    height,
                    path.Points[index],
                    path.Points[index + 1],
                    path.Style.Color,
                    NumericMath.AtLeast(path.Style.Thickness * options.Scale, 1f));
            }

            pathCount++;
        }

        using var writer = new StreamWriter(output, new UTF8Encoding(false), 4096, leaveOpen: true);
        writer.WriteLine("P3");
        writer.WriteLine($"{width} {height}");
        writer.WriteLine("255");
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[(y * width) + x];
                writer.Write(pixel.R);
                writer.Write(' ');
                writer.Write(pixel.G);
                writer.Write(' ');
                writer.Write(pixel.B);
                writer.Write(' ');
            }

            writer.WriteLine();
        }

        writer.Flush();
        return new ExportResult(true, null, pathCount, []);
    }

    public string ExportToString(StrokeFrame frame, RasterExportOptions? options = null)
    {
        using var stream = new MemoryStream();
        var result = Export(frame, options ?? RasterExportOptions.Default, stream);
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Error ?? "Raster export failed.");
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return reader.ReadToEnd();
    }

    private static void DrawLine(
        StrokeColor[] pixels,
        int width,
        int height,
        Point2D start,
        Point2D end,
        StrokeColor color,
        float thickness)
    {
        var dx = end.X - start.X;
        var dy = end.Y - start.Y;
        var length = NumericMath.AtLeast(Geometry2D.SegmentLength(start.X, start.Y, end.X, end.Y), 1f);
        var steps = NumericMath.AtLeast((int)NumericMath.Ceiling(length), 1);
        var radius = NumericMath.AtLeast((int)NumericMath.Round(thickness * 0.5f), 0);

        for (var step = 0; step <= steps; step++)
        {
            var t = step / (float)steps;
            var x = (int)NumericMath.Round(start.X + (dx * t));
            var y = (int)NumericMath.Round(start.Y + (dy * t));

            for (var offsetY = -radius; offsetY <= radius; offsetY++)
            {
                for (var offsetX = -radius; offsetX <= radius; offsetX++)
                {
                    Plot(pixels, width, height, x + offsetX, y + offsetY, color);
                }
            }
        }
    }

    private static void Plot(StrokeColor[] pixels, int width, int height, int x, int y, StrokeColor color)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return;
        }

        pixels[(y * width) + x] = color;
    }
}
