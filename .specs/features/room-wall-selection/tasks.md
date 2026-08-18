# Room Wall Selection Tasks

## Execution Protocol (MANDATORY -- do not skip)

Implement these tasks with the `tlc-spec-driven` skill: **activate it by name and follow its Execute flow and Critical Rules.** Do not search for skill files by filesystem path. The skill is the source of truth for the full flow (per-task cycle, sub-agent delegation, adequacy review, Verifier, discrimination sensor).

**If the skill cannot be activated, STOP and tell the user - do not proceed without it.**

**Commit convention override (user-level, overrides `check_commit.py`):** commits use the user's global format `[type] short description` in English (e.g. `[feat] add RoomModel selection logic`), NOT Conventional Commits. Never credit the agent in commits.

**Detailed per-task documents:** every task below has a full functional + technical description at `docs/tasks/TNN-*.md`. This file is the canonical index and execution order; the docs/tasks files carry the depth.

---

**Design**: `.specs/features/room-wall-selection/design.md`
**Status**: All 32 tasks implemented, committed and verified. Phases 1-5 were independently verified in `validation.md`; Phase 6 (T30 button behaviour, T31 row state seam, T32 art kit) landed on 2026-08-18 and is recorded in `validation.md` section 9. Gate: EditMode 67/67, PlayMode 92/92. What remains is human UAT only — the three checks in `validation.md` section 7, plus the kit's appearance

---

## Test Coverage Matrix

> Generated from codebase, project guidelines, and spec - confirm before Execute. Guidelines found: none in repo (greenfield, docs only) - strong defaults applied, constrained by AD-005 (Unity Test Framework: EditMode for pure logic, PlayMode for interaction) and AD-001 (validation target = Unity Editor only).

| Code Layer | Required Test Type | Coverage Expectation | Location Pattern | Run Command |
| ---------- | ------------------ | -------------------- | ---------------- | ----------- |
| Domain (plain C#, `Assets/Scripts/Domain/`) | EditMode unit | All branches; 1:1 to spec ACs; every listed edge case has a test | `Assets/Tests/EditMode/*Tests.cs` | Unity MCP `run_tests` (EditMode); fallback AD-006: human runs Test Runner |
| Presentation / UI (MonoBehaviours, `Assets/Scripts/Presentation/`, `Assets/Scripts/UI/`) | PlayMode | Happy path + every listed edge case + error/failure paths per AC | `Assets/Tests/PlayMode/*Tests.cs` | Unity MCP `run_tests` (PlayMode); fallback AD-006: human runs Test Runner |
| Config / scene / asmdef / ProjectSettings / URP assets / sprite import settings (`.meta`) | none | - (build gate only) | - | build gate only |

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


### Phase 6: P4 HUD visual pass + button UX (follow-up)

```
T30 -> T32
T31 -> T32
```

**Done, 2026-08-18.** T30 implemented AD-019 and AD-021, T31 moved the selected state onto the row, and T32
painted the HUD from the kit under AD-020. Follow-on decisions from the paint itself: AD-022, AD-023, AD-024.

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

- [x] URP asset + renderer asset exist and reference each other
- [x] No console errors on import

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

- [x] URP asset assigned in Graphics and all Quality tiers
- [x] Input handling set to Input System (new)
- [x] XR Plug-in Management: ARKit enabled for iOS, XR Simulation for Editor
- [x] No console errors

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

- [x] Scene opens in Editor with zero console errors
- [x] Scene 0 in Build Settings

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

- [x] 4 asmdefs compile; test assemblies visible to Test Runner
- [x] Test Runner runs green with zero tests in EditMode AND PlayMode

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

- [x] Console has zero errors on project open
- [x] `run_tests` green in EditMode and PlayMode (0 tests)
- [x] Build target switches to iOS without build-settings errors

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

- [x] All 8 types compile in Domain assembly with no UnityEngine dependency beyond Vector types
- [x] Thickness default 0.15f on SurfaceDefinition

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

- [x] `Validate(surface, existing, candidate)` returns clamped rect or rejection reason
- [x] EditMode tests cover WIN AC6-AC9, AC11 + InvalidSurfaceKind, 1:1 to ACs
- [x] Gate passes: run_tests EditMode (14 tests)
- [x] Test count: >= 8 tests pass

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

- [x] SEL AC1-AC3 + CLR AC1-AC2 behaviors implemented exactly (no event on no-op paths)
- [x] EditMode tests 1:1 to those ACs, incl. event payload (previous, current)
- [x] Gate passes: run_tests EditMode
- [x] Test count: >= 6 tests pass

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

- [x] Add rejected for non-Wall (InvalidSurfaceKind) and every validator rejection; remove by window id
- [x] EditMode tests: add ok / each rejection kind / remove ok / remove unknown id / rollback path
- [x] Gate passes: run_tests EditMode
- [x] Test count: >= 7 tests pass

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

- [x] Zero-hole slab: 2 faces + 4 sides, correct winding; N-hole slab adds 4 reveals per hole; no vertex inside any hole rect on front/back faces
- [x] EditMode tests assert vertex/triangle invariants + degenerate-input validation failure
- [x] Gate passes: run_tests EditMode
- [x] Test count: >= 6 tests pass

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

- [x] 4-vertex footprint yields 6 surfaces; 3- and 5-vertex footprints yield N+2; kinds and names correct
- [x] EditMode tests cover N=3, N=4, N=5 and naming
- [x] Gate passes: run_tests EditMode
- [x] Test count: >= 4 tests pass

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

- [x] Every cell of the AD-013 matrix returns the documented legal/illegal result; illegal → false, no state change
- [x] EditMode tests: one test per matrix cell (12 non-diagonal cells) + WindowDraw entry requires Wall-selected predicate
- [x] Gate passes: run_tests EditMode
- [x] Test count: >= 13 tests pass

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

- [x] Tapped/DragStart/DragDelta/DragEnd events fire per classification
- [x] PlayMode tests via InputTestFixture: short tap → Tapped; long/far move → drag only; second touch ignored; touch over uGUI blocked
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 5 PlayMode tests pass

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

- [x] Tint appears/disappears with selection; MaterialPropertyBlock only (no material instantiation)
- [x] PlayMode tests: tint toggles; after TryAddWindow a ray through the opening does NOT hit the wall collider; after remove it DOES
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 4 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add surface view with tint and collider sync`

---

### T16: Implement RoomBootstrap scene wiring + PlayMode test

**What**: Scene bootstrap that builds the room (6 m x 4 m x 2.8 m defaults at the time; 9 x 5 since B6), instantiates SurfaceViews, wires RoomModel/controllers; added to Main scene.
**Where**: `Assets/Scripts/Presentation/RoomBootstrap.cs`
**Depends on**: T12, T15
**Reuses**: T12 RoomBuilder, T15 SurfaceView
**Requirement**: ROOM-01

**Tools**:

- MCP: `unity-mcp` (scene edit, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [x] Play Mode shows 4 walls + floor + ceiling as solid slabs
- [x] PlayMode test: scene load yields 6 SurfaceViews with colliders matching RoomDefinition
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 2 PlayMode tests pass

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

- [x] Horizontal drag → yaw; vertical drag → pitch clamped; mouse works in Editor (via TouchSimulation)
- [x] PlayMode tests: yaw wraps past 360; pitch clamps at bounds; camera position stays inside room bounds
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 3 PlayMode tests pass

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

- [x] Tap wall/floor/ceiling selects; tap empty leaves unchanged; drag never selects
- [x] PlayMode tests: SEL AC1-AC3 via simulated taps; miss keeps selection; WindowDraw-mode tap swallowed (stub mode)
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 5 PlayMode tests pass

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

- [x] Panel lists 6 surfaces; selection change updates old + new rows in same frame; collapse/expand works
- [x] PlayMode tests: row states after Select/Clear; update-while-collapsed
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 4 PlayMode tests pass

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

- [x] Press with selection → deselected, tint removed, row updated; press with none → no event (spy on SelectionChanged)
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 2 PlayMode tests pass

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

- [x] Valid drag cuts a see-through hole; preview clamps live; all rejection paths remove preview without wall change
- [x] PlayMode tests: valid cut; tap-without-drag no window (EDGE-04); off-wall release clamps (EDGE-03); Clear cancels draw + exits mode + deselects (CLR AC3); mode-exit mid-drag cancels
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 6 PlayMode tests pass

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

- [x] Button visible for Wall+Orbit; hidden for none/Floor/Ceiling selected and in Ar/TopDown
- [x] PlayMode tests cover all 4 visibility cases
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 4 PlayMode tests pass

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

- [x] Full loop: cut → tap opening (X appears) → X (popup) → cancel (stays) / confirm (wall solid again, ray hits wall)
- [x] PlayMode tests: WIN-04 AC1-AC5 + selection unchanged on opening tap
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 6 PlayMode tests pass

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

- [x] Selected surface shows outline + tint from any angle
- [x] PlayMode test: selected surface still hittable (re-tap keeps selection, no deselect) - OUT AC2
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 2 PlayMode tests pass

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

- [x] AR mode ignores touch-drag camera input; selection taps still work; position clamped
- [x] PlayMode tests: pose-driven rotation applied (driven transform stub); clamp inside room; exit handoff preserves orientation
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 3 PlayMode tests pass

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

- [x] Toggle enters/exits AR mode; disabled when availability check fails
- [x] PlayMode tests: toggle enabled/disabled per availability; exit returns to Orbit
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 2 PlayMode tests pass

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

- [x] 2D entry cancels state, disables ceiling renderer+collider, frames fit-to-room; pinch zooms within limits and does nothing in 3D; 3D restores everything
- [x] PlayMode tests: TOP AC2, AC3, AC5, AC7, AC9 (incl. tap not blocked by ceiling)
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 5 PlayMode tests pass

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

- [x] Floor tap near wall selects nearest wall in 2D; same tap in 3D selects floor; floor tap far from walls selects floor in 2D
- [x] PlayMode tests cover those 3 cases
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 3 PlayMode tests pass

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

- [x] Buttons switch views; in 2D the list updates on plan selection and Clear works
- [x] PlayMode tests: switch both ways; list+Clear functional in 2D
- [x] Gate passes: run_tests EditMode + PlayMode
- [x] Test count: >= 3 PlayMode tests pass

**Tests**: PlayMode
**Gate**: full

**Commit**: `[feat] add 2D 3D view switch buttons`

---

### T30: Settle the window mode button's exit affordance + PlayMode test

**Done** - AD-019 plus AD-021 (pressing the button while window mode is active exits to Orbit), landed 2026-08-18.

**What**: The button stops appearing and disappearing. It stays on screen and carries its state visually: enabled while a Wall is selected in Orbit mode, disabled (art kit palette `E9FFF21A` border / `E9FFF205` fill / `4C6558` label) otherwise, with the state dot showing whether window mode is active. That supersedes AD-015's visibility rule, so `Refresh()` drives `Button.interactable` instead of `SetActive`, and the spec (WIN-01 AC1), `design.md`, T22 and the class comment are brought in line. AD-021 settled the open question: clicking while window mode is active exits to Orbit, so the button stays enabled in WindowDraw and the `OnClick` branch is reachable rather than dead.
**Where**: `Assets/Scripts/UI/WindowModeButton.cs`, `Assets/Tests/PlayMode/WindowModeButtonTests.cs`, `docs/tasks/T22-window-mode-button.md`, `design.md`
**Depends on**: None (root of Phase 6)
**Reuses**: T22 button, T13 ModeManager
**Requirement**: WIN-01 (AC1)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [x] `Refresh()` drives `interactable`, not `SetActive`; the button is never removed from the screen
- [x] PlayMode tests: the 4 cases from T22 assert enabled/disabled instead of visible/hidden, plus the state dot while mode == WindowDraw
- [x] No unreachable branch left in `WindowModeButton.OnClick`
- [x] WIN-01 AC1 returns to `Verified` in the spec's traceability table
- [x] Gate passes: run_tests EditMode + PlayMode

**Tests**: PlayMode
**Gate**: full

**Commit**: `[fix] align window mode button exit behaviour`

---

### T31: Expose the surface row's selected state as a field + update PlayMode tests

**What**: `SurfaceListPanel` marks the selected row by appending the `"  [SELECTED]"` suffix to the row label, and four PlayMode tests read that string. Move the state onto the row itself (a `SurfaceRow.IsSelected` property, or an equivalent dedicated field the tests can read) and have the tests assert that instead of parsing the label. Behaviour does not change - this is the seam that lets T32 restyle the row into the kit's green tag without touching a single test.
**Where**: `Assets/Scripts/UI/SurfaceListPanel.cs`, `Assets/Tests/PlayMode/SurfaceListPanelTests.cs`
**Depends on**: None (root of Phase 6)
**Reuses**: T19 panel + rows
**Requirement**: HUD-01 (AC3)

**Tools**:

- MCP: `unity-mcp` (run_tests)
- Skill: NONE

**Done when**:

- [x] Selected state readable without parsing the label text; label text is no longer the source of truth for selection
- [x] The four tests that read the label suffix assert the new field; LIST-01, LIST-02 and EDGE-06 coverage is unchanged
- [x] Gate passes: run_tests EditMode + PlayMode (152/152 baseline holds)

**Tests**: PlayMode
**Gate**: full

**Commit**: `[refactor] expose surface row selection as state`

---

### T32: Apply the Room Scanner HUD art kit + PlayMode raycast regression

**Done** - landed 2026-08-18. Three things the plan did not anticipate are recorded as AD-022 (Readout and HintPill
carry live data), AD-023 (`vertexColorAlwaysGammaSpace` on the HUD canvas) and AD-024 (the -3x set ships from
`Assets/Resources/HUD/`, the kit folder stays untouched reference).

**What**: Replace the placeholder grey boxes with the imported kit: set each sprite's import settings per `Assets/Sprite/Game UI mockups for Unity/Unity-handoff.md` (Sprite (2D and UI), Full Rect, the listed 9-slice borders, Pixels Per Unit matched to the -Nx variant shipped), narrow ProjectSettings to landscape and set the CanvasScaler for it (AD-020 - match the kit's art, the reference resolution itself is free), and build the panel, rail, buttons, rows, readout and overlays with the kit's sprites, colours and TMP settings. Decorative images (scanlines, glow, borders, dividers) get Raycast Target off. One thing in the kit's copy is not adopted: the Clear button asking for confirmation, which CLR-01 does not have (HUD-01 AC5 keeps the spec rule). The kit's other implied behaviour, the disabled-not-hidden window mode button, is now the spec rule via AD-019 - T30 implements it and this task only paints it, including its state dot.
**Where**: `Assets/Sprite/Game UI mockups for Unity/sprites/*.png.meta`, `Assets/Scripts/UI/*.cs` (view construction), `ProjectSettings/ProjectSettings.asset`, `Assets/Tests/PlayMode/`
**Depends on**: T30, T31
**Reuses**: T19 panel, T20/T22/T26/T29 buttons, T23 window overlay
**Requirement**: HUD-01 (AC1, AC2, AC4, AC5)

**Tools**:

- MCP: `unity-mcp` (UI, manage_asset, run_tests)
- Skill: `unity-mcp-skill`

**Done when**:

- [x] Every sprite the HUD uses carries the handoff's import settings; no sprite left on Unity defaults
- [x] App is landscape-only in ProjectSettings; the HUD reproduces the kit's layout, proportions and palette
- [x] PlayMode regression: a tap landing on a decorative image (scanline overlay, glow) still selects the surface behind it (EDGE-02 stays honest), and the paired case proves opaque HUD does stop it
- [x] No acceptance criterion outside HUD-01 changes behaviour; full suite still green
- [x] Gate passes: Unity MCP console zero compile errors + run_tests EditMode 67/67 + PlayMode 92/92

**Tests**: PlayMode
**Gate**: build

**Commit**: `[feat] apply room scanner hud art kit`

---

## Phase 7: HUD in the scene

### T33: Author the HUD into the scene + EditMode bake tests

**Done** - landed 2026-08-18, on the owner's request. Recorded as AD-025.

**What**: Take the HUD out of the play-time object graph and put it in `Main.unity` as real GameObjects - canvas, panels, `Button`s and `TextMeshProUGUI` labels the inspector can select and edit - without changing how any of it looks. The kit description stays in code and becomes bake-time: `HudRoot.Build` assembles the tree, `Formify/Bake HUD Into Scene` (`HudSceneBaker`) runs it once on a throwaway scaffold, lifts the root into the scene and wires it into `RoomBootstrap.hud`, and every view serializes the parts it used to assign at construction so the baked copy paints itself. `RoomBootstrap.Compose` no longer builds any view - it binds the model, the modes and the handlers to what the scene holds, and falls back to `HudRoot.Build` only for a scene that has no HUD (a bare test scene), which keeps baked and built the same tree by construction. The room and the windows are untouched: still generated (ROOM-01, WIN-02). The short-lived `HudScenePreview` - a baked copy that deleted itself on play, from when the live HUD was still built in code - is deleted.
**Where**: `Assets/Scripts/UI/HudRoot.cs` (new), `Assets/Scripts/UI/{HudButton,SurfaceListPanel,HudReadout,HudHintPill,ViewSwitchButtons,WindowModeButton}.cs`, `Assets/Scripts/Presentation/RoomBootstrap.cs`, `Assets/Scripts/Editor/HudSceneBaker.cs` (new), `Assets/Scenes/Main.unity`, `Assets/Tests/EditMode/HudSceneBakerTests.cs`
**Depends on**: T32
**Reuses**: the whole kit construction - `HudTheme`, `HudButton`, `SurfaceListPanel`, `SurfaceRow`, `HudReadout`, `HudHintPill`, `ViewSwitchButtons` - run unchanged by the bake
**Requirement**: HUD-01 (AC1, AC2), AD-025

**Tools**:

- MCP: `unity-mcp` (execute_menu_item, manage_scene, refresh_unity, read_console)
- Skill: `unity-mcp-skill`

**Done when**:

- [x] `Main.unity` holds `HUD/FormifyCanvas` with `SurfacesPanel`, `RightRail` (window mode, Clear, AR), `ViewToggle`, `Readout`, `HintPill` and `Scanlines` as editable GameObjects, plus a scene-owned `EventSystem`
- [x] Play builds no HUD: `Compose` only binds, and the scene's `HudRoot` is wired into `RoomBootstrap.hud`
- [x] Every view reference survives serialization, including the ones a delegate used to carry - the header's collapse click and the window mode button's `onClick` are re-wired on bind
- [x] The bake leaves no scaffold, camera or bootstrap behind, and reaches into no scene but the active one
- [x] `HudScenePreview` (the self-deleting preview) is gone, and the menu reads `Formify/Bake HUD Into Scene` / `Formify/Remove HUD From Scene`
- [ ] Gate: EditMode + PlayMode suites - **not run; the owner asked for the test runner to be held**

**Tests**: EditMode
**Gate**: build (compile verified through Unity MCP: forced refresh + compile, console clean, and the bake itself ran)

**Commit**: `[feat] author the hud into the scene` (split across five commits: serialization, view root, editor bake, scene data, docs)

---

## Phase 8: windows in the list

### T34: Windows as nested rows in the surfaces list (B1) + tests

**Done** - landed 2026-08-18. Brought one decision with it, AD-026 (one selection, two kinds).

**What**: A window is a row too (LIST-03). `RoomModel` gains `SelectedWindowId`, `SelectWindow` and `GetWindow`, and selecting a surface or a window clears the other, so "the selected row" is never two rows (AD-026); removing the selected window clears the selection before `WindowRemoved` goes out, so no view paints a window that is gone. `SurfaceListPanel` draws each window indented under the wall it was cut into, live on `WindowAdded` / `WindowRemoved`, renumbers a wall's windows when one is deleted, and gives every wall row a disclosure control - hidden until that wall carries a window - that folds its windows away without touching the wall row or any binding. `SurfaceRow` grew the two variants: indented and index-less for a window, plus the disclosure dot for a wall. The readout follows the window selection too, so selecting a window from the list no longer leaves it reading "NO SURFACE".
**Where**: `Assets/Scripts/Domain/RoomModel.cs`, `Assets/Scripts/UI/SurfaceListPanel.cs`, `Assets/Scripts/UI/SurfaceRow.cs`, `Assets/Scripts/UI/HudReadout.cs`, `Assets/Tests/EditMode/RoomModelWindowSelectionTests.cs`, `Assets/Tests/PlayMode/SurfaceListWindowRowsTests.cs`, `Assets/Scenes/Main.unity` (re-baked)
**Depends on**: T33
**Reuses**: `SurfaceRow` for both row kinds, the panel's existing selection binding and tap gate, the header's dot vocabulary for the disclosure control (the kit ships no chevron)
**Requirement**: LIST-03, AD-026

**Tools**:

- MCP: `unity-mcp` (refresh_unity, read_console, execute_menu_item, manage_scene)

**Done when**:

- [x] A placed window appears as an indented row directly under its wall, in model order; deleting one removes its row and renumbers the rest
- [x] Every wall row with at least one window offers the disclosure control; folding hides that wall's windows only, and the wall row stays
- [x] Tapping a window row selects the window, clears the surface selection and marks the window row - and the reverse holds
- [x] The panel's tap gate (AD-015) covers window rows, so nothing selects while a window is being drawn
- [x] The readout reports the selected window instead of falling back to the empty state
- [ ] Gate: EditMode + PlayMode suites - **not run; the owner asked for the test runner to be held**

**Tests**: EditMode (`RoomModelWindowSelectionTests`, 9 cases), PlayMode (`SurfaceListWindowRowsTests`, 10 cases)
**Gate**: build (compile verified through Unity MCP with a clean console; the bake re-ran and the wall rows carry their hidden disclosure control)

**Commit**: `[feat] list windows under their wall`

**Follow-up, same day** (owner tried it): the wall row itself folds its windows - the 6 px dot is an indicator now, not a control, because it carved a dead spot out of the row - and B3 landed with it as AD-027, placing a window returns to Orbit. That last one was a defect as much as a feature: the list is handed a tap gate that blocks selection for the whole of WindowDraw (AD-015), so a mode that outlived the placement left the freshly added window row refusing every tap. The panel also grows with its rows now, capped above the readout; the kit-sized 250 x 312 was full at six surfaces and the first window spilled out of it.

**Not in this task**: a tap on the window itself in 3D still opens the delete affordance and does not select (that is B2's remaining half).

---

### T35: Fly the camera between 2D and 3D (B5)

**Done** - landed 2026-08-18, in parallel with T36 and T37.

**What**: The plan view opens further back and the switch animates both ways (TOP-01 AC7, AC10). `PlanOrthographicSize` is the bare fit scaled by a serialized `planZoomOut` (1.25) and clamped into the same limits the pinch obeys, so the opening size sits honestly inside them. `BeginTransition` reads the current pose, runs the real snap, reads the result back as the target and rewinds - so the flight lands on exactly the pose the code snapped to before, and a switch arriving mid-flight retargets from mid-air instead of restarting. The flight is flown in the 3D projection in both directions, because an orthographic camera at eye height inside the room renders nothing readable. Camera input is dropped while it flies: pinch early-returns and `OrbitCameraController` gained an `InputLocked` flag, needed because on the way out the mode is already Orbit and `RoomBootstrap` would route drags into a camera still in the air.
**Where**: `Assets/Scripts/Presentation/TopDownController.cs`, `OrbitCameraController.cs`, `Assets/Tests/PlayMode/TopDownControllerTests.cs`, `OrbitCameraControllerTests.cs`
**Depends on**: T27, T29
**Requirement**: TOP-01 (AC7, AC10)

**Done when**:

- [x] The plan opens at 1.25x the bare fit, inside the pinch clamps
- [x] Both directions interpolate over 0.35 s with `SmoothStep`, driven off `Time.deltaTime` in `LateUpdate`
- [x] The mode change stays instant - ceiling, selection cancel and toggle highlight fire on `ModeChanged` as before
- [x] The flight lands exactly on the old snap pose, and a mid-flight switch retargets rather than snapping
- [x] Pinch and orbit drags do nothing while it flies
- [ ] Gate: suites not run - the owner asked for the runner to be held

**Tests**: PlayMode - 8 new cases (`B5_*` in `TopDownControllerTests`, `InputLocked_*` in `OrbitCameraControllerTests`); 3 existing cases now step frames to the landing instead of asserting on the next frame.

**Commit**: `[feat] fly the camera between 2D and 3D`

---

### T36: Widen the room to 9 x 5 m (B6)

**Done** - landed 2026-08-18.

**What**: The room was 6 x 4 m; the owner asked for bigger and more rectangular. 9 x 5 (1.8:1) in both the `RoomBootstrap` default and the serialized value on the scene's `Room` object - the serialized one is what runs, so the default alone would have changed nothing. Height and thickness unchanged.
**Where**: `Assets/Scripts/Presentation/RoomBootstrap.cs`, `Assets/Scenes/Main.unity`, `Assets/Tests/PlayMode/RoomBootstrapTests.cs`, `RoomBootstrapDragRoutingTests.cs`, `Assets/Tests/EditMode/RoomBuilderTests.cs` (comment)
**Requirement**: ROOM-01 (assumption change, spec Assumptions table)

**Done when**:

- [x] `roomSize` is 9 x 5 in the code default and in the scene
- [x] The two fixtures that assert the app's own room moved with it - Wall 3 changed plane, so the drag-routing fixture's world points and expected wall-local rect moved
- [x] Nothing camera-side needed changing: the orbit rig clamps off `RoomBounds`, the plan fits off the same extents, and window size limits are wall-local
- [ ] Gate: suites not run - the owner asked for the runner to be held

**Commit**: `[feat] widen the room to 9 x 5 metres`

---

### T37: Select a window, orbit from a drag over one, and dress the world (B2, B4)

**Done** - landed 2026-08-18. Brought AD-028 with it.

**What**: Three things.
*B2* - a tap on a window selects it (`WindowView.OnTapped` -> `RoomModel.SelectWindow`), which clears the surface selection under AD-026, and the delete affordance follows the *selected* window instead of the tapped one. That is what the owner asked for from the list side: picking a window row shows its X exactly as tapping the opening does. The selected opening is outlined with the kit's `window_border_9s` in the accent, a screen-space rect over the projected corners, re-projected each frame beside the X and never a raycast target (HUD-01 AC4). OUT-01's outline could not be reused - it is a layer swap on a surface mesh and an opening has no renderer.
*B4* - drag routing was gated on the mode rather than the gesture, so in window mode every delta went to the draw controller even when no draw had started; a drag beginning over an existing window raycasts onto that window's collider, never starts a draw, and the camera got nothing. It gates on `IsDrawing` now, which is strictly narrower than the mode, so nothing that used to draw stops drawing. AR keeps its pose, TopDown keeps its pinch.
*The world* - the room now sits in the same environment as the wardrobe configurator in `3D Test`: camera clears to that project's exact background (`040A07`), ambient is Flat 0.28 instead of a procedural sky, and a 50 x 50 m plane 2 cm under the floor slab carries its `DemoFloor` material. The plane has no collider, so it can never take a tap from a surface.
**Where**: `Assets/Scripts/Presentation/WindowView.cs`, `SelectionController.cs` (comments), `RoomBootstrap.cs` (routing), `Assets/Scenes/Main.unity`, `Assets/Materials/Ground.mat`, and the PlayMode fixtures for drag routing, selection and window views
**Requirement**: SEL-03 AC7 (rewritten), WIN-01 AC14, AD-026, AD-028

**Done when**:

- [x] A tap on a window selects it and clears the surface selection; a wall tap afterwards clears the window
- [x] The delete X and the accent border follow the selection, from the room or from the list, and leave when it moves
- [x] A drag starting over a window in window mode turns the camera and cuts nothing
- [x] Black sky, grey floor, no collider under the room
- [ ] Gate: suites not run - the owner asked for the runner to be held

**Tests**: PlayMode - `InWindowDraw_ADragStartingOverAWindowTurnsTheCameraInstead`, `Tapping_a_window_selects_it_and_clears_the_surface_selection`, `Tapping_a_wall_after_a_window_clears_the_window_selection`, `OnTapped_SelectsTheWindowAndTheDeleteAffordanceFollows`, `MovingTheSelectionElsewhere_TakesTheDeleteAffordanceWithIt`, `SelectedWindow_IsOutlinedOverTheOpeningInTheAccent`, plus one renamed routing-precedence case.

**Commit**: `[feat] select a window by tapping it and orbit from a drag over one`, `[raw] set the room's world to the configurator's black sky and grey floor`

---

### T38: Owner polish pass — accent selection, bigger windows, live draw readout, floor out of the plan

**Done** - landed 2026-08-18. Four asks from the owner after trying Phase 8, landed as four commits. Brought
AD-029 with it.

**What**:
1. *Selection reads in the kit accent* (AD-029). The OUT-01 ring and the SEL-02 tint are both `HudTheme.Accent`,
   so a selected wall and a selected window finally speak the same colour. The tint is composited in C# at
   `SelectedRowFill`'s alpha because the surface material is opaque URP Lit and drops `_BaseColor`'s alpha - a
   direct assignment would have painted the wall solid neon. The window's screen-space border was evaluated for
   surfaces and rejected; the reasoning is the whole of AD-029.
2. *Windows may be big* - the per-axis maximum went from 2.0 m to 6.0 m. A 2 m cap made a picture window
   impossible on a 9 m wall. The clamp into the wall bounds minus the edge margin still bounds every rectangle,
   so the maximum only refuses the absurd.
3. *The readout reports the drag* (WIN-01 AC15). `WindowDrawController` raises one event with `(surfaceId, rect)`
   on start, on every move, and once from `CancelDraw` where the id is already `-1` - so the same event carries
   the end of the draw and the panel falls straight back to the selection. The caption reads `DRAWING`, not the
   wall's name, so the panel never labels a size that is not the labelled thing's size.
4. *The floor leaves the plan view* (TOP-01 AC2). From straight above it was a grey lid over the room. Renderer
   only: TOP-02 AC8 picks a wall out of a tap that lands on the floor within tolerance, so the collider stays -
   the same trap AD-012 recorded for the ceiling, in reverse.

**Follow-up, same day**: the owner set the plan camera by eye at 5.2 m of orthographic half-height (it wins over the 1.25x pull-back unless the room is too big for it), and asked for the selected wall's green to be darker - the tint is now the accent at 0.55 strength, shaded to 0.62, so a selected surface reads as a deep green instead of a pale wash.

**Where**: `Assets/Scripts/Presentation/SurfaceView.cs`, `TopDownController.cs`, `WindowDrawController.cs`,
`RoomBootstrap.cs`, `Assets/Scripts/UI/HudReadout.cs`, `Assets/Scripts/Domain/WindowPlacementValidator.cs`,
`Assets/Settings/SelectionOutline.mat`, and the PlayMode fixtures for surfaces, outline, top-down and window
drawing, plus the EditMode validator fixture
**Depends on**: T34, T35, T37
**Requirement**: SEL-02, OUT-01, WIN-01 AC15, WIN-03 (max size), TOP-01 AC2, AD-029

**Done when**:

- [x] A selected surface is ringed and tinted in the kit accent, and the outline material's own default matches
      so the asset and the property block cannot disagree
- [x] A 6 x 2 m opening is legal on a long wall; anything past 6 m per axis is still refused
- [x] The readout shows the live rectangle while drawing and returns to the selection the moment the drag ends
- [x] The plan view hides the floor's renderer and keeps its collider
- [ ] Gate: suites not run - the owner asked for the test runner to be held

**Tests**: PlayMode - `TintColour_IsTheKitSelectedGreen_OverTheSurfacesOwnColour`,
`The_outline_colour_comes_from_the_kit_accent`, `Entering2D_HidesTheFloorRenderer_ButKeepsItsCollider`,
`Readout_reports_the_live_rectangle_while_the_drag_grows`, `Readout_returns_to_the_wall_after_a_placement`,
`Readout_returns_to_the_wall_after_a_rejection_and_after_a_cancel`,
`Readout_keeps_the_live_rectangle_invariant_culture`. EditMode -
`ALargePictureWindow_FitsUnderTheShippedMaximum`, plus the two rejection cases moved onto a 9 m wall.

**Commit**: `[feat] paint the selected surface in the kit accent`, `[feat] allow windows up to 6 metres per axis`,
`[feat] report the rectangle under the finger while a window is drawn`,
`[feat] take the floor out of the plan view's render`

**Open**: `Settings/SelectionOutline.mat` now carries the accent as its asset default as well as through the
property block. If UAT ever shows an orange ring, that file is the first place to look - the two paths were made
to agree rather than proven to agree, because proving it needs play mode.

---

## Phase Execution Map

```
Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 5 → Phase 6 → Phase 7 → Phase 8

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
Phase 6:  T30 → T32
          T31 → T32
Phase 7:  T32 → T33
Phase 8:  T33 → T34 → T35
          T34 → T36
          T34 → T37
          T35, T37 → T38
```

Execution was strictly sequential through T34. 38 tasks total, all done. T35, T36 and T37 ran in parallel - one sub-agent each, because they share no file; T37 was held back until T36 released `RoomBootstrap.cs`. Phases 1-5 (29 tasks) were verified in `validation.md`; Phases 6 and 7 fitted a single batch each and ran inline; Phase 8 fanned out to three sub-agents on the owner's instruction.

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
| T30 | 1 file modify + tests (+ doc corrections) | ✅ Granular |
| T31 | 1 file modify + tests | ✅ Granular |
| T32 | sprite import settings + UI construction (one cohesive visual pass) | ✅ OK (cohesive) |

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
| T30 | prior phases only | (root of Phase 6) | ✅ Match |
| T31 | prior phases only | (root of Phase 6) | ✅ Match |
| T32 | T30, T31 | T30→T32, T31→T32 | ✅ Match |

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
| T30 | UI modify | PlayMode | PlayMode | ✅ OK |
| T31 | UI modify | PlayMode | PlayMode | ✅ OK |
| T32 | UI + sprite config | PlayMode | PlayMode | ✅ OK |

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
| WIN-01 | T21, T22, T30 |
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
| HUD-01 | T31, T32 |

25/25 requirements mapped.

---

## Phase 8 backlog - requested 2026-08-18, not yet specified

The owner asked for these after T33. B1 has since been specified as LIST-03 and shipped as T34; the rest are
still intent, not tasks - no acceptance criteria yet, and two of them move rules other requirements already own
(flagged below). Specify before implementing.

| # | Asked for | Touches | Open before it can be a task |
| - | --------- | ------- | ---------------------------- |
| B1 | A window created on a wall appears in the surfaces list as a child row under that wall; the wall row can collapse its windows, and a window can be selected from the list | LIST-01, LIST-02, `SurfaceListPanel`, `SurfaceRow` | **Done - T34**, specified as LIST-03. Both row kinds are the same `SurfaceRow`; per-wall collapse sits beside the panel-wide one |
| B2 | Selecting a window behaves like selecting a wall: it clears every other selection, and the list marks the window row selected | SEL-01, LIST-01, `RoomModel.SelectedSurfaceId` | **Done - T37** (model half in T34): the model carries `SelectedWindowId` and the two selections exclude each other (AD-026), and the list honours it. The 3D side landed in T37 under AD-028: the tap selects, and the opening is outlined in the accent since OUT-01 could not be reused |
| B3 | Window mode switches itself off after each window is placed | WIN-01 AC1, AD-019, AD-021 | **Done - T34 follow-up**, recorded as AD-027. The button stays the exit when nothing was placed; a rejected rectangle keeps the mode |
| B4 | A drag that starts inside an existing window still orbits the camera | SEL-03, WIN-02, `WindowDrawController`, `RoomBootstrap.OnDragDelta` | Windows already route drags for their own gesture; which mode owns a drag that begins over a window has to be stated per mode (Orbit vs WindowDraw) |
| B5 | 2D view pulls the camera further back, and the switch between 2D and 3D is animated in both directions | TOP-01, `TopDownController`, `OrbitCameraController` | **Done - T35**, TOP-01 AC7 and AC10: 1.25x pull-back, 0.35 s SmoothStep both ways, camera input dropped while it flies |
| B6 | The room is bigger and more rectangular | ROOM-01 assumptions, `RoomBootstrap.roomSize` | **Done - T36**: 9 x 5 m, in both the `RoomBootstrap` default and the serialized value on the scene's Room object. Nothing camera-side needed changing - the orbit rig clamps off `RoomBounds` and the plan view fits off `_roomBounds.extents`, so both reframed themselves - and the window limits are wall-local, so only the walls got longer |
