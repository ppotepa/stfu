using System.Text.Json.Serialization;

namespace STFU.NPR.Composition;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(JsonEditablePresetDocument))]
internal partial class JsonEditablePresetJsonContext : JsonSerializerContext
{
}
