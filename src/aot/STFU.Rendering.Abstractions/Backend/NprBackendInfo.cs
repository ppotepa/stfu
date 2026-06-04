namespace STFU.Rendering.Abstractions.Backend;

public sealed record NprBackendInfo(
    string Id,
    string Name,
    NprBackendKind Kind,
    NprBackendCapabilities Capabilities,
    string Description);
