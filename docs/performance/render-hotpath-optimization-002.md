# Render Hot Path Optimization 002

This package tightens the first cache-backed render hot path after the cache contract patch.

## Changes

- Makes `FrameProjectionCache` safe for parallel `ProjectMeshStep` use.
- Routes parallel mesh projection through the projection cache instead of the uncached legacy projector.
- Moves `ProjectMeshStep` cache counters out of per-job execution so parallel workers do not write counters concurrently.
- Removes the duplicate local projection loop from `ProjectMeshStep`; `MeshProjectionService` is the projection source of truth.
- Adds visible face counters to `DefaultBuildFaceIdVisibilityBufferStep`.
- Avoids repeated tile range math inside visibility raster info construction.
- Reuses larger ink path/segment scratch arrays instead of reallocating exact-size arrays.
- Avoids repeated `DefaultStrokeStyle.ToString()` and pass count lookups per path in `DefaultBuildInkFrameStep`.
- Factors duplicated DirectX stroke upload/draw code into a single `RenderInstances` method.

## Non-goals

- No output appearance changes.
- No final visibility-buffer cache between frames.
- No preset/default policy changes.
- No shader changes.
