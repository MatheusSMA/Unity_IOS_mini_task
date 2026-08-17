# T08 — Implement WindowPlacementValidator + EditMode tests

**Phase**: 2 (Domain core) | **Requirement**: WIN-03 | **Depends on**: T07 | **Tests**: EditMode unit | **Gate**: quick

## Functional description

The single gatekeeper for window placement. Every rectangle the user draws goes through it; every invalid one (overlapping, tiny, oversized, edge-hugging, on a non-wall) is rejected with a named reason, so the UI can drop the preview and the wall stays untouched.

## Technical description

- **File**: `Assets/Scripts/Domain/WindowPlacementValidator.cs` (plain C#).
- **Interface**: `ValidationResult Validate(SurfaceDefinition surface, IReadOnlyList<Rect2D> existing, Rect2D candidate)` → clamped rect or `WindowRejection` reason.
- Rules, in order (all serialized defaults, spec Assumptions table):
  1. `surface.kind != Wall` → `InvalidSurfaceKind` (AD-008).
  2. Clamp candidate into wall bounds minus edge margin 0.1 m on all sides (WIN AC6).
  3. Clamped size < 0.2 x 0.2 m → `TooSmall` (AC8, also covers EDGE-04 tap-like drags).
  4. Size > 2.0 x 2.0 m → `TooLarge` (AC8).
  5. Margin violation after clamp → `MarginViolation` (AC9).
  6. Axis-aligned overlap with any existing rect on the same wall → `Overlap` (AC7).
- Pure function, no state, no UnityEngine calls.

## Tests (EditMode, 1:1 to ACs)

Valid rect accepted + clamped; each rejection kind (Overlap, TooSmall, TooLarge, MarginViolation, InvalidSurfaceKind for Floor and Ceiling); boundary cases: exactly-min size, exactly-at-margin. >= 8 tests.

## Done when

- [ ] All rules implemented in the stated order
- [ ] `run_tests` EditMode green, >= 8 tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add window placement validator`
