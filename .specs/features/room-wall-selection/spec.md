# Room Wall Selection Specification

## Problem Statement

Users need a Unity iOS app that displays a 3D room and lets them select surfaces (walls, floor, ceiling) by tapping, with clear visual feedback and a real-time list of surface states. The app also supports cutting and deleting window holes in walls, an AR-driven camera mode, and an interactive 2D top-down plan. This is the core deliverable of the Unity_IOS_mini_task project.

## Goals

- [ ] Unity 6 + URP project bootstrapped, packages pinned, tests runnable, iOS-exportable (P0)
- [ ] User can view a synthetic 3D room from a first-person camera and orbit it via touch drag
- [ ] User can select exactly one surface at a time by tapping, with tint feedback, mirrored in a real-time list
- [ ] User can clear the selection via the Clear button (the only path to the empty state)
- [ ] User can cut rectangular window holes into walls by dragging two corners (real mesh cut through a solid wall), and delete them via an X + confirmation
- [ ] User can switch to an AR mode where device pose drives the camera, and between 3D and 2D top-down views
- [ ] HUD is built from the `Room Scanner HUD` art kit instead of placeholder boxes (P4)

## Out of Scope

Explicitly excluded. Documented to prevent scope creep.

| Feature | Reason |
| ------- | ------ |
| Real-wall detection via LiDAR/ARKit plane detection | User decision: synthetic room is the room source; AR extra only drives the camera pose |
| iOS device build validation | User decision: validation target is the Unity Editor only; project stays iOS-exportable but no device build is required |
| Camera zoom (pinch) in the 3D orbital camera | User decision: first-person orbital camera without zoom. Pinch zoom in the 2D plan IS in scope (2026-08-17 grill revision) |
| Window editing (move/resize) after placement | Deletion is in scope (WIN-04); moving/resizing a placed window is not |
| Persistence of selection/windows between sessions | Not in ideia.md; runtime state only |
| Multi-room support | Single room per ideia.md |

---

## Assumptions & Open Questions

Every ambiguity is resolved or recorded here - nothing is left silently unclear.

| Assumption / decision | Chosen default | Rationale | Confirmed? |
| --------------------- | --------------- | --------- | ---------- |
| Docs and UI language | English | User expressed no preference; consistent with English commit convention and English README deliverable | y |
| Camera pitch clamp values | -60 deg to +60 deg, tunable via serialized field | Prevents flipping through floor/ceiling; exact bounds are a tuning knob | n |
| Tap vs drag discrimination | Movement under 20 px (DPI-scaled) and under 300 ms counts as tap; otherwise drag | Standard mobile heuristic; prevents orbit gestures from moving the selection | n |
| Room dimensions | 6 m x 4 m footprint, 2.8 m wall height | Typical room scale; exact dimensions are cosmetic and tunable | n |
| Surface thickness | 0.15 m default, serialized per surface | Typical interior wall; makes window reveals visible | n |
| Minimum window size | 0.2 m x 0.2 m | Prevents accidental micro-windows from a tap-like drag | n |
| Maximum window size | 2.0 m x 2.0 m, tunable | Keeps openings structurally plausible; revision request asked for a max without fixing a value | n |
| Window edge margin | 0.1 m minimum from every wall edge | Prevents degenerate slivers of wall around an opening | n |
| Surface naming | "Wall 1".."Wall N", "Floor", "Ceiling", ordered by generation order | Simple, generic for N walls | n |
| AR mode validation in Editor | Use AR Foundation XR Simulation to validate AR pose mode in Editor | Validation target is Editor-only; XR Simulation is the supported Editor path | n |
| Window rectangle plane | Rectangles are axis-aligned in wall-local space (no rotated windows) | Matches "draw a rectangle" in ideia.md; simplest predictable UX | n |
| Camera eye height | Room centre, 1.6 m eye height, serialized | Typical standing eye height; tunable | y |
| 2D wall-tap tolerance | 30 px screen-space, tunable | Walls are ~0.15 m edge-on in plan (~10 px); floor hit within tolerance of a wall selects the wall | y |
| 2D pinch zoom limits | 0.5x–2.0x of fit-to-room framing, tunable | Room stays legible; user confirmed zoom in 2D only | y |
| Window mode button availability | Always on screen; disabled palette + state dot instead of hidden (AD-019) | Owner decision 2026-08-18 following the art kit, which draws an active / not-active dot on the button. Code still hides it until T30 lands | y |
| HUD orientation | Landscape, phone held horizontally (AD-020) | Owner decision 2026-08-18. The kit is authored landscape so its layout transfers directly; the reference resolution itself is not pinned, only fidelity to the kit's art | y |

**Open questions:** none. The tuning rows marked `Confirmed? n` are defaults awaiting explicit user confirmation. They are not blocking: the stated default applies until the user overrides it, and every one is a serialized/tunable value. The two Phase 6 product questions were answered on 2026-08-18 and are logged as AD-019 and AD-020 in `.specs/STATE.md`; both are recorded in the rows above and neither is implemented yet.

---

## User Stories

### P0: Project bootstrap ⭐ (environment, not a user story)

**Purpose**: A working Unity project must exist before any P1 work. Acceptance criteria are verifiable environment facts, not user behavior.

**Execution note**: This phase assumes the Unity MCP is active for Editor-side operations. Fallback if the MCP is unavailable is defined in AD-006 (`.specs/STATE.md`): the agent writes all text assets (manifest, ProjectSettings, scene YAML, scripts) and the human performs the Editor verification steps manually; the phase does not block.

**Acceptance Criteria** (BOOT-01):

1. A Unity 6 (6000.x) project SHALL exist at `Build/FormifyTest` with URP installed, a URP asset created and assigned in Graphics and Quality settings, and the quality level in use SHALL be the one whose renderer carries the OUT-01 outline passes. <!-- ubiquitous -->
2. The following packages SHALL be installed at pinned versions: com.unity.inputsystem, com.unity.xr.arfoundation 6.x, com.unity.xr.arkit 6.x (matching), com.unity.test-framework, TextMeshPro essentials. Editor-only tooling outside this list is not covered by the pinning rule. <!-- ubiquitous -->
3. A bootstrap scene SHALL open in the Editor without console errors. <!-- ubiquitous -->
4. The Test Runner SHALL run green with zero tests in both EditMode and PlayMode assemblies. <!-- ubiquitous -->
5. The project SHALL switch to the iOS build target without build-settings errors. <!-- ubiquitous -->

**Independent Test**: Open the project in Unity 6; console clean; Test Runner green; File > Build Settings > iOS switches without errors.

---

### P1: Orbit a synthetic room ⭐ MVP

**User Story**: As a user, I want to see a 3D room and look around it by dragging my finger so that I can view every surface.

**Why P1**: Without the room and camera nothing else is usable.

**Acceptance Criteria** (each line is one EARS pattern):

1. The system SHALL render a synthetic rectangular room composed of surfaces — N >= 3 walls plus floor and ceiling — each generated as a solid slab with configurable thickness (0.15 m default), by code that supports any number N >= 3 of walls. <!-- ubiquitous -->
2. WHEN the user drags one finger horizontally THEN the system SHALL rotate the first-person camera yaw proportionally to the drag, with full 360 deg freedom. <!-- event-driven -->
3. WHEN the user drags one finger vertically THEN the system SHALL rotate the camera pitch proportionally to the drag, clamped to the configured pitch range (-60 deg to +60 deg default). <!-- event-driven -->
4. WHILE in Orbit mode the system SHALL keep the camera positioned inside the room (first-person viewpoint). <!-- state-driven -->
5. WHILE running in the Unity Editor the system SHALL accept mouse drag as the touch-drag equivalent. <!-- state-driven -->

**Independent Test**: Open the scene in Play Mode, drag with mouse; camera orbits 360 deg horizontally and clamps vertically; all 4 walls, floor and ceiling become visible by orbiting.

---

### P1: Select a surface by tapping ⭐ MVP

**User Story**: As a user, I want to tap any surface (wall, floor or ceiling) to select it with clear visual feedback so that I always know which surface is selected.

**Why P1**: Surface selection is the core interaction of the app.

**Acceptance Criteria**:

1. WHEN the user taps a surface that is not selected THEN the system SHALL select that surface and, in the same operation, deselect the previously selected surface if one exists. <!-- event-driven -->
2. The system SHALL allow at most one surface to be selected at any time. <!-- ubiquitous -->
3. WHEN the user taps the surface that is already selected THEN the system SHALL keep it selected, changing no state and raising no event. <!-- event-driven -->
4. WHILE a surface is selected the system SHALL render it with a tint (MaterialPropertyBlock) visible from any camera angle. <!-- state-driven -->
5. IF a touch moves more than the tap threshold or lasts longer than the tap duration THEN the system SHALL treat it as a camera drag and SHALL NOT change the selection. <!-- unwanted-behavior -->
6. IF a tap ray passes through a window opening and hits no surface THEN the system SHALL leave the selection unchanged. <!-- unwanted-behavior -->
7. IF a tap hits a window opening's collider THEN the system SHALL NOT change the selection and SHALL route the tap to the window deletion UI (WIN-04). <!-- unwanted-behavior -->

**Independent Test**: Tap Wall 1 (selected + tinted), tap Wall 2 (Wall 2 selected, Wall 1 deselected in the same frame), tap Wall 2 again (still selected, no flicker), tap floor (floor selected, Wall 2 deselected); drag over a surface never changes selection.

---

### P1: Real-time surface list ⭐ MVP

**User Story**: As a user, I want an on-screen list of every surface and its selection state so that I can audit the selection without orbiting.

**Why P1**: Explicit requirement in ideia.md; the list is the app's state readout.

**Acceptance Criteria**:

1. The system SHALL display a collapsible panel listing every surface (walls, floor, ceiling) by name with its current selection state. <!-- ubiquitous -->
2. WHEN the selection changes THEN the system SHALL update both the previously selected row and the newly selected row within the same frame, driven by a single selection-changed notification carrying (previous, current). <!-- event-driven -->
3. WHEN the user taps the panel's collapse control THEN the system SHALL toggle the panel between expanded and collapsed. <!-- event-driven -->
4. WHILE the panel is collapsed the system SHALL keep the collapse control visible so the panel can be re-expanded. <!-- state-driven -->

**Independent Test**: Move the selection between surfaces while watching the panel; exactly two rows change per tap (old off, new on); panel collapses and expands.

---

### P1: Clear selection ⭐ MVP

**User Story**: As a user, I want to clear the selection with one action so that I can return to the empty state.

**Why P1**: Explicit requirement. The Clear button is the ONLY path to the empty state — tapping empty space does not clear.

**Acceptance Criteria**:

1. WHEN the user taps the Clear button THEN the system SHALL deselect the selected surface, remove its tint and update its list row. <!-- event-driven -->
2. IF no surface is selected THEN the system SHALL leave all state unchanged and raise no event when Clear is triggered (idempotent). <!-- unwanted-behavior -->
3. WHEN the Clear button is pressed while window mode is active THEN the system SHALL cancel any in-progress window drawing, exit window mode and deselect the surface. <!-- event-driven -->

**Independent Test**: Select a surface, press Clear (deselected); press Clear again (nothing happens, no event fires — verifiable in EditMode test).

---

### P2: Window holes by rectangle drag

**User Story**: As a user, I want to draw a rectangle on a selected wall to cut a real window hole so that I can plan window placement.

**Why P2**: Listed in ideia.md as a feature beyond the core selection loop; independent of P1 flow.

**Acceptance Criteria**:

1. The system SHALL keep the window mode button on screen at all times and SHALL enable it only WHILE the selected surface is a Wall AND the active mode is Orbit or WindowDraw; when nothing is selected, the Floor or Ceiling is selected, or the app is in AR / 2D mode the button SHALL be disabled rather than hidden, its state dot SHALL show whether window mode is active, and WHEN it is pressed while window mode is active the system SHALL return to Orbit (AD-019 and AD-021, superseding AD-015's visibility rule). A disabled button SHALL change nothing when pressed. <!-- state-driven -->
2. WHEN the user activates window mode via its button THEN the system SHALL route wall drags to window drawing instead of camera orbit. <!-- event-driven -->
3. WHILE the user drags on a wall in window mode the system SHALL display a real-time rectangle preview between the drag start corner and the current finger position, projected onto that wall. <!-- state-driven -->
4. WHEN the user releases a valid drag THEN the system SHALL cut a rectangular hole through the solid wall mesh — including the four reveal faces of the opening — so the outside is visible through it. <!-- event-driven -->
5. WHEN a wall mesh is rebuilt (window added or removed) THEN the system SHALL assign the new mesh to that wall's MeshCollider in the same operation. <!-- event-driven -->
6. The system SHALL clamp the rectangle so it lies fully inside the wall's bounds minus the minimum edge margin (0.1 m default on all sides). <!-- ubiquitous -->
7. IF the released rectangle overlaps an existing window on the same wall THEN the system SHALL reject the placement and remove the preview without modifying the wall. <!-- unwanted-behavior -->
8. IF the released rectangle is smaller than the minimum window size or larger than the maximum window size THEN the system SHALL reject the placement and remove the preview without modifying the wall. A rectangle drawn exactly on a limit SHALL be accepted anywhere on the wall: the comparison carries a 1e-4 m band, because the clamped extent is a subtraction of two world coordinates and drifts either side of the limit depending on where the window sits. <!-- unwanted-behavior -->
9. IF nothing of the released rectangle survives the AC6 clamp — it lies entirely inside the edge margin band, or the wall is too small to have an allowed region at all — THEN the system SHALL reject the placement and remove the preview without modifying the wall. A rectangle that merely crosses into the band is clamped by AC6, not rejected. <!-- unwanted-behavior -->
10. The system SHALL support multiple non-overlapping windows per wall. <!-- ubiquitous -->
11. IF a drag in window mode starts on empty space, floor or ceiling THEN the system SHALL NOT create a preview or a window. <!-- unwanted-behavior -->
12. WHEN a window exists in a wall THEN a raycast through the opening SHALL NOT hit that wall's surface mesh (the opening's own deletion collider, WIN-04, is exempt). <!-- event-driven -->
13. WHILE window mode is active surface taps SHALL NOT change the selection — the target wall is locked; selecting another surface requires exiting window mode first. <!-- state-driven -->

**Independent Test**: Select a wall (button appears), select the floor (button hides), select the wall again, enter window mode, drag (preview follows finger), release (see through the hole, reveal faces visible at an angle, ray through opening hits nothing); attempt overlapping, tiny, oversized and edge-hugging rectangles (all rejected).

---

### P2: Window deletion

**User Story**: As a user, I want to delete a placed window so that I can undo a bad placement.

**Why P2**: Promoted from Out of Scope by the 2026-08-17 revision; completes the window feature loop.

**Acceptance Criteria**:

1. The system SHALL give each window opening its own collider. <!-- ubiquitous -->
2. WHEN the user taps a window opening's collider THEN the system SHALL show an "X" button anchored to the opening's top-right corner. <!-- event-driven -->
3. WHEN the user taps the "X" button THEN the system SHALL show a confirmation popup. <!-- event-driven -->
4. WHEN the user confirms deletion THEN the system SHALL remove the window and rebuild the wall mesh and its MeshCollider in the same operation. <!-- event-driven -->
5. WHEN the user cancels the popup THEN the system SHALL close it leaving the window unchanged. <!-- event-driven -->

**Independent Test**: Cut a window, tap the opening (X appears top-right), tap X (popup), cancel (window stays), repeat and confirm (wall is solid again; ray through the former opening now hits the wall).

---

### P2: Selection outline (polish)

**User Story**: As a user, I want an outline on the selected surface in addition to the tint so that selection reads clearly on any material.

**Why P2**: Tint (SEL-02) is the guaranteed P1 feedback; outline is visual polish demoted from P1 because it needs a dedicated render technique (a RenderObjects layer pass alone does not produce an outline — see design).

**Acceptance Criteria**:

1. WHILE a surface is selected the system SHALL render an outline around it using a stencil two-pass technique (stencil mark pass + edge pass over the `SelectedSurface` layer). <!-- state-driven -->
2. WHILE the layer swap for the outline pass is active the selection raycast mask SHALL include both the default and the `SelectedSurface` layers so the selected surface remains hittable. <!-- state-driven -->

**Independent Test**: Select a surface (outline + tint both visible); tap the same selected surface (still hittable, stays selected).

---

### P3: AR pose camera mode

**User Story**: As a user, I want to look around the synthetic room by moving my iPhone so that camera control feels natural.

**Why P3**: Marked "Extra" in ideia.md; replaces touch orbit, does not add new room capability.

**Acceptance Criteria**:

1. WHEN the user enables AR mode THEN the system SHALL drive the room camera's rotation and position from the device's AR pose (AR Foundation device tracking), with position clamped to the room interior. <!-- event-driven -->
2. WHILE AR mode is active the system SHALL ignore touch-drag camera input. <!-- state-driven -->
3. WHEN the user disables AR mode THEN the system SHALL restore touch-drag orbital control from the current camera orientation. <!-- event-driven -->
4. WHERE AR tracking is unavailable on the running platform the system SHALL keep the AR mode toggle disabled with the touch camera as fallback. <!-- optional-feature -->
5. WHILE AR mode is active the system SHALL keep surface tap selection functional. <!-- state-driven -->

**Independent Test**: With XR Simulation in Editor (or a device), enable AR mode; moving the simulated device rotates the room view; tapping surfaces still moves selection.

---

### P3: 2D / 3D view switch

**User Story**: As a user, I want to switch between the 3D view and a top-down 2D plan where I can also select surfaces so that I get an overview of the project.

**Why P3**: Marked "Extra" in ideia.md.

**Acceptance Criteria**:

1. The system SHALL display two buttons at the top of the screen: "2D | 3D". <!-- ubiquitous -->
2. WHEN the user taps "2D" THEN the system SHALL switch to an orthographic top-down camera and SHALL disable both the ceiling's MeshRenderer AND its MeshCollider (renderer alone would leave an invisible collider blocking every tap). <!-- event-driven -->
3. WHEN the 2D view is entered THEN the system SHALL cancel all transient state: the current selection is cleared and any in-progress window drawing is cancelled. <!-- event-driven -->
4. WHILE the 2D view is active the system SHALL render the selected surface with the same tint state as the 3D view, and taps on surfaces in the plan SHALL move the selection (single-selection rules apply). <!-- state-driven -->
5. WHEN the user taps "3D" THEN the system SHALL restore the ceiling's MeshRenderer and MeshCollider and place the camera back at the room centre in Orbit mode. <!-- event-driven -->
6. WHILE the 2D view is active the system SHALL keep the surface list panel and Clear button functional. <!-- state-driven -->
7. WHEN the 2D view is entered THEN the orthographic camera SHALL frame the full room plus a small margin (fit-to-room; never further out). <!-- event-driven -->
8. WHEN a tap in the 2D view hits the Floor within the configured screen-space tolerance (30 px default) of a wall THEN the system SHALL select the nearest such wall instead of the floor. <!-- event-driven -->
9. WHILE the 2D view is active the system SHALL support pinch zoom within the configured limits (0.5x–2.0x of fit-to-room, default); pinch SHALL have no effect in the 3D views. <!-- state-driven -->

**Independent Test**: Select a wall, tap "2D" (selection cleared, top-down view, ceiling invisible and not blocking taps); tap a wall in the plan (selected, list updates); tap "3D" (camera at room centre, ceiling back, selection consistent).

---

### P4: HUD visual pass

**User Story**: As a user, I want the HUD to use the `Room Scanner HUD` art kit instead of placeholder grey boxes so that the app reads as a finished product.

**Why P4**: No P0-P3 acceptance criterion covers appearance. The kit landed after the feature was verified (commit `eb0124a`) and nothing is applied yet; `validation.md` section 8 records this as the known next piece of work.

**Acceptance Criteria**:

1. The system SHALL build the HUD from the imported `Room Scanner HUD` sprites, each carrying the import settings its handoff specifies (Sprite (2D and UI), Full Rect, the listed 9-slice borders, Pixels Per Unit matched to the chosen -Nx variant). <!-- ubiquitous -->
2. WHEN the HUD canvas is created THEN the system SHALL configure it for landscape (device held horizontally, AD-020) and the HUD SHALL reproduce the art kit's layout, proportions and palette; the exact reference resolution is an implementation choice, not a requirement. <!-- event-driven -->
3. WHILE a surface is selected the surface list SHALL expose the row's selected state through a dedicated field rather than a suffix appended to the row label, and the PlayMode tests SHALL assert that field. <!-- state-driven -->
4. IF a decorative image is added to the HUD (scanlines, glow, border, divider) THEN the system SHALL set its Raycast Target off so it never consumes taps. <!-- unwanted-behavior -->
5. IF the art kit's copy implies a behaviour not in this spec — the Clear button asking for confirmation — THEN the system SHALL keep the spec's behaviour (CLR-01: Clear is the only path to the empty state and clears idempotently) until a recorded decision supersedes it. The kit's other implied behaviour, the window mode button disabled instead of hidden, WAS adopted: AD-019 rewrote WIN-01 AC1 and T30 implements it. <!-- unwanted-behavior -->

**Independent Test**: Enter Play mode: the panel, rail, buttons and rows render with the kit's sprites and colours; every existing PlayMode test still passes; tapping through a glow or the scanline overlay still selects the surface behind it.

---

## Edge Cases

| ID | Edge case |
| -- | --------- |
| EDGE-01 | IF two touches land in the same frame THEN the system SHALL process only the primary touch for tap/drag logic. <!-- unwanted-behavior --> |
| EDGE-02 | IF a tap hits a UI element (panel, buttons, popup) THEN the system SHALL NOT raycast into the 3D scene. <!-- unwanted-behavior --> |
| EDGE-03 | WHEN a window drag releases with the finger off the original wall THEN the system SHALL clamp the end corner to that wall's bounds instead of switching walls. <!-- event-driven --> |
| EDGE-04 | IF window mode is active and the user taps a wall without dragging THEN the system SHALL NOT create a window (below minimum size rule applies). <!-- unwanted-behavior --> |
| EDGE-05 | IF the wall mesh rebuild fails validation (degenerate geometry) THEN the system SHALL keep the previous wall mesh and collider and reject the operation (add or delete). <!-- unwanted-behavior --> |
| EDGE-06 | WHEN the selection changes while the list panel is collapsed THEN the system SHALL still update the underlying rows so the panel is correct when expanded. <!-- event-driven --> |

---

## Requirement Traceability

| Requirement ID | Story | Phase | Status |
| -------------- | ----- | ----- | ------ |
| BOOT-01 | P0: Project bootstrap | Tasks | Verified |
| ROOM-01 | P1: Orbit a synthetic room | Tasks | Verified |
| CAM-01 | P1: Orbit a synthetic room | Tasks | Verified |
| CAM-02 | P1: Orbit a synthetic room | Tasks | Verified |
| SEL-01 | P1: Select a surface by tapping | Tasks | Verified |
| SEL-02 | P1: Select a surface by tapping | Tasks | Verified |
| SEL-03 | P1: Select a surface by tapping | Tasks | Verified |
| LIST-01 | P1: Real-time surface list | Tasks | Verified |
| LIST-02 | P1: Real-time surface list | Tasks | Verified |
| CLR-01 | P1: Clear selection | Tasks | Verified |
| WIN-01 | P2: Window holes by rectangle drag | Tasks | Verified |
| WIN-02 | P2: Window holes by rectangle drag | Tasks | Verified |
| WIN-03 | P2: Window holes by rectangle drag | Tasks | Verified |
| WIN-04 | P2: Window deletion | Tasks | Verified |
| OUT-01 | P2: Selection outline (polish) | Tasks | Verified |
| AR-01 | P3: AR pose camera mode | Tasks | Verified |
| TOP-01 | P3: 2D / 3D view switch | Tasks | Verified |
| TOP-02 | P3: 2D / 3D view switch | Tasks | Verified |
| EDGE-01 | Edge Cases | Tasks | Verified |
| EDGE-02 | Edge Cases | Tasks | Verified |
| EDGE-03 | Edge Cases | Tasks | Verified |
| EDGE-04 | Edge Cases | Tasks | Verified |
| EDGE-05 | Edge Cases | Tasks | Verified |
| EDGE-06 | Edge Cases | Tasks | Verified |
| HUD-01 | P4: HUD visual pass | Tasks | Verified |

**ID map:** BOOT-01 project bootstrap (P0 all); ROOM-01 room generation with solid surfaces (P1.1); CAM-01 orbit drag (P1.2, P1.5); CAM-02 pitch clamp + Orbit-mode containment (P1.3, P1.4); SEL-01 single-selection tap semantics (S2.1-S2.3); SEL-02 tint feedback (S2.4); SEL-03 tap/drag discrimination + through-opening miss + window-collider routing (S2.5-S2.7); LIST-01 real-time list with (previous, current) update (S3.1, S3.2); LIST-02 collapsible panel (S3.3, S3.4); CLR-01 clear button, idempotent, window-mode interaction (S4 all); WIN-01 mode entry gating and exit (enabled in Orbit or WindowDraw) + routing + tap lock while drawing (W.1, W.2, W.13); WIN-02 preview + solid-mesh cut + collider sync + through-ray (W.3, W.4, W.5, W.12); WIN-03 validation rules incl. max size and edge margin (W.6-W.11); WIN-04 window deletion (D all); OUT-01 outline polish + raycast mask (O all); AR-01 AR pose camera (AR all); TOP-01 2D/3D switch, ceiling disable, camera reset, fit-to-room framing + pinch zoom (T.1, T.2, T.5, T.7, T.9); TOP-02 state cancel on entry + interactive plan selection incl. wall-tap tolerance (T.3, T.4, T.6, T.8); HUD-01 art-kit HUD pass incl. selected-state field and raycast hygiene (H all).

**HUD-01 was implemented on 2026-08-18** by T31 (AC3) and T32 (AC1, AC2, AC4, AC5). Appearance itself stays a
human check (`validation.md` section 7); what automation asserts is what a restyle can break silently — decoration
that starts consuming taps, opaque HUD that stops consuming them, a rail button wired twice, and the readout and
hint copy going stale. Two kit nodes the earlier notes called decoration, `Readout` and `HintPill`, carry live data
instead (AD-022), and the hint copy describes the drag that WIN-02 actually implements rather than the mock-up's
tap-to-place with resize handles — AC5 again, this time about copy rather than the Clear button.

**WIN-01 AC1 was rewritten on 2026-08-18** by AD-019 (button disabled instead of hidden, with a state dot) and AD-021 (pressing it while window mode is active exits to Orbit). T30 implemented both and the PlayMode fixture now asserts enabled/disabled, the state dot and both click directions, so WIN-01 is back to `Verified`.

**UAT-pending ACs:** BOOT-01 AC5 (iOS build-target switch), OUT-01 AC1 (outline appearance) and AR-01 (real XR Simulation pose) are `Verified` for everything automation can assert; each carries one human check listed in `validation.md` section 7. Every other AC is verified by an automated test.

**Retired IDs (do not reuse):** CLR-02 (tap-outside-clears — removed by 2026-08-17 revision: Clear button is the only path to empty).

**Status values:** Pending → In Design → In Tasks → Implementing → Verified

**Coverage:** 25 total, 25 mapped to tasks, 0 unmapped ✅ (Requirement → Task Map in `tasks.md`; per-task docs in `docs/tasks/`)

---

## Success Criteria

How we know the feature is successful:

- [ ] All surfaces (4 walls, floor, ceiling) can be viewed and selected in under 30 seconds of first use, with the list always matching the 3D state and never more than one surface selected
- [ ] Zero false selections: 20 consecutive orbit drags over surfaces produce no selection changes
- [ ] A window hole shows the outside through the solid wall (reveal faces visible), rays through openings never hit the wall, invalid rectangles (overlap, too small, too large, margin violation) are always rejected, and a deleted window restores a solid wall
- [ ] EditMode + PlayMode test suites pass for selection logic, window validation, solid-mesh cutting, collider sync and tap interaction
