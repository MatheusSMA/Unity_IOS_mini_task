# T25 — Implement ArPoseCameraController + PlayMode tests

**Phase**: 5 (P3 AR + 2D) | **Requirement**: AR-01 | **Depends on**: none intra-phase (cross-phase: T13, T16, T17) | **Tests**: PlayMode | **Gate**: full

## Functional description

Natural camera control: in AR mode the user looks around the synthetic room by moving the phone — device pose drives the camera. Touch-drag camera input is ignored; tapping surfaces still selects. Leaving AR hands the current orientation back to the orbit camera with no snap. The synthetic room stays the room source — AR only drives the pose (Out of Scope: plane detection).

## Technical description

- **File**: `Assets/Scripts/Presentation/ArPoseCameraController.cs`; scene gains ARSession + XROrigin (Unity MCP wiring).
- Active while `ModeManager.Current == Ar` (entered only from Orbit, AD-013; session started on entry, ended on exit/TopDown).
- **Rotation**: always from device pose. **Position**: pose position clamped to the room interior bounds (design risk: AR pose can walk the camera through walls).
- While active: InputRouter routes no drags to the camera (AR AC2); Tapped still flows to SelectionController (AC5).
- **Exit handoff (AC3)**: extract yaw/pitch from the final pose → `OrbitCameraController.SetRotation(yaw, pitch)`.
- Editor validation via **XR Simulation** (T03 enabled it); tests drive the rig transform through a pose-provider seam instead of real tracking.

## Tests (PlayMode)

Injected pose → rig rotation matches; pose position outside room → clamped inside; exit → OrbitCameraController orientation equals last pose yaw/pitch. >= 3 tests.

## Done when

- [ ] Pose-driven rotation, clamped position, no-snap handoff; taps still select in AR
- [ ] `run_tests` EditMode + PlayMode green, >= 3 PlayMode tests

**Tools**: unity-mcp + unity-mcp-skill (XROrigin), context7 (AR Foundation 6 API) | **Commit**: `[feat] add AR pose camera controller`
