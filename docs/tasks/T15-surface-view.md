# T15 — Implement SurfaceView + PlayMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: SEL-02, WIN-02 (AC5, AC12) | **Depends on**: T10, T11 | **Tests**: PlayMode | **Gate**: full

## Functional description

The visible body of each surface: renders the slab mesh, shows the selection tint the instant a surface is selected, and — critically — keeps the physics collider in lockstep with the visual mesh, so a cut window is see-through AND ray-through (no ghost wall inside openings).

## Technical description

- **File**: `Assets/Scripts/Presentation/SurfaceView.cs` — per-surface MeshFilter + MeshRenderer + MeshCollider.
- **Tint (SEL-02, AD-010)**: subscribes `SelectionChanged(previous, current)`; applies/clears a tint color via **MaterialPropertyBlock only** — never instantiate materials. Visible from any angle (it's a base-color tint, not view-dependent).
- **Rebuild (AD-009)**: on `WindowAdded`/`WindowRemoved` for its surface: call `SurfaceMeshBuilder.Build`, convert `MeshData` → `Mesh`, then in the **same operation** `collider.sharedMesh = null; collider.sharedMesh = newMesh`. Renderer-only updates are the design's named ghost-collider bug — forbidden.
- On builder validation failure: keep previous mesh AND collider, report failure so RoomModel rolls back (EDGE-05).
- P2 (T24) later adds the `SelectedSurface` layer swap here.

## Tests (PlayMode)

Tint set on select, cleared on deselect (read MaterialPropertyBlock); `TryAddWindow` → `Physics.Raycast` through the opening does NOT hit this wall; the same ray after `TryRemoveWindow` DOES hit; rebuild failure keeps old collider. >= 4 tests.

## Done when

- [ ] Tint via MPB; mesh+collider swapped atomically on every rebuild
- [ ] `run_tests` EditMode + PlayMode green, >= 4 PlayMode tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add surface view with tint and collider sync`
