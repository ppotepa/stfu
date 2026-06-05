namespace Agent.Cli.Workspace;

public static class TextLines
{
    public static string[] Split(string text)
    {
        return text.Replace("\r", "", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }
}
