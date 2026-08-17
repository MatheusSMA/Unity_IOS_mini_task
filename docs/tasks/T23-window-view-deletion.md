# T23 — Implement WindowView deletion flow + PlayMode tests

**Phase**: 4 (P2 Windows) | **Requirement**: WIN-04, SEL-03 (AC7) | **Depends on**: none intra-phase (cross-phase: T10, T15, T18) | **Tests**: PlayMode | **Gate**: full

## Functional description

Undoing a bad window: tap the opening → an "X" appears at its top-right corner → tap X → confirmation popup → confirm removes the window and the wall turns solid again; cancel leaves everything untouched. Tapping an opening never moves the selection.

## Technical description

- **File**: `Assets/Scripts/Presentation/WindowView.cs`; one GameObject per window, created on `WindowAdded`, destroyed on `WindowRemoved`.
- **Collider**: BoxCollider sized rect x wall thickness, positioned in the opening (WIN-04 AC1) — this is what makes an empty hole tappable.
- **Identification (AD-014)**: no dedicated layer; SelectionController (T18) detects the `WindowView` component on the hit and routes the tap here — selection never changes (SEL AC7).
- **Flow**: routed tap → show world-anchored uGUI "X" at the opening's top-right (AC2) → X click → confirmation popup (shared prefab, AC3) → confirm → `RoomModel.TryRemoveWindow(id)` → T15 rebuilds mesh + collider in the same operation (AC4); cancel → close popup, no change (AC5).
- Popup is uGUI → `IsPointerOverGameObject` already shields the scene from its taps (EDGE-02).

## Tests (PlayMode)

Add window via model → WindowView exists with collider; tap opening → X shown AND selection unchanged; X → popup; cancel → window stays; confirm → window gone, ray through former opening hits wall; WindowRemoved destroys the view. >= 6 tests.

## Done when

- [ ] WIN-04 AC1-AC5 + SEL AC7 end-to-end
- [ ] `run_tests` EditMode + PlayMode green, >= 6 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (popup prefab, run_tests) | **Commit**: `[feat] add window deletion with confirmation`
