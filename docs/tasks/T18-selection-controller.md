# T18 — Implement SelectionController + PlayMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: SEL-01, SEL-03 | **Depends on**: T14, T16 | **Tests**: PlayMode | **Gate**: full

## Functional description

The core interaction: tap any surface to select it. Misses never clear (the Clear button is the only path to empty), window openings route to the deletion UI instead of the selection, and while drawing a window all taps are swallowed (the target wall is locked).

## Technical description

- **File**: `Assets/Scripts/Presentation/SelectionController.cs`; `OnTap(Vector2 screenPos)` wired to InputRouter.Tapped.
- `Physics.Raycast` from the active camera. **Mask includes the default surface layer AND `SelectedSurface`** from day one — otherwise the P2 outline layer swap (T24) makes the selected surface unhittable (AD-010/OUT-01 AC2).
- Hit resolution order:
  1. `ModeManager.Current == WindowDraw` → swallow, nothing dispatched (AD-015, WIN AC13).
  2. Hit collider has a `WindowView` component → route to that WindowView (deletion UI), **never** touch selection (AD-014, SEL AC7). Component check, no dedicated layer.
  3. Hit a surface (wall/floor/ceiling) → `RoomModel.Select(id)` (SEL AC1-AC2).
  4. Miss — empty space or ray through an opening → selection unchanged (SEL AC6). No tap-to-clear path (CLR-02 retired).
- 2D wall-tap tolerance is added later by T28 (kept out of P1 scope).

## Tests (PlayMode, simulated taps via InputTestFixture)

Tap wall selects; tap second wall moves selection in one event; tap selected wall → no event; tap empty space → unchanged; WindowDraw mode active (stubbed) → tap swallowed. >= 5 tests.

## Done when

- [ ] Resolution order exact; mask includes both layers
- [ ] `run_tests` EditMode + PlayMode green, >= 5 PlayMode tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add tap selection controller`
