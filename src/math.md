# STFU.Common.Math direction

## Cel

`STFU.Common.Math` ma byc wspolna, stabilna biblioteka matematyczna uzywana przez domeny silnika: mesh, NPR, rendering, camera, viewport, import i parallelism.

Chcemy osiagnac trzy rzeczy:

1. Uporzadkowac odpowiedzialnosci przez fizyczne subdomeny folderow.
2. Zachowac czyste, deterministyczne API matematyczne bez ukrytego stanu.
3. Dodac jawny mechanizm cache dla drogich obliczen w hot-pathach, ale poza czystymi funkcjami `Math`.

## Obecny kierunek restrukturyzacji

Pliki w `aot/STFU.Common/Math` powinny byc grupowane wedlug odpowiedzialnosci:

- `Numerics`: clamp, abs, lerp, round, divide, range, metric, byte scaling, buffer sizing.
- `Geometry`: 2D/3D geometry, paths, transforms, mesh topology helpers.
- `Projection`: camera basis, orbit state, perspective scalars, point projection.
- `Rendering`: raster bounds, tiles, visibility sampling, color, stroke, GPU packing.
- `Sampling`: animation sampling, procedural noise.
- `Hashing`: stable hashes, signatures, deterministic ids.

Na tym etapie namespace powinien pozostac:

```csharp
namespace STFU.Common.Math;
```

Dzieki temu zmiana jest restrukturyzacja plikow, a nie migracja publicznego API. Istniejace domeny nadal moga uzywac:

```csharp
using STFU.Common.Math;
```

## Zasada podstawowa

`STFU.Common.Math` pozostaje stateless.

To znaczy:

- funkcje sa czyste i deterministyczne,
- brak globalnych cache,
- brak ukrytego stanu,
- brak zaleznosci od cyklu zycia frame, mesh, camera albo render pass,
- brak invalidation logiki w helperach matematycznych.

To jest wazne dla AOT, wielowatkowosci, testow, deterministycznego renderingu i latwego debugowania.

## Cache-math

Cache jest potrzebny, ale powinien byc jawny w lancuchu wywolania.

Preferowany model:

```text
hot-path -> domain/frame cache -> pure STFU.Common.Math
```

Nie chcemy modelu:

```text
hot-path -> global cached Math
```

Cache powinien byc opt-in. Kod, ktory go uzywa, musi jasno wskazywac scope i klucz waznosci danych.

Przykladowy ksztalt:

```csharp
var scalars = context.MathCache.Projection.GetPerspectiveScalars(cameraKey, viewportKey);
```

albo:

```csharp
var scalars = projectionCache.GetOrCreate(key, static key =>
    ProjectionMath.CreatePerspectiveScalars(
        key.FieldOfViewDegrees,
        key.AspectRatio,
        key.NearPlane,
        key.FarPlane));
```

## Proponowane typy cache

### FrameMathCache

Cache resetowany co klatke.

Dobry dla wartosci, ktore sa drogie w ramach jednej klatki, ale nie powinny zyc dluzej:

- projection scalars,
- camera basis,
- viewport scale factors,
- transformed positions dla konkretnego passu,
- triangle screen bounds,
- tile ranges.

### VersionedMathCache

Cache oparty o klucz danych i wersje.

Dobry dla danych, ktore moga zyc wiele klatek, ale musza byc uniewazniane po zmianie wejscia:

- mesh bounds,
- triangle normals,
- topology edge keys,
- welded or quantized vertex keys,
- mean edge length,
- adjacent normal angles,
- preset/style signatures.

### Domain-specific caches

Docelowo cache powinien mieszkac tam, gdzie domena zna cykl zycia danych:

- `MeshGeometryCache`
- `MeshTopologyCache`
- `ProjectionCache`
- `RasterBoundsCache`
- `VisibilitySamplingCache`
- `NprStyleSignatureCache`
- `RenderPassScratch`

Te cache moga uzywac wspolnych prymitywow, ale nie powinny byc schowane w samych funkcjach `Math`.

## Co cache'owac

Cache ma sens, gdy wynik jest:

- kosztowny,
- powtarzalny,
- zalezy od stabilnego inputu,
- wystepuje w petlach po meshach, vertexach, triangle'ach, tile'ach albo frame'ach,
- ma jasny klucz invalidation.

Dobre kandydaty:

- `CameraMath.CreateBasis`
- `ProjectionMath.CreatePerspectiveScalars`
- `ProjectionMath.Project` dla stabilnego zestawu vertexow i kamery
- `Geometry3D.Bounds`
- `Geometry3D.TriangleNormal`
- `Geometry3D.MeanTriangleEdgeLength`
- `Geometry3D.NormalAngleDegrees`
- `MeshTopologyMath.CreateUndirectedEdgeKey` w masowej budowie topologii
- `RasterMath.TrianglePixelBounds`
- `RasterMath.TileRangeFromPixelRange`
- stable hash/signature dla presetow, mesh data i render request

## Czego nie cache'owac

Nie cache'ujemy prostych operacji, gdzie lookup bedzie drozszy niz obliczenie:

- `Clamp`
- `Clamp01`
- `AtLeast`
- `AtMost`
- `Abs`
- `Lerp`
- proste `Distance2` / `Distance3`, chyba ze istnieje udowodniona powtarzalnosc tych samych par
- proste wrappery typu `RangeMath`, `SizeMath`, `DiffMath`

Nie cache'ujemy tez `NoiseMath.Noise01` globalnie, jezeli wejscia sa prawie zawsze inne albo zalezne od indeksu stroke/point/pass.

## Klucze i invalidation

Kazdy cache musi miec jasny klucz.

Przyklady:

- camera: position, target, fov, near/far, viewport size, camera version,
- mesh: mesh handle/id, mesh version, transform version,
- render pass: frame id, pass id, viewport size, quality settings,
- style: preset id, preset version, style mask/version,
- topology: mesh id, mesh geometry version, topology mode.

Preferowane sa male immutable key structy:

```csharp
public readonly record struct ProjectionCacheKey(
    int CameraVersion,
    int ViewportWidth,
    int ViewportHeight,
    float FieldOfViewDegrees,
    float NearPlane,
    float FarPlane);
```

## Wielowatkowosc

Cache w hot-pathach renderingu musi miec jawny model watkow:

- per-frame scratch moze byc single-owner,
- per-worker cache moze unikac lockow,
- globalny shared cache wymaga limitow pamieci i synchronizacji,
- deterministyczne pipeline'y nie powinny zalezec od kolejnosci wypelniania cache.

Domyslnie preferujemy cache lokalny dla frame/pass/worker zamiast globalnego singletona.

## Non-goals

Nie chcemy:

- zmieniac namespace'ow publicznych typow tylko dla porzadku folderow,
- wprowadzac globalnego cache do `STFU.Common.Math`,
- cache'owac kazdej funkcji matematycznej automatycznie,
- ukrywac invalidation w helperach,
- robic refaktoru, ktory zmienia wyniki renderingu bez testow/parity checkow.

## Plan wdrozenia

1. Utrzymac obecny podzial folderow w `STFU.Common.Math`.
2. Zidentyfikowac hot-pathy przez pomiar: projection, mesh topology, visibility, raster tile ranges.
3. Dodac male cache primitives, najlepiej jako jawne typy scoped do frame/pass/domain.
4. Przenosic cache do domen, ktore znaja cykl zycia danych.
5. Zostawic `STFU.Common.Math` jako warstwe czystych funkcji.
6. Weryfikowac zmiany buildem, testami i parity snapshotami renderingu.

## Oczekiwany efekt

Po zmianach kod powinien miec:

- czytelny podzial matematyki na subdomeny,
- brak kosztownych powtorzen w najciezszych petlach,
- jawne granice cache i invalidation,
- zachowana kompatybilnosc `using STFU.Common.Math`,
- lepsza kontrola pamieci i deterministycznosci niz przy globalnym cache.
