namespace STFU.Rendering.DirectX.Device;

public sealed record DirectXFeatureSupport(
    string AdapterName,
    string FeatureLevel,
    bool SupportsBgra,
    bool SupportsCompute,
    bool SupportsTimestampQueries);
