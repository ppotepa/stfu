using System.Text.RegularExpressions;

namespace Agent.Cli.Workspace;

public static partial class PathFilter
{
    private static readonly string[] BuiltInHardExcludes =
    [
        "**/bin/**",
        "**/obj/**",
        "**/.git/**",
        ".agents/**",
        ".logs/agent/**"
    ];

    public static bool IsAllowedForConcat(string root, AgentConfig config, ToolOptions options, string path, out string reason)
    {
        reason = "";
        var fullPath = Path.GetFullPath(path);
        if (!IsInsideRoot(root, fullPath))
        {
            reason = "outside-root";
            return false;
        }

        var relative = Normalize(Path.GetRelativePath(root, fullPath));
        if (IsHardExcluded(relative, config))
        {
            reason = "hard-exclude";
            return false;
        }

        if (IsProfileExcluded(relative, config, options, out var profileReason))
        {
            reason = profileReason;
            return false;
        }

        return true;
    }

    public static bool IsHardExcluded(string relativePath, AgentConfig config)
    {
        var relative = Normalize(relativePath);
        return BuiltInHardExcludes
            .Concat(config.ExcludePatterns)
            .Concat(config.GeneratedPatterns)
            .Concat(config.AlwaysExcludePatterns)
            .Any(pattern => Matches(relative, pattern));
    }

    public static IEnumerable<string> HardRgGlobArgs(AgentConfig config)
    {
        var patterns = BuiltInHardExcludes
            .Concat(config.ExcludePatterns)
            .Concat(config.GeneratedPatterns)
            .Concat(config.AlwaysExcludePatterns)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in patterns)
        {
            yield return "--glob";
            yield return "!" + Normalize(pattern);
        }
    }

    public static string Normalize(string path)
    {
        return path.Replace('\\', '/').TrimStart('.', '/');
    }

    private static bool IsInsideRoot(string root, string fullPath)
    {
        var normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsProfileExcluded(string relative, AgentConfig config, ToolOptions options, out string reason)
    {
        reason = "";
        var profileName = options.Get("profile") ?? config.DefaultConcatProfile;
        var unrestricted = string.IsNullOrWhiteSpace(profileName) ||
            profileName.Equals("all", StringComparison.OrdinalIgnoreCase) ||
            profileName.Equals("repo", StringComparison.OrdinalIgnoreCase);

        var includePatterns = new List<string>();
        var excludePatterns = new List<string>();
        if (!unrestricted && TryGetProfile(config, profileName, out var profile))
        {
            includePatterns.AddRange(profile.Include);
            excludePatterns.AddRange(profile.Exclude);
        }

        includePatterns.AddRange(ResolveIncludePatterns(config, options.Get("include")));

        if (excludePatterns.Any(pattern => Matches(relative, pattern)))
        {
            reason = string.IsNullOrWhiteSpace(profileName) ? "profile-exclude" : $"profile-exclude:{profileName}";
            return true;
        }

        var includeList = includePatterns.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (includeList.Length == 0)
        {
            return false;
        }

        if (includeList.Any(pattern => Matches(relative, pattern)))
        {
            return false;
        }

        reason = string.IsNullOrWhiteSpace(profileName) ? "not-in-include" : $"not-in-profile:{profileName}";
        return true;
    }

    private static IEnumerable<string> ResolveIncludePatterns(AgentConfig config, string? include)
    {
        foreach (var token in (include ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryGetScope(config, token, out var scopedPatterns))
            {
                foreach (var pattern in scopedPatterns)
                {
                    yield return pattern;
                }
                continue;
            }

            yield return LooksLikeGlob(token) ? token : $"{token.TrimEnd('/', '\\')}/**";
        }
    }

    private static bool LooksLikeGlob(string token)
    {
        return token.Contains('*', StringComparison.Ordinal) ||
               token.Contains('/', StringComparison.Ordinal) ||
               token.Contains('\\', StringComparison.Ordinal) ||
               Path.HasExtension(token);
    }

    private static bool TryGetProfile(AgentConfig config, string name, out ConcatProfile profile)
    {
        foreach (var pair in config.ConcatProfiles)
        {
            if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                profile = pair.Value;
                return true;
            }
        }

        profile = new ConcatProfile();
        return false;
    }

    private static bool TryGetScope(AgentConfig config, string name, out string[] patterns)
    {
        foreach (var pair in config.IncludeScopes)
        {
            if (pair.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                patterns = pair.Value;
                return true;
            }
        }

        patterns = [];
        return false;
    }

    private static bool Matches(string relativePath, string pattern)
    {
        var path = Normalize(relativePath);
        var normalizedPattern = Normalize(pattern);
        if (string.IsNullOrWhiteSpace(normalizedPattern))
        {
            return false;
        }

        if (normalizedPattern.StartsWith("**/*", StringComparison.Ordinal) &&
            !normalizedPattern[4..].Contains('/', StringComparison.Ordinal))
        {
            return path.EndsWith(normalizedPattern[4..], StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedPattern.StartsWith("**/", StringComparison.Ordinal) &&
            normalizedPattern.EndsWith("/**", StringComparison.Ordinal))
        {
            var segment = normalizedPattern[3..^3].Trim('/');
            return path.Equals(segment, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(segment + "/", StringComparison.OrdinalIgnoreCase) ||
                   path.Contains("/" + segment + "/", StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedPattern.EndsWith("/**", StringComparison.Ordinal))
        {
            var prefix = normalizedPattern[..^3].TrimEnd('/');
            return path.Equals(prefix, StringComparison.OrdinalIgnoreCase) ||
                   path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
        }

        if (normalizedPattern.StartsWith("**/", StringComparison.Ordinal))
        {
            var suffix = normalizedPattern[3..];
            if (!suffix.Contains('*', StringComparison.Ordinal))
            {
                return path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
            }
        }

        if (!normalizedPattern.Contains('*', StringComparison.Ordinal))
        {
            return path.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase);
        }

        var regex = "^" + Regex.Escape(normalizedPattern).Replace("\\*\\*", ".*", StringComparison.Ordinal).Replace("\\*", "[^/]*", StringComparison.Ordinal) + "$";
        return Regex.IsMatch(path, regex, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}
