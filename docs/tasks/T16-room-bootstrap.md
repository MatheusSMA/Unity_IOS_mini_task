# T16 — Implement RoomBootstrap scene wiring + PlayMode test

**Phase**: 3 (P1 Presentation) | **Requirement**: ROOM-01 | **Depends on**: T12, T15 | **Tests**: PlayMode | **Gate**: full

## Functional description

First moment the user sees something: pressing Play shows the synthetic room — four solid walls, floor and ceiling. This task assembles the pieces built so far into the Main scene.

## Technical description

- **File**: `Assets/Scripts/Presentation/RoomBootstrap.cs`; added to `Assets/Scenes/Main.unity` via Unity MCP.
- On Awake: `RoomBuilder.Build` with serialized defaults (6 m x 4 m footprint, 2.8 m height, 0.15 m thickness — spec Assumptions); create RoomModel; instantiate one SurfaceView per SurfaceDefinition with its initial mesh (via SurfaceMeshBuilder, zero holes).
- Owns/locates the shared references later controllers need (RoomModel instance, camera rig root, ModeManager) — plain serialized fields, no DI framework (AD-002).
- Basic lit URP material for surfaces (one material, per-surface tint stays in MaterialPropertyBlock).

## Tests (PlayMode)

Load Main scene → exactly 6 SurfaceViews, each with MeshCollider whose sharedMesh is non-null; surface ids/names match RoomDefinition. >= 2 tests.

## Done when

- [ ] Play Mode shows the room as solid slabs
- [ ] `run_tests` EditMode + PlayMode green, >= 2 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (scene edit, run_tests) | **Commit**: `[feat] add room bootstrap and scene wiring`
