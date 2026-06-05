namespace Agent.Cli.Workspace;

public static class RepositoryRoot
{
    public static string Find(string start)
    {
        var current = new DirectoryInfo(start);
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git")) ||
                File.Exists(Path.Combine(current.FullName, "global.json")) ||
                Directory.EnumerateFiles(current.FullName, "*.sln*").Any())
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return start;
    }
}
