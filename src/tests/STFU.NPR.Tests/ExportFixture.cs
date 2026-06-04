using STFU.Strokes;
using STFU.Strokes.Export;

internal static class ExportFixture
{
    public static StrokeFrame CreateMiniFrame()
    {
        return new StrokeFrame(
            64,
            64,
            [
                StrokePath2D.Line(
                    new Point2D(8f, 8f),
                    new Point2D(48f, 40f),
                    new StrokeStyle2D(1.5f, 0.8f, StrokeColor.Black))
            ]);
    }

    public static string ExportSvg()
    {
        return new SvgStrokeExporter().ExportToString(CreateMiniFrame(), SvgExportOptions.Default);
    }
}
