# T17 — Implement OrbitCameraController + PlayMode tests

**Phase**: 3 (P1 Presentation) | **Requirement**: CAM-01, CAM-02 | **Depends on**: T14, T16 | **Tests**: PlayMode | **Gate**: full

## Functional description

Looking around: one-finger drag rotates the first-person camera — full 360° horizontally, pitch clamped so the user can't flip through floor or ceiling. Mouse drag works in the Editor (the validation target). No zoom (out of scope for 3D).

## Technical description

- **File**: `Assets/Scripts/Presentation/OrbitCameraController.cs`, driving the camera rig transform.
- Position: room centre at **1.6 m eye height** (serialized — spec assumption, confirmed); kept inside the room **while in Orbit mode only** (AD-012 scoped the constraint).
- `OnDrag(Vector2 delta)` from InputRouter: horizontal → yaw (unclamped, wraps); vertical → pitch clamped to serialized range (-60°..+60° default).
- `SetRotation(float yaw, float pitch)` — AR → touch handoff entry (AR-01 AC3, no snap).
- `ResetToRoomCentre()` — TopDown → 3D return (TOP AC5).
- Sensitivity serialized; Editor mouse arrives via InputRouter's TouchSimulation, no extra code here.

## Tests (PlayMode)

Simulated horizontal drag past 360° → yaw wraps, no clamp; vertical drag beyond bounds → pitch stops at clamp; camera position remains inside room bounds after drags; `SetRotation` applies exactly. >= 3 tests.

## Done when

- [ ] Yaw free / pitch clamped / inside-room in Orbit; handoff APIs in place
- [ ] `run_tests` EditMode + PlayMode green, >= 3 PlayMode tests

**Tools**: unity-mcp (run_tests) | **Commit**: `[feat] add orbit camera controller`
