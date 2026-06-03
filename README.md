# STFU

Scene-To-Flat Unrenderer.

STFU is an experimental .NET 10 mini engine for turning 3D meshes into 2D NPR-style drawings.

## What Is Inside

- AOT-friendly engine modules under `src/aot`.
- Console host under `src/runtime/STFU.App`.
- Avalonia viewport UI under `src/runtime/STFU.UI`.
- Mesh loading with `assets/suzanne.obj`.
- NPR pipeline with presets, feature extraction, hatching, stroke styling, and path-based output.

See [NPR Drawing Theory](docs/NPR_DRAWING_THEORY.md) for the drawing concepts this codebase is intended to encode.
See [Target UI Screen](docs/UI_TARGET_SCREEN.md) for the planned Avalonia workspace layout.
Interactive HTML maquettes live in [maquettes](maquettes/).

## Controls

- `1` - mesh view.
- `2` - NPR view.
- Left mouse drag - orbit camera.
- Ctrl + left mouse drag - pan camera.
- Mouse wheel - change FOV.

## Build

```powershell
dotnet build STFU.slnx
```

## Test

```powershell
dotnet run --project src/tests/STFU.NPR.Tests
```

## Run

```powershell
dotnet run --project src/runtime/STFU.App
```
