# T13 — Implement ModeManager + EditMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: WIN-01 (gating), TOP-02 (cancel), AR-01 (session end) | **Depends on**: T07 | **Tests**: EditMode unit (pure logic) | **Gate**: quick

## Functional description

The traffic controller of the app: exactly one mode owns input at a time (Orbit / WindowDraw / Ar / TopDown). Encodes which mode switches are legal and what each switch cancels — the rules that prevent a dead window button in AR, a draw surviving into 2D, or WindowDraw starting without a selected wall.

## Technical description

- **File**: `Assets/Scripts/Presentation/ModeManager.cs`. Transition logic is pure C# (EditMode-testable); MonoBehaviour shell only holds references.
- **Interface**: `Mode Current`, `bool TrySet(Mode m)`, `event Action<Mode, Mode> ModeChanged`.
- **Transition matrix (AD-013)** — `TrySet` returns false on illegal, no state change:

| From \ To | Orbit | WindowDraw | Ar | TopDown |
| --------- | ----- | ---------- | -- | ------- |
| Orbit | — | only if selected surface is a Wall | legal (starts AR session) | legal; clears selection |
| WindowDraw | legal; cancels draw | — | illegal | legal; cancels draw + clears selection |
| Ar | legal; yaw/pitch handoff | illegal | — | legal; ends AR session, clears selection |
| TopDown | legal; camera to centre, ceiling restored | illegal | illegal | — |

- Side effects exposed as events/callbacks (`DrawCancelRequested`, `SelectionClearRequested`, `ArSessionEndRequested`, ...) — ModeManager depends on nothing (design: "others depend on it"); the Wall-selected predicate for WindowDraw entry is injected (`Func<bool>`).

## Tests (EditMode)

One test per non-diagonal matrix cell (12) asserting legal/illegal + fired side-effect events; WindowDraw entry with predicate false → rejected. >= 13 tests.

## Done when

- [ ] Every matrix cell behaves as documented
- [ ] `run_tests` EditMode green, >= 13 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add mode state machine with transition matrix`
