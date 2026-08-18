# Formify — Room Scanner

A Unity 6 app for iOS. It draws a room, you tap a surface to select it, drag on a wall to cut a real opening,
look around with the device in AR, or flip to a 2D plan. One scene, about 3.5k lines of C#.

---

## Why it is built this way

**The model knows nothing about Unity.** `Scripts/Domain/` is plain C#: surfaces, windows, selection, and the
rules for what counts as a legal opening. No `MonoBehaviour` anywhere in it. That is what lets the geometry
rules be tested in milliseconds instead of by dragging a finger across a wall in play mode. Everything that
needs Unity lives in `Scripts/Presentation/`. The presenters read the model; the model never looks up.

**No dependency injection.** One class wires everything: it creates the model, creates the controllers, hands
each one what it needs. Forty readable lines instead of a container and an inferred startup order. At this
size the wiring *is* the architecture diagram, so it is written out in one place.

**The room is generated, the HUD is authored.** Opposite choices on purpose. The room must be code — its mesh
changes every time a window is cut. The HUD is the opposite: real GameObjects in the scene, real buttons and
labels you can nudge in the inspector. It began as code and the cost was immediate: nothing visible without
pressing Play, every spacing tweak a code edit. Now the interface is still *described* in code, but an editor
menu runs that description once and leaves the result in the scene. Play builds no UI; it binds the model to
what is already there.

**One thing owns the input.** A single mode — Orbit, WindowDraw, TopDown, AR — and every controller checks it.
So "can I enter window mode right now?" has exactly one answer, and the button that asks cannot disagree with
the controller that enforces.

**Views repaint from events, not polling.** The model announces what changed and hands over both the old and
the new value, so the list rewrites exactly two rows instead of rebuilding itself.

---

## How it flows

```
Touch  →  InputRouter          tap or drag?
       →  SelectionController  raycast into the room
       →  RoomModel            state changes, one event goes out
       →  everyone repaints    surface tints · list marks the row · readout rewrites
```

Nobody asks "what is selected now?" — they were told. The model is the only thing that knows, and it says so
once.

A drag branches away early and never reaches the selection, which is why turning the camera over a wall does
not select it.

---

## The pieces

- **`RoomModel`** — the single source of truth. One selection at a time: a surface or a window, never both.
- **`WindowPlacementValidator`** — the only thing allowed to judge an opening: surface kind, clamp inside the
  wall, size limits, overlap. A rejection leaves the wall exactly as it was.
- **`SurfaceView` / `WindowView`** — mesh and collider rebuild when the model says a window arrived or left. The
  opening is a real hole through the slab, reveal faces included.
- **HUD** — surfaces list with windows nested under their wall, action rail, 2D/3D toggle, live readout.
- **Cameras** — orbit for 3D, orthographic for the plan, device pose for AR, with the 2D switch animated.

---

## The process: spec-driven

Built with the **`tlc-spec-driven`** workflow — Specify, Design, Tasks, Execute. What it left behind, in
`.specs/`:

- **`spec.md`** — requirements in EARS form. Not "should feel responsive" but *"WHEN the user releases a valid
  drag THEN the system SHALL cut a rectangular hole through the solid wall mesh, including the four reveal
  faces."* Every requirement has an id, traced to its tasks and its tests.
- **`tasks.md`** — 38 atomic tasks. Nothing was implemented that was not first a task.
- **`validation.md`** — per requirement: where it is satisfied, which test asserts it, and what is *not*
  asserted. That last part is the one most reports skip.
- **`STATE.md`** — 28 numbered decisions with the reasoning, written when the disagreement happened rather than
  afterwards.

The suites stand at **83 EditMode and 128 PlayMode tests, all green**. EditMode covers the domain rules with no
scene at all; PlayMode covers input, cameras, views and HUD wiring.

It earns its keep when something changes: "put the windows in the list" was not a guess about blast radius —
the requirement map named what it touched, and the decision log said why selection was a single integer, which
was exactly the thing that had to change first.

---

## The Claude Code setup

- **`unity-mcp`** — a live connection to the running Unity Editor, not a file-writing bot: reads the scene,
  compiles, reads the console, runs the test suites and reports which cases failed, takes screenshots.
- **`tlc-spec-driven`** — the workflow above, with an independent verifier pass and evidence-or-zero.
- **`claude-mem`** — memory across sessions, so a later session knows why an earlier one decided something.
- **`ponytail`** — forces the laziest solution that works. The reason there is no DI container, no tween
  library, no abstraction with a single implementation.
- **`caveman`** — compresses Claude's chat output. No effect on the code.
- **Sub-agents** — the last phases ran several in parallel, one per feature, each with an explicit list of files
  it owned and files it must not touch.

---

## Running it

1. Install **Unity 6000.3.11f1** (Unity Hub → Installs → Add). For device builds, add the **iOS Build Support**
   module.
2. Open the project at **`Build/FormifyTest`** — that folder is the Unity project, not the repository root.
3. Open **`Assets/Scenes/Main.unity`**.
4. Press **Play**. The room builds itself; the HUD is already in the scene.
5. To run the tests: **Window ▸ General ▸ Test Runner**, then Run All in both EditMode and PlayMode. Headless:

   ```
   Unity.exe -batchmode -runTests -projectPath Build/FormifyTest -testPlatform EditMode -testResults results.xml
   Unity.exe -batchmode -runTests -projectPath Build/FormifyTest -testPlatform PlayMode -testResults results.xml
   ```
6. For iOS: **File ▸ Build Settings ▸ iOS ▸ Switch Platform**, then Build. The app is landscape-only.

---

## Want the technical version?

This document is the tour. The detailed one is **[`docs/tecnico.md`](docs/tecnico.md)** — module by module, with
the business rules and the numbers. Below that sits **[`.specs/`](.specs)**: the requirements
(`features/room-wall-selection/spec.md`), the design, the task list, the evidence file, and the decision log
(`.specs/STATE.md`), which is the best single file to read if you want to know *why* rather than *what*.
