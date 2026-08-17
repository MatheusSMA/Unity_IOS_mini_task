# T27 — Implement TopDownController + PlayMode tests

**Phase**: 5 (P3 AR + 2D) | **Requirement**: TOP-01, TOP-02 (AC3, AC4, AC9) | **Depends on**: none intra-phase (cross-phase: T13, T16, T18) | **Tests**: PlayMode | **Gate**: full

## Functional description

The 2D plan: an orthographic top-down view framed to fit the room, where the user still selects surfaces and can pinch-zoom. Entering 2D is a clean slate — selection cleared, any window draw cancelled, ceiling removed from view AND from physics (an invisible ceiling collider would eat every tap). Returning to 3D restores everything with the camera at the room centre.

## Technical description

- **File**: `Assets/Scripts/Presentation/TopDownController.cs`.
- **Enter 2D** (`TrySet(TopDown)` — legal from any mode, AD-013):
  - Orthographic top camera, **fit-to-room**: ortho size = room bounds + small serialized margin, never further out (TOP AC7, AD-016).
  - Ceiling SurfaceView: disable MeshRenderer **AND** MeshCollider (TOP AC2, AD-012 — renderer-only leaves the tap-blocking ghost).
  - Cancel transient state: selection cleared + in-progress draw cancelled (TOP AC3) — fired via ModeManager side-effect events.
- **While 2D**: plan taps flow through SelectionController unchanged (TOP AC4; tint pipeline identical); **pinch zoom** scales ortho size within 0.5x–2.0x of fit (serialized limits, AD-016); pinch has no effect in 3D views (TOP AC9).
- **Exit to 3D** (only to Orbit, AD-013): restore ceiling renderer + collider, `OrbitCameraController.ResetToRoomCentre()` (TOP AC5).

## Tests (PlayMode)

Enter 2D → ceiling renderer+collider off, selection null, camera orthographic at fit size; tap position over ceiling area hits floor (not blocked); pinch → size clamped to [0.5, 2.0]x fit; pinch in 3D → no change; exit → ceiling restored, camera at centre in Orbit. >= 5 tests.

## Done when

- [ ] TOP AC2/AC3/AC5/AC7/AC9 verified
- [ ] `run_tests` EditMode + PlayMode green, >= 5 PlayMode tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add top-down 2D view controller`
