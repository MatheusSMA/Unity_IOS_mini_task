# Room Wall Selection Context

**Gathered:** 2026-08-17
**Spec:** `.specs/features/room-wall-selection/spec.md`
**Status:** Ready for design

---

## Feature Boundary

Unity iOS app showing a synthetic 3D room; the user orbits it first-person by touch drag, selects exactly one surface at a time by tap (tint feedback; outline is P2 polish), reads state in a collapsible real-time list, clears only via the Clear button, cuts real window holes by dragging rectangles (and deletes them via X + confirmation), and can switch to AR-pose camera and an interactive 2D top-down plan.

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

- Single selection: tap selects (re-tap keeps selected, no toggle-off); feedback P1 = tint, P2 = outline (AD-007/AD-010)
- Clear selection: Clear button is the ONLY path to empty (tap-outside removed by 2026-08-17 revision)
- Surface list (walls, floor, ceiling): collapsible panel, updated in real time

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

- Window editing (move/resize) after placement — deletion promoted into scope by the 2026-08-17 revision (WIN-04)
- LiDAR real-wall detection mode

---

## Revision 2026-08-17

User revision request applied to spec/design: single selection (Clear button only path to empty), Wall → Surface with selectable floor/ceiling (windows Wall-only), solid slabs with 0.15 m thickness + reveal faces, window max size + 0.1 m edge margin, window mode button gated to selected Wall, window deletion (X + confirmation), collider sync on every rebuild, "2D | 3D" view buttons with state cancel on 2D entry, camera containment scoped to Orbit, tint as P1 feedback with outline demoted to P2 (stencil two-pass), P0 bootstrap phase (Unity MCP with AD-006 fallback), mode transition matrix. Decisions recorded as AD-006..AD-013 in `.specs/STATE.md`.

---

## Grill 2026-08-17

Adversarial spec review resolved with the user: taps swallowed in WindowDraw (target wall locked); window mode button gated to Wall selected AND Orbit mode; window opening colliders identified by `WindowView` component (no layer) — window hit routes to deletion UI, never selection; 2D plan gets fit-to-room framing, 30 px wall-tap tolerance and pinch zoom 0.5x–2.0x (2D only; 3D stays zoom-free); camera eye height 1.6 m tunable; stale selection-model text in this file corrected. Decisions AD-014..AD-016 in `.specs/STATE.md`.
