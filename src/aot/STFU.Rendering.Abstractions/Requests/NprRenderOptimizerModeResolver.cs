namespace STFU.Rendering.Abstractions.Requests;

public static class NprRenderOptimizerModeResolver
{
    public static NprRenderOptimizerMode ResolveFromEnvironment()
    {
        var envValue = Environment.GetEnvironmentVariable("STFU_RENDER_OPTIMIZED");
        if (string.IsNullOrWhiteSpace(envValue))
        {
            return NprRenderOptimizerMode.Auto;
        }

        return envValue.Trim() switch
        {
            "0" => NprRenderOptimizerMode.Off,
            "1" => NprRenderOptimizerMode.On,
            _ => Parse(envValue)
        };
    }

    public static NprRenderOptimizerMode Parse(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "auto" => NprRenderOptimizerMode.Auto,
            "off" or "0" or "false" => NprRenderOptimizerMode.Off,
            "on" or "1" or "true" => NprRenderOptimizerMode.On,
            _ => throw new InvalidOperationException($"Unsupported render optimizer mode '{value}'. Expected auto/on/off.")
        };
    }
}
