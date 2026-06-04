using STFU.NPR.Pipeline;
using STFU.Strokes.Export;

namespace STFU.NPR.Export;

public sealed class SvgNprDocumentExporter
{
    private readonly NprExportRenderer _renderer = new();
    private readonly SvgStrokeExporter _exporter = new();

    public string ExportToString(INprPipeline pipeline, NprContext sourceContext, SvgExportOptions? options = null)
    {
        var exportContext = _renderer.RenderOfflineExact(pipeline, sourceContext);
        return _exporter.ExportToString(exportContext.Frame, options ?? sourceContext.Style.CreateSvgExportOptions());
    }
}
