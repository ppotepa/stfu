namespace STFU.UI.Bridge.Assets;

public sealed record AssetListItem(
    string Id,
    string Path,
    int Handle,
    int VertexCount,
    int TriangleCount,
    string Loader,
    string Status);
