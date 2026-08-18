# Validation Report — room-wall-selection

**Result:** PASS (automated scope) · 3 items deferred to human UAT
**Date:** 2026-08-17
**Feature:** room-wall-selection
**Diff range verified:** `7306a9e..910adf9` (task breakdown → HEAD, 41 commits)
**Gate at time of writing:** EditMode 67/67 · PlayMode 85/85 · 0 failed · 0 skipped

The gate was last executed against the working tree after the template-package removal
(`dfffb90`), through the Unity MCP test runner on Unity 6000.3.11f1:
EditMode 67 passed in 1.22 s, PlayMode 85 passed in 9.32 s.

---

## 1 · Method

Verification followed the tlc-spec-driven Verifier procedure: **author ≠ verifier**,
**evidence-or-zero**, plus a **discrimination sensor**.

- **Spec-anchored coverage.** Every acceptance criterion in `spec.md` was re-derived from the
  spec text, not from the code, and then matched to a named test that asserts the spec's
  outcome. A criterion with no test that fails when the behaviour is removed counts as zero,
  regardless of how much code appears to implement it.
- **Discrimination sensor.** Behaviour-level mutations were injected into an isolated mirror of
  the project and the suite re-run. A mutation that leaves the suite green proves the tests do
  not discriminate that behaviour — a coverage hole, even at 100 % line coverage.
- **Read-only over the real tree.** The Verifier does not fix. Defects it found were routed back
  as separate fix tasks, each with its own commit.

---

## 2 · Gate results

| Suite | Assembly | Tests | Passed | Failed | Skipped | Duration |
| ----- | -------- | ----: | -----: | -----: | ------: | -------: |
| EditMode | `Formify.Tests.EditMode` | 67 | 67 | 0 | 0 | 1.22 s |
| PlayMode | `Formify.Tests.PlayMode` | 85 | 85 | 0 | 0 | 9.32 s |
| **Total** | | **152** | **152** | **0** | **0** | **10.54 s** |

Test files: 7 EditMode, 17 PlayMode. Domain logic (selection semantics, window validation,
slab-with-holes geometry, room generation, mode matrix) is EditMode — no scene, no MonoBehaviour.
Everything that needs a frame, a collider or an input device is PlayMode.

---

## 3 · Coverage matrix

24 requirements, 24 covered by at least one discriminating test. `EM` = EditMode, `PM` = PlayMode.

### BOOT-01 — Project bootstrap

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · Unity 6 + URP, URP asset assigned, active quality level carries the outline passes | `RenderPipelineConfigTests.ActiveRenderPipeline_IsTheOneHostingTheOutlineFeatures`, `.SelectedSurfaceLayer_Exists` | EM |
| 2 · Packages pinned at fixed versions | `Packages/manifest.json` — inputsystem 1.19.0, arfoundation 6.4.3, arkit 6.4.3, test-framework 1.6.0, URP 17.3.0 | file |
| 3 · Bootstrap scene opens without console errors | Editor console clean on load; `RoomBootstrapTests` builds the same composition headless | PM + Editor |
| 4 · Test Runner green in both assemblies | §2 above | gate |
| 5 · Project switches to the iOS build target | **not asserted by automation** — see §7 | UAT |

AC1 exists because the Editor was running the PC quality level, whose renderer has no
RenderObjects features, so the outline rendered nothing in the very environment AD-001 declares
the validation target. The test now pins the active level (AD-017).

### ROOM-01 — Synthetic room, solid surfaces, any N ≥ 3

| AC | Evidence | Kind |
| -- | -------- | ---- |
| P1.1 · N ≥ 3 walls + floor + ceiling | `RoomBuilderTests.DefaultRectangle_Yields4WallsFloorAndCeiling`, `.VertexCount_DrivesSurfaceCountAndNaming` | EM |
| P1.1 · solid slab, configurable thickness | `RoomBuilderTests.WallDimensions_MatchEdgeLengthHeightAndThickness`, `SurfaceMeshBuilderTests.ZeroHoles_ProducesSixQuads`, `.ZeroHoles_AllVerticesInsideLocalSlabBounds` | EM |
| P1.1 · inward normals, floor up / ceiling down | `RoomBuilderTests.WallNormals_PointInward_ForBothWindings`, `.FloorNormalPointsUp_CeilingNormalPointsDown` | EM |
| P1.1 · one view per surface, collider shares the mesh | `RoomBootstrapTests.Awake_CreatesOneViewPerSurface_MatchingTheRoomDefinition`, `.EveryView_HasCollidableMesh_SharedWithTheRenderer` | PM |

### CAM-01 — Orbit drag

| AC | Evidence | Kind |
| -- | -------- | ---- |
| P1.2 · horizontal drag → yaw, full 360° | `OrbitCameraControllerTests.Yaw_is_free_and_wraps_past_a_full_turn` | PM |
| P1.2 · drag reaches the camera at all | `InputRouterTests.TouchMovedPastThresholdRaisesDragAndNeverTaps`, `RoomBootstrapDragRoutingTests.InOrbit_TheDragTurnsTheCameraAndCutsNoWindow` | PM |
| P1.5 · mouse drag stands in for touch in the Editor | `InputRouter.cs:52` enables `TouchSimulation`; the PlayMode suite drives the same path through simulated touch. No test drives a physical mouse device | code + UAT |

### CAM-02 — Pitch clamp and room containment

| AC | Evidence | Kind |
| -- | -------- | ---- |
| P1.3 · pitch clamped to the configured range | `OrbitCameraControllerTests.Pitch_clamps_at_both_ends`, `.SetRotation_applies_exactly_and_still_clamps_pitch` | PM |
| P1.4 · camera stays inside the room in Orbit | `.Position_stays_in_the_room_at_eye_height_after_drags`, `.Eye_height_is_clamped_into_a_low_room`, `.ResetToRoomCentre_restores_the_eye_position` | PM |

### SEL-01 — Single-selection tap semantics

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · tap an unselected surface selects it and deselects the previous in one operation | `RoomModelSelectionTests.Select_DifferentSurface_ReplacesSelectionAndReportsBothIds`, `SelectionControllerTests.Tapping_a_second_wall_moves_the_selection_in_one_event` | EM + PM |
| 2 · at most one selected at any time | `RoomModelSelectionTests.Select_Sequence_KeepsExactlyTheLastSelectedId`, `.Select_FromEmptyState_SetsSelectionAndRaisesOnce` | EM |
| 3 · re-tapping the selected surface changes nothing and raises nothing | `RoomModelSelectionTests.Select_AlreadySelectedId_RaisesNothing`, `SelectionControllerTests.Tapping_the_selected_wall_raises_nothing` | EM + PM |
| — · unknown id is a no-op | `RoomModelSelectionTests.Select_UnknownId_LeavesSelectionUntouched` | EM |

### SEL-02 — Tint feedback

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 4 · selected surface tinted via MaterialPropertyBlock, restored on deselect | `SurfaceViewTests.Selection_TintsThroughPropertyBlock_AndDeselectionRestoresBaseColour` (asserts `_BaseColor` in the block, both directions) | PM |

### SEL-03 — Tap/drag discrimination, through-opening miss, window routing

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 5 · movement past the threshold is a drag, never a selection | `InputRouterTests.TouchMovedPastThresholdRaisesDragAndNeverTaps` | PM |
| 5 · duration past the cutoff is a drag | `InputRouterTests.TouchHeldPastTapDurationIsClassifiedAsDrag` — driven by an injected clock seam, not wall-clock (see §5, defect 4) | PM |
| 5 · a short still touch is a tap | `InputRouterTests.ShortStillTouchRaisesTappedOnce` | PM |
| 6 · ray through an opening hits no surface → selection unchanged | `SelectionControllerTests.Tapping_empty_space_keeps_the_selection`, `SurfaceViewTests.Raycast_ThroughOpening_MissesCollider_AfterWindowAdded` | PM |
| 7 · tap on an opening collider routes to the deletion UI and never moves the selection | `SelectionControllerTests.A_window_target_takes_the_tap_and_the_selection_stands`, `WindowViewTests.OnTapped_ShowsTheDeleteButtonAndLeavesTheSelectionUntouched` | PM |

### LIST-01 — Real-time surface list

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · one row per surface, by name, with state | `SurfaceListPanelTests.Configure_ListsEverySurfaceOnceInModelOrder` (also asserts no row for an unknown id) | PM |
| 2 · exactly the two affected rows change, from one (previous, current) notification | `.Select_ThenSelectAnother_ChangesExactlyTheTwoAffectedRows` — counts changed rows, not just the two expected ones | PM |
| — · clearing turns the row off and leaves every row unselected | `.ClearSelection_TurnsTheSelectedRowOffAndLeavesAllRowsUnselected` | PM |

### LIST-02 — Collapsible panel

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 3 · collapse control toggles the panel | `SurfaceListPanelTests.Collapsed_RowsKeepTrackingSelectionAndTheCollapseControlStaysVisible` — exercises the control's `onClick`, not `ToggleCollapsed()` directly, so the wiring is covered too | PM |
| 4 · the control stays visible while collapsed | same test, asserts `activeInHierarchy` on the control | PM |

### CLR-01 — Clear selection

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · Clear deselects, drops the tint, updates the row | `ClearButtonTests.Press_WithSelection_ClearsItAndFiresSelectionChangedOnce`, `RoomModelSelectionTests.ClearSelection_WithSelection_ClearsAndRaisesWithNullCurrent` | EM + PM |
| 2 · idempotent: no state change, no event when nothing is selected | `ClearButtonTests.Press_WithNothingSelected_FiresNoSelectionChangedAtAll`, `RoomModelSelectionTests.ClearSelection_WhenEmpty_RaisesNothing` | EM + PM |
| 3 · Clear in window mode cancels the draw, exits the mode, then deselects | `ClearButtonTests.Press_InWindowDraw_LeavesTheModeFirstThenClearsTheSelection` (asserts the order) | PM |

### WIN-01 — Window mode entry, routing, tap lock

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · button visible only while a Wall is selected AND mode is Orbit | `WindowModeButtonTests.Visible_when_a_wall_is_selected_in_orbit`, `.Hidden_when_the_floor_is_selected`, `.Hidden_when_the_ceiling_is_selected`, `.Hidden_when_nothing_is_selected`, `.Hidden_when_a_wall_is_selected_but_the_mode_is_top_down`, `.Hidden_when_a_wall_is_selected_but_the_mode_is_ar` | PM |
| 2 · activating it routes wall drags to drawing instead of orbit | `RoomBootstrapDragRoutingTests.InWindowDraw_TheDragCutsAWindowAndLeavesTheCameraAlone` vs `.InOrbit_TheDragTurnsTheCameraAndCutsNoWindow` — the same gesture, three modes, asserted against both outcomes | PM |
| 2 · the button click is what enters and exits | `WindowModeButtonTests.Click_while_visible_enters_window_draw`, `.Click_again_returns_to_orbit` | PM |
| 13 · surface taps are swallowed while drawing | `SelectionControllerTests.WindowDraw_mode_swallows_the_tap` | PM |
| — · the transition matrix behind it | `ModeManagerTests` (16 tests: every legal and illegal edge, including `OrbitToWindowDraw_WithoutWallSelected_IsRejected`) | EM |

### WIN-02 — Preview, solid-mesh cut, collider sync

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 3 · live preview between the start corner and the finger | `WindowDrawControllerTests.Cancel_mid_drag_destroys_the_preview_and_creates_nothing`, `.Leaving_window_mode_mid_drag_cancels_through_the_mode_manager` — preview lifecycle asserted; its per-frame visual is UAT |  PM + UAT |
| 4 · valid release cuts through the solid wall including the four reveal faces | `SurfaceMeshBuilderTests.OneHole_AddsRevealQuadsAndSlicedFaces`, `.OneHole_NoFaceTriangleCoversTheOpening`, `.TwoHoles_ProduceEightRevealQuadsAndTwoOpenings`, `WindowDrawControllerTests.Valid_drag_creates_one_window_and_opens_the_wall` | EM + PM |
| 4 · mesh stays well-formed | `SurfaceMeshBuilderTests.AllTriangleIndicesAreInRange`, `.FrontFaceTrianglesFaceTowardsPositiveNormal` | EM |
| 5 · every rebuild reassigns the MeshCollider in the same operation | `SurfaceViewTests.Raycast_ThroughOpening_MissesCollider_AfterWindowAdded`, `.Raycast_ThroughOpening_HitsColliderAgain_AfterWindowRemoved` — asserted through physics, not through a field read | PM |
| 12 · a ray through the opening never hits the wall mesh | same two tests, plus `.Raycast_AtSolidWall_HitsCollider_WithAndWithoutWindow` as the negative control | PM |

### WIN-03 — Placement validation

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 6 · clamped inside bounds minus the edge margin | `WindowPlacementValidatorTests.RectCrossingTheMarginBand_IsClampedToTheAllowedRegion`, `.RectWellInsideWall_IsAcceptedUnchanged` | EM |
| 7 · overlap rejected | `.RectOverlappingAnExistingWindow_IsRejectedAsOverlap`, `.RectTouchingAnExistingWindowEdge_IsAccepted` (the boundary the rule must not swallow), `RoomModelWindowTests.TryAddWindow_OverlappingRect_RejectsAndKeepsSingleWindow` | EM |
| 8 · below minimum / above maximum rejected, exactly on the limit accepted with a 1e-4 band | `.RectBelowMinimumSize_IsRejectedAsTooSmall`, `.RectAboveMaximumSize_IsRejectedAsTooLarge`, `.RectAtExactlyTheMinimumSize_IsAccepted`, `.RectAtExactlyTheMaximumSize_IsAccepted`, `.RectJustBelowTheMinimumSize_IsStillRejected`, `.RectJustAboveTheMaximumSize_IsStillRejected` | EM |
| 9 · nothing surviving the clamp is rejected | `.RectEntirelyInsideTheMarginBand_IsRejectedAsMarginViolation`, `.WallTooSmallForAnyMargin_IsRejectedAsMarginViolation`, `.RectSittingExactlyOnTheMargin_IsAccepted`, `.RectCompletelyOffTheWall_IsRejectedAsOutOfBounds` | EM |
| 10 · multiple non-overlapping windows per wall | `RoomModelWindowTests.TryAddWindow_TwoNonOverlappingRects_BothStoredWithDistinctIds` | EM |
| 11 · a drag starting on empty space, floor or ceiling creates nothing | `WindowDrawControllerTests.Drag_starting_on_the_floor_creates_nothing`, `WindowPlacementValidatorTests.FloorSurface_IsRejectedAsInvalidSurfaceKind`, `.CeilingSurface_IsRejectedAsInvalidSurfaceKind`, `RoomModelWindowTests.TryAddWindow_OnFloor_RejectsWithInvalidSurfaceKind` | EM + PM |
| — · a right-to-left drag is normalised before validation | `.RectDraggedRightToLeft_IsNormalisedBeforeValidation` | EM |

### WIN-04 — Window deletion

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · each opening gets its own collider | `WindowViewTests.WindowAdded_CreatesOneViewWithAColliderSizedToTheOpening`, `.Collider_SitsInTheOpeningAndIsHitByARayFromTheRoom` | PM |
| 2 · tapping the opening shows an X anchored top-right | `.OnTapped_ShowsTheDeleteButtonAndLeavesTheSelectionUntouched`, `.DeleteButton_IsAnchoredToTheOpeningsTopRightCorner` (asserts the corner, not merely that a button exists) | PM |
| 3 · X shows a confirmation popup | `.DeleteButtonClick_ShowsTheConfirmationPopup` | PM |
| 4 · confirm removes the window and rebuilds mesh + collider together | `.ConfirmDelete_RemovesTheWindowAndDestroysTheView`, `.TryRemoveWindow_OnTheModelAlsoDestroysTheView`, `SurfaceViewTests.Raycast_ThroughOpening_HitsColliderAgain_AfterWindowRemoved` | PM |
| 5 · cancel closes the popup and leaves the window | `.CancelDelete_ClosesThePopupAndKeepsTheWindow` | PM |

### OUT-01 — Selection outline

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · stencil two-pass outline over the `SelectedSurface` layer | `SelectionOutlineTests.Selecting_moves_the_surface_to_the_outline_layer_and_deselecting_restores_it`, `RenderPipelineConfigTests.SelectedSurfaceLayer_Exists`, `.ActiveRenderPipeline_IsTheOneHostingTheOutlineFeatures` | EM + PM |
| 2 · the raycast mask includes both layers while the swap is active | `SelectionOutlineTests.The_selected_surface_stays_hittable_and_re_tapping_it_raises_nothing`, `.A_mask_without_the_outline_layer_still_stops_on_the_selected_surface` (the discriminator for the OR in the mask) | PM |
| — · the outline's on-screen appearance | **not asserted by automation** — see §7 | UAT |

### AR-01 — AR pose camera

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · AR pose drives rotation and position, clamped to the room interior | `ArPoseCameraControllerTests.InAr_RigRotationEqualsThePose`, `.InAr_PoseOutsideTheRoom_IsClampedToTheClosestInteriorPoint` | PM |
| 2 · touch-drag camera input ignored while in AR | `RoomBootstrapDragRoutingTests.InAr_TheDragGoesNowhereBecauseArOwnsThePose`, `ArPoseCameraControllerTests.InOrbit_APoseNeverMovesTheRig` (the converse) | PM |
| 3 · leaving AR restores orbit from the current orientation | `.LeavingAr_HandsTheFinalOrientationToTheOrbitCamera` | PM |
| 4 · toggle disabled where AR tracking is unavailable | `ArToggleButtonTests.AvailabilityFalse_ButtonIsDisabledAndClickingChangesNothing`, `.AvailabilityTrue_ClickEntersArAndClickingAgainReturnsToOrbit`, `.InTopDown_ButtonIsDisabledBecauseArIsOnlyReachableFromOrbit` | PM |
| 5 · tap selection keeps working in AR | `SelectionControllerTests.Ar_mode_keeps_taps_selecting` | PM |
| — · real pose tracking under XR Simulation | **not asserted by automation** — see §7 | UAT |

### TOP-01 — 2D / 3D switch

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 1 · two buttons at the top | `ViewSwitchButtonsTests.Clicking2D_EntersTopDown_AndClicking3D_ReturnsToOrbit` | PM |
| 2 · 2D disables the ceiling's MeshRenderer **and** MeshCollider | `TopDownControllerTests.Entering2D_FitsTheRoom_HidesTheCeiling_AndClearsTheSelection`, `.In2D_ARayStraightDownReachesTheFloor_NotTheCeiling` — the second one is what proves the collider went too | PM |
| 5 · 3D restores the ceiling and re-centres the camera | `.ExitingTo3D_RestoresTheCeiling_TheCamera_AndTheRig` | PM |
| 7 · fit-to-room framing plus a small margin | `.Entering2D_FitsTheRoom_HidesTheCeiling_AndClearsTheSelection` | PM |
| 9 · pinch zoom within 0.5×–2.0×, 2D only | `.Pinch_ClampsToHalfAndDoubleTheFitSize`, `.Pinch_InOrbit_ChangesNothing` | PM |
| — · the active view's button is the highlighted, unpressable one | `ViewSwitchButtonsTests.ActiveViewsButton_IsTheNonInteractableOne_InBothStates` | PM |

### TOP-02 — Interactive plan

| AC | Evidence | Kind |
| -- | -------- | ---- |
| 3 · entering 2D clears the selection and cancels an in-progress draw | `TopDownControllerTests.Entering2D_FitsTheRoom_HidesTheCeiling_AndClearsTheSelection`, `ModeManagerTests.OrbitToTopDown_ClearsSelection`, `.WindowDrawToTopDown_CancelsDrawAndClearsSelection` | EM + PM |
| 4 · taps in the plan move the selection, single-selection rules apply | `ViewSwitchButtonsTests.InTopDown_ListRowFollowsSelection_AndClearButtonStillClears`, `WallTapToleranceTests.In2D_AFloorTapAtTheRoomCentreSelectsTheFloor` | PM |
| 6 · the list panel and Clear stay functional in 2D | `ViewSwitchButtonsTests.InTopDown_ListRowFollowsSelection_AndClearButtonStillClears` | PM |
| 8 · a floor tap within the tolerance of a wall selects that wall | `WallTapToleranceTests.In2D_AFloorTapJustInsideTheToleranceSelectsTheWall`, `.In2D_AFloorTapOutsideTheToleranceSelectsTheFloor`, `.In2D_TheNearerOfTwoWallsInRangeWins`, `.InOrbit_TheSameNearWallTapSelectsTheFloor` (the tolerance is 2D-only) | PM |

### Edge cases

| ID | Evidence | Kind |
| -- | -------- | ---- |
| EDGE-01 · only the primary touch is processed | `InputRouterTests.SecondFingerProducesNoEvents` | PM |
| EDGE-02 · a tap on UI never raycasts into the scene | `InputRouterTests.TouchBeginningOverUiProducesNoEvents`, `.DefaultUiGateAsksTheEventSystem` | PM |
| EDGE-03 · a release off the wall clamps instead of switching walls | `WindowDrawControllerTests.Drag_ending_off_the_wall_clamps_into_the_bounds_minus_the_margin` | PM |
| EDGE-04 · a tap in window mode creates no window | `WindowDrawControllerTests.Tap_without_drag_creates_no_window` | PM |
| EDGE-05 · a failed rebuild keeps the previous mesh and collider, and rolls the model back | `SurfaceViewTests.Rebuild_WithDegenerateSurface_KeepsPreviousMeshAndCollider`, `.FailedRebuild_AfterAdd_RollsTheWindowBackOutOfTheModel`, `.FailedRebuild_AfterRemove_RestoresTheWindowInTheModel`, `RoomModelWindowTests.Rollback_AfterAddAndAfterRemove_RestoresListWithoutEvents`, `SurfaceMeshBuilderTests.DegenerateInputs_ReturnNull` | EM + PM |
| EDGE-06 · selection changes while collapsed still update the rows | `SurfaceListPanelTests.Collapsed_RowsKeepTrackingSelectionAndTheCollapseControlStaysVisible` | PM |

**Coverage: 24 / 24 requirements, 0 unmapped.**

---

## 4 · Discrimination sensor

Coverage counts nothing if the tests cannot tell correct code from broken code. Five
behaviour-level mutations were injected into an isolated mirror of the project and the full suite
re-run: **4 killed, 1 survivor.**

The surviving mutant sat on the `<` comparison in the window size rule. It was not a weak test —
it was a real defect: the boundary between accept and reject was being decided by float drift
rather than by the rule (§5 defect 2, AD-018). The four killed mutants confirmed the suite
discriminates the behaviours they touched; only the survivor is recorded in the decision log, so
this report does not restate the other four.

A second pass then looked for behaviour whose removal no test could notice. Six such gaps were
found and closed in `4fa3643`, and mutations re-applied to the mirror to confirm each new test
fails when the behaviour it protects is deleted:

| Gap closed | Test added | Why it could not fail before |
| ---------- | ---------- | ---------------------------- |
| OUT-01 AC2 | `A_mask_without_the_outline_layer_still_stops_on_the_selected_surface` | the raycast mask defaulted to `~0`, so ORing the `SelectedSurface` layer in was invisible; the test now narrows the mask and parks a second wall behind the selected one |
| LIST-02 AC3 | collapse driven through `onClick.Invoke()` | the old test called `ToggleCollapsed()` directly, so the button wiring was never under test |
| WIN-04 AC2 | `DeleteButton_IsAnchoredToTheOpeningsTopRightCorner` | the X's position was asserted as "exists", not as the projected top-right corner (now checked against the other three corners too) |
| WIN-01 AC2 / AR-01 AC2 | `RoomBootstrapDragRoutingTests` — one gesture, three modes, real virtual touches through the composed `InputRouter` | drag routing had no coverage at all |
| AR-01 AC5 | `Ar_mode_keeps_taps_selecting` | no test distinguished AR from WindowDraw for taps |
| WIN-01 AC1 | `Hidden_when_the_ceiling_is_selected`, `Hidden_when_a_wall_is_selected_but_the_mode_is_ar` | the Ceiling and AR branches of the visibility rule were untested |

A seventh gap — the flaky wall-clock tap-duration test — was fixed separately in `ed13bda` by
injecting a clock seam (§5 defect 4).

---

## 5 · Defects found and fixed

Each was found by verification, not by the implementer, and each landed as its own commit.

| # | Defect | Why it mattered | Fix |
| - | ------ | --------------- | --- |
| 1 | EDGE-05 rollback had no production caller | The rollback existed and was unit-tested, but nothing in the runtime path invoked it: a failed rebuild left the model holding a window the wall does not have | `25f8411` |
| 2 | Window sizes exactly on the limit were rejected by float drift | The clamped extent is a subtraction of two world coordinates, so the same rectangle measured 0.20000005 at one wall and 0.19999999 at another — the rule was position-dependent | `b266cea`, 1e-4 band (AD-018) |
| 3 | The Editor's active quality level pointed at a renderer without the outline passes | OUT-01 rendered nothing in the Editor, which AD-001 makes the entire validation target | `75cd272` (AD-017) |
| 4 | The tap-duration test read the wall clock | Flaky by construction: the pass depended on machine timing, not on the cutoff | `ed13bda`, injected clock seam |

---

## 6 · Non-functional checks

| Check | Result |
| ----- | ------ |
| Domain layer has no scene dependency | `Formify.Domain.asmdef` has an empty `references` list; every domain test is EditMode |
| Assembly boundaries hold | `Formify.Presentation` references only Domain, InputSystem, ARFoundation, XR.CoreUtils, TextMeshPro, UGUI |
| Package surface is minimal | Five unused template packages removed (`dfffb90`); gate re-run green afterwards |
| No console errors on load | Editor console reports 0 errors; the suite uses no `LogAssert` suppressions anywhere, so no expected-error is being swallowed |

---

## 7 · Deferred to human UAT

Three things automation in this project cannot assert. They are not failures — they are outside
what the Editor test runner can observe.

1. **iOS build-target switch (BOOT-01 AC5).** File > Build Settings > iOS must switch without
   build-settings errors. Per AD-001 no device build is required.
2. **Outline appearance (OUT-01 AC1).** The tests prove the layer swap, the layer's existence and
   the active pipeline's features. Whether the two stencil passes actually draw a visible edge is
   a look, and a look needs eyes.
3. **AR pose tracking (AR-01).** The tests drive a synthetic pose into the controller. Whether AR
   Foundation's XR Simulation feeds a real pose through the rig has to be seen in Play Mode.

Also worth a look, though not an acceptance criterion: **mouse drag as the touch stand-in**
(CAM-01 AC5) is enabled in code via `TouchSimulation` and exercised through simulated touch, but
no test drives a physical mouse device.

---

## 8 · Residual risk

- **Tuning defaults are unconfirmed.** Nine rows in the spec's assumptions table are still marked
  `Confirmed? n` — pitch clamp, tap thresholds, room dimensions, thickness, window min/max, edge
  margin, naming, XR Simulation. Every one is a serialized field; none changes a rule, only a
  number.
- **The HUD is placeholder.** The uGUI layer is built in code with grey boxes and white labels;
  the `Room Scanner HUD` art kit (`eb0124a`) is imported but its handoff settings are not applied
  and no test asserts anything about appearance. Now tracked: spec requirement HUD-01 and task T32
  (`docs/tasks/T32-hud-art-kit.md`). AD-020 settled the orientation on 2026-08-18: the app ships
  landscape and the HUD reproduces the kit's art; the portrait 1170x2532 scaler goes with it.
- **Selection state is rendered as text.** `SurfaceListPanel` marks the selected row with the
  `"  [SELECTED]"` suffix, and four PlayMode tests read that string. Restyling the row to the
  handoff's green tag will break those tests unless the state is exposed separately from the
  label text. Now tracked: HUD-01 AC3 and task T31, sequenced before T32 for exactly that reason.

- **Three artefacts disagree about the window mode button.** `WindowModeButton.Refresh()` deactivates
  the GameObject when the mode leaves Orbit (AD-015), so the exit branch in `OnClick` is unreachable
  from the UI — while T22 and the class comment both describe that branch as the exit, and the art
  kit ships a disabled-but-visible state for the same button. Found while reconciling the docs after
  this report was written. AD-019 answered it on 2026-08-18 — the button stays on screen, disabled
  instead of hidden, with a state dot — which rewrote WIN-01 AC1 and returned that requirement to
  `In Tasks`; T30 implemented it on 2026-08-18 — `Refresh()` now drives `Button.interactable`, the
  enabled set is `Orbit or WindowDraw` so the exit branch is reachable (AD-021), and the fixture
  asserts the state dot — which returned WIN-01 to `Verified`. Not a test failure: every WIN-01
  behaviour this report asserted still holds against the rule that was in force when it ran.

---

## 9 · Phase 6 verification (HUD-01, 2026-08-18)

**Verdict: PASS.** Author-run, not an independent fresh-eyes pass — sections 1-8 above were produced by five
independent verifiers against Phases 1-5; this section covers only what Phase 6 added, and it is honest about
being written by the author of that change.

### Gate

| Suite | Result | Evidence |
| ----- | ------ | -------- |
| EditMode | 67/67 | Unity `run_tests`, job `252ace88`, 2026-08-18 |
| PlayMode | 92/92 | Unity `run_tests`, job `69908969`, 2026-08-18 (87 before Phase 6, 5 added) |
| Console | 0 errors, 0 warnings | Unity `read_console` after a forced refresh + compile |

The suite could not run at all when the session opened: T31 removed `SurfaceRow.SelectedMarker` and left the
discrimination test referring to it (`SurfaceListPanelTests.cs:200`, CS0117). Repaired first, in its own commit.

### Per-AC evidence

| AC | Where it is satisfied | Where it is asserted |
| -- | --------------------- | -------------------- |
| HUD-01 AC1 | `Assets/Resources/HUD/*.png.meta` — Sprite (2D and UI), Full Rect, Bilinear, Compression None (`overridden: 0` on every platform block, so the default governs), sRGB on, PPU 300, borders tripled from the handoff (`row_fill_9s.png.meta:52` = 18/18/18/18 for the kit's 6). AD-024 records why the HUD reads from `Resources` | Build gate (config layer, per the coverage matrix) |
| HUD-01 AC2 | `HudTheme.cs` palette + `ReferenceResolution` 1183x670 match 0.5 (`SurfaceListPanel.cs:118-121`); rail, buttons, toggle, rows, readout, hint pill, scanlines built to the handoff's RectTransform table; `ProjectSettings.asset:62-65` allows landscape only | `HudArtKitTests.Readout_TracksTheSelection_AndTheWindowCount`, `HudArtKitTests.HintPill_FollowsTheMode`; appearance itself deferred to UAT |
| HUD-01 AC3 | `SurfaceRow.IsSelected` (T31) | `SurfaceListPanelTests.SelectedState_LivesOnTheRow_NotInTheLabelText` |
| HUD-01 AC4 | `HudTheme.AddImage` defaults `raycastTarget: false`; only `AddPanelBackground`'s fill, the rail fill, the header fill, button fills and the popup fill opt in | `HudArtKitTests.Decoration_OverTheWholeScreen_ConsumesNoTap`, paired with `Panels_DoConsumeTaps_SoNoTapFallsThroughIntoTheRoom` |
| HUD-01 AC5 | Clear stays idempotent and single-step (`ClearButton.cs`); the kit's destructive palette is used only on window deletion (`WindowView.cs`). Extended to copy: the hint pill describes WIN-02's drag, not the mock-up's tap-to-place with resize handles | `ClearButtonTests` (unchanged), `HudArtKitTests.HintPill_FollowsTheMode` |
| WIN-01 AC1 (state dot) | `WindowModeButton.StateDot` wired to `HudButton.Dot` in `RoomBootstrap.Compose` | `HudArtKitTests.RailWindowModeButton_EntersWindowMode_InASingleClick` |

### Discrimination sensor

Two behaviour-level mutations, applied to the real tree, run, then reverted; the revert was confirmed by
re-running the suite to green.

| Mutant | Result | Killed by |
| ------ | ------ | --------- |
| `AddScanlines` passes `raycastTarget: true` | **Killed** — `Expected: 0 But was: 1`, naming `Scanlines` as the hit | `Decoration_OverTheWholeScreen_ConsumesNoTap` |
| `windowModeHud.Button.onClick.AddListener(...)` duplicated | **Killed** — `Expected: WindowDraw But was: Orbit` | `RailWindowModeButton_EntersWindowMode_InASingleClick` |

No survivors. The `Panels_DoConsumeTaps` half is what makes the first sensor meaningful: an earlier draft of it
failed and exposed defect 2 below, which is also proof the raycast is not inert.

### Defects found and fixed during Phase 6

1. **The AR button was wired twice.** `ArToggleButton.Configure` registers its own `onClick`, and
   `RoomBootstrap.Compose` added the same listener again, so one press ran `OnClick` twice: Orbit -> Ar -> Orbit.
   Untestable through `ArToggleButton`'s own fixture, which builds its button by hand and never saw the second
   registration. Fixed by dropping the bootstrap's line; the same class of bug is now sensed on the window mode
   button, which is the one rail button a headless test can actually press.
2. **No panel background blocked taps.** `HudTheme.AddPanelBackground` created the fill with the default
   `raycastTarget: false`, so a tap anywhere on the surfaces list fell through into the room and selected whatever
   was behind it — EDGE-02, not HUD-01. Found by the paired half of the AC4 sensor, which reported
   `fillRaycast=False` on a panel it expected to block. Fixed in `AddPanelBackground`; the hint pill opts in the
   same way.
3. **Locale leaked into the readout.** `float.ToString("0.00")` follows the device culture, so a pt-BR phone
   rendered `4,00 × 2,80`. Fixed with `CultureInfo.InvariantCulture`; the test asserts the same way, so it would
   have passed either way — that one is caught by reading, not by the sensor.

### Known gaps

- **Appearance is not asserted anywhere.** By design (the coverage matrix puts appearance in UAT), but it means
  the palette, spacing and 9-slice work rest on the screenshots taken during this session and on human review.
- **Every screenshot was taken at the Editor Game view's 1557x1222.** That is not the landscape shape AD-020
  designs for, so proportion and overflow on a real device are unverified.
- **This section is author-verified.** Sections 1-8 had author != verifier; this one did not.
