using STFU.Parallelism;

namespace STFU.Rendering.Abstractions.Diagnostics;

public sealed class NprRenderDiagnostics
{
    private readonly List<NprPassTiming> _timings = [];

    public IReadOnlyList<NprPassTiming> Timings => _timings;

    public double TotalMilliseconds { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public int PathCount { get; set; }

    public int LayerCount { get; set; }

    public int ToneSurfaceCount { get; set; }

    public int WorkerCount { get; set; }

    public WorkerBudgetMode WorkerBudgetMode { get; set; } = WorkerBudgetMode.Performance;

    public int ProcessorCount { get; set; }

    public long AllocatedBytes { get; set; }

    public long Readbacks { get; set; }

    public VisibilityParityStats? VisibilityParity { get; set; }

    public string? Notes { get; set; }

    public void AddTiming(string name, double milliseconds, string? notes = null)
    {
        _timings.Add(new NprPassTiming(name, milliseconds, notes));
    }
}
