using STFU.NPR.Debug;

namespace STFU.NPR.Composition;

public sealed record StyleDebugRule(
    IReadOnlyList<DebugOverlayKind> EnabledOverlays);
