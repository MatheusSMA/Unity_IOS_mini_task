# T20 — Add Clear button + PlayMode test

**Phase**: 3 (P1 Presentation) | **Requirement**: CLR-01 | **Depends on**: T19 | **Tests**: PlayMode | **Gate**: full

## Functional description

The one and only way back to "nothing selected" (tap-outside-clears was removed, AD-007): a button that deselects, removes the tint and updates the list row. Pressing it with nothing selected does nothing — silently, with no event.

## Technical description

- **File**: `Assets/Scripts/UI/ClearButton.cs`; uGUI button on the T19 canvas.
- OnClick:
  - If `ModeManager.Current == WindowDraw`: first `TrySet(Orbit)` (cancels the in-progress draw via ModeManager's cancel event), then `RoomModel.ClearSelection()` (CLR AC3 — design: "forces TrySet(Orbit) then clears").
  - Otherwise: `RoomModel.ClearSelection()` directly.
- Idempotency lives in the model (T09); the button adds nothing on top.
- The full window-mode variant of AC3 is exercised end-to-end in T21's tests (WindowDrawController doesn't exist yet in this phase); this task wires and tests the call order with a stubbed mode.

## Tests (PlayMode)

Press with selection → deselected + tint gone + row updated; press with none → no `SelectionChanged` fired (event spy). >= 2 tests.

## Done when

- [ ] CLR AC1-AC2 verified; AC3 call order wired
- [ ] `run_tests` EditMode + PlayMode green, >= 2 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI, run_tests) | **Commit**: `[feat] add clear selection button`
