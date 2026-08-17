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

## C-06 — Unity template packages left in the manifest

**Raised by**: T01
**Context**: The project was created from a Unity template and carries packages no requirement needs: `com.unity.ai.navigation`, `com.unity.multiplayer.center`, `com.unity.timeline`, `com.unity.visualscripting`, `com.unity.collab-proxy`.
**Assumption applied**: left untouched. T01 only adds what BOOT-01 requires (AR Foundation 6.4.3 + ARKit 6.4.3, matching). Removing them is a separate cleanup decision.
**Answer**:
