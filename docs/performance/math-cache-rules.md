# Math / Projection / Render Cache Rules

## Cache allowed
- Mesh static topology by `MeshHandle` and mesh signature.
- Projected vertices by mesh, transform, camera, viewport and depth scale.
- Tile layouts by width, height and tile size.
- DX immutable-ish edge/index buffers by edge signature.
- Stroke instances only when path/stroke/frame signature matches.

## Cache forbidden initially
- Final visibility buffer across camera changes.
- Style-dependent stroke output without a style signature.
- Any cache that changes rendered output.
- Any cache keyed only by `MeshHandle` when mesh content can mutate.

## Required diagnostics
Every bounded cache must report:
- hits
- misses
- entries
- evictions
