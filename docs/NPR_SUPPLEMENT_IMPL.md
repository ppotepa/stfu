# NPR_SUPPLEMENT.md — techniczny suplement implementacyjny dla STFU

**Status dokumentu:** blueprint inżynierski dla kolejnych faz STFU.  
**Zakres:** uzupełnienie dokumentów `docs/NPR_DRAWING_THEORY.md` i `docs/NPR_THEORY.MD`, bez przepisywania ich treści.  
**Repozytorium odniesienia:** STFU, lekki silnik .NET 10 / C# do konwersji meshów 3D na rysunki NPR 2D.  
**Główna teza:** STFU powinno ewoluować z prostego pipeline'u ekstrakcji `FeatureLine` do systemu opartego o `FeatureCurve`, segmenty widoczności, salience, pola tonu i kierunku kreskowania, formalną gramatykę stylu oraz bogaty model stroke'ów zdolny do eksportu wektorowego.

---

## Spis treści

1. [Executive Summary](#1-executive-summary)
2. [Gap Analysis Versus Current STFU Code](#2-gap-analysis-versus-current-stfu-code)
3. [Theory-To-Engine Abstraction Map](#3-theory-to-engine-abstraction-map)
4. [Feature Curves And Line Extraction](#4-feature-curves-and-line-extraction)
5. [Visibility And Hidden-Line Removal](#5-visibility-and-hidden-line-removal)
6. [Projection, Camera, And View Context](#6-projection-camera-and-view-context)
7. [Mesh Preprocessing And Geometry Data](#7-mesh-preprocessing-and-geometry-data)
8. [Feature Graph Architecture](#8-feature-graph-architecture)
9. [Salience And Line Selection](#9-salience-and-line-selection)
10. [Tone, Shading, Hatching, And Fields](#10-tone-shading-hatching-and-fields)
11. [Stroke Synthesis And Humanization](#11-stroke-synthesis-and-humanization)
12. [Style Grammar And Preset Architecture](#12-style-grammar-and-preset-architecture)
13. [Pipeline Architecture](#13-pipeline-architecture)
14. [CPU/GPU/Hybrid Rendering Path](#14-cpugpuhybrid-rendering-path)
15. [Temporal Coherence](#15-temporal-coherence)
16. [UI, Viewport, And Debug Tooling](#16-ui-viewport-and-debug-tooling)
17. [Export Architecture](#17-export-architecture)
18. [Tests And Evaluation](#18-tests-and-evaluation)
19. [Performance, Caching, And Scale](#19-performance-caching-and-scale)
20. [Roadmap](#20-roadmap)
21. [Bibliography](#21-bibliography)
22. [Implementation Contract](#22-implementation-contract)

---

## 1. Executive Summary

`NPR_THEORY.MD` powinien wyjaśniać *dlaczego* STFU traktuje mesh jako dowód rysunkowy, a nie jako gotowy zestaw linii. Ten suplement wyjaśnia *jak* z tej filozofii zbudować implementowalny silnik. Nie jest to dokument marketingowy o NPR. Jest to mapa decyzji architektonicznych, algorytmów, struktur danych, testów i debug tooling dla kolejnych etapów projektu.

Obecne STFU ma już poprawny szkielet: AOT-friendly moduły `src/aot`, `Scene`, `Entity`, `AssetRegistry`, loader OBJ, `CameraRig`, `NprPipeline`, rejestr presetów, podstawową projekcję, `ProjectedTriangle`, `TopologyEdge`, prostą ekstrakcję boundary/silhouette/crease, sample-based hatching, przybliżoną filtrację hidden-line, deterministic pruning, styling, humanization i `StrokeFrame`. Ten stan jest wartościowy, ponieważ wprowadza podział na analizę geometrii i końcowe stroke'y. Nie jest jednak jeszcze pełnym rendererem ilustracyjnym.

Docelowa architektura powinna rozdzielać następujące warstwy:

```text
Scene / Mesh / Asset
 -> MeshAnalysisCache
 -> NprViewContext
 -> ProjectedGeometry
 -> FeatureGraph
 -> VisibilityGraph
 -> SalienceGraph
 -> Tone / Direction / Density Fields
 -> StrokeCandidateGraph
 -> StyledStrokeGraph
 -> StrokeFrame
 -> Viewport / SVG / Raster / Plotter Export
```

W praktyce oznacza to cztery duże zmiany.

Po pierwsze, `FeatureLine` musi zostać zastąpiony przez `FeatureCurve`. Dzisiejsza linia jest odcinkiem ekranu z intencją, głębokością, cieniem i ważnością. To wystarcza dla prostego szkicu, ale nie wystarcza dla occluding contours, suggestive contours, apparent ridges, hatching aligned to curvature, partial visibility ani stabilności czasowej. `FeatureCurve` powinien przechowywać punkty w przestrzeni obiektu i/lub ekranu, źródłowe trójkąty/krawędzie, typ cechy, salience, zakres parametryczny, stabilny identyfikator i późniejsze segmenty widoczności.

Po drugie, visibility musi przejść z filtracji całych odcinków do modelu `VisibilitySegment`. Obecna filtracja typu endpoint/midpoint/end jest prototypem. Linie powinny być rozcinane tam, gdzie wchodzą za inne powierzchnie lub wychodzą zza nich. Dla viewportu można zacząć od depth-buffer/sample-based visibility. Dla eksportu SVG i rysunku technicznego trzeba dojść do BVH-backed ray visibility albo dokładniejszego object-space splittingu.

Po trzecie, hatching musi przestać być losową dekoracją powierzchni. Powinien wynikać z `ToneField`, `DirectionField`, `DensityField` i `MaterialRegion`. Ton mówi, jak ciemny ma być obszar. Kierunek mówi, jak kreska ma płynąć po formie. Gęstość mówi, ile stroke'ów ma powstać na jednostkę powierzchni ekranu lub obiektu. Materiał i styl mówią, czy jest to pen-and-ink, pencil, manga screentone, blueprint construction mark, charcoal mass czy watercolor wash.

Po czwarte, style preset nie powinien być tylko zbiorem parametrów. Powinien być `StyleGrammar`: formalną definicją, które cechy są rysowane, jak silna jest widoczność, jak działa ton, jaki jest stroke hierarchy, jakie są budżety zagęszczenia, jak zachowuje się medium, jak eksportować warstwy i jakie debug view są sensowne dla danego stylu.

W tym dokumencie każdy temat jest opisany przez: problem, teorię, wymagane dane, algorytm, stan w STFU, priorytet implementacji, ryzyka i debug/UI implications.

### 1.1. Klasyfikacja zaleceń

W dalszych rozdziałach używane są trzy etykiety:

- **Istnieje teraz** — funkcja lub struktura jest widoczna w aktualnym snapshotcie kodu.
- **Do dodania następnie** — da się zaimplementować bez głębokiego R&D; wymaga głównie refaktoryzacji i nowych typów.
- **Research-heavy** — wymaga estymacji krzywizny, dokładnej widoczności, temporally coherent matching, GPU backendu, jakościowego benchmarkingu albo modułów uczenia maszynowego.

### 1.2. Główna decyzja architektoniczna

Nie należy rozdzielać stylów przez osobne renderery. Należy stworzyć jeden rdzeń interpretacyjny i wiele gramatyk stylu:

```text
common geometry + visibility + fields + stroke model
            +
style grammar / preset
            =
konkretny rysunkowy output
```

Ta decyzja utrzymuje AOT-friendly rdzeń, pozwala na SVG/export, wspiera debugowanie, ułatwia testy deterministyczne i pozwala później dodać GPU viewport bez niszczenia pipeline'u CPU.

---

## 2. Gap Analysis Versus Current STFU Code

Poniższa tabela nie zakłada funkcji, których nie widać w snapshotcie. Traktuje obecny system jako sensowny prototyp i określa, co trzeba dodać, aby uzyskać poważny system NPR.

| Area | Current state | Problem | Required abstraction | Implementation priority |
|---|---|---|---|---|
| Feature lines | `FeatureLine` jako jeden odcinek 2D z `StableId`, `Intent`, `Start`, `End`, `Depth`, `Shade`, `Importance`; ekstrakcja boundary/silhouette/crease z `TopologyEdge`. | Odcinek 2D nie reprezentuje krzywej na powierzchni, parametryzacji, źródła geometrycznego, wielu segmentów widoczności ani smooth-surface features. | `FeatureCurve`, `FeaturePoint`, `FeatureCurveSource`, `FeatureCurveKind`, `CurveParameterRange`. | Immediate. |
| Visibility | `ApplyApproximateOcclusionStep` filtruje całe linie przez start/mid/end i rzutowane trójkąty. | Linie są albo zachowane, albo usunięte; brak partial visibility i brak rozcinania krzywych. Silhouette jest przepuszczana bez pełnego testu. | `VisibilitySegment`, `VisibilityState`, `OcclusionQuery`, `DepthBias`, później BVH. | Immediate dla segmentów; near-term dla BVH. |
| Hatching | `BuildHatchingStep` generuje krótkie odcinki z surface samples, thresholdem shade i losowym jitterem kąta. | Kierunek hatchingu nie wynika z curvature/UV/material; brak cross-hatchingu, clippingu regionów i tonal art maps. | `ToneField`, `DirectionField`, `DensityField`, `HatchingPlan`, `MaterialRegion`. | Immediate dla field API; near-term dla cross-hatching. |
| Surface-flow lines | `BuildSurfaceFlowLinesStep` łączy sample sąsiadujących trójkątów przy odpowiednim cieniu i gładkiej krawędzi. | Daje sugestię przepływu, ale nie jest principal curvature ani prawdziwym contour hatchingiem. | `DirectionFieldSample`, `SurfaceFlowCurve`, `CurvatureDirection`. | Near-term. |
| Stroke model | `StrokePath2D` ma `IReadOnlyList<Point2D>` i `StrokeStyle2D(Thickness, Opacity, Color)`. `NprStroke` przechowuje intent, depth, shade, importance. | Brak per-point width/pressure/taper, breaks, texture, source metadata i export metadata. | `StrokePoint2D`, rozszerzony `StrokePath2D`, `StrokeMetadata`, `StrokeMedium`, `StrokeIntent`. | Immediate dla typów; near-term dla rendererów. |
| Humanization | `HumanizeStrokesStep` dodaje overshoot, endpoint jitter, midpoint bend i variation thickness deterministycznym hashem. | Działa dla szkicu, ale nie modeluje pressure curve, dry media, ink pooling, paper grain ani bundles. | `IStrokeHumanizer`, `StrokeNoiseProfile`, `PressureProfile`, `MediumProfile`. | Near-term. |
| Style presets | `INprPreset`, `NprPresetMetadata`, `NprPresetRegistry`, `GenericSketchNprPreset`; ustawienia są klasą `NprSettings`. | Preset nie jest formalną gramatyką stylu; brak schematu wersji, kompatybilności, DLL plugin contract i UI schema. | `StyleGrammar`, `PresetSchema`, `PresetVersion`, `StyleFeatureRule`, `StyleToneRule`, `StyleStrokeRule`. | Immediate dla modelu; near-term dla pluginów. |
| Camera/projection | `CameraRig`, `CameraState`, `CameraProjector`; viewport wysyła orbit/pan/FOV przez komendy. | Projekcja kamery i interpretacja NPR są wymieszane pojęciowo w pipeline; brak `NprViewContext` z light/style/frame/history. | `NprViewContext` albo `SceneView`, `ProjectionInfo`, `LightContext`, `FrameContext`. | Immediate. |
| Mesh preprocessing | Topologia budowana per frame z `BuildMeshTopologyStep`; face normals liczone przy projekcji. | Nie ma trwałych cache per mesh; brak smoothing groups, curvature, half-edge, degeneracy cleanup, bounds. | `MeshAnalysisCache`, `TopologyCache`, `CurvatureCache`, `BoundsCache`. | Near-term. |
| Temporal coherence | Deterministyczne hashe od `Seed` i `StableId`; testy deterministyczności frame'u. | Stabilność losowa nie daje frame-to-frame matchingu, reprojection, stroke lifetime ani object-space hatch persistence. | `FrameHistory`, `FeatureCurveStableId`, `StrokeStableId`, `TemporalMatch`. | Research-heavy, ale zacząć od ID contract. |
| Viewport debug tooling | Avalonia rysuje grid i finalne `StrokeFrame`; przełącznik `1` mesh / `2` NPR; log preset ID. | Brak overlayów geometrycznych, visibility, salience, tone fields, hatch candidates i counters. | `NprDebugFrame`, `DebugOverlayKind`, `GraphCounters`, `PipelineTrace`. | Immediate. |
| SVG/export | Nie widać eksportera; `StrokeFrame` jest tylko strukturą do renderowania. | Brak zapisu warstw, metadanych, pressure/taper, style id i source feature id. | `IStrokeExporter`, `SvgStrokeExporter`, `ExportLayer`, `ExportMetadata`. | Near-term. |
| Tests/evaluation | Testy sprawdzają rich graph, determinism i preset registry. | Brak fixture'ów dla visibility, SVG, visual regression, stroke density i performance. | `NprSnapshotTest`, `VisualRegressionMetric`, `VisibilityFixture`, `ExportFixture`. | Immediate/near-term. |
| Performance/caching | Proste listy i słowniki; topologia per frame; brak BVH i tile budgets. | Koszt visibility i curvature będzie rósł nieliniowo; bez cache eksport i viewport będą wolne. | `MeshAnalysisCache`, `Bvh`, `SpatialGrid2D`, `StrokeBudget`, `TileDensityBudget`. | Near-term. |
| AOT/plugin boundary | Rdzeń modułowy i AOT-friendly; rejestr usług prosty. | Dynamiczne plugin DLL mogą kolidować z NativeAOT; trzeba rozróżnić statyczne presety rdzenia i runtime pluginy. | `IPresetProvider`, `StaticPresetBundle`, `RuntimePresetPlugin`, manifest JSON. | Near-term. |

### 2.1. Największa luka jakościowa

Największym ograniczeniem nie jest brak kolejnych filtrów wizualnych. Największym ograniczeniem jest to, że obecny system kończy analizę geometrii za wcześnie: krawędź topologiczna staje się finalnym kandydatem na stroke bez bogatego etapu `FeatureCurve -> VisibilitySegment -> Salience -> StrokeCandidate`. Dopóki ten etap nie powstanie, każdy styl będzie wariantem stylizowanej siatki, a nie prawdziwą interpretacją rysunkową.

### 2.2. Największa luka implementacyjna

Największą luką implementacyjną jest brak warstwy debug danych. Bez niej trudno rozwijać NPR, bo błędy są wizualne i często wynikają z poprzednich etapów pipeline'u. Każdy nowy pass powinien produkować nie tylko dane wyjściowe, ale też liczniki i overlay: ile krzywych wykryto, ile odrzucono, ile podzielono na segmenty widoczności, jaka jest średnia salience, gdzie powstał hatch, ile stroke'ów przekracza budżet.

---

## 3. Theory-To-Engine Abstraction Map

Ta sekcja mapuje pojęcia teorii rysunku i NPR na typy i moduły STFU. Zasada: pojęcia geometryczne i analityczne żyją w `STFU.NPR`; finalne stroke'y i eksportowalne ścieżki żyją w `STFU.Strokes`; kamera zostaje w `STFU.Camera`, ale widok NPR powinien być kompozycją w `STFU.NPR.Pipeline`.

| Pojęcie teorii | Znaczenie rysunkowe | Typ docelowy | Moduł | Stan |
|---|---|---|---|---|
| Mesh jako dowód | Model nie jest rysunkiem; jest źródłem geometrii. | `MeshData`, `MeshAnalysisCache` | `STFU.Mesh`, `STFU.NPR.Analysis` | Częściowo istnieje. |
| Projected geometry | Przejście z 3D do danych ekranowych. | `ProjectedVertex`, `ProjectedTriangle`, `ProjectedMesh` | `STFU.NPR.Graph` | Istnieje. |
| Topological adjacency | Sąsiedztwo trójkątów i kąt normalnych. | `TopologyEdge`, `TopologyCache` | `STFU.NPR.Graph` / `STFU.Mesh.Analysis` | Edge istnieje, cache do dodania. |
| Boundary line | Otwarta krawędź mesha. | `FeatureCurve { Kind = Boundary }` | `STFU.NPR.Features` | Obecnie `FeatureLine`. |
| Silhouette | Miejsce zmiany front/back względem widoku. | `FeatureCurve { Kind = Silhouette }` | `STFU.NPR.Features` | Obecnie edge-level. |
| Occluding contour | Widoczna część konturu zasłaniania. | `FeatureCurve` + `VisibilitySegment` | `STFU.NPR.Visibility` | Do dodania. |
| Crease | Twarde przejście powierzchni. | `FeatureCurve { Kind = Crease }` | `STFU.NPR.Features` | Obecnie edge-level. |
| Ridge/valley | Linie ekstremów krzywizny. | `FeatureCurve { Kind = Ridge/Valley }` | `STFU.NPR.Features` | Research-heavy. |
| Suggestive contour | Near-contour dla lepszego przekazu formy. | `FeatureCurve { Kind = SuggestiveContour }` | `STFU.NPR.Features` | Research-heavy. |
| Apparent ridge | View-dependent maxima apparent curvature. | `FeatureCurve { Kind = ApparentRidge }` | `STFU.NPR.Features` | Research-heavy. |
| Visible span | Fragment krzywej widoczny w kadrze. | `VisibilitySegment` | `STFU.NPR.Visibility` | Do dodania. |
| Hidden span | Fragment do ukrycia albo narysowania jako dashed. | `VisibilitySegment { State = Hidden }` | `STFU.NPR.Visibility` | Do dodania. |
| Importance/salience | Czy dana cecha zasługuje na kreskę. | `SalienceScore`, `LinePriorityRule` | `STFU.NPR.Salience` | Częściowo jako `Importance`. |
| Tone | Jaka wartość tonalna ma być czytana w regionie. | `ToneField`, `ToneSample` | `STFU.NPR.Tone` | Do dodania. |
| Hatch direction | Kierunek kreskowania jako pole. | `DirectionField` | `STFU.NPR.Tone` / `STFU.NPR.Hatching` | Do dodania. |
| Hatch density | Ile kresek wygenerować. | `DensityField`, `StrokeBudget` | `STFU.NPR.Hatching` | Do dodania. |
| Material drawing rule | Jak styl zależy od materiału/regionu. | `MaterialRegion`, `StyleMask` | `STFU.NPR.Materials` | Do dodania. |
| Stroke candidate | Niewystylizowana propozycja kreski. | `StrokeCandidate` | `STFU.NPR.Strokes` | Obecny krok tworzy od razu `NprStroke`; rozdzielić. |
| Styled stroke | Finalny stroke przed output. | `StyledStroke` lub rozszerzony `StrokePath2D` | `STFU.Strokes` | Częściowo istnieje. |
| Style grammar | Reguły wyboru, tonu, stroke'u, exportu. | `StyleGrammar` | `STFU.NPR.Composition` | Do dodania. |
| Frame history | Dane do stabilizacji animacji. | `FrameHistory`, `PreviousFrameGraph` | `STFU.NPR.Temporal` | Do dodania. |
| Vector output | Finalny eksport rysunku. | `StrokeFrame`, `IStrokeExporter`, `SvgStrokeExporter` | `STFU.Strokes.Export` | `StrokeFrame` istnieje, exporter do dodania. |
| Debug overlay | Widoczność etapów pipeline. | `NprDebugFrame`, `DebugOverlayKind` | `STFU.NPR.Debug`, `STFU.UI` | Do dodania. |

### 3.1. Gdzie powinny żyć nowe typy

Rekomendowany podział modułów:

```text
src/aot/STFU.NPR/Graph
    NprGraph.cs
    FeatureCurve.cs
    FeaturePoint.cs
    VisibilitySegment.cs
    StrokeCandidate.cs
    ToneSample.cs
    SalienceScore.cs

src/aot/STFU.NPR/Analysis
    MeshAnalysisCache.cs
    TopologyCache.cs
    CurvatureCache.cs
    GeometryAnalyzer.cs

src/aot/STFU.NPR/Visibility
    IVisibilityResolver.cs
    SampleVisibilityResolver.cs
    BvhVisibilityResolver.cs
    VisibilityOptions.cs

src/aot/STFU.NPR/Fields
    ToneField.cs
    DirectionField.cs
    DensityField.cs
    TextureField.cs
    MaterialRegion.cs

src/aot/STFU.NPR/Styles
    StyleGrammar.cs
    StyleFeatureRule.cs
    StyleToneRule.cs
    StyleStrokeRule.cs
    StrokeBudgetRule.cs

src/aot/STFU.Strokes
    StrokePoint2D.cs
    StrokePath2D.cs
    StrokeStyle2D.cs
    StrokeMetadata.cs
    StrokeFrame.cs

src/aot/STFU.Strokes/Export
    IStrokeExporter.cs
    SvgStrokeExporter.cs
    RasterStrokeExporter.cs
```

`STFU.Camera` nie powinno znać NPR. `STFU.NPR` może przyjąć `CameraState` i zbudować `NprViewContext`. `STFU.UI` nie powinno robić analizy NPR; powinno jedynie wyświetlać `StrokeFrame` i `NprDebugFrame`.

---

## 4. Feature Curves And Line Extraction

### 4.1. Problem

Dobry rysunek nie jest wireframe'em. Wireframe pokazuje topologię siatki. Rysunek pokazuje formę, zależność brył, światło, materiał i decyzję artystyczną. Najważniejsze linie często nie są krawędziami trójkątów. Są krzywymi wynikającymi z relacji powierzchni, kamery i percepcji.

Obecny STFU klasyfikuje `TopologyEdge` i emituje `FeatureLine` dla boundary, silhouette i crease. To jest właściwy pierwszy etap, ale z definicji działa tylko dla krawędzi topologicznych. Na gładkim modelu o dobrej triangulacji wiele istotnych linii może przecinać wnętrza trójkątów, a nie pokrywać się z ich krawędziami.

### 4.2. Wspólny model `FeatureCurve`

Zanim opiszemy typy linii, potrzebny jest wspólny typ krzywej:

```csharp
namespace STFU.NPR.Graph;

public enum FeatureCurveKind
{
    Boundary,
    Silhouette,
    OccludingContour,
    Crease,
    Ridge,
    Valley,
    SuggestiveContour,
    ApparentRidge,
    ContactAccent,
    MaterialBoundary,
    Construction,
    HatchGuide
}

public readonly record struct FeaturePoint(
    System.Numerics.Vector3 WorldPosition,
    System.Numerics.Vector3 WorldNormal,
    STFU.Strokes.Point2D ScreenPosition,
    float Depth,
    float CurveParameter,
    int SourceTriangleIndex,
    int SourceEdgeIndex,
    float Curvature,
    float Confidence);

public sealed record FeatureCurve(
    int StableId,
    FeatureCurveKind Kind,
    IReadOnlyList<FeaturePoint> Points,
    FeatureCurveSource Source,
    float BaseSalience,
    float MeanDepth,
    float MeanShade,
    FeatureCurveFlags Flags);

public readonly record struct FeatureCurveSource(
    int MeshId,
    int EntityId,
    int PrimaryTopologyEdge,
    int PrimaryTriangle,
    int SecondaryTriangle);

[Flags]
public enum FeatureCurveFlags
{
    None = 0,
    ViewDependent = 1,
    RequiresCurvature = 2,
    CanBeHidden = 4,
    CanBeStylizedAsDashed = 8,
    GeneratedByStyle = 16
}
```

Najważniejsza zmiana: `FeatureCurve` nie jest stroke'em. To geometryczno-percepcyjny kandydat. Dopiero później visibility, salience i style grammar zdecydują, czy i jak go narysować.

### 4.3. Boundary lines

**Definicja.** Boundary line to krawędź mesha z tylko jednym sąsiadującym trójkątem. W rysunku może oznaczać otwartą powierzchnię, przecięcie modelu, granicę skanu albo artefakt pliku.

**Dane wejściowe.** Topologia krawędzi, lista trójkątów, rzutowanie wierzchołków, visibility.

**Algorytm.** Podczas budowy adjacency, jeżeli krawędź ma `SecondTriangleIndex < 0`, emituj `FeatureCurveKind.Boundary`.

```text
for each topologyEdge:
    if topologyEdge.IsBoundary:
        curve = FeatureCurve.FromEdge(edge, Kind=Boundary)
        curves.Add(curve)
```

**Trudność.** Niska. Obecny `BuildMeshTopologyStep` już wykrywa boundary.

**Failure cases.** Boundary może być artefaktem dziurawego OBJ. Dla rysunku technicznego może być istotna, ale dla szkicu organicznego można ją tłumić, jeżeli wynika z braku danych.

**Czy STFU wspiera teraz?** Tak, jako `NprStrokeIntent.Boundary` emitowany z `ExtractFeatureLinesStep`.

**Nowe struktury.** `FeatureCurve` zamiast `FeatureLine`; opcjonalnie `BoundaryPolicy` w `StyleGrammar`.

### 4.4. Silhouettes

**Definicja.** Na siatce trójkątów silhouette edge to krawędź, której sąsiednie ściany mają różny znak front-facing względem kamery. Na gładkiej powierzchni contour generator to zbiór punktów, gdzie normalna jest prostopadła do wektora widzenia: `n(p) · v(p) = 0`.

**Dane wejściowe.** Normalne ścian, pozycja kamery, topologia, projekcja.

**Algorytm meshowy.**

```text
for each nonBoundaryEdge e with adjacent faces f0, f1:
    s0 = dot(normal(f0), viewDirection(center(f0))) > 0
    s1 = dot(normal(f1), viewDirection(center(f1))) > 0
    if s0 != s1:
        emit FeatureCurveKind.Silhouette along e
```

**Trudność.** Niska dla mesha; średnia dla smooth surfaces.

**Failure cases.** Dla rzadkiego mesha silhouette jest kanciasta. Dla modeli z błędnym windingiem lub normalnymi może znikać albo migotać. Dla orthographic/perspective view definicja `v(p)` różni się: w perspektywie `v` zależy od punktu.

**Czy STFU wspiera teraz?** Tak, edge-level: `first.IsFrontFacing != second.IsFrontFacing`.

**Nowe struktury.** `FeatureCurve` z flagą `ViewDependent`; później smooth contour extraction przez interpolację `n·v` po trójkącie.

### 4.5. Occluding contours

**Definicja.** Occluding contour to widoczna część silhouette/contour generator, która faktycznie tworzy granicę zasłaniania w obrazie. Każdy occluding contour jest związany z widocznością; sama silhouette może być ukryta przez inny obiekt.

**Dane wejściowe.** Feature curves, depth buffer lub scene BVH, camera ray tests.

**Algorytm minimalny.** Najpierw wykryj silhouette, potem rozbij na segmenty i sprawdź visibility próbkami.

```text
for curve in silhouetteCurves:
    samples = sampleCurve(curve, stepPixels=2..8)
    states = [visibility(sample) for sample in samples]
    segments = splitWhereStateChanges(curve, states)
    emit VisibilitySegment for each segment
```

**Trudność.** Średnia dla próbkowania; wysoka dla dokładnego object-space splittingu.

**Failure cases.** Thin occluders, self-intersection, bias depth, z-fighting, T-junctions, brak rozdzielenia front/back dla bliskich powierzchni.

**Czy STFU wspiera teraz?** Częściowo. Ma hidden-line filtering, ale nie ma segmentów widoczności.

**Nowe struktury.** `VisibilitySegment`, `OcclusionQuery`, `DepthBias`, `VisibilityResolver`.

### 4.6. Crease lines

**Definicja.** Crease to krawędź, gdzie dihedral angle między normalnymi sąsiednich ścian przekracza próg stylu lub próg materiału. W modelach CAD crease jest często cechą semantyczną; w organice może być artefaktem niskiej teselacji.

**Dane wejściowe.** Topology edge, normalne ścian, threshold stylu, smoothing groups/material groups.

**Algorytm.**

```text
angle = acos(clamp(dot(n0, n1), -1, 1))
if angleDegrees >= settings.CreaseAngleDegrees and both faces visible/front-facing policy:
    emit FeatureCurveKind.Crease
```

**Trudność.** Niska.

**Failure cases.** Jeżeli OBJ nie ma smoothing groups lub normalnych, wszystkie ostre przejścia mogą wyglądać tak samo. Na gęstym mesh'u organicznym małe kąty mogą generować clutter.

**Czy STFU wspiera teraz?** Tak, przez `NormalAngleDegrees >= CreaseAngleDegrees`.

**Nowe struktury.** `CreasePolicy`, material-aware thresholds, per-preset crease priority.

### 4.7. Ridges and valleys

**Definicja.** Ridges i valleys to linie ekstremów krzywizny głównej na powierzchni. Ridge często biegnie wzdłuż lokalnego „grzbietu” formy; valley wzdłuż lokalnego wgłębienia.

**Dane wejściowe.** Estymacja krzywizny na mesh'u: principal curvatures `k1`, `k2`, principal directions `d1`, `d2`, pochodne krzywizny.

**Algorytm szkicowy.**

```text
for each vertex/face sample p:
    estimate k1, k2, d1, d2
    if derivative(k1 along d1) crosses zero and secondDerivative condition indicates max:
        mark ridge candidate
    if derivative(k2 along d2) crosses zero and min/max condition indicates valley:
        mark valley candidate
trace connected candidate samples into curves
```

**Trudność.** Wysoka. Wymaga `CurvatureCache`, smoothingu i dobrej jakości mesha.

**Failure cases.** Szum, under-tessellation, nierówne trójkąty, ostre crease'y traktowane jak curvature, brak stabilności temporalnej.

**Czy STFU wspiera teraz?** Nie.

**Nowe struktury.** `CurvatureCache`, `CurvatureSample`, `FeatureCurveKind.Ridge/Valley`.

### 4.8. Suggestive contours

**Definicja.** Suggestive contour to linia, która zachowywałaby się jak contour z pobliskiego punktu widzenia i pomaga komunikować formę przed tym, zanim powierzchnia stanie się faktyczną silhouette. W ujęciu DeCarlo et al. jest to near-contour związany z radial curvature i warunkami pochodnych.

**Dane wejściowe.** Krzywizna radialna względem widoku, principal curvatures/directions, pochodne krzywizny, visibility, styl.

**Algorytm przybliżony dla STFU.** Zacząć od wersji heurystycznej zanim powstanie pełna matematyka:

```text
for each surface sample p on front-facing visible triangle:
    ndotv = dot(normal(p), viewDirection(p))
    if ndotv is small but positive and local normal variation is high:
        if shade/curvature/salience above threshold:
            seed suggestive contour sample
connect samples across adjacent triangles using direction field
```

Pełniejsza wersja powinna używać radial curvature zero-crossing i derivative test.

**Trudność.** Research-heavy.

**Failure cases.** Bardzo wrażliwe na noisy curvature; łatwo uzyskać brzydkie, niestabilne linie. Wymaga smoothingu i progów confidence.

**Czy STFU wspiera teraz?** Nie.

**Nowe struktury.** `CurvatureCache`, `SuggestiveContourOptions`, `FeatureCurveConfidence`.

### 4.9. Apparent ridges

**Definicja.** Apparent ridges to view-dependent maxima apparent curvature, czyli linii maksymalnej zmienności normalnej w płaszczyźnie widoku. Mają obejmować lub wzmacniać wiele wizualnie istotnych linii, których klasyczne ridges/suggestive contours nie łapią.

**Dane wejściowe.** Normal variation, view-dependent curvature, curvature derivatives, projection.

**Algorytm szkicowy.**

```text
for each surface sample p:
    compute view-dependent curvature tensor Q_view
    find max eigenvalue q1 and direction t1 in screen plane
    if derivative(q1 along t1) crosses zero and q1 is local maximum:
        emit apparent ridge sample
trace samples into view-dependent curves
```

**Trudność.** Research-heavy.

**Failure cases.** Bardzo zależne od jakości normalnych i krzywizny; może generować zbyt dużo linii na noisy meshach.

**Czy STFU wspiera teraz?** Nie.

**Nowe struktury.** `CurvatureCache`, `ApparentCurvatureSample`, `FeatureCurveKind.ApparentRidge`.

### 4.10. Material boundaries

**Definicja.** Granice materiałów, UV islands albo semantycznych części modelu mogą być ważniejsze niż geometria. Rysownik często zaznacza szew, krawędź materiału, panel, oko, detal mechaniczny.

**Dane wejściowe.** Materiały, face groups, object parts, UV, metadane assetu.

**Algorytm.**

```text
for each topologyEdge e:
    if material(faceA) != material(faceB) or part(faceA) != part(faceB):
        emit FeatureCurveKind.MaterialBoundary
```

**Trudność.** Niska, jeżeli loader obsługuje materiały; średnia, jeżeli trzeba rozszerzyć OBJ/MTL/import.

**Czy STFU wspiera teraz?** Nie w aktualnym loaderze OBJ widocznym w snapshotcie; loader czyta pozycje i faces.

**Nowe struktury.** `MeshMaterialId`, `FaceGroup`, `MaterialRegion`, importer MTL albo neutralny format materiału.

### 4.11. Dlaczego raw triangle edges nie wystarczą

Raw triangle edges są artefaktem reprezentacji, a nie intencją rysunku. Ich użycie prowadzi do trzech problemów:

1. **Zależność od teselacji.** Ten sam kształt może mieć różne edge loops w zależności od exportu modelu. Rysunek nie powinien zmieniać się dramatycznie przez triangulację.
2. **Brak linii wewnątrz trójkątów.** Smooth contour generator i suggestive contours często przecinają wnętrza trójkątów.
3. **Brak semantyki stylu.** Nie każda krawędź topologiczna jest rysunkowo ważna; niektóre linie powinny być dodane mimo braku krawędzi.

Dlatego `TopologyEdge` jest dowodem, a nie finalną kreską. Powinien być jednym z wejść do `FeatureCurveExtractor`.

---

## 5. Visibility And Hidden-Line Removal

### 5.1. Problem

Rysunek bez poprawnej widoczności staje się nieczytelny. W stylu szkicowym czasem można zostawić construction lines albo faint hidden lines, ale w technical line art, blueprint i SVG eksport widoczność jest krytyczna. Obecny `ApplyApproximateOcclusionStep` jest dobry jako prototyp, bo usuwa część zasłoniętych linii. Nie wystarcza jednak do partial visibility: długi contour może być w połowie widoczny, w połowie ukryty.

### 5.2. Model docelowy

```csharp
public enum VisibilityState
{
    Unknown,
    Visible,
    Hidden,
    ClippedByNearPlane,
    OutsideViewport,
    Degenerate
}

public readonly record struct VisibilitySegment(
    int StableId,
    int FeatureCurveId,
    float T0,
    float T1,
    VisibilityState State,
    float MeanDepth,
    float Confidence,
    int OccluderEntityId,
    int OccluderTriangleId);

public readonly record struct DepthBias(
    float WorldBias,
    float ScreenDepthBias,
    float NormalBias);

public interface IOcclusionQuery
{
    VisibilityState Query(in FeaturePoint point, in NprViewContext view, out OcclusionHit hit);
}

public readonly record struct OcclusionHit(
    int EntityId,
    int TriangleId,
    float HitDepth,
    float DeltaDepth,
    STFU.Strokes.Point2D ScreenPosition);
```

`FeatureCurve` pozostaje geometrią. `VisibilitySegment` mówi, które zakresy parametryczne krzywej są widoczne. Stroke generator pracuje na widocznych segmentach, a style grammar może zdecydować, czy hidden segments rysować jako dashed, faint, blueprint ghost, czy całkowicie ukrywać.

### 5.3. Z-buffer / depth-buffer visibility

**Teoria.** Rasteryzujemy scenę do depth buffer, a następnie próbkujemy głębokość w miejscach krzywej. Jeżeli głębokość krzywej jest większa niż depth buffer plus bias, punkt jest ukryty.

**Zalety.** Szybkie, pasuje do viewportu i GPU, łatwe do debugowania przez overlay depth.

**Wady.** Rozdzielczość ekranu, aliasing, bias, brak dokładnego split point, problemy z cienkimi occluderami.

**STFU teraz.** Nie ma jawnego depth bufferu NPR. Obecny CPU test działa podobnie logicznie, ale testuje rzutowane trójkąty bez bufora.

**Stage recommendation.** Dla CPU można zbudować prosty software depth buffer per frame albo spatial grid 2D. Dla runtime UI można później wykorzystywać GPU depth/normal pass.

### 5.4. Object-space ray tests

**Teoria.** Dla próbki krzywej rzucamy promień z kamery do punktu na krzywej. Jeżeli najbliższe przecięcie sceny jest bliżej niż punkt krzywej, punkt jest ukryty.

**Zalety.** Dokładniejsze niż screen-space; niezależne od rozdzielczości; dobre dla eksportu.

**Wady.** Bez BVH koszt `O(samples * triangles)` jest zbyt wysoki. Wymaga robust ray/triangle intersection i biasu.

**Algorytm.**

```text
for point in sampleCurve(curve):
    ray = cameraRayThrough(point.WorldPosition)
    hit = bvh.Intersect(ray, tMax = point.Depth - bias)
    visible = hit == none or hit.triangle == source triangle near point
```

**STFU teraz.** Brak BVH i ray tests.

**Stage recommendation.** Najpierw `SampleVisibilityResolver` bez BVH do testów małych scen; potem `BvhVisibilityResolver`.

### 5.5. Curve/triangle intersection and exact splitting

**Teoria.** Dokładne hidden-line removal wyznacza przecięcia rzutowanych krzywych z rzutowanymi trójkątami, dzieli krzywe na zakresy parametryczne i porównuje głębokości w przedziałach.

**Zalety.** Najlepsze do offline SVG i technical line art.

**Wady.** Trudne numerycznie; potrzeba robust predicates, obsługi degeneracji, tangential intersections, self-overlap.

**STFU teraz.** Nie wspiera.

**Stage recommendation.** Nie implementować jako pierwszy krok. Zacząć od segment subdivision i BVH-backed visibility.

### 5.6. Etapy ewolucji widoczności w STFU

#### Etap A — sample-based visibility

- Input: `FeatureCurve`, `ProjectedTriangle`, settings.
- Output: coarse `VisibilitySegment`.
- Implementacja: sampling co N pikseli lub co M segmentów krzywej.
- Debug: punkty zielone/czerwone na krzywej.

```text
for curve in curves:
    samples = sampleByScreenLength(curve, spacing=4px)
    states = queryVisibility(samples)
    segments = compressStatesToSegments(states)
```

#### Etap B — adaptive segment subdivision

- Jeżeli próbki są różne, subdivide do progu długości.
- Pozwala znaleźć approximate transition point.

```text
ResolveSegment(curve, t0, t1):
    s0 = Query(t0)
    s1 = Query(t1)
    if s0 == s1:
        emit segment(t0,t1,s0)
    else if screenLength(t0,t1) < minPixels:
        emit uncertain split around midpoint
    else:
        tm = midpoint(t0,t1)
        ResolveSegment(t0,tm)
        ResolveSegment(tm,t1)
```

#### Etap C — BVH-backed ray visibility

- Per mesh cache: BVH trójkątów w world/object space.
- Per frame: transform bounds, camera rays.
- Output: confidence i occluder id.

#### Etap D — near-exact offline export visibility

- Dla SVG/export włącz tryb wolniejszy.
- Dzieli krzywe przez projected occluder intersections.
- Daje clean technical output.

### 5.7. Hidden-line rendering jako styl

Nie każdy hidden segment musi znikać. Gramatyka stylu powinna mieć reguły:

```csharp
public enum HiddenLinePolicy
{
    Suppress,
    DrawDashed,
    DrawFaint,
    DrawBlueprintGhost,
    DrawOnlyIfSelected,
    DrawOnlyIfTechnical
}
```

Technical line art: zwykle `Suppress` albo `DrawDashed` dla hidden edges. Blueprint: `DrawBlueprintGhost`. Sketch: `Suppress` dla tylnych linii, ale construction curves mogą zostać jako jasne stroke'y.

---

## 6. Projection, Camera, And View Context

### 6.1. Camera projection vs NPR projection

Camera projection odpowiada na pytanie: gdzie punkt 3D leży na ekranie i jaka jest jego głębokość? NPR projection odpowiada na pytanie: co ten rzut znaczy dla rysunku?

Obecne STFU ma `CameraRig`, `CameraState` i `CameraProjector`. `CameraProjector.TryProject` przelicza world position na `Point2D` i depth. To jest warstwa projekcji kamery. Następnie `BuildProjectedTrianglesStep` oblicza normal, center, screen area, shade, front-facing i visible. To już jest początek NPR projection, ponieważ trójkąty stają się dowodem do decyzji o stroke'ach.

Docelowo warto wprowadzić `NprViewContext`, który spina wszystkie view-dependent parametry.

### 6.2. `NprViewContext`

```csharp
public sealed record NprViewContext(
    STFU.Camera.CameraState Camera,
    int Width,
    int Height,
    ProjectionInfo Projection,
    LightContext Lighting,
    string ActivePresetId,
    int FrameId,
    float TimeSeconds,
    FrameHistory? PreviousFrame,
    NprViewFlags Flags);

public readonly record struct ProjectionInfo(
    System.Numerics.Matrix4x4 ViewMatrix,
    System.Numerics.Matrix4x4 ProjectionMatrix,
    System.Numerics.Matrix4x4 ViewProjectionMatrix,
    float NearPlane,
    float FarPlane,
    float Aspect,
    float FieldOfViewDegrees,
    ProjectionKind Kind);

public enum ProjectionKind
{
    Perspective,
    Orthographic
}

public readonly record struct LightContext(
    System.Numerics.Vector3 KeyLightDirection,
    float Ambient,
    float Contrast,
    ToneMappingMode ToneMappingMode);
```

### 6.3. Jak viewport karmi pipeline

Obecny `EngineViewportControl` robi trzy rzeczy: tworzy frame zależnie od render mode, obsługuje orbit/pan/FOV i przełącza `Mesh`/`Npr`. Docelowo powinien dalej działać tak samo, ale zamiast budować tylko `NprContext` z `Scene`, `Assets`, `Camera`, `Width`, `Height`, `Settings`, powinien przekazać `NprViewContext`.

```csharp
private StrokeFrame CreateNprFrame(int width, int height)
{
    var view = NprViewContextFactory.Create(
        camera: _camera.Camera,
        width: width,
        height: height,
        presetId: _nprPresetRegistry.ActivePreset.Metadata.Id,
        frameId: _frameCounter++,
        previousFrame: _frameHistory);

    var context = new NprContext
    {
        Scene = _engine.Scene,
        Assets = _assets,
        View = view,
        Settings = _nprSettings,
        DebugOptions = _debugOptions
    };

    return _nprPipeline.Execute(context);
}
```

### 6.4. Debug implication

Projection debug powinien pokazywać:

- projected vertices,
- projected triangle centers,
- front-facing/back-facing coloring,
- depth false color,
- screen area heatmap,
- near-plane clipped points,
- camera ray sample for selected curve.

Bez tego trudno diagnozować, czy błąd feature extraction wynika z geometrii, projection, normal orientation czy visibility.

---

## 7. Mesh Preprocessing And Geometry Data

### 7.1. Problem

Obecnie topologia i normalne są budowane w pipeline per frame. Przy małym modelu Suzanne to wystarczy. Przy większej scenie, wielu presetach, SVG export i krzywiznach będzie to kosztowne. NPR potrzebuje danych zależnych od mesha i danych zależnych od widoku. Te pierwsze powinny być cache'owane.

### 7.2. Co cache'ować per mesh

Per mesh, niezależnie od kamery:

- face normals,
- vertex normals, jeżeli loader ich nie daje albo trzeba je przeliczyć,
- edge adjacency,
- boundary edges,
- face areas,
- vertex-to-face adjacency,
- bounding box/sphere,
- degenerate triangle list,
- material/part ids,
- smoothing groups,
- optional half-edge structure,
- curvature estimates,
- principal curvature directions,
- BVH w local space.

Per view:

- projected vertices,
- projected triangles,
- front/back facing,
- shade/tone samples,
- feature curves zależne od widoku,
- visibility segments,
- salience zależne od screen size,
- stroke budgets.

### 7.3. `MeshAnalysisCache`

```csharp
public sealed class MeshAnalysisCache
{
    public required int MeshStableId { get; init; }
    public required TopologyCache Topology { get; init; }
    public required GeometryCache Geometry { get; init; }
    public CurvatureCache? Curvature { get; init; }
    public MeshBvh? Bvh { get; init; }
    public MeshCleanupReport CleanupReport { get; init; } = MeshCleanupReport.Empty;
}

public sealed class GeometryCache
{
    public required IReadOnlyList<System.Numerics.Vector3> Positions { get; init; }
    public required IReadOnlyList<System.Numerics.Vector3> FaceNormals { get; init; }
    public required IReadOnlyList<System.Numerics.Vector3> VertexNormals { get; init; }
    public required IReadOnlyList<float> FaceAreas { get; init; }
    public required BoundingBox3D Bounds { get; init; }
    public required float ScaleHint { get; init; }
}
```

### 7.4. `TopologyCache`

```csharp
public sealed class TopologyCache
{
    public required IReadOnlyList<CachedTopologyEdge> Edges { get; init; }
    public required IReadOnlyList<int[]> VertexToTriangles { get; init; }
    public required IReadOnlyList<int[]> TriangleToEdges { get; init; }
    public required IReadOnlyList<int> BoundaryEdgeIndices { get; init; }
    public required bool IsManifold { get; init; }
}

public readonly record struct CachedTopologyEdge(
    int StableId,
    int A,
    int B,
    int FirstTriangle,
    int SecondTriangle,
    bool IsBoundary,
    float NormalAngleDegrees,
    EdgeSemantic Semantic);

public enum EdgeSemantic
{
    Unknown,
    Boundary,
    Smooth,
    HardCrease,
    MaterialBoundary,
    NonManifold
}
```

### 7.5. `CurvatureCache`

```csharp
public sealed class CurvatureCache
{
    public required IReadOnlyList<CurvatureSample> VertexSamples { get; init; }
    public required IReadOnlyList<CurvatureSample> FaceSamples { get; init; }
    public required float MeanEdgeLength { get; init; }
    public required float SmoothingRadius { get; init; }
    public required CurvatureQuality Quality { get; init; }
}

public readonly record struct CurvatureSample(
    System.Numerics.Vector3 Position,
    System.Numerics.Vector3 Normal,
    float K1,
    float K2,
    System.Numerics.Vector3 Direction1,
    System.Numerics.Vector3 Direction2,
    float Confidence);

public enum CurvatureQuality
{
    NotComputed,
    LowConfidence,
    GoodForHatching,
    GoodForSuggestiveContours
}
```

### 7.6. Half-edge vs current edge dictionary

Obecny `BuildMeshTopologyStep` używa dictionary z kluczem `(min,max)`. To jest proste i dobre dla pierwszego etapu. Half-edge lub winged-edge staje się potrzebny, gdy chcemy:

- chodzić po powierzchni wzdłuż krzywych,
- śledzić contour generator przez trójkąty,
- estymować krzywizny i sąsiedztwa lokalne,
- obsłużyć non-manifold cases,
- łatwo przechodzić face->edge->opposite face.

Rekomendacja: nie przepisywać od razu całego `MeshData`. Dodać `TopologyCache` z API half-edge-like zbudowanym z istniejącego `MeshData`.

### 7.7. Mesh cleanup

OBJ z realnego świata może mieć:

- trójkąty o zerowym polu,
- duplicate vertices,
- inconsistent winding,
- non-manifold edges,
- isolated components,
- złe normalne,
- brak skali.

NPR jest bardzo wrażliwy na takie błędy. Pipeline powinien mieć `MeshCleanupReport`, ale nie powinien automatycznie niszczyć danych bez zgody użytkownika. UI powinno pokazać ostrzeżenia w asset browser.

---

## 8. Feature Graph Architecture

### 8.1. Obecny graph

Obecny `NprGraph` jest mutable i zawiera listy projected data, topology, feature lines, surface samples i strokes. To jest szybkie i AOT-friendly. Główna wada: jeden graf miesza dane różnych faz i ma tylko `FeatureLine`, nie `FeatureCurve`, `VisibilitySegment`, `StrokeCandidate` ani pola tonu.

### 8.2. Docelowy graph

```csharp
public sealed class NprGraph
{
    public NprGeometryGraph Geometry { get; } = new();
    public NprFeatureGraph Features { get; } = new();
    public NprVisibilityGraph Visibility { get; } = new();
    public NprToneGraph Tone { get; } = new();
    public NprStrokeGraph Strokes { get; } = new();
    public NprDebugCounters Debug { get; } = new();

    public void Clear()
    {
        Geometry.Clear();
        Features.Clear();
        Visibility.Clear();
        Tone.Clear();
        Strokes.Clear();
        Debug.Clear();
    }
}

public sealed class NprGeometryGraph
{
    public List<ProjectedMesh> Meshes { get; } = [];
    public List<ProjectedVertex> Vertices { get; } = [];
    public List<ProjectedTriangle> Triangles { get; } = [];
    public List<TopologyEdge> TopologyEdges { get; } = [];
}

public sealed class NprFeatureGraph
{
    public List<FeatureCurve> Curves { get; } = [];
    public List<FeaturePoint> CurvePointsScratch { get; } = [];
    public Dictionary<int, int> CurveIndexByStableId { get; } = new();
}

public sealed class NprVisibilityGraph
{
    public List<VisibilitySegment> Segments { get; } = [];
    public Dictionary<int, SegmentRange> SegmentsByCurveId { get; } = new();
}

public sealed class NprToneGraph
{
    public List<SurfaceSample> SurfaceSamples { get; } = [];
    public List<ToneSample> ToneSamples { get; } = [];
    public ToneField? ToneField { get; set; }
    public DirectionField? DirectionField { get; set; }
    public DensityField? DensityField { get; set; }
}

public sealed class NprStrokeGraph
{
    public List<StrokeCandidate> Candidates { get; } = [];
    public List<StyledStroke> Styled { get; } = [];
    public List<StrokeCluster> Clusters { get; } = [];
}
```

### 8.3. Mutability vs immutability

Dla AOT-friendly .NET i małego engine'u warto trzymać mutable `List<T>` w graphie per frame. To zmniejsza alokacje i upraszcza pipeline. Publiczne typy danych mogą być `record struct` albo immutable `record`, ale kontenery graphu powinny być mutable.

Rekomendacja:

- Drobne wartości (`FeaturePoint`, `VisibilitySegment`, `ToneSample`) jako `readonly record struct`.
- Cięższe obiekty (`FeatureCurve`, `ToneField`, `StrokeCluster`) jako `sealed record` lub `sealed class`.
- Graph jako mutable `sealed class` z `Clear()`.
- Export `StrokeFrame` jako immutable snapshot.

### 8.4. Debug counters

```csharp
public sealed class NprDebugCounters
{
    public int ProjectedVertexCount;
    public int ProjectedTriangleCount;
    public int TopologyEdgeCount;
    public int FeatureCurveCount;
    public int VisibilitySegmentCount;
    public int VisibleSegmentCount;
    public int HiddenSegmentCount;
    public int StrokeCandidateCount;
    public int StyledStrokeCount;
    public int FinalStrokeCount;
    public int CulledByVisibility;
    public int CulledBySalience;
    public int CulledByBudget;
    public double ProjectionMilliseconds;
    public double FeatureMilliseconds;
    public double VisibilityMilliseconds;
    public double StrokeMilliseconds;
}
```

UI powinno móc pokazać te liczniki bez parsowania logów.

---

## 9. Salience And Line Selection

### 9.1. Problem

Nie każda poprawna linia powinna zostać narysowana. Rysownik wybiera. STFU powinno odróżniać „cecha istnieje” od „cecha jest ważna dla tego stylu”. Obecne `Importance` jest dobrym początkiem, ale pruning nadal jest głównie losowo-deterministyczny przez density.

### 9.2. Sygnały salience

Salience powinno składać się z kilku komponentów:

| Sygnał | Znaczenie | Dane |
|---|---|---|
| Feature kind | Silhouette zwykle ważniejsza niż hatch. | `FeatureCurveKind` |
| Screen length | Dłuższe linie zwykle czytelniejsze. | projected points |
| Depth | Dalekie linie mogą być cieńsze lub pomijane. | mean depth |
| Curvature strength | Linie dużej krzywizny są istotne dla formy. | curvature cache |
| Normal contrast | Duża zmiana normalnych wskazuje formę. | topology/curvature |
| Tone contrast | Linia w obszarze kontrastu światła jest ważniejsza. | tone field |
| Occlusion role | Overlap contours pomagają czytać głębię. | visibility segments |
| Material/semantic role | Oczy, krawędzie paneli, szwy mogą być ważne. | material/part id |
| Local clutter | W zatłoczonym obszarze część linii trzeba usunąć. | spatial grid/tile budget |
| Focus region | Wybrany obiekt lub obszar powinien mieć więcej detalu. | UI/camera focus |
| Style priority | Manga, technical, pencil mają różne hierarchie. | `StyleGrammar` |

### 9.3. `SalienceScore`

```csharp
public readonly record struct SalienceScore(
    float Geometry,
    float Visibility,
    float Tone,
    float Material,
    float Style,
    float Focus,
    float ClutterPenalty,
    float Final)
{
    public static SalienceScore Clamp(SalienceScore s) =>
        s with { Final = Math.Clamp(s.Final, 0f, 1f) };
}

public sealed record LinePriorityRule(
    FeatureCurveKind Kind,
    float BaseWeight,
    float MinScreenLength,
    float MaxDensityPerTile,
    HiddenLinePolicy HiddenPolicy,
    bool AlwaysKeepIfOuterSilhouette);
```

### 9.4. Algorytm priority-aware pruning

Zamiast losowo usuwać linie według density, należy wyliczyć score, posortować i stosować budżety per tile.

```text
for each segment:
    score = ComputeSalience(segment, curve, tone, style, focus)
    if score.Final < style.MinSalience: reject
    bucket = spatialTile(segment.screenBounds)
    add to bucket candidates

for each tile:
    sort candidates by score desc
    keep until tile.StrokeBudget reached
    for candidates near duplicate curves: keep stronger, suppress weaker

for each kept segment:
    create StrokeCandidate
```

### 9.5. Determinism

Losowość nadal jest potrzebna, ale tylko jako tie-breaker lub style variation. Powinna być deterministyczna względem `Seed`, `StableId`, `FrameId` i `StyleId`. Nie powinna decydować o ważności geometrycznej, tylko o wariancie kreski.

---

## 10. Tone, Shading, Hatching, And Fields

### 10.1. Problem

Obecny hatching jest markiem na surface sample. Jest prosty, działa jako tekstura szkicu, ale nie niesie pełnej informacji o tonie, formie ani materiale. W pen-and-ink hatch powinien komunikować światło, kierunek powierzchni i gęstość tonu. W mandze może przejść w screentone. W charcoal powinien zamieniać się w masy tonalne. W watercolor może oznaczać wash boundary lub pigment pooling.

### 10.2. Rozróżnienie pojęć

- **Shade**: wynik modelu oświetlenia, np. `1 - dot(n, light)`; obecny STFU ma `Shade` per triangle/sample.
- **Tone/value**: docelowa czytelna wartość w rysunku; styl może ją skwantyzować, przesunąć lub uprościć.
- **Density**: ile znaków na powierzchni ma oddać tone.
- **Direction**: w którą stronę stroke'y mają płynąć.
- **Texture**: mikrostruktura medium: papier, graphite grain, ink, charcoal, screentone.
- **Salience**: czy ten obszar wymaga szczegółu.

### 10.3. Field-based architecture

```csharp
public interface IField2D<T>
{
    T Sample(STFU.Strokes.Point2D position);
}

public sealed class ToneField : IField2D<float>
{
    public int Width { get; init; }
    public int Height { get; init; }
    public float[] Values { get; init; } = [];
    public float Sample(STFU.Strokes.Point2D position) => BilinearSample(Values, position);
}

public sealed class DirectionField : IField2D<System.Numerics.Vector2>
{
    public int Width { get; init; }
    public int Height { get; init; }
    public System.Numerics.Vector2[] Directions { get; init; } = [];
    public DirectionFieldSource Source { get; init; }
}

public sealed class DensityField : IField2D<float>
{
    public int Width { get; init; }
    public int Height { get; init; }
    public float[] Density { get; init; } = [];
}

public sealed class TextureField : IField2D<TextureSample>
{
    public int Width { get; init; }
    public int Height { get; init; }
    public TextureSample[] Samples { get; init; } = [];
}

public readonly record struct TextureSample(
    float Grain,
    float PaperAbsorption,
    float Dryness,
    float Noise);
```

### 10.4. `MaterialRegion` i `StyleMask`

```csharp
public sealed record MaterialRegion(
    int StableId,
    int EntityId,
    int MaterialId,
    IReadOnlyList<int> TriangleIndices,
    float BaseTone,
    StrokeMedium PreferredMedium,
    RegionHatchingPolicy HatchingPolicy);

public sealed record StyleMask(
    int StableId,
    string Name,
    IReadOnlyList<ScreenPolygon> ScreenRegions,
    float Strength,
    StyleMaskRole Role);
```

`MaterialRegion` jest object-space. `StyleMask` może być screen-space albo user-defined. Użycie: rysować metal jako clean technical hatch, skórę jako miękki pencil shade, tło jako wash.

### 10.5. `HatchingPlan`

```csharp
public sealed record HatchingPlan(
    int StableId,
    int RegionId,
    HatchLayer Primary,
    HatchLayer? Secondary,
    HatchLayer? Tertiary,
    HatchClippingPolicy Clipping,
    float ToneTarget,
    float DensityTarget);

public sealed record HatchLayer(
    float ToneThreshold,
    float SpacingPixels,
    float StrokeLengthPixels,
    float DirectionAngleOffsetRadians,
    float Opacity,
    float Thickness,
    HatchLayerKind Kind);

public enum HatchLayerKind
{
    Primary,
    Cross,
    Tertiary,
    Contour,
    Screentone,
    Stipple
}
```

### 10.6. Staged implementation

#### Stage 0 — current sample-based hatch

Stan obecny: sample, shade threshold, density random roll, fixed angle with jitter. Zachować jako `SimpleHatchingPass` i przenieść do nowej architektury jako fallback.

#### Stage 1 — tone-aware hatch

- Zbudować `ToneField` z `ProjectedTriangle.Shade`.
- Generować hatch spacing z tonu: ciemniej = gęściej.
- Zachować deterministic seed.

```text
for sample in toneSamples:
    tone = ToneField.Sample(sample.position)
    density = style.HatchDensityCurve(tone)
    if Hash01(sample.id, seed) < density:
        emit hatch stroke
```

#### Stage 2 — second/third hatch directions

- Dla tone > threshold2 dodaj cross hatch.
- Dla tone > threshold3 dodaj trzeci kierunek albo fill mass.

```text
if tone > style.CrossHatchThreshold:
    emit layer angle + style.CrossAngle
if tone > style.DeepShadowThreshold:
    emit tertiary or charcoal mass
```

#### Stage 3 — curvature/UV aligned hatch

- `DirectionField` z principal curvature, UV tangent albo style vector.
- Hatch stroke id stabilny względem regionu i grid position.

#### Stage 4 — tonal art map backend

- Dla GPU/hybrid i temporal coherence.
- TAM jako texture atlas per style.
- Używać w viewport compositing; SVG może użyć vector hatch generator.

#### Stage 5 — region clipping

- Hatch strokes clip do visible regions, silhouettes, material regions i occluders.
- Wektorowo: polygon clipping; rastrowo: stencil/depth mask.

### 10.7. Failure cases

- Hatch przekracza silhouette i wygląda jak artefakt.
- Kierunek hatchingu nie płynie po formie, więc obiekt wygląda płasko.
- Zbyt gęsty hatch niszczy contour.
- Losowy hatch migocze przy ruchu kamery.
- Cross-hatching bez budżetu tworzy moiré.
- Stipple/screentone wymaga kontroli skali eksportu.

---

## 11. Stroke Synthesis And Humanization

### 11.1. Problem

Obecny stroke model jest celowo prosty. `StrokePath2D` przechowuje punkty i jeden styl z thickness/opacity/color. To wystarcza do Avalonia `DrawLine`, ale nie do wiernego medium. Ręczna kreska ma zmienną szerokość, pressure, taper, przerwy, teksturę, overshoot, ink pooling i interaction z papierem.

### 11.2. Docelowe typy w `STFU.Strokes`

```csharp
namespace STFU.Strokes;

public readonly record struct StrokePoint2D(
    float X,
    float Y,
    float Width,
    float Pressure,
    float Opacity,
    float Dryness,
    float Grain,
    float Time);

public sealed record StrokePath2D(
    IReadOnlyList<StrokePoint2D> Points,
    StrokeStyle2D Style,
    StrokeMetadata Metadata);

public readonly record struct StrokeStyle2D(
    float BaseThickness,
    float Opacity,
    StrokeColor Color,
    StrokeCap Cap,
    StrokeJoin Join,
    StrokeMedium Medium,
    StrokeTextureMode TextureMode);

public enum StrokeMedium
{
    Generic,
    Ink,
    Pencil,
    Charcoal,
    Brush,
    Marker,
    Watercolor,
    BlueprintLine,
    PlotterPen
}

public sealed record StrokeMetadata(
    int StableId,
    int SourceFeatureId,
    int SourceSegmentId,
    string StyleId,
    StrokeIntent Intent,
    VisibilityState Visibility,
    float Salience,
    float Depth,
    string LayerName);
```

### 11.3. Stroke qualities

| Jakość | Znaczenie | Implementacja |
|---|---|---|
| Width | Fizyczna szerokość kreski. | Per-point `Width`, style profile. |
| Taper | Zwężanie końców. | Multiply width by endpoint envelope. |
| Pressure | Wpływa na width/opacity/grain. | `PressureProfile` po parametrze t. |
| Opacity | Przezroczystość. | Per-point lub style-level. |
| Jitter | Ręczna niedokładność. | Deterministic noise w normal/tangent direction. |
| Overshoot | Przeciągnięcie poza koniec formy. | Dodać extension wzdłuż tangent. |
| Undershoot | Niedociągnięcie. | Skrócenie zakresu stroke. |
| Waviness | Falowanie. | Low-frequency noise po krzywej. |
| Breaks | Przerwy w kresce. | Split stroke path albo opacity zero spans. |
| Dry brush | Nieregularne przerwanie pigmentu. | Texture mask + per-point dryness. |
| Graphite grain | Ziarnistość ołówka. | Paper texture + pressure. |
| Ink pooling | Ciemniejsze końce/zakręty. | Opacity boost przy niskiej prędkości/końcach. |
| Stroke bundle | Kilka podobnych pociągnięć. | `StrokeCluster` generuje variants. |

### 11.4. Deterministyczna humanizacja

Obecne STFU robi deterministyczną humanizację przez hash seed/stableId. Zachować tę zasadę. Różnica: generować nie tylko jeden mid-point, ale cały profil.

```text
seed = Hash(globalSeed, strokeStableId, styleId)
for each point i on path:
    t = i / (count-1)
    tangent = estimateTangent(path, i)
    normal = perpendicular(tangent)
    jitterLow = Noise1D(seed, t, frequency=2)
    jitterHigh = Noise1D(seed, t, frequency=13)
    width = baseWidth * PressureEnvelope(t) * StyleWidthNoise(seed,t)
    opacity = baseOpacity * DrynessMask(seed,t,paper)
    point.xy += normal * (jitterLow * ampLow + jitterHigh * ampHigh)
```

### 11.5. Impact na Avalonia viewport

Avalonia `DrawingContext.DrawLine` nie obsługuje naturalnie per-point width. Są trzy opcje:

1. **Fallback fast path:** jeśli wszystkie punkty mają podobny width, rysować segmenty `DrawLine` ze średnim width.
2. **Polyline mesh path:** generować polygon outline stroke'u i wypełniać go `PathGeometry`.
3. **Raster brush path:** dla texture/grain renderować do bitmapy lub użyć custom shader później.

Immediate implementation: zachować stary renderer jako fallback i dodać debug flag `UseVariableWidthStrokePreview`.

### 11.6. Impact na SVG export

SVG wspiera `path`, `stroke-width`, `stroke-opacity`, `stroke-linecap`, `stroke-linejoin`, ale nie wspiera natywnie per-point width w jednym prostym stroke. Dla variable width są opcje:

- eksportować jako wiele krótkich segmentów z różnym `stroke-width`,
- eksportować jako filled outline path,
- eksportować dwa warianty: editable simple stroke i faithful expanded path,
- zapisać metadata w `data-*` attributes.

Rekomendacja: `SvgExportMode.Editable` i `SvgExportMode.Faithful`.

---

## 12. Style Grammar And Preset Architecture

### 12.1. Problem

Różne style nie powinny wymagać osobnych rendererów. Powinny różnić się gramatyką: które cechy są ważne, jak strict jest visibility, jak działa ton, jak rysować stroke, jaki jest budżet i jaki export.

### 12.2. `StyleGrammar`

```csharp
public sealed record StyleGrammar(
    string StyleId,
    string DisplayName,
    Version SchemaVersion,
    IReadOnlyList<StyleFeatureRule> FeatureRules,
    StyleVisibilityRule Visibility,
    StyleToneRule Tone,
    StyleHatchingRule Hatching,
    StyleStrokeRule Stroke,
    StyleBudgetRule Budget,
    StyleExportRule Export,
    StyleDebugRule Debug);

public sealed record StyleFeatureRule(
    FeatureCurveKind Kind,
    bool Enabled,
    float BaseWeight,
    float MinSalience,
    HiddenLinePolicy HiddenLinePolicy,
    StrokeIntent Intent,
    int LayerOrder);

public sealed record StyleVisibilityRule(
    VisibilityStrictness Strictness,
    float DepthBias,
    bool SplitCurves,
    bool KeepHiddenSegmentsForDebug,
    HiddenLinePolicy DefaultHiddenPolicy);

public enum VisibilityStrictness
{
    LooseSketch,
    Sampled,
    SegmentSplit,
    BvhRaycast,
    OfflineExact
}
```

### 12.3. Matrix stylów

| Styl | Feature types | Visibility strictness | Tone strategy | Hatch strategy | Stroke style | Texture needs | Export requirements | UI controls |
|---|---|---|---|---|---|---|---|---|
| Technical line art | Boundary, occluding contour, crease, material boundary; optional hidden dashed. | High: segment split/BVH/offline exact. | Minimal tone; optional Gooch-style mid-tone. | Sparse or none. | Clean, uniform, no jitter, precise caps. | Paper optional; no heavy grain. | SVG layers: contours, creases, hidden, dimensions. | Crease angle, hidden-line mode, line weights, export scale. |
| Pen-and-ink | Occluding contour, crease, suggestive contour, ridge/valley, hatching guides. | Medium-high; hidden usually suppressed. | Tone field drives density. | Primary/cross/tertiary hatch; curvature-aligned. | Taper, pressure, slight ink irregularity. | Paper grain, ink pooling optional. | SVG faithful path or segment strokes; hatch layers. | Hatch density, cross angle, ink roughness, line hierarchy. |
| Pencil sketch | Silhouette, suggestive contours, apparent ridges, loose construction. | Medium; hidden construction may be faint. | Soft tone, bundles, graphite accumulation. | Loose directional hatch, smudged tone. | High jitter, broken strokes, variable pressure. | Graphite grain, paper tooth. | Raster preview + SVG simplified; preserve stroke bundles. | Roughness, construction lines, pressure variation, graphite grain. |
| Manga/comic | Bold silhouette, selective internal contours, material boundaries. | Medium; clean occlusion. | Flat bands, screentone, high contrast. | Screentone or controlled hatch in shadow. | Bold outer line, thin inner lines, clean curves. | Screentone dots/lines, flat fills. | SVG/PDF layered: ink, tone, fills, balloons later. | Outer line width, screentone scale, shadow threshold. |
| Blueprint | Boundary, crease, hidden construction, axes/grid. | Medium-high but hidden may be visible as ghost. | Flat blue background, pale construction. | Minimal; construction hatches optional. | Uniform cyan/white lines, low humanization. | Grid/paper background. | SVG with layers; plotter-like output. | Grid size, hidden ghost opacity, construction density. |
| Charcoal | Few silhouettes, large tonal masses, contact accents. | Low-medium; exact hidden less important. | Tone regions dominate. | Hatch replaced by broad masses and smudge. | Soft, wide, grainy, broken. | Strong paper texture, charcoal dust. | Raster preferred; SVG approximate. | Grain, smudge, tonal contrast, silhouette suppression. |
| Watercolor/wash | Sparse ink contour, wash region boundaries. | Medium; contours should be clean. | Wash fields, edge darkening, pigment flow. | Usually none or very light pencil underdrawing. | Thin ink/pencil with paper bleed. | Paper absorption, granulation, wet edges. | Raster preferred; vector outline optional. | Wash strength, granulation, edge bloom, underdrawing opacity. |
| Expressive sketch | Silhouette, suggestive contours, apparent ridges, construction strokes. | Loose; can keep some hidden/construction. | Moderate tone; focus-dependent. | Loose hatch and surface-flow. | Overshoot, waviness, bundles, breaks. | Paper grain optional. | SVG editable + raster preview. | Roughness, overshoot, stroke budget, focus region. |

### 12.4. Preset plugin architecture

Obecne `INprPreset` jest dobrym początkiem. Docelowo:

```csharp
public interface INprPreset
{
    NprPresetMetadata Metadata { get; }
    StyleGrammar CreateGrammar();
    NprSettings CreateSettings();
    INprPipeline CreatePipeline();
}

public sealed record NprPresetMetadata(
    string Id,
    string Name,
    string Description,
    bool IsEditable,
    Version Version,
    Version MinimumEngineVersion,
    string Author,
    string[] Tags,
    PresetPackaging Packaging);

public enum PresetPackaging
{
    BuiltInAot,
    StaticallyLinkedModule,
    RuntimePluginDll,
    JsonEditablePreset
}
```

### 12.5. AOT constraints

NativeAOT nie lubi dynamic discovery przez reflection bez jawnej konfiguracji. Dlatego:

- Built-in presets powinny być statycznie rejestrowane.
- Plugin DLL mogą działać w runtime desktop, ale nie powinny być wymagane przez AOT core.
- JSON preset może opisywać settings i grammar, ale nie dowolny kod.
- `NprPresetRegistry` powinien mieć `Register(INprPreset)` i `Register(IPresetProvider)`.

### 12.6. Versioning

Każdy preset powinien deklarować schema version. Engine powinien umieć:

- odrzucić niekompatybilny preset,
- migrować starszy JSON,
- pokazać ostrzeżenia w UI,
- zapisać preset ID i version w `StrokeFrame` metadata.

---

## 13. Pipeline Architecture

### 13.1. Step czy Pass?

W obecnym kodzie używane jest `INprStep`. Dla CPU pipeline zostawić nazwę `Step`, bo pasuje do sekwencyjnego przetwarzania graphu. Dla GPU i render-loop używać nazwy `Pass`. Dokumentacyjnie:

- **Step** — transformacja danych w `NprGraph` w CPU/AOT core.
- **Pass** — etap renderingu GPU lub compositingu w runtime.

### 13.2. Docelowe etapy

| Stage | Input | Output | Module | Debug view | Cache behavior | Tests |
|---|---|---|---|---|---|---|
| Geometry ingestion | `Scene`, `AssetRegistry` | active meshes/entities | `STFU.Engine`, `STFU.Assets` | scene/entity list | per scene | load/assign tests |
| Mesh analysis | `MeshData` | `MeshAnalysisCache` | `STFU.NPR.Analysis` | topology/bounds overlay | per mesh | topology fixtures |
| Projection | cache + `NprViewContext` | projected vertices/triangles | `STFU.NPR.Steps.Mesh` | projected vertices, depth | per view | projection determinism |
| Feature extraction | projected geometry + topology/curvature | `FeatureCurve` | `STFU.NPR.Features` | curves by kind | per view, some per mesh | feature count fixtures |
| Visibility | curves + projected geometry/BVH | `VisibilitySegment` | `STFU.NPR.Visibility` | visible/hidden overlay | per view | occlusion fixtures |
| Salience | segments + tone/style | `SalienceScore` | `STFU.NPR.Salience` | heatmap/pruned overlay | per view/style | priority tests |
| Tone fields | projected triangles + lighting | `ToneField`, `DirectionField` | `STFU.NPR.Fields` | tone/direction overlay | per view/style | field sampling tests |
| Stroke candidate generation | visible segments + fields | `StrokeCandidate` | `STFU.NPR.Strokes` | candidates overlay | per frame | candidate count tests |
| Stroke styling | candidates + grammar | `StyledStroke` | `STFU.NPR.Styles` | style layers | per preset | style mapping tests |
| Humanization | styled strokes + seed/history | `StrokePath2D` | `STFU.NPR.Strokes` / `STFU.Strokes` | before/after overlay | per frame/history | determinism tests |
| Frame build | stroke paths | `StrokeFrame` | `STFU.Strokes` | final frame | immutable snapshot | frame tests |
| Export/render | `StrokeFrame`, debug frame | SVG/raster/viewport | `STFU.Strokes.Export`, `STFU.UI` | export preview | per export | SVG snapshot tests |

### 13.3. Target pipeline sketch

```csharp
public static INprPipeline CreateTechnicalLinePipeline()
{
    return new NprPipeline<
        EnsureMeshAnalysisCacheStep,
        ProjectMeshStep,
        BuildProjectedTrianglesStep,
        ExtractFeatureCurvesStep,
        ResolveCurveVisibilityStep,
        ScoreFeatureSalienceStep,
        PruneByStyleBudgetStep,
        BuildStrokeCandidatesStep,
        StyleStrokeCandidatesStep,
        BuildStrokePathsStep,
        BuildStrokeFrameStep>();
}
```

Dla szkicu ekspresyjnego pipeline może dodać `BuildToneFieldStep`, `BuildHatchingPlanStep`, `GenerateHatchCandidatesStep`, `HumanizeStrokesStep`.

### 13.4. Pipeline trace

Każdy step powinien opcjonalnie raportować:

```csharp
public readonly record struct NprStepTrace(
    string StepName,
    double Milliseconds,
    int InputCount,
    int OutputCount,
    int RejectedCount,
    string Notes);
```

UI `pipeline-graph` może rysować nodes z czasem i liczbą elementów.

---

## 14. CPU/GPU/Hybrid Rendering Path

### 14.1. CPU object-space NPR

CPU object-space jest najlepszy dla:

- dokładnej kontroli feature curves,
- SVG/export,
- hidden-line rendering,
- debugowania,
- testowalności,
- deterministyczności.

Obecne STFU jest naturalnie CPU-first. To dobra decyzja. Nie należy porzucać CPU core na rzecz shaderów, bo shader outline da szybką kreskę, ale nie da semantic feature graph i eksportu.

### 14.2. Screen-space depth/normal outlines

Screen-space outline używa depth, normal, material ID i color edge detection. Jest szybki, dobry dla toon/comic viewport. Nie wie jednak, czy linia jest crease, occluding contour, material boundary czy artefakt depth. Dlatego screen-space outline powinien być optional viewport acceleration, nie źródło prawdy dla SVG.

### 14.3. G-buffer stylization

G-buffer daje dostęp do normal, depth, material attributes. Nadaje się do:

- post-process outlines,
- cel bands,
- hatching textures w screen-space,
- paper compositing,
- stylized lighting.

W Unity URP mechanizmem integracji są Scriptable Render Passes/Renderer Features. W HDRP istnieją Custom Passes i injection points. W Unreal Post Process Materials mogą być wpięte w post-processing graph, korzystać z `SceneTexture`, GBuffer i CustomDepth/Stencil. Te rozwiązania są runtime/UI concern, nie AOT core.

### 14.4. Rekomendowana ścieżka STFU

#### Phase 1 — CPU correctness and SVG

- `FeatureCurve`, `VisibilitySegment`, `StrokePoint2D`.
- Sample/segment visibility.
- SVG exporter.
- Debug overlays.

#### Phase 2 — faster CPU

- `MeshAnalysisCache`.
- BVH visibility.
- spatial grid/tile pruning.
- parallel steps.

#### Phase 3 — optional GPU viewport compositing

- Runtime module, nie `src/aot` core.
- Depth/normal buffers if Avalonia backend allows, or separate render host later.
- Paper texture, grain, post-process overlays.

#### Phase 4 — hybrid real-time renderer

- CPU feature graph for important semantic lines.
- GPU screen-space fields for tone/paper/wash.
- Shared `StrokeFrame` for export.

### 14.5. AOT boundary

AOT-friendly core:

- Geometry, feature extraction, visibility data models, stroke frame, SVG export.

Runtime/UI only:

- Avalonia renderer,
- GPU buffers,
- shader/material system,
- dynamic plugin loading,
- file dialogs/export preview.

---

## 15. Temporal Coherence

### 15.1. Problem

Interaktywny NPR migocze, jeśli stroke'y są generowane niezależnie każdą klatkę. Deterministic seed pomaga, ale nie rozwiązuje wszystkich problemów. Jeżeli feature curve zmienia się topologicznie albo sample grid przesuwa się po ekranie, stroke'y mogą przeskakiwać.

### 15.2. Co obecny seed daje

Obecne hashe od `Seed` i `StableId` dają:

- powtarzalność przy tym samym widoku,
- deterministyczny pruning,
- deterministyczną humanizację,
- testowalność snapshotów.

Nie dają:

- dopasowania stroke'ów między różnymi widokami,
- lifetime stroke'u,
- reprojection previous frame,
- stabilnych hatch strokes na przesuwającej się powierzchni,
- object-space hatching coherence.

### 15.3. `FrameHistory`

```csharp
public sealed class FrameHistory
{
    public int PreviousFrameId { get; init; }
    public NprViewContext PreviousView { get; init; }
    public IReadOnlyDictionary<int, PreviousFeatureCurve> CurvesByStableId { get; init; } =
        new Dictionary<int, PreviousFeatureCurve>();
    public IReadOnlyDictionary<int, PreviousStroke> StrokesByStableId { get; init; } =
        new Dictionary<int, PreviousStroke>();
}

public sealed record PreviousFeatureCurve(
    int StableId,
    FeatureCurveKind Kind,
    IReadOnlyList<FeaturePoint> Points,
    IReadOnlyList<VisibilitySegment> Segments,
    SalienceScore Salience);

public sealed record PreviousStroke(
    int StableId,
    int SourceFeatureId,
    StrokePath2D Path,
    float Lifetime,
    float LastSeenTime,
    TemporalStrokeState State);

public enum TemporalStrokeState
{
    Alive,
    FadingIn,
    FadingOut,
    Replaced,
    Dead
}
```

### 15.4. Stable IDs

`FeatureCurveStableId` powinien wynikać z:

- mesh stable id,
- entity id,
- source topology edge lub source triangle region,
- feature kind,
- quantized parameter interval,
- style-independent seed.

Nie powinien wynikać bezpośrednio z indexu w liście, jeżeli kolejność może się zmienić.

### 15.5. Frame-to-frame matching

```text
for curve in currentCurves:
    previous = previousFrame.CurvesByStableId.TryGet(curve.StableId)
    if previous exists:
        match = DirectStableIdMatch
    else:
        match = nearestCurveBySourceAndScreenOverlap(curve, previousFrame)

for strokeCandidate in currentCandidates:
    previousStroke = match by source feature and role
    if previousStroke exists:
        blend geometry and opacity from previous to current
    else:
        mark as FadingIn
```

### 15.6. Shower-door effect

Image-space patterns stabilne względem ekranu mogą wyglądać jak tekstura przyklejona do szyby. Object-space strokes stabilne względem obiektu lepiej ujawniają formę, ale zmieniają skalę na ekranie. Dlatego:

- semantic lines: object-space stable,
- paper/grain: screen-space stable,
- hatching: zależnie od stylu; pen-and-ink często object/parameter-space, manga screentone może być screen/print-space.

---

## 16. UI, Viewport, And Debug Tooling

### 16.1. Problem

NPR nie da się rozwijać bez narzędzi debug. Finalny obraz nie mówi, czy błąd pochodzi z mesha, projection, feature extraction, visibility, salience, hatching czy stylizacji. Avalonia viewport powinien stać się nie tylko preview, ale też inspector pipeline'u.

### 16.2. UI panels

UI powinno mieć:

- asset browser,
- scene/entity list,
- camera controls,
- preset browser,
- pipeline graph,
- NPR settings panel,
- stroke intent filters,
- graph counters,
- overlay toggles,
- visibility debug,
- hatching debug,
- export preview.

### 16.3. Powiązanie z maquettes

W snapshotcie kodu nie widać folderów `maquettes/*`, ale projektowo należy spiąć dokument z następującymi ekranami:

| Maquette | Rola techniczna |
|---|---|
| `maquettes/workspace` | Główny układ: viewport, panels, status bar, selected entity. |
| `maquettes/pipeline-graph` | Graficzny podgląd etapów `INprStep`/passes, czasów i liczników. |
| `maquettes/preset-browser` | Wybór `StyleGrammar`, edycja `NprSettings`, wersje presetów. |
| `maquettes/asset-browser` | Mesh list, cleanup warnings, cache status, materials. |
| `maquettes/debug-graph` | Raw graph inspector: curves, segments, samples, strokes. |
| `maquettes/export` | SVG/raster/PDF/plotter export, warstwy, scale, metadata. |

### 16.4. Debug overlays

```csharp
public enum DebugOverlayKind
{
    None,
    ProjectedVertices,
    ProjectedTriangles,
    FrontBackFacing,
    Depth,
    TopologyEdges,
    FeatureCurves,
    FeatureCurveKinds,
    VisibilitySegments,
    HiddenSegments,
    SurfaceSamples,
    ToneField,
    DirectionField,
    DensityField,
    HatchCandidates,
    SalienceHeatmap,
    StrokeCandidates,
    FinalStrokes,
    TileBudgets,
    BvhNodes
}
```

Każdy overlay powinien mieć filtr:

- by feature kind,
- by visibility state,
- by salience range,
- by entity,
- by source mesh,
- by style layer.

### 16.5. `NprDebugFrame`

```csharp
public sealed record NprDebugFrame(
    int Width,
    int Height,
    IReadOnlyList<DebugPrimitive2D> Primitives,
    NprDebugCounters Counters,
    IReadOnlyList<NprStepTrace> StepTraces);

public sealed record DebugPrimitive2D(
    DebugOverlayKind Kind,
    IReadOnlyList<STFU.Strokes.Point2D> Points,
    STFU.Strokes.StrokeColor Color,
    float Thickness,
    string Label,
    int SourceId);
```

`STFU.UI` może rysować `StrokeFrame` i `NprDebugFrame` osobnymi warstwami.

---

## 17. Export Architecture

### 17.1. Problem

`StrokeFrame` jest obecnie dobrym outputem dla viewportu, ale eksport wymaga zachowania metadanych. SVG ma być nie tylko obrazkiem, ale warstwowym wynikiem pipeline'u: kontury, crease'y, hatch, hidden lines, construction, debug.

### 17.2. Export API

```csharp
public interface IStrokeExporter<TOptions>
{
    ExportResult Export(StrokeFrame frame, TOptions options, Stream output);
}

public sealed record ExportResult(
    bool Success,
    string? Error,
    int PathCount,
    IReadOnlyList<ExportWarning> Warnings);

public sealed record SvgExportOptions(
    SvgExportMode Mode,
    bool IncludeMetadata,
    bool IncludeDebugLayers,
    float Scale,
    string Units,
    IReadOnlyList<string> EnabledLayers);

public enum SvgExportMode
{
    Editable,
    Faithful,
    PlotterSafe,
    Debug
}
```

### 17.3. Metadata preservation

Dla każdego stroke'u zachować:

- `stroke intent`,
- `source feature id`,
- `source segment id`,
- `visibility state`,
- `thickness`,
- `opacity`,
- `color`,
- `pressure summary`,
- `style id`,
- `layer name`,
- `stable id`.

SVG może używać:

```xml
<path d="M ..." stroke="rgb(...)" stroke-width="..."
      data-stfu-stable-id="123"
      data-stfu-intent="Silhouette"
      data-stfu-feature-id="456"
      data-stfu-visibility="Visible"
      data-stfu-style="technical-line" />
```

### 17.4. Layering

Warstwy SVG:

- `background`,
- `outer-contours`,
- `inner-contours`,
- `creases`,
- `material-boundaries`,
- `hatching-primary`,
- `hatching-cross`,
- `hidden-lines`,
- `construction`,
- `debug`.

### 17.5. Plotter output

Plotter wymaga:

- ograniczenia liczby krótkich segmentów,
- minimalnej długości stroke,
- uporządkowania ścieżek dla zmniejszenia travel moves,
- braku opacity jako alpha; trzeba mapować opacity na wybór pisaka albo hatch density,
- brak filled variable-width paths, jeśli ploter ma tylko pen stroke.

### 17.6. PDF considerations

PDF jako finalny format może być generowany przez SVG-to-PDF albo osobny exporter. Uwaga: jeżeli stroke metadata ma przeżyć, PDF nie jest tak wygodny jak SVG. Dla archiwizacji technicznej eksportować oba: `.svg` z metadata i `.pdf` do druku.

---

## 18. Tests And Evaluation

### 18.1. Istniejące testy

Obecne testy sprawdzają, czy pipeline buduje bogaty graph, generuje styled paths, jest deterministyczny i czy preset registry działa. To dobry fundament. Trzeba go rozwinąć z testów „czy coś powstało” do testów „czy powstało poprawnie”.

### 18.2. Testy jednostkowe

| Test | Cel | Fixture |
|---|---|---|
| Projection determinism | Ten sam camera/mesh daje te same projected vertices. | Cube, Suzanne. |
| Topology extraction | Poprawna liczba edges/boundaries/non-manifold. | Cube, plane with hole, non-manifold sample. |
| Feature extraction | Boundary/silhouette/crease count. | Cube z kamerami canonical. |
| Visibility sample | Linie za trójkątem są hidden. | Dwa quady z overlap. |
| Visibility splitting | Długa linia częściowo zasłonięta dzieli się na segmenty. | Cross-over fixture. |
| Stroke generation | Visible segments generują candidates. | Synthetic curves. |
| Style settings | Grammar zmienia thickness/opacity/policy. | Same graph, multiple presets. |
| Humanization determinism | Seed/stable id daje identyczny path. | Synthetic stroke. |
| SVG export | Metadata i layer count poprawne. | Mini frame. |
| Performance | Czas nie przekracza progu na benchmark mesh. | Suzanne, larger mesh. |

### 18.3. Visual regression

Wizualne testy powinny zapisywać:

- PNG final frame,
- SVG final frame,
- debug overlays,
- counters JSON,
- step trace JSON.

Porównanie:

- pixel diff dla raster preview,
- path count i bounds diff dla SVG,
- contour precision/recall na syntetycznych scenach,
- stroke density histograms,
- temporal stability dla sekwencji kamer.

### 18.4. Metryki ewaluacyjne

| Metryka | Co mierzy | Użycie |
|---|---|---|
| Contour precision/recall | Zgodność linii z referencyjnymi konturami. | Feature extraction. |
| Visibility correctness | Procent segmentów z prawidłowym visible/hidden. | Hidden-line. |
| Stroke density | Liczba stroke'ów na tile/region. | Budżety i clutter. |
| Temporal stability | Zmiana stroke positions/lifetimes między klatkami. | Interakcja/animacja. |
| Perceptual similarity | Zgodność z referencją stylu. | Styl transfer i visual tuning. |
| Pairwise human preference | Czy ludzie wybierają wynik STFU jako bardziej czytelny/stylowy. | Ocena końcowa stylów. |

### 18.5. Debug snapshot schema

```json
{
  "frameId": 120,
  "presetId": "generic-sketch",
  "camera": { "fov": 60, "position": [0,0,4] },
  "counts": {
    "triangles": 968,
    "featureCurves": 241,
    "visibleSegments": 198,
    "strokeCandidates": 530,
    "finalStrokes": 421
  },
  "timingsMs": {
    "projection": 1.2,
    "features": 2.8,
    "visibility": 4.4,
    "strokes": 2.1
  }
}
```

---

## 19. Performance, Caching, And Scale

### 19.1. Główne koszty

| Etap | Typowy koszt | Problem |
|---|---|---|
| Topology build | `O(T)` | Nie robić per frame, jeśli mesh się nie zmienił. |
| Projection | `O(V + T)` | OK, ale można parallelize. |
| Feature extraction edge-level | `O(E)` | OK przy cache topology. |
| Curvature extraction | `O(V * neighborhood)` | Cache per mesh; smoothing. |
| Visibility naive | `O(C * S * T)` | Wymaga BVH/depth buffer. |
| BVH build | `O(T log T)` | Cache per mesh; update transforms. |
| BVH query | `O(log T)` average | Potrzeba robust ray tests. |
| Hatching | zależne od density | Wymaga tile budgets i LOD. |
| SVG export | `O(strokes + points)` | Variable width może zwiększyć path count. |

### 19.2. Caches

| Cache | Lifetime | Zawartość |
|---|---|---|
| Per asset | Dopóki asset loaded | raw mesh, source path, import report. |
| Per mesh | Dopóki mesh unchanged | topology, normals, bounds, curvature, BVH. |
| Per scene | Dopóki scene structure unchanged | entity list, transform versions, material refs. |
| Per camera/view | Jedna klatka lub dopóki camera unchanged | projected vertices/triangles, front-facing, depth. |
| Per preset | Dopóki preset/settings unchanged | grammar, style curves, TAM textures, thresholds. |
| Per frame | Jedna klatka | NprGraph, StrokeFrame, DebugFrame. |
| Frame history | Kilka klatek | previous curves/strokes for temporal matching. |

### 19.3. Spatial grids and tile pruning

Dla salience i clutter potrzebny jest screen-space grid:

```csharp
public sealed class ScreenTileGrid<T>
{
    public int TileSize { get; init; } = 32;
    public int Columns { get; init; }
    public int Rows { get; init; }
    public List<T>[] Tiles { get; init; } = [];
}
```

Zastosowania:

- limit stroke candidates per tile,
- local density estimation,
- szybkie wyszukiwanie nakładających się stroke'ów,
- debug heatmap clutter.

### 19.4. Parallel processing

AOT-friendly parallelizm:

- `Parallel.For` dla projekcji vertices/triangles,
- partitioning feature extraction per edge,
- parallel salience scoring,
- parallel hatching per tile/region,
- ostrożnie z mutable `NprGraph`; używać local lists i merge.

### 19.5. Memory layout

Dla dużych scen `List<record>` może być OK na start, ale hot paths mogą wymagać structure-of-arrays:

```csharp
public sealed class ProjectedTriangleBuffer
{
    public int Count;
    public int[] StableIds = [];
    public int[] A = [];
    public int[] B = [];
    public int[] C = [];
    public float[] Depth = [];
    public float[] Shade = [];
    public byte[] Flags = [];
}
```

Nie implementować od razu. Najpierw zmierzyć.

---

## 20. Roadmap

### 20.1. Immediate

1. Uporządkować dokumentację: `NPR_DRAWING_THEORY.md` jako filozofia, `NPR_THEORY.MD` jako teoria szeroka, `NPR_SUPPLEMENT.md` jako blueprint implementacyjny.
2. Dodać `FeatureCurve`, `FeaturePoint`, `FeatureCurveKind`, `FeatureCurveSource`.
3. Zastąpić `FeatureLine` albo dodać adapter `FeatureLine -> FeatureCurve` na czas migracji.
4. Dodać `VisibilitySegment`, `VisibilityState`, `SampleVisibilityResolver`.
5. Zmienić `BuildStrokeCandidatesStep`, aby przyjmował visible segments, nie raw feature lines.
6. Rozszerzyć `StrokePath2D` o `StrokePoint2D` i metadata, zachowując fallback compatibility.
7. Dodać `NprDebugCounters`, `NprStepTrace`, `NprDebugFrame`.
8. Dodać Avalonia overlay: feature kind, visibility, surface samples, final strokes.
9. Dodać testy visibility fixture i deterministycznego splittingu.
10. Dodać skeleton `SvgStrokeExporter` dla simple strokes.

### 20.2. Near-term

1. `MeshAnalysisCache` i `TopologyCache` per asset.
2. BVH dla ray visibility.
3. `ToneField`, `DirectionField`, `DensityField`.
4. Cross-hatching z drugim/trzecim kierunkiem.
5. Hatch clipping do viewport/silhouette/regions.
6. Style grammar dla `technical-line` i `pen-and-ink`.
7. Preset schema versioning i JSON editable preset.
8. SVG layered export z metadata.
9. Visual regression snapshots.
10. Tile-based stroke budgets i priority pruning.

### 20.3. Research-heavy

1. Curvature estimation robust enough for production.
2. Suggestive contours z radial curvature i derivative test.
3. Apparent ridges.
4. Ridges/valleys z smoothing confidence.
5. Temporal coherence z `FrameHistory` i matchingiem stroke'ów.
6. Tonal art maps backend.
7. GPU hybrid renderer.
8. Neural style assistance jako opcjonalne R&D, nie rdzeń.
9. Per-material stylization i semantic part importance.
10. User-study evaluation style fidelity.

---

## 21. Bibliography

Status weryfikacji:

- **Zweryfikowane URL**: źródło było dostępne jako strona, PDF lub dokumentacja w trakcie przygotowania suplementu.
- **Needs verification**: tytuł jest klasycznym źródłem NPR, ale podany URL/DOI powinien zostać ręcznie potwierdzony przed publikacją formalną.

### 21.1. Klasyczne NPR i line drawing

1. Paul Haeberli, **Paint by Numbers: Abstract Image Representations**, SIGGRAPH 1990.  
   URL/DOI: `https://doi.org/10.1145/97879.97902` — **needs verification**.  
   Znaczenie: wczesne painterly/image-based NPR; ważne historycznie dla stroke placement i user-guided abstraction.

2. Takafumi Saito, Tokiichiro Takahashi, **Comprehensible Rendering of 3-D Shapes**, SIGGRAPH 1990.  
   URL/DOI: `https://doi.org/10.1145/97879.97901` — **needs verification**.  
   Znaczenie: klasyczny punkt startowy dla renderingu z depth/normal/shape cues i czytelności formy 3D.

3. Georges Winkenbach, David H. Salesin, **Computer-Generated Pen-and-Ink Illustration**, SIGGRAPH 1994.  
   URL/DOI: `https://doi.org/10.1145/192161.192184` — **needs verification**.  
   Znaczenie: podstawowe źródło dla pen-and-ink, hatchingu i proceduralnych reguł ilustracji.

4. Michael P. Salisbury, Sean E. Anderson, Ronen Barzel, David H. Salesin, **Interactive Pen-and-Ink Illustration**, SIGGRAPH 1994.  
   URL/DOI: `https://doi.org/10.1145/192161.192195` — **needs verification**.  
   Znaczenie: stroke textures i interaktywna kontrola pen-and-ink.

5. Michael P. Salisbury et al., **Orientable Textures for Image-Based Pen-and-Ink Illustration**, SIGGRAPH 1997.  
   URL/DOI: `https://doi.org/10.1145/258734.258890` — **needs verification**.  
   Znaczenie: orientable stroke textures, ważne dla `DirectionField` i texture-driven hatching.

6. Amy Gooch, Bruce Gooch, Peter Shirley, Elaine Cohen, **A Non-Photorealistic Lighting Model For Automatic Technical Illustration**, SIGGRAPH 1998 / University of Utah tech report.  
   URL: `https://www.cs.utah.edu/docs/techreports/1998/pdf/UUCS-98-009.pdf` — zweryfikowane URL.  
   Znaczenie: technical illustration, cool-warm shading, zachowanie czytelności linii przez kontrolę tonów.

7. Doug DeCarlo, Adam Finkelstein, Szymon Rusinkiewicz, Anthony Santella, **Suggestive Contours for Conveying Shape**, ACM TOG/SIGGRAPH 2003.  
   URL: `https://gfx.cs.princeton.edu/gfx/proj/sugcon/` — zweryfikowane URL.  
   Znaczenie: podstawowe źródło suggestive contours; strona podkreśla potrzebę krzywizny i pochodnych krzywizny.

8. Szymon Rusinkiewicz, **Estimating Curvatures and Their Derivatives on Triangle Meshes**, 3DPVT 2004.  
   URL: `https://gfx.cs.princeton.edu/gfx/proj/sugcon/` — zweryfikowane jako powiązana publikacja na stronie suggestive contours.  
   Znaczenie: praktyczna estymacja krzywizn potrzebna do suggestive contours i apparent ridges.

9. Tilke Judd, Frédo Durand, Edward Adelson, **Apparent Ridges for Line Drawing**, ACM TOG/SIGGRAPH 2007.  
   URL: `https://people.csail.mit.edu/tjudd/apparentridges.html` — zweryfikowane URL.  
   Znaczenie: view-dependent curvature i apparent ridges; ważne dla bardziej ekspresyjnego line drawing.

10. Emil Praun, Hugues Hoppe, Matthew Webb, Adam Finkelstein, **Real-Time Hatching**, SIGGRAPH 2001.  
    URL: `https://hhoppe.com/hatching.pdf` — zweryfikowane URL.  
    Znaczenie: tonal art maps, spatial/temporal coherence, curvature-aligned direction fields dla hatchingu.

11. Pierre Bénard, Aaron Hertzmann, **Line Drawings from 3D Models**, Foundations and Trends in Computer Graphics and Vision, 2019 / arXiv 2018.  
    URL: `https://arxiv.org/abs/1810.01175` — zweryfikowane URL.  
    Znaczenie: syntetyczny tutorial o contour geometry, visibility, stylization, animations i tradeoffach exact vs fast methods.

12. Aaron Hertzmann, **Why Do Line Drawings Work? A Realism Hypothesis**, arXiv 2020.  
    URL: `https://arxiv.org/abs/2002.06260` — zweryfikowane URL.  
    Znaczenie: uzasadnienie percepcyjne dla wyboru linii i salience w rysunku.

13. Bruce Gooch, Amy Gooch, **Non-Photorealistic Rendering**, A K Peters, 2001.  
    URL: `https://en.wikipedia.org/wiki/Amy_Ashurst_Gooch` — pośrednie potwierdzenie bibliograficzne; formalny URL wydawcy wymaga weryfikacji.  
    Znaczenie: klasyczna książka NPR, terminologia i przegląd stylów.

14. Thomas Strothotte, Stefan Schlechtweg, **Non-Photorealistic Computer Graphics: Modeling, Rendering, and Animation**, Morgan Kaufmann, 2002.  
    URL: `https://de.wikipedia.org/wiki/Non-photorealistic_Rendering` — pośrednie potwierdzenie bibliograficzne; formalny URL wydawcy wymaga weryfikacji.  
    Znaczenie: książkowe ujęcie modelowania, renderingu i animacji NPR.

### 21.2. Neural / modern style assistance

15. Difan Liu, Mohamed Nabail, Aaron Hertzmann, Evangelos Kalogerakis, **Neural Contours: Learning to Draw Lines from 3D Shapes**, CVPR 2020.  
    URL: `https://arxiv.org/abs/2003.10333` — zweryfikowane URL.  
    Znaczenie: przykład uczenia decyzji o liniach z porównań ludzkich; nie zastępuje deterministycznego core, ale może inspirować evaluation/salience.

16. Difan Liu, Matthew Fisher, Aaron Hertzmann, Evangelos Kalogerakis, **Neural Strokes: Stylized Line Drawing of 3D Shapes**, 2021.  
    URL: `https://arxiv.org/abs/2110.03900` — zweryfikowane URL.  
    Znaczenie: stylizowane stroke'y, vector output i transfer cech kreski.

### 21.3. Engine and rendering documentation

17. Unity Manual, **Custom rendering and post-processing in URP**.  
    URL: `https://docs.unity3d.com/Manual/urp/customizing-urp.html` — zweryfikowane URL.  
    Znaczenie: odniesienie dla przyszłego runtime GPU/viewport backendu w URP.

18. Unity HDRP Manual, **Custom Pass**.  
    URL: `https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@17.0/manual/Custom-Pass.html` — zweryfikowane URL.  
    Znaczenie: custom passes, buffers i injection points dla hybrydowego renderingu.

19. Epic Games, **Post Process Materials in Unreal Engine**.  
    URL: `https://dev.epicgames.com/documentation/en-us/unreal-engine/post-process-materials-in-unreal-engine` — zweryfikowane URL.  
    Znaczenie: post-process graph, SceneTexture, GBuffer, CustomDepth/Stencil, TAA issues.

20. Freestyle, **Freestyle NPR line drawing renderer**.  
    URL: `https://freestyle.sourceforge.net/` oraz opis: `https://en.wikipedia.org/wiki/Freestyle_(software)` — częściowo zweryfikowane przez stabilną stronę opisową.  
    Znaczenie: przykład systemu NPR line drawing z programmable style modules.

---

## 22. Implementation Contract

### 22.1. Nowe abstrakcje silnika

Checklist typów do dodania:

- [ ] `FeatureCurve`
- [ ] `FeaturePoint`
- [ ] `FeatureCurveKind`
- [ ] `FeatureCurveSource`
- [ ] `VisibilitySegment`
- [ ] `VisibilityState`
- [ ] `IOcclusionQuery`
- [ ] `IVisibilityResolver`
- [ ] `NprViewContext`
- [ ] `ProjectionInfo`
- [ ] `LightContext`
- [ ] `MeshAnalysisCache`
- [ ] `TopologyCache`
- [ ] `CurvatureCache`
- [ ] `SalienceScore`
- [ ] `LinePriorityRule`
- [ ] `ToneField`
- [ ] `DirectionField`
- [ ] `DensityField`
- [ ] `TextureField`
- [ ] `MaterialRegion`
- [ ] `HatchingPlan`
- [ ] `StrokeCandidate`
- [ ] `StyledStroke`
- [ ] `StrokePoint2D`
- [ ] `StrokeMetadata`
- [ ] `StyleGrammar`
- [ ] `StyleFeatureRule`
- [ ] `StyleVisibilityRule`
- [ ] `StyleToneRule`
- [ ] `StyleStrokeRule`
- [ ] `FrameHistory`
- [ ] `NprDebugFrame`
- [ ] `NprDebugCounters`
- [ ] `NprStepTrace`
- [ ] `IStrokeExporter`
- [ ] `SvgStrokeExporter`

### 22.2. Pliki/moduły prawdopodobnie dotknięte

```text
src/aot/STFU.NPR/Graph/
    FeatureLine.cs              -> deprecated / adapter
    FeatureCurve.cs             -> new
    VisibilitySegment.cs        -> new
    StrokeCandidate.cs          -> new
    NprGraph.cs                 -> expanded sections

src/aot/STFU.NPR/Pipeline/
    NprContext.cs               -> add NprViewContext, DebugOptions
    INprStep.cs                 -> keep
    NprPipeline.cs              -> keep, optionally add tracing

src/aot/STFU.NPR/Steps/Mesh/
    ProjectMeshStep.cs          -> use view context
    BuildProjectedTrianglesStep.cs -> keep, add debug/counters
    BuildMeshTopologyStep.cs    -> move/cache into analysis later
    ExtractFeatureLinesStep.cs  -> replace with ExtractFeatureCurvesStep
    BuildSurfaceSamplesStep.cs  -> evolve into tone samples
    BuildHatchingStep.cs        -> evolve into field-driven hatching

src/aot/STFU.NPR/Steps/Analysis/
    ApplyApproximateOcclusionStep.cs -> replace with ResolveCurveVisibilityStep
    PruneFeatureLinesStep.cs         -> replace with Score/PruneBySalienceStep

src/aot/STFU.NPR/Steps/Strokes/
    BuildStrokeCandidatesStep.cs -> consume VisibilitySegment
    StyleStrokesStep.cs         -> consume StyleGrammar
    HumanizeStrokesStep.cs      -> per-point profiles
    BuildStrokeFrameStep.cs     -> output metadata-rich StrokeFrame

src/aot/STFU.NPR/Composition/
    INprPreset.cs               -> add CreateGrammar
    NprPresetMetadata.cs        -> add version/author/tags/packaging
    NprPresetRegistry.cs        -> provider/plugin model
    SketchNprPreset.cs          -> migrate to StyleGrammar

src/aot/STFU.Strokes/
    Stroke2D.cs                 -> add StrokePoint2D and metadata
    StrokeFrame.cs              -> maybe add metadata/layers

src/aot/STFU.Strokes/Export/
    SvgStrokeExporter.cs        -> new

src/aot/STFU.Viewport/
    ViewportState.cs            -> hold debug overlay selection
    ViewportSnapshot.cs         -> include NprDebugFrame optional

src/runtime/STFU.UI/
    MainWindow.cs               -> overlays, panels, export UI
```

### 22.3. Pierwsze 10 tasków implementacyjnych

1. **Dodać typy `FeatureCurve`, `FeaturePoint`, `FeatureCurveKind`, `FeatureCurveSource`.** Nie usuwać od razu `FeatureLine`; stworzyć adapter, aby testy nadal przechodziły.
2. **Rozszerzyć `NprGraph` na sekcje `Geometry`, `Features`, `Visibility`, `Tone`, `Strokes`, `Debug`.** Początkowo zachować stare właściwości jako forwarding/deprecated, jeżeli to zmniejsza koszt migracji.
3. **Zastąpić `ExtractFeatureLinesStep` nowym `ExtractFeatureCurvesStep` dla boundary/silhouette/crease.** Wynik nadal może być krzywą dwupunktową, ale typ musi być przyszłościowy.
4. **Dodać `VisibilitySegment` i `SampleVisibilityResolver`.** Pierwsza wersja może kopiować logikę start/mid/end, ale ma produkować segmenty, nie usuwać całe linie.
5. **Zmienić `BuildStrokeCandidatesStep`, aby czytał visible segments.** Hidden segments zostawić do debug i stylów technicznych.
6. **Dodać `StrokePoint2D` i `StrokeMetadata` w `STFU.Strokes`.** `StrokePath2D.Line` może tworzyć punkty z domyślną width/pressure.
7. **Dodać `NprDebugCounters` i `NprStepTrace`.** Każdy step powinien raportować counts i czas, nawet jeśli UI jeszcze tego nie pokazuje.
8. **Dodać overlay `FeatureCurves` i `VisibilitySegments` do Avalonia.** To jest ważniejsze niż nowy styl, bo umożliwia diagnozowanie pipeline'u.
9. **Dodać pierwszy `SvgStrokeExporter` w trybie simple.** Eksport path + stroke-width + opacity + color + data attributes dla stable id/intencji.
10. **Dodać test fixtures dla częściowej widoczności.** Najprostsza scena: linia/edge przecinana przez prosty quad occluder; oczekiwane dwa visible segments i jeden hidden segment.

### 22.4. Kryterium akceptacji następnej fazy

Następna faza STFU jest zaakceptowana, gdy:

- `generic-sketch` nadal działa i przechodzi dotychczasowe testy,
- pipeline tworzy `FeatureCurve` zamiast polegać bezpośrednio na `FeatureLine`,
- visibility jest reprezentowana jako segmenty,
- `StrokeFrame` może przenosić metadata,
- Avalonia potrafi pokazać co najmniej feature curve overlay i visibility overlay,
- istnieje podstawowy SVG export,
- testy deterministyczności obejmują nowe segmenty i stroke metadata.

Kryterium jakościowe: STFU ma przestać być „stylizowanym wireframe'em”, a zacząć być silnikiem decyzji rysunkowych. Każda kreska w finalnym obrazie powinna dać się prześledzić: `mesh/topology/curvature -> feature curve -> visibility segment -> salience/style rule -> stroke candidate -> styled stroke -> StrokeFrame/export`.
