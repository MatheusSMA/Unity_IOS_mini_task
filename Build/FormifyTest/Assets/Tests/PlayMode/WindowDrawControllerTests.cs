using System.Collections;
using System.Globalization;
using Formify.Domain;
using Formify.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Formify.Tests.PlayMode
{
    /// <summary>
    /// WIN-01, WIN-02 (AC3, AC11, AC12), EDGE-03, EDGE-04, CLR-01 (AC3).
    /// Scene is built in code: a camera facing a 4 x 2.8 m wall at z = 0 with a floor in front of it.
    /// Wall basis: right = +X, up = +Y, so Normal = +Z and the camera sits on the +Z side looking back.
    /// </summary>
    public class WindowDrawControllerTests
    {
        const float WallWidth = 4f;
        const float WallHeight = 2.8f;
        const float Thickness = 0.15f;
        const float EdgeMargin = 0.1f;   // WindowPlacementValidator.EdgeMargin
        const float Tolerance = 0.01f;

        SurfaceDefinition wall;
        SurfaceDefinition floor;
        RoomModel model;
        ModeManager modes;

        GameObject cameraObject;
        Camera roomCamera;
        SurfaceView wallView;
        SurfaceView floorView;
        MeshCollider wallCollider;

        GameObject controllerObject;
        WindowDrawController controller;

        GameObject readoutObject;
        CultureInfo culture;

        [SetUp]
        public void SetUp()
        {
            culture = CultureInfo.CurrentCulture;

            wall = new SurfaceDefinition
            {
                id = 0,
                name = "Wall 1",
                kind = SurfaceKind.Wall,
                origin = Vector3.zero,
                right = Vector3.right,
                up = Vector3.up,
                width = WallWidth,
                height = WallHeight,
                thickness = Thickness
            };

            floor = new SurfaceDefinition
            {
                id = 1,
                name = "Floor",
                kind = SurfaceKind.Floor,
                origin = new Vector3(0f, 0f, 4f),
                right = Vector3.right,
                up = Vector3.back,
                width = WallWidth,
                height = 4f,
                thickness = Thickness
            };

            model = new RoomModel(new[] { wall, floor });
            modes = new ModeManager(IsWallSelected);

            cameraObject = new GameObject("Room Camera");
            roomCamera = cameraObject.AddComponent<Camera>();
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(WallWidth * 0.5f, 1.4f, 5f),
                Quaternion.LookRotation(Vector3.back, Vector3.up));

            wallView = CreateSurfaceView(wall);
            floorView = CreateSurfaceView(floor);
            wallCollider = wallView.GetComponent<MeshCollider>();

            controllerObject = new GameObject("Window Draw Controller");
            controller = controllerObject.AddComponent<WindowDrawController>();
            controller.Configure(model, modes, roomCamera);
        }

        [TearDown]
        public void TearDown()
        {
            CultureInfo.CurrentCulture = culture;

            DestroyIfPresent(readoutObject);
            DestroyIfPresent(controllerObject);
            DestroyIfPresent(cameraObject);
            if (wallView != null) DestroyIfPresent(wallView.gameObject);
            if (floorView != null) DestroyIfPresent(floorView.gameObject);
        }

        // ---- WIN-02 AC3/AC4/AC12: the happy path ----

        [UnityTest]
        public IEnumerator Valid_drag_creates_one_window_and_opens_the_wall()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            Ray throughOpening = roomCamera.ScreenPointToRay(ScreenOf(wall, 1.4f, 1.4f));
            RaycastHit before;
            Assert.IsTrue(Physics.Raycast(throughOpening, out before, 20f) && before.collider == wallCollider,
                "The solid wall must block the ray before the window is cut.");

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            Assert.IsTrue(controller.IsDrawing, "The drag started on the selected wall, so drawing must begin.");
            Assert.AreEqual(wall.id, controller.TargetSurfaceId);

            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.AreEqual(1, controllerObject.transform.childCount, "The live preview object must exist.");
            Assert.AreEqual(1.0f, controller.PreviewRect.x, Tolerance);
            Assert.AreEqual(0.8f, controller.PreviewRect.width, Tolerance);
            Assert.AreEqual(0.8f, controller.PreviewRect.height, Tolerance);

            controller.OnDragEnd(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.IsFalse(controller.IsDrawing);
            Assert.AreEqual(-1, controller.TargetSurfaceId);
            Assert.AreEqual(0, controllerObject.transform.childCount, "The preview must be gone after release.");

            Assert.AreEqual(1, model.GetWindows(wall.id).Count);
            Rect2D rect = model.GetWindows(wall.id)[0].rect;
            Assert.AreEqual(1.0f, rect.x, Tolerance);
            Assert.AreEqual(1.0f, rect.y, Tolerance);
            Assert.AreEqual(0.8f, rect.width, Tolerance);
            Assert.AreEqual(0.8f, rect.height, Tolerance);

            Assert.IsTrue(wallView.Rebuild(), "The wall mesh and collider rebuild must succeed.");
            yield return new WaitForFixedUpdate();

            RaycastHit after;
            bool blocked = Physics.Raycast(throughOpening, out after, 20f);
            Assert.IsFalse(blocked, "A ray through the opening must pass through the wall (WIN-02 AC12).");
        }

        // ---- AD-027: one drag, one window ----

        /// <summary>
        /// The mode used to outlive the placement, and with it the AD-015 lock the surfaces list is given — so
        /// the row the new window had just added to the list refused every tap until the button was pressed.
        /// </summary>
        [UnityTest]
        public IEnumerator Placing_a_window_leaves_window_mode()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            controller.OnDragEnd(ScreenOf(wall, 1.8f, 1.8f));

            yield return null;

            Assert.AreEqual(1, model.GetWindows(wall.id).Count);
            Assert.AreEqual(Mode.Orbit, modes.Current, "the mode must return to Orbit once the window is placed");
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "the wall stays selected, so the mode can be re-entered");
        }

        /// <summary>A rejected drag places nothing, so there is nothing to end the mode for.</summary>
        [UnityTest]
        public IEnumerator A_rejected_drag_stays_in_window_mode()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragEnd(ScreenOf(wall, 1.0f, 1.0f));

            yield return null;

            Assert.AreEqual(0, model.GetWindows(wall.id).Count);
            Assert.AreEqual(Mode.WindowDraw, modes.Current, "a refused rectangle must not drop the user out of the mode");
        }

        // ---- EDGE-04: a tap is not a window ----

        [UnityTest]
        public IEnumerator Tap_without_drag_creates_no_window()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            Vector2 tap = ScreenOf(wall, 2.0f, 1.4f);
            controller.OnDragStart(tap);
            controller.OnDragMove(tap);
            controller.OnDragEnd(tap);
            yield return null;

            Assert.AreEqual(0, model.GetWindows(wall.id).Count, "A tap is below the minimum window size.");
            Assert.IsFalse(controller.IsDrawing);
            Assert.AreEqual(-1, controller.TargetSurfaceId);
            Assert.AreEqual(0, controllerObject.transform.childCount);
        }

        // ---- EDGE-03: an off-wall finger clamps, it never switches walls ----

        [UnityTest]
        public IEnumerator Drag_ending_off_the_wall_clamps_into_the_bounds_minus_the_margin()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 3.0f, 1.0f));
            Assert.IsTrue(controller.IsDrawing);

            // (5.0, 3.5) is past the top-right corner of a 4 x 2.8 m wall.
            controller.OnDragMove(ScreenOf(wall, 5.0f, 3.5f));
            yield return null;

            Assert.AreEqual(wall.id, controller.TargetSurfaceId, "Leaving the wall must not switch walls.");
            Assert.AreEqual(WallWidth - EdgeMargin, controller.PreviewRect.XMax, Tolerance);
            Assert.AreEqual(WallHeight - EdgeMargin, controller.PreviewRect.YMax, Tolerance);

            controller.OnDragEnd(ScreenOf(wall, 5.0f, 3.5f));
            yield return null;

            Assert.AreEqual(1, model.GetWindows(wall.id).Count);
            Rect2D rect = model.GetWindows(wall.id)[0].rect;
            Assert.AreEqual(3.0f, rect.x, Tolerance);
            Assert.AreEqual(1.0f, rect.y, Tolerance);
            Assert.AreEqual(WallWidth - EdgeMargin, rect.XMax, Tolerance);   // 3.9
            Assert.AreEqual(WallHeight - EdgeMargin, rect.YMax, Tolerance);  // 2.7
        }

        // ---- WIN-02 AC7: overlap is refused and the wall is untouched ----

        [UnityTest]
        public IEnumerator Overlapping_drag_is_rejected_and_drops_the_preview()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            controller.OnDragEnd(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.AreEqual(1, model.GetWindows(wall.id).Count, "Precondition: the first window exists.");
            Assert.IsTrue(wallView.Rebuild());
            yield return new WaitForFixedUpdate();

            // AD-027: the placement dropped the mode back to Orbit, so the second attempt has to re-enter.
            EnterWindowDraw();

            // Starts on solid wall right of the opening, ends inside it -> rect (1.5, 1.4) .. (2.2, 2.0).
            controller.OnDragStart(ScreenOf(wall, 2.2f, 1.4f));
            Assert.IsTrue(controller.IsDrawing, "The second drag starts on solid wall, so it must begin.");
            controller.OnDragMove(ScreenOf(wall, 1.5f, 2.0f));
            controller.OnDragEnd(ScreenOf(wall, 1.5f, 2.0f));
            yield return null;

            Assert.AreEqual(1, model.GetWindows(wall.id).Count, "The overlapping rectangle must be refused.");
            Assert.AreEqual(1.0f, model.GetWindows(wall.id)[0].rect.x, Tolerance);
            Assert.IsFalse(controller.IsDrawing);
            Assert.AreEqual(0, controllerObject.transform.childCount, "The preview must be gone after a rejection.");
        }

        // ---- CLR-01 AC3 / cancel paths ----

        [UnityTest]
        public IEnumerator Cancel_mid_drag_destroys_the_preview_and_creates_nothing()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.IsTrue(controller.IsDrawing);
            Assert.AreEqual(1, controllerObject.transform.childCount);

            controller.CancelDraw();
            yield return null;

            Assert.IsFalse(controller.IsDrawing);
            Assert.AreEqual(-1, controller.TargetSurfaceId);
            Assert.AreEqual(0, controllerObject.transform.childCount);
            Assert.AreEqual(0, model.GetWindows(wall.id).Count);
        }

        [UnityTest]
        public IEnumerator Leaving_window_mode_mid_drag_cancels_through_the_mode_manager()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;
            Assert.IsTrue(controller.IsDrawing);

            Assert.IsTrue(modes.TrySet(Mode.Orbit), "Orbit is always reachable.");
            yield return null;

            Assert.AreEqual(Mode.Orbit, modes.Current);
            Assert.IsFalse(controller.IsDrawing, "DrawCancelRequested must cancel the in-progress draw.");
            Assert.AreEqual(-1, controller.TargetSurfaceId);
            Assert.AreEqual(0, controllerObject.transform.childCount);
            Assert.AreEqual(0, model.GetWindows(wall.id).Count);
        }

        // ---- WIN-02 AC11: a drag that starts anywhere but the selected wall creates nothing ----

        [UnityTest]
        public IEnumerator Drag_starting_on_the_floor_creates_nothing()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();

            controller.OnDragStart(ScreenOf(floor, 2.0f, 2.0f));

            Assert.IsFalse(controller.IsDrawing, "The floor is not the selected wall.");
            Assert.AreEqual(-1, controller.TargetSurfaceId);
            Assert.AreEqual(0, controllerObject.transform.childCount, "No preview may be created.");

            controller.OnDragMove(ScreenOf(floor, 2.5f, 2.5f));
            controller.OnDragEnd(ScreenOf(floor, 2.5f, 2.5f));
            yield return null;

            Assert.AreEqual(0, model.GetWindows(wall.id).Count);
            Assert.AreEqual(0, model.GetWindows(floor.id).Count);
            Assert.AreEqual(0, controllerObject.transform.childCount);
        }

        // ---- WIN-02 / HUD-01 AC2: the readout reports the drag while it happens ----

        [UnityTest]
        public IEnumerator Readout_reports_the_live_rectangle_while_the_drag_grows()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();
            HudReadout hud = CreateReadout();

            Assert.AreEqual(wall.name.ToUpperInvariant(), hud.Caption, "Precondition: the wall's own readout.");

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.AreEqual("DRAWING", hud.Caption);
            Assert.AreEqual("on " + wall.name.ToLowerInvariant(), hud.Helper);
            Assert.IsFalse(hud.IsCountChipShown, "the chip counts placed windows, and this one is not placed yet");
            Assert.AreEqual(0.8f, controller.PreviewRect.width, Tolerance, "Precondition: 0.8 x 0.8 so far.");
            StringAssert.Contains(Metres(controller.PreviewRect), hud.Dimensions);

            string small = hud.Dimensions;

            controller.OnDragMove(ScreenOf(wall, 2.2f, 2.0f));
            yield return null;

            Assert.AreEqual(1.2f, controller.PreviewRect.width, Tolerance);
            Assert.AreEqual(1.0f, controller.PreviewRect.height, Tolerance);
            Assert.AreNotEqual(small, hud.Dimensions, "the readout follows the rectangle as it grows");
            StringAssert.Contains(Metres(controller.PreviewRect), hud.Dimensions);
        }

        /// <summary>AD-027 returns to Orbit with the wall still selected, so the wall is what comes back.</summary>
        [UnityTest]
        public IEnumerator Readout_returns_to_the_wall_after_a_placement()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();
            HudReadout hud = CreateReadout();

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;
            Assert.AreEqual("DRAWING", hud.Caption, "Precondition: the drag is being reported.");

            controller.OnDragEnd(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;

            Assert.AreEqual(1, model.GetWindows(wall.id).Count, "Precondition: the window was placed.");
            Assert.AreEqual(wall.name.ToUpperInvariant(), hud.Caption);
            StringAssert.Contains(WallWidth.ToString("0.00", CultureInfo.InvariantCulture), hud.Dimensions);
            StringAssert.Contains(WallHeight.ToString("0.00", CultureInfo.InvariantCulture), hud.Dimensions);
            Assert.AreEqual("window placed", hud.Helper);
            Assert.IsTrue(hud.IsCountChipShown, "the chip is back, counting the window that was just placed");
        }

        /// <summary>Neither a refused rectangle nor a cancel may leave the panel stuck on the drawing state.</summary>
        [UnityTest]
        public IEnumerator Readout_returns_to_the_wall_after_a_rejection_and_after_a_cancel()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();
            HudReadout hud = CreateReadout();

            // EDGE-04: a tap is under the minimum window size, so the model refuses the rectangle.
            Vector2 tap = ScreenOf(wall, 2.0f, 1.4f);
            controller.OnDragStart(tap);
            Assert.AreEqual("DRAWING", hud.Caption, "Precondition: the drag is being reported.");

            controller.OnDragEnd(tap);
            yield return null;

            Assert.AreEqual(0, model.GetWindows(wall.id).Count, "Precondition: the rectangle was refused.");
            Assert.AreEqual(wall.name.ToUpperInvariant(), hud.Caption);
            Assert.AreEqual("0 windows placed", hud.Helper);
            Assert.IsFalse(hud.IsCountChipShown);

            // A rejection stays in window mode (WIN-02 AC7), so the next drag can start straight away.
            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 1.8f, 1.8f));
            yield return null;
            Assert.AreEqual("DRAWING", hud.Caption);

            controller.CancelDraw();
            yield return null;

            Assert.AreEqual(wall.name.ToUpperInvariant(), hud.Caption, "a cancel puts the wall back as well");
            Assert.AreEqual("0 windows placed", hud.Helper);
        }

        /// <summary>HUD-01 AC2: a device on a comma-decimal locale must still read "1.20", never "1,20".</summary>
        [UnityTest]
        public IEnumerator Readout_keeps_the_live_rectangle_invariant_culture()
        {
            yield return new WaitForFixedUpdate();
            EnterWindowDraw();
            HudReadout hud = CreateReadout();

            // Built by hand rather than looked up by name, so the test does not depend on the ICU data a given
            // player carries: all that matters is that the ambient culture separates decimals with a comma.
            var commaDecimal = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            commaDecimal.NumberFormat.NumberDecimalSeparator = ",";
            CultureInfo.CurrentCulture = commaDecimal;

            controller.OnDragStart(ScreenOf(wall, 1.0f, 1.0f));
            controller.OnDragMove(ScreenOf(wall, 2.2f, 2.0f));
            yield return null;

            Assert.AreEqual("DRAWING", hud.Caption, "Precondition: the drag is being reported.");
            StringAssert.Contains(".", hud.Dimensions, "the kit shows a decimal point: " + hud.Dimensions);
            StringAssert.DoesNotContain(",", hud.Dimensions, "the device locale must not leak in: " + hud.Dimensions);
            StringAssert.Contains(Metres(controller.PreviewRect), hud.Dimensions);
        }

        // ---- helpers ----

        /// <summary>
        /// The kit's Readout on a bare canvas, bound to the same two objects RoomBootstrap.Compose binds it to
        /// (AD-022, AD-025). Built per test rather than in SetUp: only these four need it.
        /// </summary>
        HudReadout CreateReadout()
        {
            readoutObject = new GameObject("HUD Canvas", typeof(Canvas));

            HudReadout hud = HudReadout.Create(readoutObject.transform, Vector2.zero, new Vector2(250f, 92f));
            hud.Configure(model, controller);
            return hud;
        }

        /// <summary>The readout's dimensions band, minus the trailing unit tag.</summary>
        static string Metres(Rect2D rect)
        {
            return rect.width.ToString("0.00", CultureInfo.InvariantCulture) + "  ×  " +
                   rect.height.ToString("0.00", CultureInfo.InvariantCulture);
        }

        bool IsWallSelected()
        {
            int? id = model.SelectedSurfaceId;
            if (!id.HasValue) return false;

            SurfaceDefinition surface = model.GetSurface(id.Value);
            return surface != null && surface.kind == SurfaceKind.Wall;
        }

        void EnterWindowDraw()
        {
            model.Select(wall.id);
            Assert.AreEqual(wall.id, model.SelectedSurfaceId, "Precondition: the wall is selected.");
            Assert.IsTrue(modes.TrySet(Mode.WindowDraw), "Precondition: window mode is reachable.");
        }

        SurfaceView CreateSurfaceView(SurfaceDefinition surface)
        {
            var go = new GameObject(surface.name);

            // SurfaceMeshBuilder emits surface-local vertices, so the GameObject carries origin/right/up.
            go.transform.SetPositionAndRotation(surface.origin, Quaternion.LookRotation(surface.Normal, surface.up));
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>();

            SurfaceView view = go.AddComponent<SurfaceView>();
            view.Initialize(surface, model);
            return view;
        }

        Vector2 ScreenOf(SurfaceDefinition surface, float localX, float localY)
        {
            return roomCamera.WorldToScreenPoint(surface.LocalToWorld(localX, localY));
        }

        static void DestroyIfPresent(GameObject go)
        {
            if (go != null) Object.DestroyImmediate(go);
        }
    }
}
