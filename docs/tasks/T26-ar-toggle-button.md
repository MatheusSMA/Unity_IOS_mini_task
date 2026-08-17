# T26 — Add AR mode toggle + availability gating + PlayMode test

**Phase**: 5 (P3 AR + 2D) | **Requirement**: AR-01 (AC1, AC3, AC4) | **Depends on**: T25 | **Tests**: PlayMode | **Gate**: full

## Functional description

The switch into and out of AR. On platforms without AR tracking the toggle sits disabled (greyed) and the touch camera keeps working — the feature degrades, never breaks.

## Technical description

- **File**: `Assets/Scripts/UI/ArToggleButton.cs`; uGUI button on the T19 canvas.
- OnClick: `ModeManager.TrySet(Mode.Ar)` / back to `TrySet(Mode.Orbit)` (session start/end + handoff are ModeManager/T25 side effects, not button logic).
- **Availability (AC4)**: check AR support (ARSession availability / XR loader active); unavailable → `interactable = false`. In Editor, XR Simulation counts as available.
- Button hidden or disabled while in TopDown (Ar only enterable from Orbit, AD-013 — UI should prevent illegal transitions, design Error Handling).

## Tests (PlayMode)

Availability true → enabled, click enters Ar; availability false (stubbed) → disabled; exit returns to Orbit. >= 2 tests.

## Done when

- [ ] Toggle + gating work; illegal states never reachable from the UI
- [ ] `run_tests` EditMode + PlayMode green, >= 2 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI, run_tests) | **Commit**: `[feat] add AR mode toggle with availability gating`
