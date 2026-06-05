using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Agent.Cli.Workspace;

public static class CacheStore
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static string DirectoryPath(string root, AgentConfig config) => Path.GetFullPath(Path.Combine(root, config.CacheDirectory));

    public static void Ensure(string root, AgentConfig config)
    {
        var dir = DirectoryPath(root, config);
        Directory.CreateDirectory(dir);
        var manifest = Path.Combine(dir, "manifest.json");
        if (!File.Exists(manifest))
        {
            File.WriteAllText(manifest, JsonSerializer.Serialize(new
            {
                schema = "agent.cache.v1",
                createdAt = DateTimeOffset.Now,
                root
            }, Options));
        }
    }

    public static void WriteJsonLines(string root, AgentConfig config, string name, IEnumerable<Dictionary<string, object?>> items)
    {
        Ensure(root, config);
        var itemArray = items.ToArray();
        var path = Path.Combine(DirectoryPath(root, config), name);
        File.WriteAllLines(path, itemArray.Select(item => JsonSerializer.Serialize(item, Options)));
        WriteSqlite(root, config, Path.GetFileNameWithoutExtension(name), itemArray);
    }

    public static IReadOnlyList<Dictionary<string, object?>> ReadItems(string root, AgentConfig config, string bucket, ToolOptions options)
    {
        var dbPath = Path.Combine(DirectoryPath(root, config), "index.sqlite");
        if (!File.Exists(dbPath)) return [];
        using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT json FROM items
            WHERE bucket = $bucket
              AND ($itemType = '' OR item_type = $itemType)
              AND ($project = '' OR project LIKE $projectLike)
              AND ($file = '' OR file LIKE $fileLike)
              AND ($symbol = '' OR symbol_id = $symbol)
            ORDER BY updated_at DESC
            LIMIT $max;
            """;
        Add(command, "$bucket", bucket);
        Add(command, "$itemType", options.Get("item-type") ?? "");
        Add(command, "$project", options.Get("project") ?? "");
        Add(command, "$projectLike", "%" + (options.Get("project") ?? "") + "%");
        Add(command, "$file", options.Get("file") ?? "");
        Add(command, "$fileLike", "%" + (options.Get("file") ?? "") + "%");
        Add(command, "$symbol", options.Get("symbol-id") ?? "");
        Add(command, "$max", options.Max);
        using var reader = command.ExecuteReader();
        var items = new List<Dictionary<string, object?>>();
        while (reader.Read())
        {
            var json = reader.GetString(0);
            var item = JsonSerializer.Deserialize<Dictionary<string, object?>>(json, Options);
            if (item != null) items.Add(item);
        }

        return items;
    }

    public static bool IsAvailable(string root, AgentConfig config, string bucket)
    {
        var dbPath = Path.Combine(DirectoryPath(root, config), "index.sqlite");
        if (!File.Exists(dbPath)) return false;
        try
        {
            using var connection = new SqliteConnection($"Data Source={dbPath};Mode=ReadOnly");
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM items WHERE bucket = $bucket LIMIT 1;";
            Add(command, "$bucket", bucket);
            return command.ExecuteScalar() != null;
        }
        catch
        {
            return false;
        }
    }

    private static void WriteSqlite(string root, AgentConfig config, string bucket, IReadOnlyList<Dictionary<string, object?>> items)
    {
        var dbPath = Path.Combine(DirectoryPath(root, config), "index.sqlite");
        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();
        Execute(connection, """
            CREATE TABLE IF NOT EXISTS items (
                bucket TEXT NOT NULL,
                id TEXT NOT NULL,
                item_type TEXT NULL,
                symbol_id TEXT NULL,
                project TEXT NULL,
                file TEXT NULL,
                line INTEGER NULL,
                json TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                PRIMARY KEY(bucket, id)
            );
            """);
        Execute(connection, "CREATE INDEX IF NOT EXISTS idx_items_symbol ON items(symbol_id);");
        Execute(connection, "CREATE INDEX IF NOT EXISTS idx_items_file ON items(file, line);");
        Execute(connection, "CREATE INDEX IF NOT EXISTS idx_items_project ON items(project);");
        Execute(connection, "DELETE FROM items WHERE bucket = $bucket;", ("$bucket", bucket));

        using var transaction = connection.BeginTransaction();
        foreach (var item in items)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR REPLACE INTO items(bucket,id,item_type,symbol_id,project,file,line,json,updated_at)
                VALUES($bucket,$id,$itemType,$symbolId,$project,$file,$line,$json,$updatedAt);
                """;
            Add(command, "$bucket", bucket);
            Add(command, "$id", StableItemId(item));
            Add(command, "$itemType", Value(item, "itemType"));
            Add(command, "$symbolId", Value(item, "symbolId"));
            Add(command, "$project", Value(item, "project"));
            Add(command, "$file", Value(item, "file"));
            Add(command, "$line", int.TryParse(Value(item, "line"), out var line) ? line : DBNull.Value);
            Add(command, "$json", JsonSerializer.Serialize(item, Options));
            Add(command, "$updatedAt", DateTimeOffset.Now.ToString("O"));
            command.ExecuteNonQuery();
        }
        transaction.Commit();
    }

    private static void Execute(SqliteConnection connection, string sql, params (string Name, object? Value)[] parameters)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        foreach (var parameter in parameters)
        {
            Add(command, parameter.Name, parameter.Value);
        }
        command.ExecuteNonQuery();
    }

    private static void Add(SqliteCommand command, string name, object? value)
    {
        command.Parameters.AddWithValue(name, value ?? DBNull.Value);
    }

    private static string Value(Dictionary<string, object?> item, string key)
    {
        return item.TryGetValue(key, out var value) ? value?.ToString() ?? "" : "";
    }

    private static string StableItemId(Dictionary<string, object?> item)
    {
        var explicitId = Value(item, "id");
        if (!string.IsNullOrWhiteSpace(explicitId)) return explicitId;
        var symbolId = Value(item, "symbolId");
        var file = Value(item, "file");
        var line = Value(item, "line");
        var name = Value(item, "name");
        var display = Value(item, "displayName");
        var basis = $"{symbolId}|{file}|{line}|{name}|{display}|{JsonSerializer.Serialize(item, Options)}";
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(basis))).ToLowerInvariant()[..16];
    }
}
