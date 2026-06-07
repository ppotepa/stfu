using STFU.Rendering.Abstractions.Requests;

namespace STFU.UI;

internal enum ViewportPresentationKind
{
    Bitmap,
    DirectGpu
}

internal interface IViewportPresenter
{
    ViewportPresentationKind Kind { get; }

    bool TryPresent(NprRenderResult result, out string availability);
}
