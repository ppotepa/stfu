using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveEvidenceArchiveIndex
{
    public InteractiveEvidenceArchiveIndex(IEnumerable<InteractiveEvidenceArchiveEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        Entries = entries.ToArray();
    }

    public IReadOnlyList<InteractiveEvidenceArchiveEntry> Entries { get; }

    public int Count => Entries.Count;

    public long TotalBytes => Entries.Sum(entry => entry.Length);
}
