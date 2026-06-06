using STFU.Common.Math;
using STFU.UI.Bridge.Binding;

namespace STFU.UI.Bridge.Assets;

public sealed class AssetRecentItem : BindableObject
{
    private bool _isSelected;

    public AssetRecentItem(string name, string path, string source, string format)
    {
        Name = name;
        Path = path;
        Source = source;
        Format = format;
        FileName = BuildFileName(name, path);
        FolderDisplay = BuildFolderDisplay(path);
        DriveLabel = BuildDriveLabel(path, source);
    }

    public string Name { get; }

    public string Path { get; }

    public string Source { get; }

    public string Format { get; }

    public string FileName { get; }

    public string FolderDisplay { get; }

    public string DriveLabel { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    private static string BuildFileName(string name, string path)
    {
        var fileName = string.IsNullOrWhiteSpace(path) ? string.Empty : System.IO.Path.GetFileName(path);
        return string.IsNullOrWhiteSpace(fileName) ? name : fileName;
    }

    private static string BuildFolderDisplay(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "folder unknown";
        }

        try
        {
            var fullPath = System.IO.Path.GetFullPath(path);
            var directory = System.IO.Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directory))
            {
                return "folder unknown";
            }

            var root = System.IO.Path.GetPathRoot(directory);
            var relativeDirectory = !string.IsNullOrWhiteSpace(root)
                ? System.IO.Path.GetRelativePath(root, directory)
                : directory;

            var parts = relativeDirectory
                .Split([System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries)
                .Where(part => part != ".")
                .ToArray();

            if (parts.Length == 0)
            {
                return directory;
            }

            var tail = parts.Skip(NumericMath.AtLeast(parts.Length - 2, 0));
            var prefix = parts.Length > 2 ? "...\\" : string.Empty;
            return prefix + string.Join("\\", tail);
        }
        catch
        {
            return path;
        }
    }

    private static string BuildDriveLabel(string path, string source)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return source.ToUpperInvariant();
        }

        try
        {
            var root = System.IO.Path.GetPathRoot(System.IO.Path.GetFullPath(path));
            if (string.IsNullOrWhiteSpace(root))
            {
                return source.ToUpperInvariant();
            }

            if (root.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return "NETWORK";
            }

            return $"DYSK {root.TrimEnd(System.IO.Path.DirectorySeparatorChar, System.IO.Path.AltDirectorySeparatorChar)}";
        }
        catch
        {
            return source.ToUpperInvariant();
        }
    }
}
