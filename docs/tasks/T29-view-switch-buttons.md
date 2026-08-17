# T29 — Add "2D | 3D" buttons + PlayMode test

**Phase**: 5 (P3 AR + 2D) | **Requirement**: TOP-01 (AC1), TOP-02 (AC6) | **Depends on**: T27 | **Tests**: PlayMode | **Gate**: full

## Functional description

The visible switch: two buttons at the top of the screen, "2D | 3D". In 2D, the surface list panel and the Clear button keep working — the plan is a full working view, not a screenshot.

## Technical description

- **File**: `Assets/Scripts/UI/ViewSwitchButtons.cs`; two uGUI buttons anchored top-centre of the T19 canvas.
- "2D" → `ModeManager.TrySet(Mode.TopDown)` (TopDownController does the rest); "3D" → `TrySet(Mode.Orbit)`.
- Active-view state reflected on the buttons (current one highlighted/disabled).
- No extra logic here: panel + Clear functionality in 2D falls out of the architecture (they subscribe RoomModel, which is mode-agnostic) — the test proves it (TOP AC6).

## Tests (PlayMode)

"2D" click → TopDown active; "3D" click → Orbit active, camera at centre; while 2D: plan tap updates the list row AND Clear button clears. >= 3 tests.

## Done when

- [ ] Switch both ways; list + Clear functional in 2D
- [ ] `run_tests` EditMode + PlayMode green, >= 3 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI, run_tests) | **Commit**: `[feat] add 2D 3D view switch buttons`
