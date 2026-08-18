# T12 — Implement RoomBuilder + EditMode tests

**Phase**: 2 (Domain core) | **Requirement**: ROOM-01 | **Depends on**: T11 | **Tests**: EditMode unit | **Gate**: quick

## Functional description

Generates the room: from an ordered footprint polygon it produces N wall surfaces plus floor and ceiling, all solid slabs, named for the list panel ("Wall 1".."Wall N", "Floor", "Ceiling"). The shipped scene uses a 9 m x 5 m rectangle (6 x 4 until B6, 2026-08-18), but the algorithm is generic for any N >= 3 (ROOM-01 AC1).

## Technical description

- **File**: `Assets/Scripts/Domain/RoomBuilder.cs` (plain C#).
- **Interface**: `static RoomDefinition Build(IReadOnlyList<Vector2> footprint, float height, float thickness)`.
- Per footprint edge i→i+1 (wrapping): one `SurfaceDefinition` of kind Wall — origin at the edge start, `right` along the edge, `up` = world up, width = edge length, height = room height, inward-facing normal.
- Floor: kind Floor spanning the footprint (rectangular footprint → simple quad basis; generic N uses the bounding basis, mesh cut handled by T11 only for walls — floor/ceiling never take windows, AD-008).
- Ceiling: kind Ceiling at `height`, normal facing down.
- Names by generation order (spec assumption row); ids sequential.
- Defaults used by bootstrap: 9 x 5 m footprint (6 x 4 until B6), 2.8 m height, 0.15 m thickness.

## Tests (EditMode)

N=4 → 6 surfaces (4 Wall + Floor + Ceiling); N=3 and N=5 → N+2; names/kinds/order correct; wall width equals edge length. >= 4 tests.

## Done when

- [ ] Generic N >= 3 generation with correct kinds and names
- [ ] `run_tests` EditMode green, >= 4 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add room builder for N-wall footprints`
