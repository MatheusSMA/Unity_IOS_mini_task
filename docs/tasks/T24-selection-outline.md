# T24 — Implement selection outline (stencil two-pass) + PlayMode test

**Phase**: 4 (P2 polish) | **Requirement**: OUT-01 | **Depends on**: none intra-phase (cross-phase: T02, T15, T18) | **Tests**: PlayMode | **Gate**: full

## Functional description

Visual polish on top of the tint: the selected surface gets an outline so selection reads clearly on any material. Demoted from P1 because it needs a real render technique — a plain RenderObjects layer pass produces no edge (AD-010's core finding).

## Technical description

- **Files**: `Assets/Settings/` renderer asset (two features) + `Assets/Scripts/Presentation/SurfaceView.cs` (modify: layer swap).
- Add a `SelectedSurface` layer (TagManager).
- **Two RenderObjects features on the T02 renderer** (design Integration Points):
  1. Stencil mark pass: renders the `SelectedSurface` layer writing a stencil ref.
  2. Edge pass: edge material drawn where stencil differs (slightly inflated / edge shader), producing the visible outline.
- SurfaceView: while selected, swap the GameObject to `SelectedSurface` layer; restore on deselect.
- **Raycast guard (OUT-01 AC2)**: SelectionController's mask already includes default + `SelectedSurface` (T18 built it that way) — this task verifies it with a test, because forgetting it makes the selected surface untappable (design risk table).

## Tests (PlayMode)

Selected surface: layer swapped, restored on deselect; re-tap the selected surface → still hittable, stays selected, no event (OUT AC2 + SEL AC3 combined). >= 2 tests. Outline visuals themselves: human eye check in Editor (render output isn't unit-assertable; noted for UAT).

## Done when

- [ ] Outline + tint visible on selection from any angle (Editor check)
- [ ] `run_tests` EditMode + PlayMode green, >= 2 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (renderer config), context7 (RenderObjects stencil API) | **Commit**: `[feat] add stencil outline for selected surface`
