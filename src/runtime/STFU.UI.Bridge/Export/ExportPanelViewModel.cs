using System.Windows.Input;
using STFU.Strokes.Export;
using STFU.UI.Bridge.Binding;
using STFU.UI.Bridge.Session;

namespace STFU.UI.Bridge.Export;

public sealed class ExportPanelViewModel : BindableObject
{
    private readonly UiEngineSession _session;
    private string _outputPath = "artifacts/export.svg";

    public ExportPanelViewModel(UiEngineSession session)
    {
        _session = session;
        ExportSvgCommand = new RelayCommand(ExportSvg);
        ExportRawPathsCommand = new RelayCommand(() => _session.Commands.Record("Export raw paths requested"));
        ExportDebugSnapshotCommand = new RelayCommand(() => _session.Commands.Record("Export debug snapshot requested"));
    }

    public string OutputPath
    {
        get => _outputPath;
        set => SetProperty(ref _outputPath, value);
    }

    public ICommand ExportSvgCommand { get; }

    public ICommand ExportRawPathsCommand { get; }

    public ICommand ExportDebugSnapshotCommand { get; }

    private void ExportSvg()
    {
        try
        {
            var path = Path.GetFullPath(OutputPath);
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var stream = File.Create(path);
            var result = new SvgStrokeExporter().Export(
                _session.Strokes.CurrentFrame,
                SvgExportOptions.Default,
                stream);

            _session.Commands.Record(result.Success
                ? $"Export SVG -> {path} ({result.PathCount} paths)"
                : $"Export SVG failed: {result.Error}");
        }
        catch (Exception ex)
        {
            _session.Commands.Record($"Export SVG failed: {ex.Message}");
        }
    }
}
