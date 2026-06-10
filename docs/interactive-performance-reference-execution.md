# Interactive Performance reference execution policy

Interactive Performance currently keeps Reference Quality as the safe final output and export/parity baseline. Earlier IP batches always executed Reference Quality before the interactive artifact pipeline so the reference graph was available for harvesting.

This package introduces an explicit reference execution policy boundary. The default remains unchanged: Reference Quality executes before Interactive Performance. The new policy makes that prepass visible in diagnostics and creates a controlled opt-in path for later viewport-preview experiments.

## Environment switch

`STFU_INTERACTIVE_REFERENCE_EXECUTION` controls the policy. Supported values:

- `before`, `before-interactive`, `prepass`, `reference-prepass`, `always`
  - Default behavior. Reference Quality runs before Interactive Performance.
- `late`, `late-fallback`, `defer`, `deferred`, `on-demand`
  - Experimental behavior. It is accepted only when interactive preview output is explicitly enabled and reference fallback is not required as the final frame. If the interactive preview cannot be selected, Reference Quality is executed late to produce fallback output.

The late-fallback path is intentionally gated. It should not be used for export/parity or default UI behavior yet.

## Diagnostics

The pipeline writes these counters:

- `InteractivePerformance.referenceExecutionMode`
- `InteractivePerformance.referenceExecutedBeforeInteractive`
- `InteractivePerformance.referenceExecutedAfterInteractive`
- `InteractivePerformance.referenceExecutionSkipped`
- `InteractivePerformance.referenceFallbackFrameAvailable`

These counters separate three questions that were previously mixed together:

1. Did Reference Quality run as a graph-source prepass?
2. Did Reference Quality run only because final output needed fallback?
3. Did Interactive Performance return its own viewport frame?

This is groundwork for a future package that can move more viewport work to self-contained IP artifacts while keeping Reference Quality as the baseline/export path.
