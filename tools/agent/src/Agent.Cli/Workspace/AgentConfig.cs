using System.Text.Json;

namespace Agent.Cli.Workspace;

public sealed class AgentConfig
{
    public string DefaultSolution { get; set; } = "STFU.slnx";
    public string DefaultFormat { get; set; } = "table";
    public string CacheDirectory { get; set; } = ".agents/cache";
    public string[] ExcludePatterns { get; set; } = [];
    public string[] GeneratedPatterns { get; set; } = [];
    public string[] AlwaysExcludePatterns { get; set; } = [];
    public string DefaultConcatProfile { get; set; } = "";
    public Dictionary<string, ConcatProfile> ConcatProfiles { get; set; } = [];
    public Dictionary<string, string[]> IncludeScopes { get; set; } = [];

    public static AgentConfig Load(string root)
    {
        var path = Path.Combine(root, "tools", "agent", "agent.config.json");
        if (!File.Exists(path))
        {
            return new AgentConfig();
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<AgentConfig>(File.ReadAllText(path), options) ?? new AgentConfig();
    }
}

public sealed class ConcatProfile
{
    public string[] Include { get; set; } = [];
    public string[] Exclude { get; set; } = [];
}
