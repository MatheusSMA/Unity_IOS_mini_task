# T09 — Implement RoomModel selection + EditMode tests

**Phase**: 2 (Domain core) | **Requirement**: SEL-01, CLR-01 | **Depends on**: T07 | **Tests**: EditMode unit | **Gate**: quick

## Functional description

The single source of truth for "which surface is selected". Enforces the core interaction contract: at most one surface selected; tapping the selected one changes nothing; Clear is idempotent. Everything visual (tint, list rows) is downstream of this class's one event.

## Technical description

- **File**: `Assets/Scripts/Domain/RoomModel.cs` (plain C#).
- **State**: `IReadOnlyList<SurfaceDefinition> Surfaces`, `int? SelectedSurfaceId` (AD-007 — no HashSet).
- **API + exact semantics (AD-007)**:
  - `Select(int id)`: selects `id`, deselecting previous in the same operation → raises `SelectionChanged(previous, current)`. Selecting the already-selected id: **no state change, no event** (SEL AC3). Unknown id: ignore (defensive).
  - `ClearSelection()`: idempotent — nothing selected → no state change, **no event** (CLR AC2). Otherwise raises `SelectionChanged(previous, null)`.
  - `event Action<int?, int?> SelectionChanged` — (previous, current); no separate ClearedAll event.

## Tests (EditMode, 1:1 to ACs)

Select from empty (null→id); select replaces previous (payload previous=old, current=new); reselect same id fires no event (event spy); clear fires (id, null); clear when empty fires nothing; at most one selected after any sequence. >= 6 tests.

## Done when

- [ ] SEL AC1-AC3 + CLR AC1-AC2 exact, no-op paths raise no event
- [ ] `run_tests` EditMode green, >= 6 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add RoomModel single selection logic`
