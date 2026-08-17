# T14 — Implement InputRouter + PlayMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: SEL-03 (AC5), EDGE-01, EDGE-02 | **Depends on**: T13 | **Tests**: PlayMode | **Gate**: full

## Functional description

The single doorway for all touch input. Decides, once and in one place, whether a touch was a tap or a drag (so an orbit gesture never accidentally selects a wall), ignores second fingers, refuses to raycast through UI, and lets the mouse stand in for touch in the Editor.

## Technical description

- **File**: `Assets/Scripts/Presentation/InputRouter.cs`.
- EnhancedTouch entry point; `TouchSimulation.Enable()` in Editor (design risk item — EnhancedTouch has no native mouse events).
- **Classification** (single place, controllers never re-classify): movement < 20 px DPI-scaled AND duration < 300 ms → tap; otherwise drag. Both thresholds serialized.
- **Events**: `Tapped(Vector2)`, `DragStart(Vector2)`, `DragDelta(Vector2)`, `DragEnd(Vector2)`.
- Primary touch only (EDGE-01). `EventSystem.current.IsPointerOverGameObject(touchId)` gate before emitting scene-directed events (EDGE-02, SEL-03).
- Dispatch target chosen by `ModeManager.Current` (drag → orbit in Orbit mode, drag → window drawing in WindowDraw, ignored for camera in Ar).

## Tests (PlayMode, InputTestFixture from `Unity.InputSystem.TestFramework`)

Short still touch → Tapped only; move > threshold → DragStart/Delta/End, no Tapped; long-press past 300 ms → drag, no tap; second simultaneous touch ignored; touch over a uGUI overlay → no scene event. >= 5 tests.

## Done when

- [ ] Events fire per classification; UI gate + primary-touch rule active
- [ ] `run_tests` EditMode + PlayMode green, >= 5 PlayMode tests

**Tools**: unity-mcp (run_tests), context7 (InputTestFixture API) | **Commit**: `[feat] add input router with tap drag classification`
