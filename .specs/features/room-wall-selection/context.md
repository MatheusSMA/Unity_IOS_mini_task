# Room Wall Selection Context

**Gathered:** 2026-08-17
**Spec:** `.specs/features/room-wall-selection/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Unity iOS app showing a synthetic 3D room; the user orbits it first-person by touch drag, toggles wall selection by tap (outline + tint), reads state in a collapsible real-time list, clears via button or tap-outside, cuts real window holes by dragging rectangles, and can switch to AR-pose camera and an interactive 2D top-down plan.

---

## Implementation Decisions

### Room source & scope

- Synthetic 3D room is the MVP; AR is an extra that only drives the camera (no real-wall detection)
- All three extras are in scope: window holes, AR look-around, 2D top-down view
- Room is rectangular (4 walls) but generation code is generic for N walls

### Platform & stack

- Unity 6 (6000.x) + URP
- Validation target: Unity Editor only (no device build required)
- Input System (new) + EnhancedTouch
- uGUI (Canvas) + TextMeshPro for UI
- Architecture: light MVC/MVP with plain C# events, no DI framework
- Tests: EditMode (pure logic) + PlayMode (interaction)

### Camera

- First-person camera inside the room
- Free 360 deg horizontal orbit, clamped pitch, no zoom

### Selection & feedback

- Tap toggles selection; feedback is outline + tint together
- Clear selection: dedicated button AND tap on empty space
- Wall list: collapsible panel, updated in real time

### Window holes

- Drawn by dragging two corners on the wall with real-time preview
- Real procedural mesh cut (see-through hole)
- Multiple windows per wall; no overlap; clamped to wall bounds; minimum size enforced

### AR & top-down extras

- AR mode: device pose (AR Foundation device tracking) drives the synthetic-room camera
- Top-down: interactive orthographic plan; tapping walls in the plan toggles selection

### Agent's Discretion

- Docs and UI language (user: no preference) → English, consistent with English commit convention
- Concrete tuning values (pitch clamp range, tap threshold, room dimensions, minimum window size) → logged as assumptions in spec

### Declined / Undiscussed Gray Areas → Assumptions

All gray areas raised were answered. Remaining defaults (tuning values, wall naming, XR Simulation for Editor AR validation, axis-aligned windows) are recorded in the spec's Assumptions & Open Questions table.

---

## Specific References

ideia.md suggested "possibly an outline on the wall" — user chose outline + tint. No other product references.

---

## Deferred Ideas

- Window editing/deletion after placement
- LiDAR real-wall detection mode
