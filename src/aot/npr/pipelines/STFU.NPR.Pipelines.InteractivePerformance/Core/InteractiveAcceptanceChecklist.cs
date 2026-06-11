using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveAcceptanceChecklist
{
    public InteractiveAcceptanceChecklist(IEnumerable<InteractiveAcceptanceChecklistItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Items = items.ToArray();
    }

    public IReadOnlyList<InteractiveAcceptanceChecklistItem> Items { get; }

    public int PassedCount => Items.Count(item => item.Passed);

    public int FailedCount => Items.Count(item => !item.Passed);

    public bool Passed => FailedCount == 0;
}
