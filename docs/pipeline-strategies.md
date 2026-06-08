# STFU NPR pipeline strategies

STFU exposes two frame pipeline strategies:

- **Reference Quality**: the current full/reference NPR pipeline. It remains the default and is used for parity, validation, export, and highest-quality rendering.
- **Interactive Performance**: a selectable placeholder for the upcoming optimized realtime pipeline. It currently delegates to Reference Quality until the optimized implementation is added.

This split is an architectural boundary only. It should not change rendering output for the default path.
