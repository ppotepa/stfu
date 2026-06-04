# Assets agent instructions

This subtree contains local model and animation assets.

Token-saving rules:

- Do not open large binary or model files unless the task is explicitly about import, asset validation, or visual debugging.
- Prefer metadata commands over full file reads.
- For OBJ/FBX/GLB tasks, inspect file existence, size, extension, and loader logs first.
- Avoid adding large assets to commits unless explicitly requested.

Useful commands:

```powershell
Get-ChildItem assets -File | Select-Object Name, Length, LastWriteTime
Get-ChildItem assets -Recurse -File -Include *.obj,*.fbx,*.glb,*.gltf,*.amc | Select-Object FullName, Length
```
