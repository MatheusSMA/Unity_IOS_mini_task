# T21 — Implement WindowDrawController + PlayMode tests

**Phase**: 4 (P2 Windows) | **Requirement**: WIN-01, WIN-02 (AC3), EDGE-03, EDGE-04, CLR-01 (AC3) | **Depends on**: none intra-phase (cross-phase: T10, T13, T14, T18) | **Tests**: PlayMode | **Gate**: full

## Functional description

Drawing a window: in window mode, dragging on the selected wall shows a live rectangle preview between the start corner and the finger; releasing a valid drag cuts a real see-through hole. Invalid drags (overlap, too small/large, margin) just drop the preview — the wall never changes. Leaving the mode mid-drag (Clear, 2D, any mode change) cancels cleanly.

## Technical description

- **File**: `Assets/Scripts/Presentation/WindowDrawController.cs`; active only while `ModeManager.Current == WindowDraw`.
- **DragStart**: raycast fixes the target wall (must be the selected wall — mode entry guarantees a Wall is selected) + first corner projected to wall-local 2D. Start on empty/floor/ceiling → no preview, no window (WIN AC11).
- **Drag**: preview quad in wall space between the fixed corner and current projected finger position, **clamped live** to wall bounds minus 0.1 m margin. Off-wall finger clamps to bounds, never switches walls (EDGE-03).
- **DragEnd**: `RoomModel.TryAddWindow(wallId, rect, out reason)` — success: hole cut via the T15 rebuild path; rejection: destroy preview, wall untouched (WIN AC7-AC9). Tap-like release below min size → rejected (EDGE-04).
- **Cancel**: subscribe ModeManager's draw-cancel event — any exit from WindowDraw (Clear button path, 2D entry, mode change) destroys the preview with no window (design: WindowDrawController purpose).

## Tests (PlayMode)

Valid drag → window exists + ray through opening misses wall; tap without drag → no window; off-wall release → clamped rect; overlap/tiny/oversized → rejected, preview gone; Clear during draw → mode back to Orbit + draw cancelled + selection cleared (CLR AC3 end-to-end); mode exit mid-drag → cancelled. >= 6 tests.

## Done when

- [ ] Full draw loop + all rejection and cancel paths
- [ ] `run_tests` EditMode + PlayMode green, >= 6 PlayMode tests

**Tools**: unity-mcp (run_tests), context7 (projection API doubt) | **Commit**: `[feat] add window drawing with live preview`
