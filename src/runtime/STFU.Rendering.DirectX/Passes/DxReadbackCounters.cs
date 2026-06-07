namespace STFU.Rendering.DirectX.Passes;

public sealed class DxReadbackCounters
{
    public long Executed { get; private set; }
    public long Readbacks { get; private set; }
    public long RowsCopied { get; private set; }
    public long BytesCopied { get; private set; }

    public void RecordReadback(int rowsCopied, int rowBytes)
    {
        Executed++;
        Readbacks++;
        RowsCopied += rowsCopied;
        BytesCopied += (long)rowsCopied * rowBytes;
    }

    public void Reset()
    {
        Executed = 0;
        Readbacks = 0;
        RowsCopied = 0;
        BytesCopied = 0;
    }

    public string ToDiagnosticString()
    {
        return $"readbacks={Readbacks}, rows={RowsCopied}, bytes={BytesCopied}";
    }
}
