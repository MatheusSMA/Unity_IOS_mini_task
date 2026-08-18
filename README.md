# Formify — Room Scanner

A Unity 6 app for iOS. It draws a room, you tap a surface to select it, drag on a wall to cut a real opening,
look around with the device in AR, or flip to a 2D plan.

---

## Architecture

**MVP — Model, View, Presenter.** `RoomModel` is the model: the room, the windows, the selection, and the rules
about what counts as a legal opening. `SurfaceView`, `WindowView` and the HUD are the views: they draw and they
report taps, and they decide nothing. The controllers in between are the presenters — selection, window drawing,
the cameras — each one turning input into a call on the model and letting the model's events push the result
back out.

That splits the code in two folders. **`Scripts/Domain/`** is plain C# with no Unity in it;
**`Scripts/Presentation/`** is everything that needs an engine. Presenters read the domain; the domain never
reads back. Keeping the rules out of `MonoBehaviour` is what makes them testable in milliseconds instead of by
dragging a finger across a wall.

`RoomBootstrap` wires it all — creates the model, creates the controllers, hands each one what it needs. No DI
container: at this size the wiring is short enough to read top to bottom, and reading it is the fastest way to
understand the app.

Three choices worth naming:

- **The room is generated, the HUD is authored.** The room has to be code — its mesh changes every time a window
  is cut. The HUD is the opposite: real GameObjects in the scene, editable in the inspector. It is still
  *described* in code, and an editor menu runs that description once and leaves the result in the scene.
- **One mode owns the input.** Orbit, WindowDraw, TopDown, AR — a single enum every controller checks, so the
  button that offers an action and the controller that performs it can never disagree.
- **Views repaint from events.** The model says what changed and hands over the old and the new value, so the
  list rewrites two rows instead of rebuilding itself.

---

## Design

The interface follows Formify's own visual language: near-black surfaces, one green accent doing all the
signalling, thin borders, small uppercase labels with wide tracking. Selection is the clearest example — a
selected wall is tinted deep green and ringed in the accent, and a selected window gets the same green border,
so one colour means one thing everywhere.

The world around the room matches: black sky, grey ground, nothing competing with the thing being built.

---

## Tests

83 EditMode and 128 PlayMode, all green. The ones that earn their place:

- **Window rules** — overlap, minimum and maximum size, edge margin, clamping into the wall. Pure domain, no
  scene, and the reason the geometry can be trusted.
- **The opening is real** — a ray fired through a cut window passes through the wall. Without it, "the hole
  looks right" is all anyone can say.
- **Single selection** — selecting a surface or a window clears the other, and exactly the two affected rows
  repaint.
- **Decoration eats no taps** — a tap at the centre of the screen still reaches the room, paired with one
  proving opaque panels *do* stop it. A raycast flag left on in one overlay would silently kill every tap.
- **The 2D flight lands where the snap did** — the animated view switch ends on exactly the pose the code used
  to jump to.

---

## Plugins and skills

- **`unity-mcp`** — a live connection to the running Unity Editor: read the scene, compile, read the console,
  run the suites and get back which cases failed, take screenshots. Most of the work went through it.
- **`tlc-spec-driven`** — the spec workflow below.
- **`claude-mem`** — memory across sessions, so a later one knows why an earlier one decided something.
- **`ponytail`** — pushes for the smallest solution that works. Why there is no DI container and no tween
  library.
- **`caveman`** — compresses the assistant's chat output. No effect on the code.
- **Sub-agents** — several features were built in parallel, one agent each, with an explicit list of the files
  each one owned.

---

## Method: spec-driven

Specify → Design → Tasks → Execute, with the documents kept under `.specs/`:

- **`spec.md`** — requirements in EARS form, each with an id, traced to its tasks and tests.
- **`design.md`** — the object graph and the event flow.
- **`tasks.md`** — 38 atomic tasks; nothing was implemented that was not first a task.
- **`validation.md`** — per requirement: where it is satisfied, which test asserts it, and what is *not*
  asserted.
- **`STATE.md`** — 29 numbered decisions with the reasoning behind each.

The payoff shows on change requests. "Put the windows in the list" was not a guess about what it touched: the
requirement map named the requirements involved, and the decision log said why selection was a single integer —
which was the thing that had to change first.

---

## Install and run

1. Install **Unity 6000.3.11f1** through Unity Hub. Add the **iOS Build Support** module for a device build.
2. Open the project at **`Build/FormifyTest`** — that folder is the Unity project, not the repository root.
3. Open **`Assets/Scenes/Main.unity`** and press **Play**.
4. Tests: **Window ▸ General ▸ Test Runner**, Run All in EditMode and PlayMode. Headless:

   ```
   Unity.exe -batchmode -runTests -projectPath Build/FormifyTest -testPlatform EditMode -testResults results.xml
   Unity.exe -batchmode -runTests -projectPath Build/FormifyTest -testPlatform PlayMode -testResults results.xml
   ```
5. iOS: **File ▸ Build Settings ▸ iOS ▸ Switch Platform**, then Build. The app is landscape-only.

---

For the technical detail — module by module, with the business rules and the numbers — see
[`docs/tecnico.md`](docs/tecnico.md) and the documents in [`.specs/`](.specs).
