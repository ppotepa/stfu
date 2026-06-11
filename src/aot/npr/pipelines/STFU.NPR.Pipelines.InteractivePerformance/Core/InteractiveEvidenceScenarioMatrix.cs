using System.Collections.Generic;
using System.Linq;

namespace STFU.NPR.Pipeline.InteractivePerformance.Core;

public sealed class InteractiveEvidenceScenarioMatrix
{
    public InteractiveEvidenceScenarioMatrix(IEnumerable<InteractiveEvidenceScenarioMatrixRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        Rows = rows.ToArray();
    }

    public IReadOnlyList<InteractiveEvidenceScenarioMatrixRow> Rows { get; }

    public int Count => Rows.Count;

    public IEnumerable<InteractiveEvidenceScenarioMatrixRow> ByQualityMode(string qualityMode)
    {
        return Rows.Where(row => string.Equals(row.QualityMode, qualityMode, StringComparison.OrdinalIgnoreCase));
    }
}
