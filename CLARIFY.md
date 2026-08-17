# Clarifications

Open questions raised while executing `.specs/features/room-wall-selection/tasks.md`. Each entry states the assumption applied so work is not blocked. Answer inline (edit the **Answer** line) and the assumption will be revisited.

---

## C-01 — Unity project lives in `Build/FormifyTest`, not at the repository root

**Raised by**: T01
**Context**: BOOT-01 AC1 says "A Unity 6 project SHALL exist at the repository root". The actual project is at `Build/FormifyTest/` (Unity 6000.3.11f1, URP template, already open in the Editor).
**Assumption applied**: use the existing project in place. No relocation, no second project. All task paths (`Assets/...`, `Packages/...`, `ProjectSettings/...`) resolve under `Build/FormifyTest/`.
**Answer**:

## C-02 — Unity MCP is not connected in this session

**Raised by**: T01 (affects every gate)
**Context**: The project has `com.coplaydev.unity-mcp` installed, but no Unity MCP tools are exposed to the agent in this session, so `run_tests` is unavailable. AD-006 fallback applies: the agent writes all text assets, verification happens in the Editor.
**Assumption applied**: gates run through the Unity CLI in batch mode (`-runTests`) against a mirror copy of the project kept in the session scratchpad, because the running Editor holds the project lock. Test results are read from the produced NUnit XML. The live Editor's console is read from `Editor.log`.
**Answer**:

## C-03 — Sub-agent delegation declined by standing instruction

**Raised by**: Execute (pre-task check)
**Context**: 29 tasks pack into ~5 batches, so `tlc-spec-driven` requires offering sub-agent delegation before Execute. The session's standing instruction is "do not call the Agent tool unless the user requested it".
**Assumption applied**: everything is executed inline, one task at a time, in the documented order. The always-on Verifier at the end of Execute also runs inline as a fresh-eyes pass (`validate.md` standalone fallback).
**Answer**:

## C-04 — `.gitignore` added outside any task

**Raised by**: T01
**Context**: The repository has no `.gitignore`. Committing the Unity project without one would commit `Library/`, `Temp/`, `Logs/` and the generated `.csproj`/`.slnx` files (gigabytes of derived data).
**Assumption applied**: a standard Unity `.gitignore` was added in the T01 commit.
**Answer**:

## C-05 — Unconfirmed tuning defaults from the spec

**Raised by**: Phase 2 onward
**Context**: `spec.md` marks these rows `Confirmed? n`: pitch clamp (-60°..+60°), tap threshold (20 px / 300 ms), room size (6 m x 4 m x 2.8 m), surface thickness (0.15 m), min window (0.2 m), max window (2.0 m), edge margin (0.1 m), surface naming, AR validation via XR Simulation, window rectangles axis-aligned.
**Assumption applied**: the stated defaults are implemented as serialized fields, so changing them is an Inspector edit, not a code change.
**Answer**:

## C-07 — T02 was already satisfied by the URP template

**Raised by**: T02
**Context**: T02 asks for a URP pipeline asset plus a Universal Renderer asset that reference each other. The project was created from the URP template, which already ships `Assets/Settings/PC_RPAsset` → `PC_Renderer` and `Mobile_RPAsset` → `Mobile_Renderer`, both correctly cross-referenced, plus `UniversalRenderPipelineGlobalSettings`.
**Assumption applied**: no new assets created. The existing pair is used as-is; `Mobile_Renderer` is the renderer that will host the T24 outline features, since iOS is the shipping target.
**Answer**:

## C-08 — RoomBootstrap became the composition root

**Raised by**: T20 (and extended in T23, T29)
**Context**: The task list has no scene-assembly task beyond T16. Each controller and UI class is authored with a
`Configure(...)` entry point, but nothing in the task list creates or wires them at runtime, so the app would
show the room and nothing else.
**Assumption applied**: `RoomBootstrap` composes the whole runtime graph in `Awake` (input router, orbit camera,
selection, list panel, Clear, window mode button, window draw, window view factory, AR controller and toggle,
top-down controller, 2D/3D buttons). `Main.unity` therefore holds a single `Room` object. A serialized
`buildRuntimeComposition` flag switches the composition off for tests that want the bare room.
**Answer**:

## C-09 — Pinch is read by TopDownController, not by InputRouter

**Raised by**: T27
**Context**: TOP-02 AC9 needs a two-finger pinch, but InputRouter deliberately tracks only the primary touch
(EDGE-01), so a second finger never reaches a controller through it.
**Assumption applied**: `TopDownController` reads the two-finger distance itself in `Update` while the plan is
active and converts it into the fit units `ApplyPinch` expects (a spread of one screen height is one fit unit).
InputRouter is unchanged.
**Answer**:

## C-10 — AR session rig created at runtime

**Raised by**: T25
**Context**: T25 says the scene gains an ARSession and an XROrigin, which was a Unity MCP step. Without them
nothing produces a device pose and AR mode is inert, even though the controller and its tests are correct.
**Assumption applied**: `RoomBootstrap` creates the AR rig (ARSession + ARInputManager + XROrigin with a
non-rendering tracking camera driven by TrackedPoseDriver) the first time `ArSessionStartRequested` fires, and
disables it on `ArSessionEndRequested`. The room camera keeps rendering the synthetic room; AR only supplies the
pose. This path needs a human check in the Editor with XR Simulation: the automated tests inject a pose instead.
**Answer**:

## C-11 — Window mode has no visible exit affordance

**Raised by**: T22
**Context**: AD-015 hides the window mode button whenever the mode is not Orbit, so the button disappears the
moment window mode is entered. The documented way out is the Clear button (CLR-01 AC3), which also clears the
selection.
**Assumption applied**: implemented exactly as specified. If the intended UX is a visible toggle that stays on
screen while drawing, AD-015 needs revisiting.
**Answer**:

## C-12 — Edge margin is configured in two places

**Raised by**: T21
**Context**: `WindowPlacementValidator.EdgeMargin` (0.1 m) is the rule, but `WindowDrawController` needs the same
number to clamp the live preview, and the validator instance is private to `RoomModel`.
**Assumption applied**: the controller carries its own serialized `edgeMargin` that mirrors the validator's
value; the validator still has the final word on release. Tuning one without the other only makes the preview
disagree with the accepted rectangle, never accepts an invalid window.
**Answer**:

## C-13 — Task execution order deviated inside Phase 3

**Raised by**: T14
**Context**: The documented order is T13 → T14 → T15 … ; T14 (InputRouter) was authored in parallel and landed
after T15-T19.
**Assumption applied**: committed in dependency order rather than numeric order. T14 depends only on T13, and no
task that depends on T14 (T17, T18) was gated before it landed.
**Answer**:

## C-14 — README is a deliverable with no task

**Raised by**: after T29
**Context**: `docs/ideia.md` lists a readme explaining architecture, AI method, organisation, adjustments and
business rules as a deliverable, but the 29 tasks never cover it.
**Assumption applied**: written as `README.md` at the repository root after the last task and committed
separately, since it documents the finished system rather than any single task.
**Answer**:

## C-06 — Unity template packages left in the manifest

**Raised by**: T01
**Context**: The project was created from a Unity template and carries packages no requirement needs: `com.unity.ai.navigation`, `com.unity.multiplayer.center`, `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.collab-proxy`.
**Assumption applied**: left untouched. T01 only adds what BOOT-01 requires (AR Foundation 6.4.3 + ARKit 6.4.3, matching). Removing them is a separate cleanup decision.
**Answer**:
