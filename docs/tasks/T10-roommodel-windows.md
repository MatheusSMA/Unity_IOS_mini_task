# T10 — Add RoomModel window operations + EditMode tests

**Phase**: 2 (Domain core) | **Requirement**: WIN-02/WIN-04 (model side), EDGE-05 | **Depends on**: T08, T09 | **Tests**: EditMode unit | **Gate**: quick

## Functional description

Windows become real state: adding a window (validator-gated, walls only) and removing one by id, with events the mesh layer listens to. Also the rollback contract: if a mesh rebuild fails downstream, the model entry is rolled back so state never diverges from geometry (EDGE-05).

## Technical description

- **File**: `Assets/Scripts/Domain/RoomModel.cs` (modify — same class as T09).
- **State**: `Dictionary<int, List<WindowSpec>>` keyed by wall surface id; monotonic window id counter (deletion needs stable ids, WIN-04).
- **API**:
  - `bool TryAddWindow(int surfaceId, Rect2D rect, out WindowRejection reason)` — routes through `WindowPlacementValidator` (T08); non-Wall → `InvalidSurfaceKind`; success stores the **clamped** rect and raises `WindowAdded(WindowSpec)`.
  - `bool TryRemoveWindow(int windowId)` — removes, raises `WindowRemoved(WindowSpec)`; unknown id → false, no event.
  - Rollback hook for EDGE-05: if the caller reports rebuild failure for an add/remove, the model reverts that entry (design Error Handling: "model rolls back the window entry").
- Events: `event Action<WindowSpec> WindowAdded / WindowRemoved`.

## Tests (EditMode)

Add valid → stored + event with clamped rect; each rejection kind propagated with no state change; remove existing → event; remove unknown id → false, no event; multiple non-overlapping windows on one wall (WIN AC10); rollback restores previous window list. >= 7 tests.

## Done when

- [ ] Validator-gated add, id-based remove, events, rollback
- [ ] `run_tests` EditMode green, >= 7 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add window operations to RoomModel`
