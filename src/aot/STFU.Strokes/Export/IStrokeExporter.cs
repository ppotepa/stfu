using System.IO;

namespace STFU.Strokes.Export;

public interface IStrokeExporter<in TOptions>
{
    ExportResult Export(StrokeFrame frame, TOptions options, Stream output);
}
