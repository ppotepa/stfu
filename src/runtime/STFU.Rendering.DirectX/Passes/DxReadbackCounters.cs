namespace STFU.Rendering.DirectX.Passes;

public sealed class DxReadbackCounters
{
    public long Executed { get; private set; }
    public long RowsCopied { get; private set; }
    public long BytesCopied { get; private set; }

    public void RecordReadback(int rowsCopied, int rowBytes)
    {
        Executed++;
        RowsCopied += rowsCopied;
        BytesCopied += (long)rowsCopied * rowBytes;
    }

    public void Reset()
    {
        Executed = 0;
        RowsCopied = 0;
        BytesCopied = 0;
    }

    public string ToDiagnosticString()
    {
        return $"readbacks={Executed}, rows={RowsCopied}, bytes={BytesCopied}";
    }
}
