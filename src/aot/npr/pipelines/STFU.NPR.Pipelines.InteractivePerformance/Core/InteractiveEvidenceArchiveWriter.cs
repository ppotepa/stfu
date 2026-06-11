using System.Text;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public static class InteractiveEvidenceArchiveWriter
{
    public static InteractiveEvidenceArchiveIndex BuildIndex(IEnumerable<(string Path, string Kind, string Content)> files)
    {
        ArgumentNullException.ThrowIfNull(files);

        var entries = new List<InteractiveEvidenceArchiveEntry>();
        foreach (var file in files)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(file.Path);
            var bytes = Encoding.UTF8.GetByteCount(file.Content ?? string.Empty);
            var hash = InteractiveEvidenceHash.Compute(file.Content ?? string.Empty);
            entries.Add(new InteractiveEvidenceArchiveEntry(file.Path, file.Kind, hash, bytes));
        }

        return new InteractiveEvidenceArchiveIndex(entries);
    }

    public static string WriteManifest(InteractiveEvidenceArchiveIndex index)
    {
        ArgumentNullException.ThrowIfNull(index);

        var builder = new StringBuilder();
        builder.AppendLine("path,kind,sha256,length");
        foreach (var entry in index.Entries)
        {
            builder
                .Append(entry.RelativePath).Append(',')
                .Append(entry.Kind).Append(',')
                .Append(entry.Sha256).Append(',')
                .Append(entry.Length).AppendLine();
        }

        return builder.ToString();
    }
}
