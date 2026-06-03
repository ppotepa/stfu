# NPR Drawing Theory

This document describes the drawing theory STFU should encode as engine abstractions.

The goal is not to render triangle meshes as stylized wireframes. The goal is to convert a 3D model into drawing decisions: which parts of the form matter, which marks explain them, and how those marks should change across styles.

## Core Principle

An NPR renderer should treat the mesh as evidence, not as the final drawing.

```text
3D model
 -> geometric evidence
 -> view-dependent evidence
 -> drawing features
 -> stroke candidates
 -> style grammar
 -> final marks
```

A faithful NPR drawing needs three kinds of fidelity:

- Geometric fidelity: the drawing preserves the recognizable shape, silhouette, proportion, and important surface changes.
- Perceptual fidelity: the drawing communicates volume, depth, material, and lighting in a way a human can read.
- Style fidelity: the drawing follows a consistent mark language, such as sketch, ink, blueprint, manga, technical line art, or charcoal.

These three goals can conflict. A style may intentionally simplify, exaggerate, omit, or add marks. The engine should preserve that distinction: geometry analysis should describe the model; style should decide how to draw it.

## Drawing Concepts We Need

### 1. View-Dependent Feature Curves

The most important lines are usually not mesh edges. They are feature curves caused by the model, camera, and lighting.

Required concepts:

- Silhouette: where front-facing and back-facing regions meet from the current camera.
- Occluding contour: visible silhouette that actually defines the outer boundary or an internal overlap.
- Boundary: open mesh boundary edges.
- Crease: hard or high-angle surface transition.
- Suggestive contour: near-silhouette line that helps explain form before it becomes an actual silhouette.
- Apparent ridge: view-dependent ridge where curvature is visually important.
- Contact/accent line: line added to clarify overlap, grounding, or local emphasis.

Current code has boundary, silhouette-ish, and crease-ish extraction. Later versions should replace raw edge classification with a richer `FeatureCurve` abstraction.

### 2. Visibility And Hidden-Line Reasoning

Faithful line drawings depend heavily on visibility. A correct drawing does not show every back-side or occluded line.

Required concepts:

- Front/back facing state per triangle.
- Approximate depth ordering.
- Hidden-line suppression.
- Partial visibility along a curve, not only whole-line visible/hidden.
- Overlap accents for areas where one form passes in front of another.

Current CPU hidden-line filtering is approximate and line-level. A stronger version should split feature curves where they enter or leave occlusion.

### 3. Form Importance

Not every valid feature deserves a stroke. The renderer needs a salience model.

Signals:

- Screen length.
- Distance from camera.
- Normal change.
- Silhouette strength.
- Curvature estimate.
- Lighting contrast.
- Material boundary.
- Object or part importance.
- Local stroke density.
- Camera focus or selected entity.

This should produce an `Importance` or `Salience` value before styling. Style can use it, but geometry analysis should compute it.

### 4. Tone, Shade, And Hatching

Hatching should not be random surface decoration. It should describe light, shadow, form direction, material, and density.

Required concepts:

- Tone/value field: how dark a projected region should read.
- Light direction and shadow model.
- Terminator area: transition between lit and unlit form.
- Hatch direction: derived from surface flow, principal curvature, UV direction, or style.
- Hatch density: determined by shade, distance, and importance.
- Cross-hatching: second/third direction when tone is darker.
- Hatch clipping: hatches should respect silhouette, boundaries, and occlusion.

Current hatching is a basic sample-based mark. Later versions should introduce `ToneField`, `HatchingField`, and per-region hatch control.

### 5. Stroke Language

The stroke is the final mark language. It must be richer than a line segment.

Required stroke properties:

- Path points.
- Thickness.
- Opacity.
- Color or shade.
- Intent.
- Depth.
- Importance.
- Taper at endpoints.
- Pressure variation.
- Roughness/noise.
- Breaks/gaps.
- Overshoot.
- Dryness/grain later.

Current `StrokePath2D` supports path points, thickness, opacity, and color. The next natural extension is per-point stroke attributes, for example pressure or width along the path.

### 6. Style Grammar

A preset should be more than a settings object. A preset is a drawing grammar.

It should define:

- Which feature types are drawn.
- Which feature types are suppressed.
- Stroke hierarchy.
- Density rules.
- Tone strategy.
- Hatching strategy.
- Simplification rules.
- Humanization rules.
- Output target constraints, such as viewport, SVG, plotter, print, or bitmap.

Examples:

- Sketch: loose strokes, visible construction flow, moderate hatching, imperfect endpoints.
- Technical line art: clean silhouette, precise creases, minimal hatching, strong hidden-line removal.
- Manga: bold silhouette, selective internal lines, high-contrast hatching/screentone.
- Blueprint: uniform strokes, construction marks, flat color, low humanization.
- Charcoal: broad tonal masses, high grain, fewer geometric lines.

Current `INprPreset`, `NprPresetMetadata`, and `NprPresetRegistry` are the right base for this.

### 7. Composition And Level Of Detail

The same model should not produce the same number of marks at every zoom level.

Required concepts:

- Stroke budget.
- Density budget per screen area.
- LOD based on camera distance.
- Small feature suppression.
- Focus region.
- Style-specific simplification.
- Stable deterministic decisions to avoid flicker.

The current deterministic pruning is a start. Later versions should make stroke budgets explicit.

## Architecture Direction

The codebase should keep these layers separate:

```text
Mesh/Scene data
 -> Analysis graph
 -> Feature graph
 -> Stroke candidate graph
 -> Styled stroke graph
 -> StrokeFrame
 -> Viewport/SVG/export renderer
```

Recommended abstractions:

- `ProjectedTriangle`: projected geometry and lighting evidence.
- `TopologyEdge`: adjacency and normal-change evidence.
- `FeatureCurve`: drawing-relevant curve with intent, visibility, depth, salience.
- `ToneRegion`: projected region with value/shade.
- `SurfaceSample`: point evidence for flow/hatching.
- `StrokeCandidate`: unstyled mark proposal.
- `StyledStroke`: final mark before output conversion.
- `NprPreset`: pipeline plus style grammar and settings.

The current `NprGraph` can evolve to hold these concepts. If it grows too large, split it into smaller graph sections:

```text
NprGraph.Geometry
NprGraph.Features
NprGraph.Tone
NprGraph.Strokes
```

## What Should Not Be Mixed

Avoid these shortcuts:

- Do not let Avalonia rendering logic leak into NPR steps.
- Do not make mesh triangle edges equal final drawing lines.
- Do not put style rules inside geometry extraction.
- Do not use nondeterministic randomness for stroke decisions.
- Do not make plugin loading a requirement for AOT core.
- Do not make one preset control every future style.

## Current State In STFU

Already present:

- AOT-friendly NPR pipeline composition.
- Preset metadata and preset registry.
- Projected triangles.
- Mesh topology.
- Feature line extraction.
- Surface samples.
- Basic hatching.
- Approximate hidden-line filtering.
- Density pruning.
- Path-based strokes with color, opacity, and thickness.
- Deterministic humanization.

Still needed for high-quality NPR:

- Partial visibility and curve splitting.
- Better curvature and apparent-ridge extraction.
- Suggestive contours.
- Tone regions instead of only per-sample shade.
- Hatch clipping and cross-hatching.
- Per-point stroke pressure/taper.
- Material-aware styling.
- Multiple preset DLLs or statically linked preset modules.
- Export pipeline, especially SVG.
- Visual regression tests or snapshot metrics.

## Practical Next Milestones

1. Replace `FeatureLine` with `FeatureCurve`.
2. Add curve visibility splitting.
3. Add `StrokeCandidate` separate from final `NprStroke`.
4. Add per-point stroke pressure/taper.
5. Add tone regions and cross-hatching.
6. Add a second preset, such as `technical-line`.
7. Add SVG export using the same `StrokeFrame`.

The important rule is that every new feature should make the model-to-drawing interpretation clearer. NPR quality comes from better drawing decisions, not from more lines.
