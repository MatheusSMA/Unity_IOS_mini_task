# T22 — Add window mode button + PlayMode test

**Phase**: 4 (P2 Windows) | **Requirement**: WIN-01 (AC1) | **Depends on**: T21 | **Tests**: PlayMode | **Gate**: full

## Functional description

The entry door to window drawing — visible only when it can actually work: a Wall is selected AND the app is in Orbit mode. Nothing selected, floor/ceiling selected, or in AR/2D → hidden (a visible-but-dead button was the grill finding behind AD-015).

## Technical description

- **File**: `Assets/Scripts/UI/WindowModeButton.cs`; uGUI button on the T19 canvas.
- Visibility = `selectedSurface?.kind == Wall && ModeManager.Current == Orbit` (AD-015); recomputed on `SelectionChanged` AND `ModeChanged`.
- OnClick: `ModeManager.TrySet(Mode.WindowDraw)` (entry predicate re-checks Wall selected — AD-013); while in WindowDraw the button acts as exit → `TrySet(Orbit)`.

## Tests (PlayMode)

Wall selected + Orbit → visible; floor selected → hidden; nothing selected → hidden; mode = TopDown (stub) with Wall selected → hidden. >= 4 tests.

## Done when

- [ ] All 4 visibility cases correct; click toggles the mode
- [ ] `run_tests` EditMode + PlayMode green, >= 4 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI, run_tests) | **Commit**: `[feat] add window mode button with visibility gating`
