# Unity iOS Mini-Task — Room & Wall Selection

A Unity 6 (URP) iOS-exportable app that renders a synthetic room, lets you select any surface by tapping it,
cut and delete real window openings in walls, look around with the device pose in AR, and switch to a 2D
top-down plan.

The Unity project lives in [`Build/FormifyTest`](Build/FormifyTest). Open it with **Unity 6000.3.11f1** and
press Play on `Assets/Scenes/Main.unity`.

---

## What it does

| Capability | Where it lives |
| ---------- | -------------- |
| Synthetic room, N ≥ 3 walls plus floor and ceiling, solid slabs of 0.15 m | `RoomBuilder`, `SurfaceMeshBuilder`, `RoomBootstrap` |
| First-person orbit: drag to turn, pitch clamped, camera stays inside the room | `InputRouter`, `OrbitCameraController` |
| Tap to select exactly one surface, with tint feedback | `SelectionController`, `RoomModel`, `SurfaceView` |
| Live list of every surface and its state, collapsible | `SurfaceListPanel` |
| Clear button, the only path back to nothing selected | `ClearButton` |
| Window openings drawn as a rectangle on a wall, cut through the solid mesh | `WindowDrawController`, `WindowPlacementValidator`, `SurfaceMeshBuilder` |
| Window deletion: tap the opening → X → confirm | `WindowView`, `WindowViewFactory` |
| Selection outline on top of the tint | `SelectionOutline.shader` plus two URP RenderObjects passes |
| AR mode: device pose drives the camera | `ArPoseCameraController`, `ArToggleButton` |
| 2D plan with fit-to-room framing, pinch zoom and wall-tap tolerance | `TopDownController`, `ViewSwitchButtons` |

---

## Architecture

Light MVC/MVP. A plain-C# domain layer holds the state and the rules and raises `event Action`; MonoBehaviour
views and controllers subscribe and render. Input flows one way — router → controller → domain — and domain
events flow back to the views. No DI framework, no ScriptableObject event system (AD-002).

```
Assets/Scripts/
├── Domain/            Formify.Domain assembly — no scene dependency, EditMode-testable
│   ├── DomainTypes.cs             SurfaceKind, Rect2D, SurfaceDefinition, WindowSpec, MeshData, Mode…
│   ├── RoomModel.cs               single selection + windows + events, the source of truth
│   ├── RoomBuilder.cs             footprint polygon → N walls + floor + ceiling
│   ├── SurfaceMeshBuilder.cs      solid slab with real holes and reveal faces
│   └── WindowPlacementValidator.cs  every window placement rule, in one place
├── Presentation/      Formify.Presentation assembly
│   ├── RoomBootstrap.cs           composition root: builds the room and wires everything
│   ├── ModeManager.cs             Orbit / WindowDraw / Ar / TopDown state machine
│   ├── InputRouter.cs             the single touch doorway, tap vs drag decided once
│   ├── SurfaceView.cs             mesh + collider + tint per surface
│   ├── SelectionController.cs     tap resolution order
│   ├── OrbitCameraController.cs   yaw free, pitch clamped, inside the room
│   ├── WindowDrawController.cs    live preview and placement
│   ├── WindowView.cs              opening collider, X button, confirmation popup
│   ├── ArPoseCameraController.cs  device pose → camera rig
│   └── TopDownController.cs       2D plan, ceiling off, pinch zoom
└── UI/                same assembly, uGUI + TextMeshPro built in code
    ├── SurfaceListPanel.cs, ClearButton.cs, WindowModeButton.cs
    ├── ArToggleButton.cs, ViewSwitchButtons.cs
```

**Why the domain is separate.** Selection semantics, window validation and the slab-with-holes geometry are the
parts that are easy to get subtly wrong, so they are plain C# with no `MonoBehaviour` and no scene: they run in
EditMode tests in milliseconds. Everything that genuinely needs a scene — colliders, raycasts, uGUI — is tested
in PlayMode.

**Composition root.** `Main.unity` holds a single `Room` object carrying `RoomBootstrap`. On `Awake` it builds
the room, creates the model and the mode machine, then creates and wires every controller and UI element in
code. Nothing depends on hand-wired inspector references, which is why the whole app is reproducible from a
one-object scene.

---

## Method: spec-driven development

The feature was specified before it was written, and the specification is in the repository.

| Phase | Artifact |
| ----- | -------- |
| Specify | [`.specs/features/room-wall-selection/spec.md`](.specs/features/room-wall-selection/spec.md) — 24 requirements in EARS notation, plus every assumption and its default |
| Design | [`.specs/features/room-wall-selection/design.md`](.specs/features/room-wall-selection/design.md) — components, data models, error handling, risks |
| Adversarial review | the "grill" pass that produced AD-014, AD-015 and AD-016 |
| Tasks | [`.specs/features/room-wall-selection/tasks.md`](.specs/features/room-wall-selection/tasks.md) — 29 atomic tasks, dependencies, gates, coverage matrix; one detailed document each in [`docs/tasks/`](docs/tasks) |
| Execute | one commit per task, each behind a green test gate |

Project-level decisions are logged as AD-001…AD-018 in [`.specs/STATE.md`](.specs/STATE.md). Questions raised
while implementing were answered by the project owner and folded into the spec and the decision log; the
independent verification result is in
[`.specs/features/room-wall-selection/validation.md`](.specs/features/room-wall-selection/validation.md).

**AI method.** The work was executed by an agent following the spec-driven skill: requirements first, then a
design, then an adversarial review of that design, then atomic tasks with explicit dependencies, then
implementation one task at a time. Authoring of independent classes was fanned out to parallel sub-agents, each
given the same binding API contract; gates and commits stayed sequential so every commit is a verified step.
Tests were derived from the acceptance criteria, never from the implementation.

---

## Business rules worth knowing

- **Exactly one surface is selected at a time.** Tapping the selected surface again changes nothing and raises
  no event. The Clear button is the only way back to the empty state — tapping empty space does not clear.
- **Windows belong to walls.** The floor and ceiling are selectable but reject window placement
  (`InvalidSurfaceKind`).
- **A window is a real hole.** The wall is a solid slab; the opening is cut through both faces and gets four
  reveal faces. The MeshCollider is reassigned in the same operation as the renderer mesh, so a ray through an
  opening hits nothing instead of an invisible wall.
- **Placement rules**, in order: non-wall → rejected; the rectangle is clamped into the wall bounds minus a
  0.1 m edge margin; smaller than 0.2 m → rejected; larger than 2.0 m → rejected; overlapping an existing
  window → rejected. Every rejection drops the preview and leaves the wall untouched.
- **Mode transitions are explicit.** Orbit is the hub. WindowDraw is reachable only from Orbit and only with a
  wall selected; AR only from Orbit; the 2D plan from anywhere, and it exits only to Orbit. Illegal transitions
  return false and change nothing.
- **Entering the 2D plan is a clean slate**: the selection is cleared, an in-progress window draw is cancelled,
  and the ceiling loses both its renderer and its collider — hiding only the renderer would leave an invisible
  ceiling swallowing every plan tap.
- **The plan assists the finger.** A wall seen edge-on is about 10 px wide, so a floor tap within 30 px of a
  wall selects that wall. Pinch zoom is limited to 0.5×–2.0× of the fit-to-room framing, and does nothing in the
  3D views.
- **AR only drives the pose.** The synthetic room stays the world; rotation always comes from the device and the
  position is clamped inside the room. Leaving AR hands the orientation to the orbit camera so the view does not
  snap. Where AR is unavailable the toggle is disabled and touch keeps working.

Tuning values (room size, thickness, pitch clamp, tap thresholds, window limits, tolerance, zoom limits) are all
serialized fields — changing them is an Inspector edit, not a code change.

---

## Tests

| Suite | What it covers |
| ----- | -------------- |
| EditMode (`Assets/Tests/EditMode`) | domain logic: selection semantics, window validation and operations, slab meshing, room generation, the mode matrix |
| PlayMode (`Assets/Tests/PlayMode`) | interaction: input classification, tint and collider sync, raycast selection, the list panel, window drawing and deletion, the outline layer swap, AR pose handoff, the 2D plan |

Run them from **Window ▸ General ▸ Test Runner**, or headless:

```
Unity.exe -batchmode -runTests -projectPath <project> -testPlatform EditMode -testResults results.xml
Unity.exe -batchmode -runTests -projectPath <project> -testPlatform PlayMode -testResults results.xml
```

---

## Adjustments made along the way

- The Unity MCP was unavailable during execution, so the AD-006 fallback applied: every asset is a text file
  written by the agent, and Editor-side verification (import, Test Runner, iOS build target switch) ran through
  the Unity CLI in batch mode against a mirror of the project.
- The Unity project sits in `Build/FormifyTest` rather than at the repository root, which is where it already
  existed.
- Selection outline is P2 polish, not P1 feedback: a plain RenderObjects layer pass re-renders a layer, it does
  not draw an edge. The guaranteed feedback is the tint; the outline is a stencil two-pass on top of it.
- Window deletion was promoted into scope during the design revision; moving or resizing a placed window
  remains out of scope, along with LiDAR plane detection, 3D pinch zoom, persistence and multi-room.

The one open item left is the iOS build-target switch, listed as a human check in
[`validation.md`](.specs/features/room-wall-selection/validation.md).
