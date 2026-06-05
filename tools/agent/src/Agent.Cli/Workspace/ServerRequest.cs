namespace Agent.Cli.Workspace;

public sealed class ServerRequest
{
    public string Command { get; set; } = "";
    public string[] Args { get; set; } = [];
}
