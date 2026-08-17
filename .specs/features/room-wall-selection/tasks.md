# Room Wall Selection Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user - do not proceed without it.**

**Commit convention override (user-level, overrides `check_commit.py`):** commits use the user's global format `[type] short description` in English (e.g. `[feat] add RoomModel selection logic`), NOT Conventional Commits. Never credit the agent in commits.

**Detailed per-task documents:** every task below has a full functional + technical description at `docs/tasks/TNN-*.md`. This file is the canonical index and execution order; the docs/tasks files carry the depth.

---

**Design**: `.specs/features/room-wall-selection/design.md`
**Status**: Draft

---

## Test Coverage Matrix

> Generated from codebase, project guidelines, and spec - confirm before Execute. Guidelines found: none in repo (greenfield, docs only) - strong defaults applied, constrained by AD-005 (Unity Test Framework: EditMode for pure logic, PlayMode for interaction) and AD-001 (validation target = Unity Editor only).

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| ---------- | ------------------ | -------------------- | ---------------- | ----------- |
| Domain (plain C#, `Assets/Scripts/Domain/`) | EditMode unit | All branches; 1:1 to spec ACs; every listed edge case has a test | `Assets/Tests/EditMode/*Tests.cs` | Unity MCP `run_tests` (EditMode); fallback AD-006: human runs Test Runner |
| Presentation / UI (MonoBehaviours, `Assets/Scripts/Presentation/`, `Assets/Scripts/UI/`) | PlayMode | Happy path + every listed edge case + error/failure paths per AC | `Assets/Tests/PlayMode/*Tests.cs` | Unity MCP `run_tests` (PlayMode); fallback AD-006: human runs Test Runner |
| Config / scene / asmdef / ProjectSettings / URP assets | none | - (build gate only) | - | build gate only |

## Gate Check Commands

> Generated from codebase - confirm before Execute. All via Unity MCP; fallback per AD-006 is the human running the same check in the Editor.

| Gate Level | When to Use | Command |
| ---------- | ----------- | ------- |
| Quick | After tasks with EditMode tests only | Unity MCP `run_tests` filter=EditMode |
| Full | After tasks with PlayMode tests | Unity MCP `run_tests` EditMode + PlayMode |
| Build | After phase completion or config/scene-only tasks | Unity MCP console check (zero compile errors) + `run_tests` EditMode + PlayMode |

---

## Execution Plan

Phases are ordered and run sequentially - each phase completes before the next begins, and tasks within a phase execute in order.

### Phase 1: P0 Bootstrap (BOOT-01)

```
T01 → T02 → T03 → T04 → T06
T01 → T05 → T06
```

### Phase 2: Domain core (plain C#, EditMode-tested)

```
T07 → T08 → T10
T07 → T09 → T10
T07 → T11 → T12
```

### Phase 3: P1 Presentation (room, camera, selection, list, clear)

```
T07 → T13 → T14 → T17
T10 → T15 → T16 → T17
T11 → T15
T12 → T16
T14 → T18
T16 → T18
T16 → T19 → T20
```

### Phase 4: P2 Windows + outline

```
T21 → T22
T23
T24
```

### Phase 5: P3 AR + 2D plan

```
T25 → T26
T27 → T28
T27 → T29
```

---

## Task Breakdown

### T01: Create Packages/manifest.json with pinned packages

**What**: Package manifest pinning URP, Input System, AR Foundation 6.x, ARKit XR Plugin 6.x, Test Framework, uGUI/TMP.
**Where**: `Packages/manifest.json`
**Depends on**: None
**Reuses**: none (greenfield)
**Requirement**: BOOT-01

**Tools**:

- MCP: NONE (text asset; agent writes per AD-006)
- Skill: NONE

**Done when**:

- [x] manifest.json lists com.unity.render-pipelines.universal 17.x, com.unity.inputsystem, com.unity.xr.arfoundation 6.x, com.unity.xr.arkit 6.x (matching), com.unity.test-framework, com.unity.ugui at pinned versions
- [x] Unity resolves all packages without errors on import

**Tests**: none
**Gate**: build

**Commit**: `[chore] pin project packages in manifest`

---

### T02: Create URP pipeline + renderer assets

**What**: URP asset and Universal Renderer asset under Assets/Settings (renderer will later host the P2 outline features).
**Where**: `Assets/Settings/`
**Depends on**: T01
**Reuses**: none
**Requirement**: BOOT-01

**Tools**:

- MCP: `unity-mcp` (or agent-written YAML per AD-006)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] URP asset + renderer asset exist and reference each other
- [ ] No console errors on import

**Tests**: none
**Gate**: build

**Commit**: `[chore] add URP pipeline and renderer assets`

---

### T03: Configure ProjectSettings (URP assigned, iOS-exportable, XR)

**What**: Graphics/Quality reference the URP asset; iOS bundle id + company/product name; XR Plug-in Management with ARKit (iOS) + XR Simulation (Editor).
**Where**: `ProjectSettings/`
**Depends on**: T02
**Reuses**: none
**Requirement**: BOOT-01

**Tools**:

- MCP: `unity-mcp` (or agent-written per AD-006)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] URP asset assigned in Graphics and all Quality tiers
- [ ] Input handling set to Input System (new)
- [ ] XR Plug-in Management: ARKit enabled for iOS, XR Simulation for Editor
- [ ] No console errors

**Tests**: none
**Gate**: build

**Commit**: `[chore] configure project settings for URP, iOS and XR`

---

### T04: Create bootstrap scene

**What**: Empty Main scene that opens clean; registered in Build Settings.
**Where**: `Assets/Scenes/Main.unity`
**Depends on**: T03
**Reuses**: none
**Requirement**: BOOT-01

**Tools**:

- MCP: `unity-mcp`
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Scene opens in Editor with zero console errors
- [ ] Scene 0 in Build Settings

**Tests**: none
**Gate**: build

**Commit**: `[chore] add bootstrap scene`

---

### T05: Create assembly definitions + empty test assemblies

**What**: Domain.asmdef (no UnityEngine-heavy deps), Presentation.asmdef, EditMode test asmdef (`includePlatforms: ["Editor"]`, UNITY_INCLUDE_TESTS), PlayMode test asmdef (references UnityEngine.TestRunner, Unity.InputSystem, Unity.InputSystem.TestFramework).
**Where**: `Assets/**/*.asmdef`
**Depends on**: T01
**Reuses**: none
**Requirement**: BOOT-01

**Tools**:

- MCP: NONE (text assets)
- Skill: NONE

**Done when**:

- [ ] 4 asmdefs compile; test assemblies visible to Test Runner
- [ ] Test Runner runs green with zero tests in EditMode AND PlayMode

**Tests**: none
**Gate**: build

**Commit**: `[chore] add assembly definitions and empty test assemblies`

---

### T06: P0 Editor verification checkpoint

**What**: Verify BOOT-01 acceptance in the Editor: console clean, Test Runner green (0 tests), iOS build target switch without errors.
**Where**: Unity Editor (verification only, no files)
**Depends on**: T04, T05
**Reuses**: none
**Requirement**: BOOT-01

**Tools**:

- MCP: `unity-mcp` (console read, run_tests, build target switch); fallback AD-006: human verifies
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Console has zero errors on project open
- [ ] `run_tests` green in EditMode and PlayMode (0 tests)
- [ ] Build target switches to iOS without build-settings errors

**Tests**: none
**Gate**: build

**Commit**: `[chore] record P0 verification result`

---

### T07: Create domain data types

**What**: All plain-C# domain types from design Data Models: SurfaceKind, Rect2D, SurfaceDefinition, WindowSpec, RoomDefinition, MeshData, WindowRejection, Mode.
**Where**: `Assets/Scripts/Domain/DomainTypes.cs`
**Depends on**: None
**Reuses**: none
**Requirement**: ROOM-01 (types), WIN-04 (WindowSpec.id)

**Tools**:

- MCP: NONE
- Skill: NONE

**Done when**:

- [ ] All 8 types compile in Domain assembly with no UnityEngine dependency beyond Vector types
- [ ] Thickness default 0.15f on SurfaceDefinition

**Tests**: none
**Gate**: build

**Commit**: `[feat] add domain data types`

---

### T08: Implement WindowPlacementValidator + EditMode tests

**What**: All WIN-03 placement rules: clamp to bounds minus margin, overlap, min 0.2 m, max 2.0 m, margin 0.1 m, InvalidSurfaceKind for non-Wall.
**Where**: `Assets/Scripts/Domain/WindowPlacementValidator.cs`
**Depends on**: T07
**Reuses**: T07 types
**Requirement**: WIN-03

**Tools**:

- MCP: `unity-mcp` (run_tests), `context7` if API doubt
- Skill: NONE

**Done when**:

- [ ] `Validate(surface, existing, candidate)` returns clamped rect or rejection reason
- [ ] EditMode tests cover WIN AC6-AC9, AC11 + InvalidSurfaceKind, 1:1 to ACs
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 8 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add window placement validator`

---

### T09: Implement RoomModel selection + EditMode tests

**What**: Single selection: `Select(id)`, `ClearSelection()`, `SelectionChanged(int? previous, int? current)`; same-surface tap no-op; clear idempotent (AD-007).
**Where**: `Assets/Scripts/Domain/RoomModel.cs`
**Depends on**: T07
**Reuses**: T07 types
**Requirement**: SEL-01, CLR-01

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] SEL AC1-AC3 + CLR AC1-AC2 behaviors implemented exactly (no event on no-op paths)
- [ ] EditMode tests 1:1 to those ACs, incl. event payload (previous, current)
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 6 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add RoomModel single selection logic`

---

### T10: Add RoomModel window operations + EditMode tests

**What**: `TryAddWindow` (validator-gated, Wall-only) / `TryRemoveWindow`; WindowAdded/WindowRemoved events; rollback on rebuild failure (EDGE-05 model side).
**Where**: `Assets/Scripts/Domain/RoomModel.cs` (modify)
**Depends on**: T08, T09
**Reuses**: T08 validator
**Requirement**: WIN-02 (model side), WIN-04 (model side), EDGE-05

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Add rejected for non-Wall (InvalidSurfaceKind) and every validator rejection; remove by window id
- [ ] EditMode tests: add ok / each rejection kind / remove ok / remove unknown id / rollback path
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 7 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add window operations to RoomModel`

---

### T11: Implement SurfaceMeshBuilder + EditMode tests

**What**: Solid slab mesh with grid-sliced front/back faces, outer sides, 4 reveal faces per hole; pure function returning MeshData; output validation (EDGE-05).
**Where**: `Assets/Scripts/Domain/SurfaceMeshBuilder.cs`
**Depends on**: T07
**Reuses**: T07 MeshData/Rect2D
**Requirement**: WIN-02 (geometry), EDGE-05

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Zero-hole slab: 2 faces + 4 sides, correct winding; N-hole slab adds 4 reveals per hole; no vertex inside any hole rect on front/back faces
- [ ] EditMode tests assert vertex/triangle invariants + degenerate-input validation failure
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 6 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add solid slab mesh builder with window holes`

---

### T12: Implement RoomBuilder + EditMode tests

**What**: `Build(footprint, height, thickness)` → RoomDefinition with N >= 3 walls + floor + ceiling, generation-order naming ("Wall 1".."Wall N", "Floor", "Ceiling").
**Where**: `Assets/Scripts/Domain/RoomBuilder.cs`
**Depends on**: T11
**Reuses**: T07 types, T11 builder
**Requirement**: ROOM-01

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] 4-vertex footprint yields 6 surfaces; 3- and 5-vertex footprints yield N+2; kinds and names correct
- [ ] EditMode tests cover N=3, N=4, N=5 and naming
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 4 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add room builder for N-wall footprints`

---

### T13: Implement ModeManager + EditMode tests

**What**: `Mode Current`, `TrySet`, `ModeChanged`; full AD-013 transition matrix incl. side-effect hooks (cancel draw, clear selection, end AR session) as events/callbacks.
**Where**: `Assets/Scripts/Presentation/ModeManager.cs`
**Depends on**: T07
**Reuses**: T07 Mode enum
**Requirement**: WIN-01 (gating), TOP-02 (cancel), AR-01 (session end)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Every cell of the AD-013 matrix returns the documented legal/illegal result; illegal → false, no state change
- [ ] EditMode tests: one test per matrix cell (12 non-diagonal cells) + WindowDraw entry requires Wall-selected predicate
- [ ] Gate passes: run_tests EditMode
- [ ] Test count: >= 13 tests pass

**Tests**: EditMode unit
**Gate**: quick

**Commit**: `[feat] add mode state machine with transition matrix`

---

### T14: Implement InputRouter + PlayMode tests

**What**: EnhancedTouch entry point; tap vs drag (20 px DPI-scaled / 300 ms, serialized); primary touch only (EDGE-01); `IsPointerOverGameObject` gate (EDGE-02); TouchSimulation in Editor; dispatch by ModeManager.Current.
**Where**: `Assets/Scripts/Presentation/InputRouter.cs`
**Depends on**: T13
**Reuses**: T13 ModeManager
**Requirement**: SEL-03 (AC5), EDGE-01, EDGE-02

**Tools**:

- MCP: `unity-mcp` (run_tests), `context7` (InputTestFixture API)
- Skill: NONE

**Done when**:

- [ ] Tapped/DragStart/DragDelta/DragEnd events fire per classification
- [ ] PlayMode tests via InputTestFixture: short tap → Tapped; long/far move → drag only; second touch ignored; touch over uGUI blocked
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 5 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add input router with tap drag classification`

---

### T15: Implement SurfaceView + PlayMode tests

**What**: Per-surface MeshFilter/Renderer/MeshCollider; tint via MaterialPropertyBlock on SelectionChanged; rebuild on WindowAdded/WindowRemoved with mesh AND collider swapped in the same operation (AD-009).
**Where**: `Assets/Scripts/Presentation/SurfaceView.cs`
**Depends on**: T10, T11
**Reuses**: T11 MeshData, T10 events
**Requirement**: SEL-02, WIN-02 (AC5, AC12)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Tint appears/disappears with selection; MaterialPropertyBlock only (no material instantiation)
- [ ] PlayMode tests: tint toggles; after TryAddWindow a ray through the opening does NOT hit the wall collider; after remove it DOES
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 4 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add surface view with tint and collider sync`

---

### T16: Implement RoomBootstrap scene wiring + PlayMode test

**What**: Scene bootstrap that builds the room (6 m x 4 m x 2.8 m defaults), instantiates SurfaceViews, wires RoomModel/controllers; added to Main scene.
**Where**: `Assets/Scripts/Presentation/RoomBootstrap.cs`
**Depends on**: T12, T15
**Reuses**: T12 RoomBuilder, T15 SurfaceView
**Requirement**: ROOM-01

**Tools**:

- MCP: `unity-mcp` (scene edit, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Play Mode shows 4 walls + floor + ceiling as solid slabs
- [ ] PlayMode test: scene load yields 6 SurfaceViews with colliders matching RoomDefinition
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 2 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add room bootstrap and scene wiring`

---

### T17: Implement OrbitCameraController + PlayMode tests

**What**: First-person yaw 360 free / pitch clamped (-60..+60 serialized); room centre at 1.6 m eye height; inside-room constraint (Orbit mode only); `SetRotation` + `ResetToRoomCentre` for handoffs.
**Where**: `Assets/Scripts/Presentation/OrbitCameraController.cs`
**Depends on**: T14, T16
**Reuses**: T14 drag events
**Requirement**: CAM-01, CAM-02

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Horizontal drag → yaw; vertical drag → pitch clamped; mouse works in Editor (via TouchSimulation)
- [ ] PlayMode tests: yaw wraps past 360; pitch clamps at bounds; camera position stays inside room bounds
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 3 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add orbit camera controller`

---

### T18: Implement SelectionController + PlayMode tests

**What**: Tap raycast → `RoomModel.Select`; miss/through-opening → unchanged; WindowView component hit → route to deletion UI, never selection (AD-014); taps swallowed in WindowDraw (AD-015); mask includes default + SelectedSurface layers.
**Where**: `Assets/Scripts/Presentation/SelectionController.cs`
**Depends on**: T14, T16
**Reuses**: T14 Tapped event, T09 model
**Requirement**: SEL-01, SEL-03

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Tap wall/floor/ceiling selects; tap empty leaves unchanged; drag never selects
- [ ] PlayMode tests: SEL AC1-AC3 via simulated taps; miss keeps selection; WindowDraw-mode tap swallowed (stub mode)
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 5 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add tap selection controller`

---

### T19: Implement SurfaceListPanel + PlayMode tests

**What**: Collapsible uGUI panel (Canvas + TMP) listing every surface with state; updates exactly the two affected rows per SelectionChanged; rows update while collapsed (EDGE-06); collapse control always visible.
**Where**: `Assets/Scripts/UI/SurfaceListPanel.cs`
**Depends on**: T16
**Reuses**: T09 SelectionChanged
**Requirement**: LIST-01, LIST-02, EDGE-06

**Tools**:

- MCP: `unity-mcp` (UI hierarchy, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Panel lists 6 surfaces; selection change updates old + new rows in same frame; collapse/expand works
- [ ] PlayMode tests: row states after Select/Clear; update-while-collapsed
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 4 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add real-time surface list panel`

---

### T20: Add Clear button + PlayMode test

**What**: uGUI Clear button wired to `RoomModel.ClearSelection()`; in WindowDraw it first forces `TrySet(Orbit)` (cancelling the draw) then clears (CLR AC3 wiring; full window-mode test lands with T21).
**Where**: `Assets/Scripts/UI/ClearButton.cs`
**Depends on**: T19
**Reuses**: T09 ClearSelection, T13 TrySet
**Requirement**: CLR-01

**Tools**:

- MCP: `unity-mcp` (UI, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Press with selection → deselected, tint removed, row updated; press with none → no event (spy on SelectionChanged)
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 2 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add clear selection button`

---

### T21: Implement WindowDrawController + PlayMode tests

**What**: WindowDraw mode: DragStart fixes wall + corner; live clamped preview quad; DragEnd → `TryAddWindow`; rejection removes preview; cancel on mode exit / Clear / 2D (CLR AC3, EDGE-03, EDGE-04); off-wall drag start creates nothing.
**Where**: `Assets/Scripts/Presentation/WindowDrawController.cs`
**Depends on**: None (cross-phase: T10, T13, T14, T18)
**Reuses**: T10 TryAddWindow, T14 drag events, T13 modes
**Requirement**: WIN-01, WIN-02 (AC3), EDGE-03, EDGE-04, CLR-01 (AC3)

**Tools**:

- MCP: `unity-mcp` (run_tests), `context7` (plane projection API doubt)
- Skill: NONE

**Done when**:

- [ ] Valid drag cuts a see-through hole; preview clamps live; all rejection paths remove preview without wall change
- [ ] PlayMode tests: valid cut; tap-without-drag no window (EDGE-04); off-wall release clamps (EDGE-03); Clear cancels draw + exits mode + deselects (CLR AC3); mode-exit mid-drag cancels
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 6 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add window drawing with live preview`

---

### T22: Add window mode button + PlayMode test

**What**: uGUI button visible only while (selected surface is Wall AND mode == Orbit) (AD-015); toggles WindowDraw via `TrySet`.
**Where**: `Assets/Scripts/UI/WindowModeButton.cs`
**Depends on**: T21
**Reuses**: T13 ModeManager, T09 SelectionChanged
**Requirement**: WIN-01 (AC1)

**Tools**:

- MCP: `unity-mcp` (UI, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Button visible for Wall+Orbit; hidden for none/Floor/Ceiling selected and in Ar/TopDown
- [ ] PlayMode tests cover all 4 visibility cases
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 4 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add window mode button with visibility gating`

---

### T23: Implement WindowView deletion flow + PlayMode tests

**What**: Per-window GameObject: BoxCollider sized rect x thickness; tap → X anchored top-right; X → confirmation popup; confirm → `TryRemoveWindow` + rebuild; cancel → unchanged; created/destroyed on WindowAdded/Removed; tap on opening never changes selection (AD-014).
**Where**: `Assets/Scripts/Presentation/WindowView.cs`
**Depends on**: None (cross-phase: T10, T15, T18)
**Reuses**: T10 remove API, T18 routing
**Requirement**: WIN-04, SEL-03 (AC7)

**Tools**:

- MCP: `unity-mcp` (popup prefab, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Full loop: cut → tap opening (X appears) → X (popup) → cancel (stays) / confirm (wall solid again, ray hits wall)
- [ ] PlayMode tests: WIN-04 AC1-AC5 + selection unchanged on opening tap
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 6 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add window deletion with confirmation`

---

### T24: Implement selection outline (stencil two-pass) + PlayMode test

**What**: `SelectedSurface` layer; two RenderObjects features (stencil mark + edge pass) on the URP renderer; SurfaceView swaps layer while selected; SelectionController mask already includes both layers (verify).
**Where**: `Assets/Settings/` (renderer features) + `Assets/Scripts/Presentation/SurfaceView.cs` (modify)
**Depends on**: None (cross-phase: T02, T15, T18)
**Reuses**: T02 renderer asset
**Requirement**: OUT-01

**Tools**:

- MCP: `unity-mcp` (renderer config, run_tests), `context7` (RenderObjects stencil API)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Selected surface shows outline + tint from any angle
- [ ] PlayMode test: selected surface still hittable (re-tap keeps selection, no deselect) - OUT AC2
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 2 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add stencil outline for selected surface`

---

### T25: Implement ArPoseCameraController + PlayMode tests

**What**: AR mode drives rig from device pose (ARSession/XROrigin); rotation always from pose, position clamped to room; on exit hands yaw/pitch to OrbitCameraController (no snap); XR Simulation validates in Editor.
**Where**: `Assets/Scripts/Presentation/ArPoseCameraController.cs`
**Depends on**: None (cross-phase: T13, T16, T17)
**Reuses**: T17 SetRotation
**Requirement**: AR-01

**Tools**:

- MCP: `unity-mcp` (XROrigin setup, run_tests), `context7` (AR Foundation 6 API)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] AR mode ignores touch-drag camera input; selection taps still work; position clamped
- [ ] PlayMode tests: pose-driven rotation applied (driven transform stub); clamp inside room; exit handoff preserves orientation
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 3 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add AR pose camera controller`

---

### T26: Add AR mode toggle + availability gating + PlayMode test

**What**: uGUI AR toggle wired to `TrySet(Ar)`; disabled (greyed) where AR tracking unavailable (AR AC4); exit restores orbit.
**Where**: `Assets/Scripts/UI/ArToggleButton.cs`
**Depends on**: T25
**Reuses**: T13 ModeManager
**Requirement**: AR-01 (AC1, AC3, AC4)

**Tools**:

- MCP: `unity-mcp` (UI, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Toggle enters/exits AR mode; disabled when availability check fails
- [ ] PlayMode tests: toggle enabled/disabled per availability; exit returns to Orbit
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 2 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add AR mode toggle with availability gating`

---

### T27: Implement TopDownController + PlayMode tests

**What**: 2D entry: ortho top camera fit-to-room (+margin, never further out), ceiling MeshRenderer AND MeshCollider disabled, selection cleared, draw cancelled; while 2D: pinch zoom 0.5x-2.0x of fit (2D only); 3D exit: ceiling restored, Orbit at room centre (AD-012, AD-016).
**Where**: `Assets/Scripts/Presentation/TopDownController.cs`
**Depends on**: None (cross-phase: T13, T16, T18)
**Reuses**: T13 modes, T17 ResetToRoomCentre
**Requirement**: TOP-01, TOP-02 (AC3, AC4, AC9)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] 2D entry cancels state, disables ceiling renderer+collider, frames fit-to-room; pinch zooms within limits and does nothing in 3D; 3D restores everything
- [ ] PlayMode tests: TOP AC2, AC3, AC5, AC7, AC9 (incl. tap not blocked by ceiling)
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 5 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add top-down 2D view controller`

---

### T28: Add 2D wall-tap tolerance to SelectionController + PlayMode test

**What**: In 2D mode, a Floor hit within 30 px (serialized) of a wall selects the nearest such wall (AD-016); no effect in 3D.
**Where**: `Assets/Scripts/Presentation/SelectionController.cs` (modify)
**Depends on**: T27
**Reuses**: T18 raycast path
**Requirement**: TOP-02 (AC8)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [ ] Floor tap near wall selects nearest wall in 2D; same tap in 3D selects floor; floor tap far from walls selects floor in 2D
- [ ] PlayMode tests cover those 3 cases
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 3 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add 2D wall tap tolerance`

---

### T29: Add "2D | 3D" buttons + PlayMode test

**What**: Two uGUI buttons at screen top wired to TopDownController / `TrySet`; list panel + Clear stay functional in 2D (TOP AC1, AC6).
**Where**: `Assets/Scripts/UI/ViewSwitchButtons.cs`
**Depends on**: T27
**Reuses**: T27 controller, T19 panel
**Requirement**: TOP-01 (AC1), TOP-02 (AC6)

**Tools**:

- MCP: `unity-mcp` (UI, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [ ] Buttons switch views; in 2D the list updates on plan selection and Clear works
- [ ] PlayMode tests: switch both ways; list+Clear functional in 2D
- [ ] Gate passes: run_tests EditMode + PlayMode
- [ ] Test count: >= 3 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add 2D 3D view switch buttons`

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5

Phase 1:  T01 → T02 → T03 → T04 → T06
          T01 → T05 → T06
Phase 2:  T07 → T08 → T10
          T07 → T09 → T10
          T07 → T11 → T12
Phase 3:  T07 → T13 → T14 → T17
          T10 → T15 → T16 → T17
          T11 → T15
          T12 → T16
          T14 → T18
          T16 → T18
          T16 → T19 → T20
Phase 4:  T21 → T22
          T23
          T24
Phase 5:  T25 → T26
          T27 → T28
          T27 → T29
```

Execution is strictly sequential - one task at a time, in order. 29 tasks total → packs into ~4-5 batches of ~7 tasks (whole phases); sub-agent offer applies at Execute.

---

## Task Granularity Check

| Task | Scope | Status |
| ---- | ----- | ------ |
| T01 | 1 file (manifest) | ✅ Granular |
| T02 | 2 cohesive assets (pipeline+renderer) | ✅ OK (cohesive) |
| T03 | 1 settings folder (cohesive config) | ✅ OK (cohesive) |
| T04 | 1 scene | ✅ Granular |
| T05 | 4 asmdefs (one cohesive config set) | ✅ OK (cohesive) |
| T06 | verification only | ✅ Granular |
| T07 | 1 file (types) | ✅ Granular |
| T08 | 1 class + tests | ✅ Granular |
| T09 | 1 class (selection) + tests | ✅ Granular |
| T10 | same class (windows) + tests | ✅ Granular |
| T11 | 1 class + tests | ✅ Granular |
| T12 | 1 class + tests | ✅ Granular |
| T13 | 1 class + tests | ✅ Granular |
| T14 | 1 class + tests | ✅ Granular |
| T15 | 1 class + tests | ✅ Granular |
| T16 | 1 class + scene wiring | ✅ Granular |
| T17 | 1 class + tests | ✅ Granular |
| T18 | 1 class + tests | ✅ Granular |
| T19 | 1 class + UI + tests | ✅ Granular |
| T20 | 1 button + tests | ✅ Granular |
| T21 | 1 class + tests | ✅ Granular |
| T22 | 1 button + tests | ✅ Granular |
| T23 | 1 class + popup + tests | ✅ Granular |
| T24 | renderer features + 1 file modify | ✅ OK (cohesive) |
| T25 | 1 class + tests | ✅ Granular |
| T26 | 1 button + tests | ✅ Granular |
| T27 | 1 class + tests | ✅ Granular |
| T28 | 1 file modify + tests | ✅ Granular |
| T29 | 1 file (2 buttons) + tests | ✅ Granular |

## Diagram-Definition Cross-Check

| Task | Depends On (task body) | Diagram Shows | Status |
| ---- | ---------------------- | ------------- | ------ |
| T01 | None | (root) | ✅ Match |
| T02 | T01 | T01→T02 | ✅ Match |
| T03 | T02 | T02→T03 | ✅ Match |
| T04 | T03 | T03→T04 | ✅ Match |
| T05 | T01 | T01→T05 | ✅ Match |
| T06 | T04, T05 | T04→T06, T05→T06 | ✅ Match |
| T07 | None | (root) | ✅ Match |
| T08 | T07 | T07→T08 | ✅ Match |
| T09 | T07 | T07→T09 | ✅ Match |
| T10 | T08, T09 | T08→T10, T09→T10 | ✅ Match |
| T11 | T07 | T07→T11 | ✅ Match |
| T12 | T11 | T11→T12 | ✅ Match |
| T13 | T07 | T07→T13 | ✅ Match |
| T14 | T13 | T13→T14 | ✅ Match |
| T15 | T10, T11 | T10→T15, T11→T15 | ✅ Match |
| T16 | T12, T15 | T12→T16, T15→T16 | ✅ Match |
| T17 | T14, T16 | T14→T17, T16→T17 | ✅ Match |
| T18 | T14, T16 | T14→T18, T16→T18 | ✅ Match |
| T19 | T16 | T16→T19 | ✅ Match |
| T20 | T19 | T19→T20 | ✅ Match |
| T21 | prior phases only | (root of Phase 4) | ✅ Match |
| T22 | T21 | T21→T22 | ✅ Match |
| T23 | prior phases only | (root) | ✅ Match |
| T24 | prior phases only | (root) | ✅ Match |
| T25 | prior phases only | (root of Phase 5) | ✅ Match |
| T26 | T25 | T25→T26 | ✅ Match |
| T27 | prior phases only | (root) | ✅ Match |
| T28 | T27 | T27→T28 | ✅ Match |
| T29 | T27 | T27→T29 | ✅ Match |

No dependency points to a later phase.

## Test Co-location Validation

| Task | Code Layer Created/Modified | Matrix Requires | Task Says | Status |
| ---- | --------------------------- | --------------- | --------- | ------ |
| T01 | config (manifest) | none | none | ✅ OK |
| T02 | config (URP assets) | none | none | ✅ OK |
| T03 | config (ProjectSettings) | none | none | ✅ OK |
| T04 | scene | none | none | ✅ OK |
| T05 | config (asmdefs) | none | none | ✅ OK |
| T06 | none (verification) | none | none | ✅ OK |
| T07 | domain types (entity) | none | none | ✅ OK |
| T08 | domain logic | EditMode unit | EditMode unit | ✅ OK |
| T09 | domain logic | EditMode unit | EditMode unit | ✅ OK |
| T10 | domain logic | EditMode unit | EditMode unit | ✅ OK |
| T11 | domain logic | EditMode unit | EditMode unit | ✅ OK |
| T12 | domain logic | EditMode unit | EditMode unit | ✅ OK |
| T13 | presentation (pure logic) | EditMode unit (pure) | EditMode unit | ✅ OK |
| T14 | presentation | PlayMode | PlayMode | ✅ OK |
| T15 | presentation | PlayMode | PlayMode | ✅ OK |
| T16 | presentation | PlayMode | PlayMode | ✅ OK |
| T17 | presentation | PlayMode | PlayMode | ✅ OK |
| T18 | presentation | PlayMode | PlayMode | ✅ OK |
| T19 | UI | PlayMode | PlayMode | ✅ OK |
| T20 | UI | PlayMode | PlayMode | ✅ OK |
| T21 | presentation | PlayMode | PlayMode | ✅ OK |
| T22 | UI | PlayMode | PlayMode | ✅ OK |
| T23 | presentation | PlayMode | PlayMode | ✅ OK |
| T24 | config + presentation modify | PlayMode | PlayMode | ✅ OK |
| T25 | presentation | PlayMode | PlayMode | ✅ OK |
| T26 | UI | PlayMode | PlayMode | ✅ OK |
| T27 | presentation | PlayMode | PlayMode | ✅ OK |
| T28 | presentation modify | PlayMode | PlayMode | ✅ OK |
| T29 | UI | PlayMode | PlayMode | ✅ OK |

---

## Requirement → Task Map

| Requirement | Tasks |
| ----------- | ----- |
| BOOT-01 | T01-T06 |
| ROOM-01 | T12, T16 |
| CAM-01 | T17 |
| CAM-02 | T17 |
| SEL-01 | T09, T18 |
| SEL-02 | T15 |
| SEL-03 | T14, T18, T23 |
| LIST-01 | T19 |
| LIST-02 | T19 |
| CLR-01 | T09, T20, T21 |
| WIN-01 | T21, T22 |
| WIN-02 | T11, T15, T21 |
| WIN-03 | T08 |
| WIN-04 | T10, T23 |
| OUT-01 | T24 |
| AR-01 | T25, T26 |
| TOP-01 | T27, T29 |
| TOP-02 | T27, T28, T29 |
| EDGE-01 | T14 |
| EDGE-02 | T14 |
| EDGE-03 | T21 |
| EDGE-04 | T21 |
| EDGE-05 | T10, T11 |
| EDGE-06 | T19 |

24/24 requirements mapped.
