# T04 — Create bootstrap scene

**Phase**: 1 (P0 Bootstrap) | **Requirement**: BOOT-01 | **Depends on**: T03 | **Tests**: none | **Gate**: build

## Functional description

The single scene the app runs in. Empty at this point — it must simply open in the Editor with a clean console (BOOT-01 AC3). Everything later (room, camera rig, canvas) is added to this scene by its own task.

## Technical description

- **File**: `Assets/Scenes/Main.unity`.
- Contents: default Main Camera + Directional Light is enough; camera will be replaced by the rig in Phase 3.
- Register as scene 0 in Build Settings (needed for the iOS target-switch check in T06 and for PlayMode tests that `SceneManager.LoadScene` it).

## Done when

- [ ] Scene opens with zero console errors
- [ ] Scene 0 in Build Settings (build gate)

**Tools**: unity-mcp + unity-mcp-skill | **Commit**: `[chore] add bootstrap scene`
