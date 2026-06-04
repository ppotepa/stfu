using System.Text.Json.Serialization;

namespace STFU.NPR.Debug;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(NprDebugSnapshot))]
internal partial class NprDebugSnapshotJsonContext : JsonSerializerContext
{
}
