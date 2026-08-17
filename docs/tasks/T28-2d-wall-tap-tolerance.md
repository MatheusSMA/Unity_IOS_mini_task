# T28 — Add 2D wall-tap tolerance to SelectionController + PlayMode test

**Phase**: 5 (P3 AR + 2D) | **Requirement**: TOP-02 (AC8) | **Depends on**: T27 | **Tests**: PlayMode | **Gate**: full

## Functional description

Makes walls tappable in the plan. Edge-on, a 0.15 m wall is ~10 px on screen — untappable (the grill finding behind AD-016). So in 2D, tapping the floor within 30 px of a wall selects that wall instead of the floor.

## Technical description

- **File**: `Assets/Scripts/Presentation/SelectionController.cs` (modify — T18's resolution order gains one branch).
- Active only while `ModeManager.Current == TopDown`.
- On a **Floor** hit: project each wall's plan segment to screen space; if the tap is within the serialized tolerance (30 px default) of the nearest wall segment → `Select(thatWall.id)`; otherwise select the floor as usual.
- Direct wall hits unaffected; no effect in any 3D mode.

## Tests (PlayMode)

In 2D: floor tap 10 px from a wall → wall selected; floor tap far from all walls → floor selected; same near-wall tap in Orbit (3D) → floor selected. >= 3 tests.

## Done when

- [ ] 3 cases pass; tolerance serialized
- [ ] `run_tests` EditMode + PlayMode green, >= 3 PlayMode tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add 2D wall tap tolerance`
