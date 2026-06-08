# Scene visible widgets batch 008

This package is intentionally UI-only.

It assumes the Scene bridge/backend already exists in the target repository:

- `ScenePanelViewModel`
- `EntityListItem`
- `SceneDiagnosticItem`
- `SceneMeshBoundsInfo`
- `SceneRoleRouteItem`
- `SceneWidgetDescriptor`
- `InspectorViewModel.IsSceneActive`

The batch adds a real Avalonia `SceneTabView` and wires it into `InspectorView` from code-behind so the Scene tab displays visible widgets without needing to rewrite the missing `InspectorView.axaml` from the concat snapshot.

Validation:

```powershell
$repo = "<path-to-stfu4>"
rpack inspect stfu-scene-visible-widgets-008-ui-only.rpack
rpack lint stfu-scene-visible-widgets-008-ui-only.rpack
rpack check stfu-scene-visible-widgets-008-ui-only.rpack --repo $repo
rpack apply stfu-scene-visible-widgets-008-ui-only.rpack --repo $repo
dotnet restore
dotnet build --no-restore
```
