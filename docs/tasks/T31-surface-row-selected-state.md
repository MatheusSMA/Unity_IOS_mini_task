# T31 — Expose the surface row's selected state as a field + update PlayMode tests

**Phase**: 6 (P4 follow-up) | **Requirement**: HUD-01 (AC3) | **Depends on**: none | **Tests**: PlayMode | **Gate**: full

## Functional description

Nothing the user sees changes. This is the seam that makes the visual pass safe.

`SurfaceListPanel` currently marks the selected row by appending `"  [SELECTED]"` to the row's label text, and four PlayMode tests read that string to decide whether selection worked. The art kit does not render selection as label text — it renders a 2 px mark on the left of the row plus a separate green "SELECTED" tag. Applying the kit therefore deletes the suffix, and with it the only thing those four tests look at. Doing the refactor first means T32 restyles the row without touching a test; doing it second means rewriting tests in the middle of a visual change, which is exactly when a broken assertion gets "fixed" by weakening it.

`validation.md` section 8 flagged this as residual risk when the kit was imported.

## Technical description

- **File**: `Assets/Scripts/UI/SurfaceListPanel.cs` — the row build/update path that composes the label.
- Expose the state on the row itself: a `SurfaceRow.IsSelected` property (or an equivalent dedicated field the tests can read) set from the same `SelectionChanged (previous, current)` handler that updates the two affected rows today.
- The label keeps only the surface name. Whether the suffix stays visible in the meantime is cosmetic and irrelevant to the tests once they read the field.
- Tests: `Assets/Tests/PlayMode/SurfaceListPanelTests.cs` — the four assertions that parse the label read the field instead.

## Tests (PlayMode)

Same coverage, new sensor: selection moves → exactly two rows change state; collapsed panel still tracks selection (EDGE-06); clear empties the state; re-tap keeps it. Each asserts the field, not the string.

## Done when

- [ ] Selected state readable without parsing label text; label text no longer the source of truth for selection
- [ ] The four suffix-reading tests assert the new field; LIST-01, LIST-02 and EDGE-06 coverage unchanged
- [ ] `run_tests` EditMode + PlayMode green (152/152 baseline holds)

**Tools**: unity-mcp (run_tests) | **Commit**: `[refactor] expose surface row selection as state`
