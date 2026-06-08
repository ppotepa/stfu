namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveDiagnosticsBridge
{
    public static void WriteToContext(NprContext context, InteractiveFrameDiagnostics diagnostics)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(diagnostics);

        // Single integration point for interactive performance diagnostics.
        // Keep no-op for IP-004 if no stable diagnostics sink exists here yet.
        _ = context;
        _ = diagnostics;
    }
}
