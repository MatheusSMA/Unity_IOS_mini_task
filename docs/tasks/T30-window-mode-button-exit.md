# T30 — Settle the window mode button's exit affordance + PlayMode test

**Phase**: 6 (P4 follow-up) | **Requirement**: WIN-01 (AC1) | **Depends on**: none | **Tests**: PlayMode | **Gate**: full
**Decided**: AD-019 and AD-021 (2026-08-18). **Done** — implemented 2026-08-18.

## Functional description

The button stops appearing and disappearing. It stays on screen and shows its state where the art kit shows it — the palette plus the state dot:

- **Enabled** while a Wall is selected and the mode is Orbit.
- **Disabled** otherwise — nothing selected, Floor or Ceiling selected, AR or 2D mode. Art kit disabled palette: border `E9FFF21A`, fill `E9FFF205`, label `4C6558`.
- **Active** while window mode is running: the dot lit in the accent `35F08AFF`, glow child enabled.

That supersedes AD-015's visibility rule, and it settles a disagreement that had been sitting in three artefacts at once: the code hid the button (`Refresh()` calls `SetActive(false)` the moment the mode leaves Orbit), so the exit branch in `OnClick` was unreachable, while T22 and the class comment both called that branch the exit and the art kit shipped a disabled-but-visible state. A button that vanishes also left no visible way out of window mode — the only exits were completing a drag or switching views.

**Settled by AD-021:** clicking the button while window mode is active exits back to Orbit. The enabled set is therefore `Wall selected AND (mode == Orbit OR mode == WindowDraw)`, which keeps the `OnClick` branch reachable instead of dead, and gives window mode the visible exit AD-019 was after. A disabled button does nothing when pressed.

## Technical description

- **File**: `Assets/Scripts/UI/WindowModeButton.cs` — `Refresh()` computes `IsVisible` and calls `SetActive`; `OnClick()` holds the `Current == Mode.WindowDraw ? Orbit : WindowDraw` toggle.
- Tests: `Assets/Tests/PlayMode/WindowModeButtonTests.cs` — the four visibility cases from T22 stay; one case is added for mode == WindowDraw asserting the chosen behaviour.
- `Refresh()` drives `Button.interactable` instead of `SetActive`; the GameObject stays active for the whole session.
- Docs to reconcile: `docs/tasks/T22-window-mode-button.md`, the XML comment on the class, and `design.md`. `spec.md` WIN-01 AC1 and AD-015 are already updated — AC1 now describes this behaviour and WIN-01 sits back at `In Tasks` until this task lands.

## Tests (PlayMode)

The four cases from T22 become enabled/disabled assertions instead of visible/hidden: Wall + Orbit → enabled; Floor selected → disabled; nothing selected → disabled; TopDown with Wall selected → disabled. In every case the GameObject stays active. New case: mode == WindowDraw with the same Wall selected → the active state (dot lit).

## Done when

- [x] `Refresh()` drives `interactable`, not `SetActive`; the button is never removed from the screen
- [x] Code, tests, T22, the class comment and `design.md` all describe the same behaviour
- [x] No unreachable branch left in `WindowModeButton.OnClick`
- [x] WIN-01 AC1 returns to `Verified` in the spec's traceability table
- [x] `run_tests` EditMode + PlayMode green

**Tools**: unity-mcp (run_tests) | **Commit**: `[fix] align window mode button exit behaviour`
