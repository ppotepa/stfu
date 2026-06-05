using System.Text.Json.Serialization;

namespace Agent.Cli.Protocol;

public sealed class AgentEnvelope
{
    public string Schema { get; init; } = "agent.tool.v1";
    public string Tool { get; set; } = "";
    public bool Ok { get; set; } = true;
    public string Root { get; init; } = "";
    public string? Solution { get; init; }
    public Dictionary<string, object?> Query { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public AgentSummary Summary { get; init; } = new();
    public List<Dictionary<string, object?>> Items { get; init; } = [];
    public List<Dictionary<string, object?>> Edges { get; init; } = [];
    public List<Dictionary<string, object?>> Diagnostics { get; init; } = [];
    public List<Dictionary<string, object?>> Metrics { get; init; } = [];
    public List<string> Next { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public AgentError? Error { get; set; }

    [JsonIgnore]
    public int ToolExitCode { get; set; }

    public static AgentEnvelope Create(string tool, string root, string? solution)
    {
        return new AgentEnvelope
        {
            Tool = tool,
            Root = root,
            Solution = solution
        };
    }

    public static AgentEnvelope Failure(string tool, string root, string? solution, string code, string message)
    {
        return new AgentEnvelope
        {
            Tool = tool,
            Root = root,
            Solution = solution,
            Ok = false,
            Error = new AgentError(code, message),
            Summary = new AgentSummary { Count = 0, Returned = 0 }
        };
    }
}

public sealed class AgentSummary
{
    public int Count { get; set; }
    public int Returned { get; set; }
    public bool Truncated { get; set; }
    public long DurationMs { get; set; }
    public string Cache { get; set; } = "none";
}

public sealed record AgentError(string Code, string Message);
