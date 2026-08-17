# T11 — Implement SurfaceMeshBuilder + EditMode tests

**Phase**: 2 (Domain core) | **Requirement**: WIN-02 (geometry), EDGE-05 | **Depends on**: T07 | **Tests**: EditMode unit | **Gate**: quick

## Functional description

The geometry engine: turns a surface definition plus its window rectangles into a solid slab mesh with real see-through holes — including the four reveal faces that make an opening read as a cut through a thick wall, not a texture trick (AD-009).

## Technical description

- **File**: `Assets/Scripts/Domain/SurfaceMeshBuilder.cs` (plain C#, pure function).
- **Interface**: `static MeshData Build(SurfaceDefinition surface, IReadOnlyList<Rect2D> holes)`.
- **Algorithm** (design, verbatim): slab extruded along `-normal` by `thickness`; in surface-local 2D:
  1. **Front face** — grid slicing: collect X cuts and Y cuts from hole edges, tile the face, emit tiles not inside a hole (holes are axis-aligned and non-overlapping, so decomposition is exact).
  2. **Back face** — same tiles offset `-thickness`, winding reversed.
  3. **Outer slab sides** — 4 border quads.
  4. **Reveal faces** — per hole, 4 quads joining front hole edges to back hole edges, normals facing into the opening.
  - O(n²) tiles for n windows — fine at this scale (ponytail ceiling accepted in design).
- **Output validation** (EDGE-05): non-empty, all vertices finite; on failure return null/invalid so the caller keeps the previous mesh + collider.
- Returns raw arrays (`MeshData`) — EditMode-testable with no UnityEngine Mesh object.

## Tests (EditMode, invariants over arrays)

Zero holes: front+back+4 sides, vertex count and winding correct; 1 hole: +4 reveal quads, no front/back vertex strictly inside the hole rect; 2 holes: reveals per hole; ray-style containment check: hole area has no front-face triangle; degenerate input (zero-size surface / hole outside bounds) → validation failure; triangle indices all in range. >= 6 tests.

## Done when

- [ ] Slab + holes + reveals per algorithm; output validation in place
- [ ] `run_tests` EditMode green, >= 6 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add solid slab mesh builder with window holes`
