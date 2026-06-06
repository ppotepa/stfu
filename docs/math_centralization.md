# STFU Math centralization

`src/aot/STFU.Common/Math` is the source of truth for reusable math in the renderer, NPR pipeline, importers, scheduling, and diagnostics.

Domain projects may keep DTOs, domain records, and small glue code, but reusable calculations should live in the Math library. This includes geometry, projection, clip-space checks, raster/depth tests, stroke dashing, stroke humanization, color blending, pixel memory offsets, pixel diffs, index normalization, scans, worker budgets, noise, and deterministic hashing.

Allowed exceptions:

- native/vendor code,
- generated build artifacts,
- logs and release output,
- JavaScript maquettes/prototypes,
- tests that intentionally duplicate formulas for verification.

Validation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\check_math_centralization.ps1
dotnet build
```
