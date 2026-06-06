using System.Text.Json.Serialization;

namespace STFU.UI.Bridge.Renderer;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(RendererSettingsSnapshot))]
internal partial class RendererSettingsJsonContext : JsonSerializerContext
{
}
