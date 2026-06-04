using System.Text.Json.Serialization;

namespace STFU.NPR.Debug;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DefaultParitySnapshot))]
internal partial class DefaultParitySnapshotJsonContext : JsonSerializerContext
{
}
