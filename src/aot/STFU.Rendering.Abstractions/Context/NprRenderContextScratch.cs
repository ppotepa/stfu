using STFU.NPR.Graph;
using STFU.NPR.Settings;

namespace STFU.Rendering.Abstractions.Context;

public sealed class NprRenderContextScratch
{
    public NprGraph Graph { get; } = new();

    public NprSettings Settings { get; } = new();
}
