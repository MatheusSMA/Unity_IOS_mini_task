# T19 — Implement SurfaceListPanel + PlayMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: LIST-01, LIST-02, EDGE-06 | **Depends on**: T16 | **Tests**: PlayMode | **Gate**: full

## Functional description

The state readout: a collapsible on-screen panel listing every surface by name with its selection state, always in sync with the 3D view — the user audits the selection without orbiting. Collapsed, it still tracks state; the collapse control never disappears.

## Technical description

- **File**: `Assets/Scripts/UI/SurfaceListPanel.cs`; creates/owns the uGUI Canvas + TMP rows (Canvas is this task's deliverable — later buttons attach to it).
- One row per surface (walls, floor, ceiling) by RoomModel order: name + state indicator.
- Subscribes `SelectionChanged(previous, current)`: updates **exactly the two affected rows in one handler call** (LIST AC2 — the reason the event carries both ids, AD-007).
- Rows update even while the panel is collapsed (EDGE-06) — collapse hides layout, not the data binding.
- Collapse control: toggle button that stays visible when collapsed (LIST AC4).
- Panel blocks scene raycasts by existing under the EventSystem (EDGE-02 handled in InputRouter).

## Tests (PlayMode)

Select A → row A on; select B → exactly rows A and B changed; Clear → selected row off; collapse, change selection, expand → rows correct. >= 4 tests.

## Done when

- [ ] 6 rows live-sync; collapse/expand works; collapsed rows stay correct
- [ ] `run_tests` EditMode + PlayMode green, >= 4 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (UI hierarchy, run_tests) | **Commit**: `[feat] add real-time surface list panel`
