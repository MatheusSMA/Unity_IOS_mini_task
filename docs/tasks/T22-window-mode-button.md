# T22 — Add window mode button + PlayMode test

**Phase**: 4 (P2 Windows) | **Requirement**: WIN-01 (AC1) | **Depends on**: T21 | **Tests**: PlayMode | **Gate**: full

## Functional description

The entry door to window drawing — live only when it can actually work: a Wall is selected AND the app is in Orbit mode. Nothing selected, floor/ceiling selected, or in AR/2D → the button was hidden (AD-015).

**Superseded by T30 (AD-019, AD-021, 2026-08-18):** the button is never hidden. It stays on screen and is *disabled* in those cases, stays enabled while window mode runs so pressing it exits, and its state dot marks the active mode. Read T30 for the behaviour in force.

## Technical description

- **File**: `Assets/Scripts/UI/WindowModeButton.cs`; uGUI button on the T19 canvas.
- Visibility = `selectedSurface?.kind == Wall && ModeManager.Current == Orbit` (AD-015); recomputed on `SelectionChanged` AND `ModeChanged`. **T30 turned this into `Button.interactable`, with `Orbit or WindowDraw` as the enabled set.**
- OnClick: `ModeManager.TrySet(Mode.WindowDraw)` (entry predicate re-checks Wall selected — AD-013); while in WindowDraw the button acts as exit → `TrySet(Orbit)`.

## Tests (PlayMode)

Wall selected + Orbit → visible; floor selected → hidden; nothing selected → hidden; mode = TopDown (stub) with Wall selected → hidden. >= 4 tests. **T30 rewrote these as enabled/disabled with the GameObject always active.**

## Done when

- [ ] All 4 visibility cases correct; click toggles the mode
- [ ] `run_tests` EditMode + PlayMode green, >= 4 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI, run_tests) | **Commit**: `[feat] add window mode button with visibility gating`
