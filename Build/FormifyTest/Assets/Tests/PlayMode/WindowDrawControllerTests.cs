using System.Collections;
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

        [SetUp]
        public void SetUp()
        {
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

        // ---- helpers ----

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
